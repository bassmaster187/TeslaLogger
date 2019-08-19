namespace TeslaLogger
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Reflection;
    using System.Web.Script.Serialization;

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
                    else if (inhalt.Contains("No route to host"))
                    {
                        Tools.Log("No route to host");
                        System.Threading.Thread.Sleep(60000);

                        return;
                    }
                }

                string temp = "";
                if (ex != null)
                    temp = ex.ToString();

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

                if (temp.Length > 0)
                    temp += "\r\n-------------------------\r\n";

                if (inhalt == null)
                    temp += "NULL";
                else
                    temp += inhalt;
               
                FileManager.WriteException(temp);
                
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

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            try
            { 
                foreach (DirectoryInfo dir in source.GetDirectories())
                {
                    CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
                }

                foreach (FileInfo file in source.GetFiles())
                {
                    string p = Path.Combine(target.FullName, file.Name);

                    Tools.Log("Copy '" + file.FullName + "' to '" + p + "'");

                    File.Copy(file.FullName, p, true);
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
                File.Copy(srcFile, directory, true);
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
                var filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                    return;

                string json = File.ReadAllText(filePath);

                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                if (Boolean.Parse(j["SleepTimeSpanEnable"]))
                {
                    string start = j["SleepTimeSpanEnd"];
                    string[] s = start.Split(':');

                    int.TryParse(s[0], out stopSleepingHour);
                    int.TryParse(s[1], out stopSleepingMinute);
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
                var filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                    return;
               
                string json = File.ReadAllText(filePath);
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                if (IsPropertyExist(j, "SleepTimeSpanEnable") && IsPropertyExist(j, "SleepTimeSpanStart"))
                {
                    if (Boolean.Parse(j["SleepTimeSpanEnable"]))
                    {
                        string start = j["SleepTimeSpanStart"];
                        string[] s = start.Split(':');

                        if (s.Length >= 2)
                        {
                            int.TryParse(s[0], out startSleepingHour);
                            int.TryParse(s[1], out startSleepingMinutes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
            }
        }

        internal static void GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin)
        {
            power = "hp";
            temperature = "celsius";
            length = "km";
            language = "de";
            URL_Admin = "";

            try
            {
                var filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                    return;

                string json = File.ReadAllText(filePath);
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                if (IsPropertyExist(j, "Power"))
                    power = j["Power"];

                if (IsPropertyExist(j, "Temperature"))
                    temperature = j["Temperature"];

                if (IsPropertyExist(j, "Length"))
                    length = j["Length"];

                if (IsPropertyExist(j, "Language"))
                    language = j["Language"];

                if (IsPropertyExist(j, "URL_Admin"))
                {
                    if (j["URL_Admin"].ToString().Length > 0)
                        URL_Admin = j["URL_Admin"];
                }
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
            }
        }

        public static bool IsPropertyExist(dynamic settings, string name)
        {
            if (settings is IDictionary<string, object>)
                return ((IDictionary<string, object>)settings).ContainsKey(name);

            return false;
        }
    }
}
