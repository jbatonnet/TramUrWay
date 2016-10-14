using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Utilities;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace TramUrWay.Android
{
    // See https://gist.github.com/Cheesebaron/9838325 for reference

    public class StopNameAdapter : BaseAdapter<string>, IFilterable
    {
        public class StopNameFilter : Filter
        {
            private StopNameAdapter stopNamesAdapter;
            private string[] stopNames;

            public StopNameFilter(StopNameAdapter stopNamesAdapter, string[] stopNames)
            {
                this.stopNamesAdapter = stopNamesAdapter;
                this.stopNames = stopNames;
            }

            protected override FilterResults PerformFiltering(ICharSequence constraint)
            {
                FilterResults filterResults = new FilterResults();

                if (constraint == null)
                    return filterResults;

                string filter = new string(constraint.Select(c => c).ToArray());

                Predicate<string> predicate = s =>
                {
                    // ASCII normalize strings
                    string value = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(s.ToLowerInvariant()));
                    string pattern = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(filter.ToLowerInvariant()));

                    // Remove non character strings
                    value = new string(value.Select(c => char.IsLetter(c) ? c : ' ').ToArray());
                    pattern = new string(pattern.Select(c => char.IsLetter(c) ? c : ' ').ToArray());

                    return value.Contains(pattern);
                };

                string[] values = stopNames.Where(s => predicate(s)).ToArray();

                filterResults.Values = FromArray(values.Select(s => new Java.Lang.String(s)).ToArray());
                filterResults.Count = values.Length;

                constraint.Dispose();

                return filterResults;
            }
            protected override void PublishResults(ICharSequence constraint, FilterResults results)
            {
                using (var values = results.Values)
                    stopNamesAdapter.filteredStopNames = values?.ToArray<Java.Lang.String>()?.Select(r => r.ToString())?.ToArray() ?? new string[0];

                stopNamesAdapter.NotifyDataSetChanged();

                // Don't do this and see GREF counts rising
                constraint?.Dispose();
                results.Dispose();
            }
        }

        private string[] stopNames;
        private string[] filteredStopNames;

        private readonly Activity activity;

        public StopNameAdapter(Activity activity)
        {
            stopNames = TramUrWayApplication.Lines.SelectMany(l => l.Stops).Select(s => s.Name).Distinct().ToArray();
            this.activity = activity;

            Filter = new StopNameFilter(this, stopNames);
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = activity.LayoutInflater.Inflate(Resource.Layout.RouteAutocompleteItem, null);

            var stopName = (filteredStopNames ?? stopNames)[position];

            var nameView = view.FindViewById<TextView>(Resource.Id.TextView);
            nameView.Text = stopName;

            return view;
        }

        public override int Count
        {
            get { return (filteredStopNames ?? stopNames).Length; }
        }

        public override string this[int position]
        {
            get { return (filteredStopNames ?? stopNames)[position]; }
        }

        public Filter Filter { get; private set; }

        public override void NotifyDataSetChanged()
        {
            // If you are using cool stuff like sections
            // remember to update the indices here!
            base.NotifyDataSetChanged();
        }
    }
}