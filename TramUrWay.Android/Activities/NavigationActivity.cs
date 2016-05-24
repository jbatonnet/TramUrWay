using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Graphics;
using Android.Graphics.Drawables;
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
using System.Threading;
using Android.Util;

namespace TramUrWay.Android
{
    public abstract class NavigationActivity : BaseActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        protected int NavigationItemId { get; set; }

        protected DrawerLayout drawer;
        protected NavigationView navigationView;

        protected override void OnPostCreate()
        {
            base.OnPostCreate();

            // Initialize UI
            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            ActionBarDrawerToggle toggle = new ActionBarDrawerToggle(this, drawer, toolbar, Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            drawer.SetDrawerListener(toggle);
            toggle.SyncState();

            // Setup navigation view
            navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            navigationView.SetNavigationItemSelectedListener(this);

            for (int i = 0; i < navigationView.Menu.Size(); i++)
            {
                IMenuItem menuItem = navigationView.Menu.GetItem(i);
                menuItem.SetChecked(menuItem.ItemId == NavigationItemId);
            }
        }

        protected override void OnResume()
        {
            for (int i = 0; i < navigationView.Menu.Size(); i++)
            {
                IMenuItem menuItem = navigationView.Menu.GetItem(i);
                menuItem.SetChecked(menuItem.ItemId == NavigationItemId);

                if (App.Config.ExperimentalFeatures)
                {
                    if (menuItem.ItemId == Resource.Id.SideMenu_Routes)
                        menuItem.SetEnabled(true);
                }
            }

            base.OnResume();
        }
        public bool OnNavigationItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.SideMenu_Home:
                {
                    if (!(this is HomeActivity))
                        StartActivity(new Intent(this, typeof(HomeActivity)));
                    break;
                }

                case Resource.Id.SideMenu_Routes:
                {
                    if (!(this is RoutesActivity))
                        StartActivity(new Intent(this, typeof(RoutesActivity)));
                    break;
                }

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
    }
}