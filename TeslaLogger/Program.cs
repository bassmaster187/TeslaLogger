using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;

namespace TeslaLogger
{

    class Program
    {
        enum TeslaState
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

        static TeslaState currentState = TeslaState.Start;
        WebHelper wh = new WebHelper();
        static DateTime lastCarUsed = DateTime.Now;
        static DateTime lastOdometerChanged = DateTime.Now;
        static DateTime lastTryTokenRefresh = DateTime.Now;
        static bool goSleepWithWakeup = false;
        private static double odometerLastTrip;

        static void Main(string[] args)
        {
            CheckNewCredentials();

            try
            {
                Tools.SetThread_enUS();
                UpdateTeslalogger.chmod("nohup.out", 666, false);
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
                try
                {
                    if (Tools.IsDocker())
                    {
                        Logfile.Log("Docker: YES!");

                        if (!System.IO.File.Exists("/etc/teslalogger/settings.json"))
                        {
                            Logfile.Log("Creating empty settings.json");
                            System.IO.File.AppendAllText("/etc/teslalogger/settings.json", "{\"SleepTimeSpanStart\":\"\",\"SleepTimeSpanEnd\":\"\",\"SleepTimeSpanEnable\":\"false\",\"Power\":\"hp\",\"Temperature\":\"celsius\",\"Length\":\"km\",\"Language\":\"en\",\"URL_Admin\":\"\",\"ScanMyTesla\":\"false\"}");
                            UpdateTeslalogger.chmod("/etc/teslalogger/settings.json", 666);
                        }

                    }
                    else
                        Logfile.Log("Docker: NO!");
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }

                Logfile.Log("Current Culture: " + System.Threading.Thread.CurrentThread.CurrentCulture.ToString());
                Logfile.Log("Mono Runtime: " + Tools.GetMonoRuntimeVersion());
                Logfile.Log("Grafana Version: " + Tools.GetGrafanaVersion());

                Logfile.Log("DBConnectionstring: " + DBHelper.DBConnectionstring);

                Logfile.Log("Car#:" + ApplicationSettings.Default.Car);
                Logfile.Log("KeepOnlineMinAfterUsage: " + ApplicationSettings.Default.KeepOnlineMinAfterUsage);
                Logfile.Log("SuspendAPIMinutes: " + ApplicationSettings.Default.SuspendAPIMinutes);
                Logfile.Log("SleepPositions: " + ApplicationSettings.Default.SleepPosition);
                Logfile.Log("UseScanMyTesla: " + Tools.UseScanMyTesla());

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
                            Logfile.Log($"Wait for DB ({x}/30): Connection refused.");
                        else 
                            Logfile.Log("DBCONNECTION " + ex.Message);

                        System.Threading.Thread.Sleep(15000);
                    }
                }

                WebHelper wh = new WebHelper();

                if (!wh.RestoreToken())
                    wh.Tesla_token = wh.GetTokenAsync().Result;

                if (wh.Tesla_token == "NULL")
                    return;

                // don't show full Token in Logfile
                string tempToken = wh.Tesla_token;
                if (tempToken.Length > 5)
                {
                    tempToken = tempToken.Substring(0, tempToken.Length - 5);
                    tempToken += "XXXXX";
                }

                Logfile.Log("TOKEN: " + tempToken);

                if (DBHelper.DBConnectionstring.Length == 0)
                    return;

                if (wh.GetVehicles() == "NULL")
                    return;

                String online = wh.IsOnline().Result;
                Logfile.Log("Streamingtoken: " + wh.Tesla_Streamingtoken);

                if (DBHelper.GetMaxPosid(false) == 0)
                {
                    Logfile.Log("Insert first Pos");
                    wh.IsDriving(true);
                }

                DBHelper.GetEconomy_Wh_km(wh);
                wh.DeleteWakeupFile();
                String carName = wh.carSettings.Name;
                if (wh.carSettings.Raven)
                    carName += " Raven";

                Logfile.Log("Car: " + carName + " - " + wh.carSettings.Wh_TR + " Wh/km");
                double.TryParse(wh.carSettings.Wh_TR, out DBHelper.currentJSON.Wh_TR);
                DBHelper.GetLastTrip();
                UpdateTeslalogger.Start(wh);
                UpdateTeslalogger.UpdateGrafana(wh);

                DBHelper.currentJSON.current_car_version = DBHelper.GetLastCarVersion();

                MQTTClient.StartMQTTClient();

                new System.Threading.Thread(() =>
                {
                    System.Threading.Thread.Sleep(30000);

                    DBHelper.UpdateElevationForAllPoints();
                    WebHelper.UpdateAllPOIAddresses();
                    DBHelper.CheckForInterruptedCharging(true);
                    wh.UpdateAllEmptyAddresses();
                    DBHelper.UpdateIncompleteTrips();
                    DBHelper.UpdateAllChargingMaxPower();

                    var sd = new ShareData(wh.TaskerHash);
                    sd.SendAllChargingData();
                    sd.SendDegradationData();
                }).Start();

                DBHelper.currentJSON.current_odometer = DBHelper.getLatestOdometer();
                DBHelper.currentJSON.CreateCurrentJSON();

                // wh.IsDriving();
                // wh.GetCachedRollupData();

                // wh.GetEnergyChartData();
                // wh.StartStreamThread(); // xxx
                // string w = wh.Wakeup().Result;

                Address lastRacingPoint = null;

                while (true)
                {
                    try
                    {
                        switch (currentState)
                        {
                            case TeslaState.Start:
                                {
                                    RefreshToken(wh);

                                    if (wh.scanMyTesla != null)
                                        wh.scanMyTesla.FastMode(false);

                                    // Alle States werden geschlossen
                                    DBHelper.CloseChargingState();
                                    DBHelper.CloseDriveState(wh.lastIsDriveTimestamp);

                                    string res = wh.IsOnline().Result;
                                    lastCarUsed = DateTime.Now;

                                    if (res == "online")
                                    {
                                        Logfile.Log(res);
                                        currentState = TeslaState.Online;
                                        wh.IsDriving(true);
                                        wh.ResetLastChargingState();
                                        DBHelper.StartState(res);
                                        continue;
                                    }
                                    else if (res == "asleep")
                                    {
                                        Logfile.Log(res);
                                        currentState = TeslaState.Sleep;
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

                                                currentState = TeslaState.Start;
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
                                break;

                            case TeslaState.Online:
                                {
                                    if (wh.IsDriving() && DBHelper.currentJSON.current_speed > 0)
                                    {
                                        wh.ResetLastChargingState();
                                        lastCarUsed = DateTime.Now;
                                        lastOdometerChanged = DateTime.Now;

                                        Logfile.Log("Driving");
                                        if (wh.scanMyTesla != null)
                                            wh.scanMyTesla.FastMode(true);

                                        double missingOdometer = DBHelper.currentJSON.current_odometer - odometerLastTrip;

                                        if (odometerLastTrip != 0)
                                        {
                                            if (missingOdometer > 5)
                                            {
                                                Logfile.Log($"Missing: {missingOdometer} km! - Check: https://teslalogger.de/faq-1.php");
                                                WriteMissingFile(missingOdometer);
                                            }
                                            else
                                                Logfile.Log($"Missing: {missingOdometer} km");
                                        }

                                        // TODO: StartDriving
                                        currentState = TeslaState.Drive;
                                        wh.StartStreamThread(); // für altitude
                                        DBHelper.StartDriveState();

                                        Task.Run(() => wh.DeleteWakeupFile());
                                        continue;
                                    }
                                    else if (wh.isCharging())
                                    {
                                        lastCarUsed = DateTime.Now;
                                        Logfile.Log("Charging");
                                        if (wh.scanMyTesla != null)
                                            wh.scanMyTesla.FastMode(true);

                                        wh.IsDriving(true);
                                        DBHelper.StartChargingState(wh);
                                        currentState = TeslaState.Charge;

                                        wh.DeleteWakeupFile();
                                    }
                                    else
                                    {
                                        RefreshToken(wh);

                                        int startSleepHour, startSleepMinute;
                                        Tools.StartSleeping(out startSleepHour, out startSleepMinute);
                                        bool sleep = true;

                                        if (FileManager.CheckCmdGoSleepFile())
                                        {
                                            Logfile.Log("STOP communication with Tesla Server to enter sleep Mode! (Sleep Button)  https://teslalogger.de/faq-1.php");
                                            currentState = TeslaState.GoSleep;
                                            goSleepWithWakeup = false;
                                        }
                                        else if (DateTime.Now.Hour == startSleepHour && DateTime.Now.Minute == startSleepMinute)
                                        {
                                            Logfile.Log("STOP communication with Tesla Server to enter sleep Mode! (Timespan Sleep Mode)  https://teslalogger.de/faq-1.php");
                                            currentState = TeslaState.GoSleep;
                                            goSleepWithWakeup = true;
                                        }
                                        else
                                        {
                                            // wenn er 15 min online war und nicht geladen oder gefahren ist, dann muss man ihn die möglichkeit geben offline zu gehen
                                            TimeSpan ts = DateTime.Now - lastCarUsed;
                                            if (ts.TotalMinutes > ApplicationSettings.Default.KeepOnlineMinAfterUsage)
                                            {
                                                currentState = TeslaState.Start;

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

                                                            if (wh.existsWakeupFile)
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
                                                                    currentState = TeslaState.GoSleep;
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
                                                            Logfile.Log("Restart communication with Tesla Server!");
                                                    }
                                                }
                                            }
                                        }

                                        if (sleep)
                                            System.Threading.Thread.Sleep(5000);
                                        else
                                            continue;
                                    }
                                }
                                break;

                            case TeslaState.Charge:
                                {
                                    if (!wh.isCharging())
                                    {
                                        // TODO: ende des ladens in die datenbank schreiben
                                        currentState = TeslaState.Start;
                                        wh.IsDriving(true);
                                    }
                                    else
                                    {
                                        lastCarUsed = DateTime.Now;
                                        System.Threading.Thread.Sleep(10000);

                                        //wh.GetCachedRollupData();
                                    }

                                }
                                break;

                            case TeslaState.Sleep:
                                {
                                    string res = wh.IsOnline().Result;

                                    if (res == "online")
                                    {
                                        Logfile.Log(res);
                                        currentState = TeslaState.Start;

                                        wh.IsDriving(true); // Positionsmeldung in DB für Wechsel
                                    }
                                    else
                                    {
                                        // Logfile.Log(res);
                                        System.Threading.Thread.Sleep(10000);
                                    }
                                }
                                break;

                            case TeslaState.Drive:
                                {
                                    int t = Environment.TickCount;
                                    if (wh.IsDriving())
                                    {
                                        lastCarUsed = DateTime.Now;

                                        t = ApplicationSettings.Default.SleepPosition - 1000 - (Environment.TickCount - t);

                                        if (t > 0)
                                            System.Threading.Thread.Sleep(t); // alle 5 sek eine positionsmeldung

                                        if (odometerLastTrip != DBHelper.currentJSON.current_odometer)
                                        {
                                            odometerLastTrip = DBHelper.currentJSON.current_odometer;
                                            lastOdometerChanged = DateTime.Now;
                                        }
                                        else
                                        {
                                            if (wh.isCharging(true))
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
                                }
                                break;

                            case TeslaState.GoSleep:
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
                                                currentState = TeslaState.Start;
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
                                                    currentState = TeslaState.Start;
                                                    break;
                                                }
                                            }

                                            if (goSleepWithWakeup)
                                            {
                                                int stopSleepingHour, stopSleepingMinute;
                                                Tools.EndSleeping(out stopSleepingHour, out stopSleepingMinute);

                                                if (DateTime.Now.Hour == stopSleepingHour && DateTime.Now.Minute == stopSleepingMinute)
                                                {
                                                    Logfile.Log("Stop Sleeping Timespan reached!");

                                                    KeepSleeping = false;
                                                    currentState = TeslaState.Start;
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
                                break;

                        }

                    }
                    catch (Exception ex)
                    {
                        Logfile.ExceptionWriter(ex, "While Schleife");
                    }

                    if (WebHelper.geofence.RacingMode)
                        System.Threading.Thread.Sleep(10);
                    else
                        System.Threading.Thread.Sleep(1000);
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

        private static void WriteMissingFile(double missingOdometer)
        {
            try
            {
                string filepath = System.IO.Path.Combine(FileManager.GetExecutingPath(), "MISSINGKM");
                System.IO.File.AppendAllText(filepath, DateTime.Now.ToString(Tools.ciDeDE) + " : " + $"Missing: {missingOdometer}km!\r\n");

                UpdateTeslalogger.chmod(filepath, 666, false);
            }
            catch (Exception)
            { }
        }

        private static void DriveFinished(WebHelper wh)
        {
            // finish trip
            currentState = TeslaState.Start;
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
                    return;

                Logfile.Log("new_credentials.json available");

                string json = System.IO.File.ReadAllText(FileManager.GetFilePath(TLFilename.NewCredentialsFilename));
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                var doc = new XmlDocument();
                doc.Load(FileManager.GetFilePath(TLFilename.TeslaLoggerExeConfigFilename));
                XmlNodeList nodesTeslaName = doc.SelectNodes("/configuration/applicationSettings/TeslaLogger.ApplicationSettings/setting[@name='TeslaName']/value");
                nodesTeslaName.Item(0).InnerText = j["email"];

                XmlNodeList nodesTeslaPasswort = doc.SelectNodes("/configuration/applicationSettings/TeslaLogger.ApplicationSettings/setting[@name='TeslaPasswort']/value");
                nodesTeslaPasswort.Item(0).InnerText = j["password"];

                doc.Save(FileManager.GetFilePath(TLFilename.TeslaLoggerExeConfigFilename));

                if (System.IO.File.Exists(FileManager.GetFilePath(TLFilename.TeslaTokenFilename)))
                    System.IO.File.Delete(FileManager.GetFilePath(TLFilename.TeslaTokenFilename));

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

                    var temp = wh.GetTokenAsync().Result;
                    if (temp != "NULL")
                    {
                        Logfile.Log("new Token received!");

                        wh.Tesla_token = temp;
                        wh.lastTokenRefresh = DateTime.Now;

                        // Every 10 Days send degradataion Data
                        var sd = new ShareData(wh.TaskerHash);
                        sd.SendDegradationData();
                    }
                    else
                    {
                        Logfile.Log("Error getting new Token!");
                    }
                }
            }
        }
    }
}
