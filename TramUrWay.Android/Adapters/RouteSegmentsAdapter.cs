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

using Environment = System.Environment;

namespace TramUrWay.Android
{
    public class RouteSegmentsViewHolder : RecyclerView.ViewHolder
    {
        public TextView From { get; }
        public TextView To { get; }
        public TextView Duration { get; }
        public TextView Left { get; }
        public PercentRelativeLayout Preview { get; }

        public RouteSegmentsViewHolder(View itemView) : base(itemView)
        {
            From = itemView.FindViewById<TextView>(Resource.Id.RouteSegmentsItem_From);
            To = itemView.FindViewById<TextView>(Resource.Id.RouteSegmentsItem_To);
            Duration = itemView.FindViewById<TextView>(Resource.Id.RouteSegmentsItem_Duration);
            Left = itemView.FindViewById<TextView>(Resource.Id.RouteSegmentsItem_Left);
            Preview = itemView.FindViewById<PercentRelativeLayout>(Resource.Id.RouteSegmentsItem_PreviewLayout);
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
                routeSegments = value.ToList().ToArray(); // To avoid too short array from being allocated too soon
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

            viewHolder.From.Text = timeStep.First().DateFrom.ToString("HH:mm");
            viewHolder.To.Text = timeStep.Last().DateTo.ToString("HH:mm");
            viewHolder.Duration.Text = Math.Ceiling((timeStep.Last().DateTo - timeStep.First().DateFrom).TotalMinutes) + " min";

            double left = Math.Ceiling((timeStep.First().DateFrom - DateTime.Now).TotalMinutes);
            viewHolder.Left.Text = left < 0 ? "Parti" : (left + Environment.NewLine + "min");

            Context context = viewHolder.ItemView.Context;
            float density = context.Resources.DisplayMetrics.Density;

            DateTime begin = timeStep.First().DateFrom;
            DateTime end = timeStep.Last().DateTo;
            TimeSpan total = end - begin;

            viewHolder.Preview.RemoveAllViews();

            PercentRelativeLayout.LayoutParams percentLayoutParams;

            LinearLayout.LayoutParams barBackLayoutParams = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent);
            barBackLayoutParams.SetMargins((int)(4 * density), 0, (int)(4 * density), 0);

            int i = 1;

            foreach (RouteSegment segment in timeStep)
            {
                Color color = Utils.GetColorForLine(context, segment.Line);

                ImageView image = new ImageView(context);
                image.Id = i;
                image.SetImageResource(Resource.Drawable.circle2);
                image.SetColorFilter(color);

                percentLayoutParams = new PercentRelativeLayout.LayoutParams((int)(14 * density), (int)(14 * density));
                percentLayoutParams.AddRule(LayoutRules.CenterVertical);
                percentLayoutParams.SetMargins((int)(-7 * density), 0, 0, 0);
                percentLayoutParams.PercentLayoutInfo.LeftMarginPercent = (float)(segment.DateFrom - begin).Ticks / total.Ticks;

                viewHolder.Preview.AddView(image, percentLayoutParams);

                LinearLayout bar = new LinearLayout(context);

                View barBack = new View(context);
                barBack.SetBackgroundColor(color);

                bar.AddView(barBack, barBackLayoutParams);

                percentLayoutParams = new PercentRelativeLayout.LayoutParams((int)(28 * density), (int)(4 * density));
                percentLayoutParams.AddRule(LayoutRules.CenterVertical);
                percentLayoutParams.AddRule(LayoutRules.RightOf, i);
                percentLayoutParams.SetMargins((int)(-7 * density), 0, (int)(-7 * density), 0);
                percentLayoutParams.PercentLayoutInfo.WidthPercent = (float)(segment.DateTo - segment.DateFrom).Ticks / total.Ticks;

                viewHolder.Preview.AddView(bar, percentLayoutParams);

                i++;
            }

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