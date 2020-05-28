using System;
using System.IO;

namespace TeslaLogger
{
    public class Logfile
    {
        private static bool WriteToLogfile = false;
        private static string _logfilepath = null;
        private static System.Threading.Mutex mutex = new System.Threading.Mutex(false, "teslaloggerlogfile");
        static Logfile()
        {
            if (IsDocker())
            {
                WriteToLogfile = true;
            }
        }

        private static string Logfilepath
        {
            get
            {
                if (_logfilepath == null)
                {
                    _logfilepath = Path.Combine(GetExecutingPath(), "nohup.out");
                }

                return _logfilepath;
            }
        }


        public static void Log(string text)
        {
            string temp = DateTime.Now.ToString(ciDeDE) + " : " + text;
            Console.WriteLine(temp);

            if (WriteToLogfile)
            {
                try
                {
                    mutex.WaitOne();
                    File.AppendAllText(Logfilepath, temp + "\r\n");
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        public static void ExceptionWriter(Exception ex, string inhalt)
        {
            try
            {
                if (inhalt != null)
                {
                    if (inhalt.Contains("vehicle unavailable:"))
                    {
                        Log("vehicle unavailable");
                        System.Threading.Thread.Sleep(30000);

                        return;
                    }
                    else if (inhalt.Contains("upstream internal error"))
                    {
                        Log("upstream internal error");
                        System.Threading.Thread.Sleep(10000);

                        return;
                    }
                    else if (inhalt.Contains("Connection refused"))
                    {
                        Log("Connection refused");
                        System.Threading.Thread.Sleep(30000);

                        return;
                    }
                    else if (inhalt.Contains("No route to host"))
                    {
                        Log("No route to host");
                        System.Threading.Thread.Sleep(60000);

                        return;
                    }
                    else if (inhalt.Contains("You have been temporarily blocked for making too many requests!"))
                    {
                        Log("temporarily blocked for making too many requests!");
                        System.Threading.Thread.Sleep(30000);

                        return;
                    }
                }

                string temp = "";
                if (ex != null)
                {
                    temp = ex.ToString();
                }

                string prefix = GetPrefix(temp);

                if (temp.Contains("The operation has timed out"))
                {
                    Log(prefix + "HTTP Timeout");
                    System.Threading.Thread.Sleep(15000);
                    return;
                }
                if (inhalt.Contains("operation_timedout with 10s timeout for txid"))
                {
                    Log(prefix + "Mothership Timeout");
                    System.Threading.Thread.Sleep(20000);
                    return;
                }
                if (inhalt.Contains("{\"response\":null,\"error\":\"not_found\",\"error_description\":\"\"}"))
                {
                    Log(prefix + "Mothership response:null");
                    System.Threading.Thread.Sleep(20000);
                    return;
                }
                if (inhalt.Contains("502 Bad Gateway"))
                {
                    Log(prefix + "Mothership 502 Bad Gateway");
                    System.Threading.Thread.Sleep(30000);
                    return;
                }
                else if (temp.Contains("Connection refused"))
                {
                    Log(prefix + "Connection refused");
                    System.Threading.Thread.Sleep(50000);
                    return;
                }
                else if (temp.Contains("No such host is known"))
                {
                    Log(prefix + "No such host is known");
                    System.Threading.Thread.Sleep(50000);
                    return;
                }
                else if (temp.Contains("Connection timed out"))
                {
                    Log(prefix + "Connection timed out");
                    System.Threading.Thread.Sleep(50000);
                    return;
                }
                else if (temp.Contains("We're sorry, but something went wrong (500)"))
                {
                    Log(prefix + "HTTP Error 500");
                    System.Threading.Thread.Sleep(50000);
                    return;
                }
                else if (temp.Contains("Connection reset by peer"))
                {
                    Log(prefix + "Connection reset by peer");
                    System.Threading.Thread.Sleep(30000);
                    return;
                }
                else
                {

                }

                if (temp.Length > 0)
                {
                    temp += "\r\n-------------------------\r\n";
                }

                if (inhalt == null)
                {
                    temp += "NULL";
                }
                else
                {
                    temp += inhalt;
                }

                WriteException(temp);

                System.Diagnostics.Debug.WriteLine(temp);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        public static void WriteException(string temp)
        {
            string filename = "Exception/Exception_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".txt";

            string filepath = Path.Combine(GetExecutingPath(), filename);

            File.WriteAllText(filepath, temp);
        }

        public static string GetExecutingPath()
        {
            //System.IO.Directory.GetCurrentDirectory() is not returning the current path of the assembly

            System.Reflection.Assembly executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();

            string executingPath = executingAssembly.Location;

            executingPath = executingPath.Replace(executingAssembly.ManifestModule.Name, string.Empty);

            return executingPath;
        }

        public static string GetPrefix(string exception)
        {
            try
            {
                if (exception == null)
                {
                    return "";
                }
                else if (exception.Contains("TeslaLogger.WebHelper.StartStream"))
                {
                    return "StartStream: ";
                }
                else if (exception.Contains("TeslaLogger.WebHelper.IsOnline"))
                {
                    return "IsOnline: ";
                }
                else if (exception.Contains("TeslaLogger.WebHelper.isCharging"))
                {
                    return "IsCharging: ";
                }
                else if (exception.Contains("TeslaLogger.WebHelper.IsDriving"))
                {
                    return "IsDriving: ";
                }
                else if (exception.Contains("TeslaLogger.WebHelper.GetOutsideTempAsync"))
                {
                    return "GetOutsideTemp: ";
                }
            }
            catch (Exception)
            {
            }

            return "";
        }

        public static System.Globalization.CultureInfo ciDeDE = new System.Globalization.CultureInfo("de-DE");

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
                ExceptionWriter(ex, "IsDocker");
            }

            return false;
        }

    }
}
