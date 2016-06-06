using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Android;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.Locations;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Android.Utilities;
using Android.Views;

using Activity = Android.App.Activity;

namespace TramUrWay.Android
{
    public class NearbyMapFragment : TabFragment, IOnMapReadyCallback
    {
        public override string Title => "Carte";

        private const float DetailsThreshold = 13.5f;
        private const float MyLocationZoom = 14.5f;
        private const float MarkerZoom = 15.5f;

        private View view;
        private SupportMapFragment mapFragment;
        private GoogleMap googleMap;
        private Snackbar snackbar;

        private Dictionary<string, Marker> multiStopGrayMarkers = new Dictionary<string, Marker>();
        private Dictionary<Stop, Marker> multiStopDetailMarkers = new Dictionary<Stop, Marker>();
        private Dictionary<Stop, Marker> singleStopMarkers = new Dictionary<Stop, Marker>();

        private Dictionary<string, Stop[]> stops;
        private Dictionary<string, Stop> markerStops = new Dictionary<string, Stop>();

        public NearbyMapFragment()
        {
            stops = App.Lines.SelectMany(l => l.Stops)
                 .GroupBy(s => Util.Hash(s.Name, s.Line.Id))
                 .Select(g => g.First())
                 .GroupBy(s => s.Name)
                 .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            view = inflater.Inflate(Resource.Layout.NearbyMapFragment, container, false);
            return view;
        }
        protected override void OnGotFocus()
        {
            base.OnGotFocus();

            // Late load map
            if (mapFragment == null)
            {
                mapFragment = ChildFragmentManager.FindFragmentById(Resource.Id.NearbyMapFragment_Map) as SupportMapFragment;
                mapFragment.GetMapAsync(this);
            }
        }
        public async void OnMapReady(GoogleMap map)
        {
            // Register events
            googleMap = map;
            googleMap.CameraChange += GoogleMap_CameraChange;
            googleMap.MyLocationButtonClick += GoogleMap_MyLocationButtonClick;
            googleMap.MarkerClick += GoogleMap_MarkerClick;
            googleMap.InfoWindowClick += GoogleMap_InfoWindowClick;

            // Enable my location if user has granted location permission
            if (ContextCompat.CheckSelfPermission(Activity, Manifest.Permission.AccessFineLocation) == Permission.Granted)
                googleMap.MyLocationEnabled = true;

            await Task.Run(() =>
            {
                Dictionary<string, MarkerOptions> multiStopGrayMarkerOptions = new Dictionary<string, MarkerOptions>();
                Dictionary<Stop, MarkerOptions> multiStopDetailMarkerOptions = new Dictionary<Stop, MarkerOptions>();
                Dictionary<Stop, MarkerOptions> singleStopMarkerOptions = new Dictionary<Stop, MarkerOptions>();

                Bitmap multiStopIcon = Utils.GetStopIconForColor(Activity, Color.Gray, App.MapStopIconSize);
                BitmapDescriptor multiStopIconDescriptor = BitmapDescriptorFactory.FromBitmap(multiStopIcon);

                // Create multistop markers
                foreach (var pair in stops.Where(p => p.Value.Length > 1))
                {
                    MarkerOptions markerOptions = CreateStopMarker(multiStopIconDescriptor, pair.Value);
                    multiStopGrayMarkerOptions.Add(pair.Key, markerOptions);

                    foreach (Stop stop in pair.Value)
                    {
                        Bitmap stopIcon = Utils.GetStopIconForLine(Activity, stop.Line, App.MapStopIconSize);
                        BitmapDescriptor stopIconDescriptor = BitmapDescriptorFactory.FromBitmap(stopIcon);

                        markerOptions = CreateStopMarker(stopIconDescriptor, stop).Visible(false);
                        multiStopDetailMarkerOptions.Add(stop, markerOptions);
                    }
                }

                // Create single stop markers
                foreach (var pair in stops.Where(p => p.Value.Length == 1))
                {
                    Stop stop = pair.Value[0];

                    Bitmap stopIcon = Utils.GetStopIconForLine(Activity, stop.Line, App.MapStopIconSize);
                    BitmapDescriptor stopIconDescriptor = BitmapDescriptorFactory.FromBitmap(stopIcon);

                    MarkerOptions markerOptions = CreateStopMarker(stopIconDescriptor, stop).Visible(stop.Line.Type == LineType.Tram);
                    singleStopMarkerOptions.Add(stop, markerOptions);
                }

                // Add all markers to the map
                Activity.RunOnUiThread(() =>
                {
                    foreach (var pair in multiStopGrayMarkerOptions)
                        multiStopGrayMarkers.Add(pair.Key, googleMap.AddMarker(pair.Value));

                    foreach (var pair in multiStopDetailMarkerOptions)
                    {
                        Marker marker = googleMap.AddMarker(pair.Value);
                        multiStopDetailMarkers.Add(pair.Key, marker);
                        markerStops.Add(marker.Id, pair.Key);
                    }

                    foreach (var pair in singleStopMarkerOptions)
                    {
                        Marker marker = googleMap.AddMarker(pair.Value);
                        singleStopMarkers.Add(pair.Key, marker);
                        markerStops.Add(marker.Id, pair.Key);
                    }
                });
            });
        }
        private MarkerOptions CreateStopMarker(BitmapDescriptor bitmapDescriptor, params Stop[] stops)
        {
            Stop stop = stops[0];

            return new MarkerOptions()
                .SetPosition(new LatLng(stops.Average(s => s.Position.Latitude), stops.Average(s => s.Position.Longitude)))
                .SetIcon(bitmapDescriptor)
                .SetTitle(stop.Name)
                .Anchor(0.5f, 0.5f);
        }

        private void GoogleMap_CameraChange(object sender, GoogleMap.CameraChangeEventArgs e)
        {
            float zoom = googleMap.CameraPosition.Zoom;
            LatLngBounds bounds = googleMap.Projection.VisibleRegion.LatLngBounds;

            // Adjust multistops visibility
            foreach (var pair in multiStopGrayMarkers)
                pair.Value.Visible = zoom < DetailsThreshold;// && bounds.Contains(pair.Value.Position);
            foreach (var pair in multiStopDetailMarkers)
                pair.Value.Visible = zoom >= DetailsThreshold;// && bounds.Contains(pair.Value.Position);

            // Adjust bus stops visibility
            foreach (var pair in singleStopMarkers)
                pair.Value.Visible = pair.Key.Line.Type == LineType.Tram || zoom >= DetailsThreshold;// && bounds.Contains(pair.Value.Position);
        }
        private void GoogleMap_MyLocationButtonClick(object sender, GoogleMap.MyLocationButtonClickEventArgs e)
        {
            snackbar?.Dismiss();

            float zoom = googleMap.CameraPosition.Zoom;
            Location location = googleMap.MyLocation;

            if (location == null)
            {
                snackbar = Snackbar.Make(view, "Données GPS non disponibles", Snackbar.LengthIndefinite);
                snackbar.Show();

                return;
            }

            CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngZoom(new LatLng(location.Latitude, location.Longitude), Math.Max(zoom, MyLocationZoom));
            googleMap.AnimateCamera(cameraUpdate);
        }
        private void GoogleMap_MarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
            float zoom = googleMap.CameraPosition.Zoom;

            CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngZoom(e.Marker.Position, Math.Max(zoom, MarkerZoom));
            googleMap.AnimateCamera(cameraUpdate);

            Stop stop;
            if (markerStops.TryGetValue(e.Marker.Id, out stop))
                e.Marker.ShowInfoWindow();
        }
        private void GoogleMap_InfoWindowClick(object sender, GoogleMap.InfoWindowClickEventArgs e)
        {
            Stop stop;
            if (markerStops.TryGetValue(e.Marker.Id, out stop))
            {
                Intent intent = new Intent(Activity, typeof(StopActivity));

                intent.PutExtra("Stop", stop.Id);
                intent.PutExtra("Line", stop.Line.Id);

                StartActivity(intent);
            }
        }
    }
}