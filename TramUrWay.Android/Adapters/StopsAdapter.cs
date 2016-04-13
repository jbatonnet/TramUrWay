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

        public StopViewHolder(View itemView) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.StopItem_Icon);
            Name = itemView.FindViewById<TextView>(Resource.Id.StopItem_Name);
        }
    }

    public class StopsAdapter : RecyclerView.Adapter, View.IOnClickListener
    {
        public override int ItemCount
        {
            get
            {
                return stops.Count;
            }
        }

        public event EventHandler<Stop> StopClick;

        private Dictionary<string, Stop[]> stops;
        private List<StopViewHolder> viewHolders = new List<StopViewHolder>();
        
        public StopsAdapter(IEnumerable<Stop> stops)
        {
            this.stops = stops.Where(s => s.Name != null)
                              .GroupBy(s => s.Name)
                              .OrderBy(g => g.Key)
                              .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.StopItem, parent, false);
            itemView.SetOnClickListener(this);

            return new StopViewHolder(itemView);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            StopViewHolder viewHolder = holder as StopViewHolder;
            KeyValuePair<string, Stop[]> stop = stops.ElementAt(position);

            viewHolder.Icon.SetImageResource(Database.GetIconForLine(stop.Value.First().Line));
            viewHolder.Name.Text = stop.Key;

            if (!viewHolders.Contains(viewHolder))
                viewHolders.Add(viewHolder);
        }

        public void OnClick(View view)
        {
            StopViewHolder viewHolder = viewHolders.First(vh => vh.ItemView == view);
            KeyValuePair<string, Stop[]> stop = stops.ElementAt(viewHolder.AdapterPosition);

            if (StopClick != null)
                StopClick.Invoke(this, stop.Value.First());
            else
            {
                Intent intent = new Intent(view.Context, typeof(StopActivity));
                intent.PutExtra("Stop", stop.Value.First().Id);

                view.Context.StartActivity(intent);
            }
        }
    }
}