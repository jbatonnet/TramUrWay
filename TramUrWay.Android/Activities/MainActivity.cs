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
    [Activity(Label = App.Name, MainLauncher = true, Icon = "@mipmap/ic_launcher", Theme = "@style/AppTheme.NoActionBar", LaunchMode = LaunchMode.SingleTask)]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        private int currentItem = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            App.Initialize(this);

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MainActivity);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            
            // Initliaze UI
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.SetDrawerListener(toggle);
            toggle.SyncState();

            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            if (Database.GetFavoriteStops().Any())
                currentItem = Resource.Id.SideMenu_Favorites;
            else
                currentItem = Resource.Id.SideMenu_Lines;
        }
        protected override void OnResume()
        {
            base.OnResume();

            Refresh();
        }

        public override void OnBackPressed()
        {
            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            if (drawer.IsDrawerOpen(GravityCompat.Start))
                drawer.CloseDrawer(GravityCompat.Start);
            else
                base.OnBackPressed();
        }
        public bool OnNavigationItemSelected(IMenuItem item)
        {
            currentItem = item.ItemId;
            Refresh();

            DrawerLayout drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            drawer.CloseDrawer(GravityCompat.Start);

            return true;
        }
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MainMenu, menu);

            for (int i = 0; i < menu.Size(); i++)
            {
                IMenuItem item = menu.GetItem(i);

                if (item.ItemId == Resource.Id.MainMenu_Offline)
                    item.SetChecked(App.OfflineMode ? true : false);
                else if (item.ItemId == Resource.Id.MainMenu_Bug)
                    item.SetChecked(App.EnableTamBug ? true : false);
            }

            return true;
        }
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.MainMenu_Offline:
                    item.SetChecked(App.OfflineMode = !App.OfflineMode);
                    break;

                case Resource.Id.MainMenu_Bug:
                    item.SetChecked(App.EnableTamBug = !App.EnableTamBug);
                    break;
            }

            Database.SetConfigValue(nameof(App.OfflineMode), App.OfflineMode ? "true" : "false");
            Database.SetConfigValue(nameof(App.EnableTamBug), App.EnableTamBug ? "true" : "false");

            return base.OnOptionsItemSelected(item);
        }

        private void Refresh()
        {
            switch (currentItem)
            {
                case Resource.Id.SideMenu_Favorites: RefreshFavorites(); break;
                case Resource.Id.SideMenu_Lines: RefreshLines(); break;
                case Resource.Id.SideMenu_Stops: RefreshStops(); break;
            }
        }
        private void RefreshFavorites()
        {
            FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
            fragmentTransaction.Replace(Resource.Id.MainActivity_Fragment, new FavoritesFragment());
            fragmentTransaction.Commit();
        }
        private void RefreshLines()
        {
            FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
            fragmentTransaction.Replace(Resource.Id.MainActivity_Fragment, new LinesFragment());
            fragmentTransaction.Commit();
        }
        private void RefreshStops()
        {
            FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
            fragmentTransaction.Replace(Resource.Id.MainActivity_Fragment, new StopsFragment());
            fragmentTransaction.Commit();
        }
    }
}