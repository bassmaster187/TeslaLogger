using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.IO;
using Exceptionless;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Google.Protobuf.WellKnownTypes;

namespace TeslaLogger
{
    internal class TelemetryConnection
    {
        private Car car;
        Thread t;
        CancellationTokenSource cts = new CancellationTokenSource();
        ClientWebSocket ws = null;
        Random r = new Random();

        String lastCruiseState = "";

        bool connect;

        private bool driving;
        private bool _acCharging;
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

        public String lastChargeState = "";

        public bool lastFastChargerPresent = false;

        public int lastposid = 0;
        private double lastIdealBatteryRange;
        private double? lastOdometer;
        private double? lastOutsideTemp;

        double lastLatitude = 0;
        double lastLongitude = 0;
        private double lastRatedRange;

        void Log(string message)
        {
            car.Log("*** FT: " +  message);
        }

        public bool Driving
        {
            get
            {
                if (driving)
                {
                    var ts = DateTime.Now - lastDriving;
                    if (ts.TotalMinutes > 30)
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
            set => driving = value;
        }

        bool acCharging { get => _acCharging; 
            set { 
                if (_acCharging != value)
                    Log("ACCharging = " +  value);

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
            

        public TelemetryConnection(Car car)
        {
            this.car = car;
            if (car == null)
                return;

            lastIdealBatteryRange = car.CurrentJSON.current_ideal_battery_range_km;
            lastSoc = car.CurrentJSON.current_battery_level;
            lastOdometer = car.CurrentJSON.current_odometer;
            lastRatedRange = car.CurrentJSON.current_battery_range_km;

            t = new Thread(() => { Run(); });
            t.Start();
        }

        public void CloseConnection()
        {
            try
            {
                if (car.FleetAPI)
                    return;

                Log("Telemetry Server close connection!");
                connect = false;
                cts.Cancel();

            } catch (Exception ex)
            {
                car.Log("Telemetry CloseConnection " +  ex.Message);
            }
        }

        public void StartConnection()
        {
            try
            {
                if (connect)
                    return;

                Log("Telemetry Server start connection");
                cts = new CancellationTokenSource();
                connect = true;
            }
            catch (Exception ex)
            {
                Log("Telemetry StartConnection " + ex.Message);
            }
        }

        private void Run()
        {
            while (true)
            {
                try
                {
                    while (!connect)
                        Thread.Sleep(1000);

                    ConnectToServer();

                    if (ws == null)
                        continue;

                    Login();

                    while (ws.State == WebSocketState.Open)
                    {
                        Thread.Sleep(100);
                        ReceiveAsync(ws).Wait();
                    }
                }
                catch (Exception ex)
                {
                    if (!connect && ex.InnerException is TaskCanceledException)
                        System.Diagnostics.Debug.WriteLine("Telemetry Cancel OK");
                    else if (ex.InnerException?.InnerException is System.Net.Sockets.SocketException se)
                    {
                        Log(se.Message);
                        car.CreateExceptionlessClient(ex.InnerException).Submit();
                    }
                    else if (ex.InnerException?.InnerException != null)
                    {
                        Log(ex.InnerException.Message);
                        car.CreateExceptionlessClient(ex.InnerException).Submit();
                    }
                    else
                    {
                        Log("Telemetry Exception: " + ex.ToString());
                        car.CreateExceptionlessClient(ex).Submit();
                    }

                    var s = r.Next(30000, 60000);
                    Thread.Sleep(s);
                }
            }
        }

        private void handleMessage(string resultContent)
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
                        Log("Telemetry Server Data");
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
                Log(ex.ToString()+ "\n" + resultContent);
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
                        if (v == "Armed")
                        {
                            car.webhelper.is_sentry_mode = true;
                            car.CurrentJSON.current_is_sentry_mode = true;
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                        else
                        {
                            car.webhelper.is_sentry_mode = false;
                            car.CurrentJSON.current_is_sentry_mode = false;
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                        car.CurrentJSON.CreateCurrentJSON();
                    }
                    else if (key == "PreconditioningEnabled")
                    {
                        bool preconditioning = false;

                        string v = value["stringValue"];
                        if (v == "True")
                            preconditioning = true;

                        car.CurrentJSON.current_is_preconditioning = preconditioning;
                        Log("Preconditioning: " + preconditioning);
                        car.CurrentJSON.CreateCurrentJSON();
                    }
                    else if (key == "OutsideTemp")
                    {
                        string v = value["stringValue"];
                        if (double.TryParse(v, out double OutsideTemp))
                        {
                            lastOutsideTemp = OutsideTemp;
                            car.CurrentJSON.current_outside_temperature = OutsideTemp;
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                    }
                    else if (key == "TimeToFullCharge")
                    {
                        string v = value["stringValue"];
                        if (double.TryParse(v, out double TimeToFullCharge))
                        {
                            car.CurrentJSON.current_time_to_full_charge = TimeToFullCharge;
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                    }
                    else if (key == "ChargeLimitSoc")
                    {
                        string v = value["stringValue"];
                        if (double.TryParse(v, out double ChargeLimitSoc))
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
                        if (double.TryParse(v, out double MinutesToArrival))
                        {
                            car.CurrentJSON.active_route_minutes_to_arrival = (int)MinutesToArrival;
                            car.CurrentJSON.CreateCurrentJSON();
                        }
                    }
                    else if (key == "MilesToArrival")
                    {
                        string v = value["stringValue"];
                        if (double.TryParse(v, out double MilesToArrival))
                        {
                            car.CurrentJSON.active_route_km_to_arrival = (long)(MilesToArrival * 1.609344);
                            car.CurrentJSON.CreateCurrentJSON();
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
                    else if (key.StartsWith("TpmsPressure"))
                    {
                        string suffix = key.Substring(key.Length - 2);
                        string v = value["stringValue"];
                        if (double.TryParse(v, out double pressure))
                        {
                            pressure = Math.Round(pressure, 2);
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
                        if (bool.TryParse(v, out bool serviceMode))
                        {
                            //TODO : Implement ServiceMode
                        }
                    }
                    else if (key == "CarType")
                    {
                        string v = value["stringValue"];
                        if (!String.IsNullOrEmpty(v))
                        {
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
                        string Locked = value["stringValue"];
                        if (bool.TryParse(Locked, out bool l))
                        {
                            car.teslaAPIState.AddValue("locked", "bool", l, Tools.ToUnixTime(d), "vehicle_state");
                            car.CurrentJSON.CreateCurrentJSON();

                        }
                    }
                    else if (key.EndsWith("Window"))
                    {
                        string apistatekey = key.ToLower().Insert(2, "_");
                        string Window = value["stringValue"];

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

                    if (key == "ACChargingEnergyIn" && acCharging)
                    {
                        string v1 = value["stringValue"];
                        if (double.TryParse(v1, out ChargingEnergyIn))
                        {
                            ChargingEnergyIn = Math.Round(ChargingEnergyIn, 2);
                            cmd.Parameters.AddWithValue("@charge_energy_added", ChargingEnergyIn);
                            car.CurrentJSON.current_charge_energy_added = ChargingEnergyIn;
                            changed = true;
                        }

                    }
                    else if (key == "DCChargingEnergyIn" && dcCharging)
                    {
                        string v1 = value["stringValue"];
                        if (double.TryParse(v1, out ChargingEnergyIn))
                        {
                            ChargingEnergyIn = Math.Round(ChargingEnergyIn, 2);
                            cmd.Parameters.AddWithValue("@charge_energy_added", ChargingEnergyIn);
                            car.CurrentJSON.current_charge_energy_added = ChargingEnergyIn;
                            changed = true;
                        }
                    }
                    else if (key == "Soc")
                    {
                        string v1 = value["stringValue"];
                        if (double.TryParse(v1, out double Soc))
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
                        if (double.TryParse(v1, out double ChargingPower))
                        {
                            lastChargingPower = ChargingPower;
                            car.CurrentJSON.current_charger_power = Math.Round(ChargingPower, 2);
                            changed = true;
                        }
                    }
                    else if (key == "DCChargingPower" && dcCharging)
                    {
                        string v1 = value["stringValue"];
                        if (double.TryParse(v1, out double ChargingPower))
                        {
                            lastChargingPower = ChargingPower;
                            car.CurrentJSON.current_charger_power = Math.Round(ChargingPower, 2);
                            changed = true;
                        }
                    }
                    else if (key == "IdealBatteryRange")
                    {
                        string v1 = value["stringValue"];
                        if (double.TryParse(v1, out double IdealBatteryRange))
                        {
                            lastIdealBatteryRange = Tools.MlToKm(IdealBatteryRange, 1);
                            car.CurrentJSON.current_ideal_battery_range_km = lastIdealBatteryRange;
                            changed = true;
                        }
                    }
                    else if (key == "RatedRange")
                    {
                        string v1 = value["stringValue"];
                        if (double.TryParse(v1, out double RatedRange))
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

            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                cmd.Connection = con;
                cmd.ExecuteNonQuery();
                Log($"Insert Charging TR: {lastIdealBatteryRange}km");
            }
        }

        public void InsertLocation(dynamic j, DateTime d, string resultContent, bool force = false)
        {
            try
            {
                if (!car.FleetAPI)
                    return;

                double? latitude = null;
                double? longitude = null;
                double? speed = null;

                foreach (dynamic jj in j)
                {
                    string key = jj["key"];
                    dynamic value = jj["value"];
                    if (key == "Odometer")
                    {
                        string v = value["stringValue"];
                        if (v != null)
                        {
                            if (double.TryParse(v, out double Odometer))
                                lastOdometer = Tools.MlToKm(Odometer, 3);
                        }
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
                        string v1 = value["stringValue"];
                        if (v1 != null)
                        {
                            v1 = v1.Replace("\"", "");
                            if (Double.TryParse(v1, out double s))
                                speed = s;
                        }
                    }
                }

                if (speed == null)
                {
                    var ts = d - lastVehicleSpeedDate;
                    if (ts.TotalSeconds < 20)
                        speed = lastVehicleSpeed;
                }


                if (latitude != null && longitude != null && (speed != null || force))
                {
                    if (force && speed == null)
                        speed = 0;

                    long ts= (long)(d.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds*1000;
                    Log("Insert Location" + (force ? " Force" : ""));
                    lastposid = car.DbHelper.InsertPos(ts.ToString(), latitude.Value, longitude.Value, (int)speed.Value, null, lastOdometer, lastIdealBatteryRange, lastRatedRange, lastSoc, lastOutsideTemp, "");
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                car.CreateExceptionlessClient(ex).AddObject(resultContent, "ResultContent").Submit();
            }
        }

        private void handleLoginResponse(dynamic j)
        {
            try
            {
                dynamic response = j["Response"];
                dynamic updated_vehicles = response["updated_vehicles"];
                if (updated_vehicles == "1")
                    Log("LoginRespone: OK");
                else
                {
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
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private async Task ReceiveAsync(WebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            WebSocketReceiveResult result;

            String data;

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                do
                {
                    result = await socket.ReceiveAsync(buffer, cts.Token);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                    throw new Exception("CLOSE");

                ms.Seek(0, System.IO.SeekOrigin.Begin);

                data = Encoding.UTF8.GetString(ms.ToArray());
            }

            handleMessage(data);            
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
            catch (Exception ex) { 
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
                        if (cols.Any(key.Contains))
                        {
                            dynamic value = jj["value"];
                            if (value.ContainsKey("stringValue"))
                            {
                                string v1 = value["stringValue"];
                                v1 = v1.Replace("\"", "");
                                double d = double.Parse(v1, Tools.ciEnUS);
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

                                    if (!acCharging && lastChargeState == "Enable")
                                    {
                                        var current = PackCurrent(j, date);

                                        if (current > 2)
                                        {
                                            Log($"AC Charging  {current}A ***");
                                            InsertFirstCharging(date);
                                            InsertLocation(j, date, resultContent, true);
                                            acCharging = true;
                                        }
                                    }

                                    if (!dcCharging && lastFastChargerPresent)
                                    {
                                        var current = PackCurrent(j, date);
                                        Log($"FastChargerPresent {current}A ***");

                                        if (current > 5)
                                        {
                                            Log($"DC Charging ***");
                                            InsertFirstCharging(date);
                                            InsertLocation(j, date, resultContent, true);
                                            dcCharging = true;
                                        }
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

                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();
                            cmd.Connection = con;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            } catch (Exception ex)
            {
                Log("Telemetry Error: " + ex.ToString());
                car.CreateExceptionlessClient(ex).AddObject(resultContent, "ResultContent").Submit();
            }
        }

        private void InsertFirstCharging(DateTime date)
        {
            Log("InsertFirstCharging " + date.ToString());

            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Parameters.AddWithValue("@charge_energy_added", 0);
                InsertCharging(date, cmd);
            }
        }

        void handleStatemachine(dynamic j, DateTime date, string resultContent)
        {
            try
            {
                var cols = new string[] { "ChargeState", "Gear", "VehicleSpeed","Location", "FastChargerPresent" };

                foreach (dynamic jj in j)
                {
                    string key = jj["key"];
                    if (cols.Any(key.Contains))
                    {
                        dynamic value = jj["value"];
                        if (value.ContainsKey("stringValue"))
                        {
                            string v1 = value["stringValue"];
                            v1 = v1.Replace("\"", "");

                            if (key == "ChargeState")
                            {
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
                                            InsertLocation(j, date, resultContent, true);
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
                                else
                                {
                                    Log("unknown ChargeState: " + v1);
                                }

                            }
                            else if (key == "Gear")
                            {
                                if (v1 == "P")
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
                                if (Double.TryParse(v1, out double speed))
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

                                            InsertLocation(j, date, resultContent, true);
                                            dcCharging = true;
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
                            else
                            {
                                Log($"Key: {key} / Value: {v1}");
                            }
                        }
                        else
                        {
                            if (key == "Gear")
                            {
                                if (Driving)
                                {
                                    Log("Parking *** -> Gear Value = empty");
                                    driving = false;
                                }

                            }
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
            long ts = (long)(date.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds * 1000;
            
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
                    var ts = date - lastPackCurrentDate;
                    if (ts.TotalSeconds < 10)
                    {
                        return lastPackCurrent;
                    }

                    return null;
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
                if (ts.TotalMinutes > 5)
                {
                    Log("Driving stop by speed " + lastDriving.ToString());
                    Driving = false;
                }
            }
        }

        private void ConnectToServer()
        {
            Log("Connect to Telemetry Server");

            if (ws != null)
                ws.Dispose();

            ws = null;
            
            try
            {
                var cws = new ClientWebSocket();
                Task tc = cws.ConnectAsync(new Uri(ApplicationSettings.Default.TelemetryServerURL), cts.Token);
                tc.Wait();

                ws = cws;
            }
            catch (Exception ex)
            {
                if (ex is AggregateException ex2)
                {
                    Log("Connect to Telemetry Server Error: " + ex2.InnerException.Message);
                    if (ex.InnerException != null)
                        car.CreateExceptionlessClient(ex2.InnerException).Submit();
                    else
                        car.CreateExceptionlessClient(ex2).Submit();

                    Thread.Sleep(60000);
                }
                else
                {
                    Log("Connect to Telemetry Server Error: " + ex.Message);
                    car.CreateExceptionlessClient(ex).Submit();
                    Thread.Sleep(60000);
                }
            }
        }

        private void Login()
        {
            
            string configname = "";
            if (car.FleetAPI)
                configname = "paid";

            Log("Login to Telemetry Server / config: " + configname);
            string vin = car.Vin;
            // vin = "LRW3E7EK6NC483045"; // xxx
            string fw = car.CurrentJSON.current_car_version;
            if (fw?.Contains(" ") == true)
                fw = fw.Substring(0, fw.IndexOf(" ")).Trim();

            Dictionary<string, object> login = new Dictionary<string, object>{
                    { "msg_type", "login"},
                    { "vin", vin},
                    { "token", car.TaskerHash},
                    { "FW", fw},
                    { "accesstoken", car.Tesla_Token},
                    { "regionurl", car.webhelper.apiaddress},
                    { "config", configname},
                    { "version", Assembly.GetExecutingAssembly().GetName().Version.ToString()}
                };

            var jLogin = JsonConvert.SerializeObject(login);
            SendString(ws, jLogin);
        }

        public Task SendString(ClientWebSocket ws, String data)
        {
            var encoded = Encoding.UTF8.GetBytes(data);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);
            return ws.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
        }
    }
}