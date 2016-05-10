using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.App;
using Android.Utilities;
using Android.Views;

using Java.Lang;

namespace TramUrWay.Android
{
    public class LineMapFragment : TabFragment, IOnMapReadyCallback
    {
        private const int StopIconSize = 10;
        private const int TransportIconSize = 22;

        public override string Title => "Map";

        private Line line;
        private Color color;

        private SupportMapFragment mapFragment;
        private GoogleMap googleMap;

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
            //googleMap.CameraChange += GoogleMap_CameraChange;
            //googleMap.MarkerClick += GoogleMap_MarkerClick;
            //googleMap.MapClick += GoogleMap_MapClick;

            // Preload icons
            Task iconLoader = Task.Run(() =>
            {
                float density = Resources.DisplayMetrics.Density;
                Paint paint = new Paint();

                // Station icon
                int stopIconSize = (int)(StopIconSize * density);
                Bitmap stopBitmap = Bitmap.CreateBitmap(stopIconSize, stopIconSize, Bitmap.Config.Argb8888);
                Canvas stopCanvas = new Canvas(stopBitmap);

                paint.SetARGB(color.A, color.R, color.G, color.B);
                stopCanvas.DrawCircle(stopIconSize / 2, stopIconSize / 2, stopIconSize / 2, paint);

                paint.SetARGB(0xFF, 0xFF, 0xFF, 0xFF);
                stopCanvas.DrawCircle(stopIconSize / 2, stopIconSize / 2, stopIconSize / 2 - (int)(density * 2), paint);

                stopBitmapDescriptor = BitmapDescriptorFactory.FromBitmap(stopBitmap);

                // Line icon
                int transportIconSize = (int)(TransportIconSize * density);
                Drawable transportDrawable = Resources.GetDrawable(Resource.Drawable.train);
                Drawable transportDrawableOutline = Resources.GetDrawable(Resource.Drawable.train_glow);

                Bitmap transportBitmap = Bitmap.CreateBitmap(transportIconSize, transportIconSize, Bitmap.Config.Argb8888);
                Canvas transportCanvas = new Canvas(transportBitmap);

                transportDrawableOutline.SetBounds(0, 0, transportIconSize, transportIconSize);
                transportDrawableOutline.Draw(transportCanvas);

                transportDrawable.SetColorFilter(color, PorterDuff.Mode.SrcIn);
                transportDrawable.SetBounds(0, 0, transportIconSize, transportIconSize);
                transportDrawable.Draw(transportCanvas);

                transportBitmapDescriptor = BitmapDescriptorFactory.FromBitmap(transportBitmap);
            });

            // Compute global line bounds to initialize camera
            LatLngBounds.Builder boundsBuilder = new LatLngBounds.Builder();
            foreach (Route route in line.Routes)
                foreach (Step step in route.Steps)
                    boundsBuilder.Include(new LatLng(step.Stop.Position.Latitude, step.Stop.Position.Longitude));

            CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngBounds(boundsBuilder.Build(), 100);
            googleMap.MoveCamera(cameraUpdate);

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

        public void OnRefreshing()
        {
        }
        public void OnRefreshed(IEnumerable<TimeStep> timeSteps, IEnumerable<Transport> transports)
        {
        }
    }
}