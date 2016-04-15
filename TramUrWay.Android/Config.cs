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

namespace TramUrWay.Android
{
    public class Config
    {
        public bool OfflineMode
        {
            get
            {
                return preferences.GetBoolean("Config.OfflineMode", false);
            }
            set
            {
                ISharedPreferencesEditor editor = preferences.Edit();
                editor.PutBoolean("Config.OfflineMode", value);
                editor.Apply();
            }
        }
        public bool EnableTamBug
        {
            get
            {
                return preferences.GetBoolean("Config.EnableTamBug", false);
            }
            set
            {
                ISharedPreferencesEditor editor = preferences.Edit();
                editor.PutBoolean("Config.EnableTamBug", value);
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