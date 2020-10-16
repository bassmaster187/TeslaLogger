using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    public class Car
    {
        private TeslaState _currentState = TeslaState.Start;
        internal TeslaState GetCurrentState() { return _currentState; }

        private Address lastRacingPoint = null;
        internal WebHelper webhelper;

        internal enum TeslaState
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
        internal WebHelper GetWebHelper() { return webhelper; }
        private DateTime lastCarUsed = DateTime.Now;
        internal DateTime GetLastCarUsed() { return lastCarUsed; }
        private DateTime lastOdometerChanged = DateTime.Now;
        internal DateTime GetLastOdometerChanged() { return lastOdometerChanged; }
        private DateTime lastTryTokenRefresh = DateTime.Now;
        internal DateTime GetLastTryTokenRefresh() { return lastTryTokenRefresh; }
        private string lastSetChargeLimitAddressName = string.Empty;
        internal string GetLastSetChargeLimitAddressName() { return lastSetChargeLimitAddressName; }
        private bool goSleepWithWakeup = false;
        internal bool GetGoSleepWithWakeup() { return goSleepWithWakeup; }
        private double odometerLastTrip;
        internal double GetOdometerLastTrip() { return odometerLastTrip; }
        private bool highFrequencyLogging = false;
        internal bool GetHighFrequencyLogging() { return highFrequencyLogging; }
        private int highFrequencyLoggingTicks = 0;
        internal int GetHighFrequencyLoggingTicks() { return highFrequencyLoggingTicks; }
        private int highFrequencyLoggingTicksLimit = 100;
        internal int GetHighFrequencyLoggingTicksLimit() { return highFrequencyLoggingTicksLimit; }
        private DateTime highFrequencyLoggingUntil = DateTime.Now;
        internal DateTime GetHighFrequencyLoggingUntil() { return highFrequencyLoggingUntil; }
        internal enum HFLMode
        {
            Ticks,
            Time
        }
        private HFLMode highFrequencyLoggingMode = HFLMode.Ticks;
        internal HFLMode GetHighFrequencyLoggingMode() { return highFrequencyLoggingMode; }

        private Thread thread;
        private bool run = true;

        internal string TeslaName;
        internal string TeslaPasswort;
        internal string Tesla_Token;
        internal DateTime Tesla_Token_Expire;
        internal int CarInAccount;
        internal int CarInDB;

        public string ModelName;
        public bool Raven = false;
        private double _wh_TR = 0.190052356;
        public double DB_Wh_TR = 0;
        public int DB_Wh_TR_count = 0;

        public string car_type = "";
        public string car_special_type = "";
        public string trim_badging = "";

        public string Model = "";
        public string Battery = "";

        public string display_name = "";

        public string TaskerHash = "";
        public string vin = "";

        public CurrentJSON currentJSON;

        public static List<Car> allcars = new List<Car>();

        public DBHelper dbHelper;

        private TeslaAPIState teslaAPIState;

        public double Wh_TR { get => _wh_TR;
            set { 
                _wh_TR = value;
                currentJSON.Wh_TR = value;
            } 
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal TeslaAPIState GetTeslaAPIState() { return teslaAPIState; }

        public Car(int CarInDB, string TeslaName, string TeslaPasswort, int CarInAccount, string Tesla_Token, DateTime Tesla_Token_Expire, string Model_Name, string car_type, string car_special_type, string display_name, string vin, string TaskerHash, double? Wh_TR)
        {
            lock (typeof(Car))
            {
                currentJSON = new CurrentJSON(this);
                teslaAPIState = new TeslaAPIState(this);
                this.TeslaName = TeslaName;
                this.TeslaPasswort = TeslaPasswort;
                this.CarInAccount = CarInAccount;
                this.CarInDB = CarInDB;
                this.Tesla_Token = Tesla_Token;
                this.Tesla_Token_Expire = Tesla_Token_Expire;
                this.ModelName = Model_Name;
                this.car_type = car_type;
                this.car_special_type = car_special_type;
                this.display_name = display_name;
                this.vin = vin;
                this.TaskerHash = TaskerHash;
                this.Wh_TR = Wh_TR ?? 0.190;

                allcars.Add(this);

                dbHelper = new DBHelper(this);
                webhelper = new WebHelper(this);

                thread = new Thread(Loop)
                {
                    Name = "Car" + CarInDB
                };
                thread.Start();
            }
        }

        private void Loop()
        {
            try
            {
                currentJSON.current_odometer = dbHelper.GetLatestOdometer();
                currentJSON.CreateCurrentJSON();

                lock (typeof(Car))
                {
                    CheckNewCredentials();

                    InitStage3();
                }

                while (run)
                {
                    try
                    {
                        switch (GetCurrentState())
                        {
                            case TeslaState.Start:
                                HandleState_Start();
                                break;

                            case TeslaState.Online:
                                HandleState_Online();
                                break;

                            case TeslaState.Charge:
                                HandleState_Charge();
                                break;

                            case TeslaState.Sleep:
                                HandleState_Sleep();
                                break;

                            case TeslaState.Drive:
                                lastRacingPoint = HandleState_Drive(lastRacingPoint);
                                break;

                            case TeslaState.GoSleep:
                                HandleState_GoSleep();
                                break;

                            case TeslaState.Park:
                                // this state is currently unused
                                Thread.Sleep(5000);
                                break;

                            case TeslaState.WaitForSleep:
                                // this state is currently unused
                                Thread.Sleep(5000);
                                break;

                            default:
                                Log("Main loop default reached with state: " + GetCurrentState().ToString());
                                break;
                        }

                    }
                    catch (Exception ex)
                    {
                        Logfile.ExceptionWriter(ex, "#" + CarInDB + ": main loop");
                        System.Threading.Thread.Sleep(10000);
                    }
                }
            }
            catch (Exception ex)
            {
                string temp = ex.ToString();

                if (!temp.Contains("ThreadAbortException"))
                    Log(temp);
            }
            finally
            {
                Log("*** Exit Loop !!!");
            }
        }

        private void InitStage3()
        {
            try
            {
                if (!webhelper.RestoreToken())
                {
                    webhelper.Tesla_token = webhelper.GetTokenAsync().Result;
                }

                if (webhelper.Tesla_token == "NULL")
                {
                    ExitTeslaLogger("Tesla_token == NULL");
                }

                LogToken();

                if (DBHelper.DBConnectionstring.Length == 0)
                {
                    ExitTeslaLogger("DBHelper.DBConnectionstring.Length == 0");
                }

                if (webhelper.GetVehicles() == "NULL")
                {
                    ExitTeslaLogger("wh.GetVehicles() == NULL");
                }
                string online = webhelper.IsOnline().Result;
                Log("Streamingtoken: " + Tools.ObfuscateString(webhelper.Tesla_Streamingtoken));

                if (dbHelper.GetMaxPosid(false) == 0)
                {
                    Log("Insert first Pos");
                    webhelper.IsDriving(true);
                }

                Log("Country Code: " + dbHelper.UpdateCountryCode());

                dbHelper.GetEconomy_Wh_km(webhelper);
                webhelper.DeleteWakeupFile();

                if (Raven)
                {
                    ModelName += " Raven";
                }

                Log("Car: " + ModelName + " - " + Wh_TR + " Wh/km");
                dbHelper.GetLastTrip();

                currentJSON.current_car_version = dbHelper.GetLastCarVersion();
            }
            catch (Exception ex)
            {
                string temp = ex.ToString();
                if (!temp.Contains("ThreadAbortException"))
                    Log(ex.ToString());
            }
        }

        internal void ExitTeslaLogger(string v)
        {
            Log("Abort: " + v);
            run = false;
            thread.Abort();
            allcars.Remove(this);
        }

        private void HandleState_GoSleep()
        {
            webhelper.ResetLastChargingState();
            bool KeepSleeping = true;
            int round = 0;

            try
            {
                while (KeepSleeping)
                {
                    round++;
                    Thread.Sleep(1000);
                    if (File.Exists(FileManager.GetWakeupTeslaloggerPath(CarInDB)))
                    {

                        if (webhelper.DeleteWakeupFile())
                        {
                            string wakeup = webhelper.Wakeup().Result;
                        }

                        KeepSleeping = false;
                        SetCurrentState(TeslaState.Start);
                        break;
                    }
                    else if (round > 10)
                    {
                        round = 0;

                        if (webhelper.TaskerWakeupfile())
                        {
                            if (webhelper.DeleteWakeupFile())
                            {
                                string wakeup = webhelper.Wakeup().Result;
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
                            Log("Stop Sleeping Timespan reached!");

                            KeepSleeping = false;
                            SetCurrentState(TeslaState.Start);
                            break;
                        }
                    }
                }
            }
            finally
            {
                Log("Restart communication with Tesla Server! 1");
                currentJSON.current_falling_asleep = false;
                currentJSON.CreateCurrentJSON();
            }
        }

        // sleep for max 5 seconds
        private Address HandleState_Drive(Address lastRacingPoint)
        {
            int t = Environment.TickCount;
            if (webhelper.IsDriving())
            {
                lastCarUsed = DateTime.Now;
                t = ApplicationSettings.Default.SleepPosition - 1000 - (Environment.TickCount - t);

                if (t > 0)
                {
                    Thread.Sleep(t); // alle 5 sek eine positionsmeldung
                }

                if (odometerLastTrip != currentJSON.current_odometer)
                {
                    odometerLastTrip = currentJSON.current_odometer;
                    lastOdometerChanged = DateTime.Now;
                }
                else
                {
                    if (webhelper.IsCharging(true))
                    {
                        Log("Charging during Drive -> Finish Trip!!!");
                        DriveFinished();
                    }
                    else
                    {
                        // Odometer didn't change for 600 seconds 
                        TimeSpan ts = DateTime.Now - lastOdometerChanged;
                        if (ts.TotalSeconds > 600)
                        {
                            Log("Odometer didn't change for 600 seconds  -> Finish Trip!!!");
                            DriveFinished();
                        }
                    }
                }

                if (WebHelper.geofence.RacingMode)
                {
                    Address a = WebHelper.geofence.GetPOI(currentJSON.latitude, currentJSON.longitude);
                    if (a != null)
                    {
                        if (lastRacingPoint == null)
                        {
                            lastRacingPoint = a;
                            Log("RACING MODE: Finish Trip!");
                            DriveFinished();
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
                webhelper.IsDriving(true); // insert a last position. Maybe the last one is too old

                DriveFinished();

                ShareData sd = new ShareData(this);
                sd.SendAllChargingData();
            }

            return lastRacingPoint;
        }

        // if online, switch state and return
        // else sleep 10000
        private void HandleState_Sleep()
        {
            string res = webhelper.IsOnline().Result;

            if (res == "online")
            {
                //Log(res);
                SetCurrentState(TeslaState.Start);

                webhelper.IsDriving(true); // Positionsmeldung in DB für Wechsel
            }
            else
            {
                Thread.Sleep(10000);
                UpdateTeslalogger.CheckForNewVersion();
            }
        }

        // sleep 10000 unless in highFrequencyLogging mode
        private void HandleState_Charge()
        {
            {
                if (!webhelper.IsCharging())
                {
                    SetCurrentState(TeslaState.Start);
                    webhelper.IsDriving(true);
                }
                else
                {
                    lastCarUsed = DateTime.Now;
                    if (IsHighFrequenceLoggingEnabled())
                    {
                        Log("HighFrequencyLogging ...");
                    }
                    else
                    {
                        Thread.Sleep(10000);
                    }

                    //wh.GetCachedRollupData();
                }

            }
        }

        // if car is driving, switch state and return
        // else if car is charging, switch state and return
        // else if KeepOnlineMinAfterUsage is reached, sleep SuspendAPIMinutes minutes
        // else sleep 5000
        private void HandleState_Online()
        {
            {
                //if (webhelper.IsDriving() && DBHelper.currentJSON.current_speed > 0)
                if (webhelper.IsDriving() && (webhelper.GetLastShiftState().Equals("R") || webhelper.GetLastShiftState().Equals("N") || webhelper.GetLastShiftState().Equals("D")))
                {
                    webhelper.ResetLastChargingState();
                    lastCarUsed = DateTime.Now;
                    lastOdometerChanged = DateTime.Now;

                    if (webhelper.scanMyTesla != null)
                    {
                        webhelper.scanMyTesla.FastMode(true);
                    }

                    double missingOdometer = currentJSON.current_odometer - odometerLastTrip;

                    if (odometerLastTrip != 0)
                    {
                        if (missingOdometer > 5)
                        {
                            Log($"Missing: {missingOdometer} km! - Check: https://teslalogger.de/faq-1.php");
                            WriteMissingFile(missingOdometer);
                        }
                        else
                        {
                            Log($"Missing: {missingOdometer} km");
                        }
                    }

                    webhelper.StartStreamThread(); // für altitude
                    dbHelper.StartDriveState();
                    SetCurrentState(TeslaState.Drive);

                    Task.Run(() => webhelper.DeleteWakeupFile());
                    return;
                }
                else if (webhelper.IsCharging(true))
                {
                    lastCarUsed = DateTime.Now;
                    Log("Charging");
                    if (webhelper.scanMyTesla != null)
                    {
                        webhelper.scanMyTesla.FastMode(true);
                    }

                    webhelper.IsDriving(true);
                    dbHelper.StartChargingState(webhelper);
                    SetCurrentState(TeslaState.Charge);

                    webhelper.DeleteWakeupFile();
                }
                else
                {
                    RefreshToken();

                    // check sentry mode state
                    _ = webhelper.GetOdometerAsync().Result;
                    Tools.StartSleeping(out int startSleepHour, out int startSleepMinute);
                    bool doSleep = true;

                    if (FileManager.CheckCmdGoSleepFile(CarInDB))
                    {
                        Log("STOP communication with Tesla Server to enter sleep Mode! (Sleep Button)  https://teslalogger.de/faq-1.php");
                        SetCurrentState(TeslaState.GoSleep);
                        goSleepWithWakeup = false;
                    }
                    else if (DateTime.Now.Hour == startSleepHour && DateTime.Now.Minute == startSleepMinute)
                    {
                        Log("STOP communication with Tesla Server to enter sleep Mode! (Timespan Sleep Mode)  https://teslalogger.de/faq-1.php");
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

                            webhelper.IsDriving(true); // kurz bevor er schlafen geht, eine Positionsmeldung speichern und schauen ob standheizung / standklima / sentry läuft.
                            Address addr = WebHelper.geofence.GetPOI(currentJSON.latitude, currentJSON.longitude, false);
                            if (!CanFallAsleep(out string reason))
                            {
                                Log($"Reason:{reason} prevents car to get sleep");
                                lastCarUsed = DateTime.Now;
                            }
                            else if (currentJSON.current_is_preconditioning)
                            {
                                Log("preconditioning prevents car to get sleep");
                                lastCarUsed = DateTime.Now;
                            }
                            else if (webhelper.is_sentry_mode)
                            {
                                Log("sentry_mode prevents car to get sleep");
                                lastCarUsed = DateTime.Now;
                            }
                            else if (addr != null && addr.NoSleep)
                            {
                                Log($"POI {addr.name} has +nosleep");
                                lastCarUsed = DateTime.Now;
                            }
                            else
                            {
                                try
                                {
                                    Log("STOP communication with Tesla Server to enter sleep Mode! https://teslalogger.de/faq-1.php");
                                    currentJSON.current_falling_asleep = true;
                                    currentJSON.CreateCurrentJSON();

                                    for (int x = 0; x < ApplicationSettings.Default.SuspendAPIMinutes * 10; x++)
                                    {
                                        TimeSpan tsSMT = DateTime.Now - currentJSON.lastScanMyTeslaReceived;
                                        if (currentJSON.SMTSpeed > 5 &&
                                            currentJSON.SMTSpeed < 260 &&
                                            currentJSON.SMTBatteryPower > 2 &&
                                            tsSMT.TotalMinutes < 5)
                                        {
                                            Log("ScanMyTesla prevents car to get sleep. Speed: " + currentJSON.SMTSpeed);
                                            lastCarUsed = DateTime.Now;
                                            string wakeup = webhelper.Wakeup().Result;
                                            doSleep = false;
                                            break;
                                        }

                                        if (webhelper.ExistsWakeupFile)
                                        {
                                            Log("Wakeupfile prevents car to get sleep");
                                            lastCarUsed = DateTime.Now;
                                            webhelper.DeleteWakeupFile();
                                            string wakeup = webhelper.Wakeup().Result;
                                            doSleep = false;
                                            break;
                                        }

                                        if (x % 10 == 0)
                                        {
                                            Log("Waiting for car to go to sleep " + (x / 10).ToString());

                                            Tools.StartSleeping(out startSleepHour, out startSleepMinute);
                                            if (DateTime.Now.Hour == startSleepHour && DateTime.Now.Minute == startSleepMinute)
                                            {
                                                Log("STOP communication with Tesla Server to enter sleep Mode! (Timespan Sleep Mode)  https://teslalogger.de/faq-1.php");
                                                SetCurrentState(TeslaState.GoSleep);
                                                goSleepWithWakeup = true;
                                                break;
                                            }
                                        }

                                        Thread.Sleep(1000 * 6);
                                    }
                                }
                                finally
                                {
                                    if (!goSleepWithWakeup)
                                    {
                                        Log("Restart communication with Tesla Server! 2");
                                        currentJSON.current_falling_asleep = false;
                                        currentJSON.CreateCurrentJSON();
                                    }
                                }
                            }
                        }
                    }

                    currentJSON.CheckCreateCurrentJSON();

                    if (doSleep)
                    {
                        Thread.Sleep(5000);
                    }
                    else
                    {
                        return;
                    }
                }
            }

        }

        // if offline, sleep 30000
        // loop until wackup file or back online, sleep 30000 in loop
        private void HandleState_Start()
        {
            RefreshToken();

            if (webhelper.scanMyTesla != null)
            {
                webhelper.scanMyTesla.FastMode(false);
            }

            // Alle States werden geschlossen
            dbHelper.CloseChargingState();
            dbHelper.CloseDriveState(webhelper.lastIsDriveTimestamp);

            string res = webhelper.IsOnline().Result;
            lastCarUsed = DateTime.Now;
            if (res == "online")
            {
                //Log(res);
                SetCurrentState(TeslaState.Online);
                webhelper.IsDriving(true);
                webhelper.ResetLastChargingState();
                dbHelper.StartState(res);
                return;
            }
            else if (res == "asleep")
            {
                //Log(res);
                SetCurrentState(TeslaState.Sleep);
                dbHelper.StartState(res);
                webhelper.ResetLastChargingState();
                currentJSON.CreateCurrentJSON();
            }
            else if (res == "offline")
            {
                //Log(res);
                dbHelper.StartState(res);
                currentJSON.CreateCurrentJSON();

                while (true)
                {
                    Thread.Sleep(30000);
                    string res2 = webhelper.IsOnline().Result;

                    if (res2 != "offline")
                    {
                        Log("Back Online: " + res2);
                        break;
                    }

                    if (webhelper.TaskerWakeupfile())
                    {
                        if (webhelper.DeleteWakeupFile())
                        {
                            string wakeup = webhelper.Wakeup().Result;
                            lastCarUsed = DateTime.Now;
                        }

                        SetCurrentState(TeslaState.Start);
                        break;
                    }
                }
            }
            else
            {
                currentJSON.current_sleeping = false;
                currentJSON.current_online = false;
                currentJSON.CreateCurrentJSON();

                Log("Unhandled State: " + res);

                Thread.Sleep(30000); 
            }
        }


        private void LogToken()
        {
            // don't show full Token in Logfile
            string tempToken = webhelper.Tesla_token;
            if (tempToken.Length > 5)
            {
                tempToken = tempToken.Substring(0, tempToken.Length - 5);
                tempToken += "XXXXX";
            }

            //Log("TOKEN: " + tempToken);
            Log("TOKEN: " + Tools.ObfuscateString(webhelper.Tesla_token));
        }

        private void DriveFinished()
        {
            // finish trip
            SetCurrentState(TeslaState.Start);
            currentJSON.current_trip_end = DateTime.Now;
            currentJSON.current_trip_km_end = currentJSON.current_odometer;
            currentJSON.current_trip_end_range = currentJSON.current_ideal_battery_range_km;
            webhelper.StopStreaming();

            odometerLastTrip = currentJSON.current_odometer;
        }


        private void CheckNewCredentials()
        {
            /* TODO
            try
            {
                if (!File.Exists(FileManager.GetFilePath(TLFilename.NewCredentialsFilename)))
                {
                    return;
                }

                Log("new_credentials.json available");

                string json = File.ReadAllText(FileManager.GetFilePath(TLFilename.NewCredentialsFilename));
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                XmlDocument doc = new XmlDocument();
                doc.Load(FileManager.GetFilePath(TLFilename.TeslaLoggerExeConfigFilename));
                XmlNodeList nodesTeslaName = doc.SelectNodes("/configuration/applicationSettings/TeslaLogger.ApplicationSettings/setting[@name='TeslaName']/value");
                nodesTeslaName.Item(0).InnerText = j["email"];

                XmlNodeList nodesTeslaPasswort = doc.SelectNodes("/configuration/applicationSettings/TeslaLogger.ApplicationSettings/setting[@name='TeslaPasswort']/value");
                nodesTeslaPasswort.Item(0).InnerText = j["password"];

                doc.Save(FileManager.GetFilePath(TLFilename.TeslaLoggerExeConfigFilename));

                if (File.Exists(FileManager.GetFilePath(TLFilename.TeslaTokenFilename)))
                {
                    File.Delete(FileManager.GetFilePath(TLFilename.TeslaTokenFilename));
                }

                File.Delete(FileManager.GetFilePath(TLFilename.NewCredentialsFilename));

                ApplicationSettings.Default.Reload();

                Log("credentials updated!");
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            */
        }

        private void RefreshToken()
        {
            TimeSpan ts = DateTime.Now - webhelper.lastTokenRefresh;
            if (ts.TotalDays > 9)
            {
                // If car wasn't sleeping since 10 days, try to get a new Teslalogger update
                // TODO don't work anymore!
                UpdateTeslalogger.CheckForNewVersion();

                TimeSpan ts2 = DateTime.Now - lastTryTokenRefresh;
                if (ts2.TotalMinutes > 30)
                {
                    lastTryTokenRefresh = DateTime.Now;
                    Log("try to get new Token");

                    string temp = webhelper.GetTokenAsync().Result;
                    if (temp != "NULL")
                    {
                        Log("new Token received!");

                        webhelper.Tesla_token = temp;
                        webhelper.lastTokenRefresh = DateTime.Now;

                        // Every 10 Days send degradataion Data
                        ShareData sd = new ShareData(this);
                        sd.SendDegradationData();
                    }
                    else
                    {
                        Log("Error getting new Token!");
                    }
                }
            }
        }

        public void HandleShiftStateChange(string _oldState, string _newState)
        {
            Log("ShiftStateChange: " + _oldState + " -> " + _newState);
            lastCarUsed = DateTime.Now;
            Address addr = WebHelper.geofence.GetPOI(currentJSON.latitude, currentJSON.longitude, false);
            // process special flags for POI
            if (addr != null && addr.specialFlags != null && addr.specialFlags.Count > 0)
            {
                foreach (KeyValuePair<Address.SpecialFlags, string> flag in addr.specialFlags)
                {
                    switch (flag.Key)
                    {
                        case Address.SpecialFlags.OpenChargePort:
                            HandleSpecialFlag_OpenChargePort(flag.Value, _oldState, _newState);
                            break;
                        case Address.SpecialFlags.EnableSentryMode:
                            HandleSpeciaFlag_EnableSentryMode(flag.Value, _oldState, _newState);
                            break;
                        case Address.SpecialFlags.ClimateOff:
                            HandleSpeciaFlag_ClimateOff(flag.Value, _oldState, _newState);
                            break;
                        case Address.SpecialFlags.SetChargeLimit:
                        case Address.SpecialFlags.CopyChargePrice:
                        case Address.SpecialFlags.HighFrequencyLogging:
                            break;
                        default:
                            Log("handleShiftStateChange unhandled special flag " + flag.ToString());
                            break;
                    }
                }
            }
            // execute shift state change actions independant from special flags
            // TODO discuss!
            /*if (_newState.Equals("D") && DBHelper.currentJSON.current_is_sentry_mode)
            {
                Log("DisableSentryMode ...");
                string result = webhelper.PostCommand("command/set_sentry_mode?on=false", null).Result;
                Log("DisableSentryMode(): " + result);
            }*/
        }

        private void HandleSpecialFlag_HighFrequencyLogging(string _flagconfig)
        {
            string pattern = "([0-9]+)([a-z])";
            Match m = Regex.Match(_flagconfig, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1)
            {
                if (m.Groups[1].Captures[0] != null && m.Groups[2].Captures[0] != null && int.TryParse(m.Groups[1].Captures[0].ToString(), out int duration))
                {
                    DateTime until = DateTime.Now;
                    switch (m.Groups[2].Captures[0].ToString())
                    {
                        case "s":
                            until = until.AddSeconds(duration);
                            break;
                        case "m":
                            until = until.AddMinutes(duration);
                            break;
                        case "h":
                            until = until.AddHours(duration);
                            break;
                        case "d":
                            until = until.AddDays(duration);
                            break;
                        default:
                            Log("HandleSpecialFlagHighFrequencyLogging unhandled time parameter: " + m.Groups[1].Captures[0].ToString() + m.Groups[2].Captures[0].ToString());
                            break;
                    }
                    EnableHighFrequencyLoggingMode(HFLMode.Time, 0, until);
                }
            }
            else
            {
                pattern = "([0-9]+)";
                m = Regex.Match(_flagconfig, pattern);
                if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
                {
                    if (int.TryParse(m.Groups[1].Captures[0].ToString(), out int result))
                    {
                        EnableHighFrequencyLoggingMode(HFLMode.Ticks, result, DateTime.Now);
                    }
                }
            }
        }

        private void HandleSpecialFlag_OpenChargePort(string _flagconfig, string _oldState, string _newState)
        {
            string pattern = "([PRND]+)->([PRND]+)";
            Match m = Regex.Match(_flagconfig, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1 && m.Groups[1].Captures[0].ToString().Contains(_oldState) && m.Groups[2].Captures[0].ToString().Contains(_newState))
            {
                Task.Factory.StartNew(() =>
                {
                    Log("OpenChargePort ...");
                    string result = webhelper.PostCommand("command/charge_port_door_open", null).Result;
                    Log("charge_port_door_open(): " + result);
                });
            }
        }

        private void HandleSpeciaFlag_EnableSentryMode(string _flagconfig, string _oldState, string _newState)
        {
            string pattern = "([PRND]+)->([PRND]+)";
            Match m = Regex.Match(_flagconfig, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1 && m.Groups[1].Captures[0].ToString().Contains(_oldState) && m.Groups[2].Captures[0].ToString().Contains(_newState))
            {
                Task.Factory.StartNew(() =>
                {
                    Log("EnableSentryMode ...");
                    string result = webhelper.PostCommand("command/set_sentry_mode", "{\"on\":true}", true).Result;
                    Log("set_sentry_mode(): " + result);
                });
            }
        }

        private void HandleSpeciaFlag_ClimateOff(string _flagconfig, string _oldState, string _newState)
        {
            string pattern = "([PRND]+)->([PRND]+)";
            Match m = Regex.Match(_flagconfig, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1 && m.Groups[1].Captures[0].ToString().Contains(_oldState) && m.Groups[2].Captures[0].ToString().Contains(_newState))
            {
                Task.Factory.StartNew(() =>
                {
                    Log("ClimateOff ...");
                    string result = webhelper.PostCommand("command/auto_conditioning_stop", null).Result;
                    Log("auto_conditioning_stop(): " + result);
                });
            }
        }

        private void HandleSpecialFlag_SetChargeLimit(Address _addr, string _flagconfig)
        {
            string pattern = "([0-9]+)";
            Match m = Regex.Match(_flagconfig, pattern);
            if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
            {
                if (m.Groups[1].Captures[0] != null && int.TryParse(m.Groups[1].Captures[0].ToString(), out int chargelimit))
                {
                    if (!lastSetChargeLimitAddressName.Equals(_addr.name))
                    {
                        Task.Factory.StartNew(() =>
                        {
                            Log($"SetChargeLimit to {chargelimit} at '{_addr.name}' ...");
                            string result = webhelper.PostCommand("command/set_charge_limit", "{\"percent\":" + chargelimit + "}", true).Result;
                            Log("set_charge_limit(): " + result);
                            lastSetChargeLimitAddressName = _addr.name;
                        });
                    }
                }
            }
        }

        private void ResetHighFrequencyLogging()
        {
            highFrequencyLogging = false;
            highFrequencyLoggingMode = HFLMode.Ticks;
            highFrequencyLoggingTicks = 0;
            highFrequencyLoggingTicksLimit = 0;
            highFrequencyLoggingUntil = DateTime.Now;
        }

        // do something on state changes
        private void HandleStateChange(TeslaState _oldState, TeslaState _newState)
        {
            Log("change TeslaLogger state: " + _oldState.ToString() + " -> " + _newState.ToString());
            currentJSON.CreateCurrentJSON();

            // any -> Start
            if (_oldState != TeslaState.Start && _newState == TeslaState.Start)
            {
                webhelper.SetLastShiftState("P"); // reset shift state to default "P"
            }
            // charging -> any
            if (_oldState == TeslaState.Charge && _newState != TeslaState.Charge)
            {
                ResetHighFrequencyLogging();
            }
            // sleeping -> any
            if (_oldState == TeslaState.Sleep && _newState != TeslaState.Sleep)
            {
                currentJSON.current_falling_asleep = false;
                currentJSON.CreateCurrentJSON();
            }
            // Start -> Online - Update Car Version after Update
            if (_oldState == TeslaState.Start && _newState == TeslaState.Online)
            {
                _ = webhelper.GetOdometerAsync();
            }
            // charging -> any
            if (_oldState == TeslaState.Charge && _newState != TeslaState.Charge)
            {
                _ = Task.Factory.StartNew(() =>
                {
                    Address addr = WebHelper.geofence.GetPOI(currentJSON.latitude, currentJSON.longitude, false);
                    if (addr != null && addr.specialFlags != null && addr.specialFlags.Count > 0)
                    {
                        if (addr.specialFlags.ContainsKey(Address.SpecialFlags.CopyChargePrice))
                        {
                            HandleSpecialFlag_CopyChargePrice(addr);
                        }
                    }
                });
            }
            // any -> charging
            if (_oldState != TeslaState.Charge && _newState == TeslaState.Charge)
            {
                Address addr = WebHelper.geofence.GetPOI(currentJSON.latitude, currentJSON.longitude, false);
                if (addr != null && addr.specialFlags != null && addr.specialFlags.Count > 0)
                {
                    foreach (KeyValuePair<Address.SpecialFlags, string> flag in addr.specialFlags)
                    {
                        switch (flag.Key)
                        {
                            case Address.SpecialFlags.HighFrequencyLogging:
                                HandleSpecialFlag_HighFrequencyLogging(flag.Value);
                                break;
                            case Address.SpecialFlags.SetChargeLimit:
                                HandleSpecialFlag_SetChargeLimit(addr, flag.Value);
                                break;
                            case Address.SpecialFlags.ClimateOff:
                            case Address.SpecialFlags.OpenChargePort:
                            case Address.SpecialFlags.EnableSentryMode:
                            case Address.SpecialFlags.CopyChargePrice:
                                break;
                            default:
                                Log("handleShiftStateChange unhandled special flag " + flag.ToString());
                                break;
                        }
                    }
                }
            }
        }


        private void SetCurrentState(TeslaState _newState)
        {
            if (_currentState != _newState)
            {
                HandleStateChange(_currentState, _newState);
            }
            _currentState = _newState;
        }


        private void WriteMissingFile(double missingOdometer)
        {
            try
            {
                string filepath = Path.Combine(FileManager.GetExecutingPath(), "MISSINGKM");
                File.AppendAllText(filepath, DateTime.Now.ToString(Tools.ciDeDE) + " : " + $"Missing: {missingOdometer}km!\r\n");

                UpdateTeslalogger.Chmod(filepath, 666, false);
            }
            catch (Exception)
            { }
        }

        public bool IsHighFrequenceLoggingEnabled(bool justcheck = false)
        {
            if (highFrequencyLogging)
            {
                switch (highFrequencyLoggingMode)
                {
                    case HFLMode.Ticks:
                        if (highFrequencyLoggingTicks < highFrequencyLoggingTicksLimit)
                        {
                            if (!justcheck)
                            {
                                highFrequencyLoggingTicks++;
                            }
                            return true;
                        }
                        break;
                    case HFLMode.Time:
                        TimeSpan ts = highFrequencyLoggingUntil - DateTime.Now;
                        if (ts.TotalMilliseconds > 0)
                        {
                            return true;
                        }
                        break;
                    default:
                        break;
                }
            }
            return false;
        }


        private void EnableHighFrequencyLoggingMode(HFLMode _mode, int _ticklimit, DateTime _until)
        {
            Log("enable HighFrequencyLogging - mode: " + _mode.ToString() + (_mode == HFLMode.Ticks ? " ticks: " + _ticklimit : " until: " + _until.ToString()));
            switch (_mode)
            {
                case HFLMode.Ticks:
                    highFrequencyLogging = true;
                    highFrequencyLoggingMode = _mode;
                    highFrequencyLoggingTicksLimit = _ticklimit;
                    break;
                case HFLMode.Time:
                    highFrequencyLogging = true;
                    highFrequencyLoggingMode = _mode;
                    highFrequencyLoggingUntil = _until;
                    break;
                default:
                    Log("EnableHighFrequencyLoggingMode default");
                    break;
            }
        }

        public void WriteSettings()
        {
            dbHelper.WriteCarSettings();
        }

        public void Log(string text)
        {
            string temp = "#" + CarInDB + ": " + text;
            Logfile.Log(temp);
        }

        // this should be called from a task
        internal void HandleSpecialFlag_CopyChargePrice(Address _addr)
        {
            Logfile.Log($"CopyChargePrice at '{_addr.name}'");
            // find charging session at Address with cost_total != NULL and cost_kwh_meter_invoice == NULL or 0 and cost_idle_fee_total == NULL or 0
            int referenceID = 0;
            double ref_cost_total = -1.0;
            string ref_cost_currency = "";
            double ref_cost_per_kwh = 0.0;
            double ref_cost_per_session = 0.0;
            double ref_cost_per_minute = 0.0;
            bool ref_cost_per_kwh_found = false;
            bool ref_cost_per_session_found = false;
            bool ref_cost_per_minute_found = false;
            DateTime chargeStart = DateTime.Now;
            DateTime chargeEnd = chargeStart;
            double charge_energy_added = 0.0;

            // find reference charging session
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(
@"SELECT
  chargingstate.id, 
  chargingstate.cost_total, 
  chargingstate.cost_currency,
  chargingstate.cost_per_kwh,
  chargingstate.cost_per_session,
  chargingstate.cost_per_minute
FROM
  chargingstate,
  pos  
WHERE
  chargingstate.pos = pos.id
  AND pos.address = @addr
  AND chargingstate.cost_total IS NOT NULL
  AND chargingstate.CarID = @CarID
ORDER BY id DESC
LIMIT 1", con))
                {
                    cmd.Parameters.Add("@addr", MySqlDbType.VarChar).Value = _addr.name;
                    cmd.Parameters.Add("@CarID", MySqlDbType.UByte).Value = CarInDB;
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read() && dr[1] != DBNull.Value)
                    {
                        int.TryParse(dr[0].ToString(), out referenceID);
                        if (!double.TryParse(dr[1].ToString(), out ref_cost_total))
                        {
                            ref_cost_total = -1.0;
                        }
                        ref_cost_currency = dr[2].ToString();
                        if (double.TryParse(dr[3].ToString(), out ref_cost_per_kwh))
                        {
                            ref_cost_per_kwh_found = true;
                        }
                        if (double.TryParse(dr[4].ToString(), out ref_cost_per_session))
                        {
                            ref_cost_per_session_found = true;
                        }
                        if (double.TryParse(dr[5].ToString(), out ref_cost_per_minute))
                        {
                            ref_cost_per_minute_found = true;
                        }
                        Tools.DebugLog($"find ref charge session: <{dr[0]}> <{dr[1]}> <{dr[2]}> <{dr[3]}> <{dr[4]}> <{dr[5]}>");
                    }
                    con.Close();
                }
            }
            if (ref_cost_total != -1.0)
            {
                // reference charging costs for addr found, now get latest charging session at addr
                Logfile.Log($"CopyChargePrice: reference charging session found for '{_addr.name}', ID {referenceID} - cost_per_kwh:{ref_cost_per_kwh} cost_per_session:{ref_cost_per_session} cost_per_minute:{ref_cost_per_minute}");
                int chargeID = 0;
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(
@"SELECT
  chargingstate.id,
  charging.charge_energy_added,
  chargingstate.startdate,
  chargingstate.enddate
FROM
  chargingstate,
  pos,
  charging 
WHERE
chargingstate.pos = pos.id
AND chargingstate.endchargingid = charging.id
AND pos.address = @addr
AND chargingstate.cost_total IS NULL
AND chargingstate.CarID = @CarID
ORDER BY id DESC
LIMIT 1", con))
                    {
                        cmd.Parameters.Add("@addr", MySqlDbType.VarChar).Value = _addr.name;
                        cmd.Parameters.Add("@CarID", MySqlDbType.UByte).Value = CarInDB;
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read() && dr[0] != DBNull.Value)
                        {
                            Tools.DebugLog($"find latest charge session: <{dr[0]}> <{dr[1]}> <{dr[2]}> <{dr[3]}>");
                            if (
                                dr[0] != null && int.TryParse(dr[0].ToString(), out chargeID)
                                && dr[1] != null && double.TryParse(dr[1].ToString(), out charge_energy_added)
                                && dr[2] != null && DateTime.TryParse(dr[2].ToString(), out chargeStart)
                                && dr[3] != null && DateTime.TryParse(dr[3].ToString(), out chargeEnd)
                                )
                            {
                                Logfile.Log($"CopyChargePrice: latest charging session at '{_addr.name}' has ID {chargeID} - charge_energy_added:{charge_energy_added} chargeStart:{chargeStart} chargeEnd:{chargeEnd} duration:{(chargeEnd - chargeStart).TotalMinutes} minutes");
                            }
                        }
                    }
                    con.Close();
                }
                if (chargeID != 0)
                {
                    // update charge session with id chargeID
                    if (ref_cost_total == 0.0)
                    {
                        // free charging
                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();
                            using (MySqlCommand cmd = new MySqlCommand(
@"UPDATE
  chargingstate
SET
  cost_total = @cost_total,
  cost_currency = @cost_currency
WHERE
  id = @id
  AND CarID = @CarID", con))
                            {
                                cmd.Parameters.AddWithValue("@cost_total", ref_cost_total);
                                cmd.Parameters.AddWithValue("@cost_currency", DBHelper.DBNullIfEmpty(ref_cost_currency));
                                cmd.Parameters.AddWithValue("@id", chargeID);
                                cmd.Parameters.Add("@CarID", MySqlDbType.UByte).Value = CarInDB;
                                Tools.DebugLog(cmd);
                                _ = cmd.ExecuteNonQuery();
                                Logfile.Log($"CopyChargePrice: update charging session at '{_addr.name}', ID {chargeID}: cost_total 0.0");
                            }
                        }
                    }
                    else
                    {
                        // calculate costs
                        double calculated_total_cost = 0.0;
                        if ((chargeEnd - chargeStart).TotalMinutes != 0 && ref_cost_per_minute != 0.0)
                        {
                            calculated_total_cost += (chargeEnd - chargeStart).TotalMinutes * ref_cost_per_minute;
                            Logfile.Log($"CopyChargePrice: cost_per_minute:{(chargeEnd - chargeStart).TotalMinutes * ref_cost_per_minute} - total:{calculated_total_cost}");
                        }
                        if (ref_cost_per_kwh != 0.0)
                        {
                            calculated_total_cost += charge_energy_added * ref_cost_per_kwh;
                            Logfile.Log($"CopyChargePrice: cost_per_kwh:{charge_energy_added * ref_cost_per_kwh} - total:{calculated_total_cost}");
                        }
                        if (ref_cost_per_session != 0.0)
                        {
                            calculated_total_cost += ref_cost_per_session;
                            Logfile.Log($"CopyChargePrice: cost_per_session:{ref_cost_per_session} - total:{calculated_total_cost}");
                        }
                        Logfile.Log($"CopyChargePrice: calculated_total_cost:{calculated_total_cost}");
                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();
                            using (MySqlCommand cmd = new MySqlCommand(
@"UPDATE
  chargingstate
SET
  cost_total = @cost_total,
  cost_currency = @cost_currency,
  cost_per_kwh = @cost_per_kwh,
  cost_per_session = @cost_per_session,
  cost_per_minute = @cost_per_minute
WHERE
  id = @id", con))
                            {
                                cmd.Parameters.AddWithValue("@cost_total", calculated_total_cost);
                                cmd.Parameters.AddWithValue("@cost_currency", DBHelper.DBNullIfEmpty(ref_cost_currency));
                                if (ref_cost_per_kwh_found)
                                {
                                    cmd.Parameters.Add("@cost_per_kwh", MySqlDbType.Double).Value = ref_cost_per_kwh;
                                }
                                else
                                {
                                    cmd.Parameters.Add("@cost_per_kwh", MySqlDbType.Double).Value = DBNull.Value;
                                }
                                if (ref_cost_per_session_found)
                                {
                                    cmd.Parameters.Add("@cost_per_session", MySqlDbType.Double).Value = ref_cost_per_session;
                                }
                                else
                                {
                                    cmd.Parameters.Add("@cost_per_session", MySqlDbType.Double).Value = DBNull.Value;
                                }
                                if (ref_cost_per_minute_found)
                                {
                                    cmd.Parameters.Add("@cost_per_minute", MySqlDbType.Double).Value = ref_cost_per_minute;
                                }
                                else
                                {
                                    cmd.Parameters.Add("@cost_per_minute", MySqlDbType.Double).Value = DBNull.Value;
                                }
                                cmd.Parameters.AddWithValue("@id", chargeID);
                                Tools.DebugLog(cmd);
                                _ = cmd.ExecuteNonQuery();
                                Logfile.Log($"CopyChargePrice: update charging session at '{_addr.name}', ID {chargeID}: cost_total {calculated_total_cost}");
                            }
                        }
                    }
                }
                else
                {
                    Logfile.Log($"CopyChargePrice: no cost_total IS NULL charging session found for '{_addr.name}'");
                }
            }
            else
            {
                Logfile.Log($"CopyChargePrice: no reference charging session found for '{_addr.name}'");
            }
        }

        public static Car GetCarByID(int carid)
        {
            return allcars.FirstOrDefault(car => car.CarInDB == carid);
        }

        public bool IsInService()
        {
            if (teslaAPIState.GetBool("in_service", out bool in_service))
            {
                return in_service;
            }
            return false;
        }

        public bool IsParked()
        {
            // online and parked
            if (teslaAPIState.GetString("state", out string state) && state != null && state.Equals("online")
                && (teslaAPIState.GetString("shift_state", out string shift_state) && shift_state != null
                    && (shift_state.Equals("P") || shift_state.Equals("undef")))
               )
            {
                return true;
            }
            // asleep
            if (teslaAPIState.GetString("state", out state) && state.Equals("asleep"))
            {
                return true;
            }
            return false;
        }

        public bool IsInstallingSoftwareUpdate()
        {
            if (teslaAPIState.GetString("software_update.status", out string status))
            {
                return status.Equals("installing");
            }
            return false;
        }

        internal bool IsCharging()
        {
            if (teslaAPIState.GetString("charging_state", out string charging_state)
                && charging_state != null && charging_state.Equals("Charging"))
            {
                return true;
            }
            return false;
        }

        public bool TLUpdatePossible()
        {
            if (GetCurrentState() == Car.TeslaState.Sleep)
            {
                return true;
            }
            if (teslaAPIState.GetBool("locked", out bool locked)
                && teslaAPIState.GetBool("is_user_present", out bool is_user_present)
                && webhelper.GetLastShiftState().Equals("P"))
            {
                if (locked && !is_user_present)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasFreeSuC()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(
@"SELECT
  freesuc
FROM
  cars
WHERE
id = @carid", con))
                    {
                        cmd.Parameters.Add("@carid", MySqlDbType.UByte).Value = CarInDB;
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read() && dr[0] != null && dr[0] != DBNull.Value && int.TryParse(dr[0].ToString(), out int freesuc))
                        {
                            Tools.DebugLog($"HasFreeSuC() dr[0]:{dr[0]}");
                            Tools.DebugLog($"HasFreeSuC() freesuc:{freesuc}");
                            Tools.DebugLog($"HasFreeSuC() return:{freesuc == 1}");
                            return freesuc == 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog($"Exception during Car.HasFreeSuC(): {ex}");
                Logfile.ExceptionWriter(ex, "Exception during Car.HasFreeSuC()");
            }
            return false;
        }

        public bool CanFallAsleep(out string reason)
        {
            try
            {
                reason = string.Empty;
                if (teslaAPIState.GetBool("is_user_present", out bool is_user_present) && is_user_present)
                {
                    reason = "is_user_present";
                    return false;
                }
                if (teslaAPIState.GetBool("is_preconditioning", out bool is_preconditioning) && is_preconditioning)
                {
                    reason = "is_preconditioning";
                    return false;
                }
                if (teslaAPIState.GetBool("sentry_mode", out bool sentry_mode) && sentry_mode)
                {
                    reason = "sentry_mode";
                    return false;
                }
                if (teslaAPIState.GetInt("df", out int df) && df > 0)
                {
                    reason = $"Driver Front Door {df}";
                    return false;
                }
                if (teslaAPIState.GetInt("pf", out int pf) && pf > 0)
                {
                    reason = $"Passenger Front Door {pf}";
                    return false;
                }
                if (teslaAPIState.GetInt("dr", out int dr) && dr > 0)
                {
                    reason = $"Driver Rear Door {dr}";
                    return false;
                }
                if (teslaAPIState.GetInt("pr", out int pr) && pr > 0)
                {
                    reason = $"Passenger Rear Door {pr}";
                    return false;
                }
                if (teslaAPIState.GetInt("ft", out int ft) && ft > 0)
                {
                    reason = $"Fron Trunk {ft}";
                    return false;
                }
                if (teslaAPIState.GetInt("rt", out int rt) && rt > 0)
                {
                    reason = $"Rear Trunk {rt}";
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }

            reason = "";
            return true;
        }
    }
}