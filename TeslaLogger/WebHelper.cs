using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;


namespace TeslaLogger
{
    public class WebHelper
    {
        public static readonly string apiaddress = "https://owner-api.teslamotors.com/";

        public string Tesla_token = "";
        public string Tesla_id = "";
        public string Tesla_vehicle_id = "";
        public string Tesla_Streamingtoken = "";
        public string option_codes = "";
        public CarSettings carSettings = null;
        public string TaskerHash = string.Empty;
        public bool is_sentry_mode = false;
        public string fast_charger_brand = "";
        public string fast_charger_type = "";
        public string conn_charge_cable = "";
        public bool fast_charger_present = false;
        public static Geofence geofence;
        private bool stopStreaming = false;
        private string elevation = "";
        private DateTime elevation_time = DateTime.Now;
        public DateTime lastTokenRefresh = DateTime.Now;
        public DateTime lastIsDriveTimestamp = DateTime.Now;
        public DateTime lastUpdateEfficiency = DateTime.Now.AddDays(-1);
        private static int MapQuestCount = 0;
        private static int NominatimCount = 0;

        public ScanMyTesla scanMyTesla;
        private string _lastShift_State = "P";

        static WebHelper()
        {
            //Damit Mono keine Zertifikatfehler wirft :-(
            ServicePointManager.ServerCertificateValidationCallback += (p1, p2, p3, p4) => true;

            geofence = new Geofence();
        }

        public WebHelper()
        {
            carSettings = CarSettings.ReadSettings();
        }

        private string GetLastShiftState()
        {
            return _lastShift_State;
        }

        private void SetLastShiftState(string _newState)
        {
            if (!_newState.Equals(_lastShift_State))
            {
                HandleShiftStateChange(_lastShift_State, _newState);
                _lastShift_State = _newState;
            }
        }

        private void HandleShiftStateChange(string _oldState, string _newState)
        {
            Logfile.Log("Shift State Change: " + _oldState + " -> " + _newState);
            if ((_oldState.Equals("D") || _oldState.Equals("R") || _oldState.Equals("N")) && _newState.Equals("P"))
            {
                Address addr = geofence.GetPOI(DBHelper.currentJSON.latitude, DBHelper.currentJSON.longitude, false);
                HashSet<Geofence.SpecialFlags> specialFlags = Geofence.GetSpecialFlagsForLocationName(addr.name);
                foreach (Geofence.SpecialFlags flag in specialFlags)
                {
                    switch (flag)
                    {
                        case Geofence.SpecialFlags.OpenChargePort:
                            string result = PostCommand("command/charge_port_door_open", null).Result;
                            Logfile.Log("openChargePort(): " + result);
                            break;
                        default:
                            Logfile.Log("handleShiftStateChange(" + _oldState + ", " + _newState + ") default");
                            break;
                    }
                }
            }
        }

        public bool RestoreToken()
        {
            string filecontent = "";

            try
            {
                filecontent = FileManager.GetTeslaTokenFileContent();
                if (filecontent == string.Empty)
                {
                    return false;
                }

                string[] args = filecontent.Split('|');
                if (args.Length == 2 && args[0].Length == 64)
                {
                    DateTime dt = DateTime.Parse(args[1]);
                    TimeSpan ts = DateTime.Now - dt;

                    if (ts.TotalDays < 15)
                    {
                        Tesla_token = args[0];
                        lastTokenRefresh = dt;

                        Logfile.Log("Restore Token OK. Age: " + dt.ToString());
                        return true;
                    }
                    else
                    {
                        Logfile.Log("Restore Token too old! " + dt.ToString());
                    }
                }
                else
                {
                    if (filecontent == null)
                    {
                        filecontent = "NULL";
                    }

                    Logfile.Log("Restore Token not successful. " + filecontent);
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("Error in RestoreToken: " + ex.Message);
                Logfile.ExceptionWriter(ex, filecontent);
            }

            return false;
        }

        public async Task<string> GetTokenAsync()
        {
            string resultContent = "";
            try
            {
                string hiddenPassword = "";
                for (int x = 0; x < ApplicationSettings.Default.TeslaPasswort.Length; x++)
                {
                    hiddenPassword += "x";
                }

                Logfile.Log("Login with : '" + ApplicationSettings.Default.TeslaName + "' / '" + hiddenPassword + "'");

                if (ApplicationSettings.Default.TeslaName.Length == 0 || ApplicationSettings.Default.TeslaPasswort.Length == 0)
                {
                    Logfile.Log("NO Credentials");
                    throw new Exception("NO Credentials");
                }


                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "TeslaLogger");
                Dictionary<string, string> values = new Dictionary<string, string>
                {
                   { "grant_type", "password" },
                   { "client_id", "e4a9949fcfa04068f59abb5a658f2bac0a3428e4652315490b659d5ab3f35a9e" },
                   { "client_secret", "c75f14bbadc8bee3a7594412c31416f8300256d7668ea7e6e7f06727bfb9d220" },
                   { "email", ApplicationSettings.Default.TeslaName },
                   { "password", ApplicationSettings.Default.TeslaPasswort }
                };

                string json = new JavaScriptSerializer().Serialize(values);
                StringContent content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                
                DateTime start = DateTime.UtcNow;
                HttpResponseMessage result = await client.PostAsync(apiaddress + "oauth/token", content);
                resultContent = await result.Content.ReadAsStringAsync();
                
                DBHelper.AddMothershipDataToDB("GetTokenAsync()", start, (int)result.StatusCode);

                if (resultContent.Contains("authorization_required"))
                {
                    Logfile.Log("Wrong Credentials");

                    if (Tools.IsDocker())
                    {
                        Thread.Sleep(5 * 60000);
                    }

                    throw new Exception("Wrong Credentials");
                }

                Tools.SetThread_enUS();
                dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                Tesla_token = jsonResult["access_token"];

                FileManager.WriteTeslaTokenFile(Tesla_token);

                return Tesla_token;
            }
            catch (Exception ex)
            {
                Logfile.Log("Error in GetTokenAsync: " + ex.Message);
                Logfile.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        private string lastCharging_State = "";

        public void ResetLastChargingState()
        {
            lastCharging_State = "";
        }

        internal bool IsCharging(bool justCheck = false)
        {
            string resultContent = "";
            try
            {
                resultContent = GetCommand("charge_state").Result;

                Task<double?> outside_temp = GetOutsideTempAsync();

                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;

                if (r2["charging_state"] == null)
                {
                    if (justCheck)
                    {
                        return false;
                    }

                    Logfile.Log("charging_state = null");

                    Thread.Sleep(10000);

                    return lastCharging_State == "Charging";
                }

                string charging_state = r2["charging_state"].ToString();
                string timestamp = r2["timestamp"].ToString();
                decimal ideal_battery_range = (decimal)r2["ideal_battery_range"];
                if (ideal_battery_range == 999)
                {
                    ideal_battery_range = (decimal)r2["battery_range"];
                }

                string battery_level = r2["battery_level"].ToString();
                string charger_power = "";
                if (r2["charger_power"] != null)
                {
                    charger_power = r2["charger_power"].ToString();
                }

                string charge_energy_added = r2["charge_energy_added"].ToString();

                string charger_voltage = "";
                string charger_phases = "";
                string charger_actual_current = "";
                string charge_current_request = "";
                string charger_pilot_current = "";
                

                if (r2["charger_voltage"] != null)
                {
                    charger_voltage = r2["charger_voltage"].ToString();
                }

                if (r2["charger_phases"] != null)
                {
                    charger_phases = r2["charger_phases"].ToString();
                }

                if (r2["charger_actual_current"] != null)
                {
                    charger_actual_current = r2["charger_actual_current"].ToString();
                }

                if (r2["charge_current_request"] != null)
                {
                    charge_current_request = r2["charge_current_request"].ToString();
                }

                if (r2["charger_pilot_current"] != null)
                {
                    charger_pilot_current = r2["charger_pilot_current"].ToString();
                }

                if (r2["fast_charger_brand"] != null)
                {
                    fast_charger_brand = r2["fast_charger_brand"].ToString();
                }

                if (r2["fast_charger_type"] != null)
                {
                    fast_charger_type = r2["fast_charger_type"].ToString();
                }

                if (r2["conn_charge_cable"] != null)
                {
                    conn_charge_cable = r2["conn_charge_cable"].ToString();
                }

                if (r2["fast_charger_present"] != null)
                {
                    fast_charger_present = bool.Parse(r2["fast_charger_present"].ToString());
                }

                if (r2["charge_limit_soc"] != null)
                {
                    if (DBHelper.currentJSON.charge_limit_soc != Convert.ToInt32(r2["charge_limit_soc"]))
                    {
                        DBHelper.currentJSON.charge_limit_soc = Convert.ToInt32(r2["charge_limit_soc"]);
                        DBHelper.currentJSON.CreateCurrentJSON();
                    }
                }

                if (justCheck)
                {
                    if (charging_state == "Charging")
                    {
                        string dtTimestamp = "?";
                        try
                        {
                            dtTimestamp = DBHelper.UnixToDateTime(long.Parse(timestamp)).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        catch (Exception)
                        { }

                        Logfile.Log($"Charging! Voltage: {charger_voltage}V / Power: {charger_power}kW / Timestamp: {timestamp} / Date: {dtTimestamp}");

                        return double.TryParse(charger_power, out double dPowerkW) && dPowerkW >= 1.0;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (charging_state == "Charging")
                {
                    lastCharging_State = charging_state;
                    DBHelper.InsertCharging(timestamp, battery_level, charge_energy_added, charger_power, (double)ideal_battery_range, charger_voltage, charger_phases, charger_actual_current, outside_temp.Result, false, charger_pilot_current, charge_current_request);
                    return true;
                }
                else if (charging_state == "Complete")
                {
                    if (lastCharging_State != "Complete")
                    {
                        DBHelper.InsertCharging(timestamp, battery_level, charge_energy_added, charger_power, (double)ideal_battery_range, charger_voltage, charger_phases, charger_actual_current, outside_temp.Result, true, charger_pilot_current, charge_current_request);
                        System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + " : Charging Complete");
                    }

                    lastCharging_State = charging_state;
                }
            }
            catch (Exception ex)
            {
                if (resultContent == null || resultContent == "NULL")
                {
                    Logfile.Log("isCharging = NULL");
                    Thread.Sleep(10000);
                }
                else if (!resultContent.Contains("upstream internal error"))
                {
                    Logfile.ExceptionWriter(ex, resultContent);
                }

                if (lastCharging_State == "Charging" && !justCheck)
                {
                    return true;
                }
            }

            return false;
        }

        public string GetVehicles()
        {
            string resultContent = "";
            while (true)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                    string adresse = apiaddress + "api/1/vehicles";
                    DateTime start = DateTime.UtcNow;
                    Task<HttpResponseMessage> resultTask = client.GetAsync(adresse);

                    HttpResponseMessage result = resultTask.Result;
                    resultContent = result.Content.ReadAsStringAsync().Result;
                    DBHelper.AddMothershipDataToDB("GetVehicles()", start, (int)result.StatusCode);
    
                    if (result.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        Logfile.Log("HttpStatusCode = Unauthorized. Password changed or still valid?");
                    }

                    object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                    object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                    object[] r1temp = (object[])r1;

                    if (ApplicationSettings.Default.Car >= r1temp.Length)
                    {
                        Logfile.Log("Car # " + ApplicationSettings.Default.Car + " not exists!");
                        return "NULL";
                    }

                    Dictionary<string, object> r2 = (Dictionary<string, object>)r1temp[ApplicationSettings.Default.Car];

                    string OnlineState = r2["state"].ToString();
                    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + " : " + OnlineState);

                    string display_name = r2["display_name"].ToString();
                    Logfile.Log("display_name: " + display_name);

                    try
                    {
                        string filepath = System.IO.Path.Combine(FileManager.GetExecutingPath(), "DISPLAY_NAME");
                        System.IO.File.WriteAllText(filepath, display_name);
                        UpdateTeslalogger.Chmod(filepath, 666, false);
                    }
                    catch (Exception)
                    { }

                    string vin = r2["vin"].ToString();
                    Logfile.Log("vin :" + vin);

                    Tesla_id = r2["id"].ToString();
                    Logfile.Log("id :" + Tesla_id);

                    Tesla_vehicle_id = r2["vehicle_id"].ToString();
                    Logfile.Log("vehicle_id :" + Tesla_vehicle_id);

                    byte[] tempTasker = Encoding.UTF8.GetBytes(vin + ApplicationSettings.Default.TeslaName);

                    TaskerHash = string.Empty;
                    DamienG.Security.Cryptography.Crc32 crc32 = new DamienG.Security.Cryptography.Crc32();
                    foreach (byte b in crc32.ComputeHash(tempTasker))
                    {
                        TaskerHash += b.ToString("x2").ToLower();
                    }

                    if (!string.IsNullOrEmpty(ApplicationSettings.Default.TaskerPrefix))
                    {
                        TaskerHash = ApplicationSettings.Default.TaskerPrefix + "_" + TaskerHash;
                    }

                    if (ApplicationSettings.Default.Car > 0)
                    {
                        TaskerHash = TaskerHash + "_" + ApplicationSettings.Default.Car;
                    }

                    Logfile.Log("Tasker Config:\r\n Server Port : https://teslalogger.de\r\n Pfad : wakeup.php\r\n Attribute : t=" + TaskerHash);

                    try
                    {
                        string taskertokenpath = System.IO.Path.Combine(FileManager.GetExecutingPath(), "TASKERTOKEN");
                        System.IO.File.WriteAllText(taskertokenpath, TaskerHash);
                    }
                    catch (Exception)
                    { }

                    scanMyTesla = new ScanMyTesla(TaskerHash);

                    /*
                    dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                    token = jsonResult["access_token"];
                    */

                    return resultContent;

                }
                catch (Exception ex)
                {
                    Logfile.ExceptionWriter(ex, resultContent);

                    while (ex != null)
                    {
                        if (!(ex is AggregateException))
                        {
                            Logfile.Log("GetVehicles Error: " + ex.Message);
                        }

                        ex = ex.InnerException;
                    }

                    Thread.Sleep(30000);
                }
            }
        }

        private int unknownStateCounter = 0;

        public async Task<string> IsOnline()
        {
            string resultContent = "";
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                string adresse = apiaddress + "api/1/vehicles";
                
                DateTime start = DateTime.UtcNow;
                HttpResponseMessage result = await client.GetAsync(adresse);
                resultContent = await result.Content.ReadAsStringAsync();
                DBHelper.AddMothershipDataToDB("IsOnline()", start, (int)result.StatusCode);

                if (result.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Logfile.Log("HttpStatusCode = Unauthorized. Password changed or still valid?");
                    Thread.Sleep(30000);
                }

                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);

                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                object[] r2 = (object[])r1;
                object r3 = r2[ApplicationSettings.Default.Car];
                Dictionary<string, object> r4 = (Dictionary<string, object>)r3;
                string state = r4["state"].ToString();
                object[] tokens = (object[])r4["tokens"];
                Tesla_Streamingtoken = tokens[0].ToString();

                try
                {
                    /* 
                    option_codes = r4["option_codes"].ToString();
                    string[] oc = option_codes.Split(',');

                    carSettings.AWD = oc.Contains("DV4W");

                    if (oc.Contains("MDLS") || oc.Contains("MS01") || oc.Contains("MS02") || oc.Contains("MS03"))
                        carSettings.Model = "MS";
                    else if (oc.Contains("MDLX"))
                        carSettings.Model = "MX";
                    else if (oc.Contains("MDL3"))
                        carSettings.Model = "M3";

                    var battery = oc.Where(r => r.StartsWith("BT")).ToArray();
                    if (battery != null && battery.Length > 0)
                    {
                        if (carSettings.Battery != battery[0])
                        {
                            Logfile.Log("Battery: " + battery[0] + " / " + carSettings.Model);
                            carSettings.Battery = battery[0];

                            carSettings.WriteSettings();
                        }
                    }

                    carSettings.Performance = oc.Contains("PBT85") || oc.Contains("PX01") || oc.Contains("P85D") || oc.Contains("PX6D") || oc.Contains("X024") | oc.Contains("PBT8") | oc.Contains("PF01");

                    */

                    if (state == "asleep")
                    {
                        return state;
                    }
                    else if (state == "unknown")
                    {
                        Logfile.Log("unknown state " + unknownStateCounter);

                        Logfile.ExceptionWriter(new Exception("unknown state"), resultContent);

                        if (unknownStateCounter == 0)
                        {
                            string r = Wakeup().Result;
                            Logfile.Log("WakupResult: " + r);
                        }
                        else
                        {
                            Thread.Sleep(10000);
                        }

                        unknownStateCounter++;

                        if (unknownStateCounter == 6)
                        {
                            unknownStateCounter = 0;
                        }
                    }
                    else
                    {
                        unknownStateCounter = 0;
                    }

                    TimeSpan ts = DateTime.Now - lastUpdateEfficiency;

                    if (ts.TotalMinutes > 60)
                    {
                        string badge = GetCommand("vehicle_config").Result;

                        dynamic jBadge = new JavaScriptSerializer().DeserializeObject(badge);

                        dynamic jBadgeResult = jBadge["response"];

                        if (jBadgeResult != null)
                        {
                            if (Tools.IsPropertyExist(jBadgeResult, "car_type"))
                            {
                                carSettings.car_type = jBadgeResult["car_type"].ToString().ToLower().Trim();
                            }

                            if (Tools.IsPropertyExist(jBadgeResult, "car_special_type"))
                            {
                                carSettings.car_special_type = jBadgeResult["car_special_type"].ToString().ToLower().Trim();
                            }

                            carSettings.trim_badging = Tools.IsPropertyExist(jBadgeResult, "trim_badging")
                                ? (string)jBadgeResult["trim_badging"].ToString().ToLower().Trim()
                                : "";

                            UpdateEfficiency();
                            lastUpdateEfficiency = DateTime.Now;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logfile.ExceptionWriter(ex, resultContent);
                }

                return state;
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public void UpdateEfficiency()
        {
            //string eff = "0.190052356";

            if (carSettings.car_type == "model3")
            {
                int maxRange = DBHelper.GetAvgMaxRage();
                if (maxRange > 400)
                {
                    try
                    {
                        if (double.TryParse(carSettings.DB_Wh_TR, System.Globalization.NumberStyles.Any, Tools.ciEnUS, out double wh))
                        {
                            if (wh >= 0.143 && wh <= 0.148)
                            {
                                WriteCarSettings("0.152", "M3 LR RWD");
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.ToString());
                    }

                    WriteCarSettings("0.152", "M3 LR");
                    return;
                }
                else
                {
                    WriteCarSettings("0.137", "M3 SR+");
                    return;
                }
            }
            else if (carSettings.car_type == "models2" && carSettings.car_special_type == "base")
            {
                if (carSettings.trim_badging == "60")
                {
                    WriteCarSettings("0.200", "S 60");
                    return;
                }
                else if (carSettings.trim_badging == "60d")
                {
                    WriteCarSettings("0.187", "S 60D");
                    return;
                }
                else if (carSettings.trim_badging == "75d")
                {
                    WriteCarSettings("0.186", "S 75D");
                    return;
                }
                else if (carSettings.trim_badging == "75")
                {
                    WriteCarSettings("0.195", "S 75");
                    return;
                }
                else if (carSettings.trim_badging == "90d")
                {
                    WriteCarSettings("0.188", "S 90D");
                    return;
                }
                else if (carSettings.trim_badging == "p90")
                {
                    WriteCarSettings("0.201", "S P90");
                    return;
                }
                else if (carSettings.trim_badging == "p90d")
                {
                    WriteCarSettings("0.201", "S P90D");
                    return;
                }
                else if (carSettings.trim_badging == "100d")
                {
                    WriteCarSettings("0.189", "S 100D");
                    return;
                }
                else if (carSettings.trim_badging == "p100d")
                {
                    WriteCarSettings("0.200", "S P100D");
                    return;
                }
                else if (carSettings.trim_badging == "")
                {
                    WriteCarSettings("0.169", "S Raven");
                    return;
                }
                else
                {
                    WriteCarSettings("0.190", "S ???");
                    return;
                }
            }
            else if (carSettings.car_type == "models" && carSettings.car_special_type == "base")
            {
                if (carSettings.trim_badging == "60")
                {
                    WriteCarSettings("0.200", "S 60");
                    return;
                }
                else if (carSettings.trim_badging == "70")
                {
                    WriteCarSettings("0.200", "S 70");
                    return;
                }
                else if (carSettings.trim_badging == "70d")
                {
                    WriteCarSettings("0.194", "S 70D");
                    return;
                }
                else if (carSettings.trim_badging == "p85d")
                {
                    WriteCarSettings("0.201", "S P85D");
                    return;
                }
                else if (carSettings.trim_badging == "p85+")
                {
                    WriteCarSettings("0.201", "S P85+");
                    return;
                }
                else if (carSettings.trim_badging == "85d")
                {
                    WriteCarSettings("0.186", "S 85D");
                    return;
                }
                else if (carSettings.trim_badging == "p85")
                {
                    WriteCarSettings("0.210", "S P85");
                    return;
                }
                else if (carSettings.trim_badging == "85")
                {
                    WriteCarSettings("0.201", "S 85");
                    return;
                }
                else if (carSettings.trim_badging == "90")
                {
                    WriteCarSettings("0.201", "S 90");
                    return;
                }
                else if (carSettings.trim_badging == "90d")
                {
                    WriteCarSettings("0.187", "S 90D");
                    return;
                }
                else if (carSettings.trim_badging == "p90")
                {
                    WriteCarSettings("0.201", "S P90");
                    return;
                }
                else if (carSettings.trim_badging == "p90d")
                {
                    WriteCarSettings("0.202", "S P90D");
                    return;
                }
                else
                {
                    WriteCarSettings("0.200", "S ???");
                    return;
                }
            }
            else if (carSettings.car_type == "modelx" && carSettings.car_special_type == "base")
            {
                if (carSettings.trim_badging == "75d")
                {
                    WriteCarSettings("0.224", "X 75D");
                    return;
                }
                else if (carSettings.trim_badging == "100d")
                {
                    WriteCarSettings("0.217", "X 100D");
                    return;
                }
                else if (carSettings.trim_badging == "90d")
                {
                    WriteCarSettings("0.212", "X 90D");
                    return;
                }
                else if (carSettings.trim_badging == "p100d")
                {
                    WriteCarSettings("0.226", "X P100D");
                    return;
                }
                else if (carSettings.trim_badging == "p90d")
                {
                    WriteCarSettings("0.217", "X P90D");
                    return;
                }
                else
                {
                    WriteCarSettings("0.204", "X"); // Raven
                    return;
                }
            }

            return;
            /*
            if (carSettings.Model == "MS")
            {
                if (carSettings.Battery == "BTX5")
                {
                    if (carSettings.AWD)
                    {
                        eff = "0.186";
                        car = "S 75D";
                    }
                    else
                    {
                        eff = "0.185";
                        car = "S 75";
                    }
                }
                else if (carSettings.Battery == "BTX4")
                {
                    if (carSettings.Performance)
                    {
                        eff = "0.200";
                        car = "S P90D";
                    }
                    else
                    {
                        eff = "0.189";
                        car = "S90D";
                    }
                }
                else if (carSettings.Battery == "BTX6")
                {
                    if (carSettings.Performance)
                    {
                        eff = "0.200";
                        car = "S P100D";
                    }
                    else
                    {
                        eff = "0.189";
                        car = "S 100D";
                    }
                }
                else if (carSettings.Battery == "BTX8")
                {
                    if (carSettings.AWD)
                    {
                        eff = "0.186";
                        car = "S 75D (85kWh)";
                    }
                    else
                    {
                        eff = "0.185";
                        car = "S 75 (85kWh)";
                    }
                }
                else if (carSettings.Battery == "BT85")
                {
                    if (carSettings.AWD)
                    {
                        if (carSettings.Performance)
                        {
                            car = "S P85D";
                            eff = "0.201";
                        }
                        else
                        {
                            car = "S 85D";
                            eff = "0.186";
                        }
                    }
                    else
                    {
                        if (carSettings.Performance)
                        {
                            car = "S P85";
                            eff = "0.210";
                        }
                        else
                        {
                            car = "S 85";
                            eff = "0.201";
                        }
                    }
                }
                else if (carSettings.Battery == "PBT85")
                {
                    car = "S P85";
                    eff = "0.210";
                }
                else if (carSettings.Battery == "BT70")
                {
                    car = "S 70 ?";
                    eff = "0.200";
                }
                else if (carSettings.Battery == "BT60")
                {
                    car = "S 60 ?";
                    eff = "0.200";
                }
                else
                {
                    car = "S ???";
                    eff = "0.200";
                }
            }
            else if (carSettings.Model == "MX")
            {
                if (carSettings.Battery == "BTX5")
                {
                    eff = "0.208";
                    car = "X 75D";
                }
                else if (carSettings.Battery == "BTX4")
                {
                    if (!carSettings.Performance)
                    {
                        eff = "0.208";
                        car = "X 90D";
                    }
                    else
                    {
                        eff = "0.217";
                        car = "X P90D";
                    }
                }
                else if (carSettings.Battery == "BTX6")
                {
                    if (carSettings.Performance)
                    {
                        eff = "0.226";
                        car = "X P100D";
                    }
                    else
                    {
                        eff = "0.208";
                        car = "X 100D";
                    }
                }
                else
                {
                    car = "X ???";
                    eff = "0.208";
                }

            }
            else if (carSettings.Model == "M3")
            {
                if (carSettings.Battery == "BT37")
                {
                    if (carSettings.Performance)
                    {
                        eff = "0.153";
                        car = "M3P";
                    }
                    else
                    {
                        eff = "0.153";
                        car = "M3";
                    }
                }
                else
                {
                    eff = "0.153";
                    car = "M3 ???";
                }
            }
            else
            {
                if (carSettings.Battery == "BT85")
                {
                    car = "S 85 ?";
                    eff = "0.200";
                }
            }

            WriteCarSettings(eff, car);
            */
        }

        private void WriteCarSettings(string eff, string car)
        {
            if (carSettings.Name != car || carSettings.Wh_TR != eff)
            {
                Logfile.Log("WriteCarSettings " + car + " eff: " + eff);

                carSettings.Name = car;
                carSettings.Wh_TR = eff;
                carSettings.WriteSettings();
            }
        }

        public bool IsDriving(bool justinsertdb = false)
        {
            string resultContent = "";
            try
            {
                resultContent = GetCommand("drive_state").Result;

                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;
                decimal dLatitude = (decimal)r2["latitude"];
                decimal dLongitude = (decimal)r2["longitude"];

                double latitude = (double)dLatitude;
                double longitude = (double)dLongitude;

                DBHelper.currentJSON.latitude = latitude;
                DBHelper.currentJSON.longitude = longitude;

                string timestamp = r2["timestamp"].ToString();
                int speed = 0;
                if (r2["speed"] != null)
                {
                    speed = (int)r2["speed"];
                }

                int power = 0;
                if (r2["power"] != null)
                {
                    power = (int)r2["power"];
                }

                string shift_state = "";
                if (r2["shift_state"] != null)
                {
                    shift_state = r2["shift_state"].ToString();
                    SetLastShiftState(shift_state);
                }
                else
                {
                    TimeSpan ts = DateTime.Now - lastIsDriveTimestamp;

                    if (ts.TotalMinutes > 10)
                    {
                        if (GetLastShiftState() != "P")
                        {
                            Logfile.Log("No Valid IsDriving since 10min! (shift_state=NULL)");
                        }
                        SetLastShiftState("P");
                        return false;
                    }
                    else
                    {
                        shift_state = GetLastShiftState();
                    }
                }

                if (justinsertdb || shift_state == "D" || shift_state == "R" || shift_state == "N" || DBHelper.currentJSON.current_is_preconditioning)
                {
                    // var address = ReverseGecocodingAsync(latitude, longitude);
                    //var altitude = AltitudeAsync(latitude, longitude);
                    Task<double> odometer = GetOdometerAsync();
                    double? outside_temp = null;
                    Task<double?> t_outside_temp = null;

                    if (!geofence.RacingMode)
                    {
                        t_outside_temp = GetOutsideTempAsync();
                    }

                    TimeSpan tsElevation = DateTime.Now - elevation_time;
                    if (tsElevation.TotalSeconds > 30)
                    {
                        elevation = "";
                    }

                    double ideal_battery_range_km = GetIdealBatteryRangekm(out int battery_level);

                    if (t_outside_temp != null)
                    {
                        outside_temp = t_outside_temp.Result;
                    }

                    DBHelper.InsertPos(timestamp, latitude, longitude, speed, power, odometer.Result, ideal_battery_range_km, battery_level, outside_temp, elevation);

                    if (shift_state == "D" || shift_state == "R" || shift_state == "N")
                    {
                        lastIsDriveTimestamp = DateTime.Now;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (resultContent == null || resultContent == "NULL")
                {
                    Logfile.Log("IsDriving = NULL!");
                    Thread.Sleep(10000);
                }
                else
                {
                    Logfile.ExceptionWriter(ex, resultContent);
                }

                if (GetLastShiftState() == "D" || GetLastShiftState() == "R" || GetLastShiftState() == "N")
                {
                    TimeSpan ts = DateTime.Now - lastIsDriveTimestamp;

                    if (ts.TotalMinutes > 10)
                    {
                        Logfile.Log("No Valid IsDriving since 10min! (Exception)");
                        SetLastShiftState("P");
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        public void StartStreamThread()
        {
            /* StreamingAPI Doesn't work anymore
            System.Threading.Thread t = new System.Threading.Thread(() => StartStream());
            t.Start();
            */
        }

        private void StartStream()
        {
            Logfile.Log("StartStream");
            stopStreaming = false;
            string line = "";
            while (!stopStreaming)
            {
                try
                {
                    string online = IsOnline().Result;

                    using (System.Net.WebSockets.ClientWebSocket ws = new System.Net.WebSockets.ClientWebSocket())
                    {
                        byte[] byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", ApplicationSettings.Default.TeslaName, Tesla_Streamingtoken));
                        Uri serverUri = new Uri($"wss://streaming.vn.teslamotors.com/connect/{Tesla_vehicle_id}");
                        //Uri serverUri = new Uri($"wss://streaming.vn.teslamotors.com/streaming/{Tesla_vehicle_id}/?values=speed,odometer,soc,elevation,est_heading,est_lat,est_lng,power,shift_state,est_range");

                        //ws.Options.Credentials = new NetworkCredential(ApplicationSettings.Default.TeslaName, Tesla_Streamingtoken);
                        ws.Options.UseDefaultCredentials = false;
                        ws.Options.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(byteArray));

                        Task result = ws.ConnectAsync(serverUri, CancellationToken.None);

                        while (!stopStreaming && ws.State == System.Net.WebSockets.WebSocketState.Connecting)
                        {
                            System.Diagnostics.Debug.WriteLine("Connecting");
                            Thread.Sleep(100);
                        }


                        ArraySegment<byte> bufferPing = new ArraySegment<byte>(Encoding.ASCII.GetBytes("PING"));
                        string msg = "{\"msg_type\": \"data:subscribe\", \"value\": [\"speed\",\"odometer\",\"soc\",\"elevation\",\"est_heading\",\"est_lat\",\"est_lng\",\"est_corrected_lat\",\"est_corrected_lng\",\"native_latitude\",\"native_longitude\",\"native_heading\",\"native_type\",\"native_location_supported\",\"power\",\"shift_state\"]}";
                        ArraySegment<byte> bufferMSG = new ArraySegment<byte>(Encoding.ASCII.GetBytes(msg));

                        if (ws.State == System.Net.WebSockets.WebSocketState.Open)
                        {
                            ws.SendAsync(bufferMSG, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                        }

                        while (ws.State == System.Net.WebSockets.WebSocketState.Open)
                        {
                            Thread.Sleep(100);
                            byte[] buffer = new byte[1024];
                            Task<System.Net.WebSockets.WebSocketReceiveResult> response = ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);


                            string r = Encoding.UTF8.GetString(buffer);
                            System.Diagnostics.Debug.WriteLine(r);
                            Thread.Sleep(100);
                            ws.SendAsync(bufferPing, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                            Thread.Sleep(1000);

                            Logfile.ExceptionWriter(null, r);
                        }
                    }
                    Logfile.Log("StreamEnd");
                    System.Diagnostics.Debug.WriteLine("StreamEnd");



                    return;

                    /*using (var client = new HttpClient())
                    {

                        var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", ApplicationSettings.Default.TeslaName, Tesla_Streamingtoken));
                        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                        client.Timeout = TimeSpan.FromMilliseconds(System.Threading.Timeout.Infinite);

                        string url = "https://streaming.vn.teslamotors.com/stream/" + Tesla_vehicle_id + "/?values=speed,odometer,soc,elevation,est_heading,est_lat,est_lng,power,shift_state,est_range";

                        // var stream = client.GetStreamAsync(url).Result; -> funktioniert nicht in MONO - bekannter bug
                        var stream = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).Result.Content.ReadAsStreamAsync().Result;

                        using (var reader = new System.IO.StreamReader(stream))
                        {
                            while (!stopStreaming && !reader.EndOfStream)
                            {
                                line = reader.ReadLine();
                                if (!string.IsNullOrEmpty(line))
                                {
                                    if (line == "Vehicle is offline")
                                        continue;

                                    var values = line.Split(',');
                                    // Logfile.Log("Elevation: " + values[4]);

                                    elevation = values[4];
                                    elevation_time = DateTime.Now;
                                }
                            }
                        }
                    }*/
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine(ex.ToString());

                    Logfile.ExceptionWriter(ex, line);
                    Thread.Sleep(10000);
                }
            }

            Logfile.Log("StartStream Ende");
        }


        /*public async Task<double> AltitudeAsync(double latitude, double longitude)
        {
            return 0;
            /*
            string url = "";
            string resultContent = "";
            try
            {
                WebClient webClient = new WebClient();

                webClient.Headers.Add("User-Agent: TeslaLogger");
                webClient.Encoding = Encoding.UTF8;
                url = String.Format("https://api.open-elevation.com/api/v1/lookup?locations={0},{1}", latitude, longitude);
                long ms = Environment.TickCount;
                resultContent = await webClient.DownloadStringTaskAsync(new Uri(url));

                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                var r1 = ((System.Collections.Generic.Dictionary<string, object>)jsonResult)["results"];
                var r2 = (object[])r1;
                var r3 = (System.Collections.Generic.Dictionary<string, object>)r2[0];
                string elevation = r3["elevation"].ToString();
                ms = Environment.TickCount - ms;

                System.Diagnostics.Debug.WriteLine("Altitude: " + elevation +  " ms: " + ms);

                return Double.Parse(elevation);
            }
            catch (Exception ex)
            {
                if (url == null)
                    url = "NULL";

                if (resultContent == null)
                    resultContent = "NULL";

                Logfile.ExceptionWriter(ex, url + "\r\n" + resultContent);
            }
            return 0;
            
        }*/

        public static async Task<string> ReverseGecocodingAsync(double latitude, double longitude, bool forceGeocoding = false)
        {
            string url = "";
            string resultContent = "";
            try
            {
                if (!forceGeocoding)
                {
                    Address a = null;
                    a = geofence.GetPOI(latitude, longitude);
                    if (a != null)
                    {
                        Logfile.Log("Reverse geocoding by Geofence");
                        return a.name;
                    }

                    string value = GeocodeCache.Instance.Search(latitude, longitude);
                    if (value != null)
                    {
                        Logfile.Log("Reverse geocoding by Cache");
                        return value;
                    }
                }

                Tools.SetThread_enUS();

                Thread.Sleep(5000); // Sleep to not get banned by Nominatim

                WebClient webClient = new WebClient();

                webClient.Headers.Add("User-Agent: TL 1.1");
                webClient.Encoding = Encoding.UTF8;

                url = !string.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey)
                    ? "http://open.mapquestapi.com/nominatim/v1/reverse.php"
                    : "http://nominatim.openstreetmap.org/reverse";

                url += "?format=jsonv2&lat=";
                url += latitude.ToString();
                url += "&lon=";
                url += longitude.ToString();

                if (!string.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                {
                    url += "&key=";
                    url += ApplicationSettings.Default.MapQuestKey;
                }
                else
                {
                    url += "&email=mail";
                    url += "@";
                    url += "teslalogger";
                    url += ".de";
                }

                DateTime start = DateTime.UtcNow;
                resultContent = await webClient.DownloadStringTaskAsync(new Uri(url));
                DBHelper.AddMothershipDataToDB("ReverseGeocoding", start, 0);

                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                object r1 = ((Dictionary<string, object>)jsonResult)["address"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;
                string postcode = "";
                if (r2.ContainsKey("postcode"))
                {
                    postcode = r2["postcode"].ToString();
                }

                string country_code = r2["country_code"].ToString();

                if (country_code.Length > 0)
                    DBHelper.currentJSON.current_country_code = country_code;                

                string road = "";
                if (r2.ContainsKey("road"))
                {
                    road = r2["road"].ToString();
                }

                string city = "";
                if (r2.ContainsKey("city"))
                {
                    city = r2["city"].ToString();
                }
                else if (r2.ContainsKey("village"))
                {
                    city = r2["village"].ToString();
                }
                else if (r2.ContainsKey("town"))
                {
                    city = r2["town"].ToString();
                }

                string house_number = "";
                if (r2.ContainsKey("house_number"))
                {
                    house_number = r2["house_number"].ToString();
                }

                string name = "";
                if (r2.ContainsKey("name") && r2["name"] != null)
                {
                    name = r2["name"].ToString();
                }

                string address29 = "";
                if (r2.ContainsKey("address29") && r2["address29"] != null)
                {
                    address29 = r2["address29"].ToString();
                }

                string adresse = "";

                if (address29.Length > 0)
                {
                    adresse += address29 + ", ";
                }

                if (country_code != "de")
                {
                    adresse += country_code + "-";
                }

                adresse += postcode + " " + city + ", " + road + " " + house_number;

                if (name.Length > 0)
                {
                    adresse += " / " + name;
                }

                System.Diagnostics.Debug.WriteLine(url + "\r\n" + adresse);

                GeocodeCache.Instance.Insert(latitude, longitude, adresse);

                if (!string.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                {
                    MapQuestCount++;
                    Logfile.Log("Reverse geocoding by MapQuest: " + MapQuestCount);
                }
                else
                {
                    NominatimCount++;
                    Logfile.Log("Reverse geocoding by Nominatim" + NominatimCount);
                }

                return adresse;
            }
            catch (Exception ex)
            {
                if (url == null)
                {
                    url = "NULL";
                }

                if (resultContent == null)
                {
                    resultContent = "NULL";
                }

                Logfile.ExceptionWriter(ex, url + "\r\n" + resultContent);
            }

            return "";
        }

        public void UpdateAllPosAddresses()
        {
            using (SqlConnection con = new SqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("Select lat, lng, id from pos where address = ''", con);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Thread.Sleep(10000); // Sleep to not get banned by Nominatim !

                    double lat = (double)dr[0];
                    double lng = (double)dr[1];
                    int id = (int)dr[2];
                    Task<string> adress = ReverseGecocodingAsync(lat, lng);
                    //var altitude = AltitudeAsync(lat, lng);
                    //UpdateAddressByPosId(id, adress.Result, altitude.Result);
                    UpdateAddressByPosId(id, adress.Result, 0);
                }
            }
        }

        private static void UpdateAddressByPosId(int id, string address, double altitude)
        {
            try
            {
                using (MySqlConnection con2 = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con2.Open();
                    MySqlCommand cmd2 = new MySqlCommand("update pos set address=@address, altitude=@altitude where id = @id", con2);
                    cmd2.Parameters.AddWithValue("@id", id);
                    cmd2.Parameters.AddWithValue("@address", address);
                    cmd2.Parameters.AddWithValue("@altitude", altitude);
                    cmd2.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine("id updateed: " + id + " address: " + address);
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "UpdateAddressByPosId");
            }
        }

        public void UpdateAllEmptyAddresses()
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand(@"SELECT  
        pos_start.address AS Start_address,
        pos_end.address AS End_address,
        pos_start.id AS PosStartId,
        pos_start.lat AS PosStartLat,
        pos_start.lng AS PosStartLng,
        pos_end.id AS PosEndId,
        pos_end.lat AS PosEndtLat,
        pos_end.lng AS PosEndLng
FROM
        drivestate
        JOIN pos pos_start ON drivestate.StartPos = pos_start.id
        JOIN pos pos_end ON drivestate.EndPos = pos_end.id
    WHERE
        ((pos_end.odometer - pos_start.odometer) > 0.1) and (pos_start.address IS null or pos_end.address IS null or pos_start.address = '' or pos_end.address = '')", con);

                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Thread.Sleep(10000); // Sleep to not get banned by Nominatim !
                    try
                    {
                        if (!(dr["Start_address"] != DBNull.Value && dr["Start_address"].ToString().Length > 0))
                        {
                            int id = (int)dr["PosStartId"];
                            double lat = (double)dr["PosStartLat"];
                            double lng = (double)dr["PosStartLng"];
                            Task<string> address = ReverseGecocodingAsync(lat, lng);
                            //var altitude = AltitudeAsync(lat, lng);

                            string addressResult = address.Result;
                            if (!string.IsNullOrEmpty(addressResult))
                            {
                                //UpdateAddressByPosId(id, addressResult, altitude.Result);
                                UpdateAddressByPosId(id, addressResult, 0);
                            }
                        }

                        if (!(dr["End_address"] != DBNull.Value && dr["End_address"].ToString().Length > 0))
                        {
                            int id = (int)dr["PosEndId"];
                            double lat = (double)dr["PosEndtLat"];
                            double lng = (double)dr["PosEndLng"];
                            Task<string> address = ReverseGecocodingAsync(lat, lng);
                            //var altitude = AltitudeAsync(lat, lng);

                            string addressResult = address.Result;
                            if (!string.IsNullOrEmpty(addressResult))
                            {
                                //UpdateAddressByPosId(id, addressResult, altitude.Result);
                                UpdateAddressByPosId(id, addressResult, 0);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.ExceptionWriter(ex, "");
                    }
                }
            }

            GeocodeCache.Instance.Write();

            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand(@"SELECT pos.id, lat, lng FROM chargingstate join pos on chargingstate.Pos = pos.id where address IS null OR address = '' or pos.id = ''", con);

                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    Thread.Sleep(10000); // Sleep to not get banned by Nominatim !
                    try
                    {
                        int id = (int)dr[0];
                        double lat = (double)dr[1];
                        double lng = (double)dr[2];
                        Task<string> address = ReverseGecocodingAsync(lat, lng);
                        //var altitude = AltitudeAsync(lat, lng);

                        string addressResult = address.Result;
                        if (!string.IsNullOrEmpty(addressResult))
                        {
                            //UpdateAddressByPosId(id, addressResult, altitude.Result);
                            UpdateAddressByPosId(id, addressResult, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.ExceptionWriter(ex, "");
                    }
                }
            }

            GeocodeCache.Instance.Write();
        }

        public static void UpdateAllPOIAddresses()
        {
            try
            {
                if (geofence.RacingMode)
                {
                    return;
                }

                int t = Environment.TickCount;
                int count = 0;
                Logfile.Log("UpdateAllPOIAddresses start");

                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("Select lat, lng, id, address from pos where id in (SELECT Pos FROM chargingstate) or id in (SELECT StartPos FROM drivestate) or id in (SELECT EndPos FROM drivestate)", con);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    int t2 = Environment.TickCount - t;
                    Logfile.Log($"UpdateAllPOIAddresses Select {t2}ms");

                    while (dr.Read())
                    {
                        try
                        {
                            Thread.Sleep(2);
                            double lat = (double)dr[0];
                            double lng = (double)dr[1];
                            int id = (int)dr[2];

                            Address a = geofence.GetPOI(lat, lng, false);
                            if (a == null)
                            {
                                if (dr[3] == DBNull.Value || dr[3].ToString().Length == 0)
                                {
                                    DBHelper.UpdateAddress(id);
                                }
                                continue;
                            }

                            if (dr[3] == DBNull.Value || a.name != dr[3].ToString())
                            {
                                using (MySqlConnection con2 = new MySqlConnection(DBHelper.DBConnectionstring))
                                {
                                    con2.Open();
                                    MySqlCommand cmd2 = new MySqlCommand("update pos set address=@address where id = @id", con2);
                                    cmd2.Parameters.AddWithValue("@id", id);
                                    cmd2.Parameters.AddWithValue("@address", a.name);
                                    cmd2.ExecuteNonQuery();

                                    count++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logfile.Log(" Exception in UpdateAllPOIAddresses: " + ex.Message);
                        }
                    }
                }

                t = Environment.TickCount - t;
                Logfile.Log($"UpdateAllPOIAddresses end {t}ms count:{count}");
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private double GetIdealBatteryRangekm(out int battery_level)
        {
            string resultContent = "";
            battery_level = -1;

            try
            {
                resultContent = GetCommand("charge_state").Result;

                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;

                if (r2["ideal_battery_range"] == null)
                {
                    return -1;
                }

                decimal ideal_battery_range = (decimal)r2["ideal_battery_range"];
                if (ideal_battery_range == 999)
                {
                    ideal_battery_range = (decimal)r2["battery_range"];
                    if (!carSettings.Raven)
                    {
                        carSettings.Raven = true;
                        carSettings.WriteSettings();
                        Logfile.Log("Raven Model!");
                    }
                }

                if (r2["battery_level"] != null)
                {
                    battery_level = Convert.ToInt32(r2["battery_level"]);
                    DBHelper.currentJSON.current_battery_level = battery_level;
                }

                return (double)ideal_battery_range / (double)0.62137;
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, resultContent);
            }
            return -1;
        }

        private double lastOdometerKM = 0;

        private async Task<double> GetOdometerAsync()
        {
            string resultContent = "";
            try
            {
                resultContent = await GetCommand("vehicle_state");
                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;

                if (r2.ContainsKey("sentry_mode") && r2["sentry_mode"] != null)
                {
                    try
                    {
                        bool sentry_mode = (bool)r2["sentry_mode"];

                        if (sentry_mode != is_sentry_mode)
                        {
                            is_sentry_mode = sentry_mode;
                            Logfile.Log("sentry_mode: " + sentry_mode);
                        }

                        DBHelper.currentJSON.current_is_sentry_mode = sentry_mode;
                    }
                    catch (Exception ex)
                    {
                        Logfile.ExceptionWriter(ex, resultContent);
                        Logfile.Log(ex.Message);
                    }
                }

                if (r2["odometer"] == null)
                {
                    Logfile.Log("odometer = NULL");
                    return lastOdometerKM;
                }

                decimal odometer = (decimal)r2["odometer"];

                try
                {
                    string car_version = r2["car_version"].ToString();
                    if (DBHelper.currentJSON.current_car_version != car_version)
                    {
                        Logfile.Log("Car Version: " + car_version);
                        DBHelper.currentJSON.current_car_version = car_version;

                        DBHelper.SetCarVersion(car_version);

                        TaskerWakeupfile(true);
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }

                decimal odometerKM = odometer / 0.62137M;
                lastOdometerKM = (double)odometerKM;
                return lastOdometerKM;
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, resultContent);
                return lastOdometerKM;
            }
            //return 0;
        }

        private async Task<double?> GetOutsideTempAsync()
        {
            string cacheKey = "GetOutsideTempAsync";
            object cacheValue = MemoryCache.Default.Get(cacheKey);
            if (cacheValue != null)
            {
                return (double)cacheValue;
            }

            string resultContent = null;
            try
            {
                resultContent = await GetCommand("climate_state");
                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;

                try
                {
                    if (r2["inside_temp"] != null)
                    {
                        DBHelper.currentJSON.current_inside_temperature = Convert.ToDouble(r2["inside_temp"]);
                    }
                }
                catch (Exception) { }

                decimal? outside_temp = null;
                if (r2["outside_temp"] != null)
                {
                    outside_temp = (decimal)r2["outside_temp"];
                    DBHelper.currentJSON.current_outside_temp = (double)outside_temp;
                }
                else
                {
                    return null;
                }

                try
                {
                    bool? battery_heater = null;
                    if (r2["battery_heater"] != null)
                    {
                        battery_heater = (bool)r2["battery_heater"];

                        if (DBHelper.currentJSON.current_battery_heater != battery_heater)
                        {
                            DBHelper.currentJSON.current_battery_heater = (bool)battery_heater;

                            Logfile.Log("Battery heater: " + battery_heater);
                            DBHelper.currentJSON.CreateCurrentJSON();

                            // write into Database
                            Thread.Sleep(5000);
                            IsDriving(true);
                            Thread.Sleep(5000);
                        }
                    }
                }
                catch (Exception) { }


                bool preconditioning = r2["is_preconditioning"] != null && (bool)r2["is_preconditioning"];
                
                if (preconditioning != DBHelper.currentJSON.current_is_preconditioning)
                {
                    DBHelper.currentJSON.current_is_preconditioning = preconditioning;
                    Logfile.Log("Preconditioning: " + preconditioning);
                    DBHelper.currentJSON.CreateCurrentJSON();

                    // write into Database
                    Thread.Sleep(5000);
                    IsDriving(true);
                    Thread.Sleep(5000);
                }

                MemoryCache.Default.Add(cacheKey, (double)outside_temp, DateTime.Now.AddMinutes(1));
                return (double)outside_temp;
            }
            catch (Exception ex)
            {
                if (resultContent == null)
                {
                    Logfile.Log("GetOutsideTempAsync: NULL");
                    return null;
                }
                else if (!resultContent.Contains("upstream internal error"))
                {
                    Logfile.ExceptionWriter(ex, resultContent);
                }
            }
            return null;
        }

        public async Task<string> GetCommand(string cmd)
        {
            string resultContent = "";
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                string adresse = apiaddress + "api/1/vehicles/" + Tesla_id + "/data_request/" + cmd;
                
                DateTime start = DateTime.UtcNow;
                HttpResponseMessage result = await client.GetAsync(adresse);
                resultContent = await result.Content.ReadAsStringAsync();
                DBHelper.AddMothershipDataToDB("GetCommand(" + cmd + ")", start, (int)result.StatusCode);

                return resultContent;
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public async Task<string> PostCommand(string cmd, string data)
        {
            Logfile.Log("PostCommand: " + cmd + " - " + data);

            string resultContent = "";
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                string url = apiaddress + "api/1/vehicles/" + Tesla_id + "/" + cmd;

                StringContent queryString = data != null ? new StringContent(data) : null;
                
                DateTime start = DateTime.UtcNow;
                HttpResponseMessage result = await client.PostAsync(url, data != null ? queryString : null);
                resultContent = await result.Content.ReadAsStringAsync();
                DBHelper.AddMothershipDataToDB("PostCommand(" + cmd + ")", start, (int)result.StatusCode);

                return resultContent;
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public async Task<string> Wakeup()
        {
            return await PostCommand("wake_up", "");
        }


        public string GetCachedRollupData()
        {
            string resultContent = "";
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                string adresse = apiaddress + "api/1/vehicles/" + Tesla_id + "/data";
                
                DateTime start = DateTime.UtcNow;
                Task<HttpResponseMessage> resultTask = client.GetAsync(adresse);
                HttpResponseMessage result = resultTask.Result;
                resultContent = result.Content.ReadAsStringAsync().Result;
                DBHelper.AddMothershipDataToDB("GetCachedRollupData()", start, 0);

                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r1temp = (Dictionary<string, object>)r1;
                string OnlineState = r1temp["state"].ToString();
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + " : " + OnlineState);
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1temp["drive_state"];

                double latitude = double.Parse(r2["latitude"].ToString());
                double longitude = double.Parse(r2["longitude"].ToString());
                string timestamp = r2["timestamp"].ToString();
                int speed = 0;
                if (r2["speed"] != null)
                {
                    speed = (int)r2["speed"];
                }

                int power = 0;
                if (r2["power"] != null)
                {
                    power = (int)r2["power"];
                }

                string shift_state = "";
                if (r2["shift_state"] != null)
                {
                    shift_state = r2["shift_state"].ToString();
                }

                if (shift_state == "D")
                {
                    DBHelper.InsertPos(timestamp, latitude, longitude, speed, power, 0, 0, 0, 0.0, "0"); // TODO: ODOMETER, ideal battery range, address
                }

                return resultContent;
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public DataTable GetEnergyChartData()
        {
            // https://www.energy-charts.de/power/week_2018_46.json
            string resultContent = "";

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "C# App");

            Task<HttpResponseMessage> resultTask = client.GetAsync("https://www.energy-charts.de/power/week_2018_46.json");
            HttpResponseMessage result = resultTask.Result;
            resultContent = result.Content.ReadAsStringAsync().Result;

            object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);

            DataTable dt = new DataTable();
            dt.Columns.Add("name");
            dt.Columns.Add("kWh", typeof(decimal));
            dt.Columns.Add("Datum", typeof(DateTime));

            object[] o1 = (object[])jsonResult;
            foreach (object o2 in o1)
            {
                Dictionary<string, object> o3 = o2 as Dictionary<string, object>;
                object[] name = o3["key"] as object[];
                Dictionary<string, object> n2 = name[0] as Dictionary<string, object>;
                string realname = n2["de"].ToString();

                if (realname.Contains("geplant") || realname.Contains("Prognose"))
                {
                    continue;
                }

                object[] values = o3["values"] as object[];

                decimal lastkWh = 0;
                for (int x = values.Length - 1; x >= 0; x--)
                {
                    object[] v2 = values[x] as object[];

                    if (v2[1] != null)
                    {
                        if (v2[1] is decimal)
                        {
                            lastkWh = (decimal)v2[1];
                        }
                        else if (v2[1] is int)
                        {
                            lastkWh = Convert.ToDecimal((int)v2[1]);
                        }

                        DataRow dr = dt.NewRow();
                        dr["name"] = realname;
                        dr["kWh"] = lastkWh;
                        dr["Datum"] = DBHelper.UnixToDateTime((long)v2[0]);
                        dt.Rows.Add(dr);
                        break;
                    }
                }
            }


            return dt;

        }

        public void StopStreaming()
        {
            Logfile.Log("Request StopStreaming");
            stopStreaming = true;
        }

        private DateTime lastTaskerWakeupfile = DateTime.Today;

        public bool TaskerWakeupfile(bool force = false)
        {
            try
            {
                Tools.GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin);

                TimeSpan ts = DateTime.Now - lastTaskerWakeupfile;

                if (!force && ts.TotalSeconds < 20)
                {
                    return false;
                }

                //Logfile.Log("Check Tasker Webservice");

                lastTaskerWakeupfile = DateTime.Now;

                string name = carSettings.Name;
                if (carSettings.Raven && !name.Contains("Raven"))
                {
                    name += " Raven";
                }

                HttpClient client = new HttpClient();

                Dictionary<string, string> d = new Dictionary<string, string>
                {
                    { "t", TaskerHash },
                    { "v", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() },
                    { "cv", DBHelper.currentJSON.current_car_version },
                    { "m", carSettings.Model },
                    { "bt", carSettings.Battery },
                    { "n", name },
                    { "eff", carSettings.Wh_TR },
                    { "oc", option_codes },

                    { "db_eff", carSettings.DB_Wh_TR },
                    { "db_eff_cnt", carSettings.DB_Wh_TR_count },

                    { "pw", power },
                    { "temp", temperature },
                    { "le", length },
                    { "ln", language },

                    { "CT", carSettings.car_type },
                    { "CST", carSettings.car_special_type },
                    { "TB", carSettings.trim_badging },

                    { "G", Tools.GetGrafanaVersion() },

                    { "D", Tools.IsDocker() ? "1" : "0" },
                    { "SMT", Tools.UseScanMyTesla() ? "1" : "0" },
                    { "SMTs", DBHelper.GetScanMyTeslaSignalsLastWeek().ToString() },
                    { "SMTp", DBHelper.GetScanMyTeslaPacketsLastWeek().ToString() },
                    { "TR", DBHelper.GetAvgMaxRage().ToString() },

                    { "OS", Tools.GetOsVersion() },
                    { "CC", DBHelper.currentJSON.current_country_code }
                };

                FormUrlEncodedContent content = new FormUrlEncodedContent(d);
                string query = content.ReadAsStringAsync().Result;

                DateTime start = DateTime.UtcNow;
                Task<HttpResponseMessage> resultTask = client.PostAsync("http://teslalogger.de/wakefile.php", content);

                HttpResponseMessage result = resultTask.Result;
                string resultContent = result.Content.ReadAsStringAsync().Result;

                DBHelper.AddMothershipDataToDB("teslalogger.de/wakefile.php", start, (int)result.StatusCode);

                if (resultContent.Contains("wakeupfile"))
                {
                    try
                    {
                        string lasttaskerwakeupfilepaht = System.IO.Path.Combine(FileManager.GetExecutingPath(), "LASTTASKERWAKEUPFILE");
                        string ltwf = resultContent.Replace("wakeupfile", "").Trim();
                        System.IO.File.WriteAllText(lasttaskerwakeupfilepaht, ltwf);
                    }
                    catch (Exception)
                    { }

                    Logfile.Log("TaskerWakeupfile available! [Webservice]" + resultContent.Replace("wakeupfile", ""));
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("TaskerWakeupToken Exception: " + ex.Message);
            }

            return false;
        }

        public bool DeleteWakeupFile()
        {
            bool ret = false;
            if (TaskerWakeupfile())
            {
                ret = true;
            }

            if (ExistsWakeupFile)
            {
                Logfile.Log("Delete Wakeup file");
                System.IO.File.Delete(FileManager.GetFilePath(TLFilename.WakeupFilename));
                ret = true;
            }

            return ret;
        }

        public bool ExistsWakeupFile => System.IO.File.Exists(FileManager.GetFilePath(TLFilename.WakeupFilename)) || TaskerWakeupfile();
    }
}
