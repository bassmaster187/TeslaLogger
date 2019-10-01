namespace TeslaLogger
{
    using System;
    using System.Collections.Generic;

    class CurrentJSON
    {
        public bool current_charging = false;
        public bool current_driving = false;
        public bool current_online = false;
        public bool current_sleeping = false;

        public int current_speed = 0;
        public int current_power = 0;
        public double current_odometer = 0;
        public double current_ideal_battery_range_km = 0;
        public double current_outside_temp = 0;
        public int current_battery_level = 0;

        public int current_charger_voltage = 0;
        public int current_charger_phases = 0;
        public int current_charger_actual_current = 0;
        public double current_charge_energy_added = 0;
        public int current_charger_power = 0;

        public string current_car_version = "";

        public DateTime current_trip_start = DateTime.MinValue;
        public DateTime current_trip_end = DateTime.MinValue;
        public double current_trip_km_start = 0;
        public double current_trip_km_end = 0;
        public double current_trip_max_speed = 0;
        public double current_trip_max_power = 0;
        public double current_trip_start_range = 0;
        public double current_trip_end_range = 0;
        public double Wh_TR = 0.19;

        public int current_trip_duration_sec = 0;

        public string current_json = "";

        public void CreateCurrentJSON()
        {
            try
            {
                int duration = 0;
                double distance = 0;
                double trip_kwh = 0.0;
                double trip_avg_wh = 0.0;

                try
                {
                    if (current_trip_end == DateTime.MinValue)
                    {
                        duration = (int)((TimeSpan)(DateTime.Now - current_trip_start)).TotalSeconds;
                        distance = current_odometer - current_trip_km_start;
                        trip_kwh = (current_trip_start_range - current_ideal_battery_range_km) * Wh_TR;

                        if (distance > 0)
                            trip_avg_wh = trip_kwh / distance * 1000;
                    }
                    else
                    {
                        duration = (int)((TimeSpan)(current_trip_end - current_trip_start)).TotalSeconds;
                        distance = current_trip_km_end - current_trip_km_start;
                        trip_kwh = (current_trip_start_range - current_trip_end_range) * Wh_TR;

                        if (distance > 0)
                            trip_avg_wh = trip_kwh / distance * 1000;
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                    duration = 0;
                }
                if (duration < 0)
                    duration = 0;
                

                var values = new Dictionary<string, object>
                {
                   { "charging", current_charging},
                   { "driving", current_driving },
                   { "online", current_online },
                   { "sleeping", current_sleeping },
                   { "speed", current_speed},
                   { "power", current_power },
                   { "odometer", current_odometer },
                   { "ideal_battery_range_km", current_ideal_battery_range_km},
                   { "outside_temp", current_outside_temp},
                   { "battery_level", current_battery_level},
                   { "charger_voltage", current_charger_voltage},
                   { "charger_phases", current_charger_phases},
                   { "charger_actual_current", current_charger_actual_current},
                   { "charge_energy_added", current_charge_energy_added},
                   { "charger_power", current_charger_power},
                   { "car_version", current_car_version },
                   { "trip_start", current_trip_start.ToString("t",Tools.ciDeDE) },
                   { "trip_max_speed", current_trip_max_speed },
                   { "trip_max_power", current_trip_max_power },
                   { "trip_duration_sec", duration },
                   { "trip_kwh", trip_kwh },
                   { "trip_avg_kwh", trip_avg_wh },
                   { "trip_distance", distance }

                };

                current_json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(values);

                FileManager.WriteCurrentJsonFile(current_json);
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
                current_json = "";
            }
        }
    }
}
