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

namespace TramUrWay.Android
{
    [Activity(Theme = "@style/AppTheme.NoActionBar")]
    public class LineActivity : AppCompatActivity
    {
        private Line line;
        private Route route;

        private CancellationTokenSource refreshCancellationTokenSource = new CancellationTokenSource();
        private RouteAdapter routeAdapter;

        private Snackbar snackbar;
        private RecyclerView recyclerView;
        private SwipeRefreshLayout swipeRefresh;
        private Switch routeSwitch;
        private Spinner routeChoice;

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
            route = line.Routes.First();

            // Change toolbar color
            Color color = Utils.GetColorForLine(this, line);

            SupportActionBar.SetBackgroundDrawable(new ColorDrawable(color));

            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.ClearFlags(WindowManagerFlags.TranslucentStatus);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                Window.SetStatusBarColor(new Color(color.R * 2 / 3, color.G * 2 / 3, color.B * 2 / 3));

            // Refresh widget
            swipeRefresh = FindViewById<SwipeRefreshLayout>(Resource.Id.LineActivity_SwipeRefresh);
            swipeRefresh.Refresh += SwipeRefresh_Refresh;
            swipeRefresh.SetColorSchemeColors(color.ToArgb());

            // Initialize UI
            View routeSwitchView = FindViewById(Resource.Id.LineActivity_RouteSwitchLayout);
            View routeChoiceView = FindViewById(Resource.Id.LineActivity_RouteChoiceLayout);
            if (line.Routes.Length == 2)
            {
                routeChoiceView.Visibility = ViewStates.Invisible;

                TextView switchLeftChoice = FindViewById<TextView>(Resource.Id.LineActivity_RouteSwitch_LeftChoice);
                switchLeftChoice.Text = line.Routes[0].Steps.Last().Stop.Name;
                TextView switchRightChoice = FindViewById<TextView>(Resource.Id.LineActivity_RouteSwitch_RightChoice);
                switchRightChoice.Text = line.Routes[1].Steps.Last().Stop.Name;

                routeSwitch = FindViewById<Switch>(Resource.Id.LineActivity_RouteSwitch);
                routeSwitch.CheckedChange += RouteSwitch_CheckedChange;
            }
            else
            {
                routeSwitchView.Visibility = ViewStates.Invisible;

                ArrayAdapter<string> spinnerAdapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerItem, global::Android.Resource.Id.Text1);
                spinnerAdapter.SetDropDownViewResource(global::Android.Resource.Layout.SimpleSpinnerDropDownItem);

                foreach (Route route in line.Routes)
                    spinnerAdapter.Add("De " + route.Steps.First().Stop.Name + " vers " + route.Steps.Last().Stop.Name);

                routeChoice = FindViewById<Spinner>(Resource.Id.LineActivity_RouteChoice);
                routeChoice.Adapter = spinnerAdapter;
                routeChoice.ItemSelected += RouteChoice_ItemSelected;
            }

            // Show the list
            recyclerView = FindViewById<RecyclerView>(Resource.Id.LineActivity_StopList);
            recyclerView.Focusable = false;
            recyclerView.HasFixedSize = true;
            recyclerView.NestedScrollingEnabled = false;
            recyclerView.SetLayoutManager(new WrapLayoutManager(recyclerView));
            recyclerView.AddItemDecoration(new DividerItemDecoration(this, LinearLayoutManager.Vertical));
            recyclerView.SetAdapter(routeAdapter = new RouteAdapter(route));
        }
        protected override void OnPause()
        {
            refreshCancellationTokenSource?.Cancel();

            base.OnPause();
        }
        protected override void OnResume()
        {
            refreshCancellationTokenSource?.Cancel();
            refreshCancellationTokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                while (!refreshCancellationTokenSource.IsCancellationRequested)
                {
                    Refresh();
                    Thread.Sleep(App.GlobalUpdateDelay * 1000);
                }
            });
            Task.Run(() =>
            {
                while (!refreshCancellationTokenSource.IsCancellationRequested)
                {
                    RefreshIcons();
                    Thread.Sleep(App.GlobalUpdateDelay / 4 * 1000);
                }
            });

            base.OnResume();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case global::Android.Resource.Id.Home:
                    OnBackPressed();
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
        private void RouteSwitch_CheckedChange(object sender, EventArgs e)
        {
            route = routeSwitch.Checked ? line.Routes[1] : line.Routes[0];
            recyclerView.SetAdapter(routeAdapter = new RouteAdapter(route));

            snackbar?.Dismiss();
            Refresh();
        }
        private void RouteChoice_ItemSelected(object sender, EventArgs e)
        {
            route = line.Routes[routeChoice.SelectedItemPosition];
            recyclerView.SetAdapter(routeAdapter = new RouteAdapter(route));

            snackbar?.Dismiss();
            Refresh();
        }
        private void Snackbar_Retry(View v)
        {
            snackbar?.Dismiss();
            snackbar = null;

            Refresh();
        }

        private void Refresh()
        {
            swipeRefresh.Post(() => swipeRefresh.Refreshing = true);

            Task.Run(() =>
            {
                TimeStep[] timeSteps;

                // Online time steps
                try
                {
                    if (App.Config.OfflineMode)
                        throw new Exception();

                    timeSteps = App.Service.GetLiveTimeSteps().OrderBy(t => t.Date).ToArray();
                    snackbar?.Dismiss();
                }
                catch (Exception e)
                {
                    TimeTable timeTable = route.GetTimeTable();

                    // Offline data
                    if (timeTable != null)
                    {
                        DateTime now = DateTime.Now;
                        timeSteps = route.Steps.SelectMany(s => timeTable.GetStepsFromStep(s, now).Take(3)).ToArray();

                        if (snackbar == null)
                        {
                            snackbar = Snackbar.Make(swipeRefresh, "Données hors-ligne", Snackbar.LengthIndefinite);
                            if (!App.Config.OfflineMode)
                                snackbar = snackbar.SetAction("Réessayer", Snackbar_Retry);

                            snackbar.Show();
                        }
                    }
                    
                    // No data
                    else
                    {
                        timeSteps = null;

                        if (snackbar == null)
                        {
                            snackbar = Snackbar.Make(swipeRefresh, "Aucune donnée disponible", Snackbar.LengthIndefinite);
                            if (!App.Config.OfflineMode)
                                snackbar = snackbar.SetAction("Réessayer", Snackbar_Retry);

                            snackbar.Show();
                        }
                    }
                }

                routeAdapter.UpdateSteps(timeSteps);

                swipeRefresh.Post(() => swipeRefresh.Refreshing = false);

                RunOnUiThread(OnRefreshed);
            });
        }
        private void RefreshIcons()
        {
            routeAdapter.UpdateIcons();
            RunOnUiThread(OnRefreshed);
        }
        private void OnRefreshed()
        {
            routeAdapter?.NotifyDataSetChanged();
        }
    }
}