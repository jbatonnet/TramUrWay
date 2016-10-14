using System;
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
    class Program
    {
        private const string inputDirectory = @"..\..\..\Data\Hiver 2015";
        private const string outputDirectory = @"..\..\..\TramUrWay.Android\Assets";

        public static Line[] Lines { get; private set; }
        public static Dictionary<int, string> StepNames { get; } = new Dictionary<int, string>();

        public static void Main(string[] args)
        {
            // Load data
            Lines = BakerV1.Bake();

            // Dump everything
            DumpData();
            
            //TestSearch();
        }

        private static void DumpData()
        {
            foreach (Line line in Lines)
            {
                JArray lineStopsObject;
                JArray lineRoutesObject;

                JObject lineObject = new JObject
                {
                    ["Id"] = line.Id,
                    ["Name"] = line.Name,
                    ["Color"] = $"#{line.Color:x6}",
                    ["Type"] = line.Type.ToString(),
                    ["Image"] = line.Image == null ? null : Convert.ToBase64String(line.Image),
                    ["Stops"] = lineStopsObject = new JArray(),
                    ["Routes"] = lineRoutesObject = new JArray(),
                    ["Metadata"] = new JObject(line.Metadata.Select(m => new JProperty(m.Key, m.Value)))
                };

                foreach (Stop stop in line.Stops)
                {
                    JObject stopObject = new JObject
                    {
                        ["Id"] = stop.Id,
                        ["Name"] = stop.Name,
                        ["Position"] = new JArray { stop.Position.Latitude, stop.Position.Longitude }
                    };

                    lineStopsObject.Add(stopObject);
                }

                foreach (Route route in line.Routes)
                {
                    JArray routeStepsObject = new JArray();
                    JObject routeTimeTableObject = new JObject();

                    JObject routeObject = new JObject
                    {
                        ["Id"] = route.Id,
                        ["Name"] = route.Name,
                        ["Steps"] = routeStepsObject,
                        ["TimeTable"] = route.TimeTable == null ? null : routeTimeTableObject,
                    };

                    foreach (Step step in route.Steps)
                    {
                        JObject stepObject = new JObject
                        {
                            ["Stop"] = step.Stop.Id,
                            ["Partial"] = step.Partial,
                            ["Direction"] = step.Direction,
                            ["Duration"] = FormatTimeSpan(step.Duration),
                            ["Trajectory"] = step.Trajectory == null ? null : new JArray(step.Trajectory.Select(p => new JArray { p.Index, new JArray { p.Position.Latitude, p.Position.Longitude } })),
                            ["Speed"] = step.Speed == null ? null : Convert.ToBase64String(step.Speed.Data)
                        };

                        routeStepsObject.Add(stepObject);
                    }

                    if (route.TimeTable != null)
                    {
                        Dictionary<string, TimeSpan?[,]> tables = new Dictionary<string, TimeSpan?[,]>()
                        {
                            { "Week", route.TimeTable.WeekTable },
                            { "Friday", route.TimeTable.FridayTable },
                            { "Saturday", route.TimeTable.SaturdayTable },
                            { "Sunday", route.TimeTable.SundayTable },
                            { "Holidays", route.TimeTable.HolidaysTable },
                        };

                        foreach (var pair in tables)
                        {
                            if (pair.Value == null)
                                continue;

                            JArray tableArray = new JArray();
                            JProperty tableProperty = new JProperty(pair.Key, tableArray);

                            for (int i = 0; i < pair.Value.GetLength(0); i++)
                                tableArray.Add(new JArray(Enumerable.Range(0, pair.Value.GetLength(1)).Select(j => FormatTimeSpan(pair.Value[i, j]))));

                            routeTimeTableObject.Add(tableProperty);
                        }
                    }

                    lineRoutesObject.Add(routeObject);
                }

                // Dump json file
                using (FileStream fileStream = File.Open(Path.Combine(outputDirectory, $"L{line.Number}.json"), FileMode.Create))
                using (StreamWriter streamWriter = new StreamWriter(fileStream))
                using (JsonWriter jsonWriter = new JsonTextWriter(streamWriter))
                    lineObject.WriteTo(jsonWriter);
            }
        }

        private static void TestSearch()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Stop from = Lines.SelectMany(l => l.Stops).First(s => Likes(s.Name, "Saint-Lazare")); // "Saint-Lazare", "Apollo"
            Stop to = Lines.SelectMany(l => l.Stops).First(s => Likes(s.Name, "Odysseum")); // "Pierre de Coubertin", "Lattes Centre", "Odysseum"

            Console.WriteLine("Setup ...");

            RouteSearch routeSearch = new RouteSearch();
            routeSearch.Settings.AllowBusLinks = true;
            routeSearch.Settings.AllowWalkLinks = false;
            routeSearch.Prepare(Lines);

            Console.WriteLine("Setup finished in {0} ms", stopwatch.ElapsedMilliseconds);
            //Console.WriteLine();

            DateTime now = new DateTime(2016, 05, 27, 16, 24, 00);
            DateTime end = DateTime.Now + TimeSpan.FromSeconds(1000);

            List<RouteLink[]> routeLinks = new List<RouteLink[]>();
            List<RouteSegment[]> routeSegments = new List<RouteSegment[]>();

            stopwatch.Reset();

            routeLinks = routeSearch.FindRoutes(from, to).ToList();
            routeLinks.Sort((r1, r2) => (int)(r1.Sum(l => l.Weight) - r2.Sum(l => l.Weight)));

            foreach (RouteLink[] route in routeLinks)
                routeSegments.AddRange(routeSearch.SimulateTimeStepsFrom(route, now, TimeSpan.Zero, TimeSpan.FromMinutes(15)));

            routeSegments.Sort((r1, r2) => (int)(r1.Last().DateTo - r2.Last().DateTo).TotalSeconds);

            stopwatch.Stop();
            Console.WriteLine("Results found in {0} ms", stopwatch.ElapsedMilliseconds);

            foreach (RouteSegment[] route in routeSegments.Take(5))
            {
                Console.WriteLine();

                foreach (RouteSegment segment in route)
                    Console.WriteLine(segment);
            }
            
            Console.ReadKey(true);
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