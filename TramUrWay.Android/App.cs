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
    public static class App
    {
        public const string Name = "TramUrWay";

        public const int GlobalUpdateDelay = 60;
        public const int WidgetUpdateDelay = 120;

        private static bool initialized = false;
        private static AndroidDatabaseConnection connection;

        public static void Initialize(Context context)
        {
            if (initialized) return;
            initialized = true;

            // Connect to local database
            connection = new AndroidDatabaseConnection(context, Name + ".db");
            connection.VersionUpgraded += Connection_VersionUpgraded;
            connection.Open();

            // Load data
            Database.Initialize(context, connection);

            // Trigger widgets update
            //Intent intent = new Intent();
            //intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            //context.SendBroadcast(intent);
        }

        private static void Connection_VersionUpgraded(AndroidDatabaseConnection connection, int oldVersion, int newVersion)
        {
            Database.CheckDatabase(connection);
        }
    }
}