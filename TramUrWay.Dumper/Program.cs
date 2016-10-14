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
        // http://www.tam-voyages.com/horaires_ligne/index.asp?rub_code=6&actionButton=SearchByLineNumberHiddenField&lign_id=

        private const string baseAddress = "http://www.tam-voyages.com/horaires_ligne/index.asp?rub_code=6";
        private const string outputDirectory = @"..\..\..\Data\Hiver 2016";

        private const string weekDate = "18/10/2016";
        private const string fridayDate = "21/10/2016";
        private const string saturdayDate = "22/10/2016";
        private const string sundayDate = "23/10/2016";

        private static Regex blockRegex = new Regex(@"headers=""commune""([^<]|<[^\/]|<\/[^t]|<\/t[^r])+", RegexOptions.Compiled);
        private static Regex idRegex = new Regex(@"id=""arret([0-9]+)"" class=""arret""", RegexOptions.Compiled);
        private static Regex nameRegex = new Regex(@"title=""[^""]+"">([^<]+)<", RegexOptions.Compiled);
        private static Regex timeRegex = new Regex(@"s=""arret[^""]+"">([^<]+)<", RegexOptions.Compiled);

        public static void Main(string[] args)
        {
            DumpData();
            PatchData();
        }

        private static void DumpData()
        {
            WebClient webClient = new WebClient();
            webClient.Encoding = Encoding.UTF8;

            Dictionary<string, string> dumpTasks = new Dictionary<string, string>()
            {
                #region Ligne 1

                { @"L1\L1.R0.lun-jeu", $"&ladate={weekDate}&lign_id=1&sens=2" },
                { @"L1\L1.R0.ven",   $"&ladate={fridayDate}&lign_id=1&sens=2" },
                { @"L1\L1.R0.sam", $"&ladate={saturdayDate}&lign_id=1&sens=2" },
                { @"L1\L1.R0.dim",   $"&ladate={sundayDate}&lign_id=1&sens=2" },
                { @"L1\L1.R1.lun-jeu", $"&ladate={weekDate}&lign_id=1&sens=1" },
                { @"L1\L1.R1.ven",   $"&ladate={fridayDate}&lign_id=1&sens=1" },
                { @"L1\L1.R1.sam", $"&ladate={saturdayDate}&lign_id=1&sens=1" },
                { @"L1\L1.R1.dim",   $"&ladate={sundayDate}&lign_id=1&sens=1" },

                #endregion
                #region Ligne 2

                { @"L2\L2.R0.lun-jeu", $"&ladate={weekDate}&lign_id=12&sens=1" },
                { @"L2\L2.R0.ven",   $"&ladate={fridayDate}&lign_id=12&sens=1" },
                { @"L2\L2.R0.sam", $"&ladate={saturdayDate}&lign_id=12&sens=1" },
                { @"L2\L2.R0.dim",   $"&ladate={sundayDate}&lign_id=12&sens=1" },
                { @"L2\L2.R1.lun-jeu", $"&ladate={weekDate}&lign_id=12&sens=2" },
                { @"L2\L2.R1.ven",   $"&ladate={fridayDate}&lign_id=12&sens=2" },
                { @"L2\L2.R1.sam", $"&ladate={saturdayDate}&lign_id=12&sens=2" },
                { @"L2\L2.R1.dim",   $"&ladate={sundayDate}&lign_id=12&sens=2" },

                #endregion
                #region Ligne 3

                { @"L3\L3.XXX.lun-jeu", $"&ladate={weekDate}&lign_id=22&sens=1" },
                { @"L3\L3.XXX.ven",   $"&ladate={fridayDate}&lign_id=22&sens=1" },
                { @"L3\L3.XXX.sam", $"&ladate={saturdayDate}&lign_id=22&sens=1" },
                { @"L3\L3.XXX.dim",   $"&ladate={sundayDate}&lign_id=22&sens=1" },
                { @"L3\L3.YYY.lun-jeu", $"&ladate={weekDate}&lign_id=22&sens=2" },
                { @"L3\L3.YYY.ven",   $"&ladate={fridayDate}&lign_id=22&sens=2" },
                { @"L3\L3.YYY.sam", $"&ladate={saturdayDate}&lign_id=22&sens=2" },
                { @"L3\L3.YYY.dim",   $"&ladate={sundayDate}&lign_id=22&sens=2" },

                #endregion
                #region Ligne 4

                { @"L4\L4.R0.lun-jeu", $"&ladate={weekDate}&lign_id=33&sens=1" },
                { @"L4\L4.R0.ven",   $"&ladate={fridayDate}&lign_id=33&sens=1" },
                { @"L4\L4.R0.sam", $"&ladate={saturdayDate}&lign_id=33&sens=1" },
                { @"L4\L4.R0.dim",   $"&ladate={sundayDate}&lign_id=33&sens=1" },
                { @"L4\L4.R1.lun-jeu", $"&ladate={weekDate}&lign_id=33&sens=2" },
                { @"L4\L4.R1.ven",   $"&ladate={fridayDate}&lign_id=33&sens=2" },
                { @"L4\L4.R1.sam", $"&ladate={saturdayDate}&lign_id=33&sens=2" },
                { @"L4\L4.R1.dim",   $"&ladate={sundayDate}&lign_id=33&sens=2" },

                #endregion

                #region Ligne 6

                { @"L6\L6.R0.lun-jeu", $"&ladate={weekDate}&lign_id=35&sens=1" },
                { @"L6\L6.R0.ven",   $"&ladate={fridayDate}&lign_id=35&sens=1" },
                { @"L6\L6.R0.sam", $"&ladate={saturdayDate}&lign_id=35&sens=1" },
                { @"L6\L6.R0.dim",   $"&ladate={sundayDate}&lign_id=35&sens=1" },
                { @"L6\L6.R1.lun-jeu", $"&ladate={weekDate}&lign_id=35&sens=2" },
                { @"L6\L6.R1.ven",   $"&ladate={fridayDate}&lign_id=35&sens=2" },
                { @"L6\L6.R1.sam", $"&ladate={saturdayDate}&lign_id=35&sens=2" },
                { @"L6\L6.R1.dim",   $"&ladate={sundayDate}&lign_id=35&sens=2" },

                #endregion
                #region Ligne 7

                { @"L7\L7.R0.lun-jeu", $"&ladate={weekDate}&lign_id=36&sens=1" },
                { @"L7\L7.R0.ven",   $"&ladate={fridayDate}&lign_id=36&sens=1" },
                { @"L7\L7.R0.sam", $"&ladate={saturdayDate}&lign_id=36&sens=1" },
                { @"L7\L7.R0.dim",   $"&ladate={sundayDate}&lign_id=36&sens=1" },
                { @"L7\L7.R1.lun-jeu", $"&ladate={weekDate}&lign_id=36&sens=2" },
                { @"L7\L7.R1.ven",   $"&ladate={fridayDate}&lign_id=36&sens=2" },
                { @"L7\L7.R1.sam", $"&ladate={saturdayDate}&lign_id=36&sens=2" },
                { @"L7\L7.R1.dim",   $"&ladate={sundayDate}&lign_id=36&sens=2" },

                #endregion
                #region Ligne 8

                { @"L8\L8.R0.lun-jeu", $"&ladate={weekDate}&lign_id=37&sens=2" },
                { @"L8\L8.R0.ven",   $"&ladate={fridayDate}&lign_id=37&sens=2" },
                { @"L8\L8.R0.sam", $"&ladate={saturdayDate}&lign_id=37&sens=2" },
                { @"L8\L8.R0.dim",   $"&ladate={sundayDate}&lign_id=37&sens=2" },
                { @"L8\L8.R1.lun-jeu", $"&ladate={weekDate}&lign_id=37&sens=1" },
                { @"L8\L8.R1.ven",   $"&ladate={fridayDate}&lign_id=37&sens=1" },
                { @"L8\L8.R1.sam", $"&ladate={saturdayDate}&lign_id=37&sens=1" },
                { @"L8\L8.R1.dim",   $"&ladate={sundayDate}&lign_id=37&sens=1" },

                #endregion
                #region Ligne 9

                { @"L9\L9.R0.lun-jeu", $"&ladate={weekDate}&lign_id=38&sens=2" },
                { @"L9\L9.R0.ven",   $"&ladate={fridayDate}&lign_id=38&sens=2" },
                { @"L9\L9.R0.sam", $"&ladate={saturdayDate}&lign_id=38&sens=2" },
                { @"L9\L9.R0.dim",   $"&ladate={sundayDate}&lign_id=38&sens=2" },
                { @"L9\L9.R1.lun-jeu", $"&ladate={weekDate}&lign_id=38&sens=1" },
                { @"L9\L9.R1.ven",   $"&ladate={fridayDate}&lign_id=38&sens=1" },
                { @"L9\L9.R1.sam", $"&ladate={saturdayDate}&lign_id=38&sens=1" },
                { @"L9\L9.R1.dim",   $"&ladate={sundayDate}&lign_id=38&sens=1" },

                #endregion
                #region Ligne 10

                { @"L10\L10.R0.lun-jeu", $"&ladate={weekDate}&lign_id=2&sens=2" },
                { @"L10\L10.R0.ven",   $"&ladate={fridayDate}&lign_id=2&sens=2" },
                { @"L10\L10.R0.sam", $"&ladate={saturdayDate}&lign_id=2&sens=2" },
                { @"L10\L10.R0.dim",   $"&ladate={sundayDate}&lign_id=2&sens=2" },
                { @"L10\L10.R1.lun-jeu", $"&ladate={weekDate}&lign_id=2&sens=1" },
                { @"L10\L10.R1.ven",   $"&ladate={fridayDate}&lign_id=2&sens=1" },
                { @"L10\L10.R1.sam", $"&ladate={saturdayDate}&lign_id=2&sens=1" },
                { @"L10\L10.R1.dim",   $"&ladate={sundayDate}&lign_id=2&sens=1" },

                #endregion
                #region Ligne 11

                { @"L11\L11.R0.lun-jeu", $"&ladate={weekDate}&lign_id=3&sens=1" },
                { @"L11\L11.R0.ven",   $"&ladate={fridayDate}&lign_id=3&sens=1" },
                { @"L11\L11.R0.sam", $"&ladate={saturdayDate}&lign_id=3&sens=1" },
                { @"L11\L11.R0.dim",   $"&ladate={sundayDate}&lign_id=3&sens=1" },
                { @"L11\L11.R1.lun-jeu", $"&ladate={weekDate}&lign_id=3&sens=2" },
                { @"L11\L11.R1.ven",   $"&ladate={fridayDate}&lign_id=3&sens=2" },
                { @"L11\L11.R1.sam", $"&ladate={saturdayDate}&lign_id=3&sens=2" },
                { @"L11\L11.R1.dim",   $"&ladate={sundayDate}&lign_id=3&sens=2" },

                #endregion
                #region Ligne 12

                { @"L12\L12.R0.lun-jeu", $"&ladate={weekDate}&lign_id=4&sens=2" },
                { @"L12\L12.R0.ven",   $"&ladate={fridayDate}&lign_id=4&sens=2" },
                { @"L12\L12.R0.sam", $"&ladate={saturdayDate}&lign_id=4&sens=2" },
                { @"L12\L12.R0.dim",   $"&ladate={sundayDate}&lign_id=4&sens=2" },
                { @"L12\L12.R1.lun-jeu", $"&ladate={weekDate}&lign_id=4&sens=1" },
                { @"L12\L12.R1.ven",   $"&ladate={fridayDate}&lign_id=4&sens=1" },
                { @"L12\L12.R1.sam", $"&ladate={saturdayDate}&lign_id=4&sens=1" },
                { @"L12\L12.R1.dim",   $"&ladate={sundayDate}&lign_id=4&sens=1" },

                #endregion
                #region Ligne 13 (La navette)

                { @"L13\L13.R0.lun-jeu", $"&ladate={weekDate}&lign_id=5&sens=1" },
                { @"L13\L13.R0.ven",   $"&ladate={fridayDate}&lign_id=5&sens=1" },
                { @"L13\L13.R0.sam", $"&ladate={saturdayDate}&lign_id=5&sens=1" },
                { @"L13\L13.R0.dim",   $"&ladate={sundayDate}&lign_id=5&sens=1" },
                { @"L13\L13.R1.lun-jeu", $"&ladate={weekDate}&lign_id=5&sens=2" },
                { @"L13\L13.R1.ven",   $"&ladate={fridayDate}&lign_id=5&sens=2" },
                { @"L13\L13.R1.sam", $"&ladate={saturdayDate}&lign_id=5&sens=2" },
                { @"L13\L13.R1.dim",   $"&ladate={sundayDate}&lign_id=5&sens=2" },

                #endregion
                #region Ligne 14

                { @"L14\L14.R0.lun-jeu", $"&ladate={weekDate}&lign_id=6&sens=1" },
                { @"L14\L14.R0.ven",   $"&ladate={fridayDate}&lign_id=6&sens=1" },
                { @"L14\L14.R0.sam", $"&ladate={saturdayDate}&lign_id=6&sens=1" },
                { @"L14\L14.R0.dim",   $"&ladate={sundayDate}&lign_id=6&sens=1" },
                { @"L14\L14.R1.lun-jeu", $"&ladate={weekDate}&lign_id=6&sens=2" },
                { @"L14\L14.R1.ven",   $"&ladate={fridayDate}&lign_id=6&sens=2" },
                { @"L14\L14.R1.sam", $"&ladate={saturdayDate}&lign_id=6&sens=2" },
                { @"L14\L14.R1.dim",   $"&ladate={sundayDate}&lign_id=6&sens=2" },

                #endregion
                #region Ligne 15 (La Ronde)

                { @"L15\L15.R0.lun-jeu", $"&ladate={weekDate}&lign_id=7&sens=2" },
                { @"L15\L15.R0.ven",   $"&ladate={fridayDate}&lign_id=7&sens=2" },
                { @"L15\L15.R0.sam", $"&ladate={saturdayDate}&lign_id=7&sens=2" },
                { @"L15\L15.R0.dim",   $"&ladate={sundayDate}&lign_id=7&sens=2" },
                { @"L15\L15.R1.lun-jeu", $"&ladate={weekDate}&lign_id=7&sens=1" },
                { @"L15\L15.R1.ven",   $"&ladate={fridayDate}&lign_id=7&sens=1" },
                { @"L15\L15.R1.sam", $"&ladate={saturdayDate}&lign_id=7&sens=1" },
                { @"L15\L15.R1.dim",   $"&ladate={sundayDate}&lign_id=7&sens=1" },

                #endregion
                #region Ligne 17

                { @"L17\L17.R0.lun-jeu", $"&ladate={weekDate}&lign_id=9&sens=2" },
                { @"L17\L17.R0.ven",   $"&ladate={fridayDate}&lign_id=9&sens=2" },
                { @"L17\L17.R0.sam", $"&ladate={saturdayDate}&lign_id=9&sens=2" },
                { @"L17\L17.R0.dim",   $"&ladate={sundayDate}&lign_id=9&sens=2" },
                { @"L17\L17.R1.lun-jeu", $"&ladate={weekDate}&lign_id=9&sens=1" },
                { @"L17\L17.R1.ven",   $"&ladate={fridayDate}&lign_id=9&sens=1" },
                { @"L17\L17.R1.sam", $"&ladate={saturdayDate}&lign_id=9&sens=1" },
                { @"L17\L17.R1.dim",   $"&ladate={sundayDate}&lign_id=9&sens=1" },

                #endregion
                #region Ligne 19

                { @"L19\L19.R0.lun-jeu", $"&ladate={weekDate}&lign_id=11&sens=1" },
                { @"L19\L19.R0.ven",   $"&ladate={fridayDate}&lign_id=11&sens=1" },
                { @"L19\L19.R0.sam", $"&ladate={saturdayDate}&lign_id=11&sens=1" },
                { @"L19\L19.R0.dim",   $"&ladate={sundayDate}&lign_id=11&sens=1" },
                { @"L19\L19.R1.lun-jeu", $"&ladate={weekDate}&lign_id=11&sens=2" },
                { @"L19\L19.R1.ven",   $"&ladate={fridayDate}&lign_id=11&sens=2" },
                { @"L19\L19.R1.sam", $"&ladate={saturdayDate}&lign_id=11&sens=2" },
                { @"L19\L19.R1.dim",   $"&ladate={sundayDate}&lign_id=11&sens=2" },

                #endregion
           };

            foreach (var dump in dumpTasks)
            {
                string outputFile = Path.Combine(outputDirectory, dump.Key + ".csv");

                if (File.Exists(outputFile))
                    continue;

                Dictionary<string, List<TimeSpan?>> stopTimes = new Dictionary<string, List<TimeSpan?>>();
                Dictionary<string, string> stopNames = new Dictionary<string, string>();
                bool addIndex = true;

                for (int index = 1; ; index += 8)
                {
                    Console.WriteLine($"Dumping {dump.Key} #{index}");
                    string content = null;

                    while (true)
                    {
                        string url = baseAddress + dump.Value;
                        if (addIndex)
                            url += "&index=" + index;

                        content = webClient.DownloadString(url);

                        if (content.Contains("pas d'horaire"))
                        {
                            if (addIndex)
                                break;

                            addIndex = false;
                        }
                        else
                            break;
                    }

                    foreach (Match blockMatch in blockRegex.Matches(content))
                    {
                        string block = blockMatch.Value;

                        Match idMatch = idRegex.Match(block);
                        string id = idMatch.Groups[1].Value;

                        Match nameMatch = nameRegex.Match(block);
                        string name = nameMatch.Groups[1].Value;

                        List<TimeSpan?> times;
                        if (!stopTimes.TryGetValue(id, out times))
                            stopTimes.Add(id, times = new List<TimeSpan?>());

                        stopNames[id] = name;

                        foreach (Match timeBlock in timeRegex.Matches(block))
                        {
                            string time = timeBlock.Groups[1].Value;

                            if (time == "|")
                                times.Add(null);
                            else
                                times.Add(TimeSpan.Parse(time));
                        }
                    }

                    // This button indicates that there are more pages
                    if (!content.Contains("laterHour"))
                        break;
                }

                // Prepare output
                List<string> lines = new List<string>();
                lines.Add(string.Join(";", stopNames.Values));

                if (stopTimes.Count > 0)
                {
                    int count = stopTimes.First().Value.Count;
                    for (int i = 0; i < count; i++)
                        lines.Add(string.Join(";", stopTimes.Select(t => t.Value[i] == null ? "" : $"{t.Value[i]?.Hours}:{t.Value[i]?.Minutes:d2}")));
                }

                // Write deduplicated lines
                using (StreamWriter output = new StreamWriter(outputFile, false, Encoding.UTF8))
                    foreach (string line in lines.Distinct())
                        output.WriteLine(line);
            }
        }
        private static void PatchData()
        {
            // Demux line 3 routes
            foreach (string variation in new[] { "lun-jeu", "ven", "sam", "dim" })
            {
                string inputXFile = Path.Combine(outputDirectory, @"L3\L3.XXX." + variation + ".csv");
                string inputYFile = Path.Combine(outputDirectory, @"L3\L3.XXX." + variation + ".csv");

                string routeFile = Path.Combine(outputDirectory, @"L3\L3.R{0}." + variation + ".csv");

                string[] linesX = File.ReadAllLines(inputXFile, Encoding.UTF8);
                string[][] valuesX = linesX.Select(l => l.Split(';')).ToArray();

                File.WriteAllLines(string.Format(routeFile, "0"), valuesX.Select(l => l.Take(4).Concat(l.Skip(6)).Join(";")), Encoding.UTF8);
                File.WriteAllLines(string.Format(routeFile, "1"), valuesX.Select(l => l.Skip(4).Join(";")), Encoding.UTF8);

                string[] linesY = File.ReadAllLines(inputXFile, Encoding.UTF8);
                string[][] valuesY = linesY.Select(l => l.Split(';')).ToArray();

                File.WriteAllLines(string.Format(routeFile, "2"), valuesY.Select(l => l.Take(4).Concat(l.Skip(6)).Join(";")), Encoding.UTF8);
                File.WriteAllLines(string.Format(routeFile, "3"), valuesY.Select(l => l.Skip(4).Join(";")), Encoding.UTF8);
            }
        }
    }
}