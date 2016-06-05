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
    public class HomeFavoritesFragment : TabFragment
    {
        public override string Title => "Favoris";

        private RecyclerView favoriteLinesRecyclerView, favoriteStopsRecyclerView;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.HomeFavoritesFragment, container, false);

            favoriteLinesRecyclerView = view.FindViewById<RecyclerView>(Resource.Id.FavoritesFragment_FavoriteLineList);
            favoriteLinesRecyclerView.SetLayoutManager(new WrapLayoutManager(Activity));
            favoriteLinesRecyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));

            favoriteStopsRecyclerView = view.FindViewById<RecyclerView>(Resource.Id.FavoritesFragment_FavoriteStopList);
            favoriteStopsRecyclerView.SetLayoutManager(new WrapLayoutManager(Activity));
            favoriteStopsRecyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));

            return view;
        }
        public override void OnResume()
        {
            base.OnResume();
            Refresh();
        }
        protected override void OnGotFocus()
        {
            base.OnGotFocus();
            Refresh();
        }

        private new void Refresh()
        {
            if (View == null)
                return;

            bool favorites = App.Config.FavoriteLines.Any() || App.Config.FavoriteStops.Any();

            View favoritesView = View.FindViewById(Resource.Id.FavoritesFragment_Favorites);
            favoritesView.Visibility = favorites ? ViewStates.Visible : ViewStates.Gone;

            View noFavoritesView = View.FindViewById(Resource.Id.FavoritesFragment_NoFavorites);
            noFavoritesView.Visibility = favorites ? ViewStates.Gone : ViewStates.Visible;

            if (favorites)
            {
                favoriteLinesRecyclerView.SetAdapter(new LinesAdapter(App.Config.FavoriteLines));
                favoriteStopsRecyclerView.SetAdapter(new StopsAdapter(App.Config.FavoriteStops));
            }
        }
    }
}