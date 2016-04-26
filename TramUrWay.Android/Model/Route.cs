using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TramUrWay.Android
{
    public class Route
    {
        public int Id { get; set; }

        public Line Line { get; set; }
        public Step[] Steps { get; set; }

        public TimeTable TimeTable { get; set; }

        public override string ToString()
        {
            return "Line " + Line.Id + " from " + Steps.First().Stop.Name + " to " + Steps.Last().Stop.Name;
        }
    }
}