using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using static System.Math;

namespace TramUrWay.Android
{
    public struct Position
    {
        public readonly float Latitude;
        public readonly float Longitude;

        public Position(float latitude, float longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public static float operator-(Position left, Position right)
        {
            float latitudeDiff = right.Latitude - left.Latitude;
            float longitudeDiff = right.Longitude - left.Longitude;

            double a = Pow(Sin(latitudeDiff / 2), 2) + Cos(left.Latitude) * Cos(right.Latitude) * Pow(Sin(longitudeDiff / 2), 2);
            double c = 2 * Atan2(Sqrt(a), Sqrt(1 - a));

            return (float)(c * 6373 * 1000); // Return diff in meters
        }
    }
}