using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Utilities;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace TramUrWay.Android
{
    public class FavoritesFragment : TabFragment
    {
        public override string Title => "Favoris";

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.FavoritesFragment, container, false);
        }

        public override void OnResume()
        {
            base.OnResume();

            bool favorites = App.Database.GetFavoriteLines().Any() || App.Database.GetFavoriteStops().Any();

            View favoritesView = View.FindViewById(Resource.Id.FavoritesFragment_Favorites);
            favoritesView.Visibility = favorites ? ViewStates.Visible : ViewStates.Gone;

            View noFavoritesView = View.FindViewById(Resource.Id.FavoritesFragment_NoFavorites);
            noFavoritesView.Visibility = favorites ? ViewStates.Gone : ViewStates.Visible;

            if (favorites)
            {
                RecyclerView recyclerView = View.FindViewById<RecyclerView>(Resource.Id.FavoritesFragment_FavoriteLineList);
                recyclerView.SetLayoutManager(new WrapLayoutManager(Activity));
                recyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));
                recyclerView.SetAdapter(new LinesAdapter(App.Database.GetFavoriteLines()));

                recyclerView = View.FindViewById<RecyclerView>(Resource.Id.FavoritesFragment_FavoriteStopList);
                recyclerView.SetLayoutManager(new WrapLayoutManager(Activity));
                recyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));
                recyclerView.SetAdapter(new StopsAdapter(App.Database.GetFavoriteStops()));
            }
        }
    }
}