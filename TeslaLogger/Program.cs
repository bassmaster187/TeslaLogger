using Exceptionless;
using System;
using System.Data;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    internal class Program
    {
        public static bool VERBOSE; // defaults to false
        public static bool SQLTRACE; // defaults to false
        public static bool SQLFULLTRACE; // defaults to false
        public static int SQLTRACELIMIT = 250;
        public static int KeepOnlineMinAfterUsage = 5;
        public static int SuspendAPIMinutes = 30;
        public static DateTime uptime = DateTime.Now;

        public enum TLMemCacheKey
        {
            GetOutsideTempAsync,
            Housekeeping
        }

        private static WebServer webServer;
        private static bool OVMSStarted; // defaults to false;

        private static void Main(string[] _)
        {
            try
            {
                try
                {
                    ExceptionlessClient.Default.Startup(ApplicationSettings.Default.ExceptionlessApiKey);
                    // ExceptionlessClient.Default.Configuration.UseFileLogger("exceptionless.log");
                    ExceptionlessClient.Default.Configuration.ServerUrl = ApplicationSettings.Default.ExceptionlessServerUrl;
                    ExceptionlessClient.Default.Configuration.SetVersion(Assembly.GetExecutingAssembly().GetName().Version);

                    ExceptionlessClient.Default.CreateLog("Program", "Start " + Assembly.GetExecutingAssembly().GetName().Version, Exceptionless.Logging.LogLevel.Info).FirstCarUserID().Submit();
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }

                InitCheckNet8();

                InitDebugLogging();

                InitStage1();

                InitCheckDocker();

                InitStage2();

                InitConnectToDB();

                InitWebserver();

                InitOpenTopoDataService();

                InitStaticMapService();

                UpdateTeslalogger.StopComfortingMessagesThread();

                InitMQTT();

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
                Tools.ExternalLog("Teslalogger Stopped! " + ex.ToString());

                ex.ToExceptionless().FirstCarUserID().Submit();
                ExceptionlessClient.Default.ProcessQueueAsync();
            }
            finally
            {
                if (!UpdateTeslalogger.DownloadUpdateAndInstallStarted)
                {
                    try
                    {
                        Logfile.Log("Startup doesn't sucessfully run DownloadUpdateAndInstall() - retry now!");
                        ExceptionlessClient.Default.SubmitLog("Program", "Startup doesn't sucessfully run DownloadUpdateAndInstall() - retry now!");

                        UpdateTeslalogger.DownloadUpdateAndInstall();
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.Message);
                        // Logfile.ExceptionWriter(ex, "Emergency DownloadUpdateAndInstall()");

                        ExceptionlessClient.Default.SubmitLog("Program", "Emergency DownloadUpdateAndInstall()");
                    }
                }
            }
        }

        private static void InitCheckNet8()
        {
            try
            {
                if (File.Exists("TESLALOGGERNET8"))
                {
                    var net8version = Tools.GetNET8Version();
                    if (net8version?.Contains("8.") == true)
                    {
                        Logfile.Log("Start Teslalogger.net8");

                        // Copy settings for .net8
                        if (!Directory.Exists("data"))
                        {
                            Directory.CreateDirectory("data");
                            File.Copy("settings.json", "data/settings.json");
                            File.Copy("encryption.txt", "data/encryption.txt");
                        }

                        UpdateTeslalogger.Chmod("startnet8.sh", 777, false);

                        var p = new Process();
                        p.StartInfo.FileName = "/bin/bash";
                        p.StartInfo.Arguments = $"startnet8.sh";
                        p.StartInfo.CreateNoWindow = true;
                        p.StartInfo.UseShellExecute = false;
                        p.Start();

                        Thread.Sleep(5000);

                        Thread.CurrentThread.Abort();
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        private static void InitMQTT()
        {
            try
            {
                if(KVS.Get("MQTTSettings", out string mqttSettings) == KVS.SUCCESS)
                {
                    dynamic settings = JsonConvert.DeserializeObject(mqttSettings);
                    if (settings["mqtt_host"] > 0)
                    {
                        Thread mqttThread = new Thread(() =>
                        {
                        MQTT.GetSingleton().RunMqtt();
                        })
                        {
                            Name = "MqttThread"
                        };
                        mqttThread.Start();
                    }
                }
                else
                {
                    Logfile.Log("MQTT disabled (check settings)");
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }

        }

        private static void InitNearbySuCService()
        {
            try
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
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }

        }

        internal static void GetAllCars()
        {
            using (DataTable dt = DBHelper.GetCarsByTokenAge(true))
            {
                foreach (DataRow r in dt.Rows)
                {
                    StartCarThread(r);
                    Thread.Sleep(500);
                }
                dt.Clear();
            }
        }

        internal static void StartCarThread(DataRow r, Car.TeslaState oldCarState = Car.TeslaState.Start)
        {
            int id = 0;
            try
            {
                id = Convert.ToInt32(r["id"], Tools.ciDeDE);
                String Name = r["tesla_name"].ToString();
                String Password = r["tesla_password"].ToString();
                int car_id_in_account = r["tesla_carid"] as Int32? ?? 0;
                if (Name.StartsWith("KOMOOT:", StringComparison.Ordinal))
                {
                    string komoot_vin = Komoot.CheckVIN(id, r["vin"].ToString());
                    Komoot _komoot = new Komoot(id, Name.Replace("KOMOOT:", string.Empty), Password);
                    Thread komootThread = new Thread(() =>
                    {
                        _komoot.Run();
                    });
                    komootThread.Name = $"KomootThread_{id}";
                    Logfile.Log($"starting Komoot thread for ID {id} {Name.Replace("KOMOOT:", string.Empty)} <{komoot_vin}>");
                    komootThread.Start();
                }
                String tesla_token = r["tesla_token"] as String ?? "";
                if (tesla_token.StartsWith("OVMS:", StringComparison.Ordinal)) // OVMS Cars are not handled by Teslalogger
                {
                    if (!OVMSStarted)
                    {
                        var ovmsThread = new Thread(() =>
                        {
                            Tools.StartOVMS();
                        });
                        ovmsThread.Name = "OVMSStartThread";
                        ovmsThread.Start();
                    }

                    OVMSStarted = true;
                    return;
                }

                DateTime tesla_token_expire = r["tesla_token_expire"] as DateTime? ?? DateTime.MinValue;
                string Model_Name = r["Model_Name"] as String ?? "";
                string car_type = r["car_type"] as String ?? "";
                string car_special_type = r["car_special_type"] as String ?? "";
                string car_trim_badging = r["car_trim_badging"] as String ?? "";
                string display_name = r["display_name"] as String ?? "";
                string vin = r["vin"] as String ?? "";
                string tasker_hash = r["tasker_hash"] as String ?? "";
                double? wh_tr = r["wh_tr"] as double?;
                string wheel_type = r["wheel_type"] as String ?? "";
                bool raven = false;
                if (r["raven"] != DBNull.Value && Convert.ToInt32(r["raven"]) == 1)
                    raven = true;

                bool fleetAPI = false;
                if (r["fleetAPI"] != DBNull.Value && Convert.ToInt32(r["fleetAPI"]) == 1)
                    fleetAPI = true;

                bool virtualKey = false;
                if (r["virtualkey"] != DBNull.Value && Convert.ToInt32(r["virtualkey"]) == 1)
                    virtualKey = true;

                string access_type = "";
                if (r["access_type"] != DBNull.Value)
                    access_type = r["access_type"].ToString();

#pragma warning disable CA2000 // Objekte verwerfen, bevor Bereich verloren geht
                Car car = new Car(id, Name, Password, car_id_in_account, tesla_token, tesla_token_expire, Model_Name, car_type, car_special_type, car_trim_badging, display_name, vin, tasker_hash, wh_tr, fleetAPI, oldCarState, wheel_type);
                car.Raven = raven;
                car._virtual_key = virtualKey;
                car._access_type  = access_type;
#pragma warning restore CA2000 // Objekte verwerfen, bevor Bereich verloren geht
            }
            catch (Exception ex)
            {
                Logfile.Log(id + "# :" + ex.ToString());
            }
        }

        private static void InitWebserver()
        {
            UpdateTeslalogger.CertUpdate();

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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        private static void InitTLStats()
        {
            try
            {
                Thread threadTLStats = new Thread(() =>
                {
                    TLStats.run();
                })
                {
                    Name = "TLStatsThread"
                };
                threadTLStats.Start();
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        private static void InitDebugLogging()
        {
            if (ApplicationSettings.Default.VerboseMode)
            {
                VERBOSE = true;
                Logfile.Log("VerboseMode: ON");
            }
            if (ApplicationSettings.Default.SQLTrace)
            {
                SQLTRACE = true;
                Logfile.Log("SQLTrace: ON");
            }
        }

        private static void InitStage2()
        {
            TestEncryption();

            KeepOnlineMinAfterUsage = Tools.GetSettingsInt("KeepOnlineMinAfterUsage", ApplicationSettings.Default.KeepOnlineMinAfterUsage);
            SuspendAPIMinutes = Tools.GetSettingsInt("SuspendAPIMinutes", ApplicationSettings.Default.SuspendAPIMinutes);

            Logfile.Log("Current Culture: " + Thread.CurrentThread.CurrentCulture.ToString());
            Logfile.Log("Mono Runtime: " + Tools.GetMonoRuntimeVersion());
            ExceptionlessClient.Default.Configuration.DefaultData.Add("MonoRuntime", Tools.GetMonoRuntimeVersion());
            ExceptionlessClient.Default.Configuration.DefaultData.Add("OS", Tools.GetOsRelease());

            Logfile.Log("Grafana Version: " + Tools.GetGrafanaVersion());
            ExceptionlessClient.Default.Configuration.DefaultData.Add("GrafanaVersion", Tools.GetGrafanaVersion());

            Logfile.Log("OS Version: " + Tools.GetOsVersion());
            ExceptionlessClient.Default.Configuration.DefaultData.Add("OSVersion", Tools.GetOsVersion());

            Logfile.Log("Update Settings: " + Tools.GetOnlineUpdateSettings().ToString());
            ExceptionlessClient.Default.Configuration.DefaultData.Add("UpdateSettings", Tools.GetOnlineUpdateSettings().ToString());

            Logfile.Log("DBConnectionstring: " + DBHelper.GetDBConnectionstring(true));

            Logfile.Log("KeepOnlineMinAfterUsage: " + KeepOnlineMinAfterUsage);
            Logfile.Log("SuspendAPIMinutes: " + SuspendAPIMinutes);
            Logfile.Log("SleepPositions: " + ApplicationSettings.Default.SleepPosition);
            Logfile.Log("UseScanMyTesla: " + Tools.UseScanMyTesla());
            Logfile.Log("StreamingPos: " + Tools.StreamingPos());
            try
            {
                long freeDiskSpaceMB = Tools.FreeDiskSpaceMB();
                Logfile.Log($"Free disk space: {freeDiskSpaceMB}mb");
                if (freeDiskSpaceMB < 1000)
                {
                    Logfile.Log("Disk space is very low! trying to clean up ...");
                    Tools.LogDiskUsage();
                    Tools.CleanupBackupFolder();
                    Tools.CleanupExceptionsDir();
                    Tools.LogDiskUsage();
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, ex.ToString());
            }
        }

        private static void InitStage1()
        {
            Tools.SetThreadEnUS();
            UpdateTeslalogger.Chmod("nohup.out", 666, false);
            UpdateTeslalogger.Chmod("backup.sh", 777, false);
            UpdateTeslalogger.Chmod("TeslaLogger.exe", 755, false);

#pragma warning disable CA5364 // Verwenden Sie keine veralteten Sicherheitsprotokolle.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
#pragma warning restore CA5364 // Verwenden Sie keine veralteten Sicherheitsprotokolle.

            Logfile.Log("Runtime: " + Environment.Version.ToString());
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
            try
            {
                if (File.Exists("BRANCH"))
                {
                    var branch = File.ReadAllText("BRANCH").Trim();
                    Logfile.Log($"YOU ARE USING BRANCH: " + branch);

                    ExceptionlessClient.Default.Configuration.DefaultData.Add("Branch", branch);
                    ExceptionlessClient.Default.CreateLog("Program", "BRANCH: " + branch, Exceptionless.Logging.LogLevel.Warn).FirstCarUserID().Submit(); ;
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
                ex.ToExceptionless().FirstCarUserID().Submit();
            }

            Logfile.Log("OS: " + Tools.GetOsRelease());
        }

        static void TestEncryption()
        {
            try
            {
                var body = "jfsdoifjhoiwejgfüp9034eu7trfß90834ugf0ß9834uejpf90guj43pü09tgfuj45p90t8ugjedlkfgjd";
                var pass = StringCipher.GetPassPhrase();
                var encrypted = StringCipher.Encrypt(body);
                var decrypted = StringCipher.Decrypt(encrypted);
                if (body != decrypted)
                    Logfile.Log("Encryption doesn't work!!!");

            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                Logfile.Log(ex.ToString());
            }
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
                    if (ex.Message.Contains("Connection refused")
                        || ex.Message.Contains("Unable to connect to any of the specified MySQL hosts")
                        || ex.Message.Contains("Reading from the stream has failed."))
                    {
                        Logfile.Log($"Wait for DB ({x}/30): Connection refused.");
                    }
                    else
                    {
                        ex.ToExceptionless().FirstCarUserID().Submit();
                        Logfile.Log("DBCONNECTION " + ex.Message);
                    }

                    Thread.Sleep(15000);
                }
            }

            UpdateTeslalogger.Start();
            _ = Task.Factory.StartNew(() => {
                UpdateTeslalogger.UpdateGrafana();
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private static void InitCheckDocker()
        {
            try
            {
                if (Tools.IsDocker())
                {
                    Logfile.Log("Docker: YES!");

                    ExceptionlessClient.Default.Configuration.DefaultData.Add("Docker", true);

                    if (!File.Exists("/etc/teslalogger/settings.json"))
                    {
                        Logfile.Log("Creating empty settings.json");
                        File.AppendAllText("/etc/teslalogger/settings.json", GetDefaultConfigFileContent());
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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        public static string GetDefaultConfigFileContent()
        {
            return "{\"SleepTimeSpanStart\":\"\",\"SleepTimeSpanEnd\":\"\",\"SleepTimeSpanEnable\":\"false\",\"Power\":\"hp\",\"Temperature\":\"celsius\",\"Length\":\"km\",\"Language\":\"en\",\"URL_Admin\":\"\",\"ScanMyTesla\":\"false\"}";
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
                DBHelper.UpdateCO2();
                GeocodeCache.Cleanup();
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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        private static void UpdateDbInBackground()
        {
            // Run only once a day per version
            string kvskey = "UpdateDbInBackground";
            string check = DateTime.Now.ToString("yyyyMMdd") + "-" + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            if (KVS.Get(kvskey, out string updateDbInBackground) == KVS.SUCCESS)
            {
                if (updateDbInBackground == check)
                {
                    Logfile.Log("UpdateDbInBackground: SKIP today");
                    return;
                }
            }


            Thread DBUpdater = new Thread(() =>
            {
                try
                {
                    // wait for DB updates
                    while (!UpdateTeslalogger.Done)
                        Thread.Sleep(5000);

                    Thread.Sleep(30000);

                    DateTime start = DateTime.Now;
                    Logfile.Log("UpdateDbInBackground started");

                    DBHelper.UpdateElevationForAllPoints();
                    WebHelper.UpdateAllPOIAddresses();
                    DBHelper.DeleteDuplicateTrips();

                    for (int i = 0; i < Car.Allcars.Count; i++)
                    {
                        Car c = Car.Allcars[i];
                        c.DbHelper.CombineChangingStates();
                        c.webhelper.UpdateAllEmptyAddresses();
                        c.DbHelper.UpdateEmptyChargeEnergy();
                        c.DbHelper.UpdateEmptyUnplugDate();
                        c.DbHelper.AnalyzeChargingStates();
                        c.DbHelper.UpdateAllDriveHeightStatistics();
                    }

                    DBHelper.UpdateAllNullAmpereCharging();
                    DBHelper.UpdateIncompleteTrips();
                    DBHelper.UpdateAllChargingMaxPower();

                    for (int x = 0; x < Car.Allcars.Count; x++)
                    {
                        Car c = Car.Allcars[x];
                        ShareData sd = new ShareData(c);
                        sd.SendAllChargingData();
                        sd.SendDegradationData();
                        sd.SendAllDrivingData();
                    }

                    DBHelper.UpdateCarIDNull();

                    StaticMapService.CreateAllTripMaps();
                    StaticMapService.CreateAllChargingMaps();
                    StaticMapService.CreateAllParkingMaps();

                    // DBHelper.UpdateCO2();

                    Journeys.UpdateAllJourneys();

                    Car.LogActiveCars();

                    WebHelper.SearchFornewCars();

                    GeocodeCache.Cleanup();

                    DBHelper.MigratePosOdometerNullValues();

                    Logfile.Log("UpdateDbInBackground finished, took " + (DateTime.Now - start).TotalMilliseconds + "ms");
                    RunHousekeepingInBackground();

                    KVS.InsertOrUpdate(kvskey, check);
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    Logfile.Log(ex.ToString());
                }
            })
            {
                Priority = ThreadPriority.BelowNormal
            };
            DBUpdater.Name = "DBUpdaterThread";
            DBUpdater.Start();
        }
    }
}
