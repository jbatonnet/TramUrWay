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
        public TextView Date { get; }

        public RouteSegmentStopViewHolder(View itemView) : base(itemView)
        {
            StopIcon = itemView.FindViewById<ImageView>(Resource.Id.RouteSegmentStopItem_StopIcon);
            StopName = itemView.FindViewById<TextView>(Resource.Id.RouteSegmentStopItem_StopName);
            Date = itemView.FindViewById<TextView>(Resource.Id.RouteSegmentStopItem_Date);
        }
    }

    public class RouteSegmentStopAdapter : RecyclerView.Adapter
    {
        public override int ItemCount
        {
            get
            {
                return routeSegment.TimeSteps.Length;
            }
        }

        private RouteSegment routeSegment;

        private List<RouteSegmentStopViewHolder> viewHolders = new List<RouteSegmentStopViewHolder>();

        public RouteSegmentStopAdapter(RouteSegment routeSegment)
        {
            this.routeSegment = routeSegment;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.RouteSegmentStopItem, parent, false);
            return new RouteSegmentStopViewHolder(itemView);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            RouteSegmentStopViewHolder viewHolder = holder as RouteSegmentStopViewHolder;

            Stop stop;
            DateTime date;

            if (position == routeSegment.TimeSteps.Length  - 1)
            {
                stop = routeSegment.To.Stop;
                date = routeSegment.DateTo;
            }
            else
            {
                TimeStep timeStep = routeSegment.TimeSteps[position + 1];
                stop = timeStep.Step.Stop;
                date = timeStep.Date;
            }

            viewHolder.StopIcon.SetImageBitmap(Utils.GetStopIconForLine(viewHolder.StopIcon.Context, routeSegment.Line, 8));
            viewHolder.StopName.Text = stop.Name;
            viewHolder.Date.Text = date.ToString("HH:mm");

            if (!viewHolders.Contains(viewHolder))
                viewHolders.Add(viewHolder);
        }
    }
}