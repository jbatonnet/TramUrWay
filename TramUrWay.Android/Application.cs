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

using LogPriority = Android.Util.LogPriority;

namespace TramUrWay.Android
{
    [Application]
    public class TramUrWayApplication : BaseApplication
    {
        public override string Name => "TramUrWay";

        public const int GlobalUpdateDelay = 60;
        public const int WidgetUpdateDelay = 60;
        public const int MinimumServiceDelay = 30;

        public const int MapStopIconSize = 10;
        public const int MapTransportIconSize = 22;

        public static Config Config { get; private set; }
        public new static Assets Assets { get; private set; }
        public static WebService Service { get; private set; }

        public static Line[] Lines
        {
            get
            {
                if (lines == null)
                    lines = Assets.PreloadLines();

                return lines;
            }
        }

        private static Line[] lines;

        public TramUrWayApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        public override void OnCreate()
        {
            base.OnCreate();
            
            // Load data
            Config = new Config(this);
            Assets = new Assets(this);
            Service = new WebService();

#if DEBUG
            // Enable experimental features on debug builds
            Config.ExperimentalFeatures = true;
            Config.EnableWidgetRefresh = true;
#endif

            // Trigger widgets update
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(this);
            StepWidget.Update(this, appWidgetManager);

            if (Config.EnableWidgetRefresh)
                WidgetUpdateService.Start(this);
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