using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace TramUrWay.Android
{
    public class LineFragmentsAdapter : FragmentPagerAdapter
    {
        public override int Count
        {
            get
            {
                return 0;
            }
        }

        public LineFragmentsAdapter(FragmentManager fragmentManager) : base(fragmentManager)
        {
        }

        public override Fragment GetItem(int position)
        {
            throw new NotImplementedException();
        }
    }
}