using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TramUrWay.Android
{
    public class Transport
    {
        public Route Route { get; set; }
        public TimeStep TimeStep { get; set; }

        public Step Step { get; set; }
        public float Progress { get; set; }
        public float NextProgress { get; set; }
    }

    public static class TransportExtensions
    {
        public static void Update(this List<Transport> me, IEnumerable<TimeStep> steps, DateTime dateTime)
        {
            // Keep only the first steps for each stop
            steps = steps.GroupBy(s => s.Step).Select(g => g.OrderBy(s => s.Date).First()).ToArray();

            // Sort by lines
            Dictionary<Line, List<Transport>> lineTransports = me.GroupBy(t => t.Route.Line).ToDictionary(g => g.Key, g => g.ToList());
            Dictionary<Line, List<TimeStep>> lineTimeSteps = steps.GroupBy(t => t.Step.Route.Line).ToDictionary(g => g.Key, g => g.ToList());
            Line[] lines = lineTransports.Keys.Concat(lineTimeSteps.Keys).Distinct().OrderBy(l => l.Id).ToArray();

            // Only keep relevant time steps
            foreach (Line line in lines)
            {
                List<TimeStep> timeSteps;
                if (!lineTimeSteps.TryGetValue(line, out timeSteps))
                    timeSteps = new List<TimeStep>();

                foreach (Route route in line.Routes)
                {
                    Dictionary<Step, TimeStep> stepTimeSteps = timeSteps.GroupBy(s => s.Step)
                                                                        .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Date).First());
                    Step step = null;
                    Step nextStep = route.Steps.First();

                    for (; nextStep != null; step = nextStep, nextStep = nextStep.Next)
                    {
                        TimeStep nextTimeStep;
                        if (!stepTimeSteps.TryGetValue(nextStep, out nextTimeStep))
                            continue;

                        TimeStep timeStep = null;
                        if (step != null)
                            stepTimeSteps.TryGetValue(step, out timeStep);

                        if (step == null || (timeStep != null && timeStep.Date < nextTimeStep.Date))
                        {
                            timeSteps.Remove(nextTimeStep);
                            //if (nextTimeStep != null)
                            //    timeSteps.Remove(nextTimeStep);
                        }
                    }
                }
            }

            // Update existing trasports
            foreach (Line line in lines)
            {
                List<Transport> transports;
                if (!lineTransports.TryGetValue(line, out transports))
                    continue;

                List<TimeStep> timeSteps;
                if (!lineTimeSteps.TryGetValue(line, out timeSteps))
                    timeSteps = new List<TimeStep>();

                foreach (Transport transport in transports.ToArray())
                {
                    // First, try to find a timestep matching the current transport
                    TimeStep timeStep = timeSteps.Where(t => t.Step == transport.TimeStep.Step)
                                                 .OrderBy(t => Math.Abs((t.Date - transport.TimeStep.Date).TotalSeconds))
                                                 .FirstOrDefault();

                    // Then try to find a timestep with the next step
                    if (timeStep == null && transport.TimeStep.Step.Next != null)
                    {
                        timeStep = timeSteps.Where(t => t.Step == transport.TimeStep.Step.Next)
                                            .OrderBy(t => Math.Abs((t.Date - transport.TimeStep.Date + (transport.TimeStep.Step.Duration ?? TimeSpan.FromMinutes(2))).TotalSeconds))
                                            .FirstOrDefault();
                    }

                    // If no time step is found, discard this transport...
                    if (timeStep == null)
                    {
                        me.Remove(transport);
                        continue;
                    }

                    // Update the transport
                    transport.Step = timeStep.Step.Previous;
                    transport.TimeStep = timeStep;

                    timeSteps.Remove(timeStep);
                }
            }

            // Add new transports
            foreach (Line line in lines)
            {
                List<TimeStep> timeSteps;
                if (!lineTimeSteps.TryGetValue(line, out timeSteps))
                    timeSteps = new List<TimeStep>();

                if (timeSteps.Count == 0)
                    continue;

                foreach (Route route in line.Routes)
                {
                    Dictionary<Step, TimeStep> stepTimeSteps = timeSteps.GroupBy(s => s.Step)
                                                                        .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Date).First());

                    foreach (Step step in route.Steps)
                    {
                        TimeStep timeStep;
                        if (!stepTimeSteps.TryGetValue(step, out timeStep))
                            continue;

                        Transport transport = new Transport()
                        {
                            Route = route,
                            Step = timeStep.Step.Previous,
                            TimeStep = timeStep
                        };

                        me.Add(transport);
                    }
                }
            }

            // Recompute progresses for each transport
            me.UpdateProgress(dateTime);
        }
        public static void UpdateProgress(this List<Transport> me, DateTime dateTime)
        {
            // Recompute progresses for each transport
            foreach (Transport transport in me)
            {
                if (transport.Step == null)
                {
                    transport.Progress = transport.NextProgress = 0;
                    continue;
                }

                TimeSpan diff = transport.TimeStep.Date - dateTime;
                TimeSpan duration = transport.Step.Duration ?? TimeSpan.Zero;

                if (duration == TimeSpan.Zero)
                    duration = TimeSpan.FromMinutes(2);

                float progress = (float)(1 - diff.TotalMinutes / duration.TotalMinutes);
                if (progress < 0) progress = 0;
                if (progress > 1) progress = 1;

                float nextProgress = (float)(1 - diff.Subtract(TimeSpan.FromSeconds(1)).TotalMinutes / duration.TotalMinutes);
                if (nextProgress < 0) nextProgress = 0;
                if (nextProgress > 1) nextProgress = 1;

                transport.Progress = transport.Step.Speed.Evaluate(progress);
                transport.NextProgress = transport.Step.Speed.Evaluate(nextProgress);
            }
        }
    }
}