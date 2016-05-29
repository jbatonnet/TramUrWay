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
                })
                .ContinueWith(t =>
                {
#if DEBUG
                    Intent intent = new Intent(this, typeof(HomeActivity));

                    /*intent.PutExtra("RouteSegments", new string[]
                    {
                        @"{ ""Line"": 2, ""From.Route"": 1, ""From.Stop"": 42221, ""From.Date"": ""2016-05-27T23:18:00+02:00"", ""To.Route"": 1, ""To.Stop"": 42229, ""To.Date"": ""2016-05-27T23:26:00+02:00"" }",
                        @"{ ""Line"": 4, ""From.Route"": 1, ""From.Stop"": 41131, ""From.Date"": ""2016-05-27T23:28:00+02:00"", ""To.Route"": 1, ""To.Stop"": 41145, ""To.Date"": ""2016-05-27T23:36:04+02:00"" }",
                        @"{ ""Line"": 1, ""From.Route"": 1, ""From.Stop"": 41145, ""From.Date"": ""2016-05-27T23:38:00+02:00"", ""To.Route"": 1, ""To.Stop"": 41155, ""To.Date"": ""2016-05-27T23:48:54+02:00"" }"
                    });*/

                    StartActivity(intent);
#else
                    Intent intent = new Intent(this, typeof(HomeActivity));
                    StartActivity(intent);
#endif
                });
        }
    }
}