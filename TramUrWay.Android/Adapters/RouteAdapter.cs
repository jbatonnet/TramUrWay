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
        public View Rail { get; }
        public ImageView Icon1 { get; }
        public ImageView Icon2 { get; }
        public TextView Name { get; }
        public TextView Description { get; }
        public ImageView Favorite { get; }

        public EventHandler FavoriteClick;

        public StepViewHolder(View itemView) : base(itemView)
        {
            Rail = itemView.FindViewById(Resource.Id.StepItem_Rail);
            Icon1 = itemView.FindViewById< ImageView>(Resource.Id.StepItem_Icon1);
            Icon2 = itemView.FindViewById<ImageView>(Resource.Id.StepItem_Icon2);
            Name = itemView.FindViewById<TextView>(Resource.Id.StepItem_Name);
            Description = itemView.FindViewById<TextView>(Resource.Id.StepItem_Description);
            Favorite = itemView.FindViewById<ImageView>(Resource.Id.StepItem_Favorite);

            Favorite.Click += (s, e) => FavoriteClick?.Invoke(s, e);
        }
    }

    public class RouteAdapter : RecyclerView.Adapter, View.IOnClickListener
    {
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
        private float?[] tramProgresses;
        private Color color;

        public RouteAdapter(Route route)
        {
            Route = route;
            color = Database.GetColorForLine(route.Line);
        }

        public void UpdateSteps(IEnumerable<TimeStep> timeSteps)
        {
            DateTime now = DateTime.Now;

            // Update timesteps
            stepTimes = Route.Steps.Select(s => timeSteps.Where(t => t.Step == s).ToArray()).ToArray();

            // Update tramways positions
            tramProgresses = new float?[Route.Steps.Length - 1];
            for (int i = 0; i < Route.Steps.Length - 1; i++)
            {
                if (stepTimes[i + 1].Length == 0)
                    continue;

                if (stepTimes[i].Length > 0 && stepTimes[i][0].Date < stepTimes[i + 1][0].Date)
                    continue;

                TimeSpan diff = stepTimes[i + 1][0].Date - now;
                tramProgresses[i] = (float)(1 - Math.Min(diff.TotalMinutes, 2) / 2);
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.StepItem, parent, false);
            itemView.SetOnClickListener(this);

            return new StepViewHolder(itemView) { FavoriteClick = Favorite_Click };
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            StepViewHolder viewHolder = holder as StepViewHolder;
            Step step = Route.Steps[position];

            // First update
            if (holder.OldPosition == -1)
            {
                viewHolder.Name.Text = step.Stop.Name;

                // Update colors
                viewHolder.Rail.SetBackgroundColor(color);
                viewHolder.Icon1.SetColorFilter(color);
                viewHolder.Icon2.SetColorFilter(color);
            }

            // Update texts
            viewHolder.Favorite.SetImageResource(step.Stop.Favorite ? Resource.Drawable.ic_star : Resource.Drawable.ic_star_border);

            if (stepTimes != null)
            {
                TimeStep[] timeSteps = stepTimes[position];

                if (timeSteps.Length > 0)
                    viewHolder.Description.Text = Database.GetReadableTimes(timeSteps, DateTime.Now);
                else
                    viewHolder.Description.Text = "Service terminé";
            }

            // Update icon positions
            if (tramProgresses != null)
            {
                float density = viewHolder.ItemView.Context.Resources.DisplayMetrics.Density;

                {
                    float? currentProgress = position == tramProgresses.Length ? null : tramProgresses[position];
                    float currentPosition = currentProgress == null ? 72 : (int)(36 - 16 + 72 * currentProgress.Value);

                    RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams((int)(32 * density), (int)(32 * density));
                    layoutParams.AddRule(LayoutRules.CenterHorizontal);
                    layoutParams.SetMargins(0, (int)(currentPosition * density), 0, 0);

                    viewHolder.Icon1.LayoutParameters = layoutParams;
                }

                {
                    float? previousProgress = position == 0 ? null : tramProgresses[position - 1];
                    float previousPosition = previousProgress == null ? 72 : (int)(-1 - 36 - 16 + 72 * previousProgress.Value);

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

            bool favorite = !step.Stop.Favorite;
            step.Stop.Favorite = favorite;

            NotifyItemChanged(viewHolder.AdapterPosition);
        }
    }
}