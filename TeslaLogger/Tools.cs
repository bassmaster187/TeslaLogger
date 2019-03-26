using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TeslaLogger
{
    class Tools
    {
        public static string GetPrefix(string exception)
        {
            try
            {
                if (exception == null)
                    return "";
                else if (exception.Contains("TeslaLogger.WebHelper.StartStream"))
                    return "StartStream: ";
                else if (exception.Contains("TeslaLogger.WebHelper.IsOnline"))
                    return "IsOnline: ";
                else if (exception.Contains("TeslaLogger.WebHelper.IsDriving"))
                    return "IsDriving: ";
            }
            catch (Exception)
            {
            }

            return "";
        }


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
                    else if (inhalt.Contains("Connection refused"))
                    {
                        Tools.Log("Connection refused");
                        System.Threading.Thread.Sleep(30000);

                        return;
                    }
                }

                string temp = ex.ToString();
                string prefix = GetPrefix(temp);

                if (temp.Contains("The operation has timed out"))
                {
                    Tools.Log(prefix + "HTTP Timeout");
                    System.Threading.Thread.Sleep(10000);
                    return;
                }
                else if (temp.Contains("Connection refused"))
                {
                    Tools.Log(prefix + "Connection refused");
                    System.Threading.Thread.Sleep(50000);
                    return;
                }
                else
                {

                }


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

        public static void CopyFilesRecursively(System.IO.DirectoryInfo source, System.IO.DirectoryInfo target)
        {
            try
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
            catch (Exception ex)
            {
                Tools.Log("CopyFilesRecursively Exception: " + ex.ToString());
            }
        }

        public static void CopyFile(string srcFile, string directory)
        {
            try
            {
                Tools.Log("Copy '" + srcFile + "' to '" + directory + "'");
                System.IO.File.Copy(srcFile, directory, true);
            }
            catch (Exception ex)
            {
                Tools.Log("CopyFile Exception: " + ex.ToString());
            }
        }

        internal static void EndSleeping(out int stopSleepingHour, out int stopSleepingMinute)
        {
            stopSleepingHour = -1;
            stopSleepingMinute = -1;

            try
            {
                if (!System.IO.File.Exists("settings.json"))
                    return;

                string json = System.IO.File.ReadAllText("settings.json");
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);
                if (Boolean.Parse(j["SleepTimeSpanEnable"]))
                {
                    string start = j["SleepTimeSpanEnd"];
                    string[] s = start.Split(':');

                    stopSleepingHour = int.Parse(s[0]);
                    stopSleepingMinute = int.Parse(s[1]);
                }
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
            }
        }

        internal static void StartSleeping(out int startSleepingHour, out int startSleepingMinutes)
        {
            startSleepingHour = -1;
            startSleepingMinutes = -1;

            try
            {
                if (!System.IO.File.Exists("settings.json"))
                    return;

                string json = System.IO.File.ReadAllText("settings.json");
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);
                if (Boolean.Parse(j["SleepTimeSpanEnable"]))
                {
                    string start = j["SleepTimeSpanStart"];
                    string[] s = start.Split(':');

                    startSleepingHour = int.Parse(s[0]);
                    startSleepingMinutes = int.Parse(s[1]);
                }
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
            }
        }
    }
}
