using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TramUrWay.Android
{
    public enum LineType
    {
        Unknown,
        Tram,
        Bus
    }

    public class Line
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Color { get; set; }
        public LineType Type { get; set; }
        public byte[] Image { get; set; }

        public Stop[] Stops { get; set; }
        public Route[] Routes { get; set; }

        public override string ToString() => Name ?? $"{Type} ligne {Id}";
    }
}