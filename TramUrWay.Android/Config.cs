using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Net;

using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Utilities;
using Android.Views;
using Android.Widget;
using Android.Appwidget;
using Android.Preferences;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TramUrWay.Android
{
    public class Config : BaseConfig
    {
        [DisplayName("Mode hors-ligne")]
        [Description("Force l'utilisation des données hors-ligne dans l'application")]
        public bool OfflineMode
        {
            get
            {
                return GetValue(false);
            }
            set
            {
                SetValue(value);
            }
        }

        [DisplayName("Bug de la TaM")]
        [Description("Active le bug de la TaM en ajoutant 24 heures aux stations à minuit")]
        public bool EnableTamBug
        {
            get
            {
                return GetValue(false);
            }
            set
            {
                SetValue(value);
            }
        }

        [DisplayName("Mise à jour des widgets")]
        [Description("Affiche les horaires des deux prochains transports sur les widgets")]
        public bool EnableWidgetRefresh
        {
            get
            {
                return GetValue(false);
            }
            set
            {
                SetValue(value);
            }
        }

        [DisplayName("Afficher les favoris")]
        [Description("Affiche les favoris au démarrage de l'application")]
        public bool ShowFavorites
        {
            get
            {
                return GetValue(true);
            }
            set
            {
                SetValue(value);
            }
        }

        [DisplayName("Fonctionnalités expérimentales")]
        [Description("Active les fonctionnalités expérimentales de l'application")]
        public bool ExperimentalFeatures
        {
            get
            {
                return GetValue(false);
            }
            set
            {
                SetValue(value);
            }
        }

        public IList<Line> FavoriteLines
        {
            get
            {
                if (favoriteLines == null)
                {
                    favoriteLines = new ObservableCollection<Line>();

                    {
                        string[] linesData = Preferences.GetStringSet("Config." + nameof(FavoriteLines), new string[0]).ToArray();
                        foreach (string lineData in linesData)
                        {
                            JObject lineObject = JsonConvert.DeserializeObject(lineData) as JObject;

                            try
                            {
                                int lineId = lineObject["LineId"].Value<int>();
                                favoriteLines.Add(TramUrWayApplication.GetLine(lineId));
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }

                    favoriteLines.CollectionChanged += (s, e) =>
                    {
                        List<string> linesData = new List<string>();

                        foreach (Line line in favoriteLines)
                        {
                            JObject lineObject = new JObject()
                            {
                                ["LineId"] = line.Id
                            };

                            linesData.Add(lineObject.ToString());
                        }

                        ISharedPreferencesEditor editor = Preferences.Edit();
                        editor.PutStringSet("Config." + nameof(FavoriteLines), linesData);
                        editor.Apply();
                    };
                }

                return favoriteLines;
            }
        }
        private ObservableCollection<Line> favoriteLines;

        public IList<Stop> FavoriteStops
        {
            get
            {
                if (favoriteStops == null)
                {
                    favoriteStops = new ObservableCollection<Stop>();

                    {
                        string[] stopsData = Preferences.GetStringSet("Config." + nameof(FavoriteStops), new string[0]).ToArray();
                        foreach (string stopData in stopsData)
                        {
                            JObject stopObject = JsonConvert.DeserializeObject(stopData) as JObject;

                            try
                            {
                                int stopId = stopObject["StopId"].Value<int>();
                                int? lineId = stopObject.Property("LineId") == null ? null : stopObject["LineId"].Value<int>() as int?;

                                Line[] lines = TramUrWayApplication.Lines.Where(l => lineId.HasValue ? (l.Id == lineId) : true).ToArray();
                                Stop stop = lines.SelectMany(l => l.Stops).First(s => s.Id == stopId);

                                favoriteStops.Add(stop);
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }

                    favoriteStops.CollectionChanged += (s, e) =>
                    {
                        List<string> stopsData = new List<string>();

                        foreach (Stop stop in favoriteStops)
                        {
                            JObject stopObject = new JObject()
                            {
                                ["StopId"] = stop.Id,
                                ["LineId"] = stop.Line.Id
                            };

                            stopsData.Add(stopObject.ToString());
                        }

                        ISharedPreferencesEditor editor = Preferences.Edit();
                        editor.PutStringSet("Config." + nameof(FavoriteStops), stopsData);
                        editor.Apply();
                    };
                }

                return favoriteStops;
            }
        }
        private ObservableCollection<Stop> favoriteStops;

        public IDictionary<int, Step> StepWidgets
        {
            get
            {
                if (stepWidgets == null)
                {
                    stepWidgets = new ObservableDictionary<int, Step>();

                    {
                        string[] widgetsData = Preferences.GetStringSet("Config." + nameof(StepWidgets), new string[0]).ToArray();
                        foreach (string widgetData in widgetsData)
                        {
                            JObject widgetObject = JsonConvert.DeserializeObject(widgetData) as JObject;

                            try
                            {
                                int widgetId = widgetObject["WidgetId"].Value<int>();
                                int lineId = widgetObject["LineId"].Value<int>();
                                int routeId = widgetObject["RouteId"].Value<int>();
                                int stopId = widgetObject["StopId"].Value<int>();

                                Line line = TramUrWayApplication.GetLine(lineId);
                                Route route = line.Routes.FirstOrDefault(r => r.Id == routeId);
                                Step step = route.Steps.FirstOrDefault(s => s.Stop.Id == stopId);

                                if (step != null)
                                    stepWidgets.Add(widgetId, step);
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }

                    stepWidgets.CollectionChanged += (s, e) =>
                    {
                        List<string> widgetsData = new List<string>();

                        foreach (var pair in stepWidgets)
                        {
                            JObject widgetObject = new JObject()
                            {
                                ["WidgetId"] = pair.Key,
                                ["LineId"] = pair.Value.Route.Line.Id,
                                ["RouteId"] = pair.Value.Route.Id,
                                ["StopId"] = pair.Value.Stop.Id
                            };

                            widgetsData.Add(widgetObject.ToString());
                        }

                        ISharedPreferencesEditor editor = Preferences.Edit();
                        editor.PutStringSet("Config." + nameof(StepWidgets), widgetsData);
                        editor.Apply();
                    };
                }

                return stepWidgets;
            }
        }
        private ObservableDictionary<int, Step> stepWidgets;

        public Config(Context context) : base(context) { }
    }

    public static class ConfigExtensions
    {
        public static bool GetIsFavorite(this Line me)
        {
            return TramUrWayApplication.Config.FavoriteLines.Contains(me);
        }
        public static void SetIsFavorite(this Line me, bool value)
        {
            if (GetIsFavorite(me) && !value)
                TramUrWayApplication.Config.FavoriteLines.Remove(me);
            else if (value)
                TramUrWayApplication.Config.FavoriteLines.Add(me);
        }

        public static bool GetIsFavorite(this Stop me)
        {
            return TramUrWayApplication.Config.FavoriteStops.Any(s => s.Name == me.Name && s.Line.Id == me.Line.Id);
        }
        public static void SetIsFavorite(this Stop me, bool value)
        {
            if (GetIsFavorite(me) && !value)
                TramUrWayApplication.Config.FavoriteStops.Remove(me);
            else if (value)
                TramUrWayApplication.Config.FavoriteStops.Add(me);
        }
    }
}