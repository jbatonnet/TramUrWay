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
    public class LineActivity : BaseActivity
    {
        private Line line;
        private List<Transport> transports = new List<Transport>();

        private ViewPager viewPager;
        private Snackbar snackbar;
        private List<TabFragment> fragments;

        private CancellationTokenSource refreshCancellationTokenSource = new CancellationTokenSource();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            OnCreate(savedInstanceState, Resource.Layout.LineActivity);

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
                line = TramUrWayApplication.GetLine(lineId);
            }
#if DEBUG
            else
                line = TramUrWayApplication.GetLine(2);
#endif
            if (line == null)
                throw new Exception("Could not find any line matching the specified id");

            Title = line.Name;

            // Change toolbar color
            Color color = Utils.GetColorForLine(this, line);
            Color darkColor = new Color(color.R * 2 / 3, color.G * 2 / 3, color.B * 2 / 3);

            SupportActionBar.SetBackgroundDrawable(new ColorDrawable(color));

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                Window.SetStatusBarColor(darkColor);
            }

            // Tabs
            fragments = new List<TabFragment>() { new LineMapFragment(line, color) };
            foreach (Route route in line.Routes)
            {
                LineRouteFragment routeFragment = new LineRouteFragment(route, color);
                routeFragment.QueryRefresh += SwipeRefresh_Refresh;

                fragments.Add(routeFragment);
            }

            viewPager = FindViewById<ViewPager>(Resource.Id.LineActivity_ViewPager);
            viewPager.OffscreenPageLimit = fragments.Count;
            viewPager.Adapter = new TabFragmentsAdapter(SupportFragmentManager, fragments.ToArray());

            if (extras != null && extras.ContainsKey("Route"))
            {
                int routeId = extras.GetInt("Route");
                viewPager.SetCurrentItem(1 + routeId, false);
            }
            else
                viewPager.SetCurrentItem(1, false);

            TabLayout tabLayout = FindViewById<TabLayout>(Resource.Id.LineActivity_Tabs);
            tabLayout.SetBackgroundColor(color);
            tabLayout.SetSelectedTabIndicatorColor(unchecked((int)0xFFFFFFFF));
            tabLayout.SetupWithViewPager(viewPager);
            tabLayout.GetTabAt(0).SetIcon(Resource.Drawable.ic_map).SetText("");
        }
        protected override void OnPause()
        {
            refreshCancellationTokenSource?.Cancel();
            snackbar?.Dismiss();

            base.OnPause();
        }
        protected override void OnResume()
        {
            base.OnResume();

            // Cancel refresh tasks
            refreshCancellationTokenSource?.Cancel();
            refreshCancellationTokenSource = new CancellationTokenSource();

            // Run new refresh tasks
            Task.Run(async () =>
            {
                CancellationTokenSource cancellationTokenSource = refreshCancellationTokenSource;
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    Refresh();
                    await Task.Delay(TramUrWayApplication.GlobalUpdateDelay * 1000);
                }
            });
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
                    if (snackbar?.IsShown == false)
                        snackbar = null;

                    Refresh();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void SwipeRefresh_Refresh(object sender, EventArgs e)
        {
            if (snackbar?.IsShown == false)
                snackbar = null;

            Refresh();
        }
        private void Snackbar_Retry(View v)
        {
            snackbar?.Dismiss();
            snackbar = null;

            Refresh();
        }
        private void Snackbar_Activate(View v)
        {
            snackbar?.Dismiss();
            snackbar = null;

            StartActivity(typeof(SettingsActivity));
        }

        public async void Refresh()
        {
            await Task.Run(() =>
            {
                TimeStep[] timeSteps;
                DateTime now = DateTime.Now;

                RunOnUiThread(OnRefreshing);

                // Online time steps
                try
                {
                    if (TramUrWayApplication.Config.OfflineMode)
                        throw new Exception();

                    /*timeSteps = App.Service.GetLiveTimeSteps()
                        .Where(t => t.Step.Route.Line == line)
                        .OrderBy(t => t.Date)
                        .ToArray();*/

                    timeSteps = TramUrWayApplication.Service.GetLiveTimeSteps(line)
                        .OrderBy(t => t.Date)
                        .ToArray();

                    snackbar?.Dismiss();
                }
                catch (Exception e)
                {
                    timeSteps = line.Routes.SelectMany(r =>
                    {
                        TimeTable timeTable = r.TimeTable;

                        // Offline data
                        if (timeTable != null)
                        {
                            TimeStep[] routeSteps = r.Steps.SelectMany(s => timeTable.GetStepsFromStep(s, now).Take(3)).ToArray();

                            if (snackbar == null)
                            {
                                snackbar = Snackbar.Make(viewPager, "Données hors-ligne", Snackbar.LengthIndefinite);
                                if (TramUrWayApplication.Config.OfflineMode)
                                    snackbar = snackbar.SetAction("Activer", Snackbar_Activate);
                                else
                                    snackbar = snackbar.SetAction("Réessayer", Snackbar_Retry);

                                snackbar.Show();
                            }

                            return routeSteps;
                        }

                        // No data
                        else
                        {
                            if (snackbar == null)
                            {
                                snackbar = Snackbar.Make(viewPager, "Aucune donnée disponible", Snackbar.LengthIndefinite);
                                if (TramUrWayApplication.Config.OfflineMode)
                                    snackbar = snackbar.SetAction("Activer", Snackbar_Activate);
                                else
                                    snackbar = snackbar.SetAction("Réessayer", Snackbar_Retry);

                                snackbar.Show();
                            }

                            return Enumerable.Empty<TimeStep>();
                        }
                    }).ToArray();
                }
                
                // Update transports with new data
                transports.Update(timeSteps, now);

                RunOnUiThread(() => OnRefreshed(timeSteps, transports));
            });
        }
        private void OnRefreshing()
        {
            foreach (TabFragment fragment in fragments)
            {
                (fragment as LineMapFragment)?.OnRefreshing();
                (fragment as LineRouteFragment)?.OnRefreshing();
            }
        }
        private void OnRefreshed(IEnumerable<TimeStep> timeSteps, IEnumerable<Transport> transports)
        {
            foreach (TabFragment fragment in fragments)
            {
                (fragment as LineMapFragment)?.OnRefreshed(timeSteps, transports);
                (fragment as LineRouteFragment)?.OnRefreshed(timeSteps, transports);
            }
        }
    }
}