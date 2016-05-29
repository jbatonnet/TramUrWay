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
        // http://www.tam-direct.com (Down)
        // http://e-tam.fr (Down)
        // http://tam.mobitrans.fr
        // http://37.59.49.161

        private const string webServiceOldUrl = "http://tam.mobitrans.fr/webservice";
        private const string webServiceNewUrl = "https://apimobile.tam-voyages.com";

        public IEnumerable<TimeStep> GetLiveTimeSteps()
        {
            //string content;
            //using (StreamReader reader = new StreamReader(context.Assets.Open("getDetails.json")))
            //    content = reader.ReadToEnd();

            const string url = webServiceOldUrl + "/data.php?pattern=getDetails";
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

                Line line = App.GetLine(lineId);
                if (line == null)
                    continue;

                Step step = line.Routes.SelectMany(r => r.Steps).FirstOrDefault(s => s.Stop.Id == stopId);
                if (step == null)
                {
                    Log.Warning("Could not find any stop with id {0} on line {1}", stopId, lineId);
                    continue;
                }

                Step destination = step.Route.Steps.FirstOrDefault(s => Utils.Likes(s.Stop.Name, end));
                if (destination == null)
                {
                    Log.Debug("Could not find any stop with name \"{0}\" on line {1}, building a fake one", end, lineId);
                    destination = new Step() { Stop = new Stop() { Name = end }, Direction = step.Direction, Route = step.Route };
                }

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

                Line line = App.GetLine(lineId);
                if (line == null)
                    continue;

                Step step = line.Routes.SelectMany(r => r.Steps).FirstOrDefault(s => s.Stop.Id == stopId);
                if (step == null)
                {
                    Log.Warning("Could not find any stop with id {0} on line {1}", stopId, lineId);
                    continue;
                }

                Step destination = step.Route.Steps.FirstOrDefault(s => Utils.Likes(s.Stop.Name, end));
                if (destination == null)
                {
                    Log.Debug("Could not find any stop with name \"{0}\" on line {1}, building a fake one", end, lineId);
                    destination = new Step() { Stop = new Stop() { Name = end }, Direction = step.Direction, Route = step.Route };
                }

                if (time < lastTime)
                    time = time.Add(TimeSpan.FromDays(1));
                else
                    lastTime = time;

                yield return new TimeStep() { Step = step, Date = referenceDate.Add(time), Source = theorical ? TimeStepSource.Theorical : TimeStepSource.Online, Destination = destination };
            }
        }

        public IEnumerable<TimeSpan> GetLiveTimeSteps(Line line)
        {
            string url = webServiceNewUrl + "/api/v1/hours/next/line";

            // Build request
            JObject query = new JObject()
            {
                ["directions"] = new JArray() { },
                ["citywayLineId"] = line.Id,
                ["lineNumber"] = line.Id,
                ["sens"] = 1,
                ["stops"] = new JArray() { },
                ["urbanLine"] = 1
            };

            // Send the query
            string data = new WebClient().UploadString(url, query.ToString());

            // Parse results
            JObject result = JsonConvert.DeserializeObject(data) as JObject;

            yield break;
        }
    }
}