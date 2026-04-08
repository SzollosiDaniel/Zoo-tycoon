using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Zoo_tycoon
{
    internal class Animals
    {
        public string Type { get; set; }
        public int Popularity { get; set; }
        public int BuyPrice { get; set; }
        public int SellPrice { get; set; } //Buy price * 0.62
        public int Count { get; set; } //0
        public bool Active { get; set; }
        public Point Cords { get; set; }
        public int Hunger { get; set; }
        public int Health { get; set; }
        public int Relationship { get; set; }
        public Animals(string[] data)
        {
            Type = data[0];
            Popularity = Convert.ToInt32(data[1]);
            int.TryParse(data[2], out int price);
            BuyPrice = price;
            SellPrice = Convert.ToInt32(Math.Round(price * 0.62, 0));
            Count = 0;
            Active = false;
            Cords = new Point(0,0);
            Hunger = 100;
            Health = 100;
            Relationship = 0;
        }
    }
}
