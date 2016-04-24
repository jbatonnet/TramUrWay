using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Utilities;
using Android.Views;
using Android.Widget;

using Toolbar = Android.Support.V7.Widget.Toolbar;
using System.Reflection;
using System.ComponentModel;

namespace TramUrWay.Android
{
    [Activity(Theme = "@style/AppTheme.NoActionBar")]
    public class SettingsActivity : AppCompatActivity
    {
        public class SettingsFragment : PreferenceFragment
        {
            public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
            {
                PreferenceScreen = PreferenceManager.CreatePreferenceScreen(inflater.Context);

                foreach (PropertyInfo property in typeof(Config).GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    Preference preference = null;

                    if (property.PropertyType == typeof(bool))
                    {
                        preference = new CheckBoxPreference(PreferenceScreen.Context)
                        {
                            Checked = (bool)property.GetValue(App.Config),
                        };
                    }
                    else
                        continue;

                    preference.Key = property.Name;
                    preference.Title = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
                    preference.Summary = property.GetCustomAttribute<DescriptionAttribute>()?.Description;

                    PreferenceScreen.AddPreference(preference);
                }

                return base.OnCreateView(inflater, container, savedInstanceState);
            }
            public override void OnResume()
            {
                base.OnResume();

                for (int i = 0; i < PreferenceScreen.PreferenceCount; i++)
                {
                    Preference preference = PreferenceScreen.GetPreference(i);
                    PropertyInfo property = typeof(Config).GetProperty(preference.Key);

                    if (preference is CheckBoxPreference)
                        (preference as CheckBoxPreference).Checked = (bool)property.GetValue(App.Config);
                }
            }
            public override bool OnPreferenceTreeClick(PreferenceScreen preferenceScreen, Preference preference)
            {
                PropertyInfo property = typeof(Config).GetProperty(preference.Key);

                if (preference is CheckBoxPreference)
                    property.SetValue(App.Config, (preference as CheckBoxPreference).Checked);

                return base.OnPreferenceTreeClick(preferenceScreen, preference);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            App.Initialize(this);

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.SettingsActivity);
            Title = "Paramètres";

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            FragmentManager.BeginTransaction()
                           .Replace(Resource.Id.SettingsActivity_Fragment, new SettingsFragment())
                           .Commit();
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
    }
}