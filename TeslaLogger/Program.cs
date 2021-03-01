using System;
using System.Data;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;

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
            try
            {
                InitDebugLogging();

                InitStage1();

                InitCheckDocker();

                InitStage2();

                InitConnectToDB();

                InitWebserver();

                InitOpenTopoDataService();

                InitStaticMapService();

                UpdateTeslalogger.StopComfortingMessagesThread();

                MQTTClient.StartMQTTClient();

                InitTLStats();

                UpdateDbInBackground();

                Logfile.Log("Init finished, now enter main loop");

                GetAllCars();

                InitNearbySuCService();
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.Message);
                Logfile.ExceptionWriter(ex, "main loop");
                Logfile.Log("Teslalogger Stopped!");
            }
            finally
            {
                if (!UpdateTeslalogger.DownloadUpdateAndInstallStarted)
                {
                    try
                    {
                        Logfile.Log("Startup doesn't sucessfully run DownloadUpdateAndInstall() - retry now!");
                        UpdateTeslalogger.DownloadUpdateAndInstall();
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.Message);
                        Logfile.ExceptionWriter(ex, "Emergency DownloadUpdateAndInstall()");
                    }
                }
            }
        }

        private static void InitNearbySuCService()
        {
            try
            {
                if (Tools.UseNearbySuCService())
                {
                    Thread threadNearbySuCService = new Thread(() =>
                    {
                        NearbySuCService.GetSingleton().Run();
                    })
                    {
                        Name = "NearbySuCServiceThread"
                    };
                    threadNearbySuCService.Start();
                }
                else
                {
                    Logfile.Log("NearbySuCService disabled (enable in settings)");
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

        }

        internal static void GetAllCars()
        {
            using (DataTable dt = DBHelper.GetCars())
            {
                foreach (DataRow r in dt.Rows)
                {
                    int id = 0;
                    try
                    {
                        id = Convert.ToInt32(r["id"]);
                        String Name = r["tesla_name"].ToString();
                        String Password = r["tesla_password"].ToString();
                        int carid = r["tesla_carid"] as Int32? ?? 0;
                        String tesla_token = r["tesla_token"] as String ?? "";
                        DateTime tesla_token_expire = r["tesla_token_expire"] as DateTime? ?? DateTime.MinValue;
                        string Model_Name = r["Model_Name"] as String ?? "";
                        string car_type = r["car_type"] as String ?? "";
                        string car_special_type = r["car_special_type"] as String ?? "";
                        string display_name = r["display_name"] as String ?? "";
                        string vin = r["vin"] as String ?? "";
                        string tasker_hash = r["tasker_hash"] as String ?? "";
                        double? wh_tr = r["wh_tr"] as double?;

                        Car car = new Car(id, Name, Password, carid, tesla_token, tesla_token_expire, Model_Name, car_type, car_special_type, display_name, vin, tasker_hash, wh_tr);
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(id + "# :" + ex.ToString());
                    }
                }
            }
        }

        private static void InitWebserver()
        {
            try
            {
                Thread threadWebserver = new Thread(() =>
                {
                    webServer = new WebServer();
                })
                {
                    Name = "WebserverThread"
                };
                threadWebserver.Start();
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void InitTLStats()
        {
            try
            {
                Thread threadTLStats = new Thread(() =>
                {
                    TLStats.GetInstance().run();
                })
                {
                    Name = "TLStatsThread"
                };
                threadTLStats.Start();
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void InitOpenTopoDataService()
        {
            try
            {
                if (Tools.UseOpenTopoData())
                {
                    Thread threadOpenTopoDataService = new Thread(() =>
                    {
                        OpenTopoDataService.GetSingleton().Run();
                    })
                    {
                        Name = "OpenTopoServiceThread"
                    };
                    threadOpenTopoDataService.Start();
                }
                else
                {
                    Logfile.Log("OpenTopoData disabled (enable in settings)");
                }
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
            try
            {
                Logfile.Log($"Free disk space: {Tools.FreeDiskSpaceMB()}mb");
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, ex.ToString());
            }
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
                // wait for DB updates
                while (!UpdateTeslalogger.Done)
                    Thread.Sleep(5000);

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

                DBHelper.UpdateCarIDNull();

                MapQuest.createAllParkingMaps();
                MapQuest.createAllChargigMaps();
                MapQuest.createAllTripMaps();

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
                // wait for DB updates
                while (!UpdateTeslalogger.Done)
                    Thread.Sleep(5000);

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

        private static void InitStaticMapService()
        {
            try
            {
                Thread threadStaticMapService = new Thread(() =>
                {
                    StaticMapService.GetSingleton().Run();
                })
                {
                    Name = "StaticMapServiceThread"
                };
                threadStaticMapService.Start();
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

        }
    }
}