using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Content.Res;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TramUrWay.Android
{
    public class Assets
    {
        private Context context;
        private Dictionary<Route, TimeTable> timeTables = new Dictionary<Route, TimeTable>();

        public Assets(Context context)
        {
            this.context = context;
        }

        public TimeTable LoadTimeTable(Route route)
        {
            TimeTable timeTable;

            if (timeTables.TryGetValue(route, out timeTable))
                return timeTable;

            timeTable = new TimeTable() { Route = route };

            string[] files = { "lun-jeu", "ven", "sam", "dim" };
            string[] list = context.Assets.List("");

            foreach (string file in files)
            {
                string name = $"L{route.Line.Id}.R{route.Id}.{file}.csv";

                if (!list.Contains(name))
                {
                    timeTable = null;
                    break;
                }

                Stream stream = context.Assets.Open(name);
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

            timeTables.Add(route, timeTable);
            return timeTable;
        }
        public Line[] LoadLines()
        {
            string content;

            using (StreamReader reader = new StreamReader(context.Assets.Open("getAll.json")))
                content = reader.ReadToEnd();

            return ParseLines(content);
        }

        private TimeSpan?[,] LoadTimeTable(Route route, Stream stream)
        {
            Step[] partialSteps;
            List<TimeSpan?[]> partialTimes = new List<TimeSpan?[]>();

            // Read raw data
            using (StreamReader reader = new StreamReader(stream))
            {
                string header = reader.ReadLine();
                string[] headerParts = header.Split(';');

                partialSteps = headerParts.Select(p => route.Steps.FirstOrDefault(s => Utils.Likes(s.Stop.Name, p))).ToArray();

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
        private Line[] ParseLines(string content)
        {
            List<Line> lines = new List<Line>();

            JObject data = JsonConvert.DeserializeObject(content) as JObject;
            JArray linesData = data["lines"] as JArray;

            foreach (JToken lineData in linesData)
            {
                int lineId = lineData["route_number"].Value<int>();
                string lineColor = lineData["color"].Value<string>();

                Line line = new Line() { Id = lineId, Color = Convert.ToInt32(lineColor, 16) };
                List<Stop> lineStops = new List<Stop>();
                List<Route> lineRoutes = new List<Route>();

                JArray stopsData = lineData["stops"] as JArray;
                foreach (JToken stopData in stopsData)
                {
                    int stopId = stopData["stop_id"].Value<int>();

                    if (stopData.Children().OfType<JProperty>().Any(p => p.Name == "name"))
                    {
                        string stopName = stopData["name"].Value<string>();
                        double stopLatitude = stopData["stop_lat"].Value<double>();
                        double stopLongitude = stopData["stop_lon"].Value<double>();

                        Stop stop = new Stop() { Id = stopId, Name = stopName, Latitude = stopLatitude, Longitude = stopLongitude, Line = line };
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

                    if (routeData.Children().OfType<JProperty>().Any(p => p.Name == "stop_order"))
                    {
                        string stopName = routeData["stop_name"].Value<string>();
                        bool stepPartial = routeData["partiel"].Value<int>() != 0;
                        int stepOrder = routeData["stop_order"].Value<int>();
                        string stepDirection = routeData["direction"].Value<string>();

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

                        Step step = new Step() { Stop = lineStops.First(s => s.Id == stopId), Direction = stepDirection, Partial = stepPartial, Route = currentRoute };
                        routeSteps.Add(step);
                    }
                    else
                    {
                        Step step = new Step() { Stop = lineStops.First(s => s.Id == stopId), Route = currentRoute };
                        routeSteps.Add(step);
                    }
                }

                if (currentRoute != null)
                {
                    currentRoute.Steps = routeSteps.ToArray();
                    lineRoutes.Add(currentRoute);
                }

                line.Routes = lineRoutes.ToArray();
                lines.Add(line);
            }

            return lines.ToArray();
        }
    }
}