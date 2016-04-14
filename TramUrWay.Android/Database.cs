using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TramUrWay.Android
{
    public static class Database
    {
        private const string webServiceUrl = "http://www.tam-direct.com/webservice";

        public static Line[] Lines { get; private set; }

        private static Context context;
        private static DbConnection connection;

        private static WebClient webClient = new WebClient();
        private static Regex timeTableDetector = new Regex(@"L(?<Line>[0-9]+)\.R(?<Route>[0-9]+)\.(?<Type>lun-jeu|ven|sam|dim|vac)\.csv", RegexOptions.Compiled);

        public static void Initialize(Context context, DbConnection connection)
        {
            Database.context = context;
            Database.connection = connection;

            // Load lines from cached asset
            Lines = LoadLinesFromCache();

            // Load available time tables
            Task.Run(() =>
            {
                foreach (string asset in context.Assets.List(""))
                {
                    Match match = timeTableDetector.Match(asset);
                    if (!match.Success)
                        continue;

                    int lineId = int.Parse(match.Groups["Line"].Value);
                    int routeId = int.Parse(match.Groups["Route"].Value);
                    string type = match.Groups["Type"].Value;

                    Line line = Lines.FirstOrDefault(l => l.Id == lineId);
                    Route route = line.Routes.FirstOrDefault(r => r.Id == routeId);

                    if (route.TimeTable == null)
                        route.TimeTable = new TimeTable() { Route = route };

                    TimeSpan?[,] tableData = LoadTimeTable(route, context.Assets.Open(asset));
                    switch (type)
                    {
                        case "lun-jeu": route.TimeTable.WeekTable = tableData; break;
                        case "ven": route.TimeTable.FridayTable = tableData; break;
                        case "sam": route.TimeTable.SaturdayTable = tableData; break;
                        case "dim": route.TimeTable.SundayTable = tableData; break;
                        case "vac": route.TimeTable.HolidaysTable = tableData; break;
                    }
                }
            });

//#if DEBUG
            // Check database in debug mode
            CheckDatabase(connection);
//#endif
        }
        public static void CheckDatabase(DbConnection connection)
        {
            bool shouldReset = false;

            string[] checkQueries = new[]
            {
                "SELECT id FROM favorite_lines LIMIT 0",
                "SELECT id FROM favorite_stops LIMIT 0",
                "SELECT id, line_id, route_id, stop_id FROM widgets LIMIT 0",
                "SELECT key, value FROM config LIMIT 0"
            };

            try
            {
                foreach (string query in checkQueries)
                {
                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                shouldReset = true;
            }

            if (shouldReset)
            {
                string[] createQueries = new[]
                {
                    "DROP TABLE IF EXISTS favorite_lines",
                    "CREATE TABLE favorite_lines (id INTEGER NOT NULL, PRIMARY KEY (id))",

                    "DROP TABLE IF EXISTS favorite_stops",
                    "CREATE TABLE favorite_stops (id INTEGER NOT NULL, PRIMARY KEY (id))",

                    "DROP TABLE IF EXISTS widgets",
                    "CREATE TABLE widgets (id INTEGER NOT NULL, line_id INTEGER NOT NULL, route_id INTEGER NOT NULL, stop_id INTEGER NOT NULL)",

                    "DROP TABLE IF EXISTS config",
                    "CREATE TABLE config (key TEXT NOT NULL, VALUE TEXT, PRIMARY KEY (key))",
                };

                foreach (string query in createQueries)
                {
                    using (DbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public static Step FindStepByWidgetId(int widgetId)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT line_id, route_id, stop_id FROM widgets WHERE id = {widgetId}";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    int lineId = reader.GetInt32(0);
                    int routeId = reader.GetInt32(1);
                    int stopId = reader.GetInt32(2);

                    Line line = GetLine(lineId);
                    Route route = line.Routes.FirstOrDefault(r => r.Id == routeId);
                    Step step = route.Steps.FirstOrDefault(s => s.Stop.Id == stopId);

                    return step;
                }
            }
        }
        public static IEnumerable<int> GetAllStepWidgets()
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id FROM widgets";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        yield return reader.GetInt32(0);
                }
            }
        }
        public static void RegisterStepWidget(int widgetId, Step step)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"INSERT INTO widgets (id, line_id, route_id, stop_id) VALUES ({widgetId}, {step.Route.Line.Id}, {step.Route.Id}, {step.Stop.Id})";
                command.ExecuteNonQuery();
            }
        }

        public static void SetConfigValue(string key, string value)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"INSERT OR REPLACE INTO config (key, value) VALUES ('{key}', '{value}')";
                command.ExecuteNonQuery();
            }
        }
        public static string GetConfigValue(string key)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT value FROM config WHERE key = '{key}'";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    return reader.IsDBNull(0) ? null : reader.GetString(0);
                }
            }
        }

        public static void AddFavoriteLine(Line line)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"INSERT OR IGNORE INTO favorite_lines (id) VALUES ({line.Id})";
                command.ExecuteNonQuery();
            }
        }
        public static void RemoveFavoriteLine(Line line)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"DELETE FROM favorite_lines WHERE id = {line.Id}";
                command.ExecuteNonQuery();
            }
        }
        public static IEnumerable<Line> GetFavoriteLines()
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id FROM favorite_lines";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        yield return GetLine(reader.GetInt32(0));
                }
            }
        }
        public static void AddFavoriteStop(Stop stop)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"INSERT OR IGNORE INTO favorite_stops (id) VALUES ({stop.Id})";
                command.ExecuteNonQuery();
            }
        }
        public static void RemoveFavoriteStop(Stop stop)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"DELETE FROM favorite_stops WHERE id = {stop.Id}";
                command.ExecuteNonQuery();
            }
        }
        public static IEnumerable<Stop> GetFavoriteStops()
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id FROM favorite_stops";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        yield return GetStop(reader.GetInt32(0));
                }
            }
        }

        public static Line GetLine(int id)
        {
            return Lines.FirstOrDefault(l => l.Id == id);
        }
        public static Stop GetStop(int id)
        {
            return Lines.SelectMany(l => l.Stops).FirstOrDefault(s => s.Id == id);
        }

        public static int GetIconForLine(Line line)
        {
            switch (line.Id)
            {
                case 1: return Resource.Drawable.L1;
                case 2: return Resource.Drawable.L2;
                case 3: return Resource.Drawable.L3;
                case 4: return Resource.Drawable.L4;
                case 6: return Resource.Drawable.L6;
                case 7: return Resource.Drawable.L7;
                case 8: return Resource.Drawable.L8;
                case 9: return Resource.Drawable.L9;
                case 10: return Resource.Drawable.L10;
                case 11: return Resource.Drawable.L11;
                case 12: return Resource.Drawable.L12;
                case 13: return Resource.Drawable.L13;
                case 14: return Resource.Drawable.L14;
                case 15: return Resource.Drawable.L15;
                case 16: return Resource.Drawable.L16;
                case 17: return Resource.Drawable.L17;
                case 19: return Resource.Drawable.L19;
                default: return 0;
            }
        }
        public static int GetResourceForLine(Line line)
        {
            if (line.Id <= 5)
                return Resource.Drawable.ic_tram;
            else
                return Resource.Drawable.ic_directions_bus;
        }
        public static Color GetColorForLine(Line line)
        {
            Color color;

            if (line.Color != null)
            {
                color = new Color((line.Color.Value >> 16) & 0xFF, (line.Color.Value >> 8) & 0xFF, line.Color.Value & 0xFF);
                color = new Color(color.R * 4 / 5, color.G * 4 / 5, color.B * 4 / 5);
            }
            else
                color = context.Resources.GetColor(Resource.Color.colorAccent);

            return color;
        }

        public static Line[] LoadLinesFromCache()
        {
            string content;

            using (StreamReader reader = new StreamReader(context.Assets.Open("getAll.json")))
                content = reader.ReadToEnd();

            return ParseLines(content);
        }
        public static Line[] LoadLinesFromService()
        {
            const string url = webServiceUrl + "/data.php?pattern=getLines";
            string content = webClient.DownloadString(url);

            return ParseLines(content);
        }
        private static Line[] ParseLines(string content)
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

        public static IEnumerable<TimeStep> GetLiveTimeSteps()
        {
            //string content;
            //using (StreamReader reader = new StreamReader(context.Assets.Open("getDetails.json")))
            //    content = reader.ReadToEnd();

            const string url = webServiceUrl + "/data.php?pattern=getDetails";
            string content = new WebClient().DownloadString(url);

            DateTime now = DateTime.Now;
            DayOfWeek referenceDay = now.DayOfWeek;
            DateTime referenceDate = now.DayOfWeek == referenceDay ? now.Date : now.Date.AddDays(-1);

            JObject data = JsonConvert.DeserializeObject(content) as JObject;
            TimeSpan lastTime = TimeSpan.Zero;

            JArray allersData = data["aller"] as JArray;
            foreach (JToken allerData in allersData)
            {
                int diff = allerData[5].Value<int>();
                if (diff <= 0)
                    continue;

                int lineId = allerData[0].Value<int>();
                bool theorical = allerData[2].Value<int>() != 0;
                TimeSpan time = TimeSpan.Parse(allerData[3].Value<string>());
                int stopId = allerData[4].Value<int>();
                string end = allerData[6].Value<string>();

                Line line = GetLine(lineId);
                if (line == null)
                    continue;

                Step step = line.Routes.SelectMany(r => r.Steps).FirstOrDefault(s => s.Stop.Id == stopId);
                if (step == null)
                    continue;

                Step destination = step.Route.Steps.FirstOrDefault(s => string.Compare(s.Stop.Name, end, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace) == 0);
                if (destination == null)
                    continue;

                if (time < lastTime)
                    time = time.Add(TimeSpan.FromDays(1));
                else
                    lastTime = time;

                yield return new TimeStep() { Step = step, Date = referenceDate.Add(time), Source = theorical ? TimeStepSource.Theorical : TimeStepSource.Online, Destination = destination };
            }

            lastTime = TimeSpan.Zero;

            JArray retoursData = data["retour"] as JArray;
            foreach (JToken retourData in retoursData)
            {
                int diff = retourData[5].Value<int>();
                if (diff <= 0)
                    continue;

                int lineId = retourData[0].Value<int>();
                bool theorical = retourData[2].Value<int>() != 0;
                TimeSpan time = TimeSpan.Parse(retourData[3].Value<string>());
                int stopId = retourData[4].Value<int>();
                string end = retourData[6].Value<string>();

                Line line = GetLine(lineId);
                if (line == null)
                    continue;

                Step step = line.Routes.SelectMany(r => r.Steps).FirstOrDefault(s => s.Stop.Id == stopId);
                if (step == null)
                    continue;

                Step destination = step.Route.Steps.FirstOrDefault(s => string.Compare(s.Stop.Name, end, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace) == 0);
                if (destination == null)
                    continue;

                if (time < lastTime)
                    time = time.Add(TimeSpan.FromDays(1));
                else
                    lastTime = time;

                yield return new TimeStep() { Step = step, Date = referenceDate.Add(time), Source = theorical ? TimeStepSource.Theorical : TimeStepSource.Online, Destination = destination };
            }
        }
        public static string GetReadableTime(TimeStep timeStep, DateTime now, bool useLongStyle = true)
        {
            TimeSpan diff = timeStep.Date - now;
            int minutes = (int)diff.TotalMinutes;

            if (diff.TotalMinutes < 0)
                return "A quai";
            else if (minutes < 1)
                return "Proche";
            else if (minutes >= 60)
                return useLongStyle ? "Plus d'une heure" : "> 1 heure";
            else
            {
                if (App.EnableTamBug && now.Day != timeStep.Date.Day)
                    return $"24 h {minutes} min";
                else
                    return $"{minutes} min";
            }
        }
        public static string GetReadableTimes(TimeStep[] timeSteps, DateTime now, bool useLongStyle = true)
        {
            TimeStep[] steps = new TimeStep[timeSteps.Length];

            int index = 0;

            foreach (TimeStep step in timeSteps)
            {
                steps[index] = step;

                if ((step.Date - now).TotalMinutes > 60)
                    break;

                index++;
            }

            return steps.Where(s => s != null).Join(s => GetReadableTime(s, now, useLongStyle), ", ");
        }

        private static TimeSpan?[,] LoadTimeTable(Route route, Stream stream)
        {
            Step[] partialSteps;
            List<TimeSpan?[]> partialTimes = new List<TimeSpan?[]>();

            // Read raw data
            using (StreamReader reader = new StreamReader(stream))
            {
                string header = reader.ReadLine();
                string[] headerParts = header.Split(';');

                partialSteps = headerParts.Select(p => route.Steps.FirstOrDefault(s => string.Compare(s.Stop.Name, p, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace) == 0)).ToArray();

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
    }
}