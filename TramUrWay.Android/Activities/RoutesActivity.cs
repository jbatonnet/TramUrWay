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

        protected override void OnCreate(Bundle savedInstanceState)
        {
            SetContentView(Resource.Layout.RoutesActivity);
            NavigationItemId = Resource.Id.SideMenu_Routes;

            base.OnCreate(savedInstanceState);

            Stop[] stops = App.Lines.SelectMany(l => l.Stops).ToArray();
            string[] stopNames = stops.Select(s => s.Name).Distinct().ToArray();

            // Initialize UI
            AutoCompleteTextView fromTextView = FindViewById<AutoCompleteTextView>(Resource.Id.RoutesActivity_From);
            fromTextView.Adapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleDropDownItem1Line, stopNames);

            AutoCompleteTextView toTextView = FindViewById<AutoCompleteTextView>(Resource.Id.RoutesActivity_To);
            toTextView.Adapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleDropDownItem1Line, stopNames);

            RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.RoutesActivity_RoutesList);
            recyclerView.SetLayoutManager(new LinearLayoutManager(recyclerView.Context));
            recyclerView.AddItemDecoration(new DividerItemDecoration(recyclerView.Context, LinearLayoutManager.Vertical));
            recyclerView.SetAdapter(routeSegmentAdapter = new RouteSegmentsAdapter());

            Stop from = App.Lines.SelectMany(l => l.Stops).First(s => s.Name == "Saint-Lazare");
            Stop to = App.Lines.SelectMany(l => l.Stops).First(s => s.Name == "Odysseum"); // "Pierre de Coubertin", "Lattes Centre", "Odysseum"

            Search(from, to, DateConstraint.Now, DateTime.Now);
        }

        private async Task Search(Stop from, Stop to, DateConstraint constraint, DateTime date)
        {
            await Task.Run(() =>
            {
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
                        routeSegments.AddRange(routeSearch.SimulateTimeStepsFrom(route, DateTime.Now, TimeSpan.Zero, tolerance));
                    else if (constraint == DateConstraint.From)
                        routeSegments.AddRange(routeSearch.SimulateTimeStepsFrom(route, date, tolerance, tolerance));
                    else if (constraint == DateConstraint.To)
                        throw new NotImplementedException();
                    else if (constraint == DateConstraint.Last)
                        throw new NotImplementedException();

                    RunOnUiThread(() => routeSegmentAdapter.RouteSegments = routeSegments);
                }
            });
        }
    }
}