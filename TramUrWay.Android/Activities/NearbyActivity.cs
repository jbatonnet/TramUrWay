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

namespace TramUrWay.Android
{
    [Activity(Theme = "@style/AppTheme.NoActionBar", LaunchMode = LaunchMode.SingleTask)]
    public class NearbyActivity : NavigationActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            NavigationItemId = Resource.Id.SideMenu_Nearby;
            OnCreate(savedInstanceState, Resource.Layout.NearbyActivity);
            Title = "Autour de moi";
        }
    }
}