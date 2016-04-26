using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TramUrWay.Android
{
    public class Curve
    {
        public static Curve Zero { get; } = new Curve(x => 0);
        public static Curve One { get; } = new Curve(x => 1);
        public static Curve Identity { get; } = new Curve(x => x);

        public byte[] Data { get; set; }
        public byte this[byte index]
        {
            get
            {
                return Data[index];
            }
            set
            {
                Data[index] = value;
            }
        }

        public Curve()
        {
            Data = new byte[256];
        }
        public Curve(byte[] data)
        {
            Data = data;
        }
        public Curve(Func<double, double> formula)
        {
            Data = Enumerable.Range(0, 256).Select(x => formula(x / 255.0) * 255).Select(x => (byte)(int)x).ToArray();
        }

        public float Evaluate(float value)
        {
            value *= 255;

            int left = (int)Math.Floor(value);
            int right = (int)Math.Ceiling(value);
            float factor = value - left;

            return (Data[left] + (Data[right] - Data[left]) * factor) / 255;
        }
        public byte Evaluate(byte value)
        {
            return Data[value];
        }
    }
}