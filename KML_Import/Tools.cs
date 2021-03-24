using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KML_Import
{
    class Tools
    {
        public static string[] SmartSplit(string line, char separator = ',')
        {
            var inQuotes = false;
            var token = "";
            var lines = new List<string>();
            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (inQuotes) // process string in quotes, 
                {
                    if (ch == '"')
                    {
                        if (i < line.Length - 1 && line[i + 1] == '"')
                        {
                            i++;
                            token += '"';
                        }
                        else inQuotes = false;
                    }
                    else token += ch;
                }
                else
                {
                    if (ch == '"') inQuotes = true;
                    else if (ch == separator)
                    {
                        lines.Add(token);
                        token = "";
                    }
                    else token += ch;
                }
            }
            lines.Add(token);
            return lines.ToArray();
        }

        public static void Log(int id, string text)
        {
            string f = string.Format("{0}  {1,7:########} - {2}", DateTime.Now, id, text);
            System.Console.WriteLine(f);
            System.IO.File.AppendAllText("TeslaFi-Logfile.txt", f + "\r\n");
        }

        public static bool IsDocker()
        {
            try
            {
                string filename = "/tmp/teslalogger-DOCKER";

                if (File.Exists(filename))
                {
                    return true;
                }

            }
            catch (Exception ex)
            {
                Log(0, ex.ToString());
            }

            return false;
        }
    }
}
