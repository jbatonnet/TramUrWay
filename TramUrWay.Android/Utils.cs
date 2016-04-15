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

namespace TramUrWay.Android
{
    public static class Utils
    {
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

        public static int GetIconForLine(Line line)
        {
            switch (line.Id)
            {
                case 1: return Resource.Drawable.L1;
                case 2: return Resource.Drawable.L2;
                case 3: return Resource.Drawable.L3;
                case 4: return Resource.Drawable.L4;
                case 6: return Resource.Drawable.L6;
                case 7: return Resource.Drawable.L7;
                case 8: return Resource.Drawable.L8;
                case 9: return Resource.Drawable.L9;
                case 10: return Resource.Drawable.L10;
                case 11: return Resource.Drawable.L11;
                case 12: return Resource.Drawable.L12;
                case 13: return Resource.Drawable.L13;
                case 14: return Resource.Drawable.L14;
                case 15: return Resource.Drawable.L15;
                case 16: return Resource.Drawable.L16;
                case 17: return Resource.Drawable.L17;
                case 19: return Resource.Drawable.L19;
                default: return 0;
            }
        }
        public static int GetResourceForLine(Line line)
        {
            if (line.Id <= 5)
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
    }
}