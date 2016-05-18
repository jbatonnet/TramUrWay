using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;

namespace TramUrWay.Android
{
    public abstract class BaseActivity : AppCompatActivity
    {
        protected Toolbar toolbar;
        protected View toolbarShadow;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            App.Initialize(this);

            base.OnCreate(savedInstanceState);
        }

        protected virtual void OnPostCreate()
        {
            toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            if (toolbar != null)
                SetSupportActionBar(toolbar);

            toolbarShadow = FindViewById(Resource.Id.shadow);
            if (toolbarShadow != null && Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                toolbarShadow.Visibility = ViewStates.Gone;
                toolbar.Elevation = 4;
            }
        }

        protected void OnCreate(Bundle savedInstanceState, int layoutId)
        {
            OnCreate(savedInstanceState);

            SetContentView(layoutId);

            OnPostCreate();
        }
    }
}