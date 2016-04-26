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
    public class Database
    {
        private const string webServiceUrl = "http://www.tam-direct.com/webservice";

        private DbConnection connection;

        public Database(DbConnection connection)
        {
            this.connection = connection;
            
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

        public Step FindStepByWidgetId(int widgetId)
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

                    Line line = App.GetLine(lineId);
                    Route route = line.Routes.FirstOrDefault(r => r.Id == routeId);
                    Step step = route.Steps.FirstOrDefault(s => s.Stop.Id == stopId);

                    return step;
                }
            }
        }
        public IEnumerable<int> GetAllStepWidgets()
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
        public void RegisterStepWidget(int widgetId, Step step)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"INSERT INTO widgets (id, line_id, route_id, stop_id) VALUES ({widgetId}, {step.Route.Line.Id}, {step.Route.Id}, {step.Stop.Id})";
                command.ExecuteNonQuery();
            }
        }

        public void AddFavoriteLine(Line line)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"INSERT OR IGNORE INTO favorite_lines (id) VALUES ({line.Id})";
                command.ExecuteNonQuery();
            }
        }
        public void RemoveFavoriteLine(Line line)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"DELETE FROM favorite_lines WHERE id = {line.Id}";
                command.ExecuteNonQuery();
            }
        }
        public IEnumerable<Line> GetFavoriteLines()
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id FROM favorite_lines";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        yield return App.GetLine(reader.GetInt32(0));
                }
            }
        }
        public void AddFavoriteStop(Stop stop)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"INSERT OR IGNORE INTO favorite_stops (id) VALUES ({stop.Id})";
                command.ExecuteNonQuery();
            }
        }
        public void RemoveFavoriteStop(Stop stop)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"DELETE FROM favorite_stops WHERE id = {stop.Id}";
                command.ExecuteNonQuery();
            }
        }
        public IEnumerable<Stop> GetFavoriteStops()
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id FROM favorite_stops";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                        yield return App.GetStop(reader.GetInt32(0));
                }
            }
        }
    }

    public static class DatabaseExtensions
    {
        private static Dictionary<Line, bool> favoriteLines = null;
        private static Dictionary<Stop, bool> favoriteStops = null;

        public static bool GetIsFavorite(this Line me)
        {
            if (favoriteLines == null)
            {
                Line[] lines = App.Database.GetFavoriteLines().ToArray();
                favoriteLines = App.Lines.ToDictionary(l => l, l => lines.Contains(l));
            }

            return favoriteLines[me];
        }
        public static void SetIsFavorite(this Line me, bool value)
        {
            if (favoriteLines.ContainsKey(me) && favoriteLines[me] == value)
                return;

            favoriteLines[me] = value;

            if (value)
                App.Database.AddFavoriteLine(me);
            else
                App.Database.RemoveFavoriteLine(me);
        }

        public static bool GetIsFavorite(this Stop me)
        {
            if (favoriteStops == null)
            {
                Stop[] stops = App.Database.GetFavoriteStops().ToArray();
                favoriteStops = App.Lines.SelectMany(l => l.Stops).ToDictionary(s => s, s => stops.Contains(s));
            }

            return favoriteStops[me];
        }
        public static void SetIsFavorite(this Stop me, bool value)
        {
            if (favoriteStops.ContainsKey(me) && favoriteStops[me] == value)
                return;

            favoriteStops[me] = value;

            if (value)
                App.Database.AddFavoriteStop(me);
            else
                App.Database.RemoveFavoriteStop(me);
        }
    }
}