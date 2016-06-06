using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Android.Content;
using Android.Utilities;
using Android.Views;
using Android.Widget;

namespace TramUrWay.Android
{
    public class LinesAdapter : GenericAdapter<Line>
    {
        public LinesAdapter(IEnumerable<Line> lines) : base(lines, Resource.Layout.LineItem) { }

        protected override void OnBind(View view, Line line)
        {
            ImageView iconView = view.FindViewById<ImageView>(Resource.Id.LineItem_Icon);
            iconView.SetImageDrawable(line.GetIconDrawable(view.Context));

            TextView nameView = view.FindViewById<TextView>(Resource.Id.LineItem_Name);
            nameView.Text = line.Name;
        }
        protected override void OnClick(View view, Line line)
        {
            Intent intent = new Intent(view.Context, typeof(LineActivity));
            intent.PutExtra("Line", line.Id);

            view.Context.StartActivity(intent);
        }
    }
}