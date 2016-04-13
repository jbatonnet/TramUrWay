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
    public class TimeStepViewHolder : RecyclerView.ViewHolder
    {
        public ImageView Icon { get; }
        public TextView Name { get; }
        public TextView Description { get; }

        public TimeStepViewHolder(View itemView) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.TimeStepItem_Icon);
            Name = itemView.FindViewById<TextView>(Resource.Id.TimeStepItem_Name);
            Description = itemView.FindViewById<TextView>(Resource.Id.TimeStepItem_Description);
        }
    }

    public class TimeStepsAdapter : RecyclerView.Adapter, View.IOnClickListener
    {
        public override int ItemCount
        {
            get
            {
                return timeSteps.Length;
            }
        }

        private TimeStep[] timeSteps;
        private List<TimeStepViewHolder> viewHolders = new List<TimeStepViewHolder>();
        
        public TimeStepsAdapter(IEnumerable<TimeStep> timeSteps)
        {
            this.timeSteps = timeSteps.ToArray();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.TimeStepItem, parent, false);
            itemView.SetOnClickListener(this);

            return new TimeStepViewHolder(itemView);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            TimeStepViewHolder viewHolder = holder as TimeStepViewHolder;
            TimeStep timeStep = timeSteps[position];

            viewHolder.Icon.SetImageResource(Database.GetIconForLine(timeStep.Step.Route.Line));
            viewHolder.Name.Text = timeStep.Step.Direction.Replace("Vers ", "");
            viewHolder.Description.Text = Database.GetReadableTime(timeStep, DateTime.Now);

            if (!viewHolders.Contains(viewHolder))
                viewHolders.Add(viewHolder);
        }

        public void OnClick(View view)
        {
            TimeStepViewHolder viewHolder = viewHolders.First(vh => vh.ItemView == view);
            TimeStep timeStep = timeSteps[viewHolder.AdapterPosition];
        }
    }
}