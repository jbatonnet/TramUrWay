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

            Intent intent = new Intent();
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);

            pendingIntent = PendingIntent.GetBroadcast(this, 0, intent, 0);

            AlarmManager alarmManager = GetSystemService(AlarmService) as AlarmManager;
            alarmManager.SetRepeating(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime(), App.WidgetUpdateDelay * 1000, pendingIntent);
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

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
    }
}