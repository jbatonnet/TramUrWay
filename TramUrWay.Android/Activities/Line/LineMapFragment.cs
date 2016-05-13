using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.Animation;
using Android.Content;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.App;
using Android.Utilities;
using Android.Views;
using Android.Views.Animations;
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
        private Dictionary<string, Step> markerSteps = new Dictionary<string, Step>();
        private CancellationTokenSource refreshCancellationTokenSource = new CancellationTokenSource();

        private BitmapDescriptor stopBitmapDescriptor;
        private BitmapDescriptor transportBitmapDescriptor;

        private TimeStep[] timeStepsCache;
        private Transport[] transportsCache;

        public LineMapFragment(Line line, Color color)
        {
            this.line = line;
            this.color = color;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.LineMapFragment, container, false);
        }
        public override void OnPause()
        {
            refreshCancellationTokenSource?.Cancel();

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
                    RefreshTimes();
                    await Task.Delay(1000);
                }
            });
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
            googleMap.InfoWindowClick += GoogleMap_InfoWindowClick;

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

                    MarkerOptions markerOptions = new MarkerOptions()
                        .Anchor(0.5f, 0.5f)
                        .SetPosition(latLng)
                        .SetTitle(step.Stop.Name)
                        .SetIcon(stopBitmapDescriptor);

                    Marker marker = googleMap.AddMarker(markerOptions);
                    markerSteps.Add(marker.Id, step);
                }

            if (timeStepsCache != null)
                OnRefreshed(timeStepsCache, transportsCache);
        }

        private void GoogleMap_InfoWindowClick(object sender, GoogleMap.InfoWindowClickEventArgs e)
        {
            Step step = markerSteps[e.Marker.Id];

            Intent intent = new Intent(Activity, typeof(StopActivity));
            intent.PutExtra("Stop", step.Stop.Id);
            intent.PutExtra("Line", step.Route.Line.Id);

            Activity.StartActivity(intent);
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
            timeStepsCache = timeSteps.ToArray();
            transportsCache = transports.ToArray();

            if (googleMap == null)
                return;

            List<Transport> unusedTransports = transportMarkers.Keys.ToList();
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);

            foreach (Transport transport in transports)
            {
                Marker marker;

                // Create a new marker if needed
                if (!transportMarkers.TryGetValue(transport, out marker))
                {
                    Activity.RunOnUiThread(() =>
                    {
                        MarkerOptions markerOptions = new MarkerOptions()
                            .Anchor(0.5f, 0.5f)
                            .SetIcon(transportBitmapDescriptor)
                            .SetPosition(new LatLng(0, 0));

                        marker = googleMap.AddMarker(markerOptions);
                        transportMarkers.Add(transport, marker);

                        autoResetEvent.Set();
                    });

                    autoResetEvent.WaitOne();
                }
                else
                    unusedTransports.Remove(transport);
            }

            // Clean old markers
            foreach (Transport transport in unusedTransports)
            {
                Marker marker = transportMarkers[transport];
                transportMarkers.Remove(transport);

                Activity.RunOnUiThread(marker.Remove);
            }

            RefreshMarkers();
        }

        private void RefreshTimes()
        {
            DateTime now = DateTime.Now;
            transportMarkers.Keys.UpdateProgress(now);

            RefreshMarkers();
        }
        private void RefreshMarkers()
        {
            // Update each marker position
            foreach (var pair in transportMarkers)
            {
                Transport transport = pair.Key;
                Marker marker = pair.Value;

                // Compute quick position
                Position quickFrom = transport.Step.Stop.Position;
                Position quickTo = transport.TimeStep.Step.Stop.Position;
                LatLng quickPosition = new LatLng(quickFrom.Latitude + (quickTo.Latitude - quickFrom.Latitude) * transport.Progress, quickFrom.Longitude + (quickTo.Longitude - quickFrom.Longitude) * transport.Progress);

                // Update marker
                ValueAnimator valueAnimator = new ValueAnimator();
                valueAnimator.AddUpdateListener(new MarkerAnimator(Activity, marker, transport, p => SetMarkerPosition(transport, marker, p)));
                valueAnimator.SetFloatValues(0, 1);
                valueAnimator.SetInterpolator(new LinearInterpolator());
                valueAnimator.SetDuration(1000);
                Activity.RunOnUiThread(valueAnimator.Start);
            }
        }

        private void SetMarkerPosition(Transport transport, Marker marker, LatLng position)
        {
            Activity.RunOnUiThread(() =>
            {
                marker.Position = position;

                //if (marker.Id == selectedMarkerId)
                //    googleMap.AnimateCamera(CameraUpdateFactory.NewLatLng(position));
            });
        }
    }
}