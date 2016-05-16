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
    public class RouteSegmentsViewHolder : RecyclerView.ViewHolder
    {
        public ImageView Icon { get; }
        public TextView Name { get; }

        public RouteSegmentsViewHolder(View itemView) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.RouteSegmentsItem_Icon);
            Name = itemView.FindViewById<TextView>(Resource.Id.RouteSegmentsItem_Name);
        }
    }

    public class RouteSegmentsAdapter : RecyclerView.Adapter, View.IOnClickListener
    {
        public override int ItemCount
        {
            get
            {
                return routeSegments?.Length ?? 0;
            }
        }
        public IEnumerable<RouteSegment[]> RouteSegments
        {
            get
            {
                return routeSegments;
            }
            set
            {
                routeSegments = value.ToArray();
                NotifyDataSetChanged();
            }
        }

        private RouteSegment[][] routeSegments;
        private List<RouteSegmentsViewHolder> viewHolders = new List<RouteSegmentsViewHolder>();

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.RouteSegmentsItem, parent, false);
            itemView.SetOnClickListener(this);

            return new RouteSegmentsViewHolder(itemView);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            RouteSegmentsViewHolder viewHolder = holder as RouteSegmentsViewHolder;
            RouteSegment[] timeStep = routeSegments[position];

            //viewHolder.Icon.SetImageDrawable(timeStep.Step.Route.Line.GetIconDrawable(viewHolder.ItemView.Context));
            viewHolder.Name.Text = timeStep.Join(", ");
            //viewHolder.Description.Text = Utils.GetReadableTime(timeStep, DateTime.Now);

            if (!viewHolders.Contains(viewHolder))
                viewHolders.Add(viewHolder);
        }

        public void OnClick(View view)
        {
            RouteSegmentsViewHolder viewHolder = viewHolders.First(vh => vh.ItemView == view);
            RouteSegment[] timeStep = routeSegments[viewHolder.AdapterPosition];

            /*Intent intent = new Intent(view.Context, typeof(LineActivity));
            intent.PutExtra("Line", timeStep.Step.Route.Line.Id);
            intent.PutExtra("Route", timeStep.Step.Route.Id);

            view.Context.StartActivity(intent);*/
        }
    }
}