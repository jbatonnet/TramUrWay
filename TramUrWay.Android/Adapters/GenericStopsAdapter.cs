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
    public class GenericStopsAdapter : GenericAdapter<Stop>
    {
        public Position? Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        private Position? position;

        public GenericStopsAdapter(IEnumerable<Stop> stops) : base(stops, Resource.Layout.StopItem) { }

        protected override void OnBind(View view, Stop stop)
        {
            ImageView iconView = view.FindViewById<ImageView>(Resource.Id.StopItem_Icon);
            iconView.SetImageDrawable(stop.Line.GetIconDrawable(view.Context));

            TextView nameView = view.FindViewById<TextView>(Resource.Id.StopItem_Name);
            nameView.Text = stop.Name;

            TextView descriptionView = view.FindViewById<TextView>(Resource.Id.StopItem_Description);

            if (position != null)
            {
                float distance = stop.Position - position.Value;
                distance = (float)Math.Ceiling(distance / 100) * 100;

                descriptionView.Text = distance > 1000 ? ((distance / 1000) + " km") : (distance + " m");
            }
            else
                descriptionView.Text = "";

            ImageView favoriteView = view.FindViewById<ImageView>(Resource.Id.StopItem_Favorite);
            favoriteView.Click += FavoriteView_Click;
            favoriteView.SetImageResource(stop.GetIsFavorite() ? Resource.Drawable.ic_star : Resource.Drawable.ic_star_border);
            //favoriteView.Visibility = StopClick != null ? ViewStates.Gone : ViewStates.Visible;
        }
        protected override void OnClick(View view, Stop stop)
        {
            /*if (StopClick != null)
                StopClick.Invoke(this, stop.Value.First());
            else
            {
                Intent intent = new Intent(view.Context, typeof(StopActivity));
                intent.PutExtra("Stop", stop.Value.First().Id);

                view.Context.StartActivity(intent);
            }*/
        }

        private void FavoriteView_Click(object sender, EventArgs e)
        {
            
        }
    }
}