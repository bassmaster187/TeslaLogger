using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Teslamate_Import
{
    class Tools
    {
        public static void Log(int id, string text)
        {
            string f = string.Format("{0}  {1,7:########} - {2}", DateTime.Now, id, text);
            System.Console.WriteLine(f);
            System.IO.File.AppendAllText("Teslamate-Logfile.txt", f + "\r\n");
        }
    }
}
