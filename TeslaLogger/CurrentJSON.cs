﻿using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Sichtbare Instanzfelder nicht deklarieren", Justification = "<Pending>")]
    public class CurrentJSON
    {
        public static readonly ConcurrentDictionary<int, string> jsonStringHolder = new ConcurrentDictionary<int, string>();
        public bool current_charging; // defaults to false
        public bool current_driving; // defaults to false
        public bool current_online; // defaults to false
        public bool current_sleeping; // defaults to false
        public bool current_falling_asleep; // defaults to false
        public bool current_plugged_in; // defaults to false
        private long timestamp; // defaults to 0

        public int current_speed; // defaults to 0
        public int current_power; // defaults to 0
        public double current_odometer; // defaults to 0
        public double current_ideal_battery_range_km; // defaults to 0
        public double current_battery_range_km; // defaults to 0
        public double current_outside_temperature; // defaults to 0
        public int current_battery_level; // defaults to 0

        public int current_charger_voltage; // defaults to 0
        public int current_charger_phases; // defaults to 0
        public int current_charger_phases_calc; // defaults to 0
        public int current_charger_actual_current; // defaults to 0
        public int current_charger_actual_current_calc; // defaults to 0
        public int current_charge_current_request; // defaults to 0
        public double current_charge_energy_added; // defaults to 0
        public int current_charger_power; // defaults to 0
        public int current_charger_power_calc_w; // defaults to 0
        public double current_charge_rate_km; // defaults to 0
        public double current_time_to_full_charge; // defaults to 0
        public bool current_charge_port_door_open; // defaults to false
        public string current_charger_brand = "";
        public bool current_charger_present; // defaults to false

        public string current_car_version = "";

        public DateTime current_trip_start = DateTime.MinValue;
        public DateTime current_trip_end = DateTime.MinValue;
        public double current_trip_km_start; // defaults to 0;
        public double current_trip_km_end; // defaults to 0;
        public double current_trip_max_speed; // defaults to 0;
        public double current_trip_max_power; // defaults to 0;
        public double current_trip_start_range; // defaults to 0;
        public double current_trip_end_range; // defaults to 0;
        public double Wh_TR = 0.19;

        public int current_trip_duration_sec; // defaults to 0;

        private double latitude; // defaults to 0;
        private double longitude; // defaults to 0;
        public int charge_limit_soc; // defaults to 0;
        public int heading; // defaults to 0;
        public double current_inside_temperature; // defaults to 0;
        public bool current_battery_heater; // defaults to false;
        public bool current_is_sentry_mode; // defaults to false;
        public bool current_is_preconditioning; // defaults to false;

        public string current_country_code = "";
        public string current_state = "";

        public DateTime lastScanMyTeslaReceived = DateTime.MinValue;
        public double? SMTCellTempAvg; // defaults to null;
        public double? SMTCellMinV; // defaults to null;
        public double? SMTCellAvgV; // defaults to null;
        public double? SMTCellMaxV; // defaults to null;
        public double? SMTCellImbalance; // defaults to null;
        public double? SMTBMSmaxCharge; // defaults to null;
        public double? SMTBMSmaxDischarge; // defaults to null;
        public double? SMTACChargeTotal; // defaults to null;
        public double? SMTDCChargeTotal; // defaults to null;
        public double? SMTNominalFullPack; // defaults to null;

        public double? SMTSpeed; // defaults to null;
        public double? SMTBatteryPower; // defaults to null;

        public string active_route_destination; // defaults to null;
        public long? active_route_energy_at_arrival; // defaults to null;
        public long? active_route_km_to_arrival; // defaults to null;
        public double? active_route_minutes_to_arrival; // defaults to null;
        public double? active_route_traffic_minutes_delay; // defaults to null;
        public double? active_route_latitude; // defaults to null;
        public double? active_route_longitude; // defaults to null;

        public string current_json = "";
        private DateTime lastJSONwrite = DateTime.MinValue;
        Car car;

        internal CurrentJSON(Car car)
        {
            this.car = car;
        }

        public void CheckCreateCurrentJSON()
        {
            TimeSpan ts = DateTime.UtcNow - lastJSONwrite;
            if (ts.TotalMinutes > 5)
            {
                CreateCurrentJSON();
            }
        }

        public void CreateCurrentJSON()
        {
            try
            {
                lastJSONwrite = DateTime.UtcNow;

                int duration = 0;
                double distance = 0;
                double trip_kwh = 0.0;
                double trip_avg_wh = 0.0;

                try
                {
                    if (current_trip_end == DateTime.MinValue)
                    {
                        duration = (int)(DateTime.Now - current_trip_start).TotalSeconds;
                        distance = current_odometer - current_trip_km_start;
                        trip_kwh = (current_trip_start_range - current_ideal_battery_range_km) * Wh_TR;

                        if (distance > 0)
                        {
                            trip_avg_wh = trip_kwh / distance * 1000;
                        }
                    }
                    else
                    {
                        duration = (int)(current_trip_end - current_trip_start).TotalSeconds;
                        distance = current_trip_km_end - current_trip_km_start;
                        trip_kwh = (current_trip_start_range - current_trip_end_range) * Wh_TR;

                        if (distance > 0)
                        {
                            trip_avg_wh = trip_kwh / distance * 1000;
                        }
                    }
                }
                catch (Exception ex)
                {
                    car.CreateExceptionlessClient(ex).Submit();

                    Logfile.Log(ex.ToString());
                    duration = 0;
                }
                if (duration < 0)
                {
                    duration = 0;
                }

                var apistate = car.GetTeslaAPIState();

                apistate.GetBool("charge_port_door_open", out current_charge_port_door_open);
                apistate.GetString("software_update.status", out string software_update_status);
                apistate.GetString("software_update.version", out string software_update_version);

                apistate.GetInt("fd_window", out int fd_window);
                apistate.GetInt("fp_window", out int fp_window);
                apistate.GetInt("rd_window", out int rd_window);
                apistate.GetInt("rp_window", out int rp_window);

                apistate.GetInt("pf", out int pf);
                apistate.GetInt("pr", out int pr);
                apistate.GetInt("df", out int df);
                apistate.GetInt("dr", out int dr);

                apistate.GetInt("ft", out int frunk);
                apistate.GetInt("rt", out int trunk);

                bool locked = true;
                if (apistate.HasValue("locked")) // after restart the locked state is false, that tends to confuse
                    apistate.GetBool("locked", out locked);

                int open_windows = fd_window + fp_window + rd_window + rp_window;
                int open_doors = 
                    pf > 0 ? 1 :0 
                    + pr > 0 ? 1 : 0
                    + df > 0 ? 1 : 0
                    + dr > 0 ? 1 : 0;

                Dictionary<string, object> values = new Dictionary<string, object>
                {
                   { "charging", current_charging},
                   { "driving", current_driving },
                   { "online", current_online },
                   { "sleeping", current_sleeping },
                   { "falling_asleep", current_falling_asleep },
                   { "plugged_in", current_plugged_in },
                   { "speed", current_speed},
                   { "power", current_power },
                   { "odometer", current_odometer },
                   { "ideal_battery_range_km", current_ideal_battery_range_km},
                   { "battery_range_km", current_battery_range_km},
                   { "outside_temp", current_outside_temperature},
                   { "battery_level", current_battery_level},
                   { "charger_voltage", current_charger_voltage},
                   { "charger_phases", current_charger_phases},
                   { "charger_phases_calc", current_charger_phases_calc},
                   { "charger_actual_current", current_charger_actual_current},
                   { "charger_actual_current_calc", current_charger_actual_current_calc},
                   { "charge_current_request", current_charge_current_request},
                   { "charge_energy_added", current_charge_energy_added},
                   { "charger_power", current_charger_power},
                   { "charger_power_calc_w", current_charger_power_calc_w},
                   { "charge_rate_km", current_charge_rate_km},
                   { "charge_port_door_open", current_charge_port_door_open },
                   { "time_to_full_charge", current_time_to_full_charge},
                   { "fast_charger_brand", current_charger_brand},
                   { "fast_charger_present", current_charger_present},
                   { "car_version", current_car_version },
                   { "trip_start", current_trip_start.ToString("t",Tools.ciDeDE) },
                   { "trip_start_dt", current_trip_start.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", Tools.ciEnUS) },
                   { "trip_max_speed", current_trip_max_speed },
                   { "trip_max_power", current_trip_max_power },
                   { "trip_duration_sec", duration },
                   { "trip_kwh", trip_kwh },
                   { "trip_avg_kwh", trip_avg_wh },
                   { "trip_distance", distance },
                   { "ts", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", Tools.ciEnUS)},
                   { "latitude", latitude },
                   { "longitude", longitude },
                   { "charge_limit_soc", charge_limit_soc},
                   { "inside_temperature", current_inside_temperature },
                   { "battery_heater", current_battery_heater },
                   { "is_preconditioning", current_is_preconditioning },
                   { "sentry_mode", current_is_sentry_mode },
                   { "country_code", current_country_code },
                   { "state", current_state },
                   { "display_name", car.DisplayName},
                   { "heading", heading},
                   { "software_update_status", software_update_status },
                   { "software_update_version" , software_update_version },
                   { "active_route_destination" , active_route_destination },
                   { "active_route_energy_at_arrival" , active_route_energy_at_arrival },
                   { "active_route_minutes_to_arrival" , active_route_minutes_to_arrival },
                   { "active_route_traffic_minutes_delay" , active_route_traffic_minutes_delay },
                   { "active_route_latitude" , active_route_latitude },
                   { "active_route_longitude" , active_route_longitude },
                   { "open_windows" , open_windows},
                   { "open_doors" , open_doors},
                   { "frunk" , frunk},
                   { "trunk" , trunk},
                   { "locked" , locked}
                };

                TimeSpan ts = DateTime.Now - lastScanMyTeslaReceived;
                if (ts.TotalMinutes < 5)
                {
                    values.Add("SMTCellTempAvg", SMTCellTempAvg);
                    values.Add("SMTCellMinV", SMTCellMinV);
                    values.Add("SMTCellAvgV", SMTCellAvgV);
                    values.Add("SMTCellMaxV", SMTCellMaxV);
                    values.Add("SMTCellImbalance", SMTCellImbalance);
                    values.Add("SMTBMSmaxCharge", SMTBMSmaxCharge);
                    values.Add("SMTBMSmaxDischarge", SMTBMSmaxDischarge);
                    values.Add("SMTACChargeTotal", SMTACChargeTotal);
                    values.Add("SMTDCChargeTotal", SMTDCChargeTotal);
                    values.Add("SMTNominalFullPack", SMTNominalFullPack);
                }

                Address addr = Geofence.GetInstance().GetPOI(latitude, longitude, false);
                if (addr != null && addr.rawName != null)
                {
                    values.Add("TLGeofence", addr.rawName);
                    values.Add("TLGeofenceIsHome", addr.IsHome);
                    values.Add("TLGeofenceIsCharger", addr.IsCharger);
                    values.Add("TLGeofenceIsWork", addr.IsWork);
                }
                else
                {
                    values.Add("TLGeofence", "-");
                    values.Add("TLGeofenceIsHome", false);
                    values.Add("TLGeofenceIsCharger", false);
                    values.Add("TLGeofenceIsWork", false);
                }

                current_json = JsonConvert.SerializeObject(values);

                jsonStringHolder[car.CarInDB] = current_json;

                // FileManager.WriteCurrentJsonFile(car.CarInDB, current_json);
            }
            catch (Exception ex)
            {
                car.CreateExceptionlessClient(ex).Submit();

                Logfile.Log(ex.ToString());
                current_json = "";
            }
        }

        public void SetPosition(double lat, double lng, long ts)
        {
            if (ts > timestamp)
            {
                latitude = lat;
                longitude = lng;
                timestamp = ts;
            }
        }

        public double GetLatitude()
        {
            return latitude;
        }

        public double GetLongitude()
        {
            return longitude;
        }

        public void SetLatitude(double lat)
        {
            latitude = lat;
        }

        public void SetLongitude(double lng)
        {
            longitude = lng;
        }

        internal void ToKVS()
        {
            KVS.InsertOrUpdate($"currentJSON_{car.CarInDB}", jsonStringHolder[car.CarInDB]);
        }

        internal void FromKVS()
        {
            if (KVS.Get($"currentJSON_{car.CarInDB}", out string cJSON) == KVS.SUCCESS)
            {
                jsonStringHolder[car.CarInDB] = cJSON;
            }
        }
    }
}
