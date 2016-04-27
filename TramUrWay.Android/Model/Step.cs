using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TramUrWay.Android
{
    public class Step
    {
        public Stop Stop { get; set; }
        public bool Partial { get; set; }
        public string Direction { get; set; }

        public TimeSpan? Duration { get; set; }
        public TrajectoryStep[] Trajectory { get; set; }
        public float Length { get; set; }
        public Curve Speed { get; set; }

        public Route Route { get; set; }

        public override string ToString()
        {
            return Stop + " (" + Direction + ")";
        }
    }
}