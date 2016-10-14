using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Android.App;
using Android.Support.V7.App;

namespace TramUrWay.Android
{
    [Activity(Label = "TramUrWay", MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@style/AppTheme.SplashScreen", NoHistory = true)]
    public class SplashScreenActivity : AppCompatActivity
    {
        protected override void OnResume()
        {
            base.OnResume();

            Task.Run(() =>
                {
                    TramUrWayApplication.Lines.ToString();
                })
                .ContinueWith(t =>
                {
#if DEBUG
                    StartActivity(typeof(HomeActivity));
#else
                    StartActivity(typeof(HomeActivity));
#endif
                });
        }
    }
}