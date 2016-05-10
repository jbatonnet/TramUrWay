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
using Android.Support.V7.Widget;
using Android.Util;
using Android.Utilities;
using Android.Views;
using Android.Widget;

namespace TramUrWay.Android
{
    public class StepViewHolder : RecyclerView.ViewHolder
    {
        public View Rail1 { get; }
        public View Rail2 { get; }
        public ImageView Icon1 { get; }
        public ImageView Icon2 { get; }
        public ImageView Dot { get; }
        public TextView Name { get; }
        public TextView Description { get; }
        public ImageView Favorite { get; }

        public EventHandler FavoriteClick;

        public StepViewHolder(View itemView) : base(itemView)
        {
            Rail1 = itemView.FindViewById(Resource.Id.StepItem_Rail1);
            Rail2 = itemView.FindViewById(Resource.Id.StepItem_Rail2);
            Icon1 = itemView.FindViewById<ImageView>(Resource.Id.StepItem_Icon1);
            Icon2 = itemView.FindViewById<ImageView>(Resource.Id.StepItem_Icon2);
            Dot = itemView.FindViewById<ImageView>(Resource.Id.StepItem_Dot);
            Name = itemView.FindViewById<TextView>(Resource.Id.StepItem_Name);
            Description = itemView.FindViewById<TextView>(Resource.Id.StepItem_Description);
            Favorite = itemView.FindViewById<ImageView>(Resource.Id.StepItem_Favorite);

            Favorite.Click += (s, e) => FavoriteClick?.Invoke(s, e);
        }
    }

    public class RouteAdapter : RecyclerView.Adapter, View.IOnClickListener
    {
        private const int StopIconSize = 32;
        private const int TransportIconSize = 48;

        public override int ItemCount
        {
            get
            {
                return Route.Steps.Length;
            }
        }
        public Route Route { get; }

        private List<StepViewHolder> viewHolders = new List<StepViewHolder>();
        private TimeStep[][] stepTimes;
        private Transport[] stepTransports;
        private Color color;

        private Bitmap stopBitmap;
        private Bitmap transportBitmap;

        public RouteAdapter(Route route)
        {
            Route = route;
            color = Utils.GetColorForLine(null, route.Line);
        }

        public void Update(IEnumerable<TimeStep> timeSteps, IEnumerable<Transport> transports)
        {
            if (timeSteps == null)
            {
                stepTimes = null;
                stepTransports = null;

                return;
            }

            stepTimes = Route.Steps.Select(s => timeSteps.Where(t => t.Step == s).ToArray()).ToArray();
            stepTransports = Route.Steps.Select(s => transports.FirstOrDefault(t => t.Step == s)).ToArray();

            NotifyDataSetChanged();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            // Late load icons
            if (stopBitmap == null)
            {
                stopBitmap = Utils.GetStopIconForLine(parent.Context, Route.Line, StopIconSize);
                transportBitmap = Utils.GetTransportIconForLine(parent.Context, Route.Line, TransportIconSize);
            }

            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.StepItem, parent, false);
            itemView.SetOnClickListener(this);

            StepViewHolder viewHolder = new StepViewHolder(itemView) { FavoriteClick = Favorite_Click };

            viewHolder.Icon1.SetImageBitmap(transportBitmap);
            viewHolder.Icon2.SetImageBitmap(transportBitmap);
            viewHolder.Dot.SetImageBitmap(stopBitmap);

            return viewHolder;
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            StepViewHolder viewHolder = holder as StepViewHolder;
            Step step = Route.Steps[position];

            // First update
            if (holder.OldPosition == -1)
            {
                viewHolder.Name.Text = step.Stop.Name;

                viewHolder.Rail1.SetBackgroundColor(color);
                viewHolder.Rail2.SetBackgroundColor(color);

                viewHolder.Rail1.Visibility = position == 0 ? ViewStates.Gone : ViewStates.Visible;
                viewHolder.Rail2.Visibility = position == Route.Steps.Length - 1 ? ViewStates.Gone : ViewStates.Visible;
            }

            // Update texts
            viewHolder.Favorite.SetImageResource(step.Stop.GetIsFavorite() ? Resource.Drawable.ic_star : Resource.Drawable.ic_star_border);

            if (stepTimes != null)
            {
                TimeStep[] timeSteps = stepTimes[position];

                if (timeSteps.Length > 0)
                    viewHolder.Description.Text = Utils.GetReadableTimes(timeSteps, DateTime.Now);
                else
                    viewHolder.Description.Text = "Service terminé";
            }
            else
                viewHolder.Description.Text = "Informations non disponibles";

            // Update icon positions
            if (stepTransports != null)
            {
                float density = viewHolder.ItemView.Context.Resources.DisplayMetrics.Density;

                {
                    Transport currentTransport = position == stepTransports.Length ? null : stepTransports[position];
                    float currentPosition = currentTransport == null ? 72 : (int)(1 + 36 - 16 + 72 * currentTransport.Progress);

                    RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams((int)(32 * density), (int)(32 * density));
                    layoutParams.AddRule(LayoutRules.CenterHorizontal);
                    layoutParams.SetMargins(0, (int)(currentPosition * density), 0, 0);

                    viewHolder.Icon1.LayoutParameters = layoutParams;
                }

                {
                    Transport previousTransport = position == 0 ? null : stepTransports[position - 1];
                    float previousPosition = previousTransport == null ? 72 : (int)(-1 - 36 - 16 + 72 * previousTransport.Progress);

                    RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams((int)(32 * density), (int)(32 * density));
                    layoutParams.AddRule(LayoutRules.CenterHorizontal);
                    layoutParams.SetMargins(0, (int)(previousPosition * density), 0, 0);

                    viewHolder.Icon2.LayoutParameters = layoutParams;
                }
            }

            if (!viewHolders.Contains(viewHolder))
                viewHolders.Add(viewHolder);
        }
        public void OnClick(View view)
        {
            StepViewHolder viewHolder = viewHolders.First(vh => vh.ItemView == view);
            Step step = Route.Steps[viewHolder.AdapterPosition];

            Intent intent = new Intent(view.Context, typeof(StopActivity));
            intent.PutExtra("Stop", step.Stop.Id);
            intent.PutExtra("Line", step.Route.Line.Id);

            view.Context.StartActivity(intent);
        }

        private void Favorite_Click(object sender, EventArgs e)
        {
            View view = sender as View;
            StepViewHolder viewHolder = viewHolders.First(vh => vh.ItemView == view.Parent);
            Step step = Route.Steps[viewHolder.AdapterPosition];

            bool favorite = !step.Stop.GetIsFavorite();
            step.Stop.SetIsFavorite(favorite);

            NotifyItemChanged(viewHolder.AdapterPosition);
        }
    }
}