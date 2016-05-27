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
    public class RouteSegmentStopViewHolder : RecyclerView.ViewHolder
    {
        public ImageView StopIcon { get; }
        public TextView StopName { get; }

        public RouteSegmentStopViewHolder(View itemView) : base(itemView)
        {
            StopIcon = itemView.FindViewById<ImageView>(Resource.Id.RouteSegmentStopItem_StopIcon);
            StopName = itemView.FindViewById<TextView>(Resource.Id.RouteSegmentStopItem_StopName);
        }
    }

    public class RouteSegmentStopAdapter : RecyclerView.Adapter
    {
        public override int ItemCount
        {
            get
            {
                return routeSteps.Count;
            }
        }

        private RouteSegment routeSegment;
        private List<Step> routeSteps;

        private List<RouteSegmentStopViewHolder> viewHolders = new List<RouteSegmentStopViewHolder>();

        public RouteSegmentStopAdapter(RouteSegment routeSegment)
        {
            this.routeSegment = routeSegment;

            routeSteps = new List<Step>();

            Step next = routeSegment.From;
            do
            {
                next = next.Next;
                routeSteps.Add(next);
            }
            while (next != null && next.Stop.Name != routeSegment.To.Stop.Name);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.RouteSegmentStopItem, parent, false);
            return new RouteSegmentStopViewHolder(itemView);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            RouteSegmentStopViewHolder viewHolder = holder as RouteSegmentStopViewHolder;
            Step step = routeSteps[position];

            viewHolder.StopIcon.SetImageBitmap(Utils.GetStopIconForLine(viewHolder.StopIcon.Context, routeSegment.Line, 8));
            viewHolder.StopName.Text = step.Stop.Name;

            if (!viewHolders.Contains(viewHolder))
                viewHolders.Add(viewHolder);
        }
    }
}