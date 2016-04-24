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
        }
    }
}