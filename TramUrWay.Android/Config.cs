using System;
using System.Collections;
using System.Collections.Generic;
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
using System.ComponentModel;

namespace TramUrWay.Android
{
    public class Config
    {
        [DisplayName("Mode hors-ligne")]
        [Description("Force l'utilisation des données hors-ligne dans l'application")]
        public bool OfflineMode
        {
            get
            {
                return preferences.GetBoolean("Config." + nameof(OfflineMode), false);
            }
            set
            {
                ISharedPreferencesEditor editor = preferences.Edit();
                editor.PutBoolean("Config." + nameof(OfflineMode), value);
                editor.Apply();
            }
        }

        [DisplayName("Bug de la TaM")]
        [Description("Active le bug de la TaM en ajoutant 24 heures aux stations à minuit")]
        public bool EnableTamBug
        {
            get
            {
                return preferences.GetBoolean("Config." + nameof(EnableTamBug), false);
            }
            set
            {
                ISharedPreferencesEditor editor = preferences.Edit();
                editor.PutBoolean("Config." + nameof(EnableTamBug), value);
                editor.Apply();
            }
        }

        [DisplayName("Mise à jour des widgets")]
        [Description("Affiche les horaires des deux prochains transports sur les widgets")]
        public bool EnableWidgetRefresh
        {
            get
            {
                return preferences.GetBoolean("Config." + nameof(EnableWidgetRefresh), false);
            }
            set
            {
                ISharedPreferencesEditor editor = preferences.Edit();
                editor.PutBoolean("Config." + nameof(EnableWidgetRefresh), value);
                editor.Apply();
            }
        }

        [DisplayName("Afficher les favoris")]
        [Description("Affiche les favoris au démarrage de l'application")]
        public bool ShowFavorites
        {
            get
            {
                return preferences.GetBoolean("Config." + nameof(ShowFavorites), true);
            }
            set
            {
                ISharedPreferencesEditor editor = preferences.Edit();
                editor.PutBoolean("Config." + nameof(ShowFavorites), value);
                editor.Apply();
            }
        }

        private ISharedPreferences preferences;

        public Config(Context context)
        {
            preferences = context.GetSharedPreferences(App.Name + ".conf", FileCreationMode.Private);
        }
    }
}