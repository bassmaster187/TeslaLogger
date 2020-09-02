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

        public bool GetDecimal(string _name, out decimal _value)
        {
            if (storage.ContainsKey(_name))
            {
                if (storage[_name][Key.Type].Equals("decimal"))
                {
                    return decimal.TryParse(storage[_name][Key.Value].ToString(), out _value);
                }
            }
            _value = decimal.MinValue;
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
            }
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
                            // decimal
                            case "left_temp_direction":
                            case "right_temp_direction":
                            case "seat_heater_left":
                            case "seat_heater_rear_center":
                            case "seat_heater_rear_left":
                            case "seat_heater_rear_right":
                            case "seat_heater_right":
                                AddValue(key, "decimal", r2[key], timestamp, "climate_state");
                                break;
                        }
                    }
                }
            }
            catch (Exception) { }
            return false;
        }
    }
}