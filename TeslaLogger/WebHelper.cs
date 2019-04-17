using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;


namespace TeslaLogger
{
    class WebHelper
    {
        public static readonly String apiaddress = "https://owner-api.teslamotors.com/";
        
        public string Tesla_token = "";
        public string Tesla_id = "";
        public string Tesla_vehicle_id = "";
        public string Tesla_Streamingtoken = "";
        public string option_codes = "";
        public CarSettings carSettings = null;        
        public string TaskerHash = String.Empty;
        public bool is_preconditioning = false;
        static Geofence geofence;
        bool stopStreaming = false;
        string elevation = "";
        DateTime elevation_time = DateTime.Now;
        public DateTime lastTokenRefresh = DateTime.Now;
        public DateTime lastIsDriveTimestamp = DateTime.Now;

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

        public bool RestoreToken()
        {
            string filecontent = "";

            try
            {
                filecontent = FileManager.GetTeslaTokenFileContent();
                if (filecontent == string.Empty)
                    return false;

                String [] args = filecontent.Split('|');
                if (args.Length == 2 && args[0].Length == 64)
                {
                    DateTime dt = DateTime.Parse(args[1]);
                    TimeSpan ts = DateTime.Now - dt;

                    if (ts.TotalDays < 15)
                    {
                        Tesla_token = args[0];
                        lastTokenRefresh = dt;

                        Tools.Log("Restore Token OK. Age: " + dt.ToString());
                        return true;
                    }
                    else
                    {
                        Tools.Log("Restore Token too old! " + dt.ToString());
                    }
                }
                else
                {
                    if (filecontent == null)
                        filecontent = "NULL";

                    Tools.Log("Restore Token not successful. " + filecontent);
                }
            }
            catch (Exception ex)
            {
                Tools.Log("Error in RestoreToken: " + ex.Message);
                Tools.ExceptionWriter(ex, filecontent);
            }

            return false;
        }

        public async Task<String> GetTokenAsync()
        {
            string resultContent = "";
            try
            {
                string hiddenPassword = "";
                for (int x = 0; x < ApplicationSettings.Default.TeslaPasswort.Length; x++)
                    hiddenPassword += "x";

                Tools.Log("Login with : '" + ApplicationSettings.Default.TeslaName + "' / '"+ hiddenPassword +"'");

                if (ApplicationSettings.Default.TeslaName.Length == 0 || ApplicationSettings.Default.TeslaPasswort.Length == 0)
                {
                    Tools.Log("NO Credentials");
                    throw new Exception("NO Credentials");
                }


                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "TeslaLogger");
                var values = new Dictionary<string, string>
                {
                   { "grant_type", "password" },
                   { "client_id", "e4a9949fcfa04068f59abb5a658f2bac0a3428e4652315490b659d5ab3f35a9e" },
                   { "client_secret", "c75f14bbadc8bee3a7594412c31416f8300256d7668ea7e6e7f06727bfb9d220" },
                   { "email", ApplicationSettings.Default.TeslaName },
                   { "password", ApplicationSettings.Default.TeslaPasswort }
                };
                
                var json = new JavaScriptSerializer().Serialize(values);
                var content = new StringContent(json.ToString(), Encoding.UTF8, "application/json");
                var result = await client.PostAsync(apiaddress + "oauth/token", content);

                resultContent = await result.Content.ReadAsStringAsync();

                if (resultContent.Contains("authorization_required"))
                {
                    Tools.Log("Wrong Credentials");
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
                Tools.Log("Error in GetTokenAsync: " + ex.Message);
                Tools.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        String lastCharging_State = "";

        internal bool isCharging()
        {
            string resultContent = "";
            try
            {
                resultContent = GetCommand("charge_state").Result;
                var outside_temp = GetOutsideTempAsync();

                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                var r1 = ((System.Collections.Generic.Dictionary<string, object>)jsonResult)["response"];
                var r2 = (System.Collections.Generic.Dictionary<string, object>)r1;

                if (r2["charging_state"] == null)
                {
                    Tools.Log("charging_state = null");

                    System.Threading.Thread.Sleep(10000);

                    if (lastCharging_State == "Charging")
                        return true;
                    else
                        return false;
                }

                var charging_state = r2["charging_state"].ToString();
                var timestamp = r2["timestamp"].ToString();
                decimal ideal_battery_range = (decimal)r2["ideal_battery_range"];
                var battery_level = r2["battery_level"].ToString();
                var charger_power = "";
                if (r2["charger_power"] != null)
                    charger_power = r2["charger_power"].ToString();

                var charge_energy_added = r2["charge_energy_added"].ToString();

                var charger_voltage = "";
                var charger_phases = "";
                var charger_actual_current = "";

                if (r2["charger_voltage"] != null)
                    charger_voltage = r2["charger_voltage"].ToString();

                if (r2["charger_phases"] != null)
                    charger_phases = r2["charger_phases"].ToString();

                if (r2["charger_actual_current"] != null)
                    charger_actual_current = r2["charger_actual_current"].ToString();

                if (charging_state == "Charging")
                {
                    lastCharging_State = charging_state;
                    DBHelper.InsertCharging(timestamp, battery_level, charge_energy_added, charger_power, (double)ideal_battery_range, charger_voltage, charger_phases, charger_actual_current, outside_temp.Result, false);
                    return true;
                }
                else if (charging_state == "Complete")
                {
                    if (lastCharging_State != "Complete")
                    {
                        DBHelper.InsertCharging(timestamp, battery_level, charge_energy_added, charger_power, (double)ideal_battery_range, charger_voltage, charger_phases, charger_actual_current, outside_temp.Result, true);
                        System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + " : Charging Complete");
                    }

                    lastCharging_State = charging_state;
                }
            }
            catch (Exception ex)
            {
                if (resultContent == null)
                {
                    Tools.Log("isCharging = NULL");
                }
                else if (!resultContent.Contains("upstream internal error"))
                    Tools.ExceptionWriter(ex, resultContent);

                if (lastCharging_State == "Charging")
                    return true;
            }

            return false;
        }

        public String GetVehicles()
        {
            string resultContent = "";
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                string adresse = apiaddress + "api/1/vehicles";
                var resultTask = client.GetAsync(adresse);

                HttpResponseMessage result = resultTask.Result;
                resultContent = result.Content.ReadAsStringAsync().Result;

                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                var r1 = ((System.Collections.Generic.Dictionary<string, object>)jsonResult)["response"];
                var r1temp = (object[])r1;

                if (ApplicationSettings.Default.Car >= r1temp.Length)
                {
                    Tools.Log("Car # " + ApplicationSettings.Default.Car + " not exists!");
                    return "NULL";
                }

                var r2 = ((System.Collections.Generic.Dictionary<string, object>)r1temp[ApplicationSettings.Default.Car]);

                string OnlineState = r2["state"].ToString();
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + " : " + OnlineState);

                string display_name = r2["display_name"].ToString();
                Tools.Log("display_name :" + display_name);

                string vin = r2["vin"].ToString();
                Tools.Log("vin :" + vin);

                Tesla_id = r2["id"].ToString();
                Tools.Log("id :" + Tesla_id);

                Tesla_vehicle_id = r2["vehicle_id"].ToString();
                Tools.Log("vehicle_id :" + Tesla_vehicle_id);

                byte[] tempTasker = System.Text.Encoding.UTF8.GetBytes(vin + ApplicationSettings.Default.TeslaName);

                TaskerHash = String.Empty;
                var crc32 = new DamienG.Security.Cryptography.Crc32();
                foreach (byte b in crc32.ComputeHash(tempTasker))
                    TaskerHash += b.ToString("x2").ToLower();

                Tools.Log("Tasker Config:\r\n Server Port : https://teslalogger.de\r\n Pfad : wakeup.php\r\n Attribute : t=" + TaskerHash);

                /*
                dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                token = jsonResult["access_token"];
                */

                return resultContent;
            }
            catch (Exception ex)
            {
                Tools.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        int unknownStateCounter = 0;

        public async Task<String> IsOnline()
        {
            string resultContent = "";
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                string adresse = apiaddress + "api/1/vehicles";
                var result = await client.GetAsync(adresse);

                resultContent = await result.Content.ReadAsStringAsync();

                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);

                var r1 = ((System.Collections.Generic.Dictionary<string, object>)jsonResult)["response"];
                var r2 = (object[])r1;
                var r3 = r2[ApplicationSettings.Default.Car];
                var r4 = ((System.Collections.Generic.Dictionary<string, object>)r3);
                var state = r4["state"].ToString();
                object[] tokens = (object[])r4["tokens"];
                Tesla_Streamingtoken = tokens[0].ToString();

                try
                {
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
                            Tools.Log("Battery: " + battery[0] + " / " + carSettings.Model);
                            carSettings.Battery = battery[0];

                            carSettings.WriteSettings();
                        }
                    }

                    carSettings.Performance = oc.Contains("PX01") || oc.Contains("P85D") || oc.Contains("PX6D") || oc.Contains("X024") | oc.Contains("PBT8") | oc.Contains("PF01");

                    UpdateEfficiency();

                    if (state == "unknown")
                    {
                        Tools.Log("unknown state " + unknownStateCounter);

                        Tools.ExceptionWriter(new Exception("unknown state"), resultContent);

                        if (unknownStateCounter == 0)
                        {
                            string r = Wakeup().Result;
                            Tools.Log("WakupResult: " + r);
                        }
                        else
                            System.Threading.Thread.Sleep(10000);

                        unknownStateCounter++;

                        if (unknownStateCounter == 6)
                            unknownStateCounter = 0;
                    }
                    else
                    {
                        unknownStateCounter = 0;
                    }
                }
                catch (Exception ex)
                {
                    Tools.ExceptionWriter(ex, resultContent);
                }

                return state;
            }
            catch (Exception ex)
            {
                Tools.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        private void UpdateEfficiency()
        {
            string eff = "0.190052356";
            string car = "";

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
                    car = "S 85 ?";
                    eff = "0.200";
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
                    if (carSettings.Performance)
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
                    eff = "0.153"; 
                    car = "M3 LR";
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

            if (carSettings.Name != car || carSettings.Wh_TR != eff)
            {
                carSettings.Name = car;
                carSettings.Wh_TR = eff;
                carSettings.WriteSettings();
            }
        }

        String lastShift_State = "P";

        public bool IsDriving(bool justinsertdb=false)
        {
            string resultContent = "";
            try
            {
                resultContent = GetCommand("drive_state").Result;

                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                var r1 = ((System.Collections.Generic.Dictionary<string, object>)jsonResult)["response"];
                var r2 = (System.Collections.Generic.Dictionary<string, object>)r1;
                decimal dLatitude = (decimal)r2["latitude"];
                decimal dLongitude = (decimal)r2["longitude"];

                double latitude = (double)dLatitude;
                double longitude = (double)dLongitude;
                
                var timestamp = r2["timestamp"].ToString();
                int speed = 0;
                if (r2["speed"] != null)
                    speed = (int)r2["speed"];

                int power = 0;
                if (r2["power"] != null)
                    power = (int)r2["power"];

                var shift_state = "";
                if (r2["shift_state"] != null)
                {
                    shift_state = r2["shift_state"].ToString();
                    lastShift_State = shift_state;
                }
                else
                    shift_state = lastShift_State;

                if (justinsertdb || shift_state == "D" || shift_state == "R" || shift_state == "N")
                {
                    // var address = ReverseGecocodingAsync(latitude, longitude);
                    //var altitude = AltitudeAsync(latitude, longitude);
                    var odometer = GetOdometerAsync();
                    var outside_temp = GetOutsideTempAsync();

                    TimeSpan tsElevation = DateTime.Now - elevation_time;
                    if (tsElevation.TotalSeconds > 30)
                        elevation = "";
                    int battery_level;
                    double ideal_battery_range_km = GetIdealBatteryRangekm(out battery_level);
                    DBHelper.InsertPos(timestamp, latitude, longitude, speed, power, odometer.Result, ideal_battery_range_km, battery_level, outside_temp.Result, elevation);

                    if (shift_state == "D" || shift_state == "R" || shift_state == "N")
                    {
                        lastIsDriveTimestamp = DateTime.Now;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (resultContent == null)
                    Tools.Log("IsDriving: ResultContent=NULL!");
                else 
                    Tools.ExceptionWriter(ex, resultContent);

                if (lastShift_State == "D" || lastShift_State == "R" || lastShift_State == "N")
                {
                    TimeSpan ts = DateTime.Now - lastIsDriveTimestamp;

                    if (ts.TotalMinutes > 10)
                    {
                        Tools.Log("No Valid IsDriving since 10min!");
                        lastShift_State = "P";
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        public void StartStreamThread()
        {
            System.Threading.Thread t = new System.Threading.Thread(() => StartStream());
            t.Start();
        }

        void StartStream()
        {
            Tools.Log("StartStream");
            stopStreaming = false;
            string line = "";
            while (!stopStreaming)
            {
                try
                {
                    string online = IsOnline().Result;

                    using (var client = new HttpClient())
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
                                    // Tools.Log("Elevation: " + values[4]);

                                    elevation = values[4];
                                    elevation_time = DateTime.Now;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // System.Diagnostics.Debug.WriteLine(ex.ToString());
                    
                    Tools.ExceptionWriter(ex, line);
                    System.Threading.Thread.Sleep(10000);
                }
            }

            Tools.Log("StartStream Ende");
        }


        public async Task<double> AltitudeAsync(double latitude, double longitude)
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

                Tools.ExceptionWriter(ex, url + "\r\n" + resultContent);
            }
            return 0;
            */
        }


        public static async Task<string> ReverseGecocodingAsync(double latitude, double longitude)
        {
            string url = "";
            string resultContent = "";
            try
            {
                Address a = null;
                a = geofence.GetPOI(latitude, longitude);
                if (a != null)
                    return a.name;
                Tools.SetThread_enUS();

                WebClient webClient = new WebClient();

                webClient.Headers.Add("User-Agent: TeslaLogger");
                webClient.Encoding = Encoding.UTF8;
                url = "http://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=";
                url += latitude.ToString();
                url += "&lon=";
                url += longitude.ToString();

                resultContent = await webClient.DownloadStringTaskAsync(new Uri(url));
                
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                var r1 = ((System.Collections.Generic.Dictionary<string, object>)jsonResult)["address"];
                var r2 = (System.Collections.Generic.Dictionary<string, object>)r1;
                string postcode = "";
                if (r2.ContainsKey("postcode"))
                    postcode = r2["postcode"].ToString();

                var country_code = r2["country_code"].ToString();

                string road = "";
                if (r2.ContainsKey("road"))
                    road = r2["road"].ToString();

                string city = "";
                if (r2.ContainsKey("city"))
                    city = r2["city"].ToString();
                else if (r2.ContainsKey("village"))
                    city = r2["village"].ToString();
                else if (r2.ContainsKey("town"))
                    city = r2["town"].ToString();

                string house_number = "";
                if (r2.ContainsKey("house_number"))
                    house_number =r2["house_number"].ToString();

                var name = "";
                if (r2.ContainsKey("name") && r2["name"] != null)
                    name = r2["name"].ToString();

                var address29 = "";
                if (r2.ContainsKey("address29") && r2["address29"] != null)
                    address29 = r2["address29"].ToString();


                string adresse = "";

                if (address29.Length > 0)
                    adresse += address29 + ", ";

                if (country_code != "de")
                    adresse += country_code + "-";

                adresse += postcode + " " + city + ", " + road + " " + house_number;

                if (name.Length > 0)
                    adresse += " / " + name;

                System.Diagnostics.Debug.WriteLine(url + "\r\n" + adresse);

                return adresse;
            }
            catch (Exception ex)
            {
                if (url == null)
                    url = "NULL";

                if (resultContent == null)
                    resultContent = "NULL";

                Tools.ExceptionWriter(ex, url + "\r\n"+ resultContent);
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
                    var lat = (double)dr[0];
                    var lng = (double)dr[1];
                    int id = (int)dr[2];
                    var adress = ReverseGecocodingAsync(lat, lng);
                    var altitude = AltitudeAsync(lat, lng);

                    using (SqlConnection con2 = new SqlConnection(DBHelper.DBConnectionstring))
                    {
                        con2.Open();
                        SqlCommand cmd2 = new SqlCommand("update pos set address=@address, altitude=@altitude where id = @id", con2);
                        cmd2.Parameters.AddWithValue("@id", id);
                        cmd2.Parameters.AddWithValue("@address", adress.Result);
                        cmd2.Parameters.AddWithValue("@altitude", altitude.Result);
                        cmd2.ExecuteNonQuery();

                        System.Diagnostics.Debug.WriteLine("id updateed: " + id + " address: " + adress.Result);
                    }
                }
            }
        }

        public void UpdateAllPOIAddresses()
        {
            try
            {
                System.Threading.Thread.Sleep(60 * 1000 * 10); 

                int t = Environment.TickCount;
                int count = 0;
                Tools.Log("UpdateAllPOIAddresses start");

                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("Select lat, lng, id from pos where id in (SELECT Pos FROM chargingstate) or id in (SELECT StartPos FROM drivestate) or id in (SELECT EndPos FROM drivestate)", con);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    int t2 = Environment.TickCount - t;
                    Tools.Log($"UpdateAllPOIAddresses Select {t2}ms");

                    while (dr.Read())
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(100);
                            double lat = (double)dr[0];
                            double lng = (double)dr[1];
                            int id = (int)dr[2];

                            Address a = geofence.GetPOI(lat, lng);
                            if (a == null)
                                continue;

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
                        catch (Exception ex)
                        {
                            Tools.Log(" Exception in UpdateAllPOIAddresses: " + ex.Message);
                        }
                    }
                }

                t = Environment.TickCount - t;
                Tools.Log($"UpdateAllPOIAddresses end {t}ms count:{count}");
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
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
                var r1 = ((System.Collections.Generic.Dictionary<string, object>)jsonResult)["response"];
                var r2 = (System.Collections.Generic.Dictionary<string, object>)r1;

                if (r2["ideal_battery_range"] == null)
                    return -1;

                var ideal_battery_range = (decimal)r2["ideal_battery_range"];

                if (r2["battery_level"] != null)
                {
                    battery_level = Convert.ToInt32(r2["battery_level"]);
                    DBHelper.currentJSON.current_battery_level = battery_level;
                }

                return (double)ideal_battery_range / (double)0.62137;
            }
            catch (Exception ex)
            {
                Tools.ExceptionWriter(ex, resultContent);
            }
            return -1;
        }

        double lastOdometerKM = 0;

        async Task<double> GetOdometerAsync()
        {
            string resultContent = "";
            try
            {
                resultContent = await GetCommand("vehicle_state");
                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                var r1 = ((System.Collections.Generic.Dictionary<string, object>)jsonResult)["response"];
                var r2 = (System.Collections.Generic.Dictionary<string, object>)r1;

                if (r2["odometer"] == null)
                {
                    Tools.Log("odometer = NULL");
                    return lastOdometerKM;
                }

                decimal odometer = (decimal)r2["odometer"];

                try
                {
                    string car_version = r2["car_version"].ToString();
                    if (DBHelper.currentJSON.current_car_version != car_version)
                    {
                        Tools.Log("Car Version: " + car_version);
                        DBHelper.currentJSON.current_car_version = car_version;

                        TaskerWakeupfile(true);
                    }
                }
                catch (Exception ex)
                {
                    Tools.Log(ex.ToString());
                }
                
                decimal odometerKM = odometer / 0.62137M;
                lastOdometerKM = (double)odometerKM;
                return lastOdometerKM;
            }
            catch (Exception ex)
            {
                Tools.ExceptionWriter(ex, resultContent);
                return lastOdometerKM;
            }
            return 0;
        }

        async Task<double?> GetOutsideTempAsync()
        {
            string resultContent = null;
            try
            {
                resultContent = await GetCommand("climate_state");
                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                var r1 = ((System.Collections.Generic.Dictionary<string, object>)jsonResult)["response"];
                var r2 = (System.Collections.Generic.Dictionary<string, object>)r1;

                decimal? outside_temp = null;
                if (r2["outside_temp"] != null)
                {
                    outside_temp = (decimal)r2["outside_temp"];
                    DBHelper.currentJSON.current_outside_temp = (double)outside_temp;
                }
                else
                    return null;

                bool preconditioning = r2["is_preconditioning"] != null && (bool)r2["is_preconditioning"];
                if (preconditioning != is_preconditioning)
                {
                    is_preconditioning = preconditioning;
                    Tools.Log("Preconditioning: " + preconditioning);
                }

                DBHelper.current_is_preconditioning = preconditioning;

                return (double)outside_temp;
            }
            catch (Exception ex)
            {
                if (resultContent == null)
                {
                    Tools.Log("GetOutsideTempAsync: NULL");
                    return 0;   
                } else if (!resultContent.Contains("upstream internal error"))
                    Tools.ExceptionWriter(ex, resultContent);
            }
            return 0;
        }

        public async Task<String> GetCommand(String cmd)
        {
            string resultContent = "";
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                string adresse = apiaddress + "api/1/vehicles/" + Tesla_id + "/data_request/" + cmd;
                var result = await client.GetAsync(adresse);

                resultContent = await result.Content.ReadAsStringAsync();
                
                return resultContent;
            }
            catch (Exception ex)
            {
                Tools.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public async Task<String> PostCommand(String cmd, String data)
        {
            Tools.Log("PostCommand: " + cmd + " - " + data);

            string resultContent = "";
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                string adresse = apiaddress + "api/1/vehicles/" + Tesla_id + "/" + cmd;

                StringContent queryString = new StringContent(data);
                var result = await client.PostAsync(adresse, queryString);

                resultContent = await result.Content.ReadAsStringAsync();

                return resultContent;
            }
            catch (Exception ex)
            {
                Tools.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public async Task<String> Wakeup()
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
                var resultTask = client.GetAsync(adresse);
                HttpResponseMessage result = resultTask.Result;
                resultContent = result.Content.ReadAsStringAsync().Result;

                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                var r1 = ((System.Collections.Generic.Dictionary<string, object>)jsonResult)["response"];
                var r1temp = (System.Collections.Generic.Dictionary<string, object>)r1;
                string OnlineState = r1temp["state"].ToString();
                System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + " : " + OnlineState);
                var r2 = ((System.Collections.Generic.Dictionary<string, object>)r1temp["drive_state"]);

                var latitude = Double.Parse(r2["latitude"].ToString());
                var longitude = Double.Parse(r2["longitude"].ToString());
                var timestamp = r2["timestamp"].ToString();
                int speed = 0;
                if (r2["speed"] != null)
                    speed = (int)r2["speed"];

                int power = 0;
                if (r2["power"] != null)
                    power = (int)r2["power"];

                var shift_state = "";
                if (r2["shift_state"] != null)
                    shift_state = r2["shift_state"].ToString();

                if (shift_state == "D")
                    DBHelper.InsertPos(timestamp, latitude, longitude, speed, power, 0, 0 , 0, 0.0, "0"); // TODO: ODOMETER, ideal battery range, address

                return resultContent;
            }
            catch (Exception ex)
            {
                Tools.ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public DataTable GetEnergyChartData()
        {
            // https://www.energy-charts.de/power/week_2018_46.json
            string resultContent = "";

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "C# App");
            
            var resultTask = client.GetAsync("https://www.energy-charts.de/power/week_2018_46.json");
            HttpResponseMessage result = resultTask.Result;
            resultContent = result.Content.ReadAsStringAsync().Result;

            object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);

            DataTable dt = new DataTable();
            dt.Columns.Add("name");
            dt.Columns.Add("kWh", typeof(decimal));
            dt.Columns.Add("Datum", typeof(DateTime));

            object[] o1 = (object[])jsonResult;
            foreach(object o2 in o1)
            {
                System.Collections.Generic.Dictionary<string, object> o3 = o2 as System.Collections.Generic.Dictionary<string, object>;
                object[] name = o3["key"] as object[];
                System.Collections.Generic.Dictionary<string, object> n2 = name[0] as System.Collections.Generic.Dictionary<string, object>;
                string realname = n2["de"].ToString();

                if (realname.Contains("geplant") || realname.Contains("Prognose"))
                    continue;

                object[] values = o3["values"] as object[];

                decimal lastkWh = 0;
                for (int x = values.Length-1; x >= 0; x--)
                {
                    object[] v2 = values[x] as object[];

                    if (v2[1] != null)
                    {
                        if (v2[1] is decimal)
                            lastkWh = (decimal)v2[1];
                        else if (v2[1] is int)
                            lastkWh = Convert.ToDecimal((int)v2[1]);

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
            Tools.Log("Request StopStreaming");
            stopStreaming = true;
        }

        DateTime lastTaskerWakeupfile = DateTime.Today;

        public bool TaskerWakeupfile(bool force = false)
        {
            try
            {
                TimeSpan ts = DateTime.Now - lastTaskerWakeupfile;

                if (!force && ts.TotalSeconds < 20)
                    return false;

                //Tools.Log("Check Tasker Webservice");

                lastTaskerWakeupfile = DateTime.Now;

                HttpClient client = new HttpClient();

                var d = new Dictionary<string, string>();
                d.Add("t", TaskerHash);
                d.Add("v", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                d.Add("cv", DBHelper.currentJSON.current_car_version);
                d.Add("m", carSettings.Model);
                d.Add("bt", carSettings.Battery);
                d.Add("n", carSettings.Name);
                d.Add("eff", carSettings.Wh_TR);
                d.Add("oc", option_codes);

                var content = new FormUrlEncodedContent(d);
                var query = content.ReadAsStringAsync().Result;

                var resultTask = client.PostAsync("http://teslalogger.de/wakefile.php", content);

                HttpResponseMessage result = resultTask.Result;
                string resultContent = result.Content.ReadAsStringAsync().Result;

                if (resultContent.Contains("wakeupfile"))
                {
                    Tools.Log("TaskerWakeupfile available! [Webservice]" + resultContent.Replace("wakeupfile", ""));
                    return true;
                }
            }
            catch (Exception ex)
            {
                Tools.Log("TaskerWakeupToken Exception: " + ex.Message);
            }
            
            return false;
        }

        public bool DeleteWakeupFile()
        {
            bool ret = false;
            if (TaskerWakeupfile())
                ret = true;

            if (existsWakeupFile)
            {
                Tools.Log("Delete Wakeup file");
                System.IO.File.Delete(FileManager.GetFilePath(TLFilename.WakeupFilename));
                ret = true;
            }

            return ret;
        }

        public bool existsWakeupFile
        {
            get
            {
                return System.IO.File.Exists(FileManager.GetFilePath(TLFilename.WakeupFilename)) || TaskerWakeupfile();
            }
        }
    }
}
