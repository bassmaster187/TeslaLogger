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
        public static void ExceptionWriter(Exception ex, string inhalt)
        {
            try
            {
                if (inhalt != null)
                {
                    if (inhalt.Contains("vehicle unavailable:"))
                    {
                        Tools.Log("vehicle unavailable");
                        System.Threading.Thread.Sleep(30000);

                        return;
                    }
                    else if (inhalt.Contains("upstream internal error"))
                    {
                        Tools.Log("upstream internal error");
                        System.Threading.Thread.Sleep(10000);

                        return;
                    }
                }

                string temp = ex.ToString();
                temp += "\r\n-------------------------\r\n";

                if (inhalt == null)
                    temp += "NULL";
                else
                    temp += inhalt;

                string filename = "Exception/Exception_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".txt";

                System.IO.File.WriteAllText(filename, temp);
                System.Diagnostics.Debug.WriteLine(temp);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        public static void Log(string text)
        {
            Console.WriteLine(DateTime.Now.ToString(ciDeDE) + " : " + text);
        }

        public static System.Globalization.CultureInfo ciEnUS = new System.Globalization.CultureInfo("en-US");
        public static System.Globalization.CultureInfo ciDeDE = new System.Globalization.CultureInfo("de-DE");

        public static void SetThread_enUS()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = ciEnUS;
        }

        public static string GetMonoRuntimeVersion()
        {
            Type type = Type.GetType("Mono.Runtime");
            if (type != null)
            {
                MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (displayName != null)
                    return displayName.Invoke(null, null).ToString();
            }

            return "NULL";
        }

        public static bool IsMono()
        {
            return GetMonoRuntimeVersion() != "NULL";
        }

        public static bool existsWakeupFile
        {
            get {
                return System.IO.File.Exists("wakeupteslalogger.txt");
            }
        }

        public static void DeleteWakeupFile()
        {
            if (existsWakeupFile)
            {
                Log("Delete Wakeup file");
                System.IO.File.Delete("wakeupteslalogger.txt");
            }
        }

        public static void CopyFilesRecursively(System.IO.DirectoryInfo source, System.IO.DirectoryInfo target)
        {
            foreach (System.IO.DirectoryInfo dir in source.GetDirectories())
            {
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            }

            foreach (System.IO.FileInfo file in source.GetFiles())
            {
                string p = System.IO.Path.Combine(target.FullName, file.Name);
                Tools.Log("Copy '" + file.FullName + "' to '" + p + "'");
                System.IO.File.Copy(file.FullName, p, true);
            }
        }
    }
}
