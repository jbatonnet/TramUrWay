﻿using System;
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

using LogPriority = Android.Util.LogPriority;

namespace TramUrWay.Android
{
    public static class App
    {
        public const string Name = "TramUrWay";

        public const int GlobalUpdateDelay = 60;
        public const int WidgetUpdateDelay = 60;

        public static Config Config { get; private set; }
        public static Assets Assets { get; private set; }
        public static WebService Service { get; private set; }

        public static Line[] Lines { get; private set; }

        private static bool initialized = false;
        public static void Initialize(Context context)
        {
            if (initialized) return;
            initialized = true;

            // Initialize logging
#if DEBUG
            Log.TraceStream = new LogcatWriter(App.Name, LogPriority.Verbose);
            Log.DebugStream = new LogcatWriter(App.Name, LogPriority.Debug);
#endif
            Log.InfoStream = new LogcatWriter(App.Name, LogPriority.Info);
            Log.WarningStream = new LogcatWriter(App.Name, LogPriority.Warn);
            Log.ErrorStream = new LogcatWriter(App.Name, LogPriority.Error);

            // Load data
            Config = new Config(context);
            Assets = new Assets(context);
            Service = new WebService();

            // Trigger widgets update
            if (Config.EnableWidgetRefresh)
                WidgetUpdateService.Start(context);

            // Preload lines
            Lines = Assets.LoadLines();

#if DEBUG
            // Enable experimental features on debug builds
            Config.ExperimentalFeatures = true;
#endif
        }

        public static Line GetLine(int id)
        {
            return Lines.FirstOrDefault(l => l.Id == id);
        }
        public static Stop GetStop(int id)
        {
            return Lines.SelectMany(l => l.Stops).FirstOrDefault(s => s.Id == id);
        }
    }
}