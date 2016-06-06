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
using System.Threading.Tasks;

namespace TramUrWay.Android
{
    public class HomeFavoritesFragment : TabFragment
    {
        public override string Title => "Favoris";

        private LinesAdapter linesAdapter;
        private StopsAdapter stopsAdapter;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.HomeFavoritesFragment, container, false);

            RecyclerView linesRecyclerView = view.FindViewById<RecyclerView>(Resource.Id.FavoritesFragment_FavoriteLineList);
            linesRecyclerView.SetLayoutManager(new WrapLayoutManager(Activity));
            linesRecyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));
            linesRecyclerView.SetAdapter(linesAdapter = new LinesAdapter(Enumerable.Empty<Line>()));

            RecyclerView stopsRecyclerView = view.FindViewById<RecyclerView>(Resource.Id.FavoritesFragment_FavoriteStopList);
            stopsRecyclerView.SetLayoutManager(new WrapLayoutManager(Activity));
            stopsRecyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));
            stopsRecyclerView.SetAdapter(stopsAdapter = new StopsAdapter());

            return view;
        }
        protected override void OnGotFocus()
        {
            Line[] favoriteLines = App.Config.FavoriteLines.ToArray();
            Stop[] favoriteStops = App.Config.FavoriteStops.ToArray();

            bool favorites = favoriteLines.Length > 0 || favoriteStops.Length > 0;

            View favoritesView = View.FindViewById(Resource.Id.FavoritesFragment_Favorites);
            favoritesView.Visibility = favorites ? ViewStates.Visible : ViewStates.Gone;

            View noFavoritesView = View.FindViewById(Resource.Id.FavoritesFragment_NoFavorites);
            noFavoritesView.Visibility = favorites ? ViewStates.Gone : ViewStates.Visible;

            if (favorites)
            {
                linesAdapter.Items = favoriteLines;
                stopsAdapter.Update(favoriteStops);
            }
        }
    }
}