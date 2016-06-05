using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Graphics;
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
using static Android.Support.V7.Widget.SearchView;
using Java.Lang;
using System.Threading;
using System.Threading.Tasks;
using Android.Locations;
using Android.Support.V4.Content;
using Android;

namespace TramUrWay.Android
{
    public class NearbyStopsFragment : TabFragment
    {
        public override string Title => "Stations";

        private StopsAdapter stopsAdapter;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.NearbyStopsFragment, container, false);
        }
        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            RecyclerView recyclerView = View.FindViewById<RecyclerView>(Resource.Id.NearbyStopsFragment_StopList);
            recyclerView.SetLayoutManager(new LinearLayoutManager(Activity));
            recyclerView.SetLayoutManager(new WrapLayoutManager(Activity));
            recyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));
            recyclerView.SetAdapter(stopsAdapter = new StopsAdapter(App.Lines.SelectMany(l => l.Stops)));
        }

        public override void OnResume()
        {
            base.OnResume();

            Task.Run(() => RefreshPosition());
        }
        protected override void OnGotFocus()
        {
            base.OnGotFocus();

            Task.Run(() => RefreshPosition());
        }

        private void RefreshPosition()
        {
            if (ContextCompat.CheckSelfPermission(Activity, Manifest.Permission.AccessFineLocation) != Permission.Granted)
                return;

            LocationManager locationManager = Activity.GetSystemService(Context.LocationService) as LocationManager;

            Criteria criteria = new Criteria();
            criteria.Accuracy = Accuracy.Coarse;
            criteria.AltitudeRequired = false;
            criteria.BearingRequired = false;
            criteria.CostAllowed = false;
            criteria.PowerRequirement = Power.Low;

            string provider = locationManager.GetBestProvider(criteria, true);
            Location location = locationManager.GetLastKnownLocation(provider);
            Position position = new Position((float)location.Latitude, (float)location.Longitude);

            Activity.RunOnUiThread(() => stopsAdapter.Position = position);
        }
    }
}