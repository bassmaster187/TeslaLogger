using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeslaLogger;

namespace TeslaLoggerNET8.Lucid
{
    internal class LucidWebHelper : WebHelper
    {
        string lastData = "";
        private string charge_state;
        private double charge_energy_added;
        private double charger_power;
        private DateTime lastNewData;
        private double battery_level;
        private double ideal_battery_range;
        private int session_minutes_remaining;
        private string power;
        private double speed;
        private string gear_position;
        private double latitude;
        private double longitude;
        private long last_updated_ms;
        private double kwhr;
        private double kw;
        private double front_left_tire_pressure_bar;
        private double rear_left_tire_pressure_bar;
        private double front_right_tire_pressure_bar;
        private double rear_right_tire_pressure_bar;
        private long tire_pressure_last_updated;
        private double max_cell_temp;
        private double min_cell_temp;
        private double max_cell_temp_db;
        private double min_cell_temp_db;


        internal LucidWebHelper(Car car) : base(car)
        {
            car.CurrentJSON.current_car_version = car.DbHelper.GetLastCarVersion();
        }

        public override bool RestoreToken()
        {
            return true;
        }

        public override string GetVehicles()
        {
            return "";
        }

        public override async Task<string> IsOnline(bool returnOnUnauthorized = false)
        {
            GetNewData();

            if (power == "POWER_STATE_SLEEP")
                return "asleep";

            return "online";
        }

        public override bool IsDriving(bool justinsertdb = false)
        {
            GetNewData();
            bool isDriving = power == "POWER_STATE_DRIVE" || gear_position == "GEAR_DRIVE" || gear_position == "GEAR_REVERSE";

            if (justinsertdb || isDriving)
            {
                var ts = Tools.ToUnixTime(DateTime.UtcNow) * 1000;
                _ = SendDataToAbetterrouteplannerAsync(ts, battery_level, speed, false, kw, latitude, longitude);
                int id = car.DbHelper.InsertPos(ts.ToString(), latitude, longitude, (int)Math.Round(speed), (decimal)kw, car.CurrentJSON.current_odometer, Tools.MlToKm(ideal_battery_range, 1), Tools.MlToKm(ideal_battery_range, 1), battery_level, car.CurrentJSON.current_outside_temperature, elevation);
                car.Log("Insert Pos " + id);
            }
            
            if (isDriving)
                lastIsDriveTimestamp = DateTime.Now;

            return isDriving;
        }

        public override bool IsCharging(bool justCheck = false, bool noMemcache = false)
        {
            GetNewData();
            var charging = charge_state == "CHARGE_STATE_CHARGING";
            var ts = Tools.ToUnixTime(DateTime.UtcNow) * 1000;

            string chargerPower = ((int)Math.Round(charger_power)).ToString();
            string batteryLevel = ((int)Math.Round(battery_level)).ToString();

            if (!justCheck)
            {
                car.Log("Insert Charging " + batteryLevel);
                _ = SendDataToAbetterrouteplannerAsync(ts, car.CurrentJSON.current_battery_level, 0, true, charger_power, car.CurrentJSON.GetLatitude(), car.CurrentJSON.GetLongitude());
                car.DbHelper.InsertCharging(ts.ToString(), batteryLevel, charge_energy_added.ToString(), chargerPower,  (double)ideal_battery_range, (double)ideal_battery_range, "0", "0", "0", 0.0, car.IsHighFrequenceLoggingEnabled(true), "0", "0");
            }

            return charging;
        }

        public override async Task<double> GetOdometerAsync()
        {
            GetNewData();

            return car.CurrentJSON.current_odometer;
        }

        private void GetNewData()
        {
            var ts = DateTime.UtcNow - lastNewData;
            if (ts.TotalSeconds < 5)
                return;

            var last_updated_ms_before = last_updated_ms;
            var kwhr_before = kwhr;

            //lastData = System.IO.File.ReadAllText(@"C:\dev\TeslaLoggerNET8\TeslaLogger\bin\Debug\net8.0\lucid\20250430122944434.txt");
            lastData = ExecuteShellCommand();
            lastNewData = DateTime.UtcNow;

            // var j = JsonConvert.DeserializeObject(ret);

            // System.IO.File.WriteAllText("lucid/" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".txt", lastData);

            string[] lines = lastData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                try
                {
                    string[] parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        switch (key)
                        {
                            case "latitude":
                                car.CurrentJSON.SetLatitude(double.Parse(value, CultureInfo.InvariantCulture));
                                latitude = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "longitude":
                                car.CurrentJSON.SetLongitude(double.Parse(value, CultureInfo.InvariantCulture));
                                longitude = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "elevation":
                                elevation = ((int)(double.Parse(value, CultureInfo.InvariantCulture)) / 100.0).ToString() ;
                                break;
                            case "heading_precise":
                                car.CurrentJSON.heading = (int)double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "position_time":
                                // Console.WriteLine($"Position Time: {value}");
                                break;
                            case "remaining_range":
                                ideal_battery_range = Tools.KmToMl(double.Parse(value, CultureInfo.InvariantCulture));
                                car.CurrentJSON.current_ideal_battery_range_km = ideal_battery_range;
                                break;
                            case "charge_percent":
                                battery_level = double.Parse(value, CultureInfo.InvariantCulture);
                                car.CurrentJSON.current_battery_level = Math.Round(battery_level,1);
                                break;
                            case "kwhr":
                                //Console.WriteLine($"Kwhr: {value}");
                                kwhr = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "odometer_km":
                                car.CurrentJSON.current_odometer = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "charge_state":
                                charge_state = value;
                                break;
                            case "charge_session_kwh":
                                charge_energy_added = Math.Round(double.Parse(value, CultureInfo.InvariantCulture), 1);
                                break;
                            case "charge_rate_kwh_precise":
                                charger_power = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "session_minutes_remaining":
                                session_minutes_remaining = int.Parse(value, CultureInfo.InvariantCulture);
                                car.CurrentJSON.current_time_to_full_charge = session_minutes_remaining / 60.0;
                                break;
                            case "charge_limit_percent":
                                car.CurrentJSON.charge_limit_soc = (int)double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "exterior_temp":
                                car.CurrentJSON.current_outside_temperature = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "interior_temp":
                                car.CurrentJSON.current_inside_temperature = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "min_cell_temp":
                                car.CurrentJSON.SMTCellTempAvg = double.Parse(value, CultureInfo.InvariantCulture);
                                car.CurrentJSON.lastScanMyTeslaReceived = DateTime.Now;
                                min_cell_temp = (double)car.CurrentJSON.SMTCellTempAvg;
                                break;
                            case "software_version":
                                handleCarVersion(value);
                                break;
                            case "power":
                                if (!value.Contains("HVAC"))
                                {
                                    if (power != value)
                                        car.Log("Power: " + value + " Range: " + Tools.MlToKm(ideal_battery_range, 1));
                                    
                                    power = value;
                                }
                                break;
                            case "speed":
                                speed = Tools.KmToMl(double.Parse(value, CultureInfo.InvariantCulture), 1);
                                break;
                            case "gear_position":
                                if (gear_position != value)
                                {
                                    car.Log("Gear Position: " + value);
                                    if (value == "GEAR_DRIVE")
                                        SetLastShiftState("D");
                                    else if (value == "GEAR_REVERSE")
                                        SetLastShiftState("R");
                                    else if (value == "GEAR_PARK")
                                        SetLastShiftState("P");
                                    else if (value == "GEAR_NEUTRAL")
                                        SetLastShiftState("N");
                                    else
                                        SetLastShiftState("");
                                }

                                gear_position = value;
                                break;
                            case "last_updated_ms":
                                last_updated_ms = long.Parse(value, CultureInfo.InvariantCulture);
                                DateTime lastUpdated = DateTimeOffset.FromUnixTimeMilliseconds(last_updated_ms).DateTime;
                                break;

                            case "front_left_tire_pressure_bar":
                                front_left_tire_pressure_bar = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "front_right_tire_pressure_bar":
                                front_right_tire_pressure_bar = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "rear_left_tire_pressure_bar":
                                rear_left_tire_pressure_bar = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "rear_right_tire_pressure_bar":
                                rear_right_tire_pressure_bar = double.Parse(value, CultureInfo.InvariantCulture);
                                break;
                            case "tire_pressure_last_updated":
                                var tire_pressure_last_updated2 = long.Parse(value, CultureInfo.InvariantCulture);
                                if (tire_pressure_last_updated2 != tire_pressure_last_updated)
                                {
                                    DateTime d = DateTimeOffset.FromUnixTimeMilliseconds(tire_pressure_last_updated2 * 1000).DateTime;
                                    tire_pressure_last_updated = tire_pressure_last_updated2;
                                    car.DbHelper.InsertTPMS(1, front_left_tire_pressure_bar, d);
                                    car.DbHelper.InsertTPMS(2, front_right_tire_pressure_bar, d);
                                    car.DbHelper.InsertTPMS(3, rear_left_tire_pressure_bar, d);
                                    car.DbHelper.InsertTPMS(4, rear_right_tire_pressure_bar, d);
                                }
                                break;

                            case "max_cell_temp":
                                max_cell_temp = double.Parse(value, CultureInfo.InvariantCulture);
                                break;

                            default:
                                //Console.WriteLine($"Unknown Key: '{key}', Value: {value}");
                                break;

                        }
                    }
                }
                catch (Exception ex)
                {
                    car.Log($"Error parsing line '{line}': {ex.Message}");
                }
            }

            // calculate power in kW
            if (last_updated_ms_before != last_updated_ms && last_updated_ms_before > 0)
                CalculatePower(last_updated_ms_before, kwhr_before);

            if (Math.Abs(max_cell_temp - max_cell_temp_db) > 0.25 || Math.Abs(min_cell_temp - min_cell_temp_db) > 0.25)
                InsertBatteryTemperatureData();

            car.CurrentJSON.CreateCurrentJSON();
        }

        private void CalculatePower(long last_updated_ms_before, double kwhr_before)
        {
            var kwhrdiff = kwhr_before - kwhr;
            var ms = last_updated_ms - last_updated_ms_before;
            double p = kwhrdiff * 3600000.0 / (double)ms;

            kw = p;
            car.Log($"kwhr {kwhr} / kwhrdiff {kwhrdiff} / ms {ms} / p: {p}kW");
        }

        private void InsertBatteryTemperatureData()
        {
            try
            {
                car.Log("Insert Battery Temperature Data: " + min_cell_temp + " / " + max_cell_temp);
                string sql = "insert into battery (ModuleTempMin, ModuleTempMax, CarID, date) values (@ModuleTempMin, @ModuleTempMax, @CarID, @date)";
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                using (var cmd = new MySqlCommand())
                {
                    con.Open();
                    cmd.Connection = con;
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@ModuleTempMin", min_cell_temp);
                    cmd.Parameters.AddWithValue("@ModuleTempMax", max_cell_temp);
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    cmd.Parameters.AddWithValue("@date", DateTime.UtcNow);
                    cmd.ExecuteNonQuery();
                }
                max_cell_temp_db = max_cell_temp;
                min_cell_temp_db = min_cell_temp;
            }
            catch (Exception ex)
            {
                car.Log("Error inserting battery temperature data: " + ex.ToString());
            }
        }

        private void handleCarVersion(string value)
        {
            string car_version = value.Replace("\"", "") + " 0000";

            if (!String.IsNullOrEmpty(car_version))
            {
                if (car.CurrentJSON.current_car_version != car_version)
                {
                    car.Log("Car Version: " + car_version);
                    car.CurrentJSON.current_car_version = car_version;

                    car.DbHelper.SetCarVersion(car_version);

                    car.webhelper.TaskerWakeupfile(true);
                }

            }

        }

        public string ExecuteShellCommand()
        {
            try
            {
                ProcessStartInfo processInfo = null;

                if (Directory.Exists("/etc/lucidapi/examples"))
                {
                    processInfo = new ProcessStartInfo
                    {
                        FileName = "python3.13",
                        Arguments = @"/etc/lucidapi/examples/vehicle_info.py",
                        WorkingDirectory = @"/etc/lucidapi/examples",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }
                else
                {
                    processInfo = new ProcessStartInfo
                    {
                        FileName = "py",
                        Arguments = @"C:\dev\LucidAPI\python-lucidmotors\examples\vehicle_info.py",
                        WorkingDirectory = @"C:\dev\LucidAPI\python-lucidmotors\examples",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }

                using (Process process = Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        // Console.WriteLine("Output:");
                        // Console.WriteLine(output);

                        if (!string.IsNullOrEmpty(error))
                        {
                            car.Log("Error: " + error);
                        }

                        return output;
                    }
                }
            }
            catch (Exception ex)
            {
                car.Log($"An error occurred: {ex.Message}");
            }

            return string.Empty;
        }

        internal override void CheckRefreshToken()
        {
        }
    }
}
