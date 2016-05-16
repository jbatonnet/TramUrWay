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
using Android.Views.InputMethods;

namespace TramUrWay.Android
{
    [Activity(Theme = "@style/AppTheme.NoActionBar", LaunchMode = LaunchMode.SingleTask)]
    public class HomeActivity : NavigationActivity
    {
        private SearchView searchView;

        private FavoritesFragment favoritesFragment = new FavoritesFragment();
        private LinesFragment linesFragment = new LinesFragment();
        private StopsFragment stopsFragment = new StopsFragment();
        private ViewPager viewPager;
        private TabFragmentsAdapter fragmentsAdapter;

        private string lastSearch = "";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SetContentView(Resource.Layout.HomeActivity);
            NavigationItemId = Resource.Id.SideMenu_Home;

            base.OnCreate(savedInstanceState);

            // Tabs
            viewPager = FindViewById<ViewPager>(Resource.Id.MainActivity_ViewPager);
            viewPager.Adapter = fragmentsAdapter = new TabFragmentsAdapter(SupportFragmentManager, favoritesFragment, linesFragment, stopsFragment);
            viewPager.PageSelected += ViewPager_PageSelected;

            TabLayout tabLayout = FindViewById<TabLayout>(Resource.Id.MainActivity_Tabs);
            tabLayout.SetupWithViewPager(viewPager);

            if (App.Config.ShowFavorites && App.Config.FavoriteStops.Any())
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

        private void ViewPager_PageSelected(object sender, ViewPager.PageSelectedEventArgs e)
        {
            if (e.Position != 2 && !string.IsNullOrEmpty(searchView?.Query))
            {
                RunOnUiThread(() =>
                {
                    lastSearch = "  ";

                    searchView.SetQuery("", true);
                    stopsFragment.OnQueryTextChanged(sender, new QueryTextChangeEventArgs(true, ""));
                });
            }
        }
        private void SearchView_QueryTextChange(object sender, QueryTextChangeEventArgs e)
        {
            if (e.NewText.Length > 0)
            {
                RunOnUiThread(() =>
                {
                    if (viewPager.CurrentItem != 2)
                        viewPager.SetCurrentItem(2, true);

                    stopsFragment.OnQueryTextChanged(sender, e);
                });
            }

            if (lastSearch.Length > 1 && e.NewText.Length == 0)
            {
                RunOnUiThread(() =>
                {
                    searchView.ClearFocus();
                    searchView.Iconified = true;

                    stopsFragment.OnQueryTextChanged(sender, e);
                });

                searchView.PostDelayed(() =>
                {
                    InputMethodManager inputMethodManager = GetSystemService(Context.InputMethodService) as InputMethodManager;
                    inputMethodManager.HideSoftInputFromWindow(searchView.WindowToken, HideSoftInputFlags.None);
                }, 250);
            }

            lastSearch = e.NewText;
        }
    }
}