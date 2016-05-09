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
        private DrawerLayout drawer;
        private NavigationView navigationView;
        private SearchView searchView;

        private FavoritesFragment favoritesFragment = new FavoritesFragment();
        private LinesFragment linesFragment = new LinesFragment();
        private StopsFragment stopsFragment = new StopsFragment();
        private ViewPager viewPager;
        private TabFragmentsAdapter fragmentsAdapter;

        private string lastSearch = "";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            App.Initialize(this);

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MainActivity);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            
            // Initialize UI
            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.SetDrawerListener(toggle);
            toggle.SyncState();

            // Tabs
            viewPager = FindViewById<ViewPager>(Resource.Id.MainActivity_ViewPager);
            viewPager.Adapter = fragmentsAdapter = new TabFragmentsAdapter(SupportFragmentManager, favoritesFragment, linesFragment, stopsFragment);

            TabLayout tabLayout = FindViewById<TabLayout>(Resource.Id.MainActivity_Tabs);
            tabLayout.SetupWithViewPager(viewPager);

            // Setup navigation view
            navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            for (int i = 0; i < navigationView.Menu.Size(); i++)
            {
                IMenuItem menuItem = navigationView.Menu.GetItem(i);
                menuItem.SetChecked(menuItem.ItemId == Resource.Id.SideMenu_Home);
            }

            if (App.Config.ShowFavorites && App.Database.GetFavoriteStops().Any())
                viewPager.SetCurrentItem(0, true);
            else
                viewPager.SetCurrentItem(1, true);
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

        private void SearchView_QueryTextChange(object sender, QueryTextChangeEventArgs e)
        {
            searchView.Post(() =>
            {
                viewPager.SetCurrentItem(2, true);
                stopsFragment.OnQueryTextChanged(sender, e);
            });

            if (lastSearch.Length > 1 && e.NewText.Length == 0)
            {
                searchView.Post(() =>
                {
                    searchView.ClearFocus();
                    searchView.Iconified = true;
                });
            }

            lastSearch = e.NewText;
        }
    }
}