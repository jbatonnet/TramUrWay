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
using System.Threading.Tasks;

namespace TramUrWay.Android
{
    public class HomeStopsFragment : TabFragment
    {
        public override string Title => "Stations";

        private StopsAdapter stopsAdapter = new StopsAdapter();
        private bool loaded = false;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.HomeStopsFragment, container, false);
        }
        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            RecyclerView recyclerView = View.FindViewById<RecyclerView>(Resource.Id.StopsFragment_StopList);
            recyclerView.SetLayoutManager(new LinearLayoutManager(Activity));
            recyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));
            recyclerView.SetAdapter(stopsAdapter);
        }
        protected override async void OnGotFocus()
        {
            if (loaded)
                return;

            loaded = true;

            await Task.Run(() =>
            {
                foreach (Line line in TramUrWayApplication.Lines)
                    line.Loaded.WaitOne();

                Activity.RunOnUiThread(() =>
                {
                    stopsAdapter.Update(TramUrWayApplication.Lines.SelectMany(l => l.Stops));
                });
            });
        }

        public void OnQueryTextChanged(object sender, QueryTextChangeEventArgs e)
        {
            stopsAdapter.Filter = e.NewText;
        }
    }
}