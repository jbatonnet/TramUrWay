using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Utilities;
using Android.Views;
using Android.Widget;

using Toolbar = Android.Support.V7.Widget.Toolbar;
using SearchView = Android.Support.V7.Widget.SearchView;

namespace TramUrWay.Android
{
    [Register("net.thedju.TramUrWay.StepWidgetActivity")]
    [IntentFilter(new[] { AppWidgetManager.ActionAppwidgetConfigure })]
    [Activity(Theme = "@style/AppTheme.NoActionBar")]
    public class StepWidgetActivity : AppCompatActivity
    {
        private int appWidgetId = AppWidgetManager.InvalidAppwidgetId;
        private StopsAdapter adapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.StepWidgetActivity);
            Title = "Sélectionnez une station";

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            // Check intent args
            Bundle extras = Intent.Extras;
            if (extras != null)
                appWidgetId = extras.GetInt(AppWidgetManager.ExtraAppwidgetId, AppWidgetManager.InvalidAppwidgetId);

            // If they gave us an intent without the widget id, just bail.
            if (appWidgetId == AppWidgetManager.InvalidAppwidgetId)
            {
                Finish();
                return;
            }

            // Initialize UI
            RecyclerView recyclerView = FindViewById<RecyclerView>(Resource.Id.StepWidgetActivity_StopList);
            recyclerView.SetLayoutManager(new WrapLayoutManager(this));
            recyclerView.AddItemDecoration(new DividerItemDecoration(this, LinearLayoutManager.Vertical));
            recyclerView.SetAdapter(adapter = new StopsAdapter(TramUrWayApplication.Lines.SelectMany(l => l.Stops)));

            // Register UI events
            adapter.StopClick += Adapter_StopClick;
        }

        public override void OnBackPressed()
        {
            SetResult(Result.Canceled);
            base.OnBackPressed();
        }
        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.StepWidgetMenu, menu);

            for (int i = 0; i < menu.Size(); i++)
            {
                IMenuItem item = menu.GetItem(i);

                if (item.ItemId == Resource.Id.StepWidgetMenu_Search)
                {
                    SearchManager searchManager = (SearchManager)GetSystemService(SearchService);

                    SearchView searchView = item.ActionView as SearchView;
                    searchView.SetSearchableInfo(searchManager.GetSearchableInfo(ComponentName));
                    searchView.QueryHint = "Rechercher";
                    searchView.QueryTextChange += SearchView_QueryTextChange;
                }
            }

            return base.OnCreateOptionsMenu(menu);
        }
        
        private void SearchView_QueryTextChange(object sender, SearchView.QueryTextChangeEventArgs e)
        {
            adapter.Filter = e.NewText;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case global::Android.Resource.Id.Home:
                    OnBackPressed();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void Adapter_StopClick(object sender, Stop stop)
        {
            Step[] steps = stop.Line.Routes.SelectMany(r => r.Steps).Where(s => s.Stop.Name == stop.Name).ToArray();

            if (steps.Length == 1)
            {
                RegisterWidget(steps[0]);
            }
            else
            {
                string[] choices = steps.Select(s => "Vers " + s.Route.Steps.Last().Stop.Name).ToArray();

                new global::Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle("Choisissez une ligne")
                    .SetSingleChoiceItems(choices, 0, (s, e) => RegisterWidget(steps[e.Which]))
                    .Show();
            }
        }
        private void RegisterWidget(Step step)
        {
            // Register the widget
            TramUrWayApplication.Config.StepWidgets.Add(appWidgetId, step);

            // Update the widget
            AppWidgetManager appWidgetManager = AppWidgetManager.GetInstance(this);
            StepWidget.Update(this, appWidgetManager, appWidgetId);

            Intent result = new Intent();
            result.PutExtra(AppWidgetManager.ExtraAppwidgetId, appWidgetId);
            SetResult(Result.Ok, result);

            Finish();
        }
    }
}