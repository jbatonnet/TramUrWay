using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TramUrWay.Android
{
    public class Transport
    {
        public Route Route { get; set; }
        public TimeStep TimeStep { get; set; }

        public Step Step { get; set; }
        public float Progress { get; set; }
        public float NextProgress { get; set; }
    }
}