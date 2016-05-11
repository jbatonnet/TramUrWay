using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TramUrWay.Android
{
    public class Assets
    {

        private static Regex lineFileRegex = new Regex("^L([0-9]+).json$", RegexOptions.Compiled);

        private Context context;
        private Dictionary<Route, TimeTable> timeTables = new Dictionary<Route, TimeTable>();

        public Assets(Context context)
        {
            this.context = context;
        }

        public Line[] LoadLines()
        {
            string[] files = context.Assets.List("");
            ConcurrentBag<Line> lines = new ConcurrentBag<Line>();

            // Load lines
            Parallel.ForEach(files, file =>
            {
                Match lineFileMatch = lineFileRegex.Match(file);
                if (!lineFileMatch.Success)
                    return;

                int lineId = int.Parse(lineFileMatch.Groups[1].Value);
                using (Stream stream = context.Assets.Open(file))
                    lines.Add(LoadLine(lineId, stream));
            });

            return lines.OrderBy(l => l.Id).ToArray();
        }
        private Line LoadLine(int id, Stream stream)
        {
            string content;
            using (StreamReader streamReader = new StreamReader(stream))
                content = streamReader.ReadToEnd();

            JObject lineObject = JsonConvert.DeserializeObject(content) as JObject;

            Line line = new Line()
            {
                Id = lineObject["Id"].Value<int>(),
                Name = lineObject["Name"].Value<string>(),
                Color = Convert.ToInt32(lineObject["Color"].Value<string>().Replace("#", ""), 16),
                Type = (LineType)Enum.Parse(typeof(LineType), lineObject["Type"].Value<string>()),
                Image = Convert.FromBase64String(lineObject["Image"].Value<string>())
            };

            List<Stop> lineStops = new List<Stop>();

            foreach (JObject stopObject in lineObject["Stops"] as JArray)
            {
                Stop stop = new Stop()
                {
                    Id = stopObject["Id"].Value<int>(),
                    Name = stopObject["Name"].Value<string>(),
                    Position = new Position(stopObject["Position"][0].Value<float>(), stopObject["Position"][1].Value<float>()),
                    Line = line
                };

                lineStops.Add(stop);
            }

            line.Stops = lineStops.ToArray();

            List<Route> lineRoutes = new List<Route>();

            foreach (JObject routeObject in lineObject["Routes"] as JArray)
            {
                Route route = new Route()
                {
                    Id = routeObject["Id"].Value<int>(),
                    Line = line
                };

                List<Step> routeSteps = new List<Step>();

                foreach (JObject stepObject in routeObject["Steps"] as JArray)
                {
                    int stopId = stepObject["Stop"].Value<int>();

                    Step step = new Step()
                    {
                        Stop = line.Stops.First(s => s.Id == stopId),
                        Partial = stepObject["Partial"].Value<bool>(),
                        Duration = ParseTimeSpan(stepObject["Duration"].Value<string>()),
                        Direction = stepObject["Direction"].Value<string>(),
                        Speed = ParseCurve(stepObject["Speed"].Value<string>()),
                        Route = route
                    };

                    JArray trajectoryArray = stepObject["Trajectory"] as JArray;
                    if (trajectoryArray != null)
                    {
                        List<TrajectoryStep> stepTrajectory = new List<TrajectoryStep>();

                        foreach (JToken positionObject in stepObject["Trajectory"] as JArray)
                            stepTrajectory.Add(new TrajectoryStep() { Index = positionObject[0].Value<float>(), Position = new Position(positionObject[1][0].Value<float>(), positionObject[1][1].Value<float>()) });

                        step.Trajectory = stepTrajectory.ToArray();
                    }

                    routeSteps.Add(step);
                }

                route.Steps = routeSteps.ToArray();

                JObject timeTableObject = routeObject["TimeTable"] as JObject;
                if (timeTableObject != null)
                {
                    string[] tableNames = new[] { "Week", "Friday", "Saturday", "Sunday", "Holiday" };
                    TimeTable timeTable = new TimeTable() { Route = route };

                    foreach (string tableName in tableNames)
                    {
                        JArray tableArray = timeTableObject[tableName] as JArray;
                        if (tableArray == null)
                            continue;

                        List<TimeSpan?[]> tableValues = new List<TimeSpan?[]>();
                        foreach (JArray tableLineArray in tableArray.Cast<JArray>())
                            tableValues.Add(tableLineArray.Select(j => ParseTimeSpan(j.Value<string>())).ToArray());

                        TimeSpan?[,] table = new TimeSpan?[tableValues.Count, route.Steps.Length];
                        for (int i = 0; i < tableValues.Count; i++)
                            for (int j = 0; j < route.Steps.Length; j++)
                                table[i, j] = tableValues[i][j];

                        switch (tableName)
                        {
                            case "Week": timeTable.WeekTable = table; break;
                            case "Friday": timeTable.FridayTable = table; break;
                            case "Saturday": timeTable.SaturdayTable = table; break;
                            case "Sunday": timeTable.SundayTable = table; break;
                            case "Holidays": timeTable.HolidaysTable = table; break;
                        }
                    }

                    route.TimeTable = timeTable;
                }

                // Post process each step to build the linked list
                for (int i = 0; i < route.Steps.Length; i++)
                {
                    route.Steps[i].Previous = i > 0 ? route.Steps[i - 1] : null;
                    route.Steps[i].Next = i < route.Steps.Length - 1 ? route.Steps[i + 1] : null;
                }
                
                lineRoutes.Add(route);
            }
            
            line.Routes = lineRoutes.ToArray();

            return line;
        }

        private TimeSpan? ParseTimeSpan(string value)
        {
            if (value == null)
                return null;

            return TimeSpan.Parse(value);
        }
        private Curve ParseCurve(string value)
        {
            if (value == null)
                return null;

            return new Curve(Convert.FromBase64String(value));
        }
    }

    public static class AssetsExtensions
    {
        public static Drawable GetIconDrawable(this Line me, Context context)
        {
            return new BitmapDrawable(context.Resources, BitmapFactory.DecodeByteArray(me.Image, 0, me.Image.Length));
        }
    }
}