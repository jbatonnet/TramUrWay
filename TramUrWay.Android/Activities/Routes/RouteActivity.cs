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
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using Android.Views.InputMethods;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;

namespace TramUrWay.Android
{
    [Activity(Theme = "@style/AppTheme.NoActionBar")]
    public class RouteActivity : BaseActivity, IOnMapReadyCallback
    {
        private Stop from, to;
        private List<RouteSegment> routeSegments;

        private SwipeRefreshLayout swipeRefresh;
        private SupportMapFragment mapFragment;
        private GoogleMap googleMap;

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

                    List<TimeStep> timeSteps = new List<TimeStep>();

                    foreach (JObject timeStepObject in routeSegmentObject["TimeSteps"] as JArray)
                    {
                        int routeId = timeStepObject["Route"].Value<int>();
                        int stopId = timeStepObject["Stop"].Value<int>();
                        DateTime date = timeStepObject["Date"].Value<DateTime>();

                        Route route = line.Routes.First(r => r.Id == routeId);
                        Step step = fromRoute.Steps.First(s => s.Stop.Id == stopId);

                        timeSteps.Add(new TimeStep() { Step = step, Date = date });
                    }

                    routeSegments.Add(new RouteSegment() { Line = line, From = from, DateFrom = fromDate, To = to, DateTo = toDate, TimeSteps = timeSteps.ToArray() });
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
            fromDateView.Text = routeSegments.First().DateFrom.ToString("HH:mm");

            ImageView toIconView = FindViewById<ImageView>(Resource.Id.RouteActivity_ToIcon);
            toIconView.SetImageDrawable(to.Line.GetIconDrawable(this));

            TextView toNameView = FindViewById<TextView>(Resource.Id.RouteActivity_ToName);
            toNameView.Text = to.Name;

            TextView toDateView = FindViewById<TextView>(Resource.Id.RouteActivity_ToDate);
            toDateView.Text = routeSegments.Last().DateTo.ToString("HH:mm");

            // Details view
            RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.RouteActivity_SegmentsList);
            recyclerView.SetLayoutManager(new WrapLayoutManager(recyclerView.Context));
            recyclerView.AddItemDecoration(new DividerItemDecoration(recyclerView.Context, LinearLayoutManager.Vertical, true));
            recyclerView.SetAdapter(new RouteSegmentAdapter(routeSegments.ToArray()));

            // Setup maps fragment
            mapFragment = SupportMapFragment.NewInstance();
            mapFragment.GetMapAsync(this);

            FragmentTransaction fragmentTransaction = SupportFragmentManager.BeginTransaction();
            fragmentTransaction.Replace(Resource.Id.RouteActivity_Map, mapFragment);
            fragmentTransaction.Commit();

            // Refresh widget
            //swipeRefresh = FindViewById<SwipeRefreshLayout>(Resource.Id.RouteActivity_SwipeRefresh);
            //swipeRefresh.Refresh += SwipeRefresh_Refresh;
            //swipeRefresh.SetColorSchemeColors(color.ToArgb());
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
        public void OnMapReady(GoogleMap map)
        {
            googleMap = map;

            // Set initial zoom level
            CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngZoom(new LatLng(43.608340, 3.877086), 12);
            googleMap.MoveCamera(cameraUpdate);

            // Add polylines
            /*foreach (Line line in App.Lines)
            {
                foreach (Route route in line.Routes)
                {
                    PolylineOptions polyline = new PolylineOptions().InvokeWidth(5).Visible(line.Type == LineType.Tram);
                    Color color = Utils.GetColorForLine(this, line);

                    polyline = polyline.InvokeColor(color.ToArgb());

                    foreach (Step step in route.Steps.Take(route.Steps.Length - 1))
                    {
                        foreach (TrajectoryStep trajectoryStep in step.Trajectory)
                        {
                            LatLng latLng = new LatLng(trajectoryStep.Position.Latitude, trajectoryStep.Position.Longitude);
                            polyline = polyline.Add(latLng);
                        }
                    }

                    routeLines.Add(route, googleMap.AddPolyline(polyline));
                }
            }*/

            // Prepare line icons
            /*float density = Resources.DisplayMetrics.Density;
            Drawable drawable = Resources.GetDrawable(Resource.Drawable.train);
            Drawable drawableOutline = Resources.GetDrawable(Resource.Drawable.train_glow);

            foreach (Line line in App.Lines)
            {
                Bitmap bitmap = Bitmap.CreateBitmap((int)(IconSize * density), (int)(IconSize * density), Bitmap.Config.Argb8888);
                Canvas canvas = new Canvas(bitmap);
                Color color = Utils.GetColorForLine(Activity, line);

                drawableOutline.SetBounds(0, 0, (int)(IconSize * density), (int)(IconSize * density));
                drawableOutline.Draw(canvas);

                drawable.SetColorFilter(color, PorterDuff.Mode.SrcIn);
                drawable.SetBounds(0, 0, (int)(IconSize * density), (int)(IconSize * density));
                drawable.Draw(canvas);

                lineIcons[line] = BitmapDescriptorFactory.FromBitmap(bitmap);
            }*/

            // Show stops
            float density = Resources.DisplayMetrics.Density;

            foreach (Line line in App.Lines)
                foreach (Stop stop in line.Stops)
                {
                    //Bitmap bitmap = Bitmap.CreateBitmap((int)(IconSize * density), (int)(IconSize * density), Bitmap.Config.Argb8888);
                    //Canvas canvas = new Canvas(bitmap);
                    //Color color = Utils.GetColorForLine(this, line);
                    

                    googleMap.AddMarker(new MarkerOptions().SetPosition(new LatLng(stop.Position.Latitude, stop.Position.Longitude)));
                }
        }

        private void SwipeRefresh_Refresh(object sender, EventArgs e)
        {

        }
    }
}