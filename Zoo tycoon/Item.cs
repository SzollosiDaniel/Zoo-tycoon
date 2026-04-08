using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zoo_tycoon
{
    internal class Item
    {
        public string Name { get; set; }
        public int Have { get; set; }
        public List<string> For { get; set; }
        public int Heal { get; set; }
        public int Feed { get; set; }
        public int Relationship { get; set; }
        public int Price { get; set; }
        
        public Item(string line)
        {
            string[] data = line.Split(';');
            Name = data[0];
            Have = 0;
            For = data[1].Split(',').ToList();
            Heal = Convert.ToInt32(data[2]);
            Feed = Convert.ToInt32(data[3]);
            Relationship = Convert.ToInt32(data[4]);
            Price = Convert.ToInt32(data[5]);
        }
    }
}
