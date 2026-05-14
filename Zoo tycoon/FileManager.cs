using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Cryptography;
using System.Text.Unicode;

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

        public static List<Item> ReadItems()
        {
            List<Item> items = new List<Item>();
            try
            {
                foreach (string item in File.ReadLines("txts/items.txt", Encoding.UTF8).Skip(1))
                {
                    items.Add(new Item(item));
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return items;
        }

        public static Dictionary<string, string> ReadLogInInfos()
        {
            Dictionary<string, string> LogInInfos = new();
            try
            {
                foreach (string item in File.ReadAllLines("txts/LogInInfos.txt", Encoding.UTF8))
                {
                    string[] parts = item.Split(';');

                    if (parts.Length == 2)
                    {
                        string username = parts[0];
                        string password = parts[1];

                        LogInInfos.Add(username, password);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return LogInInfos;
        }
        public static void CreateAccount(string username, string password)
        {
            try
            {
                StreamWriter write = new StreamWriter("txts/LogInInfos.txt", true);
                UTF8Encoding utf8 = new();
                string newPassword = BitConverter.ToString(MD5.HashData(utf8.GetBytes(password)));
                write.WriteLine($"{username};{newPassword}");
                write.Close();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}
