using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;

using MySql.Data.MySqlClient;
using Exceptionless;
using Newtonsoft.Json;
using System.Runtime.Caching;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    public class NearbySuCService
    {
        private static NearbySuCService _NearbySuCService;

        private NearbySuCService()
        {
            Logfile.Log("NearbySuCService initialized");
        }

        public static NearbySuCService GetSingleton()
        {
            if (_NearbySuCService == null)
            {
                _NearbySuCService = new NearbySuCService();
            }
            return _NearbySuCService;
        }

        public void Run()
        {
            // initially sleep 30 seconds to let the cars get from Start to Online
            Thread.Sleep(30000);
            try
            {
                while (true)
                {
                    Work();
                    // sleep 5 Minutes
                    Thread.Sleep(300000);
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("NearbySuCService: Exception", ex);
            }
        }

        private void Work()
        {
            ArrayList send = new ArrayList();

            for (int id = 0; id < Car.Allcars.Count; id++)
            {
                Car car = Car.Allcars[id];
                if (car.IsInService())
                    continue;

                if ((car.GetCurrentState() == Car.TeslaState.Charge
                    || car.GetCurrentState() == Car.TeslaState.Drive
                    || car.GetCurrentState() == Car.TeslaState.Online)
                    && car.CurrentJSON.current_falling_asleep == false)
                {
                    string result = string.Empty;
                    try
                    {
                        result = car.webhelper.GetNearbyChargingSites().Result;
                        if (result == null || result == "NULL")
                            continue;

                        if (result.Contains("Retry later"))
                        {
                            Tools.DebugLog("NearbySuCService: Retry later");
                            return;
                        }
                        else if (result.Contains("vehicle unavailable"))
                        {
                            Tools.DebugLog("NearbySuCService: vehicle unavailable");
                            return;
                        }
                        else if (result.Contains("502 Bad Gateway"))
                        {
                            Tools.DebugLog("NearbySuCService: 502 Bad Gateway");
                            return;
                        }

                        dynamic jsonResult = JsonConvert.DeserializeObject(result);
                        if (jsonResult == null)
                            continue;

                        if (jsonResult.ContainsKey("data"))
                        {
                            dynamic data = jsonResult["data"];
                            if (data == null)
                                continue;

                            if (data.ContainsKey("charging"))
                            {
                                dynamic charging = data["charging"];
                                if (charging == null)
                                    continue;

                                if (charging.ContainsKey("nearbySites"))
                                {
                                    dynamic nearbySites = charging["nearbySites"];
                                    if (nearbySites == null)
                                        continue;

                                    if (nearbySites.ContainsKey("sitesAndDistances"))
                                    {
                                        dynamic superchargers = nearbySites["sitesAndDistances"];
                                        foreach (dynamic suc in superchargers)
                                        {
                                            /*
                  {
                    "location": { "lat": 33.848756, "long": -84.36434 },
                    "name": "Atlanta, GA - Peachtree Road",
                    "type": "supercharger",
                    "distance_miles": 10.868304,
                    "available_stalls": 4,
                    "total_stalls": 5,
                    "site_closed": false
                  }
                                             */

                                            try
                                            {
                                                AddSuperchargerState(suc, send, result);
                                            }
                                            catch (Exception ex)
                                            {
                                                car.CreateExceptionlessClient(ex).AddObject(result, "ResultContent").Submit();
                                                Logfile.Log(ex.ToString());
                                            }
                                        }
                                    }
                                }
                            }

                            if (send.Count > 0)
                                ShareSuc(send);
                        }
                        Thread.Sleep(30000);
                    }
                    catch (Exception ex)
                    {
                        car.CreateExceptionlessClient(ex).AddObject(result, "ResultContent").Submit();
                        Tools.DebugLog($"NearbySuCService.Work: result {new Tools.JsonFormatter(result).Format()}");
                        Tools.DebugLog("NearbySuCService.Work: Exception", ex);
                    }
                }
            }
        }

        private static void ShareSuc(ArrayList send)
        {
            try
            {
                string json = JsonConvert.SerializeObject(send);

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    using (StringContent content = new StringContent(json, Encoding.UTF8, "application/json"))
                    {
                        DateTime start = DateTime.UtcNow;
                        HttpResponseMessage result = client.PostAsync(new Uri("http://teslalogger.de/share_supercharger.php"), content).Result;
                        string r = result.Content.ReadAsStringAsync().Result;
                        DBHelper.AddMothershipDataToDB("teslalogger.de/share_supercharger.php", start, (int)result.StatusCode);

                        Tools.DebugLog("ShareSuc: " + Environment.NewLine + r);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("ShareSuc: " + ex.Message);
            }
        }

        private static void AddSuperchargerState(Newtonsoft.Json.Linq.JObject suc, ArrayList send, string resultContent)
        {
            int sucID = int.MinValue;
            string name = suc["localizedSiteName"]["value"].ToString();
            name = name.Replace("Tesla Supercharger", "").Trim();
            bool SuCfound = GetSuperchargerByName(name, out sucID);
            dynamic location = suc["centroid"];
            double lat = location["latitude"];
            double lng = location["longitude"];
            string Message = "";

            string siteType = suc["siteType"].ToString();
            if (siteType != "SITE_TYPE_SUPERCHARGER")
            {
                System.Diagnostics.Debug.WriteLine("siteType: " + name +" :" + siteType);
            }

            string accessType = suc["accessType"].ToString();
            if (accessType != "ACCESS_TYPE_PUBLIC")
            {
                System.Diagnostics.Debug.WriteLine("accessType: " + name + " :" + accessType);
            }

            int maxPowerKw = suc["maxPowerKw"]["value"].ToObject<int>();

            dynamic activeOutages = suc["activeOutages"];
            int activeOutageCount = activeOutages.Count;
            if (activeOutageCount > 0)
            {
                System.Diagnostics.Debug.WriteLine("Outage: " + name);
                foreach (dynamic ao in activeOutages)
                {
                    if (ao.ContainsKey("message"))
                    {
                        if (Message.Length > 0)
                            Message += "|";

                        Message += ao["message"].ToString();
                    }
                }

                System.Diagnostics.Debug.WriteLine("Message: " + Message);

                string cacheKey = "SuperchargerStateOutages_" + name;
                object cacheValue = MemoryCache.Default.Get(cacheKey);
                if (cacheValue == null)
                {
                    string base64 = Tools.ConvertString2Base64(resultContent);

                    ExceptionlessClient.Default.CreateLog("SuperchargerStateOutages", name + " " + Message, Exceptionless.Logging.LogLevel.Info)
                        .FirstCarUserID()
                        .AddObject(resultContent, "ResultContent")
                        .AddObject(base64, "ResultContentBase64").Submit();

                    MemoryCache.Default.Add(cacheKey, true, DateTime.Now.AddHours(1));
                }
            }

            if (!SuCfound)
            {
                // add new entry to supercharger list in DB

                sucID = AddNewSupercharger(name, lat, lng);
            }

            if (suc.ContainsKey("availableStalls")
                && suc.ContainsKey("totalStalls")
                )
            {
                if (int.TryParse(suc["availableStalls"]["value"].ToString(), out int available_stalls)
                    && int.TryParse(suc["totalStalls"]["value"].ToString(), out int total_stalls))
                {
                    Tools.DebugLog($"SuC: <{name}> <{available_stalls}> <{total_stalls}>");

                    if (total_stalls > 0)
                    {
                        if (!ContainsSupercharger(send, name))
                        {
                            Dictionary<string, object> sendKV = new Dictionary<string, object>();
                            send.Add(sendKV);
                            sendKV.Add("n", name);
                            sendKV.Add("lat", lat);
                            sendKV.Add("lng", lng);
                            sendKV.Add("ts", DateTime.UtcNow.ToString("s", Tools.ciEnUS));
                            sendKV.Add("a", available_stalls);
                            sendKV.Add("t", total_stalls);
                            sendKV.Add("kw", maxPowerKw);
                            sendKV.Add("m", Message);

                            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                            {
                                con.Open();
                                // find internal ID of supercharger by name
                                using (MySqlCommand cmd = new MySqlCommand(@"
INSERT
    superchargerstate(
        nameid,
        ts,
        available_stalls,
        total_stalls
    )
VALUES(
    @nameid,
    @ts,
    @available_stalls,
    @total_stalls
)", con))
                                {
                                    cmd.Parameters.AddWithValue("@nameid", sucID);
                                    cmd.Parameters.AddWithValue("@ts", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@available_stalls", available_stalls);
                                    cmd.Parameters.AddWithValue("@total_stalls", total_stalls);
                                    SQLTracer.TraceNQ(cmd);
                                }
                                con.Close();
                            }
                        }
                    }
                    else
                    {
                        // TODO how do we handle total_stalls == 0 ?
                    }
                }
            }
            /*
            else if (suc.ContainsKey("site_closed")
                && bool.TryParse(suc["site_closed"].ToString(), out site_closed)
                && site_closed)
            {
                Tools.DebugLog($"SuC: <{suc["name"]}> site_closed");
                if (!ContainsSupercharger(send, suc["name"].ToString()))
                {
                    Dictionary<string, object> sendKV = new Dictionary<string, object>();
                    send.Add(sendKV);
                    sendKV.Add("n", suc["name"]);
                    sendKV.Add("lat", lat);
                    sendKV.Add("lng", lng);
                    sendKV.Add("ts", DateTime.UtcNow.ToString("s", Tools.ciEnUS));
                    sendKV.Add("a", -1);
                    sendKV.Add("t", -1);
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
                        // find internal ID of supercharger by name
                        using (MySqlCommand cmd = new MySqlCommand("INSERT superchargerstate (nameid, ts, available_stalls, total_stalls) values (@nameid, @ts, @available_stalls, @total_stalls) ", con))
                        {
                            cmd.Parameters.AddWithValue("@nameid", sucID);
                            cmd.Parameters.AddWithValue("@ts", DateTime.Now);
                            cmd.Parameters.AddWithValue("@available_stalls", -1);
                            cmd.Parameters.AddWithValue("@total_stalls", -1);
                            SQLTracer.TraceNQ(cmd);
                        }
                        con.Close();
                    }
                }
            }
            else if (suc.ContainsKey("site_closed")
                && bool.TryParse(suc["site_closed"].ToString(), out site_closed)
                && !site_closed)
            {
                Tools.DebugLog($"SuC: <{suc["name"]}> no info (fields available: available_stalls {suc.ContainsKey("available_stalls")} total_stalls {suc.ContainsKey("available_stalls")})");
                Tools.DebugLog(new Tools.JsonFormatter(JsonConvert.SerializeObject(suc)).Format());
            }
            else
            {
                Tools.DebugLog($"suc ContainsKey available_stalls {suc.ContainsKey("available_stalls")} total_stalls {suc.ContainsKey("available_stalls")} site_closed {suc.ContainsKey("site_closed")}");
            }
            */

        }

        private static bool ContainsSupercharger(ArrayList send, string name)
        {
            foreach (object a in send)
            {
                Dictionary<string, object> b = a as Dictionary<string, object>;

                if (b?["n"].ToString() == name)
                    return true;
            }

            return false;
        }

        private static int AddNewSupercharger(string name, double lat, double lng)
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                // find internal ID of supercharger by name
                using (MySqlCommand cmd = new MySqlCommand("INSERT superchargers (name, lat, lng) values (@name, @lat, @lng) ", con))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@lat", lat);
                    cmd.Parameters.AddWithValue("@lng", lng);
                    SQLTracer.TraceNQ(cmd);
                }
                con.Close();
            }
            GetSuperchargerByName(name, out int sucID);
            return sucID;
        }

        private static bool GetSuperchargerByName(string suc, out int sucID)
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                // find internal ID of supercharger by name
                using (MySqlCommand cmd = new MySqlCommand("SELECT id from superchargers where name = @name", con))
                {
                    cmd.Parameters.AddWithValue("@name", suc);
                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    if (dr.Read() && dr[0] != DBNull.Value)
                    {
                        if (int.TryParse(dr[0].ToString(), out sucID))
                        {
                            return true;
                        }
                    }
                }
            }
            sucID = int.MinValue;
            return false;
        }
    }
}