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

namespace TramUrWay.Android
{
    public class LinesFragment : Fragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.LinesFragment, container, false);
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);

            RecyclerView recyclerView = View.FindViewById<RecyclerView>(Resource.Id.LinesFragment_LineList);
            recyclerView.SetLayoutManager(new LinearLayoutManager(Activity));
            recyclerView.SetLayoutManager(new WrapLayoutManager(Activity));
            recyclerView.AddItemDecoration(new DividerItemDecoration(Activity, LinearLayoutManager.Vertical));
            recyclerView.SetAdapter(new LinesAdapter(App.Lines));
        }
    }
}