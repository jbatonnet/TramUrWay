using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TramUrWay.Android
{
    public class Stop
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Position Position { get; set; }
        public Line Line { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}