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

        private void AddValue(string _name, string _type, object _value, long _timestamp, string _source)
        {
            if (!storage.ContainsKey(_name))
            {
                storage.Add(_name, new Dictionary<Key, object>());
            }
            storage[_name][Key.Type] = _type;
            storage[_name][Key.Value] = _value;
            storage[_name][Key.Timestamp] = _timestamp;
            storage[_name][Key.Source] = _source;
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

        public bool ParseAPI(string _source, string _JSON)
        {
            switch (_source)
            {
                case "climate_state":
                    return ParseClimateState(_JSON);
                case "vehicle_state":
                    return ParseVehicleState(_JSON);
                case "vehicle_config":
                    return ParseVehicleConfig(_JSON);
            }
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
                                AddValue(key, "bool", r2[key], timestamp, "vehicle_state");
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
                                AddValue(key, "string", r2[key], timestamp, "climate_state");
                                break;
                            // int
                            case "rear_seat_heaters":
                            case "rear_seat_type":
                            case "seat_type":
                            case "sun_roof_installed":
                                AddValue(key, "int", r2[key], timestamp, "climate_state");
                                break;
                            default:
                                Logfile.Log($"ParseVehicleConfig: unknown key {key}");
                                break;
                        }
                    }
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
                                AddValue(key, "string", r2[key], timestamp, "climate_state");
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
                                AddValue(key, "int", r2[key], timestamp, "climate_state");
                                break;
                            // double
                            case "odometer":
                                AddValue(key, "double", r2[key], timestamp, "climate_state");
                                break;
                            // TODO
                            case "media_state":
                            case "software_update":
                            case "speed_limit_mode":
                                break;
                            default:
                                Logfile.Log($"ParseVehicleConfig: unknown key {key}");
                                break;
                        }
                    }
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
                                Logfile.Log($"ParseVehicleConfig: unknown key {key}");
                                break;
                        }
                    }
                    return true;
                }
            }
            catch (Exception) { }
            return false;
        }
    }
}