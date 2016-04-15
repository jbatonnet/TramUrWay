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
    public class LineViewHolder : RecyclerView.ViewHolder
    {
        public ImageView Icon { get; }
        public TextView Name { get; }

        public LineViewHolder(View itemView) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.LineItem_Icon);
            Name = itemView.FindViewById<TextView>(Resource.Id.LineItem_Name);
        }
    }

    public class LinesAdapter : RecyclerView.Adapter, View.IOnClickListener
    {
        public override int ItemCount
        {
            get
            {
                return lines.Length;
            }
        }

        private Line[] lines;
        private List<LineViewHolder> viewHolders = new List<LineViewHolder>();
        
        public LinesAdapter(IEnumerable<Line> lines)
        {
            this.lines = lines.ToArray();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.LineItem, parent, false);
            itemView.SetOnClickListener(this);

            return new LineViewHolder(itemView);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            LineViewHolder viewHolder = holder as LineViewHolder;
            Line line = lines[position];

            viewHolder.Icon.SetImageResource(Utils.GetIconForLine(line));
            viewHolder.Name.Text = line.Name;

            if (!viewHolders.Contains(viewHolder))
                viewHolders.Add(viewHolder);
        }

        public void OnClick(View view)
        {
            LineViewHolder viewHolder = viewHolders.First(vh => vh.ItemView == view);
            Line line = lines[viewHolder.AdapterPosition];

            Intent intent = new Intent(view.Context, typeof(LineActivity));
            intent.PutExtra("Line", line.Id);

            view.Context.StartActivity(intent);
        }
    }
}