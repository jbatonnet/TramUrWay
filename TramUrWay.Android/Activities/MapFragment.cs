using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Gms.Maps;
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
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

using System.Threading.Tasks;
using System.Threading;
using Android.Gms.Maps.Model;
using Android.Animation;
using Android.Views.Animations;

namespace TramUrWay.Android
{
    public class MapFragment : Fragment, IOnMapReadyCallback
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

        private const int IconSize = 22;
        private const float BusZoomLimit = 100; //13;
        private const float AnimationZoomLimit = 14.2f;

        private CancellationTokenSource refreshCancellationTokenSource = new CancellationTokenSource();
        private SupportMapFragment mapFragment;
        private GoogleMap googleMap;
        private LatLngBounds cameraBounds;
        private CameraPosition cameraPosition;

        private string selectedMarkerId;

        private Dictionary<Route, Polyline> routeLines = new Dictionary<Route, Polyline>();
        private Dictionary<Step, Marker> stepMarkers = new Dictionary<Step, Marker>();
        private Dictionary<Transport, Marker> transportMarkers = new Dictionary<Transport, Marker>();
        private Dictionary<Marker, bool> markersVisibility = new Dictionary<Marker, bool>();
        private Dictionary<Line, BitmapDescriptor> lineIcons = new Dictionary<Line, BitmapDescriptor>();

        private List<TimeStep> timeSteps = new List<TimeStep>();

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.MapFragment, container, false);

            // Setup maps fragment
            mapFragment = SupportMapFragment.NewInstance();
            mapFragment.GetMapAsync(this);

            //FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
            //fragmentTransaction.Replace(Resource.Id.MapFragment_Map, mapFragment);
            //fragmentTransaction.Commit();

            return view;
        }
        public void OnMapReady(GoogleMap googleMap)
        {
            this.googleMap = googleMap;

            // Register events
            googleMap.CameraChange += GoogleMap_CameraChange;
            googleMap.MarkerClick += GoogleMap_MarkerClick;
            googleMap.MapClick += GoogleMap_MapClick;

            // Set initial zoom level
            CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngZoom(new LatLng(43.608340, 3.877086), 12);
            googleMap.MoveCamera(cameraUpdate);

            // Add polylines
            foreach (Line line in TramUrWayApplication.Lines)
            {
                foreach (Route route in line.Routes)
                {
                    PolylineOptions polyline = new PolylineOptions().InvokeWidth(5).Visible(line.Type == LineType.Tram);
                    Color color = Utils.GetColorForLine(Activity, line);

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
            }

            // Prepare line icons
            float density = Resources.DisplayMetrics.Density;
            Drawable drawable = Resources.GetDrawable(Resource.Drawable.train);
            Drawable drawableOutline = Resources.GetDrawable(Resource.Drawable.train_glow);

            foreach (Line line in TramUrWayApplication.Lines)
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
            }
        }

        private void GoogleMap_CameraChange(object sender, GoogleMap.CameraChangeEventArgs e)
        {
            // Update bounds
            cameraBounds = googleMap.Projection.VisibleRegion.LatLngBounds;
            cameraPosition = googleMap.CameraPosition;

            // Update lines visibility
            bool showBuses = e.Position.Zoom >= BusZoomLimit;
            foreach (Line line in TramUrWayApplication.Lines)
                foreach (Route route in line.Routes)
                {
                    Polyline polyline;
                    if (!routeLines.TryGetValue(route, out polyline))
                        continue;

                    if (line.Type == LineType.Bus)
                        polyline.Visible = showBuses;
                }

            // Update markers visibility
            foreach (var pair in transportMarkers)
            {
                bool visible = pair.Key.Route.Line.Type == LineType.Tram || showBuses;
                if (visible)
                    visible = cameraBounds.Contains(pair.Value.Position);

                pair.Value.Visible = visible;
                markersVisibility[pair.Value] = visible;
            }
        }
        private void GoogleMap_MarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
            selectedMarkerId = e.Marker.Id;
        }
        private void GoogleMap_MapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            selectedMarkerId = null;
        }

        public override void OnPause()
        {
            refreshCancellationTokenSource?.Cancel();

            base.OnPause();
        }
        public override void OnResume()
        {
            // Cancel refresh tasks
            refreshCancellationTokenSource?.Cancel();
            refreshCancellationTokenSource = new CancellationTokenSource();

            // Run new refresh tasks
            Task.Run(() =>
            {
                CancellationTokenSource cancellationTokenSource = refreshCancellationTokenSource;
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    UpdateTimeSteps();
                    Thread.Sleep(TramUrWayApplication.GlobalUpdateDelay / 2 * 1000);
                }
            });
            Task.Run(() =>
            {
                CancellationTokenSource cancellationTokenSource = refreshCancellationTokenSource;
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    UpdatePositions();
                    Thread.Sleep(1000);
                }
            });

            base.OnResume();
        }

        private void UpdateTimeSteps()
        {
            TimeStep[] newTimeSteps = null;

            // Online time steps
            try
            {
                if (TramUrWayApplication.Config.OfflineMode)
                    throw new Exception();

                newTimeSteps = new TimeStep[0];// App.Service.GetLiveTimeSteps().Where(t => t.Step.Route.Line.Id < 6).OrderBy(t => t.Date).ToArray();
            }

            // Offline time steps
            catch (Exception e)
            {
                DateTime now = DateTime.Now;

                newTimeSteps = TramUrWayApplication.Lines.Where(l => l.Id < 6)
                                        .SelectMany(l => l.Routes)
                                        .SelectMany(r => r.Steps)
                                        .Select(s => s.Route.TimeTable?.GetStepsFromStep(s, now, false)?.Take(3))
                                        .Where(s => s != null)
                                        .SelectMany(s => s)
                                        .ToArray();
            }

            lock (timeSteps)
            {
                timeSteps.Clear();
                timeSteps.AddRange(newTimeSteps);
            }
        }
        private void UpdatePositions()
        {
            TimeStep[] currentTimeSteps;
            DateTime now = DateTime.Now;
            
            // Take time steps snapshot
            lock (timeSteps)
                currentTimeSteps = timeSteps.ToArray();

            // Sort time steps by step
            Dictionary<Step, TimeStep> steps = currentTimeSteps.GroupBy(s => s.Step)
                                                               .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Date).First());

            // For each route, find transport positions
            List<Transport> transports = new List<Transport>();

            foreach (Line line in TramUrWayApplication.Lines)
                foreach (Route route in line.Routes)
                {
                    for (int i = 0; i < route.Steps.Length - 1; i++)
                    {
                        Step step = route.Steps[i];
                        Step nextStep = route.Steps[i + 1];

                        TimeStep timeStep, nextTimeStep;
                        if (!steps.TryGetValue(nextStep, out nextTimeStep))
                            continue;
                        steps.TryGetValue(step, out timeStep);

                        if (timeStep != null && timeStep.Date < nextTimeStep.Date)
                            continue;

                        TimeSpan diff = nextTimeStep.Date - now;
                        TimeSpan duration = step.Duration ?? TimeSpan.Zero;

                        if (duration == TimeSpan.Zero)
                            duration = TimeSpan.FromMinutes(2);

                        float progress = (float)(1 - Math.Min(diff.TotalMinutes, duration.TotalMinutes) / duration.TotalMinutes);
                        if (progress < 0) progress = 0;
                        if (progress > 1) progress = 1;

                        float nextProgress = (float)(1 - Math.Min(diff.Subtract(TimeSpan.FromSeconds(1)).TotalMinutes, duration.TotalMinutes) / duration.TotalMinutes);
                        if (nextProgress < 0) nextProgress = 0;
                        if (nextProgress > 1) nextProgress = 1;

                        Transport transport = new Transport()
                        {
                            Route = route,
                            Step = step,
                            TimeStep = nextTimeStep,
                            Progress = step.Speed.Evaluate(progress),
                            NextProgress = step.Speed.Evaluate(nextProgress)
                        };

                        transports.Add(transport);
                    }
                }

            // Sort markers
            Dictionary<Step, Marker> stepMarkers = transportMarkers.ToDictionary(p => p.Key.Step, p => p.Value);
            List<Marker> unusedMarkers = stepMarkers.Where(p => !transports.Any(t => t.Step == p.Key)).Select(p => p.Value).ToList();
            List<Transport> missingTransports = transports.Where(t => !stepMarkers.Keys.Contains(t.Step)).ToList();
            Dictionary<Step, Marker> reusableMarkers = stepMarkers.Where(p => transports.Any(t => t.Step == p.Key) && !missingTransports.Any(t => t.Step == p.Key)).ToDictionary();

            // Adjust markers diff
            while (unusedMarkers.Count > missingTransports.Count)
            {
                Marker marker = unusedMarkers.Last();
                unusedMarkers.RemoveAt(unusedMarkers.Count - 1);
                Activity.RunOnUiThread(marker.Remove);
            }
            while (missingTransports.Count > unusedMarkers.Count)
            {
                ManualResetEvent resetEvent = new ManualResetEvent(false);

                Activity.RunOnUiThread(() =>
                {
                    MarkerOptions markerOptions = new MarkerOptions()
                        .Anchor(0.5f, 0.5f)
                        .SetPosition(new LatLng(0, 0));

                    Marker marker = googleMap.AddMarker(markerOptions);
                    unusedMarkers.Add(marker);

                    resetEvent.Set();
                });

                resetEvent.WaitOne();
            }

            // Use existing markers to match current transports
            transportMarkers = reusableMarkers.Select(p => new KeyValuePair<Transport, Marker>(transports.First(t => t.Step == p.Key), p.Value))
                                              .Concat(missingTransports.Zip(unusedMarkers, (t, m) => new KeyValuePair<Transport, Marker>(t, m)))
                                              .ToDictionary();

            foreach (var pair in transportMarkers)
            {
                Transport transport = pair.Key;
                Marker marker = pair.Value;

                // Compute quick position
                Position quickFrom = transport.Step.Stop.Position;
                Position quickTo = transport.TimeStep.Step.Stop.Position;
                LatLng quickPosition = new LatLng(quickFrom.Latitude + (quickTo.Latitude - quickFrom.Latitude) * transport.Progress, quickFrom.Longitude + (quickTo.Longitude - quickFrom.Longitude) * transport.Progress);

                // Update marker
                Activity.RunOnUiThread(() => marker.SetIcon(lineIcons[transport.Route.Line]));
                bool visible;
                if (markersVisibility.TryGetValue(marker, out visible) && !visible)
                    continue;

                // Use animation only if zoomed enough
                if (cameraPosition.Zoom >= AnimationZoomLimit)
                {
                    ValueAnimator valueAnimator = new ValueAnimator();
                    valueAnimator.AddUpdateListener(new MarkerAnimator(Activity, marker, transport, p => SetMarkerPosition(transport, marker, p)));
                    valueAnimator.SetFloatValues(0, 1); // Ignored.
                    valueAnimator.SetInterpolator(new LinearInterpolator());
                    valueAnimator.SetDuration(1000);
                    Activity.RunOnUiThread(valueAnimator.Start);
                }
                else
                {
                    float progress = transport.Progress;
                    int index = transport.Step.Trajectory.TakeWhile(s => s.Index <= progress).Count();

                    bool last = index >= transport.Step.Trajectory.Length;
                    TrajectoryStep from = transport.Step.Trajectory[index - 1];
                    TrajectoryStep to = last ? transport.TimeStep.Step.Trajectory.First() : transport.Step.Trajectory[index];

                    progress = (progress - from.Index) / ((last ? 1 : to.Index) - from.Index);
                    LatLng position = new LatLng(from.Position.Latitude + (to.Position.Latitude - from.Position.Latitude) * transport.Progress, from.Position.Longitude + (to.Position.Longitude - from.Position.Longitude) * transport.Progress);

                    SetMarkerPosition(transport, marker, position);
                }

                // Follow selected transport
                Activity.RunOnUiThread(() =>
                {
                    if (marker.Id == selectedMarkerId)
                        googleMap.AnimateCamera(CameraUpdateFactory.NewLatLng(quickPosition));
                });
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