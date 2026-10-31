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
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Net;
using TeslaLoggerNET8.Lucid;
using System.Threading.Tasks;

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

        public async Task Run()
        {
            // initially sleep 30 seconds to let the cars get from Start to Online
            await Task.Delay(30000);
            try
            {
                while (true)
                {
                    if (Tools.UseNearbySuCService())
                    {
                        await WorkAsync();
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

        private async Task WorkAsync()
        {
            ArrayList send = new ArrayList();

            for (int id = 0; id < Car.Allcars.Count; id++)
            {
                Car car = Car.Allcars[id];
                //if (car.IsInService())
                //    continue;

                if (car is LucidCar)
                {
                    Thread.Sleep(100);
                    continue;
                }    

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

                        HttpResponseMessage result = client.PostAsync(new Uri("http://teslalogger.de/share_supercharger2.php" + suffix), content).Result    ;
                        string r = result.Content.ReadAsStringAsync().Result;
                        _ = DBHelper.AddMothershipDataToDBAsync("teslalogger.de/share_supercharger.php", start, (int)result.StatusCode, 0);

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

        const string GuestSiteUrl = "https://www.tesla.com/de_de/charging/guest/site/{0}?type=sc";
        const string GuestGraphQlUrl = "https://www.tesla.com/de_DE/charging/guest/api/graphql?operationName=GetSiteDetails";
        const string GuestGraphQlUrlFallback = "https://www.tesla.com/charging/guest/api/graphql?operationName=GetSiteDetails";

        internal Available GetGuestAvailability(Car car, int trid)
        {
            Available a = new Available();

            string payload = "{\"operationName\":\"GetSiteDetails\",\"query\":\"\\n    query GetSiteDetails($siteId: SiteIdInput!) {\\n  chargingNetwork {\\n    site(siteId: $siteId) {\\n      address {\\n        countryCode\\n      }\\n      chargerList {\\n        id\\n        label\\n        availability\\n      }\\n      holdAmount {\\n        amount\\n        currencyCode\\n      }\\n      maxPowerKw\\n      name\\n      programType\\n      publicStallCount\\n      trtId\\n      locationGUID\\n      pricing {\\n        userRates {\\n          activePricebook {\\n            charging {\\n              ...ChargingRate\\n            }\\n            parking {\\n              ...ChargingRate\\n            }\\n            congestion {\\n              ...ChargingRate\\n            }\\n          }\\n        }\\n      }\\n    }\\n  }\\n}\\n    \\n    fragment ChargingRate on ChargingUserRate {\\n  uom\\n  rates\\n  buckets {\\n    start\\n    end\\n  }\\n  bucketUom\\n  currencyCode\\n  programType\\n  vehicleMakeType\\n  touRates {\\n    enabled\\n    activeRatesByTime {\\n      startTime\\n      endTime\\n      rates\\n    }\\n  }\\n}\\n    \",\"variables\":{\"siteId\":{\"byTrtId\":{\"trtId\":$SITEID$,\"programType\":\"PTSCH\",\"chargingExperience\":\"ADHOC\",\"locale\":\"de-DE\"}}}}";
            payload = payload.Replace("$SITEID$", trid.ToString());

            if (IsGuestApiBlocked(out DateTime blockedUntil))
            {
                Tools.DebugLog($"GuestAPI blocked until {blockedUntil:u}, using OwnerAPI for trtId {trid}");
                Available owner = GetGuestAvailabilityOwnerApi(car, trid, out string ownerError);
                if (owner != null)
                    return owner;

                Tools.DebugLog($"OwnerAPI fallback failed for trtId {trid}: {ownerError}");
                ReportOwnerFallbackError(car, trid, ownerError);
                return a;
            }

            string content = PostGuestGraphQl(trid, payload, GuestGraphQlUrl, out string primaryError, out bool primaryTransient);
            if (content == null && !primaryTransient)
            {
                content = PostGuestGraphQl(trid, payload, GuestGraphQlUrlFallback, out string fallbackError, out bool fallbackTransient);
                primaryError = $"{primaryError} | Fallback: {fallbackError}";
                primaryTransient = fallbackTransient;
            }

            if (content == null)
            {
                if (primaryTransient)
                    RegisterGuestApiBlock();

                if (primaryTransient)
                {
                    Available owner = GetGuestAvailabilityOwnerApi(car, trid, out string ownerError);
                    if (owner != null)
                        return owner;

                    primaryError = $"{primaryError} | OwnerAPI: {ownerError}";
                }

                string message = $"GetGuestAvailability failed for trtId {trid}: {primaryError}";
                Tools.DebugLog(message);
                car?.CreateExeptionlessLog("Guest SuC", message, Exceptionless.Logging.LogLevel.Error).Submit();
                return a;
            }

            ResetGuestApiBlock();

            dynamic j = JsonConvert.DeserializeObject(content);
            if (j?["data"] == null || j["errors"] != null)
            {
                string message = $"Unexpected GuestAPI response for trtId {trid}: {content}";
                Tools.DebugLog(message);
                car?.CreateExeptionlessLog("Guest SuC", message, Exceptionless.Logging.LogLevel.Warn).Submit();
                return a;
            }

            dynamic site = j["data"]["chargingNetwork"]["site"];
            JArray chargers = site["chargerList"];

            string name = site["name"];
            CacheTridLocationGuid(trid, site["locationGUID"]?.ToString());

            a.total = chargers.Count;

            foreach (dynamic charger in chargers)
            {
                string availability = charger["availability"];
                CountAvailability(availability, a);
            }

            ShareSucAvailability(car, name, a);

            return a;
        }

        static void ShareSucAvailability(Car car, string name, Available a)
        {
            if (string.IsNullOrEmpty(name))
                return;

            Tools.DebugLog($"#{car.CarInDB} Guest SuC: <{name}> <{a.available}> <{a.total}>");

            ArrayList send = new ArrayList();
            Dictionary<string, object> sendKV = new Dictionary<string, object>();
            send.Add(sendKV);
            sendKV.Add("n", name);
            sendKV.Add("ts", DateTime.UtcNow.ToString("s", Tools.ciEnUS));
            sendKV.Add("a", a.available);
            sendKV.Add("t", a.total);
            sendKV.Add("kw", 0);
            sendKV.Add("m", "");
            ShareSuc(send, false, out _, out _);
        }

        static Available GetGuestAvailabilityOwnerApi(Car car, int trid, out string error)
        {
            error = null;

            string token = car?.webhelper?.Tesla_token;
            if (string.IsNullOrEmpty(token) || token == "NULL")
            {
                error = "no Tesla token available";
                return null;
            }

            string locationGuid = GetCachedLocationGuid(trid);
            string idType = locationGuid != null ? "LOCATION_GUID" : "SITE_ID";
            string idValue = locationGuid ?? trid.ToString();

            string payload = "{\"operationName\":\"getChargingSiteInformation\",\"query\":\"\\n    query getChargingSiteInformation($id: ChargingSiteIdentifierInputType!, $vehicleMakeType: ChargingVehicleMakeTypeEnum, $deviceCountry: String!, $deviceLanguage: String!) {\\n  charging {\\n    site(\\n      id: $id\\n      deviceCountry: $deviceCountry\\n      deviceLanguage: $deviceLanguage\\n      vehicleMakeType: $vehicleMakeType\\n    ) {\\n      siteStatic {\\n        name\\n        publicStallCount\\n        locationGUID\\n        trtId {\\n          value\\n        }\\n      }\\n      siteDynamic {\\n        chargersAvailable {\\n          value\\n        }\\n        chargerDetails {\\n          availability\\n        }\\n      }\\n    }\\n  }\\n}\\n    \",\"variables\":{\"id\":{\"id\":\"$SITEID$\",\"type\":\"$IDTYPE$\"},\"vehicleMakeType\":\"NON_TESLA\",\"deviceCountry\":\"DE\",\"deviceLanguage\":\"en\"}}";
            payload = payload.Replace("$SITEID$", idValue).Replace("$IDTYPE$", idType);

            string url = "https://akamai-apigateway-charging-ownership.tesla.com/graphql?deviceLanguage=en&deviceCountry=DE&ttpLocale=de_DE&vin=" + Uri.EscapeDataString(car.Vin ?? "") + "&operationName=getChargingSiteInformation";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(20);
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + token);
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "okhttp/4.11.0");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("x-tesla-user-agent", "TeslaApp/4.44.5-3304/3a5d531cc3/android/27");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "*/*");

                    using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
                    using (var response = client.PostAsync(url, content).Result)
                    {
                        string body = response.Content.ReadAsStringAsync().Result;
                        if (!response.IsSuccessStatusCode)
                        {
                            error = $"OwnerAPI HTTP {(int)response.StatusCode}: {Truncate(body, 1000)}";
                            Tools.DebugLog(error);
                            return null;
                        }

                        JObject j = JsonConvert.DeserializeObject<JObject>(body);
                        JToken site = j?["data"]?["charging"]?["site"];
                        if (site == null)
                        {
                            error = $"OwnerAPI unexpected response: {Truncate(body, 1000)}";
                            return null;
                        }

                        Available a = new Available();
                        string name = site["siteStatic"]?["name"]?.ToString();
                        string ownerLocationGuid = site["siteStatic"]?["locationGUID"]?.ToString();
                        int ownerTrid = 0;
                        if (int.TryParse(site["siteStatic"]?["trtId"]?["value"]?.ToString(), out ownerTrid))
                        {
                            if (ownerTrid != trid)
                            {
                                error = $"OwnerAPI returned trtId {ownerTrid} for requested trtId {trid}";
                                return null;
                            }

                            CacheTridLocationGuid(ownerTrid, ownerLocationGuid);
                        }

                        CacheTridLocationGuid(trid, ownerLocationGuid);

                        JArray chargers = site["siteDynamic"]?["chargerDetails"] as JArray;

                        if (chargers == null || chargers.Count == 0)
                        {
                            error = $"OwnerAPI returned no charger details: {Truncate(body, 1000)}";
                            return null;
                        }

                        a.total = chargers.Count;

                        foreach (dynamic charger in chargers)
                        {
                            CountAvailability(charger["availability"]?.ToString(), a);
                        }

                        ShareSucAvailability(car, name, a);
                        return a;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                Tools.DebugLog("OwnerAPI exception", ex);
                return null;
            }
        }

        static void CountAvailability(string availability, Available a)
        {
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

        static DateTime _ownerFallbackErrorReported = DateTime.MinValue;

        static readonly object _tridLocationGuidLock = new object();
        static Dictionary<int, string> _tridLocationGuids;

        static string GetTridLocationGuidPath()
        {
            return System.IO.Path.Combine(FileManager.GetExecutingPath(), "trid_locationguid.json");
        }

        static Dictionary<int, string> LoadTridLocationGuids()
        {
            try
            {
                string path = GetTridLocationGuidPath();
                if (System.IO.File.Exists(path))
                    return JsonConvert.DeserializeObject<Dictionary<int, string>>(System.IO.File.ReadAllText(path)) ?? new Dictionary<int, string>();
            }
            catch (Exception ex)
            {
                Tools.DebugLog("Guest SuC cache load exception", ex);
            }

            return new Dictionary<int, string>();
        }

        static string GetCachedLocationGuid(int trid)
        {
            lock (_tridLocationGuidLock)
            {
                if (_tridLocationGuids == null)
                    _tridLocationGuids = LoadTridLocationGuids();

                return _tridLocationGuids.TryGetValue(trid, out string guid) ? guid : null;
            }
        }

        static void CacheTridLocationGuid(int trid, string locationGuid)
        {
            if (trid <= 0 || string.IsNullOrEmpty(locationGuid))
                return;

            lock (_tridLocationGuidLock)
            {
                if (_tridLocationGuids == null)
                    _tridLocationGuids = LoadTridLocationGuids();

                if (_tridLocationGuids.TryGetValue(trid, out string existing) && existing == locationGuid)
                    return;

                _tridLocationGuids[trid] = locationGuid;

                try
                {
                    System.IO.File.WriteAllText(GetTridLocationGuidPath(), JsonConvert.SerializeObject(_tridLocationGuids));
                }
                catch (Exception ex)
                {
                    Tools.DebugLog("Guest SuC cache save exception", ex);
                }
            }
        }

        static void ReportOwnerFallbackError(Car car, int trid, string error)
        {
            if (DateTime.UtcNow - _ownerFallbackErrorReported < TimeSpan.FromHours(6))
                return;

            _ownerFallbackErrorReported = DateTime.UtcNow;
            string message = $"GetGuestAvailability OwnerAPI fallback failed for trtId {trid}: {error}";
            car?.CreateExeptionlessLog("Guest SuC", message, Exceptionless.Logging.LogLevel.Error).Submit();
        }

        static string PostGuestGraphQl(int trid, string payload, string url, out string error, out bool transient)
        {
            error = null;
            transient = false;

            try
            {
                TeslaGuestCurl.Response response = TeslaGuestCurl.Post(url, payload, string.Format(GuestSiteUrl, trid));
                if (!string.IsNullOrEmpty(response.Error))
                {
                    error = response.Error;
                    Tools.DebugLog($"GuestAPI {url} exception: {response.Error}");
                    return null;
                }

                if (response.StatusCode >= 200 && response.StatusCode < 300)
                    return response.Body;

                transient = response.StatusCode == 403 || response.StatusCode == 429 || response.StatusCode == 503;
                error = $"HTTP {response.StatusCode}: {Truncate(response.Body, 1000)}";
                Tools.DebugLog($"GuestAPI {url} returned {response.StatusCode}: {response.Body}");
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                Tools.DebugLog($"GuestAPI {url} exception", ex);
            }

            return null;
        }

        static string Truncate(string value, int length)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= length)
                return value;

            return value.Substring(0, length);
        }

        static DateTime _guestApiBlockedUntil = DateTime.MinValue;
        static int _guestApiBlockCount = 0;
        static readonly object _guestApiBlockLock = new object();

        static bool IsGuestApiBlocked(out DateTime blockedUntil)
        {
            lock (_guestApiBlockLock)
            {
                blockedUntil = _guestApiBlockedUntil;
                return DateTime.UtcNow < _guestApiBlockedUntil;
            }
        }

        static void RegisterGuestApiBlock()
        {
            lock (_guestApiBlockLock)
            {
                _guestApiBlockCount = Math.Min(_guestApiBlockCount + 1, 5);
                int minutes = 30 * (1 << (_guestApiBlockCount - 1));
                _guestApiBlockedUntil = DateTime.UtcNow.AddMinutes(minutes);
                Tools.DebugLog($"GuestAPI blocked for {minutes} minutes (consecutive blocks: {_guestApiBlockCount})");
            }
        }

        static void ResetGuestApiBlock()
        {
            lock (_guestApiBlockLock)
            {
                _guestApiBlockCount = 0;
                _guestApiBlockedUntil = DateTime.MinValue;
                _ownerFallbackErrorReported = DateTime.MinValue;
            }
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
    }
}
