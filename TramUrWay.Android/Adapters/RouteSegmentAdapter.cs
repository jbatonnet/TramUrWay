using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Support.Percent;
using Android.Support.V7.Widget;
using Android.Utilities;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json.Linq;
using Environment = System.Environment;

namespace TramUrWay.Android
{
    public class RouteSegmentViewHolder : RecyclerView.ViewHolder
    {
        public ImageView StopIcon { get; }
        public ImageView LineIcon { get; }
        public View LineBar { get; }
        public TextView StopName { get; }
        public RecyclerView StopList { get; }

        public RouteSegmentViewHolder(View itemView) : base(itemView)
        {
            StopIcon = itemView.FindViewById<ImageView>(Resource.Id.RouteSegmentItem_StopIcon);
            LineIcon = itemView.FindViewById<ImageView>(Resource.Id.RouteSegmentItem_LineIcon);
            LineBar = itemView.FindViewById(Resource.Id.RouteSegmentItem_LineBar);
            StopName = itemView.FindViewById<TextView>(Resource.Id.RouteSegmentItem_StopName);
            StopList = itemView.FindViewById<RecyclerView>(Resource.Id.RouteSegmentItem_StopList);
        }
    }

    public class RouteSegmentAdapter : RecyclerView.Adapter
    {
        public override int ItemCount
        {
            get
            {
                return routeSegments.Length;
            }
        }

        private RouteSegment[] routeSegments;
        private List<RouteSegmentViewHolder> viewHolders = new List<RouteSegmentViewHolder>();

        public RouteSegmentAdapter(RouteSegment[] routeSegments)
        {
            this.routeSegments = routeSegments;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.RouteSegmentItem, parent, false);
            return new RouteSegmentViewHolder(itemView);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            RouteSegmentViewHolder viewHolder = holder as RouteSegmentViewHolder;
            RouteSegment segment = routeSegments[position];

            viewHolder.StopIcon.SetImageBitmap(Utils.GetStopIconForLine(viewHolder.StopIcon.Context, segment.Line));
            viewHolder.LineIcon.SetImageDrawable(segment.Line.GetIconDrawable(viewHolder.StopIcon.Context));
            viewHolder.LineBar.SetBackgroundColor(Utils.GetColorForLine(viewHolder.StopIcon.Context, segment.Line));
            viewHolder.StopName.Text = segment.From.Stop.Name;

            viewHolder.StopList.SetLayoutManager(new WrapLayoutManager(viewHolder.StopIcon.Context));
            viewHolder.StopList.SetAdapter(new RouteSegmentStopAdapter(segment));

            if (!viewHolders.Contains(viewHolder))
                viewHolders.Add(viewHolder);
        }
    }
}