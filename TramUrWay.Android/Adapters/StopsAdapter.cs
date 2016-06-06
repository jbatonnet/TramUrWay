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
        public TextView Description { get; }
        public ImageView Favorite { get; }

        public EventHandler FavoriteClick;

        public StopViewHolder(View itemView) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.StopItem_Icon);
            Name = itemView.FindViewById<TextView>(Resource.Id.StopItem_Name);
            Description = itemView.FindViewById<TextView>(Resource.Id.StopItem_Description);
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
        public Position? Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                UpdateFilter();
            }
        }

        public event EventHandler<Stop> StopClick;

        private List<KeyValuePair<string, Stop[]>> stops;
        private List<KeyValuePair<string, Stop[]>> filteredStops;
        private List<StopViewHolder> viewHolders = new List<StopViewHolder>();
        private string filter = null;
        private Position? position;
        
        public StopsAdapter()
        {
            stops = new List<KeyValuePair<string, Stop[]>>();
            UpdateFilter();
        }
        public StopsAdapter(IEnumerable<Stop> stops)
        {
            Update(stops);
        }

        public void Update(IEnumerable<Stop> stops)
        {
            this.stops = stops.Where(s => s.Name != null)
                              .GroupBy(s => Util.Hash(s.Name, s.Line.Id))
                              .OrderBy(g => g.First().Name)
                              .Select(g => new KeyValuePair<string, Stop[]>(g.First().Name, g.ToArray()))
                              .ToList();

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

            viewHolder.Icon.SetImageDrawable(stop.Value[0].Line.GetIconDrawable(viewHolder.ItemView.Context));
            viewHolder.Name.Text = stop.Key;

            if (this.position != null)
            {
                float distance = stop.Value[0].Position - this.position.Value;
                distance = (float)Math.Ceiling(distance / 100) * 100;

                viewHolder.Description.Text = distance > 1000 ? ((distance / 1000) + " km") : (distance + " m");
            }
            else
                viewHolder.Description.Text = "";

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
                foreach (Stop s in stop.Value)
                    s.SetIsFavorite(true);
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

                filteredStops = stops.Where(p => predicate(p.Key)).ToList();
            }

            if (position != null)
                filteredStops = filteredStops.OrderBy(p => p.Value[0].Position - position.Value).ToList();

            NotifyDataSetChanged();
        }
    }
}