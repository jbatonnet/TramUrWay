﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TramUrWay.Android;

namespace TramUrWay.Baker
{
    internal class BakerV1
    {
        private const string inputDirectory = @"..\..\..\Data\Hiver 2015";

        public static Line[] Lines { get; private set; }
        public static Dictionary<int, string> StepNames { get; } = new Dictionary<int, string>();

        public static Line[] Bake()
        {
            // Load data
            LoadStations();

            // Patch wrong info
            PatchData();

            // Setup other info
            LoadImages();
            LoadTimeTables();
            LoadDurations();
            LoadTrajectories();
            LoadSpeedCurves();


            // Temp
            /*DateTime firstDate = new DateTime(2016, 5, 6, 15, 30, 0);
            DateTime secondDate = firstDate.AddSeconds(30);

            TimeStep[] firstTimeSteps = Lines.Where(l => l.Id < 6).SelectMany(l => l.Routes.SelectMany(r => r.Steps.SelectMany(s => r.TimeTable?.GetStepsFromStep(s, firstDate)?.Take(3) ?? Enumerable.Empty<TimeStep>()))).ToArray();
            TimeStep[] secondTimeSteps = Lines.Where(l => l.Id < 6).SelectMany(l => l.Routes.SelectMany(r => r.Steps.SelectMany(s => r.TimeTable?.GetStepsFromStep(s, secondDate)?.Take(3) ?? Enumerable.Empty<TimeStep>()))).ToArray();

            List<Transport> transports = new List<Transport>();
            transports.Update(firstTimeSteps, firstDate);
            transports.Update(secondTimeSteps, secondDate);*/

            //TestSearch();

            return Lines;
        }

        private static void LoadStations()
        {
            List<Line> lines = new List<Line>();

            string content = File.ReadAllText(Path.Combine(inputDirectory, "getAll.json"));
            JObject data = JsonConvert.DeserializeObject(content) as JObject;
            JArray linesData = data["lines"] as JArray;

            foreach (JToken lineData in linesData)
            {
                int lineId = lineData["route_number"].Value<int>();
                LineType lineType = lineId < 6 ? LineType.Tram : LineType.Bus;
                string lineName = (lineType == LineType.Tram ? "Tramway" : "Bus") + " ligne " + lineId;
                string lineColor = lineData["color"].Value<string>();

                Line line = new Line() { Id = lineId, Name = lineName, Color = Convert.ToInt32(lineColor, 16), Type = lineType };
                List<Stop> lineStops = new List<Stop>();
                List<Route> lineRoutes = new List<Route>();

                JArray stopsData = lineData["stops"] as JArray;
                foreach (JToken stopData in stopsData)
                {
                    int stopId = stopData["stop_id"].Value<int>();

                    if (stopData.Children().OfType<JProperty>().Any(p => p.Name == "name"))
                    {
                        string stopName = stopData["name"].Value<string>();
                        float stopLatitude = (float)stopData["stop_lat"].Value<double>();
                        float stopLongitude = (float)stopData["stop_lon"].Value<double>();

                        Stop stop = new Stop() { Id = stopId, Name = stopName, Position = new Position(stopLatitude, stopLongitude), Line = line };
                        lineStops.Add(stop);
                    }
                    else
                    {
                        Stop stop = new Stop() { Id = stopId, Line = line };
                        lineStops.Add(stop);
                    }
                }
                line.Stops = lineStops.ToArray();

                // For now, skip lines 18 and > 19
                if (line.Id == 18 || line.Id > 19)
                    continue;

                Route currentRoute = null;
                List<Step> routeSteps = new List<Step>();
                int routeId = 0;
                int lastOrder = 0;

                JArray routesData = lineData["routes"] as JArray;
                foreach (JToken routeData in routesData)
                {
                    int stopId = routeData["stop_id"].Value<int>();
                    Stop stop = lineStops.First(s => s.Id == stopId);

                    if (routeData.Children().OfType<JProperty>().Any(p => p.Name == "stop_order"))
                    {
                        string stopName = routeData["stop_name"].Value<string>();
                        bool stepPartial = routeData["partiel"].Value<int>() != 0;
                        int stepOrder = routeData["stop_order"].Value<int>();
                        string stepDirection = routeData["direction"].Value<string>();

                        StepNames[stopId] = stopName;
                        //stop.Name = stopName; // Re-update stop name as it can be wrong

                        if (currentRoute == null || stepOrder < lastOrder)
                        {
                            if (currentRoute != null)
                            {
                                currentRoute.Steps = routeSteps.ToArray();
                                lineRoutes.Add(currentRoute);
                            }

                            routeSteps.Clear();
                            currentRoute = new Route() { Id = routeId++, Line = line };
                        }

                        lastOrder = stepOrder;

                        Step step = new Step() { Stop = stop, Direction = stepDirection, Partial = stepPartial, Route = currentRoute };
                        routeSteps.Add(step);
                    }
                    else
                    {
                        Step step = new Step() { Stop = stop, Route = currentRoute };
                        routeSteps.Add(step);
                    }
                }

                if (currentRoute != null)
                {
                    currentRoute.Steps = routeSteps.ToArray();
                    lineRoutes.Add(currentRoute);
                }

                foreach (Route route in lineRoutes)
                {
                    for (int i = 0; i < route.Steps.Length; i++)
                    {
                        route.Steps[i].Previous = i > 0 ? route.Steps[i - 1] : null;
                        route.Steps[i].Next = i < route.Steps.Length - 1 ? route.Steps[i + 1] : null;
                    }
                }

                line.Routes = lineRoutes.ToArray();
                lines.Add(line);
            }

            Lines = lines.ToArray();
        }
        private static void LoadImages()
        {
            foreach (Line line in Lines)
            {
                string path = Path.Combine(inputDirectory, $@"L{line.Id}\L{line.Id}.png");
                if (!File.Exists(path))
                    continue;

                using (Image image = Image.FromFile(path))
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    image.Save(memoryStream, ImageFormat.Png);
                    line.Image = memoryStream.ToArray();
                }
            }
        }
        private static void LoadTimeTables()
        {
            foreach (Line line in Lines)
                foreach (Route route in line.Routes)
                {
                    TimeTable timeTable = new TimeTable() { Route = route };

                    string[] files = { "lun-jeu", "ven", "sam", "dim" };

                    foreach (string file in files)
                    {
                        string path = Path.Combine(inputDirectory, $@"L{route.Line.Id}\L{route.Line.Id}.R{route.Id}.{file}.csv");
                        FileInfo fileInfo = new FileInfo(path);

                        if (!fileInfo.Exists)
                        {
                            timeTable = null;
                            break;
                        }

                        Stream stream = fileInfo.OpenRead();
                        TimeSpan?[,] tableData = LoadTimeTable(route, stream);

                        switch (file)
                        {
                            case "lun-jeu": timeTable.WeekTable = tableData; break;
                            case "ven": timeTable.FridayTable = tableData; break;
                            case "sam": timeTable.SaturdayTable = tableData; break;
                            case "dim": timeTable.SundayTable = tableData; break;
                            case "vac": timeTable.HolidaysTable = tableData; break;
                        }
                    }

                    route.TimeTable = timeTable;
                }
        }
        private static void LoadDurations()
        {
            // Compute average step duration using time tables
            foreach (Line line in Lines)
                foreach (Route route in line.Routes)
                {
                    for (int i = 0; i < route.Steps.Length - 1; i++)
                        route.Steps[i].Duration = TimeSpan.FromMinutes(2); // Default to 2 minutes

                    if (route.TimeTable == null)
                        continue;

                    TimeSpan?[,] table = route.TimeTable.SundayTable ?? route.TimeTable.SaturdayTable ?? route.TimeTable.FridayTable ?? route.TimeTable.WeekTable;
                    if (table == null)
                        continue;

                    List<TimeSpan>[] durations = Enumerable.Range(0, route.Steps.Length - 1)
                                                           .Select(i => new List<TimeSpan>())
                                                           .ToArray();

                    for (int i = 0; i < route.Steps.Length - 1; i++)
                        for (int j = 0; j < table.GetLength(0); j++)
                        {
                            TimeSpan? left = table[j, i];
                            TimeSpan? right = table[j, i + 1];

                            if (left != null && right != null && right.Value > left.Value)
                                durations[i].Add(right.Value.Subtract(left.Value));
                        }

                    for (int i = 0; i < route.Steps.Length - 1; i++)
                        if (durations[i].Count > 0)
                            route.Steps[i].Duration = new TimeSpan((long)durations[i].Average(t => t.Ticks));
                }
        }
        private static void LoadTrajectories()
        {
            Regex positionRegex = new Regex(@"^[0-9.]+,[0-9.]+(,[0-9.]+)?$", RegexOptions.Compiled);

            // Read trajectories
            foreach (Line line in Lines)
                foreach (Route route in line.Routes)
                {
                    for (int i = 0; i < route.Steps.Length - 1; i++)
                    {
                        Step step = route.Steps[i];
                        Step nextStep = route.Steps[i + 1];

                        step.Trajectory = new TrajectoryStep[] { new TrajectoryStep() { Index = 0, Position = step.Stop.Position } };
                    }

                    Step last = route.Steps.Last();
                    last.Trajectory = new TrajectoryStep[] { new TrajectoryStep() { Index = 0, Position = last.Stop.Position } };

                    string path = Path.Combine(inputDirectory, $@"L{line.Id}\L{line.Id}.R{route.Id}.Trajectory.txt");
                    FileInfo file = new FileInfo(path);

                    if (!file.Exists)
                        continue;

                    using (StreamReader streamReader = new StreamReader(file.OpenRead()))
                    {
                        Step step = null;
                        List<TrajectoryStep> trajectory = new List<TrajectoryStep>();

                        while (true)
                        {
                            string data = streamReader.ReadLine()?.Trim();
                            if (data == "")
                                continue;
                            if (data == null)
                                break;

                            if (!positionRegex.IsMatch(data))
                            {
                                if (step != null && trajectory.Count > 0)
                                    step.Trajectory = trajectory.Take(trajectory.Count - 1).ToArray();

                                step = route.Steps.First(s => Likes(s.Stop.Name, data));
                                trajectory.Clear();
                            }
                            else
                            {
                                string[] positionParts = data.Split(',');

                                // Trajectory position is reversed
                                Position position = new Position(float.Parse(positionParts[1], CultureInfo.InvariantCulture), float.Parse(positionParts[0], CultureInfo.InvariantCulture));
                                trajectory.Add(new TrajectoryStep() { Index = 0, Position = position });
                            }
                        }

                        if (step != null && trajectory.Count > 0)
                            step.Trajectory = trajectory.Take(trajectory.Count - 1).ToArray();
                    }
                }

            // Compute path length
            foreach (Line line in Lines)
                foreach (Route route in line.Routes)
                    for (int i = 0; i < route.Steps.Length - 1; i++)
                    {
                        Step step = route.Steps[i];
                        Step nextStep = route.Steps[i + 1];

                        step.Length = (nextStep.Trajectory?.First()?.Position ?? nextStep.Stop.Position) - step.Trajectory.Last().Position;
                        for (int j = 0; j < step.Trajectory.Length - 1; j++)
                        {
                            TrajectoryStep trajectoryStep = step.Trajectory[j];
                            TrajectoryStep nextTrajectoryStep = step.Trajectory[j + 1];

                            step.Length += nextTrajectoryStep.Position - trajectoryStep.Position;
                        }

                        float length = 0;
                        for (int j = 1; j < step.Trajectory.Length; j++)
                        {
                            TrajectoryStep trajectoryStep = step.Trajectory[j - 1];
                            TrajectoryStep nextTrajectoryStep = step.Trajectory[j];

                            length += nextTrajectoryStep.Position - trajectoryStep.Position;
                            step.Trajectory[j].Index = length / step.Length;
                        }
                    }
        }
        private static void LoadSpeedCurves()
        {
            Curve defaultCurve = new Curve(x => Math.Pow(0.5 - Math.Sin(Math.PI / 2 + x * Math.PI) / 2, 1.2));

            foreach (Line line in Lines)
                foreach (Route route in line.Routes)
                    for (int i = 0; i < route.Steps.Length - 1; i++)
                    {
                        Step step = route.Steps[i];
                        step.Speed = defaultCurve;
                    }
        }

        private static void PatchData()
        {
            // Custom line names
            Lines.First(l => l.Id == 13).Name = "La navette";
            Lines.First(l => l.Id == 15).Name = "La ronde";

            // TaM data fixes
            {
                // Last stop of line 1 is Odysseum
                Lines.First(l => l.Id == 1).Routes.Last().Steps.Last().Stop.Name = "Odysseum";

                // First stop of lines 8 and 12 is Gare Saint-Roch (Pont de Sète)
                Lines.First(l => l.Id == 8).Routes.First().Steps.First().Stop.Name = "Gare Saint-Roch (Pont de Sète)";
                Lines.First(l => l.Id == 12).Routes.First().Steps.First().Stop.Name = "Gare Saint-Roch (Pont de Sète)";

                // Second stop of line 12 is Frédéric Peyson
                Lines.First(l => l.Id == 12).Routes.First().Steps.ElementAt(1).Stop.Name = "Frédéric Peyson";

                // Line 17 is nearly completely wrong ... thanks TaM
                foreach (Stop stop in Lines.First(l => l.Id == 17).Stops)
                    stop.Name = StepNames[stop.Id];

                // On line 9, Domaine de la Pompignane does not exists anymore
                foreach (Route route in Lines.First(l => l.Id == 9).Routes)
                {
                    Step step = route.Steps.First(s => s.Stop.Name == "Domaine de la Pompignane");

                    step.Previous.Next = step.Next;
                    step.Next.Previous = step.Previous;

                    route.Steps = route.Steps.Except(step).ToArray();
                }
            }
        }

        // Helpers
        private static TimeSpan?[,] LoadTimeTable(Route route, Stream stream)
        {
            Step[] partialSteps;
            List<TimeSpan?[]> partialTimes = new List<TimeSpan?[]>();

            // Read raw data
            using (StreamReader reader = new StreamReader(stream))
            {
                string header = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(header))
                    return new TimeSpan?[0, route.Steps.Length];

                string[] headerParts = header.Split(';');

                partialSteps = headerParts.Select(p => route.Steps.FirstOrDefault(s => Likes(s.Stop.Name, p))).ToArray();

                TimeSpan[] lastTimes = new TimeSpan[partialSteps.Length];
                while (true)
                {
                    string data = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(data))
                        break;

                    string[] dataParts = data.Split(';');
                    TimeSpan?[] times = new TimeSpan?[partialSteps.Length];

                    for (int i = 0; i < partialSteps.Length; i++)
                    {
                        TimeSpan time;
                        if (TimeSpan.TryParse(dataParts[i], out time))
                        {
                            if (time < lastTimes[i])
                                times[i] = time.Add(TimeSpan.FromDays(1));
                            else
                            {
                                times[i] = time;
                                lastTimes[i] = time;
                            }
                        }
                    }

                    partialTimes.Add(times);
                }
            }

            // Fill the missing stops
            Tuple<int, int, float>[] missingSteps = route.Steps.Select((s, i) =>
            {
                if (partialSteps.Contains(s))
                    return null;

                Step previousStep = route.Steps.Take(i).Reverse().First(l => partialSteps.Contains(l));
                Step nextStep = route.Steps.Skip(i).First(l => partialSteps.Contains(l));

                int previousPartialIndex = partialSteps.IndexOf(previousStep);
                int nextPartialIndex = partialSteps.IndexOf(nextStep);

                int previousFullIndex = route.Steps.IndexOf(previousStep);
                int nextFullIndex = route.Steps.IndexOf(nextStep);

                return new Tuple<int, int, float>(previousPartialIndex, nextPartialIndex, (float)(i - previousFullIndex) / (nextFullIndex - previousFullIndex));
            }).ToArray();

            TimeSpan?[,] fullTimes = new TimeSpan?[partialTimes.Count, route.Steps.Length];
            for (int i = 0; i < partialTimes.Count; i++)
            {
                for (int j = 0; j < route.Steps.Length; j++)
                {
                    int k = partialSteps.IndexOf(route.Steps[j]);

                    if (k >= 0)
                        fullTimes[i, j] = partialTimes[i][k];
                    else
                    {
                        int previousIndex = missingSteps[j].Item1;
                        int nextIndex = missingSteps[j].Item2;
                        float weight = missingSteps[j].Item3;

                        TimeSpan? previousTime = partialTimes[i][previousIndex];
                        TimeSpan? nextTime = partialTimes[i][nextIndex];

                        fullTimes[i, j] = (previousTime == null || nextTime == null) ? null : previousTime.Value.Add(TimeSpan.FromTicks((long)((nextTime.Value.Ticks - previousTime.Value.Ticks) * weight))) as TimeSpan?;
                    }
                }
            }

            return fullTimes;
        }
        private static bool Likes(string left, string right)
        {
            Dictionary<string, char> replacements = new Dictionary<string, char>()
            {
                { "éèê", 'e' },
                { "àâ", 'a' },
            };

            Func<char, char> replacer = c =>
            {
                foreach (var pair in replacements)
                    if (pair.Key.Contains(c))
                        return pair.Value;
                return c;
            };

            // Remove accents
            left = new string(left.Select(c => replacer(c)).ToArray());
            right = new string(right.Select(c => replacer(c)).ToArray());

            // ASCII normalize strings
            left = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(left.ToLowerInvariant()));
            right = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(right.ToLowerInvariant()));

            // Direct equality
            if (string.Compare(left, right, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) == 0)
                return true;

            return false;
        }
        private static string FormatTimeSpan(TimeSpan? value)
        {
            if (value == null)
                return null;

            string result = value?.ToString(@"hh\:mm");

            if (value?.Days > 0)
                result = value?.Days + "." + result;
            if (value?.Seconds > 0)
                result = result + ":" + value?.Seconds.ToString("d2");

            return result;
        }
    }
}