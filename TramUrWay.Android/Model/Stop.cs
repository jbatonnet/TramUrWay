using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TramUrWay.Android
{
    public class Stop
    {
        private static Dictionary<Stop, bool> favorites = null;

        public int Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Line Line { get; set; }

        public bool Favorite
        {
            get
            {
                if (favorites == null)
                {
                    Stop[] favoriteStops = App.Database.GetFavoriteStops().ToArray();
                    favorites = App.Lines.SelectMany(l => l.Stops).ToDictionary(s => s, s => favoriteStops.Contains(s));
                }

                return favorites[this];
            }
            set
            {
                if (favorites.ContainsKey(this) && favorites[this] == value)
                    return;

                favorites[this] = value;

                if (value)
                    App.Database.AddFavoriteStop(this);
                else
                    App.Database.RemoveFavoriteStop(this);
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}