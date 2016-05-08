using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Utilities;
using Android.Views;
using Android.Widget;

using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace TramUrWay.Android
{
    [Activity(Theme = "@style/AppTheme.NoActionBar")]
    public class LineActivity : AppCompatActivity
    {
        private Line line;
        private ViewPager viewPager;
        private List<TabFragment> fragments;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            App.Initialize(this);

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.LineActivity);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            // Handle bundle parameter
            Bundle extras = Intent.Extras;
            if (extras != null && extras.ContainsKey("Line"))
            {
                int lineId = -1;

                // Parse device path
                try
                {
                    lineId = extras.GetInt("Line");
                }
                catch (Exception e)
                {
                    Toast.MakeText(Parent, "Wrong line id", ToastLength.Short).Show();
                    Finish();
                }

                // Try to find device
                line = App.GetLine(lineId);
            }
#if DEBUG
            else
                line = App.GetLine(2);
#endif
            if (line == null)
                throw new Exception("Could not find any line matching the specified id");

            Title = line.Name;

            // Change toolbar color
            Color color = Utils.GetColorForLine(this, line);

            SupportActionBar.SetBackgroundDrawable(new ColorDrawable(color));

            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.ClearFlags(WindowManagerFlags.TranslucentStatus);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                Window.SetStatusBarColor(new Color(color.R * 2 / 3, color.G * 2 / 3, color.B * 2 / 3));

            // Tabs
            fragments = new List<TabFragment>() { new LineMapFragment(line, color) };
            foreach (Route route in line.Routes)
                fragments.Add(new RouteFragment(route, color));

            viewPager = FindViewById<ViewPager>(Resource.Id.LineActivity_ViewPager);
            viewPager.Adapter = new TabFragmentsAdapter(SupportFragmentManager, fragments.ToArray());
            viewPager.SetCurrentItem(1, false);

            TabLayout tabLayout = FindViewById<TabLayout>(Resource.Id.LineActivity_Tabs);
            tabLayout.SetBackgroundColor(color);
            tabLayout.SetupWithViewPager(viewPager);
            tabLayout.GetTabAt(0).SetIcon(Resource.Drawable.ic_map).SetText("");
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.LineMenu, menu);
            return true;
        }
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case global::Android.Resource.Id.Home:
                    OnBackPressed();
                    break;

                case Resource.Id.LineMenu_Refresh:
                    fragments[viewPager.CurrentItem].Refresh();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }
    }
}