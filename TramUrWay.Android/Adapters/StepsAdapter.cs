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
        public ImageView Icon { get; }
        public TextView Name { get; }
        public TextView Description { get; }
        public ImageView Favorite { get; }

        public EventHandler FavoriteClick;

        public StepViewHolder(View itemView) : base(itemView)
        {
            Rail = itemView.FindViewById(Resource.Id.StepItem_Rail);
            Icon = itemView.FindViewById< ImageView>(Resource.Id.StepItem_Icon);
            Name = itemView.FindViewById<TextView>(Resource.Id.StepItem_Name);
            Description = itemView.FindViewById<TextView>(Resource.Id.StepItem_Description);
            Favorite = itemView.FindViewById<ImageView>(Resource.Id.StepItem_Favorite);

            Favorite.Click += (s, e) => FavoriteClick?.Invoke(s, e);
        }
    }

    public class StepsAdapter : RecyclerView.Adapter, View.IOnClickListener
    {
        public override int ItemCount
        {
            get
            {
                return stepTimes.Count;
            }
        }

        private Dictionary<Step, TimeStep[]> stepTimes;
        private List<StepViewHolder> viewHolders = new List<StepViewHolder>();
        
        public StepsAdapter(Dictionary<Step, TimeStep[]> stepTimes)
        {
            this.stepTimes = stepTimes;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.StepItem, parent, false);
            itemView.SetOnClickListener(this);

            return new StepViewHolder(itemView)
            {
                FavoriteClick = Favorite_Click
            };
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            StepViewHolder viewHolder = holder as StepViewHolder;
            KeyValuePair<Step, TimeStep[]> stepTime = stepTimes.ElementAt(position);

            // Update texts
            viewHolder.Name.Text = stepTime.Key.Stop.Name;
            viewHolder.Favorite.SetImageResource(stepTime.Key.Stop.Favorite ? Resource.Drawable.ic_star : Resource.Drawable.ic_star_border);

            if (stepTime.Value != null)
            {
                if (stepTime.Value.Length > 0)
                    viewHolder.Description.Text = Database.GetReadableTimes(stepTime.Value, DateTime.Now);
                else
                    viewHolder.Description.Text = "Service terminé";
            }
            else
                viewHolder.Description.Text = "Information indisponible";

            // Update colors
            Color color = Database.GetColorForLine(stepTime.Key.Route.Line);

            viewHolder.Rail.SetBackgroundColor(color);
            viewHolder.Icon.SetColorFilter(color);

            // Update icon positions
            TimeStep timeStep = stepTime.Value.FirstOrDefault();
            TimeSpan? diff = timeStep == null ? (TimeSpan?)null : timeStep.Date - DateTime.Now;
            int density = (int)(viewHolder.ItemView.Context.Resources.DisplayMetrics.Density);

            if (timeStep == null || diff?.TotalSeconds < 0 || diff?.TotalMinutes > 2)
            {
                TimeStep nextStepTime = position + 1 >= stepTimes.Count ? null : stepTimes.ElementAt(position + 1).Value?.FirstOrDefault();
                diff = nextStepTime == null ? (TimeSpan?)null : nextStepTime.Date - DateTime.Now;

                if (nextStepTime == null || diff?.TotalSeconds < 0 || diff?.TotalMinutes > 2)
                {
                    RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams(32 * density, 32 * density);
                    layoutParams.SetMargins(0, 72 * density, 0, 0);
                    viewHolder.Icon.LayoutParameters = layoutParams;
                }
                else
                {
                    int margin = 72 + 1 + 36 - (int)(diff.Value.TotalMinutes / 2 * 72) - 16;

                    RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams(32 * density, 32 * density);
                    layoutParams.AddRule(LayoutRules.CenterHorizontal);
                    layoutParams.SetMargins(0, margin * density, 0, 0);
                    viewHolder.Icon.LayoutParameters = layoutParams;
                }
            }
            else
            {
                int margin = 36 - (int)(diff.Value.TotalMinutes / 2 * 72) - 16;

                RelativeLayout.LayoutParams layoutParams = new RelativeLayout.LayoutParams(32 * density, 32 * density);
                layoutParams.AddRule(LayoutRules.CenterHorizontal);
                layoutParams.SetMargins(0, margin * density, 0, 0);
                viewHolder.Icon.LayoutParameters = layoutParams;
            }

            if (!viewHolders.Contains(viewHolder))
                viewHolders.Add(viewHolder);
        }
        public void OnClick(View view)
        {
            StepViewHolder viewHolder = viewHolders.First(vh => vh.ItemView == view);
            KeyValuePair<Step, TimeStep[]> stepTime = stepTimes.ElementAt(viewHolder.AdapterPosition);

            Intent intent = new Intent(view.Context, typeof(StopActivity));
            intent.PutExtra("Stop", stepTime.Key.Stop.Id);
            intent.PutExtra("Line", stepTime.Key.Route.Line.Id);

            view.Context.StartActivity(intent);
        }

        private void Favorite_Click(object sender, EventArgs e)
        {
            View view = sender as View;
            StepViewHolder viewHolder = viewHolders.First(vh => vh.ItemView == view.Parent);
            KeyValuePair<Step, TimeStep[]> stepTime = stepTimes.ElementAt(viewHolder.AdapterPosition);

            bool favorite = !stepTime.Key.Stop.Favorite;
            stepTime.Key.Stop.Favorite = favorite;

            NotifyItemChanged(viewHolder.AdapterPosition);
        }
    }
}