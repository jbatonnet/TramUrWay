using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TramUrWay.Dumper
{
    public class Program
    {
        private const string baseAddress = "http://www.tam-voyages.com/horaires_ligne/index.asp?rub_code=6";
        private const string outputDirectory = @"..\..\..\Data\Hiver 2015";

        private static Regex blockRegex = new Regex(@"headers=""commune""([^<]|<[^\/]|<\/[^t]|<\/t[^r])+", RegexOptions.Compiled);
        private static Regex nameRegex = new Regex(@"title=""[^""]+"">([^<]+)<", RegexOptions.Compiled);
        private static Regex timeRegex = new Regex(@"s=""arret[^""]+"">([^<]+)<", RegexOptions.Compiled);

        public static void Main(string[] args)
        {
            WebClient webClient = new WebClient();
            webClient.Encoding = Encoding.UTF8;

            Dictionary<string, string> dumpTasks = new Dictionary<string, string>()
            {
                { "L1.R0.lun-jeu", "&ladate=03/05/2016&lign_id=1&sens=2" },
                { "L1.R0.ven", "&ladate=15/04/2016&lign_id=1&sens=2" },
                { "L1.R0.sam", "&ladate=16/04/2016&lign_id=1&sens=2" },
                { "L1.R0.dim", "&ladate=17/04/2016&lign_id=1&sens=2" },
                { "L1.R1.lun-jeu", "&ladate=03/05/2016&lign_id=1&sens=1" },
                { "L1.R1.ven", "&ladate=15/04/2016&lign_id=1&sens=1" },
                { "L1.R1.sam", "&ladate=16/04/2016&lign_id=1&sens=1" },
                { "L1.R1.dim", "&ladate=17/04/2016&lign_id=1&sens=1" },

                { "L2.R0.lun-jeu", "&ladate=03/05/2016&lign_id=12&sens=1" },
                { "L2.R0.ven", "&ladate=15/04/2016&lign_id=12&sens=1" },
                { "L2.R0.sam", "&ladate=16/04/2016&lign_id=12&sens=1" },
                { "L2.R0.dim", "&ladate=17/04/2016&lign_id=12&sens=1" },
                { "L2.R1.lun-jeu", "&ladate=03/05/2016&lign_id=12&sens=2" },
                { "L2.R1.ven", "&ladate=15/04/2016&lign_id=12&sens=2" },
                { "L2.R1.sam", "&ladate=16/04/2016&lign_id=12&sens=2" },
                { "L2.R1.dim", "&ladate=17/04/2016&lign_id=12&sens=2" },
            };

            foreach (var dump in dumpTasks)
            {
                string outputFile = Path.Combine(outputDirectory, dump.Key + ".csv");

                if (File.Exists(outputFile))
                    continue;

                Dictionary<string, List<TimeSpan?>> stopTimes = new Dictionary<string, List<TimeSpan?>>();

                for (int index = 1; ; index += 8)
                {
                    Console.WriteLine($"Dumping {dump.Key} #{index}");
                    string content = webClient.DownloadString(baseAddress + dump.Value + "&index=" + index);

                    foreach (Match blockMatch in blockRegex.Matches(content))
                    {
                        string block = blockMatch.Value;

                        Match nameMatch = nameRegex.Match(block);
                        string name = nameMatch.Groups[1].Value;

                        List<TimeSpan?> times;
                        if (!stopTimes.TryGetValue(name, out times))
                            stopTimes.Add(name, times = new List<TimeSpan?>());

                        foreach (Match timeBlock in timeRegex.Matches(block))
                        {
                            string time = timeBlock.Groups[1].Value;

                            if (time == "|")
                                times.Add(null);
                            else
                                times.Add(TimeSpan.Parse(time));
                        }
                    }

                    if (!content.Contains("laterHour"))
                        break;
                }

                // Prepare output
                List<string> lines = new List<string>();
                lines.Add(string.Join(";", stopTimes.Keys));

                int count = stopTimes.First().Value.Count;
                for (int i = 0; i < count; i++)
                    lines.Add(string.Join(";", stopTimes.Select(t => t.Value[i] == null ? "" : $"{t.Value[i]?.Hours}:{t.Value[i]?.Minutes:d2}")));

                // Write deduplicated lines
                using (StreamWriter output = new StreamWriter(outputFile, false, Encoding.UTF8))
                    foreach (string line in lines.Distinct())
                        output.WriteLine(line);
            }
        }
    }
}