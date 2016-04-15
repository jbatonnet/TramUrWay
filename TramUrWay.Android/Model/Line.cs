using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TramUrWay.Android
{
    public class Line
    {
        private static Dictionary<Line, bool> favorites = null;

        public int Id { get; set; }
        public int? Color { get; set; }
        public string Image { get; set; }

        public string Name
        {
            get
            {
                return Id <= 5 ? $"Tramway ligne {Id}" : $"Bus ligne {Id}";
            }
        }
        public bool Favorite
        {
            get
            {
                if (favorites == null)
                {
                    Line[] favoriteLines = App.Database.GetFavoriteLines().ToArray();
                    favorites = App.Lines.ToDictionary(l => l, l => favoriteLines.Contains(l));
                }

                return favorites[this];
            }
            set
            {
                if (favorites.ContainsKey(this) && favorites[this] == value)
                    return;

                favorites[this] = value;

                if (value)
                    App.Database.AddFavoriteLine(this);
                else
                    App.Database.RemoveFavoriteLine(this);
            }
        }

        public Stop[] Stops { get; set; }
        public Route[] Routes { get; set; }
    }
}