using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;

namespace TramUrWay.Android
{
    public abstract class BaseActivity : AppCompatActivity
    {
        protected AppBarLayout appBarLayout;
        protected Toolbar toolbar;
        protected View toolbarShadow;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            App.Initialize(this);
            base.OnCreate(savedInstanceState);
        }

        protected virtual void OnPostCreate()
        {
            appBarLayout = FindViewById<AppBarLayout>(Resource.Id.appbar);

            toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            if (toolbar != null)
                SetSupportActionBar(toolbar);

            toolbarShadow = FindViewById(Resource.Id.shadow);
            if (toolbarShadow != null && Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                toolbarShadow.Visibility = ViewStates.Gone;

                float elevation = 4 * Resources.DisplayMetrics.Density;

                if (appBarLayout != null)
                    appBarLayout.Elevation = elevation;
                else if (toolbar != null)
                    toolbar.Elevation = elevation;
            }
        }

        protected void OnCreate(Bundle savedInstanceState, int layoutId)
        {
            App.Initialize(this);
            base.OnCreate(savedInstanceState);

            SetContentView(layoutId);

            OnPostCreate();
        }
    }
}