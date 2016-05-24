using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
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
    internal class BakerV2
    {
        private const string inputDirectory = @"..\..\..\Data";

        public static Line[] Lines { get; private set; }

        private static Dictionary<Line, int> lineIds = new Dictionary<Line, int>();
        private static Dictionary<Line, int> lineTamIds = new Dictionary<Line, int>();
        private static Dictionary<Line, int> lineCitywayIds = new Dictionary<Line, int>();

        private static Dictionary<Stop, int> stopIds = new Dictionary<Stop, int>();
        private static Dictionary<Stop, int> stopTamIds = new Dictionary<Stop, int>();
        private static Dictionary<Stop, int> stopCitywayIds = new Dictionary<Stop, int>();

        public static Line[] Bake()
        {
            // Load data
            LoadStations();

            return Lines;
        }

        private static Line GetLineFromCitywayId(int citywayId)
        {
            Line line;

            if (lineCitywayIds.TryGetKey(citywayId, out line))
                return line;
            else
                return null;
        }
        private static Stop GetStopFromTamId(int tamId)
        {
            Stop stop;

            if (stopTamIds.TryGetKey(tamId, out stop))
                return stop;
            else
                return null;
        }
        private static Stop GetStopFromCitywayId(int citywayId)
        {
            Stop stop;

            if (stopCitywayIds.TryGetKey(citywayId, out stop))
                return stop;
            else
                return null;
        }

        private static void LoadStations()
        {
            SQLiteConnection sqliteConnection = new SQLiteConnection("Data Source=" + inputDirectory + @"\referential_android.sqlite");
            sqliteConnection.Open();

            List<Line> lines = new List<Line>();

            using (DbCommand command = sqliteConnection.CreateCommand("SELECT * FROM LINE ORDER BY CAST(tam_id AS integer)"))
            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Line line = new Line();

                    int lineId, lineTamId, lineCitywayId;
                    lineIds.Add(line, lineId = (int)(long)reader["_id"]);
                    lineTamIds.Add(line, lineTamId = int.Parse((string)reader["tam_id"]));
                    lineCitywayIds.Add(line, lineCitywayId = int.Parse((string)reader["cityway_id"]));

                    line.Id = lineTamId;

                    int lineMode = (int)(long)reader["mode"];
                    line.Type = lineMode == 0 ? LineType.Tram : lineMode == 3 ? LineType.Bus : LineType.Unknown;

                    string lineShortName = (string)reader["short_name"];
                    line.Name = lineShortName.Length < 3 ? (line.Type == LineType.Tram ? "Tramway" : "Bus") + " ligne " + lineTamId : lineShortName;

                    string lineColor = (string)reader["color"];
                    line.Color = Convert.ToInt32(lineColor.TrimStart('#'), 16);

                    lines.Add(line);
                }
            }

            foreach (Line line in lines)
            {
                List<Stop> lineStops = new List<Stop>();
                List<Route> lineRoutes = new List<Route>();

                using (DbCommand command = sqliteConnection.CreateCommand("SELECT * FROM LINE_STOP ls INNER JOIN STOP s ON s.tam_id = ls.stop WHERE ls.line = " + lineIds[line]))
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Stop stop = new Stop();

                        int stopId, stopTamId, stopCitywayId;
                        stopIds.Add(stop, stopId = (int)(long)reader["_id"]);
                        stopTamIds.Add(stop, stopTamId = (int)(long)reader["tam_id"]);
                        stopCitywayIds.Add(stop, stopCitywayId = (int)(long)reader["cityway_id"]);

                        stop.Id = stopTamId;
                        stop.Name = (string)reader["stop_name"];

                        float stopLatitude = (float)(double)reader["latitude"];
                        float stopLongitude = (float)(double)reader["longitude"];
                        stop.Position = new Position(stopLatitude, stopLongitude);

                        lineStops.Add(stop);
                    }
                }

                line.Stops = lineStops.ToArray();

                List<int> lineRouteIds = new List<int>();

                using (DbCommand command = sqliteConnection.CreateCommand("SELECT DISTINCT direction FROM LINE_STOP ls WHERE ls.line = " + lineIds[line]))
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        lineRouteIds.Add(reader.GetInt32(0));
                }

                foreach (int lineRouteId in lineRouteIds)
                {
                    Route route = new Route() { Id = lineRouteId };

                    List<Step> routeSteps = new List<Step>();

                    using (DbCommand command = sqliteConnection.CreateCommand("SELECT * FROM LINE_STOP ls WHERE ls.line = " + lineIds[line] + " AND direction = " + lineRouteId))
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int stopId = (int)(long)reader["stop"];
                            Stop stop = GetStopFromTamId(stopId);

                            bool stepPartial = (bool)reader["main_section"];

                            Step step = new Step() { Stop = stop, Partial = stepPartial, Route = route };
                            routeSteps.Add(step);
                        }
                    }

                    route.Steps = routeSteps.ToArray();
                    lineRoutes.Add(route);
                }

                foreach (Route route in lineRoutes)
                {
                    for (int i = 0; i < route.Steps.Length; i++)
                    {
                        route.Steps[i].Direction = "Vers " + route.Steps.Last().Stop.Name;
                        route.Steps[i].Previous = i > 0 ? route.Steps[i - 1] : null;
                        route.Steps[i].Next = i < route.Steps.Length - 1 ? route.Steps[i + 1] : null;
                    }
                }

                line.Routes = lineRoutes.ToArray();
                lines.Add(line);
            }

            Lines = lines.ToArray();
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