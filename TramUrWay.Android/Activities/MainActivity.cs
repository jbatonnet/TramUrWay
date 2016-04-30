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
using SearchView = Android.Support.V7.Widget.SearchView;
using System.Threading.Tasks;
using static Android.Support.V7.Widget.SearchView;

namespace TramUrWay.Android
{
    [Activity(Theme = "@style/AppTheme.NoActionBar", LaunchMode = LaunchMode.SingleTask)]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        private int currentItem = 0;

        private DrawerLayout drawer;
        private NavigationView navigationView;
        private MainFragment fragment;
        private SearchView searchView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            App.Initialize(this);

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MainActivity);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            
            // Initliaze UI
            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.SetDrawerListener(toggle);
            toggle.SyncState();

            navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            if (App.Database.GetFavoriteStops().Any())
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
            switch (item.ItemId)
            {
                case Resource.Id.SideMenu_Settings:
                {
                    StartActivity(new Intent(this, typeof(SettingsActivity)));
                    break;
                }

                case Resource.Id.SideMenu_About:
                {
                    StartActivity(new Intent(this, typeof(AboutActivity)));
                    break;
                }

                default:
                    currentItem = item.ItemId;
                    Refresh();
                    break;
            }

            drawer.CloseDrawer(GravityCompat.Start);
            return true;
        }
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.MainMenu, menu);

            for (int i = 0; i < menu.Size(); i++)
            {
                IMenuItem item = menu.GetItem(i);

                if (item.ItemId == Resource.Id.MainMenu_Search)
                {
                    SearchManager searchManager = (SearchManager)GetSystemService(SearchService);

                    searchView = item.ActionView as SearchView;
                    searchView.SetSearchableInfo(searchManager.GetSearchableInfo(ComponentName));
                    searchView.QueryHint = "Rechercher";
                    searchView.QueryTextChange += SearchView_QueryTextChange;
                }
            }

            return base.OnCreateOptionsMenu(menu);
        }

        private void Refresh()
        {
            for (int i = 0; i < navigationView.Menu.Size(); i++)
            {
                IMenuItem menuItem = navigationView.Menu.GetItem(i);
                menuItem.SetChecked(menuItem.ItemId == currentItem);
            }

            switch (currentItem)
            {
                case Resource.Id.SideMenu_Favorites: RefreshFavorites(); break;
                case Resource.Id.SideMenu_Lines: RefreshLines(); break;
                case Resource.Id.SideMenu_Stops: RefreshStops(); break;
                case Resource.Id.SideMenu_Routes: RefreshRoutes(); break;
                case Resource.Id.SideMenu_Map: RefreshMap(); break;
            }
        }
        private void RefreshFavorites()
        {
            FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
            fragmentTransaction.Replace(Resource.Id.MainActivity_Fragment, fragment = new FavoritesFragment());
            fragmentTransaction.Commit();
        }
        private void RefreshLines()
        {
            FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
            fragmentTransaction.Replace(Resource.Id.MainActivity_Fragment, fragment = new LinesFragment());
            fragmentTransaction.Commit();
        }
        private void RefreshStops()
        {
            FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
            fragmentTransaction.Replace(Resource.Id.MainActivity_Fragment, fragment = new StopsFragment());
            fragmentTransaction.Commit();
        }
        private void RefreshRoutes()
        {
            FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
            fragmentTransaction.Replace(Resource.Id.MainActivity_Fragment, fragment = new RoutesFragment());
            fragmentTransaction.Commit();
        }
        private void RefreshMap()
        {
            FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
            fragmentTransaction.Replace(Resource.Id.MainActivity_Fragment, fragment = new MapFragment());
            fragmentTransaction.Commit();
        }

        private void SearchView_QueryTextChange(object sender, QueryTextChangeEventArgs e)
        {
            if (fragment?.HandleSearch(sender, e) == true)
                return;

            currentItem = Resource.Id.SideMenu_Stops;
            Refresh();

            searchView.Post(() => fragment?.HandleSearch(sender, e));
        }
    }
}