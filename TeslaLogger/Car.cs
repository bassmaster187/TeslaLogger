using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    internal class Car
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
        internal void SetLastCarUsed(DateTime dt)
        {
            lastCarUsed = dt;
            Tools.DebugLog("#" + CarInDB + ": " + $"lastCarUsed: {lastCarUsed}");
        }
        private DateTime lastOdometerChanged = DateTime.Now;
        internal DateTime GetLastOdometerChanged() { return lastOdometerChanged; }
        private DateTime lastTryTokenRefresh = DateTime.Now;
        internal DateTime GetLastTryTokenRefresh() { return lastTryTokenRefresh; }
        private string lastSetChargeLimitAddressName = string.Empty;

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

        private string modelName;
        private bool raven = false;
        private double _wh_TR = 0.190052356;
        private double dB_Wh_TR = 0;
        private int dB_Wh_TR_count = 0;

        private string car_type = "";
        private string car_special_type = "";
        private string trim_badging = "";

        private string model = "";
        private string battery = "";

        private string display_name = "";

        private string taskerHash = "";
        private string vin = "";

        private string aBRP_token = "";
        private int aBRP_mode = 0;

        private CurrentJSON currentJSON;

        private static List<Car> allcars = new List<Car>();

        private DBHelper dbHelper;

        private readonly TeslaAPIState teslaAPIState;

        private bool useTaskerToken = true;

        public double WhTR
        {
            get => _wh_TR;
            set
            {
                _wh_TR = value;
                CurrentJSON.Wh_TR = value;
            }
        }

        public string LastSetChargeLimitAddressName { get => lastSetChargeLimitAddressName; set => lastSetChargeLimitAddressName = value; }
        public string LastSetChargingAmpsAddressName { get; internal set; }
        public string ModelName { get => modelName; set => modelName = value; }
        public bool Raven { get => raven; set => raven = value; }
        public double DBWhTR { get => dB_Wh_TR; set => dB_Wh_TR = value; }
        public int DBWhTRcount { get => dB_Wh_TR_count; set => dB_Wh_TR_count = value; }
        public string CarType { get => car_type; set => car_type = value; }
        public string CarSpecialType { get => car_special_type; set => car_special_type = value; }
        public string TrimBadging { get => trim_badging; set => trim_badging = value; }
        public string Model { get => model; set => model = value; }
        public string Battery { get => battery; set => battery = value; }
        public string DisplayName { get => display_name; set => display_name = value; }
        public string TaskerHash { get => taskerHash; set => taskerHash = value; }
        public string Vin { get => vin; set => vin = value; }
        public string ABRPToken { get => aBRP_token; set => aBRP_token = value; }
        public int ABRPMode { get => aBRP_mode; set => aBRP_mode = value; }
        public CurrentJSON CurrentJSON { get => currentJSON; set => currentJSON = value; }
        public static List<Car> Allcars { get => allcars; }
        public DBHelper DbHelper { get => dbHelper; set => dbHelper = value; }
        public bool UseTaskerToken { get => useTaskerToken; set => useTaskerToken = value; }
        public string MFACode { get => mFA_Code; set => mFA_Code = value; }
        public string Captcha { get => captcha; set => captcha = value; }
        public string CaptchaString { get => captcha_String; set => captcha_String = value; }
        public string ReCaptchaCode { get => reCaptcha_Code; set => reCaptcha_Code = value; }
        public double Avgkm { get => avgkm; set => avgkm = value; }
        public double Kwh100km { get => kwh100km; set => kwh100km = value; }
        public double Avgsocdiff { get => avgsocdiff; set => avgsocdiff = value; }
        public double Maxkm { get => maxkm; set => maxkm = value; }
        public double CarVoltageAt50SOC { get => carVoltageAt50SOC; set => carVoltageAt50SOC = value; }
        public StringBuilder Passwortinfo { get => passwortinfo; set => passwortinfo = value; }
        public int Year { get => year; set => year = value; }
        public bool AWD { get => aWD; set => aWD = value; }
        public bool MIC { get => mIC; set => mIC = value; }
        public string Motor { get => motor; set => motor = value; }
        public static object InitCredentialsLock { get => initCredentialsLock; set => initCredentialsLock = value; }
        public double Sumkm { get => sumkm; set => sumkm = value; }

        private string mFA_Code;
        private string captcha;
        private string captcha_String;
        private string reCaptcha_Code;

        internal int LoginRetryCounter = 0;
        private double sumkm = 0;
        private double avgkm = 0;
        private double kwh100km = 0;
        private double avgsocdiff = 0;
        private double maxkm = 0;
        private double carVoltageAt50SOC = 0;

        private StringBuilder passwortinfo = new StringBuilder();
        private int year = 0;
        private bool aWD = false;
        private bool mIC = false;
        private string motor = "";
        internal bool waitForMFACode;
        internal bool waitForRecaptcha;
        private static object initCredentialsLock = new object();
        private static object _syncRoot = new object();

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal TeslaAPIState GetTeslaAPIState() { return teslaAPIState; }

        public Car(int CarInDB, string TeslaName, string TeslaPasswort, int CarInAccount, string TeslaToken, DateTime TeslaTokenExpire, string ModelName, string cartype, string carspecialtype, string cartrimbadging, string displayname, string vin, string TaskerHash, double? WhTR, TeslaState currentState = TeslaState.Start)
        {
            lock (_syncRoot)
            {
                try
                {
                    CurrentJSON = new CurrentJSON(this);
                    teslaAPIState = new TeslaAPIState(this);
                    this.TeslaName = TeslaName;
                    this.TeslaPasswort = TeslaPasswort;
                    this.CarInAccount = CarInAccount;
                    this.CarInDB = CarInDB;
                    this.Tesla_Token = TeslaToken;
                    this.Tesla_Token_Expire = TeslaTokenExpire;
                    this.ModelName = ModelName;
                    this.CarType = cartype;
                    this.CarSpecialType = carspecialtype;
                    this.TrimBadging = cartrimbadging;
                    this.DisplayName = displayname;
                    this.Vin = vin;
                    this.TaskerHash = TaskerHash;
                    this.WhTR = WhTR ?? 0.190;
                    this._currentState = currentState;

                    if (CarInDB > 0)
                    {
                        Allcars.Add(this);
                    }
                    DbHelper = new DBHelper(this);
                    webhelper = new WebHelper(this);

                    if (CarInDB > 0)
                    {
                        thread = new Thread(Loop)
                        {
                            Name = "Car_" + CarInDB
                        };
                        thread.Start();
                    }
                }
                catch (Exception ex)
                {
                    SendException2Exceptionless(ex);

                    ExceptionDispatchInfo.Capture(ex).Throw();
                }
            }
        }

        private void Loop()
        {
            try
            {
                CurrentJSON.current_odometer = DbHelper.GetLatestOdometer();
                CurrentJSON.CreateCurrentJSON();

                Monitor.Enter(InitCredentialsLock);
                try
                {
                    CheckNewCredentials();

                    InitStage3();
                }
                finally
                {
                    Monitor.Exit(InitCredentialsLock);
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
                        SendException2Exceptionless(ex);

                        Logfile.ExceptionWriter(ex, "#" + CarInDB + ": main loop");
                        Thread.Sleep(10000);
                    }
                }
            }
            catch (Exception ex)
            {
                string temp = ex.ToString();

                if (!temp.Contains("ThreadAbortException"))
                {
                    SendException2Exceptionless(ex);
                    Log(temp);
                }
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
                DbHelper.GetAvgConsumption(out this.sumkm, out this.avgkm, out this.kwh100km, out this.avgsocdiff, out this.maxkm);

                if (!webhelper.RestoreToken())
                {
                    webhelper.Tesla_token = webhelper.GetToken();
                }

                if (webhelper.Tesla_token == "NULL")
                {
                    ExitCarThread("Tesla_token == NULL");
                }

                LogToken();

                if (DBHelper.DBConnectionstring.Length == 0)
                {
                    ExitCarThread("DBHelper.DBConnectionstring.Length == 0");
                }

                if (webhelper.GetVehicles() == "NULL")
                {
                    ExitCarThread("wh.GetVehicles() == NULL");
                }

                DbHelper.GetEconomy_Wh_km(webhelper);

                string online = webhelper.IsOnline().Result;
                Log("Streamingtoken: " + Tools.ObfuscateString(webhelper.Tesla_Streamingtoken));

                if (DbHelper.GetMaxPosid(false) == 0)
                {
                    Log("Insert first Pos");
                    webhelper.IsDriving(true);
                }

                Log("Country Code: " + DbHelper.UpdateCountryCode());
                CarVoltageAt50SOC = DbHelper.GetVoltageAt50PercentSOC(out DateTime startdate, out DateTime ende);
                Log("Voltage at 50% SOC:" + CarVoltageAt50SOC + "V Date:" + startdate.ToString(Tools.ciEnUS));

                string vindecoder = Tools.VINDecoder(Vin, out year, out _, out aWD, out mIC, out _, out motor, out _).ToString();

                webhelper.DeleteWakeupFile();

                if (Raven)
                {
                    ModelName += " Raven";
                }

                Log("Car: " + ModelName + " - " + WhTR + " Wh/km");
                Log($"VIN decoder: {vindecoder}");
                Log($"Vehicle Config: car_type:'{CarType}' car_special_type:'{CarSpecialType}' trim_badging:'{TrimBadging}'");

                InitMeter();

                DbHelper.GetLastTrip();

                CurrentJSON.current_car_version = DbHelper.GetLastCarVersion();

                DbHelper.GetABRP(out aBRP_token, out aBRP_mode);

                webhelper.StartStreamThread();
            }
            catch (Exception ex)
            {
                string temp = ex.ToString();
                if (!temp.Contains("ThreadAbortException"))
                {
                    SendException2Exceptionless(ex);

                    Log(ex.ToString());
                }
            }
        }

        private void InitMeter()
        {
            try
            {
                var v = ElectricityMeterBase.Instance(this);
                if (v != null)
                {
                    Log("Meter Status: " + v.ToString());
                }
                else
                {
                    Log("No meter config");
                }
            }
            catch (Exception ex)
            {
                SendException2Exceptionless(ex);

                Logfile.Log(ex.ToString());
            }
        }

        internal void ExitCarThread(string v)
        {
            Log("ExitCarThread: " + v);
            run = false;
            thread.Abort();
            Allcars.Remove(this);
        }

        public void ThreadJoin()
        {
            if (thread != null)
                thread.Join();
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
                CurrentJSON.current_falling_asleep = false;
                CurrentJSON.CreateCurrentJSON();
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

                if (odometerLastTrip != CurrentJSON.current_odometer)
                {
                    odometerLastTrip = CurrentJSON.current_odometer;
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

                if (Geofence.GetInstance().RacingMode)
                {
                    Address a = Geofence.GetInstance().GetPOI(CurrentJSON.GetLatitude(), CurrentJSON.GetLongitude());
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
                _ = Task.Factory.StartNew(() =>
                {
                    sd.SendAllChargingData();
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
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
                if (webhelper.IsDriving()
                    && (webhelper.GetLastShiftState().Equals("R", StringComparison.Ordinal)
                        || webhelper.GetLastShiftState().Equals("N", StringComparison.Ordinal)
                        || webhelper.GetLastShiftState().Equals("D", StringComparison.Ordinal)))
                {
                    webhelper.ResetLastChargingState();
                    lastCarUsed = DateTime.Now;
                    lastOdometerChanged = DateTime.Now;

                    if (webhelper.scanMyTesla != null)
                    {
                        webhelper.scanMyTesla.FastMode(true);
                    }

                    double missingOdometer = CurrentJSON.current_odometer - odometerLastTrip;

                    if (odometerLastTrip != 0)
                    {
                        if (missingOdometer > 5)
                        {
                            Log($"Missing: {missingOdometer} km! - Check: https://teslalogger.de/faq-1.php");
                            WriteMissingFile(missingOdometer);
                            
                            CreateExeptionlessLog("Missing", $"Missing: {missingOdometer} km", Exceptionless.Logging.LogLevel.Warn).Submit();
                        }
                        else
                        {
                            Log($"Missing: {missingOdometer} km");
                        }
                    }

                    DbHelper.StartDriveState(DateTime.Now);
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
                    DbHelper.StartChargingState(webhelper);
                    SetCurrentState(TeslaState.Charge);

                    webhelper.DeleteWakeupFile();
                }
                else
                {
                    RefreshToken();
                    UpdateTeslalogger.CheckForNewVersion();

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
                        if (ts.TotalMinutes > Program.KeepOnlineMinAfterUsage)
                        {
                            SetCurrentState(TeslaState.Start);

                            webhelper.IsDriving(true); // kurz bevor er schlafen geht, eine Positionsmeldung speichern und schauen ob standheizung / standklima / sentry läuft.
                            Address addr = Geofence.GetInstance().GetPOI(CurrentJSON.GetLatitude(), CurrentJSON.GetLongitude(), false);
                            if (!CanFallAsleep(out string reason))
                            {
                                Log($"Reason:{reason} prevents car to get sleep");
                                lastCarUsed = DateTime.Now;
                            }
                            else if (CurrentJSON.current_is_preconditioning)
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
                                    CurrentJSON.current_falling_asleep = true;
                                    CurrentJSON.CreateCurrentJSON();

                                    for (int x = 0; x < ApplicationSettings.Default.SuspendAPIMinutes * 10; x++)
                                    {
                                        if (webhelper.DrivingOrChargingByStream)
                                        {
                                            Log("StreamAPI prevents car to get sleep.");
                                            lastCarUsed = DateTime.Now;
                                            doSleep = false;
                                            break;
                                        }

                                        TimeSpan tsSMT = DateTime.Now - CurrentJSON.lastScanMyTeslaReceived;
                                        if (CurrentJSON.SMTSpeed > 5 &&
                                            CurrentJSON.SMTSpeed < 260 &&
                                            CurrentJSON.SMTBatteryPower > 2 &&
                                            tsSMT.TotalMinutes < 5)
                                        {
                                            Log("ScanMyTesla prevents car to get sleep. Speed: " + CurrentJSON.SMTSpeed);
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
                                            Log("Waiting for car to go to sleep " + (x / 10).ToString(Tools.ciEnUS));

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
                                        CurrentJSON.current_falling_asleep = false;
                                        CurrentJSON.CreateCurrentJSON();
                                    }
                                }
                            }
                        }
                    }

                    CurrentJSON.CheckCreateCurrentJSON();

                    if (doSleep)
                    {
                        int sleepduration = 5000;
                        // if charging is starting just now, decrease sleepduration to 0.5 second
                        try
                        {
                            // get charging_state, must not be older than 2 minutes = 120 seconds = 120000 milliseconds
                            if (GetTeslaAPIState().GetState("charging_state", out Dictionary<TeslaAPIState.Key, object> charging_state, 120000))
                            {
                                // charging_state == Starting?
                                if (charging_state[TeslaAPIState.Key.Value] != null
                                    && (charging_state[TeslaAPIState.Key.Value].ToString().Equals("Starting", StringComparison.Ordinal)
                                        || charging_state[TeslaAPIState.Key.Value].ToString().Equals("NoPower", StringComparison.Ordinal))
                                    )
                                {
                                    Tools.DebugLog($"charging_state: {charging_state[TeslaAPIState.Key.Value]}");
                                    // check if charging_state value is not older than 1 minute
                                    long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                                    if (long.TryParse(charging_state[TeslaAPIState.Key.ValueLastUpdate].ToString(), out long valueLastUpdate))
                                    {
                                        Tools.DebugLog($"charging_state now {now} vlu {valueLastUpdate} diff {now - valueLastUpdate}");
                                        if (now - valueLastUpdate < 60000)
                                        {
                                            // charging_state changed to Starting or NoPower less than 1 minute ago
                                            // reduce sleepduration to 0.5 second
                                            sleepduration = 500;
                                            Tools.DebugLog($"charging_state sleepduration: {sleepduration}");
                                        }
                                    }
                                }
                            }
                            // get charge_port_door_open, must not be older than 2 minutes = 120 seconds = 1200000 milliseconds
                            if (GetTeslaAPIState().GetState("charge_port_door_open", out Dictionary<TeslaAPIState.Key, object> charge_port_door_open, 120000))
                            {
                                // charge_port_door_open == true?
                                if (GetTeslaAPIState().GetBool("charge_port_door_open", out bool bcharge_port_door_open) && bcharge_port_door_open)
                                {
                                    Tools.DebugLog($"charge_port_door_open: {charge_port_door_open[TeslaAPIState.Key.Value]}");
                                    long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                                    // check if charge_port_door_open value True is not older than 1 minute
                                    if (long.TryParse(charge_port_door_open[TeslaAPIState.Key.ValueLastUpdate].ToString(), out long valueLastUpdate))
                                    {
                                        Tools.DebugLog($"charge_port_door_open now {now} vlu {valueLastUpdate} diff {now - valueLastUpdate}");
                                        if (now - valueLastUpdate < 60000)
                                        {
                                            // charge_port_door_open changed to Charging less than 1 minute ago
                                            // reduce sleepduration to 0.5 second
                                            sleepduration = 500;
                                            Tools.DebugLog($"charge_port_door_open sleepduration: {sleepduration}");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            SendException2Exceptionless(ex);

                            Tools.DebugLog("Exception sleepduration", ex);
                        }
                        Thread.Sleep(sleepduration);
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
            DbHelper.CloseChargingStates();
            //dbHelper.CloseChargingState();
            try
            {
                DbHelper.CloseDriveState(webhelper.lastIsDriveTimestamp);
            }
            catch (MySqlException ex)
            {
                if (ex.ErrorCode == -2147467259) // {"Duplicate entry 'xxx' for key 'ix_endpos'"}
                {
                    webhelper.IsDriving(true);
                    Log(ex.Message);
                }

                SendException2Exceptionless(ex);
            }

            string res = webhelper.IsOnline().Result;
            lastCarUsed = DateTime.Now;
            if (res == "online")
            {
                //Log(res);
                SetCurrentState(TeslaState.Online);
                webhelper.IsDriving(true);
                webhelper.ResetLastChargingState();
                DbHelper.StartState(res);
                DbHelper.CleanPasswort();
                return;
            }
            else if (res == "asleep")
            {
                //Log(res);
                SetCurrentState(TeslaState.Sleep);
                DbHelper.StartState(res);
                webhelper.ResetLastChargingState();
                CurrentJSON.CreateCurrentJSON();
            }
            else if (res == "offline")
            {
                //Log(res);
                DbHelper.StartState(res);
                CurrentJSON.CreateCurrentJSON();

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
            else if (res == "INSERVICE")
            {
                SetCurrentState(TeslaState.Start);
                Log("IS IN SERVICE");
                Thread.Sleep(1000 * 60 * 30);
            }
            else
            {
                CurrentJSON.current_sleeping = false;
                CurrentJSON.current_online = false;
                CurrentJSON.CreateCurrentJSON();

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

        internal void DriveFinished()
        {
            // finish trip
            SetCurrentState(TeslaState.Start);
            CurrentJSON.current_trip_end = DateTime.Now;
            CurrentJSON.current_trip_km_end = CurrentJSON.current_odometer;
            CurrentJSON.current_trip_end_range = CurrentJSON.current_ideal_battery_range_km;
            //webhelper.StopStreaming();

            odometerLastTrip = CurrentJSON.current_odometer;

            DbHelper.GetAvgConsumption(out this.sumkm, out this.avgkm, out this.kwh100km, out this.avgsocdiff, out this.maxkm);
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
                dynamic j = JsonConvert.DeserializeObject(json);

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
            if (ts.TotalDays > 30)
            {
                // If car wasn't sleeping since 20 days, try to get a new Teslalogger update
                // TODO don't work anymore!
                UpdateTeslalogger.CheckForNewVersion();

                TimeSpan ts2 = DateTime.Now - lastTryTokenRefresh;
                if (ts2.TotalMinutes > 30)
                {
                    lastTryTokenRefresh = DateTime.Now;
                    Log("try to get new Token");

                    string temp = webhelper.GetToken();
                    if (temp != "NULL")
                    {
                        Log("new Token received!");

                        webhelper.Tesla_token = temp;
                        webhelper.lastTokenRefresh = DateTime.Now;

                        // Every 10 Days send degradataion Data
                        ShareData sd = new ShareData(this);
                        _ = Task.Factory.StartNew(() =>
                        {
                            sd.SendDegradationData();
                        }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                    }
                    else
                    {
                        Log("Error getting new Token!");
                    }
                }
            }
        }

        public void HandleShiftStateChange(string oldState, string newState)
        {
            Log("ShiftStateChange: " + oldState + " -> " + newState);
            lastCarUsed = DateTime.Now;
            Address addr = Geofence.GetInstance().GetPOI(CurrentJSON.GetLatitude(), CurrentJSON.GetLongitude(), false);
            // process special flags for POI
            if (addr != null && addr.specialFlags != null && addr.specialFlags.Count > 0)
            {
                foreach (KeyValuePair<Address.SpecialFlags, string> flag in addr.specialFlags)
                {
                    switch (flag.Key)
                    {
                        case Address.SpecialFlags.OpenChargePort:
                            HandleSpecialFlag_OpenChargePort(flag.Value, oldState, newState);
                            break;
                        case Address.SpecialFlags.EnableSentryMode:
                            HandleSpeciaFlag_EnableSentryMode(flag.Value, oldState, newState);
                            break;
                        case Address.SpecialFlags.DisableSentryMode:
                            HandleSpeciaFlag_DisableSentryMode(flag.Value, oldState, newState);
                            break;
                        case Address.SpecialFlags.ClimateOff:
                            HandleSpeciaFlag_ClimateOff(flag.Value, oldState, newState);
                            break;
                        case Address.SpecialFlags.SetChargeLimit:
                        case Address.SpecialFlags.SetChargeLimitOnArrival:
                        case Address.SpecialFlags.CopyChargePrice:
                        case Address.SpecialFlags.HighFrequencyLogging:
                        case Address.SpecialFlags.CombineChargingStates:
                        case Address.SpecialFlags.DoNotCombineChargingStates:
                        case Address.SpecialFlags.OnChargeComplete:
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
                _ = Task.Factory.StartNew(() =>
                {
                    Log("OpenChargePort ...");
                    string result = webhelper.PostCommand("command/charge_port_door_open", null).Result;
                    Log("charge_port_door_open(): " + result);
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private void HandleSpeciaFlag_EnableSentryMode(string _flagconfig, string _oldState, string _newState)
        {
            string pattern = "([PRND]+)->([PRND]+)";
            Match m = Regex.Match(_flagconfig, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1 && m.Groups[1].Captures[0].ToString().Contains(_oldState) && m.Groups[2].Captures[0].ToString().Contains(_newState))
            {
                _ = Task.Factory.StartNew(() =>
                {
                    Log("EnableSentryMode ...");
                    string result = webhelper.PostCommand("command/set_sentry_mode", "{\"on\":true}", true).Result;
                    Log("set_sentry_mode(): " + result);
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private void HandleSpeciaFlag_DisableSentryMode(string _flagconfig, string _oldState, string _newState)
        {
            string pattern = "([PRND]+)->([PRND]+)";
            Match m = Regex.Match(_flagconfig, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1 && m.Groups[1].Captures[0].ToString().Contains(_oldState) && m.Groups[2].Captures[0].ToString().Contains(_newState))
            {
                _ = Task.Factory.StartNew(() =>
                {
                    Log("DisableSentryMode ...");
                    string result = webhelper.PostCommand("command/set_sentry_mode", "{\"on\":false}", true).Result;
                    Log("set_sentry_mode(): " + result);
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private void HandleSpeciaFlag_ClimateOff(string _flagconfig, string _oldState, string _newState)
        {
            string pattern = "([PRND]+)->([PRND]+)";
            Match m = Regex.Match(_flagconfig, pattern);
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1 && m.Groups[1].Captures[0].ToString().Contains(_oldState) && m.Groups[2].Captures[0].ToString().Contains(_newState))
            {
                _ = Task.Factory.StartNew(() =>
                {
                    Log("ClimateOff ...");
                    string result = webhelper.PostCommand("command/auto_conditioning_stop", null).Result;
                    Log("auto_conditioning_stop(): " + result);
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
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
                    if (!LastSetChargeLimitAddressName.Equals(_addr.name, StringComparison.Ordinal))
                    {
                        _ = Task.Factory.StartNew(() =>
                        {
                            Log($"SetChargeLimit to {chargelimit} at '{_addr.name}' ...");
                            string result = webhelper.PostCommand("command/set_charge_limit", "{\"percent\":" + chargelimit + "}", true).Result;
                            Log("set_charge_limit(): " + result);
                            LastSetChargeLimitAddressName = _addr.name;
                        }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                    }
                }
            }
        }

        internal void HandleSpecialFlag_OnChargeComplete(Address _addr, string _flagconfig)
        {
            string pattern = "([0-9]+)";
            Match m = Regex.Match(_flagconfig, pattern);
            if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
            {
                if (m.Groups[1].Captures[0] != null && int.TryParse(m.Groups[1].Captures[0].ToString(), out int chargelimit))
                {
                    _ = Task.Factory.StartNew(() =>
                      {
                          Log($"OnChargeComplete set charge limit to {chargelimit} at '{_addr.name}' ...");
                          string result = webhelper.PostCommand("command/set_charge_limit", "{\"percent\":" + chargelimit + "}", true).Result;
                          Log("set_charge_limit(): " + result);
                          // reset LastSetChargeLimitAddressName so that +scl can set the charge limit again
                          LastSetChargeLimitAddressName = string.Empty;
                      }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
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
            CurrentJSON.CreateCurrentJSON();

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
                CurrentJSON.current_falling_asleep = false;
                CurrentJSON.CreateCurrentJSON();
            }
            // Start -> Online - Update Car Version after Update
            if (_oldState == TeslaState.Start && _newState == TeslaState.Online)
            {
                _ = webhelper.GetOdometerAsync();
                Tools.DebugLog($"Start -> Online SendDataToAbetterrouteplannerAsync(utc:{Tools.ToUnixTime(DateTime.UtcNow) * 1000}, soc:{CurrentJSON.current_battery_level}, speed:0, charging:false, power:0, lat:{CurrentJSON.GetLatitude()}, lon:{CurrentJSON.GetLongitude()})");
                _ = webhelper.SendDataToAbetterrouteplannerAsync(Tools.ToUnixTime(DateTime.UtcNow) * 1000, CurrentJSON.current_battery_level, 0, false, 0, CurrentJSON.GetLatitude(), CurrentJSON.GetLongitude());

            }
            // any -> Driving
            if (_oldState != TeslaState.Drive && _newState == TeslaState.Drive)
            {
                // reset lastSetChargeLimitAddressName
                LastSetChargeLimitAddressName = string.Empty;
                // combine charging sessions
                _ = Task.Factory.StartNew(() =>
                {
                    dbHelper.CombineChangingStates();
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
            // any -> charging
            if (_oldState != TeslaState.Charge && _newState == TeslaState.Charge)
            {
                // evaluate +hfl special flag
                Address addr = Geofence.GetInstance().GetPOI(CurrentJSON.GetLatitude(), CurrentJSON.GetLongitude(), false);
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
                            case Address.SpecialFlags.SetChargeLimitOnArrival:
                            case Address.SpecialFlags.EnableSentryMode:
                            case Address.SpecialFlags.CopyChargePrice:
                            case Address.SpecialFlags.CombineChargingStates:
                            case Address.SpecialFlags.DoNotCombineChargingStates:
                            case Address.SpecialFlags.OnChargeComplete:
                                break;
                            default:
                                Log("HandleStateChange unhandled special flag " + flag.ToString());
                                break;
                        }
                    }
                }
            }
            // driving -> any
            if (_oldState == TeslaState.Drive && _newState != TeslaState.Drive)
            {
                Address addr = Geofence.GetInstance().GetPOI(CurrentJSON.GetLatitude(), CurrentJSON.GetLongitude(), false);
                if (addr != null && addr.specialFlags != null && addr.specialFlags.Count > 0)
                {
                    foreach (KeyValuePair<Address.SpecialFlags, string> flag in addr.specialFlags)
                    {
                        switch (flag.Key)
                        {
                            case Address.SpecialFlags.SetChargeLimitOnArrival:
                                Tools.DebugLog($"SetChargeLimitOnArrival: {flag.Value}");
                                HandleSpecialFlag_SetChargeLimit(addr, flag.Value);
                                break;
                            case Address.SpecialFlags.SetChargeLimit:
                            case Address.SpecialFlags.ClimateOff:
                            case Address.SpecialFlags.HighFrequencyLogging:
                            case Address.SpecialFlags.OpenChargePort:
                            case Address.SpecialFlags.EnableSentryMode:
                            case Address.SpecialFlags.CopyChargePrice:
                            case Address.SpecialFlags.CombineChargingStates:
                            case Address.SpecialFlags.DoNotCombineChargingStates:
                            case Address.SpecialFlags.OnChargeComplete:
                                break;
                            default:
                                Log("handleShiftStateChange unhandled special flag " + flag.ToString());
                                break;
                        }
                    }
                }
                // enable +hfl:1m for fast charger
                if (GetTeslaAPIState().GetBool("fast_charger_present", out bool fast_charger_present) && fast_charger_present)
                {
                    DateTime until = DateTime.Now;
                    until = until.AddMinutes(1);
                    EnableHighFrequencyLoggingMode(HFLMode.Time, 0, until);
                }
            }
        }

        internal void Restart(string reason, int waitSeconds)
        {
            Log("Restart Car " + CarInDB);

            webhelper.StopStreaming();
            webhelper.scanMyTesla?.StopThread();

            var t = new Thread(() =>
            {
                for (int x = 0; x < waitSeconds; x++)
                {
                    Logfile.Log("Restart carthread in " + (waitSeconds - x).ToString() + "sec");

                    Thread.Sleep(1000);
                }

                webhelper.scanMyTesla.KillThread();

                var dr = DBHelper.GetCar(CarInDB);
                if (dr != null)
                {
                    Logfile.Log("Start Car " + CarInDB);
                    Program.StartCarThread(dr, this.GetCurrentState());
                }

            });
            t.Name = "RestartThread_" + CarInDB;
            t.Start();

            ExitCarThread(reason);

            ThreadJoin();
        }

        private void SetCurrentState(TeslaState _newState)
        {
            if (_currentState != _newState)
            {
                HandleStateChange(_currentState, _newState);
            }
            _currentState = _newState;
        }


        private static void WriteMissingFile(double missingOdometer)
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
            Log("enable HighFrequencyLogging - mode: " + _mode.ToString() + (_mode == HFLMode.Ticks ? " ticks: " + _ticklimit : " until: " + _until.ToString(Tools.ciEnUS)));
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
            DbHelper.WriteCarSettings();
        }

        public void Log(string text)
        {
            Logfile.Log($"#{CarInDB}[{Thread.CurrentThread.Name}:{Thread.CurrentThread.ManagedThreadId}]: {text}");
        }

        public void ExternalLog(string text)
        {
            CreateExeptionlessLog("Car", text, Exceptionless.Logging.LogLevel.Info).Submit(); ;

            string temp = TaskerHash + ": " + text;
            Tools.ExternalLog(temp);
        }

        public static Car GetCarByID(int carid)
        {
            return Allcars.FirstOrDefault(car => car.CarInDB == carid);
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
            if (teslaAPIState.GetString("state", out string state) && state != null && state.Equals("online", StringComparison.Ordinal)
                && (teslaAPIState.GetString("shift_state", out string shift_state) && shift_state != null
                    && (shift_state.Equals("P", StringComparison.Ordinal) || shift_state.Equals("undef", StringComparison.Ordinal)))
               )
            {
                return true;
            }
            // asleep
            if (teslaAPIState.GetString("state", out state) && state.Equals("asleep", StringComparison.Ordinal))
            {
                return true;
            }
            return false;
        }

        public bool IsInstallingSoftwareUpdate()
        {
            if (teslaAPIState.GetString("software_update.status", out string status))
            {
                return status.Equals("installing", StringComparison.Ordinal);
            }
            return false;
        }

        internal bool IsCharging()
        {
            if (teslaAPIState.GetString("charging_state", out string charging_state)
                && charging_state != null && charging_state.Equals("Charging", StringComparison.Ordinal))
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
                && webhelper.GetLastShiftState().Equals("P", StringComparison.Ordinal))
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
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read() && dr[0] != null && dr[0] != DBNull.Value && int.TryParse(dr[0].ToString(), out int freesuc))
                        {
                            return freesuc == 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SendException2Exceptionless(ex);

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
                SendException2Exceptionless(ex);

                Log(ex.ToString());
            }

            reason = "";
            return true;
        }

        public static void LogActiveCars()
        {
            try
            {
                if (Car.Allcars == null)
                    return;

                ExceptionlessClient.Default.CreateFeatureUsage("Active_Cars_" + Car.Allcars.Count).FirstCarUserID().Submit();
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
            }
        }

        internal void SendException2Exceptionless(Exception ex)
        {
            CreateExceptionlessClient(ex).Submit();
        }

        internal EventBuilder CreateExceptionlessClient(Exception ex)
        {
            EventBuilder b = ex.ToExceptionless().SetUserIdentity(TaskerHash)
                        .AddObject(ModelName, "ModelName")
                        .AddObject(CarType, "CarType")
                        .AddObject(CarSpecialType, "CarSpecialType")
                        .AddObject(TrimBadging, "CarTrimBadging");

            return b;
        }

        internal EventBuilder CreateExeptionlessLog(string source, string message, Exceptionless.Logging.LogLevel logLevel)
        {
            EventBuilder b = ExceptionlessClient.Default.CreateLog(source, message, logLevel)
                .SetUserIdentity(TaskerHash)
                .AddObject(ModelName, "ModelName")
                .AddObject(CarType, "CarType")
                .AddObject(CarSpecialType, "CarSpecialType")
                .AddObject(TrimBadging, "CarTrimBadging");

            return b;
        }

        internal EventBuilder CreateExeptionlessFeature(string feature)
        {
            EventBuilder b = ExceptionlessClient.Default.CreateFeatureUsage(feature)
                .SetUserIdentity(TaskerHash)
                .AddObject(ModelName, "ModelName")
                .AddObject(CarType, "CarType")
                .AddObject(CarSpecialType, "CarSpecialType")
                .AddObject(TrimBadging, "CarTrimBadging");

            return b;
        }
    }   
}
