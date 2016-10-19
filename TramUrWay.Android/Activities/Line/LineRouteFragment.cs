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
    public class LineRouteFragment : TabFragment
    {
        public override string Title => route.Name ?? "Vers " + route.Steps.Last().Stop.Name;

        public event EventHandler QueryRefresh;

        private Route route;
        private Color color;
        private RouteAdapter routeAdapter;
        private TimeStep[] lastTimeSteps;
        private Transport[] lastTransports;

        private SwipeRefreshLayout swipeRefresh;
        private RecyclerView recyclerView;

        public LineRouteFragment() { }
        public LineRouteFragment(Route route, Color color)
        {
            this.route = route;
            this.color = color;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (savedInstanceState?.ContainsKey("Line") == true && savedInstanceState?.ContainsKey("Route") == true)
            {
                int lineId = savedInstanceState.GetInt("Line");
                Line line = TramUrWayApplication.GetLine(lineId);

                int routeId = savedInstanceState.GetInt("Route");
                route = line.Routes.FirstOrDefault(r => r.Id == routeId);
            }
            if (savedInstanceState?.ContainsKey("Color") == true)
            {
                int argb = savedInstanceState.GetInt("Color");
                color = new Color(argb);
            }

            View view = inflater.Inflate(Resource.Layout.RouteFragment, container, false);

            // Refresh widget
            swipeRefresh = view.FindViewById<SwipeRefreshLayout>(Resource.Id.RouteFragment_SwipeRefresh);
            swipeRefresh.Refresh += (s, e) => QueryRefresh?.Invoke(s, e);
            swipeRefresh.SetColorSchemeColors(color.ToArgb());

            // Steps list
            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.RouteFragment_StopList);
            recyclerView.Focusable = false;
            recyclerView.HasFixedSize = true;
            recyclerView.SetLayoutManager(new LinearLayoutManager(recyclerView.Context));
            recyclerView.AddItemDecoration(new DividerItemDecoration(recyclerView.Context, LinearLayoutManager.Vertical));
            recyclerView.SetAdapter(routeAdapter = new RouteAdapter(route));

            if (lastTimeSteps != null)
                routeAdapter.Update(lastTimeSteps, lastTransports);

            return view;
        }
        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            outState.PutInt("Line", route.Line.Id);
            outState.PutInt("Route", route.Id);
            outState.PutInt("Color", color.ToArgb());
        }

        public void OnRefreshing()
        {
            swipeRefresh?.Post(() => swipeRefresh.Refreshing = true);
        }
        public void OnRefreshed(IEnumerable<TimeStep> timeSteps, IEnumerable<Transport> transports)
        {
            lastTimeSteps = timeSteps.Where(t => t.Step.Route == route).ToArray();
            lastTransports = transports.Where(t => t.TimeStep.Step.Route == route).ToArray();

            routeAdapter?.Update(lastTimeSteps, lastTransports);
            swipeRefresh?.Post(() => swipeRefresh.Refreshing = false);
        }
    }
}