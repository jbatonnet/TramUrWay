using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
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
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using Android.Support.V4.Content;
using Android;
using Android.Locations;

namespace TramUrWay.Android
{
    [Activity(Theme = "@style/AppTheme.NoActionBar", LaunchMode = LaunchMode.SingleTask)]
    public class NearbyActivity : NavigationActivity
    {
        private ViewPager viewPager;
        private TabFragmentsAdapter fragmentsAdapter;

        private NearbyMapFragment mapFragment = new NearbyMapFragment();
        private NearbyStopsFragment stopsFragment = new NearbyStopsFragment();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            NavigationItemId = Resource.Id.SideMenu_Nearby;
            OnCreate(savedInstanceState, Resource.Layout.NearbyActivity);
            Title = "Autour de moi";

            // Tabs
            viewPager = FindViewById<ViewPager>(Resource.Id.NearbyActivity_ViewPager);
            viewPager.OffscreenPageLimit = 3;
            viewPager.Adapter = fragmentsAdapter = new TabFragmentsAdapter(SupportFragmentManager, mapFragment, stopsFragment);

            TabLayout tabLayout = FindViewById<TabLayout>(Resource.Id.NearbyActivity_Tabs);
            tabLayout.SetupWithViewPager(viewPager);

            viewPager.SetCurrentItem(1, false);
        }
    }
}