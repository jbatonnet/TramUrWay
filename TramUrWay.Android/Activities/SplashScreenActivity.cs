using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Content.PM;
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

using Toolbar = Android.Support.V7.Widget.Toolbar;
using System.Threading.Tasks;

namespace TramUrWay.Android
{
    [Activity(Label = App.Name, MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@style/AppTheme.SplashScreen", NoHistory = true)]
    public class SplashScreenActivity : AppCompatActivity
    {
        protected override void OnResume()
        {
            base.OnResume();

            Task.Run(() =>
            {
                App.Initialize(this);
            }).ContinueWith(t =>
            {
                Intent intent = new Intent(this, typeof(MainActivity));
                StartActivity(intent);
            });
        }
    }
}