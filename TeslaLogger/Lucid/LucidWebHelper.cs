using Newtonsoft.Json.Linq;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        internal LucidWebHelper(Car car) : base(car)
        {
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
            return "online";
        }

        public override bool IsDriving(bool justinsertdb = false)
        {
            GetNewData();
            
            if (justinsertdb || gear_position == "GEAR_DRIVE" || gear_position == "GEAR_REVERSE")
            {
                car.Log("Insert Pos");
                var ts = Tools.ToUnixTime(DateTime.UtcNow) * 1000;
                car.DbHelper.InsertPos(ts.ToString(), latitude, longitude, (int)Math.Round(speed), null, car.CurrentJSON.current_odometer, ideal_battery_range, ideal_battery_range, battery_level, car.CurrentJSON.current_outside_temperature, elevation);
            }

            return power == "POWER_STATE_DRIVE" || gear_position == "GEAR_DRIVE" || gear_position == "GEAR_REVERSE";
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
                car.DbHelper.InsertCharging(ts.ToString(), batteryLevel, charge_energy_added.ToString(), chargerPower, (double)ideal_battery_range, (double)ideal_battery_range, "0", "0", "0", 0.0, car.IsHighFrequenceLoggingEnabled(true), "0", "0");
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
                                elevation = value;
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
                                car.CurrentJSON.current_battery_level = battery_level;
                                break;
                            case "kwhr":
                                //Console.WriteLine($"Kwhr: {value}");
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
                                break;
                            case "software_version":
                                car.CurrentJSON.current_car_version = value.Replace("\"","") + " 0000";
                                break;
                            case "power":
                                if (!value.Contains("HVAC"))
                                {
                                    if (power != value)
                                        car.Log("Power: " + value);
                                    
                                    power = value;
                                }
                                break;
                            case "speed":
                                speed = double.Parse(value, CultureInfo.InvariantCulture);
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
            car.CurrentJSON.CreateCurrentJSON();
        }

        public string ExecuteShellCommand()
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "py",
                    Arguments = @"C:\dev\LucidAPI\python-lucidmotors\examples\vehicle_info.py",
                    WorkingDirectory = @"C:\dev\LucidAPI\python-lucidmotors\examples",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

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

        bool isCharging()
        {
            return charge_state == "CHARGE_STATE_CHARGING";
        }
    }
}
