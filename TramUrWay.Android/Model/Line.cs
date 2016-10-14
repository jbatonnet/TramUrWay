using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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
        public ManualResetEvent Loaded { get; } = new ManualResetEvent(false);

        public int Number { get; set; }
        public string Name { get; set; }
        public int? Color { get; set; }
        public LineType Type { get; set; }
        public byte[] Image { get; set; }
        public IDictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

        public Stop[] Stops { get; set; }
        public Route[] Routes { get; set; }

        public override string ToString() => Name ?? $"{Type} ligne {Number}";
    }
}