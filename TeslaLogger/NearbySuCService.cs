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
using System.Linq;
using Microsoft.VisualBasic.Logging;

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
                //if (car.IsInService())
                //    continue;

                if ((car.GetCurrentState() == Car.TeslaState.Charge
                    || car.GetCurrentState() == Car.TeslaState.Drive
                    || car.GetCurrentState() == Car.TeslaState.Online)
                    && car.CurrentJSON.current_falling_asleep == false)
                {
                    string result = string.Empty;
                    try
                    {
                        bool logSent = true;
                        double requestlat = 0; ;
                        double requestlng = 0;

                        for (int stage = 0; stage < 2; stage++)
                        {
                            double lat = 0;
                            double lng = 0;

                            if (stage == 0)
                            {
                                lat = car.CurrentJSON.GetLatitude();
                                lng = car.CurrentJSON.GetLongitude();
                            }
                            else if (stage == 1) {
                                lat = requestlat;
                                lng = requestlng;
                            }

                            if (lat == 0 ||lng == 0)
                            {
                                continue;
                            }

                            /*
                            for (double lat = 20; lat < 55; lat += 4)
                                for(double lng = -124; lng < -61; lng += 4)
                            */
                            {
                                result = car.webhelper.GetNearbyChargingSites(lat, lng).Result;
                                if (result == null || result == "NULL")
                                {
                                    continue;
                                }

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
                                {
                                    continue;
                                }

                                if (jsonResult.ContainsKey("data"))
                                {
                                    dynamic data = jsonResult["data"];
                                    if (data == null)
                                    {
                                        continue;
                                    }

                                    if (data.ContainsKey("charging"))
                                    {
                                        dynamic charging = data["charging"];
                                        if (charging == null)
                                        {
                                            continue;
                                        }

                                        if (charging.ContainsKey("nearbySites"))
                                        {
                                            dynamic nearbySites = charging["nearbySites"];
                                            if (nearbySites == null)
                                            {
                                                continue;
                                            }

                                            if (nearbySites.ContainsKey("sitesAndDistances"))
                                            {
                                                car.webhelper.nearbySuCServiceOK++;
                                                car.Log("nearbySuCServiceOK " + car.webhelper.nearbySuCServiceOK);
                                                //Tools.DebugLog(new Tools.JsonFormatter(nearbySites.ToString()).Format());

                                                dynamic superchargers = nearbySites["sitesAndDistances"];
                                                if (superchargers == null)
                                                {
                                                    continue;
                                                }
                                                foreach (dynamic suc in superchargers)
                                                {
                                                    try
                                                    {
                                                        AddSuperchargerState(suc, send, result, stage == 0);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        car.CreateExceptionlessClient(ex).AddObject(result, "ResultContent").Submit();
                                                        Logfile.Log(ex.ToString());
                                                        if (ex is InvalidOperationException)
                                                        {
                                                            Tools.DebugLog("NearbySuCService.Work: Exception parsing " + result);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    if (logSent)
                                    {
                                        string firstname = "";
                                        try
                                        {
                                            if (send.Count > 0)
                                            {
                                                dynamic d = send[0];
                                                firstname = d["n"];
                                            }
                                        }
                                        catch (Exception ex)
                                        { Tools.DebugLog(ex.ToString()); }

                                        if (stage == 0)
                                            car.Log("SuC sent: " + send.Count + " lat:" + lat + " lng: " + lng + " - " + firstname);
                                        else
                                            car.Log("Request from teslalogger.de: " + send.Count + " lat:" + lat + " lng: " + lng + " - " + firstname);
                                    }

                                    if (send.Count > 0)
                                        ShareSuc(send, stage==0, out requestlat, out requestlng);

                                    send.Clear();
                                }

                                Thread.Sleep(1000);
                            }
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

        private static void ShareSuc(ArrayList send, bool nextsuc, out double requestlat, out double requestlng)
        {
            requestlat = 0;
            requestlng = 0;

            try
            {
                string json = JsonConvert.SerializeObject(send);

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    using (StringContent content = new StringContent(json, Encoding.UTF8, "application/json"))
                    {
                        DateTime start = DateTime.UtcNow;
                        string suffix = "";
                        if (nextsuc)
                            suffix = "?nextsuc=1";

                        HttpResponseMessage result = client.PostAsync(new Uri("http://teslalogger.de/share_supercharger2.php"+ suffix), content).Result;
                        string r = result.Content.ReadAsStringAsync().Result;
                        DBHelper.AddMothershipDataToDB("teslalogger.de/share_supercharger.php", start, (int)result.StatusCode);

                        Tools.DebugLog("ShareSuc: " + Environment.NewLine + r);

                        string[] ret = r.Split('\n');
                        if (ret.Length > 0) { 
                            if (ret[0].StartsWith("NEXTSUC:"))
                            {
                                string csv = ret[0].Trim();
                                csv = csv.Substring(csv.IndexOf(":") + 1);
                                var c = csv.Split(',');
                                if (c.Length > 1) { 
                                    requestlat = Convert.ToDouble(c[0], Tools.ciEnUS);
                                    requestlng = Convert.ToDouble(c[1], Tools.ciEnUS);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("ShareSuc: " + ex.Message);
            }
        }

        private static void AddSuperchargerState(Newtonsoft.Json.Linq.JObject suc, ArrayList send, string resultContent, bool insertdb)
        {
            /* suc:
             {
    "activeOutages":
    [
    ],
    "availableStalls":
    {
        "value": 8
    },
    "centroid":
    {
        "latitude": 38.922231,
        "longitude": -6.375038
    },
    "drivingDistanceMiles": null,
    "entryPoint":
    {
        "latitude": 38.922409,
        "longitude": -6.375284
    },
    "haversineDistanceMiles":
    {
        "value": 117.37894565724743
    },
    "id":
    {
        "text": "87e6ef6e-9f25-474c-a164-ba185b970f4d"
    },
    "localizedSiteName":
    {
        "value": "Merida, Spain"
    },
    "maxPowerKw":
    {
        "value": 150
    },
    "totalStalls":
    {
        "value": 10
    },
    "siteType": "SITE_TYPE_SUPERCHARGER",
    "accessType": "ACCESS_TYPE_PUBLIC"
} 
             */
            //Tools.DebugLog(new Tools.JsonFormatter(suc.ToString()).Format());
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
                Tools.DebugLog("siteType: " + name +" :" + siteType);
            }

            string accessType = suc["accessType"].ToString();
            if (accessType != "ACCESS_TYPE_PUBLIC")
            {
                Tools.DebugLog("accessType: " + name + " :" + accessType);
            }

            int maxPowerKw = suc["maxPowerKw"]["value"].ToObject<int>();

            dynamic activeOutages = suc["activeOutages"];
            int activeOutageCount = activeOutages.Count;
            if (activeOutageCount > 0)
            {
                Tools.DebugLog("Outage: " + name);
                foreach (dynamic ao in activeOutages)
                {
                    if (ao.ContainsKey("message"))
                    {
                        if (Message.Length > 0)
                            Message += "|";

                        Message += ao["message"].ToString();
                    }
                }

                Tools.DebugLog("Message: " + Message);

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
                if (!suc["availableStalls"].HasValues)
                {
                    Logfile.Log($"SUC: {name} has no values at availableStalls");
                    return;
                }

                if (!suc["totalStalls"].HasValues)
                {
                    Logfile.Log($"SUC: {name} has no values at totalStalls");
                    return;
                }

                if (suc["availableStalls"].HasValues
                    && suc["totalStalls"].HasValues)
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

                                if (insertdb)
                                {
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
                                            _ = SQLTracer.TraceNQ(cmd, out _);
                                        }
                                        con.Close();
                                    }
                                }
                            }
                        }
                        else
                        {
                            // TODO how do we handle total_stalls == 0 ?
                        }
                    }
                }
                else
                {
                    Logfile.Log("Supercharger " + name + " doesn't contain availableStalls.value or totalStalls.value");
                }
            }
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
                    _ = SQLTracer.TraceNQ(cmd, out _);
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