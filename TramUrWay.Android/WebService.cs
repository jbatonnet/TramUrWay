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
    public class WebService
    {
        private class CacheEntry
        {
            public DateTime Date { get; set; }
            public TimeStep[] Steps { get; set; }
        }
        
        private const string webServiceUrl = "https://apimobile.tam-voyages.com";

        private Dictionary<Line, CacheEntry> cache = new Dictionary<Line, CacheEntry>();

        public IEnumerable<TimeStep> GetLiveTimeSteps(Line line)
        {
            CacheEntry cacheEntry;

            if (!cache.TryGetValue(line, out cacheEntry) || (DateTime.Now - cacheEntry.Date).TotalSeconds > TramUrWayApplication.MinimumServiceDelay)
            {
                string url = webServiceUrl + "/api/v1/hours/next/line";

                // Build request
                JObject query = new JObject()
                {
                    ["directions"] = new JArray(line.Routes.SelectMany(r => r.Steps).Select(s => s.Stop.Id).ToArray()),
                    ["citywayLineId"] = line.Id,
                    ["lineNumber"] = line.Number,
                    ["stops"] = new JArray(line.Routes.SelectMany(r => r.Steps).Select(s => s.Stop.Id).ToArray()),
                    ["urbanLine"] = line.Metadata?["Urban"] as bool? ?? false ? 1 : 0
                };

                // Send the query
                Log.Info("Downloading live timesteps from service for line {0}", line.Id);
                string data = new WebClient().UploadString(url, query.ToString());

                // Parse results
                JArray results = JsonConvert.DeserializeObject(data) as JArray;
                List<TimeStep> timeSteps = new List<TimeStep>();

                foreach (JObject resultData in results)
                {
                    int stopId = resultData["cityway_stop_id"].Value<int>();
                    int directionId = resultData["line_direction"].Value<int>();
                    Step step = line.Routes.SelectMany(r => r.Steps).FirstOrDefault(s => s.Stop.Id == stopId);
                    Step direction = step.Route.Steps.FirstOrDefault(s => s.Stop.Id == directionId);

                    JArray timeStepsData = resultData["stop_next_time"].Value<JArray>();
                    foreach (JObject timeStepData in timeStepsData)
                    {
                        int hour = timeStepData["passing_hour"].Value<int>();
                        int minute = timeStepData["passing_minute"].Value<int>();

                        DateTime date = DateTime.Now.Date.AddHours(hour).AddMinutes(minute);

                        timeSteps.Add(new TimeStep() { Step = step, Date = date, Source = TimeStepSource.Online, Destination = direction });
                    }
                }

                cache[line] = cacheEntry = new CacheEntry() { Date = DateTime.Now, Steps = timeSteps.ToArray() };
            }

            return cacheEntry.Steps;
        }
    }
}