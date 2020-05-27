using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;

namespace TeslaLogger
{
    internal class Program
    {
        private enum TeslaState
        {
            Start,
            Drive,
            Park,
            Charge,
            Sleep,
            WaitForSleep,
            Online,
            GoSleep
        }

        // encapsulate state
        private static TeslaState _currentState = TeslaState.Start;

        private static TeslaState GetCurrentState() { return _currentState; }

        private WebHelper wh = new WebHelper();
        private static DateTime lastCarUsed = DateTime.Now;
        private static DateTime lastOdometerChanged = DateTime.Now;
        private static DateTime lastTryTokenRefresh = DateTime.Now;
        private static bool goSleepWithWakeup = false;
        private static double odometerLastTrip;
        private static bool supercharging = false;
        private static int superchargerTics = 0;
        private const int superchargerTicsLimit = 100;

        private static void Main(string[] args)
        {
            CheckNewCredentials();

            try
            {
                IntiStage1();

                InitCheckDocker();

                InitStage2();

                InitConnectToDB();

                WebHelper wh = new WebHelper();

                InitStage3(wh);

                MQTTClient.StartMQTTClient();

                UpdateDBinBackground(wh);

                DBHelper.currentJSON.current_odometer = DBHelper.GetLatestOdometer();
                DBHelper.currentJSON.CreateCurrentJSON();

                Address lastRacingPoint = null;

                while (true)
                {
                    try
                    {
                        switch (GetCurrentState())
                        {
                            case TeslaState.Start:
                                HandleStateStart(wh);
                                break;

                            case TeslaState.Online:
                                HandleStateOnline(wh);
                                break;

                            case TeslaState.Charge:
                                HandleStateCharge(wh);
                                break;

                            case TeslaState.Sleep:
                                HandleStateSleep(wh);
                                break;

                            case TeslaState.Drive:
                                lastRacingPoint = HandleStateDrive(wh, lastRacingPoint);
                                break;

                            case TeslaState.GoSleep:
                                HandleStateGoSleep(wh);
                                break;
                            case TeslaState.Park:
                                break;
                            case TeslaState.WaitForSleep:
                                break;
                            default:
                                Logfile.Log("Main loop default reached with state: " + GetCurrentState().ToString());
                                break;
                        }

                    }
                    catch (Exception ex)
                    {
                        Logfile.ExceptionWriter(ex, "While Schleife");
                    }

                    if (WebHelper.geofence.RacingMode || (supercharging && superchargerTics < superchargerTicsLimit))
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }

            }
            catch (Exception ex)
            {
                Logfile.Log(ex.Message);
                Logfile.ExceptionWriter(ex, "While Schleife");
            }
            finally
            {
                Logfile.Log("Teslalogger Stopped!");
            }
        }

        private static void HandleStateGoSleep(WebHelper wh)
        {
            wh.ResetLastChargingState();
            bool KeepSleeping = true;
            int round = 0;

            try
            {
                while (KeepSleeping)
                {
                    round++;
                    System.Threading.Thread.Sleep(1000);
                    if (System.IO.File.Exists(FileManager.GetFilePath(TLFilename.WakeupFilename)))
                    {

                        if (wh.DeleteWakeupFile())
                        {
                            string wakeup = wh.Wakeup().Result;
                        }

                        KeepSleeping = false;
                        SetCurrentState(TeslaState.Start);
                        break;
                    }
                    else if (round > 10)
                    {
                        round = 0;

                        if (wh.TaskerWakeupfile())
                        {
                            if (wh.DeleteWakeupFile())
                            {
                                string wakeup = wh.Wakeup().Result;
                            }

                            KeepSleeping = false;
                            SetCurrentState(TeslaState.Start);
                            break;
                        }
                    }

                    if (goSleepWithWakeup)
                    {
                        Tools.EndSleeping(out int stopSleepingHour, out int stopSleepingMinute);

                        if (DateTime.Now.Hour == stopSleepingHour && DateTime.Now.Minute == stopSleepingMinute)
                        {
                            Logfile.Log("Stop Sleeping Timespan reached!");

                            KeepSleeping = false;
                            SetCurrentState(TeslaState.Start);
                            break;
                        }
                    }
                }
            }
            finally
            {
                Logfile.Log("Restart communication with Tesla Server!");
            }
        }

        private static Address HandleStateDrive(WebHelper wh, Address lastRacingPoint)
        {
            int t = Environment.TickCount;
            if (wh.IsDriving())
            {
                lastCarUsed = DateTime.Now;
                t = ApplicationSettings.Default.SleepPosition - 1000 - (Environment.TickCount - t);

                if (t > 0)
                {
                    System.Threading.Thread.Sleep(t); // alle 5 sek eine positionsmeldung
                }

                if (odometerLastTrip != DBHelper.currentJSON.current_odometer)
                {
                    odometerLastTrip = DBHelper.currentJSON.current_odometer;
                    lastOdometerChanged = DateTime.Now;
                }
                else
                {
                    if (wh.IsCharging(true))
                    {
                        Logfile.Log("Charging during Drive -> Finish Trip!!!");
                        DriveFinished(wh);
                    }
                    else
                    {
                        // Odometer didn't change for 600 seconds 
                        TimeSpan ts = DateTime.Now - lastOdometerChanged;
                        if (ts.TotalSeconds > 600)
                        {
                            Logfile.Log("Odometer didn't change for 600 seconds  -> Finish Trip!!!");
                            DriveFinished(wh);
                        }
                    }
                }

                if (WebHelper.geofence.RacingMode)
                {
                    Address a = WebHelper.geofence.GetPOI(DBHelper.currentJSON.latitude, DBHelper.currentJSON.longitude);
                    if (a != null)
                    {
                        if (lastRacingPoint == null)
                        {
                            lastRacingPoint = a;
                            Logfile.Log("RACING MODE: Finish Trip!");
                            DriveFinished(wh);
                        }
                    }
                    else
                    {
                        lastRacingPoint = null;
                    }
                }
            }
            else
            {
                DriveFinished(wh);

                ShareData sd = new ShareData(wh.TaskerHash);
                sd.SendAllChargingData();
            }

            return lastRacingPoint;
        }

        private static void HandleStateSleep(WebHelper wh)
        {
            string res = wh.IsOnline().Result;

            if (res == "online")
            {
                Logfile.Log(res);
                SetCurrentState(TeslaState.Start);

                wh.IsDriving(true); // Positionsmeldung in DB für Wechsel
            }
            else
            {
                System.Threading.Thread.Sleep(10000);
            }
        }

        private static void HandleStateCharge(WebHelper wh)
        {
            {
                if (!wh.IsCharging())
                {
                    // TODO: ende des ladens in die datenbank schreiben
                    SetCurrentState(TeslaState.Start);
                    wh.IsDriving(true);
                }
                else
                {
                    lastCarUsed = DateTime.Now;
                    // Logfile.Log(res);
                    if (wh.fast_charger_brand.Equals("Tesla") && superchargerTics < superchargerTicsLimit)
                    {
                        // SuperCharger fast mode
                        Logfile.Log("Supercharging ...");
                        superchargerTics++;
                        supercharging = true;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(10000);
                    }

                    //wh.GetCachedRollupData();
                }

            }
        }

        private static void HandleStateOnline(WebHelper wh)
        {
            {
                if (wh.IsDriving() && DBHelper.currentJSON.current_speed > 0)
                {
                    wh.ResetLastChargingState();
                    lastCarUsed = DateTime.Now;
                    lastOdometerChanged = DateTime.Now;

                    Logfile.Log("Driving");
                    if (wh.scanMyTesla != null)
                    {
                        wh.scanMyTesla.FastMode(true);
                    }

                    double missingOdometer = DBHelper.currentJSON.current_odometer - odometerLastTrip;

                    if (odometerLastTrip != 0)
                    {
                        if (missingOdometer > 5)
                        {
                            Logfile.Log($"Missing: {missingOdometer} km! - Check: https://teslalogger.de/faq-1.php");
                            WriteMissingFile(missingOdometer);
                        }
                        else
                        {
                            Logfile.Log($"Missing: {missingOdometer} km");
                        }
                    }

                    // TODO: StartDriving
                    SetCurrentState(TeslaState.Drive);
                    wh.StartStreamThread(); // für altitude
                    DBHelper.StartDriveState();

                    Task.Run(() => wh.DeleteWakeupFile());
                    return;
                }
                else if (wh.IsCharging(true))
                {
                    lastCarUsed = DateTime.Now;
                    Logfile.Log("Charging");
                    if (wh.scanMyTesla != null)
                    {
                        wh.scanMyTesla.FastMode(true);
                    }

                    wh.IsDriving(true);
                    DBHelper.StartChargingState(wh);
                    SetCurrentState(TeslaState.Charge);

                    wh.DeleteWakeupFile();
                }
                else
                {
                    RefreshToken(wh);

                    Tools.StartSleeping(out int startSleepHour, out int startSleepMinute);
                    bool sleep = true;

                    if (FileManager.CheckCmdGoSleepFile())
                    {
                        Logfile.Log("STOP communication with Tesla Server to enter sleep Mode! (Sleep Button)  https://teslalogger.de/faq-1.php");
                        SetCurrentState(TeslaState.GoSleep);
                        goSleepWithWakeup = false;
                    }
                    else if (DateTime.Now.Hour == startSleepHour && DateTime.Now.Minute == startSleepMinute)
                    {
                        Logfile.Log("STOP communication with Tesla Server to enter sleep Mode! (Timespan Sleep Mode)  https://teslalogger.de/faq-1.php");
                        SetCurrentState(TeslaState.GoSleep);
                        goSleepWithWakeup = true;
                    }
                    else
                    {
                        // wenn er 15 min online war und nicht geladen oder gefahren ist, dann muss man ihn die möglichkeit geben offline zu gehen
                        TimeSpan ts = DateTime.Now - lastCarUsed;
                        if (ts.TotalMinutes > ApplicationSettings.Default.KeepOnlineMinAfterUsage)
                        {
                            SetCurrentState(TeslaState.Start);

                            wh.IsDriving(true); // kurz bevor er schlafen geht, eine Positionsmeldung speichern und schauen ob standheizung / standklima / sentry läuft.
                            if (DBHelper.currentJSON.current_is_preconditioning)
                            {
                                Logfile.Log("preconditioning prevents car to get sleep");
                                lastCarUsed = DateTime.Now;
                            }
                            else if (wh.is_sentry_mode)
                            {
                                Logfile.Log("sentry_mode prevents car to get sleep");
                                lastCarUsed = DateTime.Now;
                            }
                            else
                            {
                                try
                                {
                                    Logfile.Log("STOP communication with Tesla Server to enter sleep Mode! https://teslalogger.de/faq-1.php");

                                    for (int x = 0; x < ApplicationSettings.Default.SuspendAPIMinutes * 10; x++)
                                    {
                                        TimeSpan tsSMT = DateTime.Now - DBHelper.currentJSON.lastScanMyTeslaReceived;
                                        if (DBHelper.currentJSON.SMTSpeed > 5 &&
                                            DBHelper.currentJSON.SMTSpeed < 260 &&
                                            DBHelper.currentJSON.SMTBatteryPower > 2 &&
                                            tsSMT.TotalMinutes < 5)
                                        {
                                            Logfile.Log("ScanMyTesla prevents car to get sleep. Speed: " + DBHelper.currentJSON.SMTSpeed);
                                            lastCarUsed = DateTime.Now;
                                            string wakeup = wh.Wakeup().Result;
                                            sleep = false;
                                            break;
                                        }

                                        if (wh.ExistsWakeupFile)
                                        {
                                            Logfile.Log("Wakeupfile prevents car to get sleep");
                                            lastCarUsed = DateTime.Now;
                                            wh.DeleteWakeupFile();
                                            string wakeup = wh.Wakeup().Result;
                                            sleep = false;
                                            break;
                                        }

                                        if (x % 10 == 0)
                                        {
                                            Logfile.Log("Waiting for car to go to sleep " + (x / 10).ToString());

                                            Tools.StartSleeping(out startSleepHour, out startSleepMinute);
                                            if (DateTime.Now.Hour == startSleepHour && DateTime.Now.Minute == startSleepMinute)
                                            {
                                                Logfile.Log("STOP communication with Tesla Server to enter sleep Mode! (Timespan Sleep Mode)  https://teslalogger.de/faq-1.php");
                                                SetCurrentState(TeslaState.GoSleep);
                                                goSleepWithWakeup = true;
                                                break;
                                            }
                                        }

                                        System.Threading.Thread.Sleep(1000 * 6);
                                    }
                                }
                                finally
                                {
                                    if (!goSleepWithWakeup)
                                    {
                                        Logfile.Log("Restart communication with Tesla Server!");
                                    }
                                }
                            }
                        }
                    }

                    if (sleep)
                    {
                        System.Threading.Thread.Sleep(5000);
                    }
                    else
                    {
                        return;
                    }
                }
            }

        }

        private static void HandleStateStart(WebHelper wh)
        {
            RefreshToken(wh);

            if (wh.scanMyTesla != null)
            {
                wh.scanMyTesla.FastMode(false);
            }

            // Alle States werden geschlossen
            DBHelper.CloseChargingState();
            DBHelper.CloseDriveState(wh.lastIsDriveTimestamp);

            string res = wh.IsOnline().Result;
            lastCarUsed = DateTime.Now;
            if (res == "online")
            {
                Logfile.Log(res);
                SetCurrentState(TeslaState.Online);
                wh.IsDriving(true);
                wh.ResetLastChargingState();
                DBHelper.StartState(res);
                return;
            }
            else if (res == "asleep")
            {
                Logfile.Log(res);
                SetCurrentState(TeslaState.Sleep);
                DBHelper.StartState(res);
                wh.ResetLastChargingState();
                DBHelper.currentJSON.CreateCurrentJSON();
            }
            else if (res == "offline")
            {
                Logfile.Log(res);
                DBHelper.StartState(res);
                DBHelper.currentJSON.CreateCurrentJSON();

                while (true)
                {
                    System.Threading.Thread.Sleep(30000);
                    string res2 = wh.IsOnline().Result;

                    if (res2 != "offline")
                    {
                        Logfile.Log("Back Online: " + res2);
                        break;
                    }

                    if (wh.TaskerWakeupfile())
                    {
                        if (wh.DeleteWakeupFile())
                        {
                            string wakeup = wh.Wakeup().Result;
                            lastCarUsed = DateTime.Now;
                        }

                        SetCurrentState(TeslaState.Start);
                        break;
                    }
                }
            }
            else
            {
                DBHelper.currentJSON.current_sleeping = false;
                DBHelper.currentJSON.current_online = false;
                DBHelper.currentJSON.CreateCurrentJSON();

                Logfile.Log("Unhandled State: " + res);
            }
        }

        private static void InitStage3(WebHelper wh)
        {
            if (!wh.RestoreToken())
            {
                wh.Tesla_token = wh.GetTokenAsync().Result;
            }

            if (wh.Tesla_token == "NULL")
            {
                ExitTeslaLogger("Tesla_token == NULL");
            }

            LogToken(wh);

            if (DBHelper.DBConnectionstring.Length == 0)
            {
                ExitTeslaLogger("DBHelper.DBConnectionstring.Length == 0");
            }

            if (wh.GetVehicles() == "NULL")
            {
                ExitTeslaLogger("wh.GetVehicles() == NULL");
            }

            string online = wh.IsOnline().Result;
            Logfile.Log("Streamingtoken: " + wh.Tesla_Streamingtoken);

            if (DBHelper.GetMaxPosid(false) == 0)
            {
                Logfile.Log("Insert first Pos");
                wh.IsDriving(true);
            }

            Logfile.Log("Country Code: " + DBHelper.UpdateCountryCode());

            DBHelper.GetEconomy_Wh_km(wh);
            wh.DeleteWakeupFile();
            string carName = wh.carSettings.Name;
            if (wh.carSettings.Raven)
            {
                carName += " Raven";
            }

            Logfile.Log("Car: " + carName + " - " + wh.carSettings.Wh_TR + " Wh/km");
            double.TryParse(wh.carSettings.Wh_TR, out DBHelper.currentJSON.Wh_TR);
            DBHelper.GetLastTrip();
            UpdateTeslalogger.Start(wh);
            UpdateTeslalogger.UpdateGrafana(wh);

            DBHelper.currentJSON.current_car_version = DBHelper.GetLastCarVersion();
        }

        private static void InitStage2()
        {
            Logfile.Log("Current Culture: " + System.Threading.Thread.CurrentThread.CurrentCulture.ToString());
            Logfile.Log("Mono Runtime: " + Tools.GetMonoRuntimeVersion());
            Logfile.Log("Grafana Version: " + Tools.GetGrafanaVersion());
            Logfile.Log("OS Version: " + Tools.GetOsVersion());

            Logfile.Log("DBConnectionstring: " + DBHelper.DBConnectionstring);

            Logfile.Log("Car#:" + ApplicationSettings.Default.Car);
            Logfile.Log("KeepOnlineMinAfterUsage: " + ApplicationSettings.Default.KeepOnlineMinAfterUsage);
            Logfile.Log("SuspendAPIMinutes: " + ApplicationSettings.Default.SuspendAPIMinutes);
            Logfile.Log("SleepPositions: " + ApplicationSettings.Default.SleepPosition);
            Logfile.Log("UseScanMyTesla: " + Tools.UseScanMyTesla());
        }

        private static void IntiStage1()
        {
            Tools.SetThread_enUS();
            UpdateTeslalogger.Chmod("nohup.out", 666, false);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            Logfile.Log("TeslaLogger Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            Logfile.Log("Logfile Version: " + System.Reflection.Assembly.GetAssembly(typeof(Logfile)).GetName().Version);
            Logfile.Log("SRTM Version: " + System.Reflection.Assembly.GetAssembly(typeof(SRTM.SRTMData)).GetName().Version);
            try
            {
                string versionpath = System.IO.Path.Combine(FileManager.GetExecutingPath(), "VERSION");
                System.IO.File.WriteAllText(versionpath, System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            }
            catch (Exception)
            { }
        }

        private static void LogToken(WebHelper wh)
        {
            // don't show full Token in Logfile
            string tempToken = wh.Tesla_token;
            if (tempToken.Length > 5)
            {
                tempToken = tempToken.Substring(0, tempToken.Length - 5);
                tempToken += "XXXXX";
            }

            Logfile.Log("TOKEN: " + tempToken);
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

                    System.Threading.Thread.Sleep(15000);
                }
            }
        }

        private static void InitCheckDocker()
        {
            try
            {
                if (Tools.IsDocker())
                {
                    Logfile.Log("Docker: YES!");

                    if (!System.IO.File.Exists("/etc/teslalogger/settings.json"))
                    {
                        Logfile.Log("Creating empty settings.json");
                        System.IO.File.AppendAllText("/etc/teslalogger/settings.json", "{\"SleepTimeSpanStart\":\"\",\"SleepTimeSpanEnd\":\"\",\"SleepTimeSpanEnable\":\"false\",\"Power\":\"hp\",\"Temperature\":\"celsius\",\"Length\":\"km\",\"Language\":\"en\",\"URL_Admin\":\"\",\"ScanMyTesla\":\"false\"}");
                        UpdateTeslalogger.Chmod("/etc/teslalogger/settings.json", 666);
                    }

                    if (!System.IO.Directory.Exists("/etc/teslalogger/backup"))
                    {
                        System.IO.Directory.CreateDirectory("/etc/teslalogger/backup");
                        UpdateTeslalogger.Chmod("/etc/teslalogger/backup", 777);
                    }

                    if (!System.IO.Directory.Exists("/etc/teslalogger/Exception"))
                    {
                        System.IO.Directory.CreateDirectory("/etc/teslalogger/Exception");
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

        private static void UpdateDBinBackground(WebHelper wh)
        {
            new System.Threading.Thread(() =>
            {
                System.Threading.Thread.Sleep(30000);

                DBHelper.UpdateElevationForAllPoints();
                WebHelper.UpdateAllPOIAddresses();
                DBHelper.CheckForInterruptedCharging(true);
                wh.UpdateAllEmptyAddresses();
                DBHelper.UpdateIncompleteTrips();
                DBHelper.UpdateAllChargingMaxPower();

                ShareData sd = new ShareData(wh.TaskerHash);
                sd.SendAllChargingData();
                sd.SendDegradationData();
            }).Start();
        }

        private static void WriteMissingFile(double missingOdometer)
        {
            try
            {
                string filepath = System.IO.Path.Combine(FileManager.GetExecutingPath(), "MISSINGKM");
                System.IO.File.AppendAllText(filepath, DateTime.Now.ToString(Tools.ciDeDE) + " : " + $"Missing: {missingOdometer}km!\r\n");

                UpdateTeslalogger.Chmod(filepath, 666, false);
            }
            catch (Exception)
            { }
        }

        private static void DriveFinished(WebHelper wh)
        {
            // finish trip
            SetCurrentState(TeslaState.Start);
            DBHelper.currentJSON.current_trip_end = DateTime.Now;
            DBHelper.currentJSON.current_trip_km_end = DBHelper.currentJSON.current_odometer;
            DBHelper.currentJSON.current_trip_end_range = DBHelper.currentJSON.current_ideal_battery_range_km;
            wh.StopStreaming();

            odometerLastTrip = DBHelper.currentJSON.current_odometer;
        }

        private static void CheckNewCredentials()
        {
            try
            {
                if (!System.IO.File.Exists(FileManager.GetFilePath(TLFilename.NewCredentialsFilename)))
                {
                    return;
                }

                Logfile.Log("new_credentials.json available");

                string json = System.IO.File.ReadAllText(FileManager.GetFilePath(TLFilename.NewCredentialsFilename));
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                XmlDocument doc = new XmlDocument();
                doc.Load(FileManager.GetFilePath(TLFilename.TeslaLoggerExeConfigFilename));
                XmlNodeList nodesTeslaName = doc.SelectNodes("/configuration/applicationSettings/TeslaLogger.ApplicationSettings/setting[@name='TeslaName']/value");
                nodesTeslaName.Item(0).InnerText = j["email"];

                XmlNodeList nodesTeslaPasswort = doc.SelectNodes("/configuration/applicationSettings/TeslaLogger.ApplicationSettings/setting[@name='TeslaPasswort']/value");
                nodesTeslaPasswort.Item(0).InnerText = j["password"];

                doc.Save(FileManager.GetFilePath(TLFilename.TeslaLoggerExeConfigFilename));

                if (System.IO.File.Exists(FileManager.GetFilePath(TLFilename.TeslaTokenFilename)))
                {
                    System.IO.File.Delete(FileManager.GetFilePath(TLFilename.TeslaTokenFilename));
                }

                System.IO.File.Delete(FileManager.GetFilePath(TLFilename.NewCredentialsFilename));

                ApplicationSettings.Default.Reload();

                Logfile.Log("credentials updated!");
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void RefreshToken(WebHelper wh)
        {
            TimeSpan ts = DateTime.Now - wh.lastTokenRefresh;
            if (ts.TotalDays > 9)
            {
                TimeSpan ts2 = DateTime.Now - lastTryTokenRefresh;
                if (ts2.TotalMinutes > 30)
                {
                    lastTryTokenRefresh = DateTime.Now;
                    Logfile.Log("try to get new Token");

                    string temp = wh.GetTokenAsync().Result;
                    if (temp != "NULL")
                    {
                        Logfile.Log("new Token received!");

                        wh.Tesla_token = temp;
                        wh.lastTokenRefresh = DateTime.Now;

                        // Every 10 Days send degradataion Data
                        ShareData sd = new ShareData(wh.TaskerHash);
                        sd.SendDegradationData();
                    }
                    else
                    {
                        Logfile.Log("Error getting new Token!");
                    }
                }
            }
        }

        private static void ExitTeslaLogger(string _msg, int _exitcode = 0)
        {
            Logfile.Log("Exit: " + _msg);
            Environment.Exit(_exitcode);
        }

        public static bool IsSupercharging()
        {
            return supercharging;
        }

        private static void SetCurrentState(TeslaState _newState)
        {
            if (_currentState != _newState)
            {
                HandleStateChange(_currentState, _newState);
            }
            _currentState = _newState;
        }

        // do something on state changes
        private static void HandleStateChange(TeslaState _oldState, TeslaState _newState)
        {
            Logfile.Log("change TeslaLogger state: " + _oldState.ToString() + " -> " + _newState.ToString());
            // charging -> any
            if (_oldState == TeslaState.Charge && _newState != TeslaState.Charge)
            {
                // reset supercharger mode
                supercharging = false;
                superchargerTics = 0;
            }
        }
    }
}
