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
        private GenericStopsAdapter stopsAdapter;
        private View favoritesView, noFavoritesView;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.HomeFavoritesFragment, container, false);

            favoritesView = view.FindViewById(Resource.Id.FavoritesFragment_Favorites);
            noFavoritesView = view.FindViewById(Resource.Id.FavoritesFragment_NoFavorites);

            RecyclerView linesRecyclerView = view.FindViewById<RecyclerView>(Resource.Id.FavoritesFragment_FavoriteLineList);
            linesRecyclerView.SetLayoutManager(new WrapLayoutManager(Activity));
            linesRecyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));
            linesRecyclerView.SetAdapter(linesAdapter = new LinesAdapter(Enumerable.Empty<Line>()));

            RecyclerView stopsRecyclerView = view.FindViewById<RecyclerView>(Resource.Id.FavoritesFragment_FavoriteStopList);
            stopsRecyclerView.SetLayoutManager(new WrapLayoutManager(Activity));
            stopsRecyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));
            stopsRecyclerView.SetAdapter(stopsAdapter = new GenericStopsAdapter());

            stopsAdapter.Click += StopsAdapter_Click;

            RefreshFavorites();

            return view;
        }
        protected override async void OnGotFocus()
        {
            await Task.Run(new Action(RefreshFavorites));
        }

        private void StopsAdapter_Click(GenericAdapter<Stop> adapter, View view, Stop item)
        {
            Intent intent = new Intent(view.Context, typeof(StopActivity));
            intent.PutExtra("Stop", item.Id);
            intent.PutExtra("Line", item.Line.Id);

            view.Context.StartActivity(intent);
        }

        private void RefreshFavorites()
        {
            foreach (Line line in TramUrWayApplication.Lines)
                line.Loaded.WaitOne();

            Line[] favoriteLines = TramUrWayApplication.Config.FavoriteLines.ToArray();
            Stop[] favoriteStops = TramUrWayApplication.Config.FavoriteStops.GroupBy(s => Utils.Hash(s.Line.Id, s.Name))
                                                                            .Select(g => g.First())
                                                                            .ToArray();

            Log.Info("{0} favorite lines", favoriteLines.Length);
            Log.Info("{0} favorite stops", favoriteStops.Length);

            bool favorites = favoriteLines.Length > 0 || favoriteStops.Length > 0;

            Activity.RunOnUiThread(() =>
            {
                favoritesView.Visibility = favorites ? ViewStates.Visible : ViewStates.Gone;
                noFavoritesView.Visibility = favorites ? ViewStates.Gone : ViewStates.Visible;

                if (favorites)
                {
                    linesAdapter.Items = favoriteLines;
                    stopsAdapter.Items = favoriteStops.GroupBy(s => Utils.Hash(s.Line.Id, s.Name)).Select(g => g.First());
                }
            });
        }
    }
}