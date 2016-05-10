using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Animation;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.App;
using Android.Utilities;
using Android.Views;

using Activity = Android.App.Activity;

namespace TramUrWay.Android
{
    public class LineMapFragment : TabFragment, IOnMapReadyCallback, GoogleMap.IOnMapLoadedCallback
    {
        public class MarkerAnimator : Java.Lang.Object, ValueAnimator.IAnimatorUpdateListener
        {
            private Activity activity;
            private Marker marker;
            private Transport transport;
            private Action<LatLng> positionUpdater;

            public MarkerAnimator(Activity activity, Marker marker, Transport transport, Action<LatLng> positionUpdater)
            {
                this.activity = activity;
                this.marker = marker;
                this.transport = transport;
                this.positionUpdater = positionUpdater;
            }

            public void OnAnimationUpdate(ValueAnimator animation)
            {
                float progress = transport.Progress + (transport.NextProgress - transport.Progress) * animation.AnimatedFraction;
                int index = transport.Step.Trajectory.TakeWhile(s => s.Index <= progress).Count();

                bool last = index >= transport.Step.Trajectory.Length;
                TrajectoryStep from = transport.Step.Trajectory[index - 1];
                TrajectoryStep to = last ? transport.TimeStep.Step.Trajectory.First() : transport.Step.Trajectory[index];

                progress = (progress - from.Index) / ((last ? 1 : to.Index) - from.Index);
                LatLng position = new LatLng(from.Position.Latitude + (to.Position.Latitude - from.Position.Latitude) * progress, from.Position.Longitude + (to.Position.Longitude - from.Position.Longitude) * progress);

                positionUpdater(position);
            }
        }

        private const int StopIconSize = 10;
        private const int TransportIconSize = 22;

        public override string Title => "Map";

        private Line line;
        private Color color;

        private SupportMapFragment mapFragment;
        private GoogleMap googleMap;
        private Dictionary<Transport, Marker> transportMarkers = new Dictionary<Transport, Marker>();

        private BitmapDescriptor stopBitmapDescriptor;
        private BitmapDescriptor transportBitmapDescriptor;

        public LineMapFragment(Line line, Color color)
        {
            this.line = line;
            this.color = color;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.LineMapFragment, container, false);
        }
        protected override void OnGotFocus()
        {
            base.OnGotFocus();

            // Late load map
            if (mapFragment == null)
            {
                mapFragment = SupportMapFragment.NewInstance();
                mapFragment.GetMapAsync(this);

                FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
                fragmentTransaction.Replace(Resource.Id.MapFragment_Map, mapFragment);
                fragmentTransaction.Commit();
            }
        }
        public void OnMapReady(GoogleMap googleMap)
        {
            this.googleMap = googleMap;

            // Configure map
            googleMap.UiSettings.MyLocationButtonEnabled = true;
            googleMap.UiSettings.MapToolbarEnabled = true;

            // Register events
            mapFragment.View.Post(OnMapLoaded);
            //googleMap.CameraChange += GoogleMap_CameraChange;
            //googleMap.MarkerClick += GoogleMap_MarkerClick;
            //googleMap.MapClick += GoogleMap_MapClick;

            // Preload icons
            Task iconLoader = Task.Run(() =>
            {
                stopBitmapDescriptor = BitmapDescriptorFactory.FromBitmap(Utils.GetStopIconForLine(Activity, line, StopIconSize));
                transportBitmapDescriptor = BitmapDescriptorFactory.FromBitmap(Utils.GetTransportIconForLine(Activity, line, TransportIconSize));
            });

            // Add a polyline between steps
            foreach (Route route in line.Routes)
            {
                PolylineOptions polyline = new PolylineOptions()
                    .InvokeWidth(5)
                    .InvokeZIndex(1)
                    .InvokeColor(color.ToArgb());

                foreach (Step step in route.Steps.Take(route.Steps.Length - 1))
                {
                    foreach (TrajectoryStep trajectoryStep in step.Trajectory)
                    {
                        LatLng latLng = new LatLng(trajectoryStep.Position.Latitude, trajectoryStep.Position.Longitude);
                        polyline = polyline.Add(latLng);
                    }
                }

                googleMap.AddPolyline(polyline);
            }

            // Add a marker for each station
            iconLoader.Wait();

            foreach (Route route in line.Routes)
                foreach (Step step in route.Steps)
                {
                    Position position = step.Trajectory == null ? step.Stop.Position : step.Trajectory[0].Position;
                    LatLng latLng = new LatLng(position.Latitude, position.Longitude);

                    MarkerOptions marker = new MarkerOptions()
                        .Anchor(0.5f, 0.5f)
                        .SetPosition(latLng)
                        .SetIcon(stopBitmapDescriptor);

                    googleMap.AddMarker(marker);
                }
        }
        public void OnMapLoaded()
        {
            // Compute global line bounds to initialize camera
            LatLngBounds.Builder boundsBuilder = new LatLngBounds.Builder();
            foreach (Route route in line.Routes)
                foreach (Step step in route.Steps)
                    boundsBuilder.Include(new LatLng(step.Stop.Position.Latitude, step.Stop.Position.Longitude));

            CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngBounds(boundsBuilder.Build(), 100);
            googleMap.MoveCamera(cameraUpdate);
        }

        public void OnRefreshing()
        {
        }
        public void OnRefreshed(IEnumerable<TimeStep> timeSteps, IEnumerable<Transport> transports)
        {
            List<Transport> unusedTransports = transportMarkers.Keys.ToList();

            foreach (Transport transport in transports)
            {
                Marker marker;

                if (!transportMarkers.TryGetValue(transport, out marker))
                {

                }
                else
                {
                    unusedTransports.Remove(transport);
                }
            }
        }
    }
}