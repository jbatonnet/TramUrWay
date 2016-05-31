using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TramUrWay.Android
{
    public class RouteLink
    {
        public string From { get; set; }
        public Step To { get; set; }
        public Line Line { get; set; }

        internal float Weight;

        public override string ToString()
        {
            return $"[{Line?.Number.ToString() ?? "W"}] {From} > {To.Stop.Name} ({Weight})";
        }
    }
    public class RouteSegment
    {
        public Step From { get; set; }
        public DateTime DateFrom { get; set; }

        public Step To { get; set; }
        public DateTime DateTo { get; set; }

        public Line Line { get; set; }
        public TimeStep[] TimeSteps { get; set; }

        public override string ToString()
        {
            return $"On L{Line.Number}, from {From.Stop.Name} ({DateFrom}) to {To.Stop.Name} ({DateTo})";
        }
    }

    public class RouteSearch
    {
        public class RouteSearchSettings
        {
            public bool AllowBusLinks { get; set; } = true;
            public bool AllowWalkLinks { get; set; } = true;

            public int WalkLinksCount { get; set; } = 3;
            public int BestRoutesCount { get; set; } = 3;

            public float TramWeight { get; set; } = 1.0f;
            public float BusWeight { get; set; } = 1.5f;
            public float WalkWeight { get; set; } = 2.0f;

            public float ChangeWeight { get; set; } = 60.0f;
        }

        public RouteSearchSettings Settings { get; } = new RouteSearchSettings();

        private Dictionary<string, List<RouteLink>> routeLinks = new Dictionary<string, List<RouteLink>>();

        public void Prepare(IEnumerable<Line> lines)
        {
            routeLinks.Clear();

            // Flush prior queries
            lines = lines.ToArray();

            // Build steps cache
            foreach (Line line in lines)
            {
                if (!Settings.AllowBusLinks && line.Type == LineType.Bus)
                    continue;

                foreach (Route route in line.Routes)
                    for (int i = 0; i < route.Steps.Length - 1; i++)
                    {
                        Step fromStep = route.Steps[i];
                        Step toStep = route.Steps[i + 1];

                        List<RouteLink> stepLinks;
                        if (!routeLinks.TryGetValue(fromStep.Stop.Name, out stepLinks))
                            routeLinks.Add(fromStep.Stop.Name, stepLinks = new List<RouteLink>());

                        if (stepLinks.Any(l => l.Line == fromStep.Route.Line && l.To.Stop.Name == toStep.Stop.Name))
                            continue;

                        TimeSpan duration = fromStep.Duration ?? TimeSpan.FromMinutes(0);
                        if (duration.TotalMinutes == 0)
                            duration = TimeSpan.FromMinutes(2);

                        stepLinks.Add(new RouteLink() { From = fromStep.Stop.Name, To = toStep, Line = line, Weight = (float)duration.TotalSeconds * (line.Type == LineType.Tram ? Settings.TramWeight : Settings.BusWeight) });
                    }
            }

            if (Settings.AllowWalkLinks)
            {
                // Find nearest steps
                Dictionary<string, List<RouteLink>> walkLinks = new Dictionary<string, List<RouteLink>>();
                Step[] allSteps = lines.SelectMany(l => l.Routes).SelectMany(r => r.Steps).ToArray();

                foreach (Step step in allSteps)
                {
                    IEnumerable<Step> sortedSteps = allSteps.Where(s => s.Stop.Name != step.Stop.Name)
                                                            .OrderBy(s => s.Stop.Position - step.Stop.Position);

                    List<RouteLink> stepLinks;
                    if (!walkLinks.TryGetValue(step.Stop.Name, out stepLinks))
                        walkLinks.Add(step.Stop.Name, stepLinks = new List<RouteLink>());

                    foreach (Step toStep in sortedSteps.Take(Settings.WalkLinksCount))
                        stepLinks.Add(new RouteLink() { From = step.Stop.Name, To = toStep, Line = null, Weight = (toStep.Stop.Position - step.Stop.Position) * Settings.WalkWeight });

                    stepLinks = stepLinks.GroupBy(l => l.To.Stop.Name).Select(g => g.OrderBy(l => l.Weight).First()).ToList();
                    stepLinks.Sort((l1, l2) => (int)(l1.Weight - l2.Weight));

                    walkLinks[step.Stop.Name] = stepLinks;
                }

                // Merge links
                foreach (var pair in walkLinks)
                {
                    List<RouteLink> stepLinks;
                    if (!routeLinks.TryGetValue(pair.Key, out stepLinks))
                        routeLinks.Add(pair.Key, stepLinks = new List<RouteLink>());

                    stepLinks.AddRange(pair.Value);
                }
            }
        }

        #region Path finding

        public IEnumerable<RouteLink[]> FindRoutes(Stop from, Stop to)
        {
            AutoResetEvent resultEvent = new AutoResetEvent(false);
            bool searchEnded = false;

            List<float> bestWeights = new List<float>(Settings.BestRoutesCount);
            ConcurrentQueue<RouteLink[]> bestRoutes = new ConcurrentQueue<RouteLink[]>();

            float maxWeight = to.Position - from.Position;
            bestWeights.Add(maxWeight);

            Action<RouteLink[], string, float> browseRoutes = null;
            browseRoutes = (route, last, weight) =>
            {
                List<RouteLink> links;
                if (!routeLinks.TryGetValue(last, out links))
                    return;

                RouteLink lastLink = route.LastOrDefault();

                foreach (RouteLink link in links)
                {
                    string linkTo = link.To.Stop.Name;

                    // Skip loops
                    if (route.Any(l => l.From == linkTo))
                        continue;

                    // Skip reusage of same line
                    if (lastLink != null && lastLink.Line != link.Line && route.Any(l => l.Line == link.Line))
                        continue;

                    // Avoid leaving the line we search
                    if (lastLink != null && lastLink.Line != link.Line && lastLink.Line == to.Line)
                        continue;

                    float linkWeight = weight + link.Weight;

                    // Add an arbitraty weight when changing line
                    if (lastLink?.Line != link?.Line)
                        linkWeight += Settings.ChangeWeight;

                    // Skip too long routes
                    if (linkWeight > maxWeight)
                        continue;

                    lock (bestWeights)
                    {
                        // Skip too long routes
                        if (linkWeight > bestWeights.First() * 2)
                            continue;

                        // Keep only shortest routes
                        if (bestWeights.Count == 3 && linkWeight > bestWeights.Last())
                            continue;
                    }

                    // Build the route
                    RouteLink[] linkRoute = route.Concat(link).ToArray();

                    // If the destination is found, register the answer
                    if (linkTo == to.Name)
                    {
                        lock (bestWeights)
                        {
                            if (bestWeights.Count == Settings.BestRoutesCount)
                                bestWeights.RemoveAt(Settings.BestRoutesCount - 1);

                            bestWeights.Add(linkWeight);
                            bestWeights.Sort();

                            bestRoutes.Enqueue(linkRoute);
                            resultEvent.Set();
                        }
                    }
                    else
                        browseRoutes(linkRoute, linkTo, linkWeight);
                }
            };

            // Start pathfinding
            Task routesTask = Task.Run(() =>
            {
                browseRoutes(new RouteLink[0], from.Name, 0);

                searchEnded = true;
                resultEvent.Set();
            });

            while (true)
            {
                resultEvent.WaitOne();

                RouteLink[] route;
                while (bestRoutes.TryDequeue(out route))
                    yield return route;

                if (searchEnded)
                    break;
            }

            yield break;
        }

        public async Task<RouteLink[][]> FindRoutesAsync(Stop from, Stop to)
        {
            return await Task.Run(() => FindRoutes(from, to).ToArray());
        }
        public async Task<RouteLink[][]> FindRoutesAsync(Stop from, Stop to, TimeSpan timeout)
        {
            return await Task.Run(() =>
            {
                DateTime end = DateTime.Now + timeout;
                List<RouteLink[]> routes = new List<RouteLink[]>();

                IEnumerable<RouteLink[]> routesEnumerable = FindRoutes(from, to);
                IEnumerator<RouteLink[]> routesEnumerator = routesEnumerable.GetEnumerator();

                while (true)
                {
                    Task<bool> moveNextTask = Task.Run(() => routesEnumerator.MoveNext());

                    // Exit if timed out
                    timeout = end - DateTime.Now;
                    if (!moveNextTask.Wait(timeout))
                        break;

                    // Exit of enumeration finished
                    if (moveNextTask.Result == false)
                        break;

                    routes.Add(routesEnumerator.Current);
                }

                routes.Sort((r1, r2) => (int)(r1.Sum(l => l.Weight) - r2.Sum(l => l.Weight)));
                return routes.ToArray();
            });
        }

        #endregion
        #region Time simulation

        public IEnumerable<RouteSegment[]> SimulateTimeStepsFrom(RouteLink[] route, DateTime date)
        {
            return SimulateTimeStepsFrom(route, date, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
        }
        public IEnumerable<RouteSegment[]> SimulateTimeStepsFrom(RouteLink[] route, DateTime date, TimeSpan lowerTolerance, TimeSpan upperTolerance)
        {
            RouteLink[] changes = route.Distinct(l => l.Line).ToArray();
            RouteLink last = route.Last();

            if (route.Any(l => l.To.Route.TimeTable == null))
                yield break;

            RouteLink firstLink = route.First();
            //Step firstStep = firstLink.Line?.Routes?.SelectMany(r => r.Steps)?.First(s => s.Stop.Name == firstLink.From && s.Next?.Stop?.Name == firstLink.To.Stop.Name);
            Step firstStep = firstLink.To.Route.Steps.First(s => s.Stop.Name == firstLink.From && s.Next?.Stop?.Name == firstLink.To.Stop.Name);

            // Setup simulation
            DateTime simulationDate = date;
            DateTime lowerBound = simulationDate - lowerTolerance;
            DateTime upperBound = simulationDate + upperTolerance;

            TimeStep[] timeSteps = firstStep.Route.TimeTable.GetStepsFromStep(firstStep, lowerBound).TakeWhile(s => s.Date <= upperBound).ToArray();

            TimeStep lastTimeStep;
            List<TimeStep> segmentTimeSteps;

            foreach (TimeStep timeStep in timeSteps)
            {
                List<RouteSegment> segments = new List<RouteSegment>();
                RouteSegment segment;

                lastTimeStep = timeStep;
                simulationDate = lastTimeStep.Date;

                Step lastStep = firstStep;
                foreach (RouteLink change in changes.Skip(1))
                {
                    segment = new RouteSegment() { From = lastStep, DateFrom = lastTimeStep.Date, Line = lastStep.Route.Line };
                    segments.Add(segment);

                    segmentTimeSteps = new List<TimeStep>();

                    while (lastStep.Stop.Name != change.From)
                    {
                        segmentTimeSteps.Add(lastTimeStep);

                        lastStep = lastStep.Next;
                        lastTimeStep = lastStep.Route.TimeTable.GetStepsFromStep(lastStep, simulationDate).FirstOrDefault();

                        simulationDate = lastTimeStep.Date;
                    }

                    segment.TimeSteps = segmentTimeSteps.ToArray();
                    segment.To = lastStep;
                    segment.DateTo = simulationDate;

                    //lastStep = change.Line?.Routes?.SelectMany(r => r.Steps)?.First(s => s.Stop.Name == change.From && s.Next?.Stop?.Name == change.To.Stop.Name);
                    lastStep = change.To.Route.Steps.First(s => s.Stop.Name == change.From && s.Next?.Stop?.Name == change.To.Stop.Name);
                    lastTimeStep = lastStep.Route.TimeTable.GetStepsFromStep(lastStep, simulationDate).FirstOrDefault();

                    if (lastTimeStep == null)
                        yield break;

                    simulationDate = lastTimeStep.Date;
                }

                segment = new RouteSegment() { From = lastStep, DateFrom = lastTimeStep.Date, Line = lastStep.Route.Line };
                segments.Add(segment);

                segmentTimeSteps = new List<TimeStep>();

                while (lastStep.Stop.Name != last.To.Stop.Name)
                {
                    segmentTimeSteps.Add(lastTimeStep);

                    lastStep = lastStep.Next;
                    lastTimeStep = lastStep.Route.TimeTable.GetStepsFromStep(lastStep, simulationDate).FirstOrDefault();

                    simulationDate = lastTimeStep.Date;
                }

                segment.TimeSteps = segmentTimeSteps.ToArray();
                segment.To = lastStep;
                segment.DateTo = simulationDate;

                yield return segments.ToArray();
            }
        }

        #endregion
    }
}