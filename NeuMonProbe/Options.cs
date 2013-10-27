using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NeuMonProbe
{
    public class Options
    {
        private static SortedList<string, string> Params = new SortedList<string, string>();

        private static string fileName = Utils.GetFullPath("NeuMonProbe.dat");

        public static string Get(string k, string def)
        {
            if(Params.ContainsKey(k))
                return Params[k];
            else
                return def;
        }

        public static int GetInt(string k, int def)
        {
            if (Params.ContainsKey(k))
                return Convert.ToInt32(Params[k]);
            else
                return def;
        }

        public static bool GetBool(string k, bool def)
        {
            if (Params.ContainsKey(k))
                return Convert.ToBoolean(Params[k]);
            else
                return def;
        }

        public static void Set(string k, string v)
        {
            Params[k.Trim()] = v.Trim();
        }

        public static void ReadCommandLine(string[] args)
        {
            Set("mode", "commandline");

            foreach (string arg in args)
            {
                string[] fields = arg.Split('=');
                if (fields.Length == 1)
                {
                    Set(fields[0], "true");
                }
                else if (fields.Length == 2)
                {
                    Set(fields[0], fields[1]);
                }
            }
        }

        public static void Read()
        {
            if (File.Exists(fileName) == false)
                return;

            string o = System.IO.File.ReadAllText(fileName);
            string[] rows = o.Split('\n');
            foreach(string row in rows)
            {
                if (row.Trim() != "")
                {
                    string[] fields = row.Split('=');
                    if (fields.Length == 2)
                    {
                        Set(fields[0], fields[1]);
                    }
                }
            }
        }

        public static void SaveIfNotExists()
        {
            if (File.Exists(fileName) == false)
                Save();
        }

        public static void Save()
        {
            if (Get("mode", "") == "commandline")
                return;

            string o = "";
            foreach(KeyValuePair<string, string> kvp in Params)
            {
                o += kvp.Key.Trim().ToString() + "=" + kvp.Value.Trim().ToString() + "\n";
            }

            System.IO.File.WriteAllText(fileName, o);      
        }
    }
}
