using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.Animation;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Locations;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
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
            private CancellationTokenSource cancellationTokenSource;

            public MarkerAnimator(Activity activity, Marker marker, Transport transport, Action<LatLng> positionUpdater, CancellationTokenSource cancellationTokenSource)
            {
                this.activity = activity;
                this.marker = marker;
                this.transport = transport;
                this.positionUpdater = positionUpdater;
                this.cancellationTokenSource = cancellationTokenSource;
            }

            public void OnAnimationUpdate(ValueAnimator animation)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                    return;

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

        public override string Title => "Map";

        private Line line;
        private Color color;

        private SupportMapFragment mapFragment;
        private GoogleMap googleMap;
        private Dictionary<Transport, Marker> transportMarkers = new Dictionary<Transport, Marker>();
        private Dictionary<Marker, ValueAnimator> markerAnimators = new Dictionary<Marker, ValueAnimator>();
        private Dictionary<string, Step> markerSteps = new Dictionary<string, Step>();
        private CancellationTokenSource refreshCancellationTokenSource = new CancellationTokenSource();
        private Snackbar snackbar;

        private BitmapDescriptor stopBitmapDescriptor;
        private BitmapDescriptor transportBitmapDescriptor;

        private TimeStep[] timeStepsCache;
        private Transport[] transportsCache;
        private bool hasFocus = false;

        public LineMapFragment() { }
        public LineMapFragment(Line line, Color color)
        {
            this.line = line;
            this.color = color;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (savedInstanceState?.ContainsKey("Line") == true)
            {
                int lineId = savedInstanceState.GetInt("Line");
                line = TramUrWayApplication.GetLine(lineId);
            }
            if (savedInstanceState?.ContainsKey("Color") == true)
            { 
                int argb = savedInstanceState.GetInt("Color");
                color = new Color(argb);
            }

            return inflater.Inflate(Resource.Layout.LineMapFragment, container, false);
        }
        public override void OnDestroyView()
        {
            base.OnDestroyView();

            // Clean markers
            foreach (Marker marker in transportMarkers.Values)
                Activity.RunOnUiThread(marker.Remove);

            transportMarkers.Clear();
            markerSteps.Clear();

            // Stop and clean animators
            foreach (ValueAnimator valueAnimator in markerAnimators.Values)
                Activity.RunOnUiThread(valueAnimator.Pause);

            markerAnimators.Clear();

            // Dispose map
            googleMap.Clear();
            googleMap.Dispose();
            mapFragment.Dispose();
            mapFragment = null;
        }
        public override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);

            outState.PutInt("Line", line.Id);
            outState.PutInt("Color", color.ToArgb());
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

            hasFocus = true;

            // Late load map
            if (mapFragment == null)
            {
                mapFragment = ChildFragmentManager.FindFragmentById(Resource.Id.MapFragment_Map) as SupportMapFragment;
                mapFragment.GetMapAsync(this);
            }
        }
        protected override void OnLostFocus()
        {
            base.OnLostFocus();

            hasFocus = false;
        }
        public void OnMapReady(GoogleMap map)
        {
            // Register events
            googleMap = map;
            mapFragment.View.Post(OnMapLoaded);
            googleMap.MyLocationButtonClick += GoogleMap_MyLocationButtonClick;
            googleMap.InfoWindowClick += GoogleMap_InfoWindowClick;

            // Enable my location if user has granted location permission
            if (ContextCompat.CheckSelfPermission(Activity, Manifest.Permission.AccessFineLocation) == Permission.Granted)
                googleMap.MyLocationEnabled = true;

            // Preload icons
            Task iconLoader = Task.Run(() =>
            {
                stopBitmapDescriptor = BitmapDescriptorFactory.FromBitmap(Utils.GetStopIconForLine(Activity, line, TramUrWayApplication.MapStopIconSize));
                transportBitmapDescriptor = BitmapDescriptorFactory.FromBitmap(Utils.GetTransportIconForLine(Activity, line, TramUrWayApplication.MapTransportIconSize));
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

        private void GoogleMap_MyLocationButtonClick(object sender, GoogleMap.MyLocationButtonClickEventArgs e)
        {
            snackbar?.Dismiss();

            float zoom = googleMap.CameraPosition.Zoom;
            Location location = googleMap.MyLocation;

            if (location == null)
            {
                snackbar = Snackbar.Make(View, "Données GPS non disponibles", Snackbar.LengthIndefinite);
                snackbar.Show();

                return;
            }

            CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngZoom(new LatLng(location.Latitude, location.Longitude), Math.Max(zoom, TramUrWayApplication.MyLocationZoom));
            googleMap.AnimateCamera(cameraUpdate);
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

            if (googleMap == null || Activity == null)
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

                ValueAnimator valueAnimator;
                if (markerAnimators.TryGetValue(marker, out valueAnimator))
                {
                    Activity.RunOnUiThread(valueAnimator.Cancel);
                    markerAnimators.Remove(marker);
                }
            }

            RefreshMarkers();
        }

        private void RefreshTimes()
        {
            DateTime now = DateTime.Now;
            transportMarkers.Keys.UpdateProgress(now);

            if (hasFocus)
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
                ValueAnimator valueAnimator;
                if (!markerAnimators.TryGetValue(marker, out valueAnimator))
                {
                    valueAnimator = new ValueAnimator();

                    valueAnimator.AddUpdateListener(new MarkerAnimator(Activity, marker, transport, p => SetMarkerPosition(transport, marker, p), refreshCancellationTokenSource));
                    valueAnimator.SetInterpolator(new LinearInterpolator());
                    valueAnimator.SetFloatValues(0, 1);
                    valueAnimator.SetDuration(1000);

                    markerAnimators.Add(marker, valueAnimator);
                }
                else
                    Activity.RunOnUiThread(valueAnimator.Cancel);

                Activity.RunOnUiThread(valueAnimator.Start);
            }
        }

        private void SetMarkerPosition(Transport transport, Marker marker, LatLng position)
        {
            if (!hasFocus)
                return;

            Activity.RunOnUiThread(() =>
            {
                marker.Position = position;

                //if (marker.Id == selectedMarkerId)
                //    googleMap.AnimateCamera(CameraUpdateFactory.NewLatLng(position));
            });
        }
    }
}