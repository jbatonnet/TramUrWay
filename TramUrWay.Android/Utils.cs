using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;

using Android.App;
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
using Android.Utilities;
using Android.Views;
using Android.Widget;
using Android.Appwidget;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Android.Graphics.Drawables;

namespace TramUrWay.Android
{
    public static class Utils
    {
        private static Hashtable Cache { get; } = new Hashtable();

        public static int Hash(params object[] args)
        {
            int hash = 0;

            foreach (object arg in args)
            {
                hash = hash << 8 | ~hash >> 24;

                if (arg != null)
                    hash ^= arg.GetHashCode();
            }

            return hash;
        }
        public static bool Likes(string left, string right)
        {
            // ASCII normalize strings
            left = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(left.ToLowerInvariant()));
            right = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(right.ToLowerInvariant()));

            // Direct equality
            if (string.Compare(left, right, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) == 0)
                return true;

            // Begin, end
            //if (left.StartsWith(right) || left.EndsWith(right))
            //    return true;
            //if (right.StartsWith(left) || right.EndsWith(left))
            //    return true;

            return false;
        }

        public static string GetReadableTime(TimeStep timeStep, DateTime now, bool useLongStyle = true)
        {
            TimeSpan diff = timeStep.Date - now;
            int minutes = (int)diff.TotalMinutes;

            if (diff.TotalMinutes < 0)
                return "A quai";
            else if (minutes < 1)
                return "Proche";
            else if (minutes >= 60)
                return useLongStyle ? "Plus d'une heure" : "> 1 heure";
            else
            {
                if (App.Config.EnableTamBug && now.Day != timeStep.Date.Day)
                    return $"24 h {minutes} min";
                else
                    return $"{minutes} min";
            }
        }
        public static string GetReadableTimes(TimeStep[] timeSteps, DateTime now, bool useLongStyle = true)
        {
            TimeStep[] steps = new TimeStep[timeSteps.Length];

            int index = 0;

            foreach (TimeStep step in timeSteps)
            {
                steps[index] = step;

                if ((step.Date - now).TotalMinutes > 60)
                    break;

                index++;
            }

            return steps.Where(s => s != null).Join(s => GetReadableTime(s, now, useLongStyle), ", ");
        }

        public static int GetResourceForLine(Line line)
        {
            if (line.Number <= 5)
                return Resource.Drawable.ic_tram;
            else
                return Resource.Drawable.ic_directions_bus;
        }
        public static Color GetColorForLine(Context context, Line line)
        {
            Color color;

            if (line.Color != null)
            {
                color = new Color((line.Color.Value >> 16) & 0xFF, (line.Color.Value >> 8) & 0xFF, line.Color.Value & 0xFF);
                color = new Color(color.R * 4 / 5, color.G * 4 / 5, color.B * 4 / 5);
            }
            else
                color = context?.Resources?.GetColor(Resource.Color.colorAccent) ?? default(Color);

            return color;
        }

        public static Bitmap GetStopIconForLine(Context context, Line line, int stopIconSize = 16)
        {
            Color color = GetColorForLine(context, line);
            return GetStopIconForColor(context, color, stopIconSize);
        }
        public static Bitmap GetStopIconForColor(Context context, Color color, int stopIconSize = 16)
        {
            int hash = Hash(nameof(GetStopIconForColor), color.ToArgb(), stopIconSize);
            if (Cache.ContainsKey(hash))
                return Cache[hash] as Bitmap;

            float density = context.Resources.DisplayMetrics.Density;

            Paint paint = new Paint();
            paint.AntiAlias = true;

            stopIconSize = (int)(stopIconSize * density);
            Bitmap stopBitmap = Bitmap.CreateBitmap(stopIconSize, stopIconSize, Bitmap.Config.Argb8888);
            Canvas stopCanvas = new Canvas(stopBitmap);

            paint.SetARGB(color.A, color.R, color.G, color.B);
            stopCanvas.DrawCircle(stopIconSize / 2, stopIconSize / 2, stopIconSize / 2, paint);

            paint.SetARGB(0xFF, 0xFF, 0xFF, 0xFF);
            stopCanvas.DrawCircle(stopIconSize / 2, stopIconSize / 2, stopIconSize / 2 - (int)(density * (1 + stopIconSize / 16)), paint);

            Cache.Add(hash, stopBitmap);
            return stopBitmap;
        }
        public static Bitmap GetTransportIconForLine(Context context, Line line, int transportIconSize = 24)
        {
            int hash = Hash(nameof(GetTransportIconForLine), line.Id, transportIconSize);

            if (Cache.ContainsKey(hash))
                return Cache[hash] as Bitmap;

            Color color = GetColorForLine(context, line);
            float density = context.Resources.DisplayMetrics.Density;
            Paint paint = new Paint();

            transportIconSize = (int)(transportIconSize * density);
            Drawable transportDrawable = context.Resources.GetDrawable(Resource.Drawable.train);
            Drawable transportDrawableOutline = context.Resources.GetDrawable(Resource.Drawable.train_glow);

            Bitmap transportBitmap = Bitmap.CreateBitmap(transportIconSize, transportIconSize, Bitmap.Config.Argb8888);
            Canvas transportCanvas = new Canvas(transportBitmap);

            transportDrawableOutline.SetBounds(0, 0, transportIconSize, transportIconSize);
            transportDrawableOutline.Draw(transportCanvas);

            transportDrawable.SetColorFilter(color, PorterDuff.Mode.SrcIn);
            transportDrawable.SetBounds(0, 0, transportIconSize, transportIconSize);
            transportDrawable.Draw(transportCanvas);

            Cache.Add(hash, transportBitmap);
            return transportBitmap;
        }
    }
}