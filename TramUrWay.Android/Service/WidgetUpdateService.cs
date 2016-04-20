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
    [Service]
    public class WidgetUpdateService : Service
    {
        private PendingIntent pendingIntent;

        public override void OnCreate()
        {
            base.OnCreate();

            // Register ScreenOn, ScreenOff intents
            RegisterReceiver(IntentBroadcastReceiver.Instance, new IntentFilter(Intent.ActionScreenOn));
            RegisterReceiver(IntentBroadcastReceiver.Instance, new IntentFilter(Intent.ActionScreenOff));

            // Create widget update intent
            Intent intent = new Intent();
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            pendingIntent = PendingIntent.GetBroadcast(this, 0, intent, 0);

            // Setup an alarm
            AlarmManager alarmManager = GetSystemService(AlarmService) as AlarmManager;
            alarmManager.SetInexactRepeating(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime(), App.WidgetUpdateDelay * 1000, pendingIntent);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            // Unregister ScreenOn, ScreenOff intents
            UnregisterReceiver(IntentBroadcastReceiver.Instance);
            UnregisterReceiver(IntentBroadcastReceiver.Instance);

            // Cancel the pending intent
            AlarmManager alarmManager = GetSystemService(AlarmService) as AlarmManager;
            alarmManager.Cancel(pendingIntent);
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
        }

        public static void Start(Context context, bool force = false)
        {
            bool start = force;

            if (!start)
            {
                AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(context);

                ComponentName stepWidgetComponent = StepWidget.GetComponentName(context);
                start |= appWidgetManager.GetAppWidgetIds(stepWidgetComponent).Length > 0;
            }

            if (start)
            {
                Intent intent = new Intent(context, typeof(WidgetUpdateService));
                context.StartService(intent);
            }
        }
        public static void Stop(Context context)
        {
            Intent intent = new Intent(context, typeof(WidgetUpdateService));
            context.StopService(intent);
        }
    }
}