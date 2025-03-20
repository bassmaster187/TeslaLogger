using Exceptionless;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TeslaLogger
{
    public class TelemetryParser
    {
        private readonly Car car;

        private bool _dcCharging;

        public DateTime lastDriving = DateTime.MinValue;
        public DateTime lastMessageReceived = DateTime.MinValue;

        public DateTime lastPackCurrentDate = DateTime.MinValue;
        public double lastPackCurrent = 0.0;

        public DateTime lastVehicleSpeedDate = DateTime.MinValue;
        public double lastVehicleSpeed = 0.0;

        public DateTime lastSocDate = DateTime.MinValue;
        public double lastSoc = 0.0;

        public double lastChargingPower = 0.0;
        double lastDCChargingPower = 0.0;

        public String lastChargeState = "";

        public bool lastFastChargerPresent = false;

        public int lastposid = 0;
        private double lastIdealBatteryRange;
        private double? lastOdometer;
        private double? lastOutsideTemp;
        private double? lastInsideTemp;

        double? latitude = null;
        double? longitude = null;
        double? speed = null;

        double lastLatitude = 0;
        double lastLongitude = 0;
        private double lastRatedRange;

        internal double? charge_energy_added = null;
        double? ACChargingPower = null;

        String lastCruiseState = "";

        public bool databaseCalls = true;

        public event EventHandler handleACChargeChange;

        internal TelemetryParser(Car c)
        {
            car = c;

            lastIdealBatteryRange = car.CurrentJSON.current_ideal_battery_range_km;
            lastSoc = car.CurrentJSON.current_battery_level;
            lastOdometer = car.CurrentJSON.current_odometer;
            lastRatedRange = car.CurrentJSON.current_battery_range_km;
        }

        public void InitFromDB()
        {
            try
            {
                car.dbHelper.GetMaxChargeid(out DateTime chargeStart, out double? _charge_energy_added);

                double drivenAfterLastCharge = car.dbHelper.GetDrivenKm(chargeStart, DateTime.Now);
                if (drivenAfterLastCharge > 0)
                {
                    charge_energy_added = 0;
                    Log("Driving after last charge: " + drivenAfterLastCharge + " km -> charge_energy_added = 0");
                }
                else
                {
                    charge_energy_added = _charge_energy_added;
                    Log("charge_energy_added from DB: " + charge_energy_added);
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                car.CreateExceptionlessClient(ex).Submit();
            }
        }

        private bool driving;
        private bool _acCharging;
        private string lastDetailedChargeState;

        internal bool dcCharging
        {
            get => _dcCharging;
            set
            {
                if (_dcCharging != value)
                {
                    car.CurrentJSON.current_fast_charger_present = value;
                    car.CurrentJSON.CreateCurrentJSON();
                }
                _dcCharging = value;
            }
        }


        public bool Driving
        {
            get
            {
                if (driving)
                {
                    var ts = DateTime.Now - lastDriving;
                    if (ts.TotalMinutes > 60)
                    {
                        Log("Stop Driving by timeout 30 minutes ***");
                        driving = false;
                        Log("Parking time: " + lastDriving.ToString());
                        return false;
                    }

                    return true;
                }

                return false;

            }
            set
            {
                if (value)
                {
                    charge_energy_added = 0;
                }

                driving = value;
            }
        }

        internal bool acCharging
        {
            get => _acCharging;
            set
            {
                if (_acCharging != value)
                {
                    Log("ACCharging = " + value);
                    _acCharging = value;
                    handleACChargeChange?.Invoke(this, EventArgs.Empty);
                }

                _acCharging = value;
            }
        }

        public bool IsCharging => acCharging || dcCharging;

        public bool IsOnline()
        {
            if (!Driving && !acCharging && !dcCharging)
            {
                if (OnlineTimeout())
                    return false;
            }

            return true;
        }

        public bool OnlineTimeout()
        {
            var ts = DateTime.UtcNow - lastMessageReceived;
            return ts.TotalMinutes > 10;
        }

        void Log(string message)
        {
            car.Log("*** FT: " + message);
        }

        public void handleMessage(string resultContent)
        {
            try
            {
                dynamic j = JsonConvert.DeserializeObject(resultContent);

                if (j.ContainsKey("data"))
                {
                    Log(resultContent);

                    dynamic jData = j["data"];
                    string vin = j["vin"];
                    DateTime d = j["createdAt"];

                    if (car.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase))
                    {
                        // Log("Telemetry Server Data");
                        if (OnlineTimeout())
                            Log("Car Online!");

                        lastMessageReceived = DateTime.UtcNow;
                        InsertBatteryTable(jData, d, resultContent);
                        InsertCruiseStateTable(jData, d, resultContent);
                        handleStatemachine(jData, d, resultContent);
                        InsertLocation(jData, d, resultContent);
                        InsertCharging(jData, d, resultContent);
                        InsertStates(jData, d, resultContent);
                    }
                }
                else if (j.ContainsKey("alerts"))
                {
                    dynamic jData = j["alerts"];
                    string vin = j["vin"];
                    DateTime d = j["createdAt"];

                    if (car.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase))
                    {
                        Log("Telemetry Server Alerts");

                        foreach (dynamic ji in jData)
                        {
                            InsertAlert(ji, resultContent);
                        }
                    }
                }
                else if (j.ContainsKey("Teslalogger"))
                {
                    String cmd = j["Teslalogger"];
                    switch (cmd)
                    {
                        case "LoginRespone":
                            handleLoginResponse(j);
                            break;
                        case "ConfigAlreadySent":
                            string Config = j["Config"];
                            Log("Config Already Sent: " + Config);
                            break;
                        default:
                            Log("Unhandled Teslalogger MSG: " + resultContent);
                            break;
                    }
                }
                else
                {
                    Log("Unhandled: " + resultContent);

                    CheckDriving();
                    driving = false;
                    acCharging = false;
                    dcCharging = false;
                    Log("Sleep");
                    lastMessageReceived = DateTime.MinValue;
                }

            }
            catch (Exception ex)
            {
                Log(ex.ToString() + "\n" + resultContent);
            }
        }



        private void InsertStates(dynamic j, DateTime d, string resultContent)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                foreach (dynamic jj in j)
                {
                    string key = jj["key"];
                    dynamic value = jj["value"];

                    if (key == "SentryMode")
                    {
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["sentryModeStateValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        if (v.Contains("Armed"))
                        {
                            car.webhelper.is_sentry_mode = true;
                            car.CurrentJSON.current_is_sentry_mode = true;
                            car.CurrentJSON.CreateCurrentJSON();

                            Log("Insert Location (SentryMode)");
                            InsertLastLocation(d, false);
                        }
                        else
                        {
                            car.webhelper.is_sentry_mode = false;
                            car.CurrentJSON.current_is_sentry_mode = false;
                            car.CurrentJSON.CreateCurrentJSON();

                            Log("Insert Location (SentryMode)");
                            InsertLastLocation(d, false);
                        }
                        car.CurrentJSON.CreateCurrentJSON();
                    }
                    else if (key == "PreconditioningEnabled")
                    {
                        bool preconditioning = false;

                        string v = value["stringValue"];
                        if (v == "True")
                            preconditioning = true;

                        bool v1 = value["booleanValue"];
                        if (v1)
                            preconditioning = true;

                        car.CurrentJSON.current_is_preconditioning = preconditioning;
                        Log("Preconditioning: " + preconditioning);
                        car.CurrentJSON.CreateCurrentJSON();
                        
                        Log("Insert Location (Preconditioning)");
                        InsertLastLocation(d, false);
                    }
                    else if (key == "OutsideTemp")
                    {
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["doubleValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double OutsideTemp))
                        {
                            lastOutsideTemp = OutsideTemp;
                            car.CurrentJSON.current_outside_temperature = OutsideTemp;
                            car.CurrentJSON.CreateCurrentJSON();

                            Log("Insert Location (OutsideTemp)");
                            InsertLastLocation(d, false);
                        }
                    }
                    else if (key == "InsideTemp")
                    {
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["doubleValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double InsideTemp))
                        {
                            lastInsideTemp = InsideTemp;
                            car.CurrentJSON.current_inside_temperature = InsideTemp;
                            car.CurrentJSON.CreateCurrentJSON();

                            Log("Insert Location (InsideTemp)");
                            InsertLastLocation(d, false); 
                        }
                    }
                    else if (key == "TimeToFullCharge")
                    {
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["doubleValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double TimeToFullCharge))
                        {
                            car.CurrentJSON.current_time_to_full_charge = TimeToFullCharge;
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                    }
                    else if (key == "ChargeLimitSoc")
                    {
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["intValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double ChargeLimitSoc))
                        {
                            car.CurrentJSON.charge_limit_soc = (int)ChargeLimitSoc;
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                    }
                    else if (key == "DestinationName")
                    {
                        string v = value["stringValue"];

                        car.CurrentJSON.active_route_destination = v;
                        car.CurrentJSON.CreateCurrentJSON();

                    }
                    else if (key == "MinutesToArrival")
                    {
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["doubleValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double MinutesToArrival))
                        {
                            car.CurrentJSON.active_route_minutes_to_arrival = (int)MinutesToArrival;
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                    }
                    else if (key == "MilesToArrival")
                    {
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["doubleValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double MilesToArrival))
                        {
                            car.CurrentJSON.active_route_km_to_arrival = (long)(MilesToArrival * 1.609344);
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                    }
                    else if (key == "ExpectedEnergyPercentAtTripArrival")
                    {
                        try
                        {
                            int? v = value["intValue"];
                            car.CurrentJSON.active_route_energy_at_arrival = v;
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                        catch (Exception ex)
                        {
                            ex.ToExceptionless().Submit();
                            Log(ex.ToString());
                        }

                    }
                    else if (key == "RouteTrafficMinutesDelay")
                    {
                        try
                        {
                            double v = value["doubleValue"];

                            car.CurrentJSON.active_route_traffic_minutes_delay = v;
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                        catch (Exception ex)
                        {
                            ex.ToExceptionless().Submit();
                            Log(ex.ToString());
                        }
                    }
                    else if (key == "BatteryHeaterOn")
                    {
                        /*
                        string v = value["stringValue"];
                        if (bool.TryParse(v, out bool BatteryHeaterOn))
                        {
                            car.CurrentJSON.current_battery_heater = BatteryHeaterOn;
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                        */
                    }
                    else if (key == "DefrostForPreconditioning")
                    {
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["booleanValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        v = v.ToLower(CultureInfo.InvariantCulture);
                        if (bool.TryParse(v, out bool preconditioning))
                        {
                            car.CurrentJSON.current_is_preconditioning = preconditioning;
                            Log("Preconditioning: " + preconditioning);
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                    }
                    else if (key.StartsWith("TpmsPressure"))
                    {
                        string suffix = key.Substring(key.Length - 2);
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["doubleValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double pressure))
                        {
                            pressure = Math.Round(pressure, 2);
                            if (databaseCalls)
                            {
                                switch (suffix)
                                {
                                    case "Fl":
                                        car.DbHelper.InsertTPMS(1, pressure, d);
                                        break;
                                    case "Fr":
                                        car.DbHelper.InsertTPMS(2, pressure, d);
                                        break;
                                    case "Rl":
                                        car.DbHelper.InsertTPMS(3, pressure, d);
                                        break;
                                    case "Rr":
                                        car.DbHelper.InsertTPMS(4, pressure, d);
                                        break;
                                }
                            }
                        }
                    }
                    else if (key == "VehicleName")
                    {
                        string v = value["stringValue"];
                        if (!String.IsNullOrEmpty(v))
                        {
                            if (car.DisplayName != v)
                            {
                                Log("DisplayName: " + v);
                                car.DisplayName = v;
                                car.DbHelper.WriteCarSettings();
                            }
                        }
                    }
                    else if (key == "Trim")
                    {
                        string v = value["stringValue"];
                        if (!String.IsNullOrEmpty(v))
                        {
                            v = v.ToLower();
                            if (car.TrimBadging != v)
                            {
                                Log("Trim: " + v);
                                car.TrimBadging = v;
                                car.DbHelper.WriteCarSettings();
                                car.webhelper.UpdateEfficiency();

                                Log("Car Model Name: " + car.ModelName);
                            }
                        }
                    }
                    else if (key == "ServiceMode")
                    {
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["booleanValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        v = v.ToLower(CultureInfo.InvariantCulture);
                        if (bool.TryParse(v, out bool serviceMode))
                        {
                            //TODO : Implement ServiceMode
                        }
                    }
                    else if (key == "CarType")
                    {
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["carTypeValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        if (!String.IsNullOrEmpty(v))
                        {
                            v = v.Replace("CarType", "");
                            v = v.ToLower();

                            if (car.CarType != v)
                            {
                                Log("CarType: " + v);
                                car.CarType = v;
                                car.CarSpecialType = "base";
                                car.DbHelper.WriteCarSettings();
                                car.webhelper.UpdateEfficiency();

                                Log("Car Model Name: " + car.ModelName);
                            }
                        }
                    }
                    else if (key == "Version")
                    {
                        string car_version = value["stringValue"];
                        if (!String.IsNullOrEmpty(car_version))
                        {
                            if (car.CurrentJSON.current_car_version != car_version)
                            {
                                Log("Car Version: " + car_version);
                                car.CurrentJSON.current_car_version = car_version;
                                car.CurrentJSON.CreateCurrentJSON();

                                car.DbHelper.SetCarVersion(car_version);

                                car.webhelper.TaskerWakeupfile(true);
                            }

                        }
                    }
                    else if (key == "DoorState")
                    {
                        string DoorState = value["stringValue"];
                        if (!String.IsNullOrEmpty(DoorState))
                        {
                            if (DoorState.Contains("DriverFront"))
                                car.teslaAPIState.AddValue("df", "int", 1, Tools.ToUnixTime(d), "vehicle_state");
                            else
                                car.teslaAPIState.AddValue("df", "int", 0, Tools.ToUnixTime(d), "vehicle_state");

                            if (DoorState.Contains("DriverRear"))
                                car.teslaAPIState.AddValue("dr", "int", 1, Tools.ToUnixTime(d), "vehicle_state");
                            else
                                car.teslaAPIState.AddValue("dr", "int", 0, Tools.ToUnixTime(d), "vehicle_state");

                            if (DoorState.Contains("PassengerFront"))
                                car.teslaAPIState.AddValue("pf", "int", 1, Tools.ToUnixTime(d), "vehicle_state");
                            else
                                car.teslaAPIState.AddValue("pf", "int", 0, Tools.ToUnixTime(d), "vehicle_state");

                            if (DoorState.Contains("PassengerRear"))
                                car.teslaAPIState.AddValue("pr", "int", 1, Tools.ToUnixTime(d), "vehicle_state");
                            else
                                car.teslaAPIState.AddValue("pr", "int", 0, Tools.ToUnixTime(d), "vehicle_state");

                            if (DoorState.Contains("TrunkFront"))
                                car.teslaAPIState.AddValue("ft", "int", 1, Tools.ToUnixTime(d), "vehicle_state");
                            else
                                car.teslaAPIState.AddValue("ft", "int", 0, Tools.ToUnixTime(d), "vehicle_state");

                            if (DoorState.Contains("TrunkRear"))
                                car.teslaAPIState.AddValue("rt", "int", 1, Tools.ToUnixTime(d), "vehicle_state");
                            else
                                car.teslaAPIState.AddValue("rt", "int", 0, Tools.ToUnixTime(d), "vehicle_state");
                        }
                        else if (value is JObject obj && obj.ContainsKey("doorValue"))
                        {
                            Dictionary<string, string> doorMapping = new Dictionary<string, string>
                            {
                                { "DriverFront", "df" },
                                { "PassengerFront", "pf" },
                                { "DriverRear", "dr" },
                                { "PassengerRear", "pr" },
                                { "TrunkFront", "ft" },
                                { "TrunkRear", "rt" }
                            };

                            JToken doors = obj["doorValue"]; 

                            if (doors is JObject doorValues)
                            {
                                foreach (var dm in doorMapping) // close all doors
                                    car.teslaAPIState.AddValue(dm.Value, "int", 0, Tools.ToUnixTime(d), "vehicle_state");
                               
                                foreach (var door in doorValues)
                                {
                                    string originalKey = door.Key;
                                    bool isOpen = door.Value.ToObject<bool>();

                                    // Übersetzung des Türnamens
                                    if (doorMapping.TryGetValue(originalKey, out string shortKey))
                                    {
                                        car.teslaAPIState.AddValue(shortKey, "int", isOpen ? 1 : 0, Tools.ToUnixTime(d), "vehicle_state");
                                    }
                                }
                            }
                        }
                        else
                        {
                            car.teslaAPIState.AddValue("df", "int", 0, Tools.ToUnixTime(d), "vehicle_state");
                            car.teslaAPIState.AddValue("dr", "int", 0, Tools.ToUnixTime(d), "vehicle_state");
                            car.teslaAPIState.AddValue("pf", "int", 0, Tools.ToUnixTime(d), "vehicle_state");
                            car.teslaAPIState.AddValue("pr", "int", 0, Tools.ToUnixTime(d), "vehicle_state");
                            car.teslaAPIState.AddValue("ft", "int", 0, Tools.ToUnixTime(d), "vehicle_state");
                            car.teslaAPIState.AddValue("rt", "int", 0, Tools.ToUnixTime(d), "vehicle_state");
                        }

                        Log("XXXX DoorState:" + DoorState);
                        car.CurrentJSON.CreateCurrentJSON();

                    }
                    else if (key == "Locked")
                    {
                        string v = value["stringValue"];
                        if (v == null)
                        {
                            v = value["booleanValue"];
                            if (v == null)
                            {
                                continue;
                            }
                        }
                        v = v.ToLower(CultureInfo.InvariantCulture);
                        if (bool.TryParse(v, out bool l))
                        {
                            car.teslaAPIState.AddValue("locked", "bool", l, Tools.ToUnixTime(d), "vehicle_state");
                            car.CurrentJSON.CreateCurrentJSON();

                        }
                    }
                    else if (key.EndsWith("Window"))
                    {
                        string apistatekey = key.ToLower().Insert(2, "_");
                        string Window = value["stringValue"];
                        if (Window == null)
                        {
                            Window = value["windowStateValue"];
                            if (Window == null)
                            {
                                continue;
                            }
                        }
                        Log("Window: " + key + " / " + Window);
                        if (!String.IsNullOrEmpty(Window))
                        {
                            int apistatevalue = 0;
                            if (Window.Contains("Open"))
                                apistatevalue = 1;

                            car.teslaAPIState.AddValue(apistatekey, "int", apistatevalue, Tools.ToUnixTime(d), "vehicle_state");
                        }
                        else
                            car.teslaAPIState.AddValue(apistatekey, "int", 0, Tools.ToUnixTime(d), "vehicle_state");

                        car.CurrentJSON.CreateCurrentJSON();
                    }
                    else if (key == "DetailedChargeState")
                    {
                        string DetailedChargeState = value["detailedChargeStateValue"];

                        if (!String.IsNullOrEmpty(DetailedChargeState))
                        {
                            lastDetailedChargeState = DetailedChargeState;

                            CheckDetailedChargeState(d);

                            if (IsCharging && DetailedChargeState == "DetailedChargeStateStopped")
                            {
                                Log("Stop Charging by DetailedChargeState");
                                acCharging = false;
                                dcCharging = false;
                            }

                            if (DetailedChargeState.Contains("DetailedChargeStateNoPower") ||
                                DetailedChargeState.Contains("DetailedChargeStateStarting") ||
                                DetailedChargeState.Contains("DetailedChargeStateCharging") ||
                                DetailedChargeState.Contains("DetailedChargeStateComplete") ||
                                DetailedChargeState.Contains("DetailedChargeStateStopped"))
                            {
                                car.CurrentJSON.current_plugged_in = true;
                            }
                            else
                            {
                                car.CurrentJSON.current_plugged_in = false;
                            }
                            car.CurrentJSON.CreateCurrentJSON();

                        }
                        Log("DetailedChargeState: " + DetailedChargeState);

                    }
                }
            }
        }

        private void CheckDetailedChargeState(DateTime d)
        {
            if (!IsCharging && lastDetailedChargeState == "DetailedChargeStateCharging")
            {
                if (lastFastChargerPresent)
                {
                    if (lastPackCurrent > 1 || lastDCChargingPower > 1)
                    {
                        Log("Start DC Charging by DetailedChargeState Packcurrent: " + lastPackCurrent);
                        StartDCCharging(d);
                    }
                }
                else
                {
                    if (lastPackCurrent > 1 || ACChargingPower > 1)
                    {
                        Log("Start AC Charging by DetailedChargeState Packcurrent: " + lastPackCurrent);
                        StartACCharging(d);
                    }
                }
            }
        }

        private void InsertCharging(dynamic j, DateTime d, string resultContent)
        {
            double ChargingEnergyIn = double.NaN;
            bool changed = false;

            using (MySqlCommand cmd = new MySqlCommand())
            {
                foreach (dynamic jj in j)
                {
                    string key = jj["key"];
                    dynamic value = jj["value"];

                    if (key == "ACChargingEnergyIn" && charge_energy_added == null)
                    {
                        string v1 = value["stringValue"];
                        if (v1 == null)
                        {
                            v1 = value["doubleValue"];
                            if (v1 == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v1, NumberStyles.Any, CultureInfo.InvariantCulture, out ChargingEnergyIn))
                        {
                            ChargingEnergyIn = Math.Round(ChargingEnergyIn, 2);
                            Log("charge_energy_added: " + charge_energy_added);
                            charge_energy_added = ChargingEnergyIn;
                        }
                    }

                    if (key == "ACChargingPower")
                    {
                        string v1 = value["stringValue"];
                        if (v1 == null)
                        {
                            v1 = value["doubleValue"];
                            if (v1 == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v1, NumberStyles.Any, CultureInfo.InvariantCulture, out double v2))
                        {
                            ACChargingPower = Math.Round(v2, 2); 

                            if (!acCharging && lastChargeState == "Enable" && ACChargingPower > 0.1)
                            {
                                Log("*** Start AC Charging - ACChargingPower: " + ACChargingPower);
                                StartACCharging(d);
                            }
                        }
                    }

                    //Changed from ACChargingEnergyIn to DCChargingEnergyIn: AC* is grid side, DC* is energy charged into to the battery
                    if (key == "DCChargingEnergyIn" && acCharging)
                    {
                        string v1 = value["stringValue"];
                        if (v1 == null)
                        {
                            v1 = value["doubleValue"];
                            if(v1 == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v1, NumberStyles.Any, CultureInfo.InvariantCulture, out ChargingEnergyIn))
                        {
                            ChargingEnergyIn = Math.Round(ChargingEnergyIn, 2);
                            charge_energy_added = ChargingEnergyIn;
                            cmd.Parameters.AddWithValue("@charge_energy_added", ChargingEnergyIn);
                            car.CurrentJSON.current_charge_energy_added = ChargingEnergyIn;
                            changed = true;
                        }
                    }
                    else if (key == "DCChargingEnergyIn" && dcCharging)
                    {
                        string v1 = value["stringValue"];
                        if (v1 == null)
                        {
                            v1 = value["doubleValue"];
                            if (v1 == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v1, NumberStyles.Any, CultureInfo.InvariantCulture, out ChargingEnergyIn))
                        {
                            ChargingEnergyIn = Math.Round(ChargingEnergyIn, 2);
                            charge_energy_added = ChargingEnergyIn;
                            cmd.Parameters.AddWithValue("@charge_energy_added", ChargingEnergyIn);
                            car.CurrentJSON.current_charge_energy_added = ChargingEnergyIn;
                            changed = true;
                        }
                    }
                    else if (key == "Soc")
                    {
                        string v1 = value["stringValue"];
                        if (v1 == null)
                        {
                            v1 = value["doubleValue"];
                            if (v1 == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v1, NumberStyles.Any, CultureInfo.InvariantCulture, out double Soc))
                        {
                            Soc = Math.Round(Soc, 1);
                            lastSoc = Soc;
                            lastSocDate = d;

                            car.teslaAPIState.AddValue("battery_level", "int", Soc, Tools.ToUnixTime(d), "charge_state");

                            car.CurrentJSON.current_battery_level = lastSoc;
                            changed = true;
                        }
                    }
                    else if (key == "ACChargingPower" && acCharging)
                    {
                        string v1 = value["stringValue"];
                        if (v1 == null)
                        {
                            v1 = value["doubleValue"];
                            if (v1 == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v1, NumberStyles.Any, CultureInfo.InvariantCulture, out double ChargingPower))
                        {
                            lastChargingPower = ChargingPower;
                            car.CurrentJSON.current_charger_power = Math.Round(ChargingPower, 2);
                            changed = true;
                        }
                    }
                    else if (key == "DCChargingPower" && dcCharging)
                    {
                        string v1 = value["stringValue"];
                        if (v1 == null)
                        {
                            v1 = value["doubleValue"];
                            if (v1 == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v1, out double ChargingPower))
                        {
                            lastDCChargingPower = ChargingPower;
                            lastChargingPower = ChargingPower;
                            car.CurrentJSON.current_charger_power = Math.Round(ChargingPower, 2);
                            changed = true;
                        }
                    }
                    else if (key == "IdealBatteryRange")
                    {
                        string v1 = value["stringValue"];
                        if (v1 == null)
                        {
                            v1 = value["doubleValue"];
                            if (v1 == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v1, NumberStyles.Any, CultureInfo.InvariantCulture, out double IdealBatteryRange))
                        {
                            lastIdealBatteryRange = Tools.MlToKm(IdealBatteryRange, 1);
                            lastRatedRange = lastIdealBatteryRange;
                            car.CurrentJSON.current_ideal_battery_range_km = lastIdealBatteryRange;
                            car.CurrentJSON.current_battery_range_km = lastIdealBatteryRange;
                            changed = true;
                        }
                    }
                    else if (key == "RatedRange")
                    {
                        string v1 = value["stringValue"];
                        if (v1 == null)
                        {
                            v1 = value["doubleValue"];
                            if (v1 == null)
                            {
                                continue;
                            }
                        }
                        if (double.TryParse(v1, NumberStyles.Any, CultureInfo.InvariantCulture, out double RatedRange))
                        {
                            lastRatedRange = Tools.MlToKm(RatedRange, 1);
                            car.CurrentJSON.current_battery_range_km = lastRatedRange;
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    car.CurrentJSON.CreateCurrentJSON();
                }

                if (cmd.Parameters.Count > 0 && IsCharging)
                {
                    InsertCharging(d, cmd);
                }
            }
        }

        private void InsertCharging(DateTime d, MySqlCommand cmd)
        {
            cmd.Parameters.AddWithValue("@battery_level", lastSoc);

            cmd.Parameters.AddWithValue("@charger_power", lastChargingPower);
            cmd.Parameters.AddWithValue("@ideal_battery_range_km", lastIdealBatteryRange);
            cmd.Parameters.AddWithValue("@battery_range_km", lastRatedRange);
            cmd.Parameters.AddWithValue("@outside_temp", lastOutsideTemp);
            cmd.Parameters.AddWithValue("@battery_heater", car.CurrentJSON.current_battery_heater);

            cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
            cmd.Parameters.AddWithValue("@Datum", d);

            var sb = new StringBuilder("insert into charging (");
            var sbc = new StringBuilder(") values (");
            var names = cmd.Parameters.Cast<MySqlParameter>()
                .Select(p => p.ParameterName.Substring(1))
                .ToArray();
            sb.Append(string.Join(", ", names));

            var values = cmd.Parameters.Cast<MySqlParameter>()
                .Select(p => p.ParameterName)
                .ToArray();
            sbc.Append(string.Join(", ", values));
            sbc.Append(")");

            sb.Append(sbc);

            sb.Append("\n ON DUPLICATE KEY UPDATE ");
            var update = cmd.Parameters.Cast<MySqlParameter>()
                .Where(w => w.ParameterName != "@CarID" && w.ParameterName != "@Datum")
                .Select(p => p.ParameterName.Substring(1) + "=" + p.ParameterName)
                .ToArray();

            sb.Append(string.Join(", ", update));
            cmd.CommandText = sb.ToString();

            if (databaseCalls)
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    cmd.Connection = con;
                    cmd.ExecuteNonQuery();
                    
                }
            }
            Log($"Insert Charging TR: {lastIdealBatteryRange}km");
        }

        public void InsertLocation(dynamic j, DateTime d, string resultContent, bool force = false)
        {
            try
            {
                if (!car.FleetAPI)
                    return;

                foreach (dynamic jj in j)
                {
                    string key = jj["key"];
                    dynamic value = jj["value"];
                    if (key == "Odometer")
                    {
                        string v1;
                        if (value.ContainsKey("stringValue"))
                        {
                            v1 = value["stringValue"];
                            v1 = v1.Replace("\"", "");

                        }
                        else if (value.ContainsKey("doubleValue"))
                        {
                            v1 = value["doubleValue"];
                        }
                        else
                        {
                            continue;
                        }

                        if (double.TryParse(v1, NumberStyles.Any, CultureInfo.InvariantCulture, out double Odometer))
                            lastOdometer = Tools.MlToKm(Odometer, 3);
                    }
                    else if (key == "Location")
                    {
                        dynamic locationValue = value["locationValue"];
                        if (locationValue != null)
                        {
                            latitude = locationValue["latitude"];
                            longitude = locationValue["longitude"];
                        }
                        else
                        {
                            string v = value["stringValue"];
                            if (v != null)
                            {
                                v = v.Replace("(", "").Replace(")", "");
                                v = v.Replace("\"", "");
                                var vs = v.Split(',');
                                if (vs.Length == 2)
                                {
                                    // Split the string into latitude and longitude parts
                                    string[] coordinates = v.Split(',');

                                    // Parse latitude
                                    string latitudeStr = coordinates[0].Trim();
                                    latitude = Double.Parse(latitudeStr.TrimEnd('N', 'S'), CultureInfo.InvariantCulture);
                                    if (latitudeStr.EndsWith("S"))
                                    {
                                        latitude = -latitude;
                                    }

                                    // Parse longitude
                                    string longitudeStr = coordinates[1].Trim();
                                    longitude = Double.Parse(longitudeStr.TrimEnd('E', 'W'), CultureInfo.InvariantCulture);
                                    if (longitudeStr.EndsWith("W"))
                                    {
                                        longitude = -longitude;
                                    }
                                }
                            }
                        }

                        if (lastLatitude != latitude || lastLongitude != longitude)
                        {
                            if (latitude != null && longitude != null)
                            {
                                if (lastLatitude != 0 && lastLongitude != 0)
                                {
                                    double bearing = Tools.CalculateBearing(lastLatitude, lastLongitude, latitude.Value, longitude.Value);
                                    car.CurrentJSON.heading = (int)bearing;
                                }

                                lastLatitude = latitude.Value;
                                lastLongitude = longitude.Value;
                            }
                        }

                    }
                    else if (key == "VehicleSpeed")
                    {
                        string v1;
                        if (value.ContainsKey("stringValue"))
                        {
                            v1 = value["stringValue"];
                            v1 = v1.Replace("\"", "");

                        }
                        else if (value.ContainsKey("doubleValue"))
                        {
                            v1 = value["doubleValue"];
                        }
                        else
                        {
                            continue;
                        }

                        if (Double.TryParse(v1, NumberStyles.Any, CultureInfo.InvariantCulture, out double s))
                            speed = s;
                    }
                }

                if (speed == null)
                {
                    speed = lastVehicleSpeed;
                }


                if (latitude != null && longitude != null && (speed != null || force))
                {
                    if (force && speed == null)
                        speed = 0;

                    Log("Insert Location" + (force ? " Force" : ""));

                    InsertLastLocation(d, false);
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                car.CreateExceptionlessClient(ex).AddObject(resultContent, "ResultContent").Submit();
            }
        }

        public static long DateTimeToUTC_UnixTimestamp(DateTime d)
        {
            return (long)(d.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds * 1000;
        }

        void InsertLastLocation(DateTime d, bool loggingPosId = true)
        {
            try
            {
                if (lastLatitude != 0 && lastLongitude != 0)
                {
                    long ts = DateTimeToUTC_UnixTimestamp(d);
                    
                    if (speed == null)
                        speed = 0;

                    lastposid = car.DbHelper.InsertPos(ts.ToString(), lastLatitude, lastLongitude, (int)speed.Value, null, lastOdometer, lastIdealBatteryRange, lastRatedRange, lastSoc, lastOutsideTemp, "");

                    if (loggingPosId)
                    {
                        Log("Insert Last Location ID: " + lastposid);
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                car.CreateExceptionlessClient(ex).Submit();
            }
        }

        private void handleLoginResponse(dynamic j)
        {
            try
            {
                dynamic response = j["Response"];
                dynamic updated_vehicles = response["updated_vehicles"];
                
                if (updated_vehicles == "1")
                {
                    string cfg = j["Config"];
                    Log("LoginRespone: OK / Config: " + cfg);
                    return;
                }

                dynamic skipped_vehicles = response["skipped_vehicles"];

                if (skipped_vehicles != null)
                {
                    dynamic missing_key = skipped_vehicles["missing_key"];

                    if (missing_key is JArray arrayMissing_key)
                    {
                        if (arrayMissing_key?.Count == 1)
                        {
                            dynamic mkvin = arrayMissing_key[0];
                            if (mkvin?.ToString() == car.Vin)
                            {
                                Log("LoginRespone: missing_key");
                                car.CurrentJSON.FatalError = "missing_key";
                                car.CurrentJSON.CreateCurrentJSON();
                                return;
                            }
                        }
                    }
                }

                Log("LoginRespone ERROR: " + response);
                car.CurrentJSON.FatalError = "Telemetry Login Error!!! Check Logfile!";
                car.CurrentJSON.CreateCurrentJSON();

                if (response.ToString().Contains("not_found"))
                {
                    Thread.Sleep(10 * 60 * 1000);
                }
                else if (response.ToString().Contains("token expired"))
                {
                    Log("Login Error: token expired!");
                    car.webhelper.GetToken();
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }


        private void InsertAlert(dynamic ji, string resultContent)
        {
            string name = ji["name"];
            DateTime startedAt = ji["startedAt"];
            DateTime? endedAt = null;
            if (ji.ContainsKey("endedAt"))
                endedAt = ji["endedAt"];

            dynamic audiences = ji["audiences"];

            int nameid = GetAlertNameID(name);

            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"insert into alerts (CarID, startedAt, nameID, endedAt) 
                    values (@CarID, @startedAt, @nameID, @endedAt)
                    ON DUPLICATE KEY UPDATE endedAt=@endedAt, id=LAST_INSERT_ID(id)", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    cmd.Parameters.AddWithValue("@startedAt", startedAt);
                    cmd.Parameters.AddWithValue("@nameID", nameid);

                    if (endedAt != null)
                        cmd.Parameters.AddWithValue("@endedAt", endedAt);
                    else
                        cmd.Parameters.AddWithValue("@endedAt", DBNull.Value);


                    int o = cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT LAST_INSERT_ID()";
                    cmd.Parameters.Clear();
                    object id = cmd.ExecuteScalar();

                    DBHelper.ExecuteSQLQuery("delete from alert_audiences where alertsid = " + id.ToString());

                    foreach (dynamic jau in audiences)
                    {
                        string s = jau.ToString();
                        int aid = -1;
                        switch (s)
                        {
                            case "Customer": aid = 1; break;
                            case "Service": aid = 2; break;
                            default:
                                car.CreateExeptionlessLog("Telemetry audience unknown", s, Exceptionless.Logging.LogLevel.Error).Submit();
                                Log("Audience unknown: " + s);
                                break;
                        }

                        DBHelper.ExecuteSQLQuery($"insert into alert_audiences (alertsID,audienceID) values ({id}, {aid})");

                    }
                }
            }
        }





        int GetAlertNameID(string name)
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM alert_names where Name = @name", con))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    object o = SQLTracer.TraceSc(cmd);

                    if (o != null)
                        return Convert.ToInt32(o);
                    else
                    {
                        cmd.CommandText = "insert into alert_names (name) values (@name)";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "SELECT LAST_INSERT_ID()";
                        cmd.Parameters.Clear();
                        object id = cmd.ExecuteScalar();
                        return Convert.ToInt32(id);

                    }
                }
            }
        }

        private void InsertCruiseStateTable(dynamic j, DateTime d, string resultContent)
        {
            try
            {
                foreach (dynamic jj in j)
                {
                    string key = jj["key"];
                    if (key == "CruiseState")
                    {
                        dynamic value = jj["value"];
                        if (value.ContainsKey("stringValue"))
                        {
                            string v1 = value["stringValue"];
                            v1 = v1.Replace("\"", "");

                            if (v1 != lastCruiseState)
                            {
                                lastCruiseState = v1;

                                int? state = null;
                                switch (v1)
                                {
                                    case "Off":
                                        state = 0;
                                        break;
                                    case "On":
                                        state = 1;
                                        break;
                                    case "Override":
                                        state = 2;
                                        break;
                                    case "Standby":
                                        state = -1;
                                        break;
                                    case "Standstill":
                                        state = -2;
                                        break;
                                    default:
                                        state = -99;
                                        Log("Unhandled Cruise State: " + v1);
                                        car.CreateExeptionlessLog("CruiseStateUnhandled", v1, Exceptionless.Logging.LogLevel.Warn).Submit();
                                        break;
                                }

                                if (state != null)
                                {
                                    using (var con = new MySqlConnection(DBHelper.DBConnectionstring))
                                    {
                                        con.Open();

                                        var cmd = new MySqlCommand("insert into cruisestate (CarId, date, state) values (@carid, @date, @state)", con);
                                        cmd.Parameters.AddWithValue("@carid", car.CarInDB);
                                        cmd.Parameters.AddWithValue("@date", d);
                                        cmd.Parameters.AddWithValue("@state", state);
                                        cmd.ExecuteNonQuery();

                                        Log("Telemetry Server: Cruise State");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                car.CreateExceptionlessClient(ex).AddObject(resultContent, "ResultContent").Submit();
            }
        }

        private void InsertBatteryTable(dynamic j, DateTime date, string resultContent)
        {
            try
            {
                var cols = new string[] {"PackVoltage", "PackCurrent", "IsolationResistance", "NumBrickVoltageMax", "BrickVoltageMax",
                "NumBrickVoltageMin", "BrickVoltageMin", "ModuleTempMax", "ModuleTempMin", "LifetimeEnergyUsed", "LifetimeEnergyUsedDrive"};

                double? BrickVoltageMin = null;
                double? BrickVoltageMax = null;
                bool currentJSONUpdated = false;

                using (var cmd = new MySqlCommand())
                {
                    foreach (dynamic jj in j)
                    {
                        string key = jj["key"];
                        if (cols.Any(key.Equals))
                        {

                            double d;
                            dynamic value = jj["value"];
                            if (value.ContainsKey("stringValue"))
                            {
                                string v1 = value["stringValue"];
                                v1 = v1.Replace("\"", "");
                                d = double.Parse(v1, Tools.ciEnUS);
                            }
                            else if (value.ContainsKey("doubleValue"))
                            {
                                string v1 = value["doubleValue"];
                                v1 = v1.Replace("\"", "");
                                d = double.Parse(v1, Tools.ciEnUS);
                            }
                            else if (value.ContainsKey("intValue"))
                            {
                                string v1 = value["intValue"];
                                v1 = v1.Replace("\"", "");
                                d = double.Parse(v1, Tools.ciEnUS);
                            }
                            else
                            {
                                continue;
                            }

                            cmd.Parameters.AddWithValue("@" + key, d);

                            if (key == "ModuleTempMin")
                            {
                                System.Diagnostics.Debug.WriteLine("ModuleTempMin: " + d);
                                car.CurrentJSON.lastScanMyTeslaReceived = DateTime.Now;
                                car.CurrentJSON.SMTCellTempAvg = d;
                                currentJSONUpdated = true;
                            }
                            else if (key == "BrickVoltageMin")
                            {
                                System.Diagnostics.Debug.WriteLine("BrickVoltageMin: " + d);
                                car.CurrentJSON.lastScanMyTeslaReceived = DateTime.Now;
                                car.CurrentJSON.SMTCellMinV = d;
                                currentJSONUpdated = true;
                                BrickVoltageMin = d;
                            }
                            else if (key == "BrickVoltageMax")
                            {
                                System.Diagnostics.Debug.WriteLine("BrickVoltageMax: " + d);
                                car.CurrentJSON.lastScanMyTeslaReceived = DateTime.Now;
                                car.CurrentJSON.SMTCellMaxV = d;
                                currentJSONUpdated = true;
                                BrickVoltageMax = d;
                            }
                            else if (key == "PackCurrent")
                            {
                                System.Diagnostics.Debug.WriteLine("PackCurrent: " + d);
                                lastPackCurrent = d;
                                lastPackCurrentDate = date;

                                CheckDetailedChargeState(date);

                                if (!acCharging && lastChargeState == "Enable")
                                {
                                    var current = PackCurrent(j, date);

                                    if (current > 1)
                                    {
                                        Log($"AC Charging  {current}A ***");
                                        StartACCharging(date);
                                    }
                                }

                                if (!dcCharging && lastFastChargerPresent)
                                {
                                    var current = PackCurrent(j, date);
                                    Log($"FastChargerPresent {current}A ***");

                                    if (current > 5)
                                    {
                                        Log($"DC Charging ***");
                                        StartDCCharging(date);
                                    }
                                }
                            }
                        }
                    }

                    if (currentJSONUpdated)
                    {
                        if (BrickVoltageMax.HasValue && BrickVoltageMin.HasValue)
                            car.CurrentJSON.SMTCellImbalance = (BrickVoltageMax - BrickVoltageMin) * 1000.0;

                        car.CurrentJSON.CreateCurrentJSON();
                    }

                    if (cmd.Parameters.Count > 0)
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.Parameters.AddWithValue("@date", date);

                        var sb = new StringBuilder("insert into battery (");
                        var sbc = new StringBuilder(") values (");
                        var names = cmd.Parameters.Cast<MySqlParameter>()
                            .Select(p => p.ParameterName.Substring(1))
                            .ToArray();
                        sb.Append(string.Join(", ", names));

                        var values = cmd.Parameters.Cast<MySqlParameter>()
                            .Select(p => p.ParameterName)
                            .ToArray();
                        sbc.Append(string.Join(", ", values));
                        sbc.Append(")");

                        sb.Append(sbc);

                        sb.Append("\n ON DUPLICATE KEY UPDATE ");
                        var update = cmd.Parameters.Cast<MySqlParameter>()
                            .Where(w => w.ParameterName != "@CarID" && w.ParameterName != "@date")
                            .Select(p => p.ParameterName.Substring(1) + "=" + p.ParameterName)
                            .ToArray();

                        sb.Append(string.Join(", ", update));
                        cmd.CommandText = sb.ToString();

                        if (databaseCalls)
                        {
                            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                            {
                                con.Open();
                                cmd.Connection = con;
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Telemetry Error: " + ex.ToString());
                car.CreateExceptionlessClient(ex).AddObject(resultContent, "ResultContent").Submit();
            }
        }

        private void StartDCCharging(DateTime date)
        {
            InsertFirstCharging(date);
            InsertLastLocation(date);
            dcCharging = true;
        }

        private void StartACCharging(DateTime date)
        {
            InsertFirstCharging(date);
            InsertLastLocation(date);
            acCharging = true;
        }

        private void InsertFirstCharging(DateTime date)
        {
            Log("InsertFirstCharging " + date.ToString() + " charge_energy_added: " + charge_energy_added);

            if (charge_energy_added == null)
                return;

            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Parameters.AddWithValue("@charge_energy_added", charge_energy_added);
                InsertCharging(date, cmd);
            }
        }

        void handleStatemachine(dynamic j, DateTime date, string resultContent)
        {
            try
            {
                var cols = new string[] { "ChargeState", "Gear", "VehicleSpeed", "Location", "FastChargerPresent", "DCChargingPower" };

                foreach (dynamic jj in j)
                {
                    string key = jj["key"];
                    if (cols.Any(key.Contains))
                    {
                        dynamic value = jj["value"];

                        if (key == "ChargeState")
                        {
                            if (value.ContainsKey("stringValue"))
                            {
                                string v1 = value["stringValue"];
                                v1 = v1.Replace("\"", "");
                                if (lastChargeState != v1)
                                {
                                    lastChargeState = v1;
                                    Log("ChargeState " + lastChargeState);
                                }

                                if (v1 == "Enable")
                                {
                                    if (Driving)
                                    {
                                        Log("Driving -> AC Charging ***");
                                        Driving = false;
                                    }

                                    if (!acCharging)
                                    {
                                        var current = PackCurrent(j, date);

                                        if (current > 2)
                                        {
                                            Log($"AC Charging  {current}A ***");
                                            InsertLastLocation(date);
                                            acCharging = true;
                                        }
                                    }
                                }
                                else if (v1 == "Idle")
                                {
                                    if (acCharging)
                                    {
                                        Log("Stop AC Charging ***");
                                        acCharging = false;
                                    }
                                }
                                else if (v1 == "QualifyLineConfig")
                                {

                                }
                                else if (v1 == "Startup")
                                {

                                }
                                else if(v1 == "ClearFaults")
                                {

                                }
                                else
                                {
                                    Log("unknown ChargeState: " + v1);
                                }
                            }
                        }
                        else if (key == "Gear")
                        {
                            string v1;
                            if (value.ContainsKey("stringValue"))
                            {
                                v1 = value["stringValue"];
                                v1 = v1.Replace("\"", "");
                            }
                            else if (value.ContainsKey("shiftStateValue"))
                            {
                                v1 = value["shiftStateValue"];
                            }
                            else
                            {
                                if (Driving)
                                {
                                    Log("Parking *** -> Gear Value = empty");
                                    driving = false;
                                }
                                continue;
                            }

                            if (v1.EndsWith("P"))
                            {
                                if (Driving)
                                {
                                    Log("Parking ***");
                                    driving = false;
                                }
                            }
                            else if (v1.Length > 0)
                            {
                                lastDriving = DateTime.Now;
                                
                                if (!Driving)
                                {
                                    Log("Driving ***");
                                    InsertFirstPos(date, 0);
                                    Driving = true;
                                }
                            }

                            Log("Gear: " + v1);

                        }
                        else if (key == "VehicleSpeed")
                        {
                            string v1;
                            if (value.ContainsKey("stringValue"))
                            {
                                v1 = value["stringValue"];
                                v1 = v1.Replace("\"", "");

                            }
                            else if (value.ContainsKey("doubleValue"))
                            {
                                v1 = value["doubleValue"];
                            }
                            else
                            {
                                continue;
                            }

                            if (Double.TryParse(v1, NumberStyles.Any, CultureInfo.InvariantCulture, out double speed))
                            {
                                if (speed > 0)
                                {
                                    lastVehicleSpeed = speed;
                                    lastVehicleSpeedDate = date;
                                    car.webhelper.lastIsDriveTimestamp = DateTime.Now;
                                    
                                    if (acCharging)
                                    {
                                        Log("Stop AC Charging by speed ***");
                                        acCharging = false;
                                    }

                                    if (dcCharging)
                                    {
                                        Log("Stop DC Charging by speed ***");
                                        dcCharging = false;
                                    }

                                    lastDriving = DateTime.Now;

                                    if (!Driving)
                                    {
                                        Log("Driving by speed ***");
                                        InsertFirstPos(date, (int)speed);
                                        Driving = true;
                                    }
                                }

                                Log("Speed: " + v1);
                            }
                        }
                        else if (key == "FastChargerPresent")
                        {
                            string v1;
                            if (value.ContainsKey("stringValue"))
                            {
                                v1 = value["stringValue"];
                                v1 = v1.Replace("\"", "");
                            }
                            else if (value.ContainsKey("booleanValue"))
                            {
                                v1 = value["booleanValue"];
                            }
                            else
                            {
                                continue;
                            }
                            v1 = v1.ToLower(CultureInfo.InvariantCulture);

                            if (v1 == "true")
                            {
                                if (!lastFastChargerPresent)
                                {
                                    Log("lastFastChargerPresent = true");
                                    lastFastChargerPresent = true;
                                }
                                
                                if (Driving)
                                {
                                    Log("Driving -> DC Charging ***");
                                    Driving = false;
                                }

                                if (!dcCharging)
                                {
                                    var current = PackCurrent(j, date);
                                    Log($"FastChargerPresent {current}A ***");

                                    if (current > 5)
                                    {
                                        Log($"DC Charging ***");

                                        StartDCCharging(date);
                                    }
                                }
                            }
                            else if (v1 == "false")
                            {
                                lastFastChargerPresent = false;
                                
                                if (dcCharging)
                                {
                                    Log("stop DC Charging ***");
                                    dcCharging = false;
                                }
                            }
                        }
                        else if (key == "DCChargingPower")
                        {
                            string v1;
                            if (value.ContainsKey("stringValue"))
                            {
                                v1 = value["stringValue"];
                                v1 = v1.Replace("\"", "");

                            }
                            else if (value.ContainsKey("doubleValue"))
                            {
                                v1 = value["doubleValue"];
                            }
                            else
                            {
                                continue;
                            }

                            if (!dcCharging && lastFastChargerPresent)
                            {
                                double d = double.Parse(v1, Tools.ciEnUS);

                                Log($"FastChargerPresent DCChargingPower " + d);
                                if (d > 5)
                                {
                                    Log($"DC Charging ***");
                                    StartDCCharging(date);
                                }
                            }
                        }
                        else
                        {
                            Log($"Key: {key}");
                        }
                    }
                }

                CheckDriving();

            }
            catch (Exception ex)
            {
                Log("Telemetry Error: " + ex.ToString());
                car.CreateExceptionlessClient(ex).AddObject(resultContent, "ResultContent").Submit();
            }

        }

        private void InsertFirstPos(DateTime date, int speed)
        {
            long ts = DateTimeToUTC_UnixTimestamp(date);

            if (databaseCalls)
                lastposid = car.DbHelper.InsertPos(ts.ToString(), lastLatitude, lastLongitude, speed, null, lastOdometer, lastIdealBatteryRange, lastRatedRange, lastSoc, lastOutsideTemp, "");

            Log($"InsertFirstPos {date} ID: {lastposid}");
        }

        internal double? PackCurrent(dynamic o, DateTime date)
        {
            JToken j = null;
            try
            {
                j = o.SelectToken("$[?(@.key=='PackCurrent')].value.stringValue");
                if (j == null)
                {
                    j = o.SelectToken("$[?(@.key=='PackCurrent')].value.doubleValue");
                    if (j == null)
                    {
                        var ts = date - lastPackCurrentDate;
                        if (ts.TotalSeconds < 10)
                        {
                            return lastPackCurrent;
                        }

                        return null;
                    }
                }
                
                double? val = j.Value<double?>();
                return val;
            }
            catch (Exception e)
            {
                try
                {
                    string v = j.Value<string>();
                    if (!String.IsNullOrEmpty(v))
                    {
                        v = v.Replace("\"", "");
                        return Double.Parse(v, CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception e2)
                {
                    var ts = date - lastPackCurrentDate;
                    if (ts.TotalSeconds < 10)
                    {
                        return lastPackCurrent;
                    }

                    Logfile.Log("*** FT: PackCurrent2 " + e2.ToString());
                    e2.ToExceptionless().FirstCarUserID().Submit();
                }

                Logfile.Log("*** FT: PackCurrent " + e.ToString());
                e.ToExceptionless().FirstCarUserID().Submit();
            }

            return null;
        }

        private void CheckDriving()
        {
            if (Driving)
            {
                var ts = DateTime.Now - lastDriving;
                if (ts.TotalMinutes > 60)
                {
                    Log("Driving stop by speed " + lastDriving.ToString());
                    Driving = false;
                }
            }
        }
    }
}
