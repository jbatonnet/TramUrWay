using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
using Android.Util;
using Android.Utilities;
using Android.Views;
using Android.Widget;

using Toolbar = Android.Support.V7.Widget.Toolbar;
using Object = Java.Lang.Object;

namespace TramUrWay.Android
{
    public class RouteFragment : TabFragment
    {
        public override string Title => "Vers " + route.Steps.Last().Stop.Name;

        private Route route;
        private Color color;
        private RouteAdapter routeAdapter;

        private SwipeRefreshLayout swipeRefresh;
        private RecyclerView recyclerView;
        private Snackbar snackbar;

        private CancellationTokenSource refreshCancellationTokenSource = new CancellationTokenSource();

        public RouteFragment(Route route, Color color)
        {
            this.route = route;
            this.color = color;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.RouteFragment, container, false);

            // Refresh widget
            swipeRefresh = view.FindViewById<SwipeRefreshLayout>(Resource.Id.RouteFragment_SwipeRefresh);
            swipeRefresh.Refresh += SwipeRefresh_Refresh;
            swipeRefresh.SetColorSchemeColors(color.ToArgb());

            // Steps list
            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.RouteFragment_StopList);
            recyclerView.Focusable = false;
            recyclerView.HasFixedSize = true;
            recyclerView.SetLayoutManager(new LinearLayoutManager(recyclerView.Context));
            recyclerView.AddItemDecoration(new DividerItemDecoration(recyclerView.Context, LinearLayoutManager.Vertical));
            recyclerView.SetAdapter(routeAdapter = new RouteAdapter(route));

            return view;
        }
        public override void OnPause()
        {
            refreshCancellationTokenSource?.Cancel();
            snackbar?.Dismiss();

            base.OnPause();
        }
        public override void OnResume()
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
                    await Task.Delay(App.GlobalUpdateDelay * 1000);
                }
            });
            Task.Run(async () =>
            {
                CancellationTokenSource cancellationTokenSource = refreshCancellationTokenSource;
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    RefreshIcons();
                    await Task.Delay(App.GlobalUpdateDelay / 4 * 1000);
                }
            });

            swipeRefresh.Post(() => swipeRefresh.Refreshing = true);
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

            Intent intent = new Intent(Activity, typeof(SettingsActivity));
            StartActivity(intent);
        }

        public override async void Refresh()
        {
            await Task.Run(() =>
            {
                TimeStep[] timeSteps;

                // Online time steps
                try
                {
                    if (App.Config.OfflineMode)
                        throw new Exception();

                    swipeRefresh.Post(() => swipeRefresh.Refreshing = true);

                    timeSteps = App.Service.GetLiveTimeSteps().OrderBy(t => t.Date).ToArray();
                    snackbar?.Dismiss();
                }
                catch (Exception e)
                {
                    TimeTable timeTable = route.TimeTable;

                    // Offline data
                    if (timeTable != null)
                    {
                        DateTime now = DateTime.Now;
                        timeSteps = route.Steps.SelectMany(s => timeTable.GetStepsFromStep(s, now).Take(3)).ToArray();

                        if (snackbar == null)
                        {
                            snackbar = Snackbar.Make(swipeRefresh, "Données hors-ligne", Snackbar.LengthIndefinite);
                            if (App.Config.OfflineMode)
                                snackbar = snackbar.SetAction("Activer", Snackbar_Activate);
                            else
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
                            if (App.Config.OfflineMode)
                                snackbar = snackbar.SetAction("Activer", Snackbar_Activate);
                            else
                                snackbar = snackbar.SetAction("Réessayer", Snackbar_Retry);

                            snackbar.Show();
                        }
                    }
                }

                routeAdapter.UpdateSteps(timeSteps);

                swipeRefresh.Post(() => swipeRefresh.Refreshing = false);

                Activity.RunOnUiThread(OnRefreshed);
            });
        }
        private void RefreshIcons()
        {
            routeAdapter.UpdateIcons();
            Activity.RunOnUiThread(OnRefreshed);
        }
        private void OnRefreshed()
        {
            routeAdapter?.NotifyDataSetChanged();
        }
    }
}