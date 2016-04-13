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
        private Dictionary<Step, TimeStep[]> stepTimes;

        private Snackbar dataSnackbar;

        private SwipeRefreshLayout swipeRefresh;
        private CancellationTokenSource refreshCancellationTokenSource = new CancellationTokenSource();
        
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
                line = Database.GetLine(lineId);
            }
#if DEBUG
            else
                line = Database.GetLine(2);
#endif
            if (line == null)
                throw new Exception("Could not find any line matching the specified id");

            Title = line.Name;

            // Change toolbar color
            Color color = Database.GetColorForLine(line);

            SupportActionBar.SetBackgroundDrawable(new ColorDrawable(color));

            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
            Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
            Window.SetStatusBarColor(new Color(color.R * 2 / 3, color.G * 2 / 3, color.B * 2 / 3));

            // Refresh widget
            swipeRefresh = FindViewById<SwipeRefreshLayout>(Resource.Id.LineActivity_SwipeRefresh);
            swipeRefresh.Refresh += (s, e) => Refresh();
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

                Switch switchView = FindViewById<Switch>(Resource.Id.LineActivity_RouteSwitch);
                switchView.CheckedChange += (s, e) => Refresh();
            }
            else
            {
                routeSwitchView.Visibility = ViewStates.Invisible;

                ArrayAdapter<string> spinnerAdapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleSpinnerItem, global::Android.Resource.Id.Text1);
                spinnerAdapter.SetDropDownViewResource(global::Android.Resource.Layout.SimpleSpinnerDropDownItem);

                foreach (Route route in line.Routes)
                    spinnerAdapter.Add("De " + route.Steps.First().Stop.Name + " vers " + route.Steps.Last().Stop.Name);

                Spinner spinner = FindViewById<Spinner>(Resource.Id.LineActivity_RouteChoice);
                spinner.Adapter = spinnerAdapter;
                spinner.ItemSelected += (s, e) => Refresh();
            }

            // Show the list
            RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.LineActivity_StopList);
            recyclerView.HasFixedSize = true;
            recyclerView.NestedScrollingEnabled = false;
            recyclerView.SetLayoutManager(new WrapLayoutManager(this));
            recyclerView.AddItemDecoration(new DividerItemDecoration(this, LinearLayoutManager.Vertical));
        }
        protected override void OnPause()
        {
            refreshCancellationTokenSource?.Cancel();

            base.OnPause();
        }
        protected override void OnResume()
        {
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
                    RunOnUiThread(() => OnRefreshedTimes());
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

        private async Task Refresh()
        {
            await Task.Run(() =>
            {
                swipeRefresh.Post(() => swipeRefresh.Refreshing = true);

                if (line.Routes.Length == 2)
                {
                    Switch switchView = FindViewById<Switch>(Resource.Id.LineActivity_RouteSwitch);
                    route = switchView.Checked ? line.Routes[1] : line.Routes[0];
                }
                else
                {
                    Spinner spinner = FindViewById<Spinner>(Resource.Id.LineActivity_RouteChoice);
                    route = line.Routes[spinner.SelectedItemPosition];
                }

                // Online time steps
                try
                {
                    dataSnackbar?.Dismiss();

                    TimeStep[] timeSteps = Database.GetLiveTimeSteps().OrderBy(t => t.Date).ToArray();
                    stepTimes = route.Steps.ToDictionary(s => s, s => timeSteps.Where(t => t.Step.Stop == s.Stop)
                                                                               .Take(3)
                                                                               .ToArray());
                }
                catch (Exception e)
                {
                    // No offline data
                    if (route.TimeTable == null)
                    {
                        stepTimes = null;

                        dataSnackbar = Snackbar.Make(swipeRefresh, "Aucune donnée disponible", Snackbar.LengthIndefinite)
                                               .SetAction("Réessayer", v => Refresh());
                        dataSnackbar.Show();
                    }
                    else
                    {
                        DateTime now = DateTime.Now;
                        stepTimes = route.Steps.ToDictionary(s => s, s => route.TimeTable.GetStepsFromStep(s, now).Take(3).ToArray());

                        dataSnackbar = Snackbar.Make(swipeRefresh, "Données hors-ligne", Snackbar.LengthIndefinite)
                                               .SetAction("Réessayer", v => Refresh());
                        dataSnackbar.Show();
                    }
                }

                RunOnUiThread(OnRefreshed);
            });
        }

        private void OnRefreshed()
        {
            swipeRefresh.Refreshing = false;

            if (route != null)
            {
                RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.LineActivity_StopList);
                recyclerView.SetAdapter(stepTimes == null ? null : new StepsAdapter(stepTimes));
            }
        }
        private void OnRefreshedTimes()
        {
            if (route != null && stepTimes != null)
            {
                RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.LineActivity_StopList);
                recyclerView.GetAdapter()?.NotifyDataSetChanged();
            }
        }
    }
}