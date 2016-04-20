using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Graphics.Drawable;
using Android.Views;
using Android.Widget;

namespace TramUrWay.Android
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class IntentBroadcastReceiver : BroadcastReceiver
    {
        public static IntentBroadcastReceiver Instance { get; } = new IntentBroadcastReceiver();

        public override void OnReceive(Context context, Intent intent)
        {
            App.Initialize(context);

            if (intent.Action == Intent.ActionScreenOn)
                WidgetUpdateService.Start(context);
            else if (intent.Action == Intent.ActionScreenOff)
                WidgetUpdateService.Stop(context);
        }
    }
}