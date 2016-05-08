using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using Android.Utilities;
using Android.Views;

using Java.Lang;

namespace TramUrWay.Android
{
    public class LineMapFragment : TabFragment, IOnMapReadyCallback
    {
        public override string Title => "Map";

        private Line line;
        private Color color;

        private SupportMapFragment mapFragment;
        private GoogleMap googleMap;

        public LineMapFragment(Line line, Color color)
        {
            this.line = line;
            this.color = color;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.LineMapFragment, container, false);

            // Setup maps fragment
            mapFragment = SupportMapFragment.NewInstance();
            mapFragment.GetMapAsync(this);

            FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
            fragmentTransaction.Replace(Resource.Id.MapFragment_Map, mapFragment);
            fragmentTransaction.Commit();
         
            return view;
        }
        public void OnMapReady(GoogleMap googleMap)
        {
            this.googleMap = googleMap;

            // Register events
            //googleMap.CameraChange += GoogleMap_CameraChange;
            //googleMap.MarkerClick += GoogleMap_MarkerClick;
            //googleMap.MapClick += GoogleMap_MapClick;

            // Set initial zoom level
            CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngZoom(new LatLng(43.608340, 3.877086), 12);
            googleMap.MoveCamera(cameraUpdate);

            // Add a polyline between steps
            foreach (Route route in line.Routes)
            {
                PolylineOptions polyline = new PolylineOptions()
                    .InvokeWidth(10)
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

            // Add a point for each station
            /*foreach (Route route in line.Routes)
                foreach (Step step in route.Steps)
                {
                    CircleOptions circle = new CircleOptions()
                        //.InvokeRadius(20)
                        .InvokeFillColor(unchecked((int)0xFFFFFFFF))
                        .InvokeStrokeColor(color.ToArgb());
                        //.InvokeStrokeWidth(5);

                    googleMap.AddCircle(circle);
                }*/
        }
    }
}