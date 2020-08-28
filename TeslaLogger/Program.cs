using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;

namespace TeslaLogger
{
    internal class Program
    {
        public static bool VERBOSE = false;

        public enum TLMemCacheKey
        {
            GetOutsideTempAsync,
            Housekeeping
        }

        private static WebServer webServer;

        private static void Main(string[] args)
        {
            InitDebugLogging();

            try
            {
                InitStage1();

                InitCheckDocker();

                InitStage2();

                InitConnectToDB();

                InitWebserver();

                MQTTClient.StartMQTTClient();

                UpdateDbInBackground();

                Logfile.Log("Init finished, now enter main loop");

                GetFirstCar();
                GetAllCars();

            }
            catch (Exception ex)
            {
                Logfile.Log(ex.Message);
                Logfile.ExceptionWriter(ex, "main loop");
                Logfile.Log("Teslalogger Stopped!");
            }
        }

        private static void GetAllCars()
        {
            DataTable dt = DBHelper.GetCars();
            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["id"]);
                String Name = r["tesla_name"].ToString();
                String Password = r["tesla_password"].ToString();
                int carid = Convert.ToInt32(r["tesla_carid"]);
                string tesla_token = r["tesla_token"].ToString();
                DateTime tesla_token_expire = DateTime.MinValue;
                if (r["tesla_token_expire"] is DateTime)
                    tesla_token_expire = (DateTime)r["tesla_token_expire"];
                Car car = new Car(id, Name, Password, carid, tesla_token, tesla_token_expire);
            }
        }

        static Car GetFirstCar()
        {
            return null; // TODO return new Car(1, ApplicationSettings.Default.TeslaName, ApplicationSettings.Default.TeslaPasswort, ApplicationSettings.Default.Car);
        }

        private static void InitWebserver()
        {
            try
            {
                Thread threadWebserver = new Thread(() =>
                {
                    webServer = new WebServer();
                });
                threadWebserver.Name = "WebserverThread";
                threadWebserver.Start();
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void InitDebugLogging()
        {
            if (ApplicationSettings.Default.VerboseMode)
            {
                VERBOSE = true;
                Logfile.Log("VerboseMode ON");
            }
        }

        private static void InitStage2()
        {
            Logfile.Log("Current Culture: " + Thread.CurrentThread.CurrentCulture.ToString());
            Logfile.Log("Mono Runtime: " + Tools.GetMonoRuntimeVersion());
            Logfile.Log("Grafana Version: " + Tools.GetGrafanaVersion());
            Logfile.Log("OS Version: " + Tools.GetOsVersion());
            Logfile.Log("Update Settings: " + Tools.GetOnlineUpdateSettings().ToString());

            Logfile.Log("DBConnectionstring: " + DBHelper.DBConnectionstring);

            Logfile.Log("KeepOnlineMinAfterUsage: " + ApplicationSettings.Default.KeepOnlineMinAfterUsage);
            Logfile.Log("SuspendAPIMinutes: " + ApplicationSettings.Default.SuspendAPIMinutes);
            Logfile.Log("SleepPositions: " + ApplicationSettings.Default.SleepPosition);
            Logfile.Log("UseScanMyTesla: " + Tools.UseScanMyTesla());
        }

        private static void InitStage1()
        {
            Tools.SetThread_enUS();
            UpdateTeslalogger.Chmod("nohup.out", 666, false);
            UpdateTeslalogger.Chmod("backup.sh", 777, false);
            UpdateTeslalogger.Chmod("TeslaLogger.exe", 755, false);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            Logfile.Log("TeslaLogger Version: " + Assembly.GetExecutingAssembly().GetName().Version);
            Logfile.Log("Teslalogger Online Version: " + WebHelper.GetOnlineTeslaloggerVersion());
            Logfile.Log("Logfile Version: " + Assembly.GetAssembly(typeof(Logfile)).GetName().Version);
            Logfile.Log("SRTM Version: " + Assembly.GetAssembly(typeof(SRTM.SRTMData)).GetName().Version);
            try
            {
                string versionpath = Path.Combine(FileManager.GetExecutingPath(), "VERSION");
                File.WriteAllText(versionpath, Assembly.GetExecutingAssembly().GetName().Version.ToString());
            }
            catch (Exception)
            { }
        }

        private static void InitConnectToDB()
        {
            for (int x = 1; x <= 30; x++) // try 30 times until DB is up and running
            {
                try
                {
                    Logfile.Log("DB Version: " + DBHelper.GetVersion());
                    Logfile.Log("Count Pos: " + DBHelper.CountPos()); // test the DBConnection
                    break;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Connection refused"))
                    {
                        Logfile.Log($"Wait for DB ({x}/30): Connection refused.");
                    }
                    else
                    {
                        Logfile.Log("DBCONNECTION " + ex.Message);
                    }

                    Thread.Sleep(15000);
                }
            }

            UpdateTeslalogger.Start();
            UpdateTeslalogger.UpdateGrafana();
        }

        private static void InitCheckDocker()
        {
            try
            {
                if (Tools.IsDocker())
                {
                    Logfile.Log("Docker: YES!");

                    if (!File.Exists("/etc/teslalogger/settings.json"))
                    {
                        Logfile.Log("Creating empty settings.json");
                        File.AppendAllText("/etc/teslalogger/settings.json", "{\"SleepTimeSpanStart\":\"\",\"SleepTimeSpanEnd\":\"\",\"SleepTimeSpanEnable\":\"false\",\"Power\":\"hp\",\"Temperature\":\"celsius\",\"Length\":\"km\",\"Language\":\"en\",\"URL_Admin\":\"\",\"ScanMyTesla\":\"false\"}");
                        UpdateTeslalogger.Chmod("/etc/teslalogger/settings.json", 666);
                    }

                    if (!Directory.Exists("/etc/teslalogger/backup"))
                    {
                        Directory.CreateDirectory("/etc/teslalogger/backup");
                        UpdateTeslalogger.Chmod("/etc/teslalogger/backup", 777);
                    }

                    if (!Directory.Exists("/etc/teslalogger/Exception"))
                    {
                        Directory.CreateDirectory("/etc/teslalogger/Exception");
                        UpdateTeslalogger.Chmod("/etc/teslalogger/Exception", 777);
                    }
                }
                else
                {
                    Logfile.Log("Docker: NO!");
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void UpdateDbInBackground()
        {
            Thread DBUpdater = new Thread(() =>
            {
                Thread.Sleep(30000);
                DateTime start = DateTime.Now;
                Logfile.Log("UpdateDbInBackground started");
                DBHelper.UpdateElevationForAllPoints();
                WebHelper.UpdateAllPOIAddresses();
                foreach (Car c in Car.allcars)
                {
                    c.dbHelper.CheckForInterruptedCharging(true);
                    c.webhelper.UpdateAllEmptyAddresses();
                }
                DBHelper.UpdateIncompleteTrips();
                DBHelper.UpdateAllChargingMaxPower();

                foreach (Car c in Car.allcars)
                {
                    ShareData sd = new ShareData(c);
                    sd.SendAllChargingData();
                    sd.SendDegradationData();
                }

                Logfile.Log("UpdateDbInBackground finished, took " + (DateTime.Now - start).TotalMilliseconds + "ms");
                RunHousekeepingInBackground();
            })
            {
                Priority = ThreadPriority.BelowNormal
            };
            DBUpdater.Start();
        }

        internal static void RunHousekeepingInBackground()
        {
            Thread Housekeeper = new Thread(() =>
            {
                Thread.Sleep(30000);
                DateTime start = DateTime.Now;
                Logfile.Log("RunHousekeepingInBackground started");
                Tools.Housekeeping();
                Logfile.Log("RunHousekeepingInBackground finished, took " + (DateTime.Now - start).TotalMilliseconds + "ms");
            })
            {
                Priority = ThreadPriority.BelowNormal
            };
            Housekeeper.Start();
        }

        private static void ExitTeslaLogger(string _msg, int _exitcode = 0)
        {
            Logfile.Log("Exit: " + _msg);
            Environment.Exit(_exitcode);
        }
    }
}