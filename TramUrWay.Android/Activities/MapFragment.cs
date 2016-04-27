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
using System.Threading.Tasks;
using System.Threading;
using Android.Gms.Maps.Model;

namespace TramUrWay.Android
{
    public class MapFragment : Fragment, IOnMapReadyCallback
    {
        global::Android.Gms.Maps.MapFragment mapFragment;

        List<Polyline> trajectories = new List<Polyline>();
        List<Circle> tramCircles = new List<Circle>();
        List<Circle> busCircles = new List<Circle>();

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.MapFragment, container, false);

            mapFragment = global::Android.Gms.Maps.MapFragment.NewInstance();
            mapFragment.GetMapAsync(this);

            FragmentTransaction fragmentTransaction = FragmentManager.BeginTransaction();
            fragmentTransaction.Replace(Resource.Id.MapFragment_Map, mapFragment);
            fragmentTransaction.Commit();

            return view;
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            // Set initial zoom level
            CameraUpdate cameraUpdate = CameraUpdateFactory.NewLatLngZoom(new LatLng(43.608340, 3.877086), 12);
            googleMap.MoveCamera(cameraUpdate);

            // Add polylines
            foreach (Line line in App.Lines)
            {
                foreach (Route route in line.Routes)
                {
                    PolylineOptions polyline = new PolylineOptions().InvokeWidth(5);
                    
                    if (line.Color != null)
                        polyline = polyline.InvokeColor(unchecked((int)(0xFF000000 | line.Color.Value)));

                    foreach (Step step in route.Steps.Take(route.Steps.Length - 1))
                    {
                        foreach (Position position in step.Trajectory)
                        {
                            LatLng latLng = new LatLng(position.Latitude, position.Longitude);
                            polyline = polyline.Add(latLng);
                        }

                        /*List<Circle> circles = line.Id < 6 ? tramCircles : busCircles;

                        circles.Add(googleMap.AddCircle(new CircleOptions()
                            .InvokeCenter(new LatLng(step.Stop.Position.Latitude, step.Stop.Position.Longitude))
                            .InvokeRadius(10)
                            .InvokeStrokeColor(unchecked((int)(0xFF000000 | line.Color.Value)))
                            .Visible(false)));*/
                    }

                    trajectories.Add(googleMap.AddPolyline(polyline));
                }
            }

            // Make tram circles visible
            foreach (Circle circle in tramCircles)
                circle.Visible = true;
        }
    }
}