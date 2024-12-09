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
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net;

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
                    if (Tools.UseNearbySuCService())
                    {
                        Work();
                        GetNextSuperchargerToCalculate();
                    }

                    // sleep 10 Minutes
                    Thread.Sleep(600000);
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

                if (car.FleetAPI)
                {
                    if ((car.GetCurrentState() == Car.TeslaState.Charge
                   || car.GetCurrentState() == Car.TeslaState.Drive
                   || car.GetCurrentState() == Car.TeslaState.Online)
                   && car.CurrentJSON.current_falling_asleep == false)
                    {
                        /* nearby charging sites is a paid pricing category: https://developer.tesla.com/docs/fleet-api/endpoints/vehicle-endpoints#nearby-charging-sites
                        string result = string.Empty;
                        try
                        {
                            result = car.webhelper.GetCommand("nearby_charging_sites?detail=true", true).Result;
                            if (result.Equals("NULL"))
                            {
                                continue;
                            }
                            AddSuperchargerStateFleetAPI(result, send);
                            if (send.Count > 0)
                            {
                                ShareSuc(send, false, out _, out _);
                            }
                            send.Clear();
                        }
                        catch (Exception ex)
                        {
                            car.CreateExceptionlessClient(ex).AddObject(result, "ResultContent").Submit();
                            Tools.DebugLog($"NearbySuCService.Work: result {new Tools.JsonFormatter(result).Format()}");
                            Tools.DebugLog("NearbySuCService.Work: Exception", ex);
                        }
                        */
                    }
                    continue;
                }

                // classic API pre-FleetAPI

                if ((car.GetCurrentState() == Car.TeslaState.Charge
                    || car.GetCurrentState() == Car.TeslaState.Drive
                    || car.GetCurrentState() == Car.TeslaState.Online)
                    && car.CurrentJSON.current_falling_asleep == false)
                {
                    string result = string.Empty;
                    try
                    {
                        result = car.webhelper.GetNearbyChargingSitesOwnerAPI();
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

                        dynamic response = jsonResult["response"];
                        if (response == null)
                        {
                            Tools.DebugLog(new Tools.JsonFormatter(result).Format());
                            continue;
                        }

                        dynamic superchargers = response["superchargers"];
                        if (superchargers == null)
                        {
                            Tools.DebugLog(new Tools.JsonFormatter(result).Format());
                            continue;
                        }
                        foreach (dynamic suc in superchargers)
                        {
                            try
                            {
                                AddSuperchargerStateOwnerAPI(suc, send, true);
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
                        if (send.Count > 0)
                        {
                            ShareSuc(send, false, out _, out _);
                        }
                        send.Clear();
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

        private static void AddSuperchargerStateFleetAPI(string result, ArrayList send)
        {
            dynamic jsonResult = JsonConvert.DeserializeObject(result);
            if (jsonResult.ContainsKey("response"))
            {
                dynamic response = jsonResult["response"];
                if (response.ContainsKey("superchargers"))
                {
                    foreach (dynamic suc in response["superchargers"])
                    {
                        //Tools.DebugLog(new Tools.JsonFormatter(suc.ToString()).Format());
                        if (suc.ContainsKey("available_stalls")
                            && suc.ContainsKey("total_stalls")
                            && suc.ContainsKey("name")
                            && suc.ContainsKey("location")
                            && suc["location"].ContainsKey("lat")
                            && suc["location"].ContainsKey("long")
                            )
                        {
                            string name = suc["name"].ToString();
                            name = name.Replace("Tesla Supercharger", "").Trim();
                            bool SuCfound = GetSuperchargerByName(name, out int sucID);
                            double lat = suc["location"]["lat"];
                            double lng = suc["location"]["long"];
                            if (!SuCfound)
                            {
                                // add new entry to supercharger list in DB
                                sucID = AddNewSupercharger(name, lat, lng);
                            }
                            if (int.TryParse(suc["available_stalls"].ToString(), out int available_stalls))
                            {
                                if (int.TryParse(suc["total_stalls"].ToString(), out int total_stalls))
                                {
                                    {
                                        Tools.DebugLog($"SuC: <{name}> <{available_stalls}> <{total_stalls}>");
                                        if (total_stalls > 0)
                                        {
                                            InsertIntoDB(sucID, available_stalls, total_stalls);
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
                                                sendKV.Add("kw", 0);
                                                sendKV.Add("m", "");
                                            }
                                        }
                                    }
                                }
                            }
                        }
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

                        HttpResponseMessage result = client.PostAsync(new Uri("http://teslalogger.de/share_supercharger2.php" + suffix), content).Result;
                        string r = result.Content.ReadAsStringAsync().Result;
                        DBHelper.AddMothershipDataToDB("teslalogger.de/share_supercharger.php", start, (int)result.StatusCode, 0);

                        Tools.DebugLog("ShareSuc: " + Environment.NewLine + r);

                        string[] ret = r.Split('\n');
                        if (ret.Length > 0)
                        {
                            if (ret[0].StartsWith("NEXTSUC:"))
                            {
                                string csv = ret[0].Trim();
                                csv = csv.Substring(csv.IndexOf(":") + 1);
                                var c = csv.Split(',');
                                if (c.Length > 1)
                                {
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

        private static void AddSuperchargerStateOwnerAPI(dynamic suc, ArrayList send, bool insertdb)
        {
            string name = suc["name"].ToString();
            bool SuCfound = GetSuperchargerByName(name, out int sucID);
            dynamic location = suc["location"];
            double lat = location["lat"];
            double lng = location["long"];
            string Message = "";

            string siteType = suc["type"].ToString();
            if (siteType != "supercharger")
            {
                Tools.DebugLog("siteType: " + name + " :" + siteType);
            }

            if (!SuCfound)
            {
                // add new entry to supercharger list in DB

                sucID = AddNewSupercharger(name, lat, lng);
            }

            if (suc.ContainsKey("available_stalls")
                && suc.ContainsKey("total_stalls")
                )
            {
                if (int.TryParse(suc["available_stalls"].ToString(), out int available_stalls)
                        & int.TryParse(suc["total_stalls"].ToString(), out int total_stalls))
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
                            sendKV.Add("kw", "n/a");
                            sendKV.Add("m", Message);

                            if (insertdb)
                            {
                                InsertIntoDB(sucID, available_stalls, total_stalls);
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

        private static void InsertIntoDB(int sucID, int available_stalls, int total_stalls)
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

        void GetNextSuperchargerToCalculate()
        {
            try
            {
                if (Car.Allcars.Count > 0)
                {
                    Car c = Car.Allcars[0];
                    HttpClient client = c.webhelper.httpclient_teslalogger_de;

                    HttpResponseMessage result = client.GetAsync("https://teslalogger.de:8089/GetNextSuperchargerToCalculate").Result;
                    var resultContent = result.Content.ReadAsStringAsync().Result;

                    if (int.TryParse(resultContent, out int trid))
                    {
                        GetGuestAvailability(c, trid);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("GetNextSuperchargerToCalculate: Exception", ex);
            }
        }

        Available GetGuestAvailability(Car car, int trid)
        {
            Available a = new Available();

            string content = "{\"operationName\":\"GetSiteDetails\",\"query\":\"\\n    query GetSiteDetails($siteId: SiteIdInput!) {\\n  chargingNetwork {\\n    site(siteId: $siteId) {\\n      address {\\n        countryCode\\n      }\\n      chargerList {\\n        id\\n        label\\n        availability\\n      }\\n      holdAmount {\\n        amount\\n        currencyCode\\n      }\\n      maxPowerKw\\n      name\\n      programType\\n      publicStallCount\\n      trtId\\n      pricing {\\n        userRates {\\n          activePricebook {\\n            charging {\\n              ...ChargingRate\\n            }\\n            parking {\\n              ...ChargingRate\\n            }\\n            congestion {\\n              ...ChargingRate\\n            }\\n          }\\n        }\\n      }\\n    }\\n  }\\n}\\n    \\n    fragment ChargingRate on ChargingUserRate {\\n  uom\\n  rates\\n  buckets {\\n    start\\n    end\\n  }\\n  bucketUom\\n  currencyCode\\n  programType\\n  vehicleMakeType\\n  touRates {\\n    enabled\\n    activeRatesByTime {\\n      startTime\\n      endTime\\n      rates\\n    }\\n  }\\n}\\n    \",\"variables\":{\"siteId\":{\"byTrtId\":{\"trtId\":$SITEID$,\"programType\":\"PTSCH\",\"chargingExperience\":\"ADHOC\",\"locale\":\"en-US\"}}}}";
            content = content.Replace("$SITEID$", trid.ToString());

            var client = TeslaGuestHttpClient();

            using (var request = new HttpRequestMessage())
            {
                using (var scontent = new StringContent(content, Encoding.UTF8, "application/json"))
                {
                    string r = "";
                    try
                    {
                        var result = client.PostAsync("https://www.tesla.com/charging/guest/api/graphql?operationName=GetSiteDetails", scontent).Result;
                        r = result.Content.ReadAsStringAsync().Result;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                        throw;
                    }

                    dynamic j = JsonConvert.DeserializeObject(r);
                    if (j?["data"] == null || j["errors"] != null)
                    {
                        Tools.DebugLog($"Unexpected GuestAPI response: {r}");
                        return a;
                    }

                    dynamic site = j["data"]["chargingNetwork"]["site"];
                    JArray chargers = site["chargerList"];
                    //JArray chargerDetails = site["chargersAvailable"]["chargerDetails"];

                    string name = site["name"];

                    a.total = chargers.Count;

                    foreach (dynamic charger in chargers)
                    {
                        string id = charger["id"];
                        string label = charger["label"];


                        string availability = charger["availability"];
                        // System.Diagnostics.Debug.WriteLine($"Label: {label} availability: {availability}");

                        if (availability == "CHARGER_AVAILABILITY_AVAILABLE")
                            a.available++;
                        else if (availability == "CHARGER_AVAILABILITY_OCCUPIED")
                            a.occupied++;
                        else if (availability == "CHARGER_AVAILABILITY_DOWN")
                            a.down++;
                        else if (availability == "CHARGER_AVAILABILITY_UNKNOWN")
                            a.unknown++;
                        else
                            a.unhandled++;

                    }

                    if (!string.IsNullOrEmpty(name))
                    {
                        Tools.DebugLog($"#{car.CarInDB} Guest SuC: <{name}> <{a.available}> <{a.total}>");

                        ArrayList send = new ArrayList();
                        Dictionary<string, object> sendKV = new Dictionary<string, object>();
                        send.Add(sendKV);
                        sendKV.Add("n", name);
                        // sendKV.Add("lat", lat);
                        // sendKV.Add("lng", lng);
                        sendKV.Add("ts", DateTime.UtcNow.ToString("s", Tools.ciEnUS));
                        sendKV.Add("a", a.available);
                        sendKV.Add("t", a.total);
                        sendKV.Add("kw", 0);
                        sendKV.Add("m", "");
                        ShareSuc(send, false, out _, out _);
                    }
                }
            }

            return a;
        }
        /*
        Available GetGuestAvailabilityOld(Car c, int trid)
        {
            Available a = new Available();

            string content = "{\"variables\":{\"identifier\":{\"siteId\":{\"id\":$SITEID$,\"siteType\":\"SITE_TYPE_SUPERCHARGER\"}},\"experience\":\"GUEST\",\"deviceLocale\":\"de-DE\"},\"operationName\":\"getGuestChargingSiteDetails\",\"query\":\"\\n    query getGuestChargingSiteDetails($identifier: ChargingSiteIdentifierInput!, $deviceLocale: String!, $experience: ChargingExperienceEnum!) {\\n  site(\\n    identifier: $identifier\\n    deviceLocale: $deviceLocale\\n    experience: $experience\\n  ) {\\n    activeOutages\\n    address {\\n      countryCode\\n    }\\n    chargers {\\n      id\\n      label\\n    }\\n    chargersAvailable {\\n      chargerDetails {\\n        id\\n        availability\\n      }\\n    }\\n    holdAmount {\\n      holdAmount\\n      currencyCode\\n    }\\n    maxPowerKw\\n    name\\n    programType\\n    publicStallCount\\n    id\\n    pricing(experience: $experience) {\\n      userRates {\\n        activePricebook {\\n          charging {\\n            uom\\n            rates\\n            buckets {\\n              start\\n              end\\n            }\\n            bucketUom\\n            currencyCode\\n            programType\\n            vehicleMakeType\\n            touRates {\\n              enabled\\n              activeRatesByTime {\\n                startTime\\n                endTime\\n                rates\\n              }\\n            }\\n          }\\n          parking {\\n            uom\\n            rates\\n            buckets {\\n              start\\n              end\\n            }\\n            bucketUom\\n            currencyCode\\n            programType\\n            vehicleMakeType\\n            touRates {\\n              enabled\\n              activeRatesByTime {\\n                startTime\\n                endTime\\n                rates\\n              }\\n            }\\n          }\\n          congestion {\\n            uom\\n            rates\\n            buckets {\\n              start\\n              end\\n            }\\n            bucketUom\\n            currencyCode\\n            programType\\n            vehicleMakeType\\n            touRates {\\n              enabled\\n              activeRatesByTime {\\n                startTime\\n                endTime\\n                rates\\n              }\\n            }\\n          }\\n        }\\n      }\\n    }\\n  }\\n}\\n    \"}";
            content = content.Replace("$SITEID$", trid.ToString());

            var client = TeslaGuestHttpClient();

            using (var request = new HttpRequestMessage())
            {
                using (var scontent = new StringContent(content, Encoding.UTF8, "application/json"))
                {
                    var result = client.PostAsync("https://www.tesla.com/de_DE/charging/guest/api/graphql?operationName=getGuestChargingSiteDetails", scontent).Result;
                    string r = result.Content.ReadAsStringAsync().Result;

                    dynamic j = JsonConvert.DeserializeObject(r);
                    if (j?["data"] == null || j["errors"] != null)
                    {
                        Tools.DebugLog($"Unexpected GuestAPI response: {r}");
                        return a;
                    }

                    dynamic site = j["data"]["site"];
                    JArray chargers = site["chargers"];
                    JArray chargerDetails = site["chargersAvailable"]["chargerDetails"];

                    string name = site["name"];

                    a.total = chargers.Count;

                    foreach (dynamic charger in chargers)
                    {
                        string id = charger["id"];
                        string label = charger["label"];

                        foreach (dynamic item in chargerDetails)
                        {
                            if (item["id"] == id)
                            {
                                string availability = item["availability"];
                                // System.Diagnostics.Debug.WriteLine($"Label: {label} availability: {availability}");

                                if (availability == "CHARGER_AVAILABILITY_AVAILABLE")
                                    a.available++;
                                else if (availability == "CHARGER_AVAILABILITY_OCCUPIED")
                                    a.occupied++;
                                else if (availability == "CHARGER_AVAILABILITY_DOWN")
                                    a.down++;
                                else if (availability == "CHARGER_AVAILABILITY_UNKNOWN")
                                    a.unknown++;
                                else
                                    a.unhandled++;

                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(name))
                    {
                        Tools.DebugLog($"Guest SuC: <{name}> <{a.available}> <{a.total}>");

                        ArrayList send = new ArrayList();
                        Dictionary<string, object> sendKV = new Dictionary<string, object>();
                        send.Add(sendKV);
                        sendKV.Add("n", name);
                        // sendKV.Add("lat", lat);
                        // sendKV.Add("lng", lng);
                        sendKV.Add("ts", DateTime.UtcNow.ToString("s", Tools.ciEnUS));
                        sendKV.Add("a", a.available);
                        sendKV.Add("t", a.total);
                        sendKV.Add("kw", 0);
                        sendKV.Add("m", "");
                        ShareSuc(send, false, out _, out _);
                    }
                }
            }

            return a;
        }
        */

        public class Available
        {
            public int available = 0;
            public int occupied = 0;
            public int unhandled = 0;
            public int total = 0;
            public int down = 0;
            public int unknown = 0;
        }

        static HttpClient _teslaGuestHttpClient;
        static HttpClient TeslaGuestHttpClient()
        {
            if (_teslaGuestHttpClient == null)
            {
                var ch = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer(),
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    AllowAutoRedirect = true,
                    UseCookies = false
                };

                var client = new HttpClient(ch)
                {
                    BaseAddress = new Uri("https://www.tesla.com"),
                    DefaultRequestHeaders =
                {
                    ConnectionClose = true,
                    Accept = { new MediaTypeWithQualityHeaderValue("application/json") },
                    }
                };

                client.DefaultRequestHeaders.Add("accept", "application/json; charset=UTF-8");
                client.DefaultRequestHeaders.Add("accept-language", "de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7,es-ES;q=0.6,es;q=0.5,zh-CN;q=0.4,zh;q=0.3,fr;q=0.2");
                // client.DefaultRequestHeaders.Add("cache-control", "no-cache");
                // client.DefaultRequestHeaders.Add("pragma", "no-cache");
                // client.DefaultRequestHeaders.Add("upgrade-insecure-requests", "1");
                client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                // client.DefaultRequestHeaders.Add("x-tesla-user-agent", "TeslaApp/4.19.5-1667/3a5d531cc3/android/27");

                client.Timeout = TimeSpan.FromSeconds(20);

                _teslaGuestHttpClient = client;
            }

            return _teslaGuestHttpClient;
        }
    }
}