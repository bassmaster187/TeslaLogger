using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace TeslaLogger
{
    public class TeslaAPIState
    {
        public enum Key
        {
            Type,
            Value,
            Timestamp,
            Source
        }

        private readonly Dictionary<string, Dictionary<Key, object>> storage = new Dictionary<string, Dictionary<Key, object>>();
        private HashSet<string> unknownKeys = new HashSet<string>();
        private Car car;

        public TeslaAPIState(Car car)
        {
            this.car = car;
        }

        private void AddValue(string _name, string _type, object _value, long _timestamp, string _source)
        {
            if (!storage.ContainsKey(_name))
            {
                storage.Add(_name, new Dictionary<Key, object>());
            }
            else
            {
                HandleStateChange(_name, storage[_name][Key.Value], _value, long.Parse(storage[_name][Key.Timestamp].ToString()), _timestamp);
            }
            storage[_name][Key.Type] = _type;
            storage[_name][Key.Value] = _value;
            storage[_name][Key.Timestamp] = _timestamp;
            storage[_name][Key.Source] = _source;
        }

        private void HandleStateChange(string name, object oldvalue, object newvalue, long oldTS, long newTS)
        {
            // TODO
            if (oldvalue != null && newvalue != null && !oldvalue.ToString().Equals(newvalue.ToString()))
            {
                switch (name)
                {
                    case "battery_level":
                        if (car.GetCurrentState() == Car.TeslaState.Online && car.GetWebHelper().GetLastShiftState().Equals("P"))
                        {
                            Tools.DebugLog($"#{car.CarInDB}: TeslaAPIHandleStateChange {name} {oldvalue} -> {newvalue}");
                            // write car data to DB eg to update Grafana Dashboard status
                            car.GetWebHelper().IsDriving(true);
                        }
                        break;
                }
            }
        }

        public bool GetState(string _name, out Dictionary<Key, object> _state)
        {
            if (storage.ContainsKey(_name))
            {
                _state = storage[_name];
                return true;
            }
            _state = new Dictionary<Key, object>();
            return false;
        }

        public bool GetBool(string _name, out bool _value)
        {
            if (storage.ContainsKey(_name))
            {
                if (storage[_name][Key.Type].Equals("bool"))
                {
                    return bool.TryParse(storage[_name][Key.Value].ToString(), out _value);
                }
            }
            _value = false;
            return false;
        }

        public bool GetInt(string _name, out int _value)
        {
            if (storage.ContainsKey(_name))
            {
                if (storage[_name][Key.Type].Equals("int"))
                {
                    return int.TryParse(storage[_name][Key.Value].ToString(), out _value);
                }
            }
            _value = int.MinValue;
            return false;
        }

        public bool GetDouble(string _name, out double _value)
        {
            if (storage.ContainsKey(_name))
            {
                if (storage[_name][Key.Type].Equals("double"))
                {
                    return double.TryParse(storage[_name][Key.Value].ToString(), out _value);
                }
            }
            _value = double.MinValue;
            return false;
        }

        public bool GetString(string _name, out string _value)
        {
            if (storage.ContainsKey(_name))
            {
                if (storage[_name][Key.Type].Equals("string"))
                {
                    _value = storage[_name][Key.Value].ToString();
                    return true;
                }
            }
            _value = string.Empty;
            return false;
        }

        public bool ParseAPI(string _JSON, string _source, int CarInAccount = 0)
        {
            switch (_source)
            {
                case "charge_state":
                    return ParseChargeState(_JSON);
                case "climate_state":
                    return ParseClimateState(_JSON);
                case "drive_state":
                    return ParseDriveState(_JSON);
                case "vehicle_config":
                    return ParseVehicleConfig(_JSON);
                case "vehicle_state":
                    return ParseVehicleState(_JSON);
                case "vehicles":
                    return ParseVehicles(_JSON, CarInAccount);
                default:
                    Logfile.Log($"ParseAPI: unknown source {_source}");
                    break;
            }
            return false;
        }

        private bool ParseVehicles(string _JSON, int CarInAccount)
        {
            try
            {
                object jsonResult = new JavaScriptSerializer().DeserializeObject(_JSON);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                object[] r2 = (object[])r1;
                object r3 = r2[CarInAccount];
                Dictionary<string, object> r4 = (Dictionary<string, object>)r3;
                /* {"response":
                 *      [
                 *         {
                 *          "id":24342078186123456,
                 *          "vehicle_id":1154123456,
                 *          "vin":"5YJSA7H17FF123456",
                 *          "display_name":"Tessi",
                 *          "option_codes":"AD15,MDL3,PBSB,RENA,BT37,ID3W,RF3G,S3PB,DRLH,DV2W,W39B,APF0,COUS,BC3B,CH07,PC30,FC3P,FG31,GLFR,HL31,HM31,IL31,LTPB,MR31,FM3B,RS3H,SA3P,STCP,SC04,SU3C,T3CA,TW00,TM00,UT3P,WR00,AU3P,APH3,AF00,ZCST,MI00,CDM0",
                 *          "color":null,
                 *          "access_type":"OWNER",
                 *          "tokens":
                 *             [
                 *              "d5e62570d352asdf",
                 *              "919f1b2a7f73asdf"
                 *             ],
                 *          "state":"asleep",
                 *          "in_service":false,
                 *          "id_s":"24342078186123456",
                 *          "calendar_enabled":true,
                 *          "api_version":10,
                 *          "backseat_token":null,
                 *          "backseat_token_updated_at":null,
                 *          "vehicle_config":null
                 *         }
                 *      ],
                 *      "count":1
                 * }
                 */
                foreach (string key in r4.Keys)
                {
                    switch (key)
                    {
                        case "timestamp":
                            break;
                        // bool
                        case "in_service":
                        case "calendar_enabled":
                            AddValue(key, "bool", r4[key], 0, "vehicles");
                            break;
                        // string
                        case "id":
                        case "vehicle_id":
                        case "vin":
                        case "display_name":
                        case "option_codes":
                        case "color":
                        case "access_type":
                        case "state":
                        case "id_s":
                        case "backseat_token":
                        case "backseat_token_updated_at":
                        case "vehicle_config":
                            AddValue(key, "string", r4[key], 0, "vehicles");
                            break;
                        // int
                        case "api_version":
                            AddValue(key, "int", r4[key], 0, "vehicles");
                            break;
                        // TODO
                        case "tokens":
                            break;
                        default:
                            if (!unknownKeys.Contains(key))
                            {
                                Logfile.Log($"ParseVehicles: unknown key {key}");
                                unknownKeys.Add(key);
                            }
                            break;
                    }
                }
                return true;
            }
            catch (Exception) { }
            return false;
        }

        private bool ParseChargeState(string _JSON)
        {
            try
            {
                object jsonResult = new JavaScriptSerializer().DeserializeObject(_JSON);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;
                /*
                 * {"response":
                 *     {
                 *      "battery_heater_on":false,
                 *      "battery_level":51,
                 *      "battery_range":148.56,
                 *      "charge_current_request":16,
                 *      "charge_current_request_max":16,
                 *      "charge_enable_request":true,
                 *      "charge_energy_added":0.0,
                 *      "charge_limit_soc":85,
                 *      "charge_limit_soc_max":100,
                 *      "charge_limit_soc_min":50,
                 *      "charge_limit_soc_std":90,
                 *      "charge_miles_added_ideal":0.0,
                 *      "charge_miles_added_rated":0.0,
                 *      "charge_port_cold_weather_mode":null,
                 *      "charge_port_door_open":false,
                 *      "charge_port_latch":"Blocking",
                 *      "charge_rate":0.0,
                 *      "charge_to_max_range":false,
                 *      "charger_actual_current":0,
                 *      "charger_phases":null,
                 *      "charger_pilot_current":16,
                 *      "charger_power":0,
                 *      "charger_voltage":0,
                 *      "charging_state":"Disconnected",
                 *      "conn_charge_cable":"<invalid>",
                 *      "est_battery_range":142.78,
                 *      "fast_charger_brand":"<invalid>",
                 *      "fast_charger_present":false,
                 *      "fast_charger_type":"<invalid>",
                 *      "ideal_battery_range":118.85,
                 *      "managed_charging_active":false,
                 *      "managed_charging_start_time":null,
                 *      "managed_charging_user_canceled":false,
                 *      "max_range_charge_counter":1,
                 *      "minutes_to_full_charge":0,
                 *      "not_enough_power_to_heat":false,
                 *      "scheduled_charging_pending":false,
                 *      "scheduled_charging_start_time":null,
                 *      "time_to_full_charge":0.0,
                 *      "timestamp":1598862369327,
                 *      "trip_charging":false,
                 *      "usable_battery_level":51,
                 *      "user_charge_enable_request":null
                 *     }
                 * }
                 */
                if (long.TryParse(r2["timestamp"].ToString(), out long timestamp))
                {
                    foreach (string key in r2.Keys)
                    {
                        switch (key)
                        {
                            case "timestamp":
                                break;
                            // bool
                            case "battery_heater_on":
                            case "charge_enable_request":
                            case "charge_port_door_open":
                            case "charge_to_max_range":
                            case "fast_charger_present":
                            case "managed_charging_active":
                            case "managed_charging_user_canceled":
                            case "not_enough_power_to_heat":
                            case "scheduled_charging_pending":
                            case "trip_charging":
                                AddValue(key, "bool", r2[key], timestamp, "charge_state");
                                break;
                            // string
                            case "charge_port_cold_weather_mode":
                            case "charge_port_latch":
                            case "charger_phases":
                            case "charging_state":
                            case "conn_charge_cable":
                            case "fast_charger_brand":
                            case "fast_charger_type":
                            case "managed_charging_start_time":
                            case "scheduled_charging_start_time":
                            case "user_charge_enable_request":
                                AddValue(key, "string", r2[key], timestamp, "charge_state");
                                break;
                            // int
                            case "battery_level":
                            case "charge_current_request":
                            case "charge_current_request_max":
                            case "charge_limit_soc":
                            case "charge_limit_soc_max":
                            case "charge_limit_soc_min":
                            case "charge_limit_soc_std":
                            case "charger_actual_current":
                            case "charger_pilot_current":
                            case "charger_power":
                            case "charger_voltage":
                            case "max_range_charge_counter":
                            case "minutes_to_full_charge":
                            case "usable_battery_level":
                                AddValue(key, "int", r2[key], timestamp, "charge_state");
                                break;
                            // double
                            case "battery_range":
                            case "charge_energy_added":
                            case "charge_miles_added_ideal":
                            case "charge_miles_added_rated":
                            case "charge_rate":
                            case "est_battery_range":
                            case "ideal_battery_range":
                            case "time_to_full_charge":
                                AddValue(key, "double", r2[key], timestamp, "charge_state");
                                break;
                            default:
                                if (!unknownKeys.Contains(key))
                                {
                                    Logfile.Log($"ParseChargeState: unknown key {key}");
                                    unknownKeys.Add(key);
                                }
                                break;
                        }
                    }
                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }

        private bool ParseDriveState(string _JSON)
        {
            try
            {
                object jsonResult = new JavaScriptSerializer().DeserializeObject(_JSON);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;
                /*
                 * {"response":
                 *     {
                 *      "gps_as_of":1599039106,
                 *      "heading":253,
                 *      "latitude":123.577843,
                 *      "longitude":123.314109,
                 *      "native_latitude":123.577843,
                 *      "native_location_supported":1,
                 *      "native_longitude":123.314109,
                 *      "native_type":"wgs",
                 *      "power":0,
                 *      "shift_state":null,
                 *      "speed":null,
                 *      "timestamp":1599039108406
                 *     }
                 * }
                 */
                if (long.TryParse(r2["timestamp"].ToString(), out long timestamp))
                {
                    foreach (string key in r2.Keys)
                    {
                        switch (key)
                        {
                            case "timestamp":
                                break;
                            // string
                            case "native_type":
                            case "shift_state":
                                AddValue(key, "string", r2[key], timestamp, "drive_state");
                                break;
                            // int
                            case "gps_as_of":
                            case "heading":
                            case "native_location_supported":
                            case "power":
                            case "speed":
                                AddValue(key, "int", r2[key], timestamp, "drive_state");
                                break;
                            // double
                            case "latitude":
                            case "longitude":
                            case "native_latitude":
                            case "native_longitude":
                                AddValue(key, "double", r2[key], timestamp, "drive_state");
                                break;
                            default:
                                if (!unknownKeys.Contains(key))
                                {
                                    Logfile.Log($"ParseDriveState: unknown key {key}");
                                    unknownKeys.Add(key);
                                }
                                break;
                        }
                    }
                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }

        private bool ParseVehicleConfig(string _JSON)
        {
            try
            {
                object jsonResult = new JavaScriptSerializer().DeserializeObject(_JSON);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;
                /*
                 * {"response":
                 *     {
                 *      "can_accept_navigation_requests":true,
                 *      "can_actuate_trunks":true,
                 *      "car_special_type":"base",
                 *      "car_type":"models",
                 *      "charge_port_type":"EU",
                 *      "ece_restrictions":true,
                 *      "eu_vehicle":true,
                 *      "exterior_color":"Red",
                 *      "has_air_suspension":false,
                 *      "has_ludicrous_mode":false,
                 *      "motorized_charge_port":false,
                 *      "plg":true,
                 *      "rear_seat_heaters":1,
                 *      "rear_seat_type":1,
                 *      "rhd":false,
                 *      "roof_color":"None",
                 *      "seat_type":1,
                 *      "spoiler_type":"None",
                 *      "sun_roof_installed":2,
                 *      "third_row_seats":"None",
                 *      "timestamp":1598862351936,
                 *      "trim_badging":"85",
                 *      "use_range_badging":false,
                 *      "wheel_type":"Base19"
                 *     }
                 * }
                 */
                if (long.TryParse(r2["timestamp"].ToString(), out long timestamp))
                {
                    foreach (string key in r2.Keys)
                    {
                        switch (key)
                        {
                            case "timestamp":
                                break;
                            // bool
                            case "can_accept_navigation_requests":
                            case "can_actuate_trunks":
                            case "ece_restrictions":
                            case "eu_vehicle":
                            case "has_air_suspension":
                            case "has_ludicrous_mode":
                            case "motorized_charge_port":
                            case "plg":
                            case "rhd":
                            case "use_range_badging":
                                AddValue(key, "bool", r2[key], timestamp, "vehicle_config");
                                break;
                            // string
                            case "car_special_type":
                            case "car_type":
                            case "charge_port_type":
                            case "exterior_color":
                            case "roof_color":
                            case "spoiler_type":
                            case "third_row_seats":
                            case "trim_badging":
                            case "wheel_type":
                            case "perf_config":
                                AddValue(key, "string", r2[key], timestamp, "vehicle_config");
                                break;
                            // int
                            case "rear_seat_heaters":
                            case "rear_seat_type":
                            case "seat_type":
                            case "sun_roof_installed":
                                AddValue(key, "int", r2[key], timestamp, "vehicle_config");
                                break;
                            default:
                                if (!unknownKeys.Contains(key))
                                {
                                    Logfile.Log($"ParseVehicleConfig: unknown key {key}");
                                    unknownKeys.Add(key);
                                }
                                break;
                        }
                    }
                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }

        private bool ParseVehicleState(string _JSON)
        {
            try
            {
                object jsonResult = new JavaScriptSerializer().DeserializeObject(_JSON);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;
                /*
                 * {"response":
                 *     {
                 *      "api_version":10,
                 *      "autopark_state_v2":"ready",
                 *      "autopark_style":"dead_man",
                 *      "calendar_supported":true,
                 *      "car_version":"2020.32.3 b9bd4364fd17",
                 *      "center_display_state":0,
                 *      "df":0,
                 *      "dr":0,
                 *      "ft":0,
                 *      "homelink_device_count":0,
                 *      "homelink_nearby":false,
                 *      "is_user_present":false,
                 *      "last_autopark_error":"no_error",
                 *      "locked":true,
                 *      "media_state":
                 *          {
                 *           "remote_control_enabled":true
                 *          },
                 *      "notifications_supported":true,
                 *      "odometer":47743.589221,
                 *      "parsed_calendar_supported":true,
                 *      "pf":0,
                 *      "pr":0,
                 *      "remote_start":false,
                 *      "remote_start_enabled":false,
                 *      "remote_start_supported":true,
                 *      "rt":0,
                 *      "smart_summon_available":false,
                 *      "software_update":
                 *         {
                 *          "download_perc":0,
                 *          "expected_duration_sec":2700,
                 *          "install_perc":1,
                 *          "status":"",
                 *          "version":""
                 *         },
                 *      "speed_limit_mode":
                 *         {
                 *          "active":false,
                 *          "current_limit_mph":85.0,
                 *          "max_limit_mph":90,
                 *          "min_limit_mph":50,
                 *          "pin_code_set":false
                 *         },
                 *      "summon_standby_mode_enabled":false,
                 *      "sun_roof_percent_open":0,
                 *      "sun_roof_state":"closed",
                 *      "timestamp":1598862368166,
                 *      "valet_mode":false,
                 *      "valet_pin_needed":true,
                 *      "vehicle_name":"Tessi"
                 *     }
                 * }
                 */
                if (long.TryParse(r2["timestamp"].ToString(), out long timestamp))
                {
                    foreach (string key in r2.Keys)
                    {
                        switch (key)
                        {
                            case "timestamp":
                                break;
                            // bool
                            case "calendar_supported":
                            case "homelink_nearby":
                            case "is_user_present":
                            case "locked":
                            case "notifications_supported":
                            case "parsed_calendar_supported":
                            case "remote_start":
                            case "remote_start_enabled":
                            case "remote_start_supported":
                            case "sentry_mode":
                            case "sentry_mode_available":
                            case "smart_summon_available":
                            case "summon_standby_mode_enabled":
                            case "valet_mode":
                            case "valet_pin_needed":
                                AddValue(key, "bool", r2[key], timestamp, "vehicle_state");
                                break;
                            // string
                            case "autopark_state_v2":
                            case "autopark_style":
                            case "car_version":
                            case "last_autopark_error":
                            case "sun_roof_state":
                            case "vehicle_name":
                                AddValue(key, "string", r2[key], timestamp, "vehicle_state");
                                break;
                            // int
                            case "api_version":
                            case "center_display_state":
                            case "df":
                            case "dr":
                            case "ft":
                            case "homelink_device_count":
                            case "pf":
                            case "pr":
                            case "rt":
                            case "sun_roof_percent_open":
                                AddValue(key, "int", r2[key], timestamp, "vehicle_state");
                                break;
                            // double
                            case "odometer":
                                AddValue(key, "double", r2[key], timestamp, "vehicle_state");
                                break;
                            // TODO
                            case "media_state":
                            case "software_update":
                            case "speed_limit_mode":
                                break;
                            default:
                                if (!unknownKeys.Contains(key))
                                {
                                    Logfile.Log($"ParseVehicleState: unknown key {key}");
                                    unknownKeys.Add(key);
                                }
                                break;
                        }
                    }
                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }


        private bool ParseClimateState(string _JSON)
        {
            try
            {
                object jsonResult = new JavaScriptSerializer().DeserializeObject(_JSON);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;
                /*
                 * {"response":
                 *     {
                 *      "battery_heater":false,
                 *      "battery_heater_no_power":false,
                 *      "climate_keeper_mode":"off",
                 *      "defrost_mode":0,
                 *      "driver_temp_setting":21.0,
                 *      "fan_status":0,
                 *      "inside_temp":26.5,
                 *      "is_auto_conditioning_on":false,
                 *      "is_climate_on":false,
                 *      "is_front_defroster_on":false,
                 *      "is_preconditioning":false,
                 *      "is_rear_defroster_on":false,
                 *      "left_temp_direction":-267,
                 *      "max_avail_temp":28.0,
                 *      "min_avail_temp":15.0,
                 *      "outside_temp":15.5,
                 *      "passenger_temp_setting":21.0,
                 *      "remote_heater_control_enabled":false,
                 *      "right_temp_direction":-267,
                 *      "seat_heater_left":0,
                 *      "seat_heater_rear_center":0,
                 *      "seat_heater_rear_left":0,
                 *      "seat_heater_rear_right":0,
                 *      "seat_heater_right":0,
                 *      "side_mirror_heaters":false,
                 *      "steering_wheel_heater":false,
                 *      "timestamp":1598862369248,
                 *      "wiper_blade_heater":false
                 *     }
                 * }
                 */
                if (long.TryParse(r2["timestamp"].ToString(), out long timestamp)) {
                    foreach (string key in r2.Keys)
                    {
                        switch (key)
                        {
                            case "timestamp":
                                break;
                            // bool
                            case "battery_heater":
                            case "battery_heater_no_power":
                            case "is_auto_conditioning_on":
                            case "is_climate_on":
                            case "is_front_defroster_on":
                            case "is_preconditioning":
                            case "is_rear_defroster_on":
                            case "remote_heater_control_enabled":
                            case "side_mirror_heaters":
                            case "steering_wheel_heater":
                            case "wiper_blade_heater":
                            case "smart_preconditioning":
                                AddValue(key, "bool", r2[key], timestamp, "climate_state");
                                break;
                            // string
                            case "climate_keeper_mode":
                                AddValue(key, "string", r2[key], timestamp, "climate_state");
                                break;
                            // int
                            case "defrost_mode":
                            case "fan_status":
                            case "left_temp_direction":
                            case "right_temp_direction":
                            case "seat_heater_left":
                            case "seat_heater_rear_center":
                            case "seat_heater_rear_left":
                            case "seat_heater_rear_right":
                            case "seat_heater_right":
                                AddValue(key, "int", r2[key], timestamp, "climate_state");
                                break;
                            // double
                            case "driver_temp_setting":
                            case "inside_temp":
                            case "max_avail_temp":
                            case "min_avail_temp":
                            case "outside_temp":
                            case "passenger_temp_setting":
                                AddValue(key, "double", r2[key], timestamp, "climate_state");
                                break;
                            default:
                                if (!unknownKeys.Contains(key))
                                {
                                    Logfile.Log($"ParseClimateState: unknown key {key}");
                                    unknownKeys.Add(key);
                                }
                                break;
                        }
                    }
                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }

        public override string ToString()
        {
            string str = string.Empty;
            foreach (string key in storage.Keys)
            {
                str += string.Concat($"{key} => v:{storage[key][Key.Value]} t:{storage[key][Key.Type]} s:{storage[key][Key.Source]} ts:{storage[key][Key.Timestamp]}", Environment.NewLine);
            }
            return str;
        }
    }
}