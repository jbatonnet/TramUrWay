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

        private static Association<Line, int> lineIds = new Association<Line, int>();
        private static Association<Line, int> lineTamIds = new Association<Line, int>();
        private static Association<Line, int> lineCitywayIds = new Association<Line, int>();

        private static Association<Stop, int> stopIds = new Association<Stop, int>();
        private static Association<Stop, int> stopTamIds = new Association<Stop, int>();
        private static Association<Stop, int> stopCitywayIds = new Association<Stop, int>();

        public static Line[] Bake()
        {
            // Load data
            LoadStations();

            return Lines;
        }

        private static Line GetLineFromCitywayId(int citywayId)
        {
            Line line;

            if (lineCitywayIds.TryGetLeft(citywayId, out line))
                return line;
            else
                return null;
        }
        private static Stop GetStopFromId(int id)
        {
            Stop stop;

            if (stopIds.TryGetLeft(id, out stop))
                return stop;
            else
                return null;
        }
        private static Stop GetStopFromTamId(int tamId)
        {
            Stop stop;

            if (stopTamIds.TryGetLeft(tamId, out stop))
                return stop;
            else
                return null;
        }
        private static Stop GetStopFromCitywayId(int citywayId)
        {
            Stop stop;

            if (stopCitywayIds.TryGetLeft(citywayId, out stop))
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

                    line.Metadata["urban"] = (bool)reader["urban"];

                    lines.Add(line);
                }
            }

            foreach (Line line in lines.Where(l => l.Id < 6))
            {
                List<Stop> lineStops = new List<Stop>();
                List<Route> lineRoutes = new List<Route>();

                using (DbCommand command = sqliteConnection.CreateCommand("SELECT * FROM LINE_STOP ls INNER JOIN STOP s ON s._id = ls.stop WHERE ls.line = " + lineIds[line]))
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
                        stop.Line = line;

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
                            Stop stop = GetStopFromId(stopId);
                            if (stop == null)
                                throw new Exception();

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
            }

            Lines = lines.Where(l => l.Id < 6).ToArray();
        }

        // Helpers
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