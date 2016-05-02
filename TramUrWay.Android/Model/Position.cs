using System;
using System.Collections.Generic;
using System.Globalization;
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
            double latitudeDiff = (right.Latitude - left.Latitude) * PI / 180;
            double longitudeDiff = (right.Longitude - left.Longitude) * PI / 180;

            double a = Sin(latitudeDiff / 2) * Sin(latitudeDiff / 2) +
                       Cos(left.Latitude * PI / 180) * Cos(right.Latitude * PI / 180) *
                       Sin(longitudeDiff / 2) * Sin(longitudeDiff / 2);
            double c = 2 * Atan2(Sqrt(a), Sqrt(1 - a));

            return (float)(c * 6373 * 1000); // Return diff in meters
        }

        public override string ToString()
        {
            return Latitude.ToString(CultureInfo.InvariantCulture) + ", " + Longitude.ToString(CultureInfo.InvariantCulture);
        }
    }

    public class TrajectoryStep
    {
        public float Index { get; set; }
        public Position Position { get; set; }
    }
}