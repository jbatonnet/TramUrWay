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

namespace TramUrWay.Android
{
    public class StopViewHolder : RecyclerView.ViewHolder
    {
        public ImageView Icon { get; }
        public TextView Name { get; }
        public ImageView Favorite { get; }

        public EventHandler FavoriteClick;

        public StopViewHolder(View itemView) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.StopItem_Icon);
            Name = itemView.FindViewById<TextView>(Resource.Id.StopItem_Name);
            Favorite = itemView.FindViewById<ImageView>(Resource.Id.StopItem_Favorite);

            Favorite.Click += (s, e) => FavoriteClick?.Invoke(s, e);
        }
    }

    public class StopsAdapter : RecyclerView.Adapter, View.IOnClickListener
    {
        public override int ItemCount
        {
            get
            {
                return filteredStops.Count;
            }
        }
        public string Filter
        {
            get
            {
                return filter;
            }
            set
            {
                filter = value;
                UpdateFilter();
            }
        }

        public event EventHandler<Stop> StopClick;

        private Dictionary<string, Stop[]> stops;
        private Dictionary<string, Stop[]> filteredStops;
        private List<StopViewHolder> viewHolders = new List<StopViewHolder>();
        private string filter = null;
        
        public StopsAdapter(IEnumerable<Stop> stops)
        {
            this.stops = stops.Where(s => s.Name != null)
                              .GroupBy(s => s.Name)
                              .OrderBy(g => g.Key)
                              .ToDictionary(g => g.Key, g => g.ToArray());

            UpdateFilter();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.StopItem, parent, false);
            itemView.SetOnClickListener(this);

            return new StopViewHolder(itemView) { FavoriteClick = Favorite_Click };
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            StopViewHolder viewHolder = holder as StopViewHolder;
            KeyValuePair<string, Stop[]> stop = filteredStops.ElementAt(position);

            viewHolder.Icon.SetImageDrawable(stop.Value.First().Line.GetIconDrawable(viewHolder.ItemView.Context));
            viewHolder.Name.Text = stop.Key;

            viewHolder.Favorite.SetImageResource(stop.Value.Any(s => s.GetIsFavorite()) ? Resource.Drawable.ic_star : Resource.Drawable.ic_star_border);
            viewHolder.Favorite.Visibility = StopClick != null ? ViewStates.Gone : ViewStates.Visible;

            if (!viewHolders.Contains(viewHolder))
            viewHolders.Add(viewHolder);
        }

        public void OnClick(View view)
        {
            StopViewHolder viewHolder = viewHolders.First(vh => vh.ItemView == view);
            KeyValuePair<string, Stop[]> stop = filteredStops.ElementAt(viewHolder.AdapterPosition);

            if (StopClick != null)
                StopClick.Invoke(this, stop.Value.First());
            else
            {
                Intent intent = new Intent(view.Context, typeof(StopActivity));
                intent.PutExtra("Stop", stop.Value.First().Id);

                view.Context.StartActivity(intent);
            }
        }

        private void Favorite_Click(object sender, EventArgs e)
        {
            View view = sender as View;
            StopViewHolder viewHolder = viewHolders.First(vh => vh.ItemView == view.Parent);
            KeyValuePair<string, Stop[]> stop = filteredStops.ElementAt(viewHolder.AdapterPosition);

            if (stop.Value.Any(s => s.GetIsFavorite()))
            {
                foreach (Stop s in stop.Value)
                    s.SetIsFavorite(false);
            }
            else
            {
                Stop[] stops = App.Lines.SelectMany(l => l.Stops)
                                        .Where(s => s.Name == stop.Key)
                                        .GroupBy(s => s.Line)
                                        .Select(g => g.First())
                                        .OrderBy(s => s.Line.Id)
                                        .ToArray();

                if (stops.Length == 1)
                    stops[0].SetIsFavorite(true);
                else
                {
                    string[] choices = stops.Select(s => s.Line.ToString()).ToArray();

                    new global::Android.Support.V7.App.AlertDialog.Builder(view.Context)
                        .SetTitle("Choisissez une ligne")
                        .SetSingleChoiceItems(choices, 0, (s, a) =>
                        {
                            stops[a.Which].SetIsFavorite(true);
                            (s as Dialog).Dismiss();

                            NotifyDataSetChanged();
                        })
                        .Show();
                }
            }

            NotifyItemChanged(viewHolder.AdapterPosition);
        }

        private void UpdateFilter()
        {
            if (string.IsNullOrWhiteSpace(filter))
                filteredStops = stops;
            else
            {
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

                filteredStops = stops.Where(p => predicate(p.Key))
                                     .ToDictionary(p => p.Key, p => p.Value);
            }

            NotifyDataSetChanged();
        }
    }
}