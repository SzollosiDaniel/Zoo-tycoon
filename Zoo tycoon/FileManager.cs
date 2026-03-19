using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zoo_tycoon
{
    internal class FileManager
    {
        public static List<Animals> ReadFile(string filename)
        {
            List<Animals> list = new List<Animals>();
            try
            {
                foreach (string item in File.ReadAllLines(filename, Encoding.UTF8).Skip(1))
                {
                    list.Add(new Animals(item.Split(";")));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return list;
        }
    }
}
