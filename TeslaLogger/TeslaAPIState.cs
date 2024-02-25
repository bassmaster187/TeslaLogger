﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Exceptionless;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "brauchen wir nicht")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    public class TeslaAPIState
    {
        public enum Key
        {
            Type,
            Value,
            ValueLastUpdate,
            Timestamp,
            Source
        }

        private readonly SortedDictionary<string, Dictionary<Key, object>> storage = new SortedDictionary<string, Dictionary<Key, object>>();
        private readonly HashSet<string> unknownKeys = new HashSet<string>();
        private readonly Car car;
        private bool dumpJSON;
        private readonly object TeslaAPIStateLock = new object();

        internal bool DumpJSON {
            get => dumpJSON;
            set {
                if (value)
                {
                    try
                    {
                        DumpJSONSessionDir = Path.Combine(Logfile.GetExecutingPath(), $"JSON/{DateTime.UtcNow:yyyyMMddHHmmssfff}");
                        if (!Directory.Exists(DumpJSONSessionDir))
                        {
                            Directory.CreateDirectory(DumpJSONSessionDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        car.CreateExceptionlessClient(ex).Submit();
                        Tools.DebugLog("DumpJSON", ex);
                    }
                }
                car.Log($"DumpJSON {value}");
                dumpJSON = value;
            }
        }
        private string DumpJSONSessionDir = string.Empty;

        internal TeslaAPIState(Car car)
        {
            this.car = car;
        }

        private void AddValue(string name, string type, object value, long timestamp, string source)
        {
            lock (TeslaAPIStateLock)
            {
                if (!storage.TryGetValue(name, out Dictionary<Key, object> _))
                {
                    storage.Add(name, new Dictionary<Key, object>() {
                    { Key.Type , "undef" },
                    { Key.Value , "undef" },
                    { Key.ValueLastUpdate , timestamp },
                    { Key.Timestamp , timestamp },
                    { Key.Source , "undef" }
                });
                }
                else
                {
                    try
                    {
                        if (storage.TryGetValue(name, out Dictionary<Key, object> dict)
                            && dict.TryGetValue(Key.Value, out object oldvalue)
                            && dict.TryGetValue(Key.Timestamp, out object oldTS) && oldTS != null)
                        {
                            if (
                                // olvalue != null and value changed
                                !(oldvalue == null || value == null || oldvalue.ToString() == value.ToString())
                                // oldvalue was null and newvalue is not null
                                || (oldvalue == null && value != null)
                                )
                            {
                                storage[name][Key.ValueLastUpdate] = timestamp;
                                HandleStateChange(name, oldvalue, value, long.Parse(oldTS.ToString(), Tools.ciEnUS), timestamp);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        car.CreateExceptionlessClient(ex).Submit();
                        Tools.DebugLog("Exception", ex);
                    }
                }
                storage[name][Key.Type] = type;
                if (type.Equals("string", StringComparison.Ordinal) && (value == null || (value != null && string.IsNullOrEmpty(value.ToString()))))
                {
                    storage[name][Key.Value] = string.Empty;
                }
                else
                {
                    storage[name][Key.Value] = value;
                }
                storage[name][Key.Timestamp] = timestamp;
                storage[name][Key.Source] = source;
            }
        }

        private void HandleStateChange(string name, object oldvalue, object newvalue, long oldTS, long newTS)
        {
            switch (name)
            {
                case "car_version":
                    Tools.DebugLog($"#{car.CarInDB}: TeslaAPIHandleStateChange {name} {oldvalue} -> {newvalue}");
                    _ = car.GetWebHelper().GetOdometerAsync();
                    break;
                case "is_user_present":
                    Tools.DebugLog($"#{car.CarInDB}: TeslaAPIHandleStateChange {name} {oldvalue} -> {newvalue}");
                    if (oldvalue is bool && (bool)oldvalue == true && newvalue is bool && (bool)newvalue == false && !car.IsCharging())
                    {
                        car.DriveFinished();
                    }
                    // car was used, eg. door opened/closed
                    if (oldvalue != null && newvalue != null && oldvalue != newvalue)
                    {
                        car.SetLastCarUsed(DateTime.Now);
                    }
                    break;
                case "locked":
                case "charge_port_door_open":
                case "df":
                case "pf":
                case "dr":
                case "pr":
                case "ft":
                case "rt":
                    // car was used, eg. door opened/closed
                    if (oldvalue != null && newvalue != null && oldvalue != newvalue)
                    {
                        car.SetLastCarUsed(DateTime.Now);
                        car.CurrentJSON.CreateCurrentJSON();
                    }
                    break;
                case "charging_state":
                    Tools.DebugLog($"#{car.CarInDB}: TeslaAPIHandleStateChange {name} {oldvalue} -> {newvalue}");
                    // charging_state Charging -> Complete - evaluate +occ special flag
                    if (oldvalue.Equals("Charging") && newvalue.Equals("Complete"))
                    {
                        Address addr = Geofence.GetInstance().GetPOI(car.CurrentJSON.GetLatitude(), car.CurrentJSON.GetLongitude(), false);
                        if (addr != null && addr.specialFlags != null && addr.specialFlags.Count > 0) {
                            foreach (KeyValuePair<Address.SpecialFlags, string> flag in addr.specialFlags)
                            {
                                switch (flag.Key)
                                {
                                    case Address.SpecialFlags.OnChargeComplete:
                                        car.HandleSpecialFlag_OnChargeComplete(addr, flag.Value);
                                        break;
                                    case Address.SpecialFlags.OpenChargePort:
                                    case Address.SpecialFlags.HighFrequencyLogging:
                                    case Address.SpecialFlags.EnableSentryMode:
                                    case Address.SpecialFlags.SetChargeLimit:
                                    case Address.SpecialFlags.SetChargeLimitOnArrival:
                                    case Address.SpecialFlags.ClimateOff:
                                    case Address.SpecialFlags.CopyChargePrice:
                                    case Address.SpecialFlags.CombineChargingStates:
                                    case Address.SpecialFlags.DoNotCombineChargingStates:
                                        break;
                                    default:
                                        car.Log("TeslaAPIHandleStateChange unhandled special flag " + flag.ToString());
                                        break;
                                }
                            }
                        }
                    }
                    else if (newvalue.Equals("Disconnected"))
                    {
                        car.DbHelper.UpdateUnplugDate();
                    }
                    break;
                case "battery_level":
                    // car is idle and battery level changed -> update ABRP
                    if (car.IsParked() && !car.IsCharging())
                    {
                        Tools.DebugLog($"#{car.CarInDB}: TeslaAPIHandleStateChange {name} {oldvalue} ({oldTS}) -> {newvalue} ({newTS})");
                        Tools.DebugLog($"TeslaAPIHandleStateChange {name} SendDataToAbetterrouteplannerAsync(utc:{newTS}, soc:{int.Parse(newvalue.ToString(), Tools.ciEnUS)}, speed:0, charging:false, power:0, lat:{car.CurrentJSON.GetLatitude()}, lon:{car.CurrentJSON.GetLongitude()})");
                        _ = car.webhelper.SendDataToAbetterrouteplannerAsync(newTS, int.Parse(newvalue.ToString(), Tools.ciEnUS), 0, false, 0, car.CurrentJSON.GetLatitude(), car.CurrentJSON.GetLongitude());
                    }
                    break;
                default:
                    break;
            }
        }

        public bool GetState(string name, out Dictionary<Key, object> state, int maxage = 0)
        {
            lock (TeslaAPIStateLock)
            {
                try
                {
                    if (storage.ContainsKey(name))
                    {
                        state = storage[name];
                        if (maxage != 0)
                        {
                            long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                            if (long.TryParse(storage[name][Key.Timestamp].ToString(), out long ts) && now - ts > maxage)
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                    state = new Dictionary<Key, object>() {
                    { Key.Type , "undef" },
                    { Key.Value , "undef" },
                    { Key.Timestamp , long.MinValue },
                    { Key.ValueLastUpdate , long.MinValue },
                    { Key.Source , "undef" }
                };
                    return false;
                }
                catch (Exception ex)
                {
                    car.CreateExceptionlessClient(ex).Submit();
                    Tools.DebugLog("Exception", ex);
                }
                state = new Dictionary<Key, object>() {
                    { Key.Type , "undef" },
                    { Key.Value , "undef" },
                    { Key.Timestamp , long.MinValue },
                    { Key.ValueLastUpdate , long.MinValue },
                    { Key.Source , "undef" }
                };
            }
            return false;
        }

        public bool HasValue(string name)
        {
            if (storage.ContainsKey(name) && storage[name].ContainsKey(Key.Type) && storage[name].ContainsKey(Key.Value))
            {
                return true;
            }

            return false;
        }

        public bool GetBool(string name, out bool value, int maxage = 0)
        {
            lock (TeslaAPIStateLock)
            {
                try
                {
                    if (storage.ContainsKey(name) && storage[name].ContainsKey(Key.Type) && storage[name].ContainsKey(Key.Value))
                    {
                        if (storage[name][Key.Type].Equals("bool") && storage[name][Key.Value] != null)
                        {
                            if (maxage != 0)
                            {
                                long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                                if (long.TryParse(storage[name][Key.Timestamp].ToString(), out long ts) && now - ts > maxage)
                                {
                                    value = false;
                                    return false;
                                }
                            }
                            return bool.TryParse(storage[name][Key.Value].ToString(), out value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    car.CreateExceptionlessClient(ex).Submit();
                    Tools.DebugLog("Exception", ex);
                }
            }
            value = false;
            return false;
        }

        public bool GetInt(string name, out int value, int maxage = 0)
        {
            lock (TeslaAPIStateLock)
            {
                try
                {
                    if (storage.ContainsKey(name) && storage[name].ContainsKey(Key.Type) && storage[name].ContainsKey(Key.Value))
                    {
                        if (storage[name][Key.Type].Equals("int") && storage[name][Key.Value] != null)
                        {
                            if (maxage != 0)
                            {
                                long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                                if (long.TryParse(storage[name][Key.Timestamp].ToString(), out long ts) && now - ts > maxage)
                                {
                                    value = int.MinValue;
                                    return false;
                                }
                            }
                            return int.TryParse(storage[name][Key.Value].ToString(), out value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    car.CreateExceptionlessClient(ex).Submit();
                    Tools.DebugLog("Exception", ex);
                }
                value = int.MinValue;
                return false;
            }
        }

        public bool GetDouble(string name, out double value, int maxage = 0)
        {
            lock (TeslaAPIStateLock)
            {
                try
                {
                    if (storage.ContainsKey(name) && storage[name].ContainsKey(Key.Type) && storage[name].ContainsKey(Key.Value))
                    {
                        if (storage[name][Key.Type].Equals("double") && storage[name][Key.Value] != null)
                        {
                            if (maxage != 0)
                            {
                                long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                                if (long.TryParse(storage[name][Key.Timestamp].ToString(), out long ts) && now - ts > maxage)
                                {
                                    value = double.NaN;
                                    return false;
                                }
                            }
                            return double.TryParse(storage[name][Key.Value].ToString(), out value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    car.CreateExceptionlessClient(ex).Submit();
                    Tools.DebugLog("Exception", ex);
                }
            }
            value = double.MinValue;
            return false;
        }

        public bool GetString(string name, out string value, int maxage = 0)
        {
            lock (TeslaAPIStateLock)
            {
                try
                {
                    if (storage.ContainsKey(name) && storage[name].ContainsKey(Key.Type) && storage[name].ContainsKey(Key.Value))
                    {
                        if (storage[name][Key.Type].Equals("string") && storage[name][Key.Value] != null)
                        {
                            if (maxage != 0)
                            {
                                long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                                if (long.TryParse(storage[name][Key.Timestamp].ToString(), out long ts) && now - ts > maxage)
                                {
                                    value = string.Empty;
                                    return false;
                                }
                            }
                            value = storage[name][Key.Value].ToString();
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    car.CreateExceptionlessClient(ex).Submit();
                    Tools.DebugLog("Exception", ex);
                }
                value = string.Empty;
                return false;
            }
        }

        public bool ParseAPI(string JSON, string source)
        {
            if (string.IsNullOrEmpty(JSON))
            {
                return false;
            }
            if (!JSON.Contains("{"))
            {
                return false;
            }
            if (dumpJSON)
            {
                string filename = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{source}_{car.CarInDB}.json";
                string filepath = Path.Combine(DumpJSONSessionDir, filename);
                _ = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        File.WriteAllText(filepath, new Tools.JsonFormatter(JSON).Format());
                    }
                    catch (Exception ex)
                    {
                        car.CreateExceptionlessClient(ex).Submit();
                        Tools.DebugLog("Exception", ex);
                    }
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
            try
            {
                dynamic jsonResult = JsonConvert.DeserializeObject(JSON);

                if (!Tools.IsPropertyExist(jsonResult, "response") || string.IsNullOrEmpty(jsonResult["response"].ToString()))
                    return false;
            }
            catch (ArgumentException aex)
            {
                car.CreateExceptionlessClient(aex).Submit();
                Tools.DebugLog("ArgumentException", aex);
                Tools.DebugLog("JSON: <" + JSON + ">");
                return false;
            }
            catch (Exception ex)
            {
                car.CreateExceptionlessClient(ex).Submit();
                Tools.DebugLog("Exception", ex);
                return false;
            }
            switch (source)
            {
                /*
                case "charge_state":
                    return ParseChargeState(JSON);
                case "climate_state":
                    return ParseClimateState(JSON);
                case "drive_state":
                    return ParseDriveState(JSON);
                case "vehicle_config":
                    return ParseVehicleConfig(JSON);
                case "vehicle_state":
                    return ParseVehicleState(JSON);
                */
                case "vehicle_data":
                    ParseChargeState(JSON);
                    ParseClimateState(JSON);
                    ParseDriveState(JSON);
                    ParseVehicleConfig(JSON);
                    ParseVehicleState(JSON);
                    break;
                case "vehicles":
                    return ParseVehicles(JSON);
                case "vehicle_data?endpoints=location_data":
                    break;
                default:
                    Logfile.Log($"ParseAPI: unknown source {source}");
                    break;
            }
            return false;
        }

        private bool ParseVehicles(string _JSON)
        {
            try
            {
                dynamic jsonResult = JsonConvert.DeserializeObject(_JSON);
                dynamic r1 = jsonResult["response"];
                if (r1 == null)
                    return false;

                dynamic r3 = SearchCarDictionary(r1);

                if (r3 == null)
                    return false;

                Dictionary<string, object> r4 = r3.ToObject<Dictionary<string, object>>();
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
                        case "ble_autopair_enrolled":
                        case "calendar_enabled":
                            if (r4.TryGetValue(key, out object value))
                            {
                                AddValue(key, "bool", value, 0, "vehicles");
                            }
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
                        case "granular_access":
                        case "command_signing":
                            if (r4.TryGetValue(key, out value))
                            {
                                AddValue(key, "string", value, 0, "vehicles");
                            }
                            break;
                        // int
                        case "api_version":
                            if (r4.TryGetValue(key, out value))
                            {
                                AddValue(key, "int", value, 0, "vehicles");
                            }
                            break;
                        // TODO
                        case "tokens":
                            break;
                        default:
                            if (!unknownKeys.Contains(key))
                            {
                                if (r4.TryGetValue(key, out value))
                                {
                                    string temp = $"INFO: ParseVehicles: unknown key {key} value <{value}> - " + value?.GetType().ToString();
                                    ExceptionlessLogUnknowKey(temp);
                                }
                                else
                                {
                                    string temp = $"INFO: ParseVehicles: unknown key {key}";
                                    ExceptionlessLogUnknowKey(temp);
                                }
                                unknownKeys.Add(key);
                            }
                            break;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                car.webhelper.SubmitExceptionlessClientWithResultContent(ex, _JSON);
                Tools.DebugLog("Exception", ex);
            }
            return false;
        }

        private bool ParseChargeState(string _JSON)
        {
            try
            {
                Dictionary<string, object> r2 = ExtractResponse(_JSON, "charge_state");
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
                if (r2.ContainsKey("timestamp") && long.TryParse(r2["timestamp"].ToString(), out long timestamp))
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
                            case "supercharger_session_trip_planner":
                            case "preconditioning_enabled":
                            case "off_peak_charging_enabled":
                            case "supports_fan_only_cabin_overheat_protection":                            
                                if (r2.TryGetValue(key, out object value))
                                {
                                    AddValue(key, "bool", value, timestamp, "charge_state");
                                }
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
                            case "scheduled_charging_mode":
                            case "preconditioning_times":
                            case "off_peak_charging_times":
                            case "charge_port_color":
                            case "hvac_auto_request":
                            case "cabin_overheat_protection":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "string", value, timestamp, "charge_state");
                                }
                                break;
                            // int
                            case "battery_level":
                            case "charge_amps":
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
                            case "off_peak_hours_end_time":
                            case "scheduled_charging_start_time_app":
                            case "scheduled_charging_start_time_minutes":
                            case "scheduled_departure_time":
                            case "scheduled_departure_time_minutes":
                            case "usable_battery_level":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "int", value, timestamp, "charge_state");
                                }
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
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "double", value, timestamp, "charge_state");
                                }
                                break;
                            default:
                                if (!unknownKeys.Contains(key))
                                {
                                    if (r2.TryGetValue(key, out value))
                                    {
                                        string temp = $"INFO: ParseChargeState: unknown key {key} value <{value}> - " + value?.GetType().ToString();
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    else
                                    {
                                        string temp = $"INFO: ParseChargeState: unknown key {key}";
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    unknownKeys.Add(key);
                                }
                                break;
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("Exception", ex);
            }
            return false;
        }

        private static Dictionary<string, object> ExtractResponse(string _JSON, string command)
        {
            dynamic jsonResult = JsonConvert.DeserializeObject(_JSON);
            Dictionary<string, object> r1 = jsonResult["response"][command].ToObject<Dictionary<string, object>>();
            return r1;
        }

        private bool ParseDriveState(string _JSON)
        {
            try
            {
                Dictionary<string, object> r2 = ExtractResponse(_JSON, "drive_state");
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
                if (r2.ContainsKey("timestamp") && long.TryParse(r2["timestamp"].ToString(), out long timestamp))
                {
                    foreach (string key in r2.Keys)
                    {
                        switch (key)
                        {
                            case "timestamp":
                                break;
                            // string
                            case "active_route_destination":
                            case "native_type":
                            case "shift_state":
                                if (r2.TryGetValue(key, out object value))
                                {
                                    AddValue(key, "string", value, timestamp, "drive_state");
                                }
                                break;
                            // int
                            case "active_route_energy_at_arrival":
                            case "gps_as_of":
                            case "heading":
                            case "native_location_supported":
                            case "power":
                            case "speed":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "int", value, timestamp, "drive_state");
                                }
                                break;
                            // double
                            case "active_route_latitude":
                            case "active_route_longitude":
                            case "active_route_miles_to_arrival":
                            case "active_route_minutes_to_arrival":
                            case "active_route_traffic_minutes_delay":
                            case "latitude":
                            case "longitude":
                            case "native_latitude":
                            case "native_longitude":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "double", value, timestamp, "drive_state");
                                }
                                break;
                            default:
                                if (!unknownKeys.Contains(key))
                                {
                                    if (r2.TryGetValue(key, out value))
                                    {
                                        string temp = $"INFO: ParseDriveState: unknown key {key} value <{value}> - " + value?.GetType().ToString();
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    else
                                    {
                                        string temp = $"INFO: ParseDriveState: unknown key {key}";
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    unknownKeys.Add(key);
                                }
                                break;
                        }
                    }

                    try
                    {
                        if (r2.ContainsKey("active_route_destination"))
                            car.CurrentJSON.active_route_destination = r2["active_route_destination"].ToString();
                        else
                            car.CurrentJSON.active_route_destination = null;

                        if (r2.ContainsKey("active_route_energy_at_arrival"))
                            car.CurrentJSON.active_route_energy_at_arrival = (long)r2["active_route_energy_at_arrival"];
                        else
                            car.CurrentJSON.active_route_energy_at_arrival = null;

                        if (r2.ContainsKey("active_route_minutes_to_arrival"))
                            car.CurrentJSON.active_route_minutes_to_arrival = (double)r2["active_route_minutes_to_arrival"];
                        else
                            car.CurrentJSON.active_route_minutes_to_arrival = null;

                        if (r2.ContainsKey("active_route_traffic_minutes_delay"))
                            car.CurrentJSON.active_route_traffic_minutes_delay = (double)r2["active_route_traffic_minutes_delay"];
                        else
                            car.CurrentJSON.active_route_traffic_minutes_delay = null;

                        if (r2.ContainsKey("active_route_latitude"))
                            car.CurrentJSON.active_route_latitude = (double)r2["active_route_latitude"];
                        else
                            car.CurrentJSON.active_route_latitude = null;

                        if (r2.ContainsKey("active_route_longitude"))
                            car.CurrentJSON.active_route_longitude = (double)r2["active_route_longitude"];
                        else
                            car.CurrentJSON.active_route_longitude = null;
                    }
                    catch (Exception ex)
                    {
                         ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("Exception", ex);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("Exception", ex);
            }
            return false;
        }

        private bool ParseVehicleConfig(string _JSON)
        {
            try
            {
                Dictionary<string, object> r2 = ExtractResponse(_JSON, "vehicle_config");
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
                if (r2.ContainsKey("timestamp") && long.TryParse(r2["timestamp"].ToString(), out long timestamp))
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
                            case "cop_user_set_temp_supported": // COP = cabin overheat protection
                            case "dashcam_clip_save_supported":
                            case "ece_restrictions":
                            case "eu_vehicle":
                            case "has_air_suspension":
                            case "has_ludicrous_mode":
                            case "has_seat_cooling":
                            case "motorized_charge_port":
                            case "plg":
                            case "pws":
                            case "range_plus_badging":
                            case "rhd":
                            case "supports_qr_pairing":
                            case "use_range_badging":
                            case "webcam_selfie_supported":
                            case "webcam_supported":
                                if (r2.TryGetValue(key, out object value))
                                {
                                    AddValue(key, "bool", value, timestamp, "vehicle_config");
                                }
                                break;
                            // string
                            case "aux_park_lamps":
                            case "car_special_type":
                            case "car_type":
                            case "charge_port_type":
                            case "default_charge_to_max":
                            case "driver_assist":
                            case "efficiency_package":
                            case "exterior_color":
                            case "exterior_trim":
                            case "exterior_trim_override":
                            case "front_drive_unit":
                            case "headlamp_type":
                            case "interior_trim_type":
                            case "paint_color_override":
                            case "perf_config":
                            case "performance_package":
                            case "rear_drive_unit":
                            case "roof_color":
                            case "spoiler_type":
                            case "third_row_seats":
                            case "trim_badging":
                            case "wheel_type":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "string", value, timestamp, "vehicle_config");
                                }
                                break;
                            // int
                            case "rear_seat_heaters":
                            case "rear_seat_type":
                            case "seat_type":
                            case "steering_wheel_type":
                            case "sun_roof_installed":
                            case "key_version":
                            case "utc_offset":
                            case "badge_version":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "int", value, timestamp, "vehicle_config");
                                }
                                break;
                            default:
                                if (!unknownKeys.Contains(key))
                                {
                                    if (r2.TryGetValue(key, out value))
                                    {
                                        string temp = $"INFO: ParseVehicleConfig: unknown key {key} value <{value}> - " + value?.GetType().ToString();
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    else
                                    {
                                        string temp = $"INFO: ParseVehicleConfig: unknown key";
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    unknownKeys.Add(key);
                                }
                                break;
                        }
                    }

                    try
                    {
                        if (r2.ContainsKey("wheel_type"))
                        {
                            string wheel_type = r2["wheel_type"].ToString();
                            if (wheel_type?.Length > 0)
                            {
                                if (car.wheel_type != wheel_type)
                                {
                                    car.Log("Wheel type changed: " + wheel_type);
                                    car.wheel_type = wheel_type;
                                    car.WriteSettings();
                                }
                            }
                        }
                    } catch (Exception ex)
                    {
                        car.CreateExceptionlessClient(ex).Submit();
                        Tools.DebugLog("Exception", ex);
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                car.CreateExceptionlessClient(ex).Submit();
                Tools.DebugLog("Exception", ex);
            }
            return false;
        }

        private bool ParseVehicleState(string _JSON)
        {
            try
            {
                Dictionary<string, object> r2 = ExtractResponse(_JSON, "vehicle_state");
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
                CheckTPMS(1, r2);
                CheckTPMS(2, r2);
                CheckTPMS(3, r2);
                CheckTPMS(4, r2);

                if (r2.ContainsKey("timestamp") && long.TryParse(r2["timestamp"].ToString(), out long timestamp))
                {
                    foreach (string key in r2.Keys)
                    {
                        switch (key)
                        {
                            case "timestamp":
                                break;
                            // bool
                            case "allow_authorized_mobile_devices_only":
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
                            case "service_mode":
                            case "service_mode_plus":
                            case "smart_summon_available":
                            case "summon_standby_mode_enabled":
                            case "valet_mode":
                            case "valet_pin_needed":
                            case "dashcam_clip_save_available":
                            case "vehicle_self_test_requested":
                            case "webcam_available":
                            case "tpms_soft_warning_fr":
                            case "tpms_soft_warning_fl":
                            case "tpms_soft_warning_rr":
                            case "tpms_soft_warning_rl":
                            case "tpms_hard_warning_fr":
                            case "tpms_hard_warning_fl":
                            case "tpms_hard_warning_rr":
                            case "tpms_hard_warning_rl":
                                if (r2.TryGetValue(key, out object value))
                                {
                                    AddValue(key, "bool", value, timestamp, "vehicle_state");
                                }
                                break;
                            // string
                            case "autopark_state_v2":
                            case "autopark_state_v3":
                            case "autopark_style":
                            case "car_version":
                            case "last_autopark_error":
                            case "sun_roof_state":
                            case "vehicle_name":
                            case "dashcam_state":
                            case "feature_bitmask":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "string", value, timestamp, "vehicle_state");
                                }
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
                            case "fd_window":
                            case "fp_window":
                            case "rd_window":
                            case "rp_window":
                            case "santa_mode":
                            case "vehicle_self_test_progress":
                            case "tpms_last_seen_pressure_time_fl":
                            case "tpms_last_seen_pressure_time_fr":
                            case "tpms_last_seen_pressure_time_rr":
                            case "tpms_last_seen_pressure_time_rl":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "int", value, timestamp, "vehicle_state");
                                }
                                break;
                            // double
                            case "odometer":
                            case "tpms_pressure_rr":
                            case "tpms_pressure_rl":
                            case "tpms_pressure_fr":
                            case "tpms_pressure_fl":
                            case "tpms_rcp_rear_value":
                            case "tpms_rcp_front_value":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "double", value, timestamp, "vehicle_state");
                                }
                                break;

                            // special case: software update
                            case "software_update":
                                if (r2.TryGetValue(key, out value))
                                {
                                    ParseSoftwareUpdate(value, timestamp);
                                }
                                break;
                            // TODO
                            case "media_state":
                            // TODO
                            case "media_info":
                            case "speed_limit_mode":
                                break;
                            default:
                                if (!unknownKeys.Contains(key))
                                {
                                    if (r2.TryGetValue(key, out value))
                                    {
                                        string temp = $"INFO: ParseVehicleState: unknown key {key} value <{value}> - " + value?.GetType().ToString();
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    else
                                    {
                                        string temp = $"INFO: ParseVehicleState: unknown key {key}";
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    unknownKeys.Add(key);
                                }
                                break;
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                car.CreateExceptionlessClient(ex).Submit();
                Tools.DebugLog("Exception", ex);
            }
            return false;
        }

        private void CheckTPMS(int TireID, Dictionary<string, object> r2)
        {
            try
            {
                string Prefix = "";
                switch (TireID)
                {
                    case 1:
                        Prefix = "fl"; break;
                    case 2:
                        Prefix = "fr"; break;
                    case 3:
                        Prefix = "rl"; break;
                    case 4:
                        Prefix = "rr"; break;
                    default:
                        return;
                }

                if (r2.ContainsKey("tpms_pressure_"+Prefix) && r2.ContainsKey("tpms_last_seen_pressure_time_" + Prefix) && r2["tpms_last_seen_pressure_time_" + Prefix] != null && r2["tpms_pressure_"+Prefix] != null)
                {
                    double pressure = (double)r2["tpms_pressure_"+Prefix];
                    DateTime dtPressure = DBHelper.UnixToDateTime((long)r2["tpms_last_seen_pressure_time_"+Prefix] * 1000);
                    //Tools.DebugLog($"Car{car.CarInDB} TPMS {Prefix}: {pressure} {dtPressure}");
                    car.DbHelper.InsertTPMS(TireID, pressure, dtPressure);
                }
            }
            catch (Exception ex)
            {
                car.CreateExceptionlessClient(ex).Submit();
            }
        }

        private void ParseSoftwareUpdate(object software_update, long timestamp)
        {
            /*
             * "software_update":{
             *   "download_perc":100,
             *   "expected_duration_sec":3000,
             *   "install_perc":60,
             *   "status":"installing",
             *   "version":"2020.36.3.1"
             *  }
             */
            if (software_update != null
                && software_update is JObject jo)
            {
                Dictionary<string, object> dictionary = jo.ToObject<Dictionary<string, object>>();
                if (dictionary != null)
                {
                    foreach (string key in dictionary.Keys)
                    {
                        switch (key)
                        {
                            // int
                            case "download_perc":
                            case "expected_duration_sec":
                            case "install_perc":
                            case "scheduled_time_ms":
                            case "warning_time_remaining_ms":
                                if (dictionary.TryGetValue(key, out object value))
                                {
                                    AddValue($"software_update.{key}", "int", value, timestamp, "vehicle_state.software_update");
                                }
                                break;
                            // string
                            case "status":
                            case "version":
                                if (dictionary.TryGetValue(key, out value))
                                {
                                    AddValue($"software_update.{key}", "string", value, timestamp, "vehicle_state.software_update");
                                }
                                break;
                            default:
                                if (!unknownKeys.Contains($"software_update.{key}"))
                                {
                                    if (dictionary.TryGetValue(key, out value))
                                    {
                                        string temp = $"INFO: ParseSoftwareUpdate: unknown key {key} value <{value}> - " + value?.GetType().ToString();
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    else
                                    {
                                        string temp = $"INFO: ParseSoftwareUpdate: unknown key {key}";
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    unknownKeys.Add($"software_update.{key}");
                                }
                                break;
                        }
                    }
                }
            }
        }

        private bool ParseClimateState(string _JSON)
        {
            try
            {
                Dictionary<string, object> r2 = ExtractResponse(_JSON, "climate_state");
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
                if (r2.ContainsKey("timestamp") && long.TryParse(r2["timestamp"].ToString(), out long timestamp))
                {
                    foreach (string key in r2.Keys)
                    {
                        switch (key)
                        {
                            case "timestamp":
                                break;
                            // bool
                            case "auto_steering_wheel_heat":
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
                            case "bioweapon_mode":
                            case "supports_fan_only_cabin_overheat_protection":
                            case "allow_cabin_overheat_protection":
                            case "cabin_overheat_protection_actively_cooling":
                            case "auto_seat_climate_right":
                            case "auto_seat_climate_left":

                                if (r2.TryGetValue(key, out object value))
                                {
                                    AddValue(key, "bool", value, timestamp, "climate_state");
                                }
                                break;
                            // string
                            case "climate_keeper_mode":
                            case "cop_activation_temperature": // COP = cabin overheat protection
                            case "hvac_auto_request":
                            case "cabin_overheat_protection":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "string", value, timestamp, "climate_state");
                                }
                                break;
                            // int
                            case "defrost_mode":
                            case "fan_status":
                            case "left_temp_direction":
                            case "right_temp_direction":
                            case "seat_fan_front_left":
                            case "seat_fan_front_right":
                            case "seat_heater_left":
                            case "seat_heater_rear_center":
                            case "seat_heater_rear_left":
                            case "seat_heater_rear_right":
                            case "seat_heater_right":
                            case "steering_wheel_heat_level":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "int", value, timestamp, "climate_state");
                                }
                                break;
                            // double
                            case "driver_temp_setting":
                            case "inside_temp":
                            case "max_avail_temp":
                            case "min_avail_temp":
                            case "outside_temp":
                            case "passenger_temp_setting":
                                if (r2.TryGetValue(key, out value))
                                {
                                    AddValue(key, "double", value, timestamp, "climate_state");
                                }
                                break;
                            default:
                                if (!unknownKeys.Contains(key))
                                {
                                    if (r2.TryGetValue(key, out value))
                                    {
                                        string temp = $"INFO: ParseClimateState: unknown key {key} value <{value}> - " + value?.GetType().ToString();
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    else
                                    {
                                        string temp = $"INFO: ParseClimateState: unknown key {key}";
                                        ExceptionlessLogUnknowKey(temp);
                                    }
                                    unknownKeys.Add(key);
                                }
                                break;
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                car.CreateExceptionlessClient(ex).Submit();
                Tools.DebugLog("Exception", ex);
            }
            return false;
        }

        public string ToString(bool compareTs = false)
        {
            string str = string.Empty;
            foreach (string key in storage.Keys)
            {
                if (compareTs && storage[key][Key.Timestamp] != null
                    && long.TryParse(storage[key][Key.Timestamp].ToString(), out long ts) && ts != 0
                    && long.TryParse(storage[key][Key.ValueLastUpdate].ToString(), out long vlu) && vlu != 0)
                {
                    long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                    str += string.Concat($"{key} => v:[{storage[key][Key.Value]}] t:{storage[key][Key.Type]} s:{storage[key][Key.Source]} ts:{storage[key][Key.Timestamp]} now:{now} diff:{now - ts}ms vlu:{storage[key][Key.ValueLastUpdate]} now:{now} diff:{now - vlu}ms", Environment.NewLine);
                }
                else
                {
                    str += string.Concat($"{key} => v:[{storage[key][Key.Value]}] t:{storage[key][Key.Type]} s:{storage[key][Key.Source]} ts:{storage[key][Key.Timestamp]}", Environment.NewLine);
                }
            }
            return str;
        }

        internal long GetTimestampAge(string source)
        {
            long maxTS = 0;
            foreach (string property in storage.Keys)
            {
                if (string.IsNullOrEmpty(source) || source == property)
                {
                    maxTS = (long)((maxTS < (long)storage[property][Key.Timestamp]) ? storage[property][Key.Timestamp] : maxTS);
                }
            }
            long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            return maxTS > 0 ? now - maxTS : 0;
        }

        void ExceptionlessLogUnknowKey(string text)
        {
            ExceptionlessClient.Default.CreateLog("TeslaApiState", text)
                .SetUserIdentity(car.TaskerHash)
                .AddObject(car.ModelName, "ModelName")
                .AddObject(car.CarType, "CarType")
                .AddObject(car.CarSpecialType, "CarSpecialType")
                .AddObject(car.TrimBadging, "CarTrimBadging")
                .Submit();

            Logfile.Log(text);
        }

        private object SearchCarDictionary(Newtonsoft.Json.Linq.JArray cars)
        {
            if (car.Vin?.Length > 0)
            {
                for (int x = 0; x < cars.Count; x++)
                {
                    var cc = cars[x];
                    var ccVin = cc["vin"].ToString();

                    if (ccVin == car.Vin)
                        return cc;
                }

                Logfile.Log("Car with VIN: " + car.Vin + " not found! Display Name: " + car.DisplayName);

                // DBHelper.ExecuteSQLQuery("delete from cars where id = " + car.CarInDB); 

                return null;
            }

            return null;
        }
    }
}