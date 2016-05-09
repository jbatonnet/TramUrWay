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

        public event EventHandler QueryRefresh;

        private Route route;
        private Color color;
        private RouteAdapter routeAdapter;
        private TimeStep[] lastTimeSteps;
        private Transport[] lastTransports;

        private SwipeRefreshLayout swipeRefresh;
        private RecyclerView recyclerView;

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