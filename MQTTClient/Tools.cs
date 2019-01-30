using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TeslaLogger
{
    class Tools
    {
        public static void Log(string text)
        {
            // Console.WriteLine(DateTime.Now.ToString(ciDeDE) + " : " + text);
            Console.WriteLine("MQTT : " + text);
        }
    }
}
