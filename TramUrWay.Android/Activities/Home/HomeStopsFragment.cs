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
using System.Text;

namespace TramUrWay.Android
{
    public class HomeStopsFragment : TabFragment
    {
        public override string Title => "Stations";

        private GenericStopsAdapter stopsAdapter;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.HomeStopsFragment, container, false);

            // Load view
            RecyclerView recyclerView = view.FindViewById<RecyclerView>(Resource.Id.StopsFragment_StopList);
            recyclerView.SetLayoutManager(new LinearLayoutManager(Activity));
            recyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));
            recyclerView.SetAdapter(stopsAdapter = new GenericStopsAdapter());

            // Setup adapter
            stopsAdapter.Click += StopsAdapter_Click;
            stopsAdapter.Filter = (stop, search) =>
            {
                // ASCII normalize strings
                string value = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(stop.Name.ToLowerInvariant()));
                string pattern = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(search.ToLowerInvariant()));

                // Remove non character strings
                value = new string(value.Select(c => char.IsLetter(c) ? c : ' ').ToArray());
                pattern = new string(pattern.Select(c => char.IsLetter(c) ? c : ' ').ToArray());

                return value.Contains(pattern);
            };

            // Trigger async loading
            Task.Run(() =>
            {
                foreach (Line line in TramUrWayApplication.Lines)
                    line.Loaded.WaitOne();

                Activity.RunOnUiThread(() =>
                {
                    stopsAdapter.Items = TramUrWayApplication.Lines.SelectMany(l => l.Stops)
                                                                   .GroupBy(s => Utils.Hash(s.Line.Id, s.Name))
                                                                   .Select(g => g.First());
                });
            });

            return view;
        }
        public void OnQueryTextChanged(object sender, QueryTextChangeEventArgs e)
        {
            stopsAdapter.FilterText = e.NewText;
        }

        private void StopsAdapter_Click(GenericAdapter<Stop> adapter, View view, Stop item)
        {
            Intent intent = new Intent(view.Context, typeof(StopActivity));
            intent.PutExtra("Stop", item.Id);
            intent.PutExtra("Line", item.Line.Id);

            view.Context.StartActivity(intent);
        }
    }
}