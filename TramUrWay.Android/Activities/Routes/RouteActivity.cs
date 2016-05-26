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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace TramUrWay.Android
{
    [Activity(Theme = "@style/AppTheme.NoActionBar")]
    public class RouteActivity : BaseActivity
    {
        private Stop from, to;
        private List<RouteSegment> routeSegments;

        private SwipeRefreshLayout swipeRefresh;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            OnCreate(savedInstanceState, Resource.Layout.RouteActivity);
            
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            
            // Handle bundle parameter
            Bundle extras = Intent.Extras;
            if (extras != null && extras.ContainsKey("RouteSegments"))
            {
                string[] routeSegmentsData = extras.GetStringArray("RouteSegments");

                routeSegments = new List<RouteSegment>();
                foreach (string routeSegmentData in routeSegmentsData)
                {
                    JObject routeSegmentObject = JsonConvert.DeserializeObject(routeSegmentData) as JObject;
                    if (routeSegmentObject == null)
                        throw new Exception("Unable to decode specified route information");

                    Line line = App.GetLine(routeSegmentObject["Line"].Value<int>());

                    int fromRouteId = routeSegmentObject["From.Route"].Value<int>();
                    int fromStopId = routeSegmentObject["From.Stop"].Value<int>();
                    DateTime fromDate = routeSegmentObject["From.Date"].Value<DateTime>();

                    int toRouteId = routeSegmentObject["To.Route"].Value<int>();
                    int toStopId = routeSegmentObject["To.Stop"].Value<int>();
                    DateTime toDate = routeSegmentObject["To.Date"].Value<DateTime>();

                    Route fromRoute = line.Routes.First(r => r.Id == fromRouteId);
                    Step from = fromRoute.Steps.First(s => s.Stop.Id == fromStopId);

                    Route toRoute = line.Routes.First(r => r.Id == toRouteId);
                    Step to = toRoute.Steps.First(s => s.Stop.Id == toStopId);

                    routeSegments.Add(new RouteSegment() { Line = line, From = from, DateFrom = fromDate, To = to, DateTo = toDate });
                }

                from = routeSegments.First().From.Stop;
                to = routeSegments.Last().To.Stop;
            }

            if (from == null || to == null)
                throw new Exception("Could not find specified route information");

            // Initialize UI
            ImageView fromIconView = FindViewById<ImageView>(Resource.Id.RouteActivity_FromIcon);
            fromIconView.SetImageDrawable(from.Line.GetIconDrawable(this));

            TextView fromNameView = FindViewById<TextView>(Resource.Id.RouteActivity_FromName);
            fromNameView.Text = from.Name;

            TextView fromDateView = FindViewById<TextView>(Resource.Id.RouteActivity_FromDate);
            fromDateView.Text = routeSegments.First().DateFrom.ToShortTimeString();

            ImageView toIconView = FindViewById<ImageView>(Resource.Id.RouteActivity_ToIcon);
            toIconView.SetImageDrawable(to.Line.GetIconDrawable(this));

            TextView toNameView = FindViewById<TextView>(Resource.Id.RouteActivity_ToName);
            toNameView.Text = to.Name;

            TextView toDateView = FindViewById<TextView>(Resource.Id.RouteActivity_ToDate);
            toDateView.Text = routeSegments.Last().DateTo.ToShortTimeString();

            // Details view
            RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.RouteActivity_SegmentsList);
            /*recyclerView.SetLayoutManager(new LinearLayoutManager(recyclerView.Context));
            recyclerView.AddItemDecoration(new DividerItemDecoration(recyclerView.Context, LinearLayoutManager.Vertical));
            recyclerView.SetAdapter(routeSegmentAdapter = new RouteSegmentsAdapter());*/

            // Refresh widget
            swipeRefresh = FindViewById<SwipeRefreshLayout>(Resource.Id.RouteActivity_SwipeRefresh);
            swipeRefresh.Refresh += SwipeRefresh_Refresh;
            //swipeRefresh.SetColorSchemeColors(color.ToArgb());
        }

        private void SwipeRefresh_Refresh(object sender, EventArgs e)
        {

        }
    }
}