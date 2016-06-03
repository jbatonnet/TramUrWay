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
using Android.Utilities;
using Android.Views;
using Android.Widget;

using Toolbar = Android.Support.V7.Widget.Toolbar;
using PopupMenu = Android.Support.V7.Widget.PopupMenu;
using Android.Views.InputMethods;

namespace TramUrWay.Android
{
    [Activity(Theme = "@style/AppTheme.NoActionBar")]
    public class RoutesActivity : NavigationActivity
    {
        public enum DateConstraint
        {
            Now,
            From,
            To,
            Last
        }

        private RouteSegmentsAdapter routeSegmentAdapter;
        private List<RouteSegment[]> routeSegments = new List<RouteSegment[]>();

        private TextInputLayout fromLayout, toLayout;
        private AutoCompleteTextView fromTextView, toTextView;
        private SwipeRefreshLayout swipeRefresh;
        private IMenuItem searchMenuItem;
        private View noResultsView;
        private RecyclerView recyclerView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.RoutesActivity);
            NavigationItemId = Resource.Id.SideMenu_Routes;

            OnPostCreate();

            Stop[] stops = App.Lines.SelectMany(l => l.Stops).ToArray();
            string[] stopNames = stops.Select(s => s.Name).Distinct().ToArray();

            // Initialize UI
            fromLayout = FindViewById<TextInputLayout>(Resource.Id.RoutesActivity_FromLayout);
            fromTextView = FindViewById<AutoCompleteTextView>(Resource.Id.RoutesActivity_From);
            fromTextView.Adapter = new ArrayAdapter<string>(this, Resource.Layout.RouteAutocompleteItem, stopNames);
            fromTextView.TextChanged += TextView_TextChanged;

            View fromButton = FindViewById(Resource.Id.RoutesActivity_FromButton);
            fromButton.Click += FromButton_Click;

            toLayout = FindViewById<TextInputLayout>(Resource.Id.RoutesActivity_ToLayout);
            toTextView = FindViewById<AutoCompleteTextView>(Resource.Id.RoutesActivity_To);
            toTextView.Adapter = new ArrayAdapter<string>(this, Resource.Layout.RouteAutocompleteItem, stopNames);
            toTextView.TextChanged += TextView_TextChanged;

            View toButton = FindViewById(Resource.Id.RoutesActivity_ToButton);
            toButton.Click += ToButton_Click;

            View dateLayout = FindViewById(Resource.Id.RoutesActivity_DateLayout);
            dateLayout.Click += DateLayout_Click;

            // Handle bundle parameter
            Bundle extras = Intent.Extras;
            if (extras != null && extras.ContainsKey("From"))
            {
                int stopId = extras.GetInt("From");
                Stop stop = App.GetStop(stopId);
                if (stop != null)
                    fromTextView.Text = stop.Name;
            }
#if DEBUG
            else
                fromTextView.Text = "Saint-Lazare";
#endif

            if (extras != null && extras.ContainsKey("To"))
            {
                int stopId = extras.GetInt("To");
                Stop stop = App.GetStop(stopId);
                if (stop != null)
                    toTextView.Text = stop.Name;
            }
#if DEBUG
            else
                toTextView.Text = "Odysseum";
#endif

            recyclerView = FindViewById<RecyclerView>(Resource.Id.RoutesActivity_RoutesList);
            recyclerView.SetLayoutManager(new LinearLayoutManager(recyclerView.Context));
            recyclerView.AddItemDecoration(new DividerItemDecoration(recyclerView.Context, LinearLayoutManager.Vertical));
            recyclerView.SetAdapter(routeSegmentAdapter = new RouteSegmentsAdapter());

            // Refresh widget
            swipeRefresh = FindViewById<SwipeRefreshLayout>(Resource.Id.RoutesActivity_SwipeRefresh);
            swipeRefresh.Refresh += SwipeRefresh_Refresh;
            swipeRefresh.SetColorSchemeColors(Resources.GetColor(Resource.Color.colorAccent).ToArgb());

            noResultsView = FindViewById(Resource.Id.RoutesActivity_NoResults);
            noResultsView.Visibility = ViewStates.Gone;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.RoutesMenu, menu);

            for (int i = 0; i < menu.Size(); i++)
            {
                IMenuItem menuItem = menu.GetItem(i);

                if (menuItem.ItemId == Resource.Id.RoutesMenu_Search)
                    searchMenuItem = menuItem;
            }

            return base.OnCreateOptionsMenu(menu);
        }
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.RoutesMenu_Search:
                    TriggerSearch();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void FromButton_Click(object sender, EventArgs e)
        {
            PopupMenu menu = new PopupMenu(this, sender as View);
            menu.MenuItemClick += (s, a) =>
            {
                if (a.Item.ItemId == 0)
                    throw new NotImplementedException();
                else if (a.Item.ItemId == 1)
                {
                    fromTextView.RequestFocus();
                    fromTextView.ShowDropDown();

                    fromTextView.PostDelayed(() =>
                    {
                        InputMethodManager inputMethodManager = GetSystemService(Context.InputMethodService) as InputMethodManager;
                        inputMethodManager.ShowSoftInput(fromTextView, ShowFlags.Forced);
                    }, 250);
                }
                else
                {
                    Stop stop = App.GetStop(a.Item.ItemId);
                    fromTextView.Text = stop.Name;
                }
            };

            // Auto: based on current location and favorites
            //menu.Menu.Add(1, 0, 1, "Automatique").SetIcon(Resource.Drawable.ic_place);

            // Favorite stops
            foreach (Stop stop in App.Config.FavoriteStops)
                menu.Menu.Add(1, stop.Id, 2, stop.Name);

            // Other: focus the search box and trigger autocomplete
            menu.Menu.Add(1, 1, 3, "Autre ...");

            menu.Show();
        }
        private void ToButton_Click(object sender, EventArgs e)
        {
            PopupMenu menu = new PopupMenu(this, sender as View);
            menu.MenuItemClick += (s, a) =>
            {
                if (a.Item.ItemId == 1)
                {
                    toTextView.RequestFocus();
                    toTextView.ShowDropDown();

                    toTextView.PostDelayed(() =>
                    {
                        InputMethodManager inputMethodManager = GetSystemService(Context.InputMethodService) as InputMethodManager;
                        inputMethodManager.ShowSoftInput(toTextView, ShowFlags.Forced);
                    }, 250);
                }
                else
                {
                    Stop stop = App.GetStop(a.Item.ItemId);
                    toTextView.Text = stop.Name;
                }
            };

            // Favorite stops
            foreach (Stop stop in App.Config.FavoriteStops)
                menu.Menu.Add(1, stop.Id, 2, stop.Name);

            // Other: focus the search box and trigger autocomplete
            menu.Menu.Add(1, 1, 3, "Autre ...");

            menu.Show();
        }
        private void TextView_TextChanged(object sender, global::Android.Text.TextChangedEventArgs e)
        {
            fromLayout.Error = toLayout.Error = null;
            fromLayout.ErrorEnabled = toLayout.ErrorEnabled = false;
        }
        private void DateLayout_Click(object sender, EventArgs e)
        {
            /*new global::Android.Support.V7.App.AlertDialog.Builder(this)
                .SetView()
                .Show();*/
        }
        private void SwipeRefresh_Refresh(object sender, EventArgs e)
        {
            TriggerSearch();
        }

        private void TriggerSearch()
        {
            if (string.IsNullOrWhiteSpace(fromTextView.Text))
            {
                fromLayout.Error = "Spécifiez une station de départ";
                swipeRefresh?.Post(() => swipeRefresh.Refreshing = false);
                return;
            }

            Stop from = App.Lines.SelectMany(l => l.Stops).FirstOrDefault(s => s.Name == fromTextView.Text);
            if (from == null)
            {
                fromLayout.Error = "La station spécifiée n'existe pas";
                swipeRefresh?.Post(() => swipeRefresh.Refreshing = false);
                return;
            }

            if (string.IsNullOrWhiteSpace(toTextView.Text))
            {
                toLayout.Error = "Spécifiez une station de destination";
                swipeRefresh?.Post(() => swipeRefresh.Refreshing = false);
                return;
            }

            Stop to = App.Lines.SelectMany(l => l.Stops).FirstOrDefault(s => s.Name == toTextView.Text);
            if (to == null)
            {
                toLayout.Error = "La station spécifiée n'existe pas";
                swipeRefresh?.Post(() => swipeRefresh.Refreshing = false);
                return;
            }

            if (from == to)
            {
                toLayout.Error = "Spécifiez une station différente de celle de départ";
                swipeRefresh?.Post(() => swipeRefresh.Refreshing = false);
                return;
            }

            defaultFocus.RequestFocus();
            fromTextView.PostDelayed(() =>
            {
                InputMethodManager inputMethodManager = GetSystemService(Context.InputMethodService) as InputMethodManager;
                inputMethodManager.HideSoftInputFromWindow(fromTextView.WindowToken, HideSoftInputFlags.None);
            }, 250);

            routeSegments.Clear();
            routeSegmentAdapter.RouteSegments = Enumerable.Empty<RouteSegment[]>();

            View initialHintView = FindViewById(Resource.Id.RoutesActivity_InitialHint);
            initialHintView.Visibility = ViewStates.Gone;

#if DEBUG
            Search(from, to, DateConstraint.From, new DateTime(2016, 05, 27, 16, 24, 00));
            //Search(from, to, DateConstraint.Now, DateTime.Now);
#else
            Search(from, to, DateConstraint.Now, DateTime.Now);
#endif
        }
        private async Task Search(Stop from, Stop to, DateConstraint constraint, DateTime date)
        {
            swipeRefresh?.Post(() => swipeRefresh.Refreshing = true);

            await Task.Run(() =>
            {
                routeSegments.Clear();

                // Build a new route searcher
                RouteSearch routeSearch = new RouteSearch();
                routeSearch.Settings.AllowWalkLinks = false;
                routeSearch.Prepare(App.Lines);

                // Start enumeration
                DateTime end = DateTime.Now + TimeSpan.FromSeconds(5);

                IEnumerable<RouteLink[]> routesEnumerable = routeSearch.FindRoutes(from, to);
                IEnumerator<RouteLink[]> routesEnumerator = routesEnumerable.GetEnumerator();

                while (true)
                {
                    Task<bool> moveNextTask = Task.Run(() => routesEnumerator.MoveNext());

                    // Exit if timed out
                    TimeSpan timeout = end - DateTime.Now;
                    if (!moveNextTask.Wait(timeout))
                        break;

                    // Exit if enumeration finished
                    if (moveNextTask.Result == false)
                        break;

                    RouteLink[] route = routesEnumerator.Current;
                    TimeSpan tolerance = TimeSpan.FromMinutes(15);

                    // We found a route, try to find times
                    if (constraint == DateConstraint.Now)
                        routeSegments.AddRange(routeSearch.SimulateTimeStepsFrom(route, date, TimeSpan.Zero, tolerance));
                    else if (constraint == DateConstraint.From)
                        routeSegments.AddRange(routeSearch.SimulateTimeStepsFrom(route, date, tolerance, tolerance));
                    else if (constraint == DateConstraint.To)
                        throw new NotImplementedException();
                    else if (constraint == DateConstraint.Last)
                        throw new NotImplementedException();

                    // Filter out too long times
                    TimeSpan maxTime = TimeSpan.FromMinutes(0.05 * (to.Position - from.Position));

                    routeSegments.RemoveAll(r => r.Last().DateTo - r.First().DateFrom > maxTime);
                    routeSegments.Sort((r1, r2) => (int)(ComputeRouteWeight(constraint, date, r1) - ComputeRouteWeight(constraint, date, r2)));

                    // Group routes by from and to time steps
                    RouteSegment[][] routeSegmentsCopy = routeSegments.GroupBy(r => GetRouteHash(r)).Select(g => g.First()).ToArray();
                    RunOnUiThread(() => routeSegmentAdapter.RouteSegments = routeSegmentsCopy);
                }

                RunOnUiThread(() =>
                {
                    noResultsView.Visibility = routeSegments.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
                    recyclerView.Visibility = routeSegments.Count > 0 ? ViewStates.Visible : ViewStates.Gone;
                });

                swipeRefresh?.Post(() => swipeRefresh.Refreshing = false);
            });
        }

        private float ComputeRouteWeight(DateConstraint constraint, DateTime date, RouteSegment[] route)
        {
            const float maxPenality = 1.2f;

            float duration = (float)(route.Last().DateTo - route.First().DateFrom).TotalSeconds;

            // Changes count
            float changes = MathUtil.Clamp(MathUtil.Map(route.Length, 0, 4, 1, maxPenality), 1, maxPenality);

            // Priority to use trams
            float buses = MathUtil.Clamp(MathUtil.Map(route.Count(s => s.Line.Type != LineType.Tram), 0, 4, 1, maxPenality), 1, maxPenality);

            // Changes duration
            float changeDuration = Enumerable.Range(1, route.Length - 1).Select(i => route[i].DateFrom - route[i - 1].DateTo).Sum(d => (float)d.TotalSeconds);

            // Diff from asked
            float diff = 1;
            switch (constraint)
            {
                case DateConstraint.Now: diff = (float)MathUtil.Clamp(MathUtil.Map((route.First().DateFrom - date).TotalMinutes, 0, 15, 1, maxPenality), 1, maxPenality); break;
                case DateConstraint.From: diff = (float)MathUtil.Clamp(MathUtil.Map(Math.Abs((route.First().DateFrom - date).TotalMinutes), 0, 15, 1, maxPenality), 1, maxPenality); break;
                case DateConstraint.To: diff = (float)MathUtil.Clamp(MathUtil.Map(Math.Abs((route.Last().DateTo - date).TotalMinutes), 0, 15, 1, maxPenality), 1, maxPenality); break;
            }

            return (duration + changeDuration) * changes * buses * diff;
        }
        private int GetRouteHash(RouteSegment[] route)
        {
            RouteSegment firstSegment = route.First();
            RouteSegment lastSegment = route.Last();

            return firstSegment.Line.Id.GetHashCode() ^ firstSegment.DateFrom.Ticks.GetHashCode() ^ lastSegment.Line.Id.GetHashCode() ^ lastSegment.DateTo.Ticks.GetHashCode();
        }
    }
}