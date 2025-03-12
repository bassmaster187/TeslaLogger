using Exceptionless;
using Exceptionless.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using static TeslaLogger.Car;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    public class WebHelper : IDisposable
    {
        public const string vehicle_data_everything = "vehicle_data?endpoints=drive_state%3Blocation_data%3Bclimate_state%3Bvehicle_state%3Bcharge_state%3Bvehicle_config&let_sleep=true";
        public string apiaddress
        {
            get
            {
                if (car.FleetAPI)
                {
                    if (String.IsNullOrEmpty(car.FleetApiAddress))
                    {
                        var ret = GetRegion();
                        return ret;
                    }
                    else
                        return car.FleetApiAddress;
                }
                else if (car.oldAPIchinaCar)
                    return "https://owner-api.vn.cloud.tesla.cn/";
                else
                    return "https://owner-api.teslamotors.com/";
            }
        }

        private double lastOdometerKM; // defaults to 0;
        private string tesla_token = "";
        internal string Tesla_id = "";
        internal string Tesla_vehicle_id = "";
        internal string Tesla_Streamingtoken = "";
        internal string option_codes = "";
        internal string vehicle_config = "";
        internal bool is_sentry_mode; // defaults to false;
        internal string fast_charger_brand = "";
        internal string fast_charger_type = "";
        internal string conn_charge_cable = "";
        internal bool fast_charger_present; // defaults to false;
        //private bool stopStreaming = false;
        private string elevation = "";
        private DateTime elevation_time = DateTime.Now;
        internal DateTime lastTokenRefresh = DateTime.Now;
        internal DateTime lastIsDriveTimestamp = DateTime.Now;
        internal DateTime lastUpdateEfficiency = DateTime.Now.AddDays(-1);
        private static int MapQuestCount; // defaults to 0;
        private static int NominatimCount; // defaults to 0;
        private string cacheGUID = Guid.NewGuid().ToString();

        string authHost = "https://auth.tesla.com";
        CookieContainer tokenCookieContainer;

        private bool _drivingOrChargingByStream; // defaults to false;

        const string TESLA_CLIENT_ID = "81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384";
        const string TESLA_CLIENT_SECRET = "c7257eb71a564034f9419ee651c7d0e5f7aa6bfbd18bafb5c5c033b093bb2fa3";
        private const string INSERVICE = "INSERVICE";
        internal ScanMyTesla scanMyTesla;
        private string _lastShift_State = "P";
        private static readonly Regex regexAssemblyVersion = new Regex("\n\\[assembly: AssemblyVersion\\(\"([0-9\\.]+)\"", RegexOptions.Compiled);

        internal ConcurrentDictionary<string, string> TeslaAPI_Commands = new ConcurrentDictionary<string, string>();
        internal Car car;

        bool getTokenDebugVerbose; // defaults to false, only needed for debugging
        private double last_latitude_streaming = double.NaN;
        private double last_longitude_streaming = double.NaN;
        private decimal last_power_streaming = 0;

        private double battery_range2ideal_battery_range = 0.8000000416972936;

        internal HttpClient httpclient_teslalogger_de = new HttpClient();
        internal HttpClient httpClientForAuthentification;
        internal static HttpClient httpClientABRP; // defaults to null;
        internal HttpClient httpClientSuCBingo; // defaults to null;
        private HttpClient httpClientTeslaAPI; // defaults to null;
        private HttpClient httpClientTeslaChargingSites; // defaults to null;
        private HttpClient httpClientGetChargingHistoryV2; // defaults to null;
        private static object httpClientLock = new object();

        DateTime lastRefreshToken = DateTime.MinValue;
        internal DateTime nextTeslaTokenFromRefreshToken = DateTime.Now.AddHours(1);

        internal int commandCounter = 0;
        internal int commandCounterDrive = 0;
        internal int commandCounterCharging = 0;
        internal int commandcounterOnline = 0;
        int commandCounterDay = DateTime.UtcNow.Day;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources.
                httpclient_teslalogger_de.Dispose();
                httpClientForAuthentification.Dispose();
                httpClientABRP.Dispose();
                httpClientSuCBingo.Dispose();
                httpClientTeslaAPI.Dispose();
                httpClientTeslaChargingSites.Dispose();
                httpClientGetChargingHistoryV2.Dispose();
            }
            // Free native resources.
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        DateTime lastABRPActive = DateTime.MinValue;

        internal static int ABRPtimeouts; // defaults to 0;

        static Dictionary<string, Account> vehicles2Account = new Dictionary<string, Account>();
        static int nextAccountId = 1;

        object getAllVehiclesLock = new object();

        internal int nearbySuCServiceFail; // defaults to 0;
        private int getChargingHistoryV2Fail; // defaults to 0;
        internal int nearbySuCServiceOK; // defaults to 0;
        private int getChargingHistoryV2OK; // defaults to 0;

        static WebHelper()
        {
            //Damit Mono keine Zertifikatfehler wirft :-(
#pragma warning disable CA5359 // Deaktivieren Sie die Zertifikatüberprüfung nicht
            ServicePointManager.ServerCertificateValidationCallback += (p1, p2, p3, p4) => true;
#pragma warning restore CA5359 // Deaktivieren Sie die Zertifikatüberprüfung nicht
        }

        internal WebHelper(Car car)
        {
            this.car = car;

            if (KVS.Get($"commandCounter_{car.CarInDB}", out commandCounter) == KVS.NOT_FOUND)
                commandCounter = 0;

            if (KVS.Get($"commandCounterDrive_{car.CarInDB}", out commandCounterDrive) == KVS.NOT_FOUND)
                commandCounterDrive = 0;

            if (KVS.Get($"commandCounterCharging_{car.CarInDB}", out commandCounterCharging) == KVS.NOT_FOUND)
                commandCounterCharging = 0;

            if (KVS.Get($"commandCounterOnline_{car.CarInDB}", out commandcounterOnline) == KVS.NOT_FOUND)
                commandcounterOnline = 0;

            if (KVS.Get($"commandCounterDay{car.CarInDB}", out commandCounterDay) == KVS.NOT_FOUND)
                commandCounterDay = DateTime.UtcNow.Day;

            nextTeslaTokenFromRefreshToken = car.Tesla_Token_Expire;

            ResetCommandCounterEveryDay();


            httpclient_teslalogger_de.DefaultRequestHeaders.ConnectionClose = true;
            ProductInfoHeaderValue userAgent = new ProductInfoHeaderValue("Teslalogger", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            httpclient_teslalogger_de.DefaultRequestHeaders.UserAgent.Add(userAgent);
            httpclient_teslalogger_de.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(" + car.TaskerHash + "; " + Thread.CurrentThread.ManagedThreadId + ")"));

            CheckUseTaskerToken();
        }

        private void CheckUseTaskerToken()
        {
            string reply = "";

            try
            {
                if (String.IsNullOrEmpty(car.TaskerHash))
                    return;

                Log("CheckUseTaskerToken");

                using (WebClient client = new WebClient())
                {
                    DateTime start = DateTime.UtcNow;
                    reply = client.DownloadString("https://teslalogger.de/tasker_date.php?t=" + car.TaskerHash);
                    DBHelper.AddMothershipDataToDB("tasker_date.php", start, 200, car.CarInDB);

                    if (reply.Contains("not found") || reply.Contains("never!"))
                    {
                        Log("LastTaskerToken not found - Stop using fast TaskerToken request! Reply: " + reply);
                        car.UseTaskerToken = false;
                        return;
                    }

                    DateTime dt = DateTime.Parse(reply, Tools.ciEnUS);
                    var ts = DateTime.Now - dt;
                    if (ts.TotalDays > 2)
                    {
                        Log("LastTaskerToken: " + reply + " Stop using fast TaskerToken request!");
                        car.UseTaskerToken = false;
                    }
                    else
                    {
                        if (!car.UseTaskerToken)
                        {
                            Log("LastTaskerToken: " + reply + " Start using fast TaskerToken request!");
                            car.UseTaskerToken = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                reply = reply ?? "NULL";
                Log("Reply: " + reply + "\r\n" + ex.Message);

                if (!WebHelper.FilterNetworkoutage(ex))
                    car.CreateExceptionlessClient(ex).AddObject(reply, "Reply").Submit();
            }
        }

        internal string GetLastShiftState()
        {
            return _lastShift_State;
        }

        internal void SetLastShiftState(string newState)
        {
            if (!newState.Equals(_lastShift_State, StringComparison.Ordinal))
            {
                car.HandleShiftStateChange(_lastShift_State, newState);
                _lastShift_State = newState;
            }
        }

        public bool RestoreToken()
        {
            try
            {
                if (String.IsNullOrEmpty(car.Tesla_Token))
                {
                    return false;
                }

                /*

                TimeSpan ts = DateTime.Now - car.Tesla_Token_Expire;

                if (ts.TotalDays < 8)
                { */
                    Tesla_token = car.Tesla_Token;
                    lastTokenRefresh = car.Tesla_Token_Expire;

                    Log("Restore Token OK. Valid: " + car.Tesla_Token_Expire.ToString(Tools.ciEnUS));
                    return true;

                /*
                }
                else
                {
                    Log("Restore Token too old! " + car.Tesla_Token_Expire.ToString(Tools.ciEnUS));
                }*/

            }
            catch (Exception ex)
            {
                car.CreateExceptionlessClient(ex).Submit();

                Log("Error in RestoreToken: " + ex.Message);
                ex.ToExceptionless().SetUserIdentity(car.TaskerHash).Submit();
            }


            return false;
        }
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string ComputeSHA256Hash(string text)
        {
            string hashString;
            using (var sha256 = SHA256Managed.Create())
            {
                var hash = sha256.ComputeHash(Encoding.Default.GetBytes(text));
                hashString = ToHex(hash, false);
            }

            return hashString;
        }

        private static string ToHex(byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2", Tools.ciEnUS));
            }
            return result.ToString();
        }

        static string GetNameValue(string text, string name)
        {
            Regex x = new Regex("name=\\\"" + name + "\\\".+value=\\\"(.+)\\\"");
            var m = x.Match(text);
            if (m.Groups.Count > 1)
            {
                return m.Groups[1].Value;
            }

            return "";
        }

        HttpClient GetDefaultHttpClientForAuthentification()
        {
            lock (httpClientLock)
            {
                if (httpClientForAuthentification == null)
                {
                    tokenCookieContainer = new CookieContainer();

                    HttpClientHandler handler = new HttpClientHandler()
                    {
                        CookieContainer = tokenCookieContainer,
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                        AllowAutoRedirect = false,
                        UseCookies = true
                    };

                    httpClientForAuthentification = new HttpClient(handler);
                    httpClientForAuthentification.Timeout = TimeSpan.FromSeconds(30);
                    httpClientForAuthentification.DefaultRequestHeaders.Add("User-Agent", ApplicationSettings.Default.UserAgent);
                    httpClientForAuthentification.DefaultRequestHeaders.Add("x-tesla-user-agent", "TeslaApp/3.4.4-350/fad4a582e/android/8.1.0");
                    //client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*"));
                    //httpClientForAuthentification.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                    httpClientForAuthentification.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
                    httpClientForAuthentification.DefaultRequestHeaders.Add("Connection", "keep-alive");
                    // client.DefaultRequestHeaders.ConnectionClose = true;
                    httpClientForAuthentification.BaseAddress = new Uri(authHost);
                }
            }

            return httpClientForAuthentification;
        }

        internal static void LogGetToken(string resultContent, string name)
        {
            if (System.IO.File.Exists("LOGGETTOKEN"))
                System.IO.File.WriteAllText("Logfile_GetToken_" + name + ".txt", resultContent);
        }

        public string GetToken()
        {
            string resultContent = "";
            MatchCollection m;

            try
            {
                car.Passwortinfo.Append("Start getting Token<br>");

                string tempToken = UpdateTeslaTokenFromRefreshToken();

                if (!String.IsNullOrEmpty(tempToken))
                    return tempToken;

            }
            catch (ThreadAbortException)
            {
                System.Diagnostics.Debug.WriteLine("Thread Stop!");
            }
            catch (Exception ex)
            {
                car.Passwortinfo.Append("Error in GetTokenAsync: " + ex.Message + "<br>");

                if (ex.InnerException != null)
                    car.Passwortinfo.Append("Error in GetTokenAsync: " + ex.InnerException.Message + "<br>");

                if (ex.Message == "NO Credentials" || ex.Message == "Car inactive")
                {
                    Log(ex.Message);
                    return "NULL";
                }

                Log("Error in GetTokenAsync: " + ex.Message);
                ExceptionWriter(ex, resultContent);

                ex.ToExceptionless().SetUserIdentity(car.TaskerHash).AddObject(resultContent, "ResultContent").Submit();
            }

            return "NULL";
        }

        internal string UpdateTeslaTokenFromRefreshToken()
        {
            car.CreateExeptionlessLog("Tesla Token", "UpdateTeslaTokenFromRefreshToken", Exceptionless.Logging.LogLevel.Info).Submit();

            string refresh_token = car.DbHelper.GetRefreshToken(out string tesla_token);

            if (car.FleetAPI)
            {
                if (!String.IsNullOrEmpty(ApplicationSettings.Default.TelemetryClientID))
                    return UpdateTeslaTokenFromRefreshTokenFromFleetAPIWithClientID(refresh_token);

                return UpdateTeslaTokenFromRefreshTokenFromFleetAPI(refresh_token);
            }

            if (car.oldAPIchinaCar)
                authHost = "https://auth.tesla.cn";

            if (String.IsNullOrEmpty(refresh_token))
            {
                car.Passwortinfo.Append("No Refresh Token<br>");
                Log("No Refresh Token");
                return "";
            }

            int HttpStatusCode = 0;
            string resultContent = "";

            try
            {
                Log("Update Tesla Token From Refresh Token!");
                var d = new Dictionary<string, string>();
                d.Add("grant_type", "refresh_token");
                d.Add("client_id", "ownerapi");
                d.Add("refresh_token", refresh_token);
                d.Add("scope", "openid email offline_access");

                string json = JsonConvert.SerializeObject(d);

                using (HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                })
                {

                    DateTime start = DateTime.UtcNow;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromSeconds(300);
                        client.DefaultRequestHeaders.Add("User-Agent", ApplicationSettings.Default.UserAgent);
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Connection.Add("keep-alive");

                        using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
                        {
                            HttpResponseMessage result = client.PostAsync(new Uri(authHost + "/oauth2/v3/token"), content).Result;
                            resultContent = result.Content.ReadAsStringAsync().Result;

                            DBHelper.AddMothershipDataToDB("UpdateTeslaTokenFromRefreshToken()", start, (int)result.StatusCode, car.CarInDB);

                            HttpStatusCode = (int)result.StatusCode;

                            car.Log("HttpStatus: " + result.StatusCode.ToString());

                            dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);

                            if (Tools.IsPropertyExist(jsonResult, "error"))
                            {
                                string error = jsonResult["error"];
                                string error_description = jsonResult["error_description"];

                                car.Log("ResultContent UpdateTeslaTokenFromRefreshToken: " + resultContent);
                                car.CreateExeptionlessLog("UpdateTeslaTokenFromRefreshToken", "Error: " + error, Exceptionless.Logging.LogLevel.Error)
                                    .AddObject(error_description, "error_description")
                                    .AddObject(HttpStatusCode, "HttpStatusCode")
                                    .AddObject(resultContent, "resultContent")
                                    .Submit();
                            }


                            string access_token = jsonResult["access_token"] ?? throw new Exception("access_token Missing");
                            string new_refresh_token = jsonResult["refresh_token"] ?? throw new Exception("refresh_token Missing");
                            CheckNewRefreshToken(refresh_token, new_refresh_token);

                            // as of March 21 2022 Tesla returns a bearer token. GetTokenAsync4 is no longer neeaded. 
                            car.CreateExeptionlessLog("Tesla Token", "UpdateTeslaTokenFromRefreshToken Success", Exceptionless.Logging.LogLevel.Info).Submit();
                            string Token = jsonResult["access_token"];

                            if (jsonResult.ContainsKey("expires_in"))
                            {
                                var t = DateTime.UtcNow.AddSeconds((int)(jsonResult["expires_in"])).AddHours(-2);
                                if (t > DateTime.UtcNow.AddHours(1))
                                    nextTeslaTokenFromRefreshToken = t;
                                else
                                {
                                    t = DateTime.UtcNow.AddSeconds((int)(jsonResult["expires_in"]));
                                    nextTeslaTokenFromRefreshToken = t;
                                }

                                Log("access token expires: " + nextTeslaTokenFromRefreshToken.ToLocalTime());
                            }

                            SetNewAccessToken(Token);

                            return Tesla_token;


                            // return GetTokenAsync4(access_token);
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                System.Diagnostics.Debug.WriteLine("Thread Stop!");
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
                // car.ExternalLog("UpdateTeslaTokenFromRefreshToken: \r\nHTTP StatusCode: " + HttpStatusCode+ "\r\nresultContent: " + resultContent +"\r\n" + ex.ToString());
                car.CreateExeptionlessLog("UpdateTeslaTokenFromRefreshToken", "Error getting access token", Exceptionless.Logging.LogLevel.Error).AddObject(HttpStatusCode, "HTTP StatusCode").AddObject(resultContent, "ResultContent").Submit();
                car.CreateExceptionlessClient(ex).AddObject(HttpStatusCode, "HTTP StatusCode").AddObject(resultContent, "ResultContent").MarkAsCritical().Submit();
                ExceptionlessClient.Default.ProcessQueueAsync();
            }
            return "";
        }

        private void CheckNewRefreshToken(string refresh_token, string new_refresh_token)
        {
            if (new_refresh_token == null || new_refresh_token.Length < 10)
            {
                Log("new Refresh Token is invalid!!");
            }
            else if (new_refresh_token == refresh_token)
            {
                Log("refresh_token not changed");
            }
            else
            {
                car.DbHelper.UpdateRefreshToken(new_refresh_token);
            }
        }

        void SetNewAccessToken(string access_token)
        {
            Tesla_token = StringCipher.Decrypt(access_token);
            car.Tesla_Token = StringCipher.Decrypt(access_token);
            car.Tesla_Token_Expire = nextTeslaTokenFromRefreshToken;
            car.LoginRetryCounter = 0;
            car.DbHelper.UpdateTeslaToken();

            try
            {
                _ = IsOnline(true).Result; // get new Tesla_Streamingtoken;
                                           // restart streaming thread with new token
                RestartStreamThreadWithTask();
            }
            catch (Exception ex)
            {
                car.Log("SetNewAccessToken: " + ex.ToString());
            }
        }

        public string GetRegion()
        {
            try
            {
                if (!car.FleetAPI)
                {
                    return "";
                }

                HttpClient httpClientTeslaAPI = GetHttpClientTeslaAPI();
                using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(ApplicationSettings.Default.TeslaHttpProxyURL + "/api/1/users/region")))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Tesla_token);
                    Tools.DebugLog($"GetRegion #{car.CarInDB} request: {request.RequestUri}");
                    HttpResponseMessage response = httpClientTeslaAPI.SendAsync(request).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        if (result.Contains("\"error\""))
                        {
                            car.Log(result);
                            return "";
                        }

                        dynamic j = JsonConvert.DeserializeObject(result);
                        dynamic r = j["response"];
                        String fleeturl = r["fleet_api_base_url"];
                        if (fleeturl.StartsWith("https:", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (!fleeturl.EndsWith("/"))
                            {
                                fleeturl += "/";
                            }

                            car.FleetApiAddress = fleeturl;
                            car.Log("FleetApiAddress: " + fleeturl);
                            car.DbHelper.UpdateCarColumn("fleetAPIaddress", fleeturl);
                            return fleeturl;
                        }

                        car.CreateExeptionlessLog("GetRegion", "no url", LogLevel.Fatal).AddObject(result, "ResultContent").Submit();
                        return "";
                    }
                    else
                    {
                        if (result.Contains("token expired"))
                        {
                            car.Log("Token expired");
                            UpdateTeslaTokenFromRefreshToken();
                        }

                        car.CreateExeptionlessLog("GetRegion", "Error", LogLevel.Fatal).AddObject((int)response.StatusCode + " / " + response.StatusCode.ToString(), "StatusCode").Submit();
                        Log("Error getting Region: " + (int)response.StatusCode + " / " + response.StatusCode.ToString() + " result: " + result);
                        return "";
                    }
                }
            }
            catch (ThreadAbortException ex)
            {
                System.Diagnostics.Debug.WriteLine("Thread Stop!");
                car.CreateExceptionlessClient(ex).MarkAsCritical().Submit();
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
                car.CreateExceptionlessClient(ex).MarkAsCritical().Submit();
                ExceptionlessClient.Default.ProcessQueueAsync();
            }

            return "";
        }

        private string UpdateTeslaTokenFromRefreshTokenFromFleetAPI(string refresh_token)
        {
            try
            {
                var ts = DateTime.UtcNow - lastRefreshToken;
                if (ts.TotalMinutes < 5)
                {
                    car.Log("ERROR: Refresh Token Spam!!!");
                    return "";
                }

                lastRefreshToken = DateTime.UtcNow;

                Log("Update Access Token From Refresh Token - FleetAPI!");
                if (String.IsNullOrEmpty(refresh_token))
                {
                    car.Log("No Refresh Token");
                    return "";
                }

                using (var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("refresh_token", refresh_token),
                new KeyValuePair<string, string>("taskertoken", car.TaskerHash),
                new KeyValuePair<string, string>("vin", car.Vin),
            }))
                {

                    var response = httpclient_teslalogger_de.PostAsync(new Uri("https://teslalogger.de/teslaredirect/refresh_token.php"), formContent).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        if (result.Contains("User revoked consent"))
                            car.CreateExeptionlessLog("User revoked consent", "Teslalogger won't work anymore!", LogLevel.Warn).Submit();

                        if (result.Contains("\"error\""))
                        {
                            string error = result;

                            try
                            {
                                dynamic j2 = JsonConvert.DeserializeObject(result);
                                error = j2["error"];
                            }
                            catch (Exception)
                            { }

                            car.CreateExeptionlessLog("UpdateTeslaTokenFromRefreshTokenFromFleetAPI", error, LogLevel.Error)
                                .AddObject(result, "Result Content")
                                .Submit();
                            car.Log(result);
                            Thread.Sleep(30000);
                            return "";
                        }

                        dynamic jsonResult = JsonConvert.DeserializeObject(result);
                        if (jsonResult.ContainsKey("expires_in"))
                        {
                            var t = DateTime.UtcNow.AddSeconds((int)(jsonResult["expires_in"])).AddHours(-2);
                            if (t > DateTime.UtcNow.AddHours(1))
                                nextTeslaTokenFromRefreshToken = t;
                            else
                            {
                                t = DateTime.UtcNow.AddSeconds((int)(jsonResult["expires_in"]));
                                nextTeslaTokenFromRefreshToken = t;
                            }

                            Log("access token expires: " + nextTeslaTokenFromRefreshToken.ToLocalTime());

                            /*
                            CacheItemPolicy policy = new CacheItemPolicy();
                            policy.AbsoluteExpiration = DateTime.Now.AddSeconds((int)(jsonResult["expires_in"])).AddMinutes(-5);
                            policy.RemovedCallback = new CacheEntryRemovedCallback((CacheEntryRemovedArguments _) =>
                            {
                                Tools.DebugLog($"#{car.CarInDB}: access token will expire in 5 minutes");
                                UpdateTeslaTokenFromRefreshToken();
                            });
                            _ = MemoryCache.Default.Add("RefreshToken_" + car.CarInDB+ $"_{Environment.TickCount}", policy, policy);
                            */
                        }
                        string access_token = jsonResult["access_token"];

                        string new_refresh_token = jsonResult["refresh_token"];
                        CheckNewRefreshToken(refresh_token, new_refresh_token);

                        SetNewAccessToken(access_token);
                        return access_token;
                    }
                    else
                    {
                        car.CreateExeptionlessLog("UpdateTeslaTokenFromRefreshTokenFromFleetAPI", response.StatusCode.ToString(), LogLevel.Error)
                            .AddObject(result, "Result Content")
                            .Submit();

                        Log("Error getting Access Token from Refreh Token: " + (int)response.StatusCode + " / " + response.StatusCode.ToString());
                        Thread.Sleep(30000);
                        return "";
                    }
                }
            }
            catch (ThreadAbortException)
            {
                System.Diagnostics.Debug.WriteLine("Thread Stop!");
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
                car.CreateExceptionlessClient(ex).MarkAsCritical().Submit();
                ExceptionlessClient.Default.ProcessQueueAsync();
                Thread.Sleep(30000);
            }

            return "";
        }

        private string UpdateTeslaTokenFromRefreshTokenFromFleetAPIWithClientID(string refresh_token)
        {
            try
            {
                var ts = DateTime.UtcNow - lastRefreshToken;
                if (ts.TotalMinutes < 5)
                {
                    car.Log("ERROR: Refresh Token Spam!!!");
                    return "";
                }

                lastRefreshToken = DateTime.UtcNow;

                Log("Update Access Token From Refresh Token - FleetAPI! with ClientID");
                if (String.IsNullOrEmpty(refresh_token))
                {
                    car.Log("No Refresh Token");
                    return "";
                }

                using (var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("refresh_token", refresh_token),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", ApplicationSettings.Default.TelemetryClientID),
            }))
                {

                    var response = httpclient_teslalogger_de.PostAsync(new Uri("https://auth.tesla.com/oauth2/v3/token"), formContent).Result;
                    string result = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        if (result.Contains("User revoked consent"))
                            car.CreateExeptionlessLog("User revoked consent", "Teslalogger won't work anymore!", LogLevel.Warn).Submit();

                        if (result.Contains("\"error\""))
                        {
                            string error = result;

                            try
                            {
                                dynamic j2 = JsonConvert.DeserializeObject(result);
                                error = j2["error"];
                            }
                            catch (Exception)
                            { }

                            car.CreateExeptionlessLog("UpdateTeslaTokenFromRefreshTokenFromFleetAPI", error, LogLevel.Error)
                                .AddObject(result, "Result Content")
                                .Submit();
                            car.Log(result);
                            Thread.Sleep(30000);
                            return "";
                        }

                        dynamic jsonResult = JsonConvert.DeserializeObject(result);
                        if (jsonResult.ContainsKey("expires_in"))
                        {
                            var t = DateTime.UtcNow.AddSeconds((int)(jsonResult["expires_in"])).AddHours(-2);
                            if (t > DateTime.UtcNow.AddHours(1))
                                nextTeslaTokenFromRefreshToken = t;
                            else
                            {
                                t = DateTime.UtcNow.AddSeconds((int)(jsonResult["expires_in"]));
                                nextTeslaTokenFromRefreshToken = t;
                            }

                            Log("access token expires: " + nextTeslaTokenFromRefreshToken.ToLocalTime());

                            /*
                            CacheItemPolicy policy = new CacheItemPolicy();
                            policy.AbsoluteExpiration = DateTime.Now.AddSeconds((int)(jsonResult["expires_in"])).AddMinutes(-5);
                            policy.RemovedCallback = new CacheEntryRemovedCallback((CacheEntryRemovedArguments _) =>
                            {
                                Tools.DebugLog($"#{car.CarInDB}: access token will expire in 5 minutes");
                                UpdateTeslaTokenFromRefreshToken();
                            });
                            _ = MemoryCache.Default.Add("RefreshToken_" + car.CarInDB+ $"_{Environment.TickCount}", policy, policy);
                            */
                        }
                        string access_token = jsonResult["access_token"];

                        string new_refresh_token = jsonResult["refresh_token"];
                        CheckNewRefreshToken(refresh_token, new_refresh_token);

                        SetNewAccessToken(access_token);
                        return access_token;
                    }
                    else
                    {
                        car.CreateExeptionlessLog("UpdateTeslaTokenFromRefreshTokenFromFleetAPI", response.StatusCode.ToString(), LogLevel.Error)
                            .AddObject(result, "Result Content")
                            .Submit();

                        Log("Error getting Access Token from Refreh Token: " + (int)response.StatusCode + " / " + response.StatusCode.ToString());
                        Thread.Sleep(30000);
                        return "";
                    }
                }
            }
            catch (ThreadAbortException)
            {
                System.Diagnostics.Debug.WriteLine("Thread Stop!");
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
                car.CreateExceptionlessClient(ex).MarkAsCritical().Submit();
                ExceptionlessClient.Default.ProcessQueueAsync();
                Thread.Sleep(30000);
            }

            return "";
        }

        internal static void SearchFornewCars()
        {
            Logfile.Log("SearchFornewCars");
            try
            {
                foreach (KeyValuePair<string, Account> s in vehicles2Account)
                {
                    try
                    {
                        SearchFornewCars(s.Key, s.Value);
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.ToString());
                        ex.ToExceptionless();
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
                ex.ToExceptionless();
            }
        }

        internal static void SearchFornewCars(string vin, Account a)
        {
            // dynamic j = JsonConvert.DeserializeObject(s);
            // dynamic d = j["response"];

            // for (int x = 0; x < d.Count; x++)
            {
                try
                {
                    // dynamic i = d[x];
                    // string vin = i["vin"];
                    // string display_name = i["display_name"] ?? "";
                    // System.Diagnostics.Debug.WriteLine("VIN: " + vin);

                    string display_name = a.display_name;
                    string access_token = a.tesla_token;
                    bool fleetAPI = a.fleetAPI;

                    int x = 1;

                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
                        MySqlCommand cmd = new MySqlCommand("select * from cars where vin = @vin", con);
                        cmd.Parameters.AddWithValue("@vin", vin);
                        var dr = cmd.ExecuteReader();
                        if (!dr.Read())
                        {
                            String temp = $"Create new Car VIN: {vin} ID: {x} DisplayName: {display_name} FleetAPI: {fleetAPI}";
                            Logfile.Log(temp);
                            System.Diagnostics.Debug.WriteLine(temp);

                            var refresh_token = DBHelper.GetRefreshTokenFromAccessToken(access_token);
                            DBHelper.InsertNewCar("", "", x, false, access_token, refresh_token, vin, display_name, fleetAPI);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                    ex.ToExceptionless();
                }
            }
        }

        private void RestartStreamThreadWithTask()
        {
            _ = Task.Factory.StartNew(() =>
            {
                if (streamThread != null)
                    Tools.DebugLog($"streamThread {streamThread.Name}:{streamThread.ManagedThreadId} state:{streamThread.ThreadState}");

                StopStreaming();
                bool newThreadCreated = false;
                for (int i = 0; i < 100 && !newThreadCreated; i++)
                {
                    if (streamThread != null)
                        Tools.DebugLog($"streamThread {streamThread.Name}:{streamThread.ManagedThreadId} state:{streamThread.ThreadState}");

                    if (streamThread == null || streamThread.ThreadState == ThreadState.Stopped)
                    {
                        newThreadCreated = true;
                        streamThread = null;
                        StartStreamThread();

                        if (streamThread != null)
                            Tools.DebugLog($"streamThread {streamThread.Name}:{streamThread.ManagedThreadId} state:{streamThread.ThreadState}");
                    }
                    else
                    {
                        Thread.Sleep(1000);

                        if (streamThread != null)
                            Tools.DebugLog($"streamThread {streamThread.Name}:{streamThread.ManagedThreadId} state:{streamThread.ThreadState}");
                    }
                }
                if (!newThreadCreated)
                {
                    car.Log("Failed to restart stream thread");
                }
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

       
        private string lastCharging_State = "";

        public void ResetLastChargingState()
        {
            lastCharging_State = "";
        }

        internal bool IsCharging(bool justCheck = false, bool noMemcache = false)
        {
            if (car.FleetAPI)
            {
                return car.telemetryParser?.IsCharging ?? false;
            }

            string resultContent = "";
            try
            {
                // resultContent = GetCommand("charge_state").Result;
                resultContent = GetCommand(vehicle_data_everything).Result;

                if (resultContent == INSERVICE)
                {
                    System.Threading.Thread.Sleep(10000);
                    return false;
                }

                Task<double?> outside_temp = GetOutsideTempAsync();

                Tools.SetThreadEnUS();
                dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                dynamic charge_state = jsonResult["response"]["charge_state"];

                if (charge_state["charging_state"] == null || (resultContent != null && resultContent.Contains("vehicle unavailable")))
                {
                    if (justCheck)
                    {
                        return false;
                    }

                    if (charge_state["charging_state"] == null)
                    {
                        Log("charging_state = null");
                    }
                    else if (resultContent != null && resultContent.Contains("vehicle unavailable"))
                    {
                        Log("charging_state: vehicle unavailable");
                    }

                    Thread.Sleep(10000);

                    return lastCharging_State == "Charging";
                }

                string charging_state = charge_state["charging_state"].ToString();
                _ = long.TryParse(charge_state["timestamp"].ToString(), out long ts);


                decimal battery_range = (decimal)charge_state["battery_range"];

                decimal ideal_battery_range = (decimal)charge_state["ideal_battery_range"];
                if (ideal_battery_range == 999)
                {
                    ideal_battery_range = battery_range;
                }

                car.CurrentJSON.current_ideal_battery_range_km = Math.Round((double)ideal_battery_range * 1.609344, 1);

                string battery_level = charge_state["battery_level"].ToString();
                if (battery_level != null && Convert.ToDouble(battery_level) != car.CurrentJSON.current_battery_level)
                {
                    car.CurrentJSON.current_battery_level = Convert.ToDouble(battery_level);
                    car.CurrentJSON.CreateCurrentJSON();
                }
                string charger_power = "";
                if (charge_state["charger_power"] != null)
                {
                    charger_power = charge_state["charger_power"].ToString();
                }

                string charge_energy_added = charge_state["charge_energy_added"].ToString();

                string charger_voltage = "";
                string charger_phases = "";
                string charger_actual_current = "";
                string charge_current_request = "";
                string charger_pilot_current = "";

                if (charge_state["charger_voltage"] != null)
                {
                    charger_voltage = charge_state["charger_voltage"].ToString();
                }

                if (charge_state["charger_phases"] != null)
                {
                    charger_phases = charge_state["charger_phases"].ToString();
                }

                if (charge_state["charger_actual_current"] != null)
                {
                    charger_actual_current = charge_state["charger_actual_current"].ToString();
                }

                if (charge_state["charge_current_request"] != null)
                {
                    charge_current_request = charge_state["charge_current_request"].ToString();
                }

                if (charge_state["charger_pilot_current"] != null)
                {
                    charger_pilot_current = charge_state["charger_pilot_current"].ToString();
                }

                if (charge_state["fast_charger_brand"] != null)
                {
                    fast_charger_brand = charge_state["fast_charger_brand"].ToString();
                }

                if (charge_state["fast_charger_type"] != null)
                {
                    fast_charger_type = charge_state["fast_charger_type"].ToString();
                }

                if (charge_state["conn_charge_cable"] != null)
                {
                    conn_charge_cable = charge_state["conn_charge_cable"].ToString();
                }

                if (charge_state["fast_charger_present"] != null)
                {
                    fast_charger_present = bool.Parse(charge_state["fast_charger_present"].ToString());
                }

                if (charge_state["charge_rate"] != null)
                {
                    car.CurrentJSON.current_charge_rate_km = Convert.ToDouble(charge_state["charge_rate"]) * 1.609344;
                }

                if (charge_state["charge_limit_soc"] != null)
                {
                    if (car.CurrentJSON.charge_limit_soc != Convert.ToInt32(charge_state["charge_limit_soc"]))
                    {
                        car.CurrentJSON.charge_limit_soc = Convert.ToInt32(charge_state["charge_limit_soc"]);
                        car.CurrentJSON.CreateCurrentJSON();
                    }
                }

                if (charge_state["time_to_full_charge"] != null)
                {
                    if (car.CurrentJSON.current_time_to_full_charge != Convert.ToDouble(charge_state["time_to_full_charge"], Tools.ciEnUS))
                    {
                        car.CurrentJSON.current_time_to_full_charge = Convert.ToDouble(charge_state["time_to_full_charge"], Tools.ciEnUS);
                        car.CurrentJSON.CreateCurrentJSON();
                    }
                }

                double power = 0;
                if (Double.TryParse(charger_power, out power))
                    power *= -1;


                if (justCheck)
                {
                    if (charging_state == "Charging")
                    {
                        string dtTimestamp = "?";
                        try
                        {
                            dtTimestamp = DBHelper.UnixToDateTime(long.Parse(ts.ToString())).ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        catch (Exception)
                        { }

                        Log($"Charging! Voltage: {charger_voltage}V / Power: {charger_power}kW / Timestamp: {ts} / Date: {dtTimestamp}");
                        if (!lastCharging_State.Equals(charging_state))
                        {
                            car.DbHelper.InsertCharging(ts.ToString(), battery_level, charge_energy_added, charger_power, (double)ideal_battery_range, (double)battery_range, charger_voltage, charger_phases, charger_actual_current, outside_temp.Result, car.IsHighFrequenceLoggingEnabled(true), charger_pilot_current, charge_current_request);
                        }
                        return double.TryParse(charger_power, out double dPowerkW) && dPowerkW >= 1.0;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (charging_state == "Charging")
                {
                    _ = SendDataToAbetterrouteplannerAsync(ts, car.CurrentJSON.current_battery_level, 0, true, power, car.CurrentJSON.GetLatitude(), car.CurrentJSON.GetLongitude());

                    lastCharging_State = charging_state;
                    car.DbHelper.InsertCharging(ts.ToString(), battery_level, charge_energy_added, charger_power, (double)ideal_battery_range, (double)battery_range, charger_voltage, charger_phases, charger_actual_current, outside_temp.Result, car.IsHighFrequenceLoggingEnabled(true), charger_pilot_current, charge_current_request);
                    return true;
                }
                else if (charging_state == "Complete")
                {
                    if (lastCharging_State != "Complete")
                    {
                        car.DbHelper.InsertCharging(ts.ToString(), battery_level, charge_energy_added, charger_power, (double)ideal_battery_range, (double)battery_range, charger_voltage, charger_phases, charger_actual_current, outside_temp.Result, true, charger_pilot_current, charge_current_request);
                        Log("Charging Complete");
                    }

                    lastCharging_State = charging_state;
                }
            }
            catch (Exception ex)
            {

                if (resultContent == null || resultContent == "NULL")
                {
                    Log("isCharging = NULL");
                    Thread.Sleep(10000);
                }
                else if (ex is TaskCanceledException)
                {
                    Log("isCharging: TaskCanceledException");
                    Thread.Sleep(3000);
                }
                else if (!resultContent.Contains("upstream internal error"))
                {
                    SubmitExceptionlessClientWithResultContent(ex, resultContent);

                    ExceptionWriter(ex, resultContent);
                }

                if (lastCharging_State == "Charging" && !justCheck)
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        HttpClient GetHttpClientTeslaAPI()
        {
            if (httpClientTeslaAPI == null)
            {
                if (String.IsNullOrEmpty(Tesla_token) || Tesla_token == "NULL")
                {
                    car.Log("ERROR: Create HTTP Client with wrong Tesla Token!");
                }

                httpClientTeslaAPI = new HttpClient();
                {
                    httpClientTeslaAPI.DefaultRequestHeaders.Add("x-tesla-user-agent", "TeslaApp/3.4.4-350/fad4a582e/android/8.1.0");
                    httpClientTeslaAPI.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Linux; Android 8.1.0; Pixel XL Build/OPM4.171019.021.D1; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/68.0.3440.91 Mobile Safari/537.36");
                    // do not set auth headers in DefaultRequestHeaders
                    // https://makolyte.com/csharp-how-to-make-concurrent-requests-with-httpclient/#Only_use_DefaultRequestHeaders_for_headers_that_dont_change
                    // httpClientTeslaAPI.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);
                    httpClientTeslaAPI.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClientTeslaAPI.Timeout = TimeSpan.FromSeconds(11);
                }
            }
            return httpClientTeslaAPI;
        }

        HttpClient GethttpclientTeslaNearbyChargingSites()
        {
            lock (httpClientLock)
            {
                if (httpClientTeslaChargingSites == null)
                {
                    httpClientTeslaChargingSites = new HttpClient();
                    {
                        // https://github.com/ev-map/EVMap/blob/master/app/src/main/java/net/vonforst/evmap/api/availability/TeslaAvailabilityDetector.kt#L444
                        httpClientTeslaChargingSites.DefaultRequestHeaders.Add("x-tesla-user-agent", "TeslaApp/4.19.5-1667/3a5d531cc3/android/27");
                        httpClientTeslaChargingSites.DefaultRequestHeaders.Add("User-Agent", "okhttp/4.9.2");
                        httpClientTeslaChargingSites.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);
                        httpClientTeslaChargingSites.DefaultRequestHeaders.Add("Accept", "*/*");
                        httpClientTeslaChargingSites.Timeout = TimeSpan.FromSeconds(11);
                    }
                }
                return httpClientTeslaChargingSites;
            }
        }

        HttpClient GethttpclientgetChargingHistoryV2()
        {
            lock (httpClientLock)
            {
                if (httpClientGetChargingHistoryV2 == null)
                {
                    httpClientGetChargingHistoryV2 = new HttpClient();
                    httpClientGetChargingHistoryV2.DefaultRequestHeaders.Add("User-Agent", "curl/8.4.0");
                    httpClientGetChargingHistoryV2.DefaultRequestHeaders.Add("Accept", "*/*");
                    httpClientGetChargingHistoryV2.Timeout = TimeSpan.FromSeconds(120);
                }
                return httpClientGetChargingHistoryV2;
            }
        }

        public string GetVehicles()
        {
            string resultContent = "";
            while (true)
            {
                try
                {
                    Newtonsoft.Json.Linq.JArray r1temp;
                    GetAllVehicles(out resultContent, out r1temp, false);

                    if (r1temp == null)
                    {
                        if (resultContent != null)
                            car.Log("GetVehicles: " + resultContent);

                        car.CurrentJSON.FatalError = "Car not found!";
                        car.CurrentJSON.CreateCurrentJSON();

                        return "NULL";
                    }

                    /*
                    if (car.CarInAccount >= r1temp.Count)
                    {
                        Log("Car # " + car.CarInAccount + " not exists!");
                        ListCarsInAccount(r1temp);

                        return "NULL";
                    }
                    */

                    dynamic r2 = SearchCarDictionary(r1temp);

                    if (r2 == null)
                    {
                        Log("Car VIN: " + car.Vin + " not exists!");
                        return "NULL";
                    }

                    string OnlineState = r2["state"].ToString();
                    System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + " : " + OnlineState);

                    string display_name = r2["display_name"].ToString();
                    if(string.IsNullOrEmpty(display_name) && string.IsNullOrEmpty(car.DisplayName))
                    {
                        // Grafana dashboards break, if Car's display_name is null or empty, so
                        // if display_name is null from API and car.DisplayName is also null already
                        // we just write "Car $id" to database because that is how the fallback in admin panel works                        
                        display_name = $"Car {car.CarInDB}";
                    }
                    if (!string.IsNullOrEmpty(display_name) && car.DisplayName != display_name)
                    {
                        car.DisplayName = display_name;
                        Log("WriteCarSettings -> Display_Name");
                        car.WriteSettings();
                    }


                    Log("display_name: " + display_name);

                    /* TODO not needed anymore?
                    try
                    {
                        string filepath = System.IO.Path.Combine(FileManager.GetExecutingPath(), "DISPLAY_NAME");
                        System.IO.File.WriteAllText(filepath, display_name);
                        UpdateTeslalogger.Chmod(filepath, 666, false);
                    }
                    catch (Exception)
                    { }
                    */

                    string vin = r2["vin"].ToString();
                    Log("vin: " + Tools.ObfuscateVIN(vin));

                    if (car.Vin != vin)
                    {
                        car.Vin = vin;
                        Tools.VINDecoder(vin, out int year, out _, out bool aWD, out bool mIC, out _, out string motor, out bool mIG);
                        car.Year = year;
                        car.AWD = aWD;
                        car.MIC = mIC;
                        car.MIG = mIG;
                        car.Motor = motor;
                        Log("WriteCarsettings -> VIN");
                        car.WriteSettings();
                    }

                    Tesla_id = r2["id"].ToString();
                    Log("id: " + Tools.ObfuscateString(Tesla_id));

                    Tesla_vehicle_id = r2["vehicle_id"].ToString();
                    Log("vehicle_id: " + Tools.ObfuscateString(Tesla_vehicle_id));

                    byte[] tempTasker = Encoding.UTF8.GetBytes(vin + car.TeslaName);

                    string oldTaskerHash = car.TaskerHash;

                    car.TaskerHash = string.Empty;
                    using (DamienG.Security.Cryptography.Crc32 crc32 = new DamienG.Security.Cryptography.Crc32())
                    {
                        foreach (byte b in crc32.ComputeHash(tempTasker))
                        {
                            car.TaskerHash += b.ToString("x2").ToLower();
                        }

                        if (!string.IsNullOrEmpty(ApplicationSettings.Default.TaskerPrefix))
                        {
                            car.TaskerHash = ApplicationSettings.Default.TaskerPrefix + "_" + car.TaskerHash;
                        }

                        if (car.CarInAccount > 0)
                        {
                            car.TaskerHash = car.TaskerHash + "_" + car.CarInAccount;
                        }

                        if (oldTaskerHash != car.TaskerHash)
                        {
                            Log("WriteCarsettings -> TaskerToken");
                            car.WriteSettings();

                            CheckUseTaskerToken();
                        }

                        Log("Tasker Config:\r\n Server Port: https://teslalogger.de\r\n Path: wakeup.php\r\n Attribute: t=" + car.TaskerHash);

                        /*
                        try
                        {
                            string taskertokenpath = System.IO.Path.Combine(FileManager.GetExecutingPath(), "TASKERTOKEN");
                            System.IO.File.WriteAllText(taskertokenpath, TaskerHash);
                        }
                        catch (Exception)
                        { }
                        */

                        scanMyTesla = new ScanMyTesla(car);

                        /*
                        dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                        token = jsonResult["access_token"];
                        */

                        return resultContent;
                    }

                }
                catch (Exception ex)
                {
                    if (resultContent.IndexOf("Retry Later", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        int sleep = random.Next(10000) + 10000;
                        Log("GetVehicles Error: Retry Later - Sleep " + sleep);

                        Thread.Sleep(sleep);
                    }
                    else
                    {
                        SubmitExceptionlessClientWithResultContent(ex, resultContent);

                        ExceptionWriter(ex, resultContent);

                        while (ex != null)
                        {
                            if (!(ex is AggregateException))
                            {
                                Log("GetVehicles Error: " + ex.Message);
                            }
                            else
                                Log("GetVehicles Error: " + ex.Message);

                            ex = ex.InnerException;
                        }

                        Thread.Sleep(30000);
                    }
                }
            }
        }

        internal void GetAllVehicles(out string resultContent, out Newtonsoft.Json.Linq.JArray vehicles, bool throwExceptionOnUnauthorized, bool doNotCache = false)
        {
            lock (getAllVehiclesLock)
            {
                int accountid = 0;
                lock (vehicles2Account)
                {
                    if (vehicles2Account.TryGetValue(car.Vin, out Account a))
                    {
                        accountid = a.id;
                    }
                }

                string cacheKey = accountid + "_vehicles";
                object cachedValue = MemoryCache.Default.Get(cacheKey);
                bool checkVehicle2Account = false;

                if (!doNotCache && cachedValue != null && accountid > 0)
                {
                    resultContent = cachedValue as String;
                }
                else
                {
                    HttpClient httpClientTeslaAPI = GetHttpClientTeslaAPI();
                    string adresse = "https://owner-api.teslamotors.com/api/1/products?orders=true";

                    if (car.FleetAPI)
                    {
                        adresse = apiaddress + "api/1/vehicles";
                    }

                    Task<HttpResponseMessage> resultTask;
                    HttpResponseMessage result;

                    if (!car.oldAPIchinaCar)
                    {
                        DoGetVehiclesRequest(out resultContent, httpClientTeslaAPI, adresse, out resultTask, out result);

                        if (resultContent.Contains("user not allowed in region"))
                        {
                            car.oldAPIchinaCar = true;
                            car.DbHelper.UpdateCarColumn("oldAPIchinaCar", "1");
                            adresse = "https://owner-api.vn.cloud.tesla.cn/api/1/products?orders=true";
                            DoGetVehiclesRequest(out resultContent, httpClientTeslaAPI, adresse, out resultTask, out result);
                        }
                    }
                    else
                    {
                        adresse = "https://owner-api.vn.cloud.tesla.cn/api/1/products?orders=true";
                        DoGetVehiclesRequest(out resultContent, httpClientTeslaAPI, adresse, out resultTask, out result);
                    }

                    if (result.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        if (throwExceptionOnUnauthorized)
                            throw new UnauthorizedAccessException();

                        if (LoginRetry(result))
                        {
                            httpClientTeslaAPI = GetHttpClientTeslaAPI();

                            DoGetVehiclesRequest(out resultContent, httpClientTeslaAPI, adresse, out resultTask, out result);

                            if (result.IsSuccessStatusCode)
                            {
                                MemoryCache.Default.Add(cacheKey, resultContent, DateTime.Now.AddSeconds(30));
                                checkVehicle2Account = true;
                            }
                        }
                    }
                }

                dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                vehicles = jsonResult["response"];

                if (checkVehicle2Account)
                {
                    InsertVehicles2AccountFromVehiclesResponse(vehicles);
                }
            }
        }

        private void InsertVehicles2AccountFromVehiclesResponse(string resultContent)
        {
            try
            {
                lock (vehicles2Account)
                {
                    dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                    JArray vehicles = jsonResult["response"];
                    InsertVehicles2AccountFromVehiclesResponse(vehicles);
                }
            }
            catch (Exception ex)
            {
                SubmitExceptionlessClientWithResultContent(ex, resultContent);
            }
        }

        private void InsertVehicles2AccountFromVehiclesResponse(JArray vehicles)
        {
            lock (vehicles2Account)
            {
                bool inserted = false;
                try
                {
                    foreach (dynamic v in vehicles)
                    {
                        string vin = v["vin"];

                        if (!vehicles2Account.ContainsKey(vin))
                        {
                            string display_name = v["display_name"].ToString();

                            Account a = new Account();
                            a.id = nextAccountId;
                            a.tesla_token = car.Tesla_Token;
                            a.fleetAPI = car.FleetAPI;
                            a.display_name = display_name;
                            vehicles2Account.Add(vin, a);
                            inserted = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless();
                }
                if (inserted)
                    nextAccountId++;

            }
        }

        private void ListCarsInAccount(Newtonsoft.Json.Linq.JArray cars)
        {
            try
            {
                Log("Existig Cars in Account:");

                for (int x = 0; x < cars.Count; x++)
                {
                    var cc = cars[x];
                    var ccVin = cc["vin"].ToString();
                    var ccDisplayName = cc["display_name"].ToString();

                    Log($"   Car # {x} / Vin: {ccVin} / Name: {ccDisplayName}");
                }
            }
            catch (Exception ex)
            {
                car.CreateExceptionlessClient(ex).Submit();

                Logfile.Log(ex.ToString());
            }
        }

        private object SearchCarDictionary(Newtonsoft.Json.Linq.JArray cars)
        {
            if (cars == null)
            {
                return null;
            }

            if (car.Vin?.Length > 0)
            {
                for (int x = 0; x < cars.Count; x++)
                {
                    var cc = cars[x];
                    var ccVin = cc["vin"].ToString();

                    if (ccVin == car.Vin)
                    {
                        return cc;
                    }
                }

                Logfile.Log("Car with VIN: " + car.Vin + " not found! Display Name: " + car.DisplayName);

                // DBHelper.ExecuteSQLQuery("delete from cars where id = " + car.CarInDB); 

                return null;
            }

            return null;
        }

        private void DoGetVehiclesRequest(out string resultContent, HttpClient client, string adresse, out Task<HttpResponseMessage> resultTask, out HttpResponseMessage result)
        {
            DateTime start = DateTime.UtcNow;
            using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(adresse)))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Tesla_token);
                Tools.DebugLog($"DoGetVehiclesRequest #{car.CarInDB} request: {request.RequestUri}");
                resultTask = client.SendAsync(request);
                result = resultTask.Result;
                resultContent = result.Content.ReadAsStringAsync().Result;

                // resultContent = Tools.ConvertBase64toString("eyJSZXNwb25zZSI6bnVsbCwiRXJyb3IgZGVzY3JpcHRpb24iOiIiLCJFcnJvciI6Im5vdCBmb3VuZCJ9"); // {"Response":null,"Error description":"","Error":"not found"}

                _ = car.GetTeslaAPIState().ParseAPI(resultContent, "vehicles");
                DBHelper.AddMothershipDataToDB("GetVehicles()", start, (int)result.StatusCode, car.CarInDB);

                if (TeslaAPI_Commands.ContainsKey("vehicles"))
                {
                    TeslaAPI_Commands.TryGetValue("vehicles", out string drive_state);
                    TeslaAPI_Commands.TryUpdate("vehicles", resultContent, drive_state);
                }
                else
                {
                    TeslaAPI_Commands.TryAdd("vehicles", resultContent);
                }
            }
        }

        private int unknownStateCounter; // defaults to 0;
#pragma warning disable CA2211 // Nicht konstante Felder dürfen nicht sichtbar sein
        public static object isOnlineLock = new object();
#pragma warning restore CA2211 // Nicht konstante Felder dürfen nicht sichtbar sein

        public async Task<string> IsOnline(bool returnOnUnauthorized = false)
        {
            string resultContent = "";
            try
            {

                int accountid = 0;
                lock (vehicles2Account)
                {
                    if (vehicles2Account.TryGetValue(car.Vin, out Account account))
                    {
                        accountid = account.id;
                    }
                }

                string cacheKey = accountid + "_vehicles";

                HttpResponseMessage result = null;

                object cachedValue = MemoryCache.Default.Get(cacheKey);
                DateTime start = DateTime.UtcNow;

                if (cachedValue != null && accountid > 0)
                {
                    resultContent = cachedValue as String;
                }
                else
                {
                    CheckRefreshToken();

                    if (car.FleetAPI)
                    {
                        if (car.telemetryParser?.IsOnline() == true)
                            return "online";
                        else
                            return "asleep";
                    }


                    HttpClient httpClientTeslaAPI = GetHttpClientTeslaAPI();
                    string adresse = "https://owner-api.teslamotors.com/api/1/products?orders=true";

                    if (car.oldAPIchinaCar)
                    {
                        adresse = "https://owner-api.vn.cloud.tesla.cn/api/1/products?orders=true";
                    }
                    if (car.FleetAPI)
                    {
                        adresse = apiaddress + "api/1/vehicles";
                    }
                    using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(adresse)))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Tesla_token);
                        Tools.DebugLog($"IsOnline #{car.CarInDB} request: {adresse}");
                        result = await httpClientTeslaAPI.SendAsync(request);

                        if (returnOnUnauthorized && result?.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            return "NULL";
                        }

                        if (LoginRetry(result))
                        {
                            return "NULL";
                        }

                        resultContent = await result.Content.ReadAsStringAsync();
                        // resultContent = Tools.ConvertBase64toString("");
                    }
                }


                if (resultContent == null || resultContent == "NULL")
                {
                    Log("isOnline = NULL");
                    Thread.Sleep(5000);
                    return "NULL";
                }

                if (resultContent.Contains("upstream connect error or disconnect"))
                {
                    Log("isOnline Result Content: " + resultContent);
                    Thread.Sleep(5000);
                    return "NULL";
                }

                if (resultContent.Contains("operation_timedout with 10s timeout"))
                {
                    Log("isOnline: operation_timedout with 10s timeout");
                    Thread.Sleep(20000);
                    return "NULL";
                }

                if (resultContent.Contains("Retry later"))
                {
                    int sleep = random.Next(10000) + 10000;
                    Log("isOnline: Retry later - Sleep: " + sleep);
                    Thread.Sleep(sleep);
                    return "NULL";
                }

                if (resultContent.Contains("upstream internal error"))
                {
                    int sleep = random.Next(10000) + 10000;
                    Log("isOnline: upstream internal error - Sleep: " + sleep);
                    Thread.Sleep(sleep);
                    return "NULL";
                }

                _ = car.GetTeslaAPIState().ParseAPI(resultContent, "vehicles");
                if (result != null && cachedValue == null)
                {
                    if (result.IsSuccessStatusCode)
                    {
                        DBHelper.AddMothershipDataToDB("IsOnline()", start, (int)result.StatusCode, car.CarInDB);
                    }
                    else
                    {
                        DBHelper.AddMothershipDataToDB("IsOnline()", -1, (int)result.StatusCode, car.CarInDB);
                    }
                }


                if (TeslaAPI_Commands.ContainsKey("vehicles"))
                {
                    TeslaAPI_Commands.TryGetValue("vehicles", out string drive_state);
                    TeslaAPI_Commands.TryUpdate("vehicles", resultContent, drive_state);
                }
                else
                {
                    TeslaAPI_Commands.TryAdd("vehicles", resultContent);
                }

                dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);

                JArray response = jsonResult["response"];


                if (response == null && resultContent?.Contains("not found") == true)
                {
                    Log("IsOnline response = NULL: " + resultContent);

                    car.CreateExeptionlessLog("WebHelper", "IsOnline:Not Found", Exceptionless.Logging.LogLevel.Warn).AddObject(resultContent, "resultContent").Submit();
                    car.Restart("IsOnline: not found", 0);

                    return "NULL";
                }

                dynamic r4 = SearchCarDictionary(response);

                if (r4 == null)
                {
                    Log("Vin not found in Response!");
                    return "NULL";
                }

                try
                {
                    string access_type = r4["access_type"].ToString();
                    car.Access_type = access_type;

                    if (result != null && result.IsSuccessStatusCode && cachedValue == null)
                    {
                        if (access_type == "OWNER")
                        {
                            InsertVehicles2AccountFromVehiclesResponse(resultContent);
                            if (accountid == 0)
                            {
                                lock (vehicles2Account)
                                {
                                    if (vehicles2Account.TryGetValue(car.Vin, out Account account))
                                    {
                                        accountid = account.id;
                                    }
                                }

                                cacheKey = accountid + "_vehicles";
                            }
                            MemoryCache.Default.Add(cacheKey, resultContent, DateTime.Now.AddSeconds(20));
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("access_type: " + access_type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SubmitExceptionlessClientWithResultContent(ex, resultContent);
                }

                string state = r4["state"].ToString();
                if (r4.ContainsKey("tokens") && r4["tokens"] != null)
                {
                    string temp_Tesla_Streamingtoken = r4["tokens"][0].ToString();
                    if (temp_Tesla_Streamingtoken != Tesla_Streamingtoken)
                    {
                        Tesla_Streamingtoken = temp_Tesla_Streamingtoken;
                    }
                }

                try
                {
                    /* 
                    option_codes = r4["option_codes"].ToString();
                    string[] oc = option_codes.Split(',');

                    car.AWD = oc.Contains("DV4W");

                    if (oc.Contains("MDLS") || oc.Contains("MS01") || oc.Contains("MS02") || oc.Contains("MS03"))
                        car.Model = "MS";
                    else if (oc.Contains("MDLX"))
                        car.Model = "MX";
                    else if (oc.Contains("MDL3"))
                        car.Model = "M3";

                    var battery = oc.Where(r => r.StartsWith("BT")).ToArray();
                    if (battery != null && battery.Length > 0)
                    {
                        if (car.Battery != battery[0])
                        {
                            Log("Battery: " + battery[0] + " / " + car.Model);
                            car.Battery = battery[0];

                            car.WriteSettings();
                        }
                    }

                    car.Performance = oc.Contains("PBT85") || oc.Contains("PX01") || oc.Contains("P85D") || oc.Contains("PX6D") || oc.Contains("X024") | oc.Contains("PBT8") | oc.Contains("PF01");

                    */

                    if (state == "asleep")
                    {
                        return state;
                    }
                    else if (state == "unknown")
                    {
                        Log("unknown state " + unknownStateCounter);

                        ExceptionWriter(new Exception("unknown state"), resultContent);

                        car.CreateExeptionlessLog("IsOnline", "unknown state", Exceptionless.Logging.LogLevel.Warn).AddObject(resultContent, "resultContent").Submit();

                        if (unknownStateCounter == 0)
                        {
                            /*
                            string r = Wakeup().Result;
                            Log("WakupResult: " + r);
                            */
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

                    if (ts.TotalMinutes > 240)
                    {
                        if (state == "offline" || state == "asleep")
                            return state;

                        CheckVehicleConfig();
                    }
                }
                catch (Exception ex)
                {
                    if (ex is TaskCanceledException)
                    {
                        Log("IsOnline: TaskCanceledException");
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        SubmitExceptionlessClientWithResultContent(ex, resultContent);
                        ExceptionWriter(ex, resultContent);
                    }
                }

                return state;

            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException)
                {
                    Log("IsOnline: TaskCanceledException");
                    Thread.Sleep(1000);
                }
                else
                {
                    SubmitExceptionlessClientWithResultContent(ex, resultContent);
                    ExceptionWriter(ex, resultContent);
                }
            }

            return "NULL";
        }

        internal void CheckRefreshToken()
        {
            if (nextTeslaTokenFromRefreshToken < DateTime.UtcNow)
            {
                nextTeslaTokenFromRefreshToken = DateTime.UtcNow.AddMinutes(5);
                UpdateTeslaTokenFromRefreshToken();
            }
        }

        void CheckVehicleConfig()
        {
            if (car.FleetAPI)
            {
                UpdateEfficiency();
                lastUpdateEfficiency = DateTime.Now;
                return;
            }

            string resultContent2 = "";
            try
            {
                // resultContent2 = GetCommand("vehicle_config").Result;
                // resultContent2 = GetCommand("vehicle_data?endpoints=vehicle_config&let_sleep=true").Result;
                Log("CheckVehicleConfig");
                resultContent2 = GetCommand(vehicle_data_everything).Result;

                if (resultContent2 == INSERVICE || resultContent2 == "NULL")
                {
                    System.Threading.Thread.Sleep(5000);
                    return;
                }

                if (resultContent2?.Length > 0)
                    vehicle_config = resultContent2;

                dynamic jBadge = JsonConvert.DeserializeObject(resultContent2);
                dynamic jBadgeResult = jBadge["response"]["vehicle_config"];

                if (jBadgeResult != null)
                {
                    string car_type = car.CarType;
                    string car_special_type = car.CarSpecialType;
                    string trim_badging = car.TrimBadging;


                    if (Tools.IsPropertyExist(jBadgeResult, "car_type"))
                    {
                        car.CarType = jBadgeResult["car_type"].ToString().ToLower().Trim();
                    }

                    if (Tools.IsPropertyExist(jBadgeResult, "car_special_type"))
                    {
                        car.CarSpecialType = jBadgeResult["car_special_type"].ToString().ToLower().Trim();
                    }

                    car.TrimBadging = Tools.IsPropertyExist(jBadgeResult, "trim_badging")
                        ? (string)jBadgeResult["trim_badging"].ToString().ToLower().Trim()
                        : "";

                    UpdateEfficiency();
                    lastUpdateEfficiency = DateTime.Now;

                    if (car_type != car.CarType || car_special_type != car.CarSpecialType || trim_badging != car.TrimBadging)
                        car.DbHelper.WriteCarSettings();
                }
            }
            catch (Exception ex)
            {
                SubmitExceptionlessClientWithResultContent(ex, resultContent2);
                ExceptionWriter(ex, resultContent2);
            }
        }

        public void SubmitExceptionlessClientWithResultContent(Exception ex, string content)
        {
            if (FilterNetworkoutage(ex))
                return;

            CreateExceptionlessClientWithResultContent(ex, content).Submit();
        }

        public static bool FilterNetworkoutage(Exception ex)
        {
            string temp = ex.ToString();
            if (temp.Contains("No route to host"))
                return true;
            else if (temp.Contains("NameResolutionFailure"))
                return true;
            else if (temp.Contains("No such host is known"))
                return true;
            else if (temp.Contains("Network is unreachable"))
                return true;

            return false;
        }

        public EventBuilder CreateExceptionlessClientWithResultContent(Exception ex, string content)
        {
            string base64 = Tools.ConvertString2Base64(content);

            return car.CreateExceptionlessClient(ex).AddObject(content, "ResultContent").AddObject(base64, "ResultContentBase64");
        }

        public void UpdateEfficiency()
        {
            //string eff = "0.190052356";
            
            Tools.VINDecoder(car.Vin, out int year, out string vinCarType, out bool AWD, out bool MIC, out string battery, out string motor, out bool MIG);

            if (car.CarType == "model3" || vinCarType == "Model 3")
            {
                // Tools.VINDecoder(car.Vin, out int year, out _, out bool AWD, out bool MIC, out string battery, out string motor, out _);

                if (car.TrimBadging == "p74d" && year < 2021)
                {
                    WriteCarSettings("0.152", "M3 LR P");
                    return;
                }
                if (car.TrimBadging == "p74d" && year >= 2024)
                {
                    WriteCarSettings("0.147", "M3 LR P 2024");
                    return;
                }
                if (car.TrimBadging == "74d" && year >= 2024)
                {
                    WriteCarSettings("0.141", "M3 LR 2024");
                    return;
                }
                if (car.TrimBadging == "p74d" && year >= 2021)
                {
                    WriteCarSettings("0.158", "M3 LR P 2021");
                    return;
                }
                if (car.TrimBadging == "74d" && AWD && year < 2021)
                {
                    WriteCarSettings("0.152", "M3 LR");
                    return;
                }
                if (car.TrimBadging == "74" && !AWD && year == 2019)
                {
                    WriteCarSettings("0.145", "M3 LR RWD 2019");
                    return;
                }

                int maxRange = car.DbHelper.GetAvgMaxRage();
                if (maxRange > 430)
                {
                    try
                    {
                        if (!AWD)
                        {
                            if (MIC)
                                if (battery == "LFP")
                                    WriteCarSettings("0.138", "M3 SR+ LFP 2021");
                                else
                                    WriteCarSettings("0.142", "M3 LR RWD 2023");
                            else
                                WriteCarSettings("0.145", "M3 LR RWD 2019");
                            return;

                        }
                        else if (motor == "3 dual performance" && year == 2021)
                        {
                            WriteCarSettings("0.158", "M3 LR P 2021");
                            return;
                        }
                        else if (motor == "3 dual performance" && year < 2021)
                        {
                            WriteCarSettings("0.158", "M3 LR P");
                            return;
                        }
                        else if (motor == "3 dual performance highland" && year >= 2024)
                        {
                            WriteCarSettings("0.147", "M3 LR P 2024");
                            return;
                        }
                        else if (car.DBWhTR >= 0.135 && car.DBWhTR <= 0.142 && AWD && year >= 2024)
                        {
                            WriteCarSettings("0.141", "M3 LR 2024");
                            return;
                        }
                        else if (car.DBWhTR >= 0.135 && car.DBWhTR <= 0.142 && AWD)
                        {
                            WriteCarSettings("0.139", "M3 LR FL");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        car.CreateExceptionlessClient(ex).Submit();
                        Log(ex.ToString());
                    }

                    WriteCarSettings("0.152", "M3 LR");
                    return;
                }
                else
                {
                    if (battery == "LFP")
                    {
                        if (year == 2021 || car.DBWhTR > 0 && car.DBWhTR < 0.130)
                            WriteCarSettings("0.127", "M3 SR+ LFP 2021");
                        else
                            WriteCarSettings("0.133", "M3 SR+ LFP");
                    }
                    else if (year == 2021)
                        WriteCarSettings("0.126", "M3 SR+ 2021");
                    else
                        WriteCarSettings("0.137", "M3 SR+");
                    return;
                }
            }
            else if (car.CarType == "models2" && car.CarSpecialType == "base")
            {
                if (car.TrimBadging == "60")
                {
                    WriteCarSettings("0.200", "S 60");
                    return;
                }
                else if (car.TrimBadging == "60d")
                {
                    WriteCarSettings("0.187", "S 60D");
                    return;
                }
                else if (car.TrimBadging == "75d")
                {
                    if (car.CarVoltageAt50SOC > 350)
                        WriteCarSettings("0.186", "S 75D 400V");
                    else
                        WriteCarSettings("0.186", "S 75D");
                    return;
                }
                else if (car.TrimBadging == "75")
                {
                    WriteCarSettings("0.195", "S 75");
                    return;
                }
                else if (car.TrimBadging == "90d")
                {
                    WriteCarSettings("0.188", "S 90D");
                    return;
                }
                else if (car.TrimBadging == "p90")
                {
                    WriteCarSettings("0.201", "S P90");
                    return;
                }
                else if (car.TrimBadging == "p90d")
                {
                    WriteCarSettings("0.201", "S P90D");
                    return;
                }
                else if (car.TrimBadging == "100d")
                {
                    if (car.DBWhTR <= 0.165)
                    {
                        WriteCarSettings("0.162", "S 100D Raven");
                        return;
                    }

                    WriteCarSettings("0.193", "S 100D");
                    return;
                }
                else if (car.TrimBadging == "p100d")
                {
                    WriteCarSettings("0.200", "S P100D");
                    return;
                }
                else if (car.TrimBadging.Length == 0)
                {
                    // Tools.VINDecoder(car.Vin, out _, out _, out bool AWD, out _, out _, out string motor, out _);
                    int maxRange = car.DbHelper.GetAvgMaxRage();
                    if (maxRange > 500)
                    {
                        if (motor == "dual performance")
                        {
                            WriteCarSettings("0.173", "S Raven LR P");
                            return;
                        }

                        WriteCarSettings("0.173", "S Raven LR");
                    }
                    else
                    {
                        WriteCarSettings("0.163", "S Raven SR");
                    }

                    return;
                }
                else
                {
                    WriteCarSettings("0.190", "S ???");
                    return;
                }
            }
            else if (car.CarType == "models" && (car.CarSpecialType == "base" || car.CarSpecialType == "signature") && year < 2021)
            {
                if (car.TrimBadging == "60")
                {
                    WriteCarSettings("0.200", "S 60");
                    return;
                }
                else if (car.TrimBadging == "70")
                {
                    WriteCarSettings("0.200", "S 70");
                    return;
                }
                else if (car.TrimBadging == "70d")
                {
                    WriteCarSettings("0.194", "S 70D");
                    return;
                }
                else if (car.TrimBadging == "p85d")
                {
                    WriteCarSettings("0.201", "S P85D");
                    return;
                }
                else if (car.TrimBadging == "p85+")
                {
                    WriteCarSettings("0.201", "S P85+");
                    return;
                }
                else if (car.TrimBadging == "85d")
                {
                    if (car.CarVoltageAt50SOC > 300 && car.CarVoltageAt50SOC < 350)
                        WriteCarSettings("0.186", "S 85D 350V");
                    else
                        WriteCarSettings("0.186", "S 85D");
                    return;
                }
                else if (car.TrimBadging == "p85")
                {
                    WriteCarSettings("0.201", "S P85");
                    return;
                }
                else if (car.TrimBadging == "85")
                {
                    if (car.CarVoltageAt50SOC > 300 && car.CarVoltageAt50SOC < 350)
                        WriteCarSettings("0.201", "S 85 350V");
                    else
                        WriteCarSettings("0.201", "S 85");
                    return;
                }
                else if (car.TrimBadging == "90")
                {
                    WriteCarSettings("0.201", "S 90");
                    return;
                }
                else if (car.TrimBadging == "90d")
                {
                    WriteCarSettings("0.187", "S 90D");
                    return;
                }
                else if (car.TrimBadging == "p90")
                {
                    WriteCarSettings("0.201", "S P90");
                    return;
                }
                else if (car.TrimBadging == "p90d")
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
            else if (car.CarType == "modelx" && (car.CarSpecialType == "base" || car.CarSpecialType == "founder") && year < 2021)
            {
                if (car.TrimBadging == "75d")
                {
                    WriteCarSettings("0.224", "X 75D");
                    return;
                }
                else if (car.TrimBadging == "100d")
                {
                    if (car.Raven)
                        WriteCarSettings("0.184", "X 100D");
                    else
                        WriteCarSettings("0.217", "X 100D");
                    return;
                }
                else if (car.TrimBadging == "90d")
                {
                    WriteCarSettings("0.212", "X 90D");
                    return;
                }
                else if (car.TrimBadging == "p100d")
                {
                    WriteCarSettings("0.226", "X P100D");
                    return;
                }
                else if (car.TrimBadging == "p90d")
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
            else if (car.CarType == "modely" && car.CarSpecialType == "base")
            {
                // Tools.VINDecoder(car.Vin, out int year, out string ct, out bool AWD, out bool MIC, out string battery, out string motor, out bool MIG);
                if (car.TrimBadging == "74d")
                {
                    if (MIC)
                    {
                        if (year < 2022)
                        {
                            WriteCarSettings("0.148", "Y LR AWD (MIC 2021)"); //LG 74kWh
                            return;
                        }
                        else
                        {
                            WriteCarSettings("0.148", "Y LR AWD (MIC 2022)"); //LG 79kWh
                            return;
                        }
                    }
                    else if (MIG)
                    {
                        WriteCarSettings("0.148", "Y LR AWD (MIG)");
                        return;
                    }
                    else
                    {
                        WriteCarSettings("0.148", "Y LR AWD (US)");
                        return;
                    }

                }
                else if (car.TrimBadging == "p74d")
                {
                    if (MIG)
                    {
                        WriteCarSettings("0.165", "Y P (MIG)");
                        return;
                    }
                    else
                    {
                        WriteCarSettings("0.148", "Y P (US)");
                        return;
                    }
                }
                else if (car.TrimBadging == "50")
                {
                    if (MIG)
                    {
                        if (!AWD && car.Vin[6] == 'E' && (car.Vin[7] == 'S' || car.Vin[7] == 'J'))
                        {
                            WriteCarSettings("0.142", "Y SR (MIG BYD)");
                            return;
                        }
                        else if (battery == "LFP")
                        {
                            WriteCarSettings("0.142", "Y SR (MIG CATL)");
                            return;
                        }
                    }
                    else if (MIC)
                    {
                        WriteCarSettings("0.142", "Y SR (MIC)");
                        return;
                    }

                    WriteCarSettings("0.142", "Y SR+");
                    return;
                }
                else if (car.TrimBadging == "74")
                {
                    WriteCarSettings("0.149", "Y LR RWD");
                    return;
                }
            }
            else if (car.CarType == "tamarind" && car.CarSpecialType == "base")
            {
                // Tools.VINDecoder(car.Vin, out int year, out _, out bool AWD, out bool MIC, out string battery, out string motor, out _);

                if (motor == "triple 2021 plaid")
                {
                    WriteCarSettings("0.193", "X 2021 Plaid");
                    return;
                }
                else
                {
                    WriteCarSettings("0.149", "X 2021 LR");
                    return;
                }
            }
            else if (car.CarType == "lychee" && car.CarSpecialType == "base")
            {
                // Tools.VINDecoder(car.Vin, out int year, out _, out bool AWD, out bool MIC, out string battery, out string motor, out _);

                if (motor == "triple 2021 plaid")
                {
                    WriteCarSettings("0.172", "S 2021 Plaid");
                    return;
                }
                else
                {
                    WriteCarSettings("0.151", "S 2021 LR");
                    return;
                }
            }
            else if (vinCarType == "Model X" && year >= 2021)
            {
                if (motor == "triple 2021 plaid")
                {
                    WriteCarSettings("0.193", "X 2021 Plaid");
                    return;
                }
                else
                {
                    WriteCarSettings("0.181", "X 2021 LR");
                    return;
                }
            }
            else if (vinCarType == "Model S" && year >= 2021)
            {
                if (motor == "triple 2021 plaid")
                {
                    WriteCarSettings("0.172", "S 2021 Plaid");
                    return;
                }
                else
                {
                    WriteCarSettings("0.151", "S 2021 LR");
                    return;
                }
            }


            /*
            if (car.Model == "MS")
            {
                if (car.Battery == "BTX5")
                {
                    if (car.AWD)
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
                else if (car.Battery == "BTX4")
                {
                    if (car.Performance)
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
                else if (car.Battery == "BTX6")
                {
                    if (car.Performance)
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
                else if (car.Battery == "BTX8")
                {
                    if (car.AWD)
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
                else if (car.Battery == "BT85")
                {
                    if (car.AWD)
                    {
                        if (car.Performance)
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
                        if (car.Performance)
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
                else if (car.Battery == "PBT85")
                {
                    car = "S P85";
                    eff = "0.210";
                }
                else if (car.Battery == "BT70")
                {
                    car = "S 70 ?";
                    eff = "0.200";
                }
                else if (car.Battery == "BT60")
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
            else if (car.Model == "MX")
            {
                if (car.Battery == "BTX5")
                {
                    eff = "0.208";
                    car = "X 75D";
                }
                else if (car.Battery == "BTX4")
                {
                    if (!car.Performance)
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
                else if (car.Battery == "BTX6")
                {
                    if (car.Performance)
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
            else if (car.Model == "M3")
            {
                if (car.Battery == "BT37")
                {
                    if (car.Performance)
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
                if (car.Battery == "BT85")
                {
                    car = "S 85 ?";
                    eff = "0.200";
                }
            }

            WriteCarSettings(eff, car);
            */
        }

        private void WriteCarSettings(string eff, string ModelName)
        {
            // TODO eff in double
            if (car.ModelName != ModelName || car.WhTR.ToString(Tools.ciEnUS) != eff)
            {
                Log("WriteCarSettings -> ModelName: " + ModelName + " eff: " + eff);

                car.ModelName = ModelName;
                car.WhTR = Convert.ToDouble(eff, Tools.ciEnUS);
                car.WriteSettings();
            }
        }

        public bool IsDriving(bool justinsertdb = false)
        {
            if (car.FleetAPI)
            {
                if (car.telemetryParser?.Driving == false)
                {
                    return false;
                }
                else
                {
                    if (car.telemetry != null)
                        return car.telemetryParser.Driving == true;
                    else
                        return false;
                }
            }

            string resultContent = "";
            try
            {
                if (car.FirmwareAtLeastVersion("2023.38.4"))
                {
                    resultContent = GetCommand(vehicle_data_everything).Result;
                }
                else
                {
                    resultContent = GetCommand("vehicle_data?endpoints=drive_state&let_sleep=true").Result;
                }

                if (resultContent == INSERVICE)
                {
                    System.Threading.Thread.Sleep(10000);
                    return false;
                }

                // Log("IsDriving");

                Tools.SetThreadEnUS();
                dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                dynamic drive_state = jsonResult["response"]["drive_state"];
                _ = long.TryParse(drive_state["timestamp"].ToString(), out long ts);

                decimal dLatitude = 0;
                decimal dLongitude = 0;
                int heading = 0;

                /*
                try
                {
                    string temp_Tesla_Streamingtoken = jsonResult["response"]["tokens"][0].ToString();

                    if (temp_Tesla_Streamingtoken != Tesla_Streamingtoken)
                    {
                        Tesla_Streamingtoken = temp_Tesla_Streamingtoken;
                        //Log("Streamingtoken changed (IsDriving): " + Tools.ObfuscateString(Tesla_Streamingtoken));

                        // can be ignored, is not used at the moment car.Log("Tesla_Streamingtoken changed!");
                    }
                }
                catch (Exception ex)
                {
                    SubmitExceptionlessClientWithResultContent(ex, resultContent);
                    ExceptionWriter(ex, resultContent);
                }
                */

                if (drive_state.ContainsKey("latitude"))
                {
                    dLatitude = (decimal)drive_state["latitude"];
                    dLongitude = (decimal)drive_state["longitude"];
                    heading = (int)drive_state["heading"];
                }
                else
                {
                    // New API after 2023.38.4 
                    var rc2 = GetCommand(vehicle_data_everything).Result;
                    if (rc2 == null)
                        return false;
                    try
                    {
                        dynamic jsonResult2 = JsonConvert.DeserializeObject(rc2);
                        dynamic r2x = jsonResult2["response"]["drive_state"];

                        if (r2x?.ContainsKey("latitude") == true)
                        {
                            dLatitude = (decimal)r2x["latitude"];
                            dLongitude = (decimal)r2x["longitude"];
                            heading = (int)r2x["heading"];
                        }
                        else
                            return false;
                    }
                    catch (Exception)
                    {
                        resultContent = rc2;
                        throw;
                    }
                }

                double latitude = (double)dLatitude;
                double longitude = (double)dLongitude;
                car.CurrentJSON.heading = heading;

                car.CurrentJSON.SetPosition(latitude, longitude, ts);

                int speed = 0;
                if (drive_state["speed"] != null)
                {
                    speed = (int)drive_state["speed"];
                }

                int power = 0;
                if (drive_state["power"] != null)
                {
                    power = (int)drive_state["power"];
                }

                string shift_state = "";
                if (drive_state["shift_state"] != null)
                {
                    shift_state = drive_state["shift_state"].ToString();
                    SetLastShiftState(shift_state);
                }
                else
                {
                    TimeSpan timespan = DateTime.Now - lastIsDriveTimestamp;

                    if (timespan.TotalMinutes > 10)
                    {
                        if (!GetLastShiftState().Equals("P"))
                        {
                            Log("No Valid IsDriving since 10min! (shift_state=NULL)");
                            SetLastShiftState("P");
                        }
                        return false;
                    }
                    else
                    {
                        shift_state = GetLastShiftState();
                    }
                }


                if (justinsertdb || shift_state == "D" || shift_state == "R" || shift_state == "N" || car.CurrentJSON.current_is_preconditioning)
                {
                    // var address = ReverseGecocodingAsync(latitude, longitude);
                    //var altitude = AltitudeAsync(latitude, longitude);
                    // Log("IsDriving2");

                    Task<double> odometer = GetOdometerAsync();
                    double? outside_temp = null;
                    Task<double?> t_outside_temp = null;

                    if (!Geofence.GetInstance().RacingMode)
                    {
                        t_outside_temp = GetOutsideTempAsync();
                    }

                    TimeSpan tsElevation = DateTime.Now - elevation_time;
                    if (tsElevation.TotalSeconds > 30)
                    {
                        elevation = "";
                    }

                    double ideal_battery_range_km = GetIdealBatteryRangekm(out double battery_level, out double battery_range_km);

                    if (t_outside_temp != null)
                    {
                        outside_temp = t_outside_temp.Result;
                    }

                    _ = SendDataToAbetterrouteplannerAsync(ts, battery_level, speed, false, power, (double)dLatitude, (double)dLongitude);

                    if (car.FleetAPI && !justinsertdb)
                    {
                        latitude = 0;
                        longitude = 0;
                    }

                    car.DbHelper.InsertPos(ts.ToString(), latitude, longitude, speed, power, odometer.Result, ideal_battery_range_km, battery_range_km, battery_level, outside_temp, elevation);

                    if (shift_state == "D" || shift_state == "R" || shift_state == "N")
                    {
                        lastIsDriveTimestamp = DateTime.Now;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (resultContent == null || resultContent == "NULL" )
                {
                    Log("IsDriving = NULL!");
                    Thread.Sleep(10000);
                }
                else
                {
                    SubmitExceptionlessClientWithResultContent(ex, resultContent);
                    ExceptionWriter(ex, resultContent);
                }

                if (GetLastShiftState() == "D" || GetLastShiftState() == "R" || GetLastShiftState() == "N")
                {
                    TimeSpan ts = DateTime.Now - lastIsDriveTimestamp;

                    if (ts.TotalMinutes > 10)
                    {
                        Log("No Valid IsDriving since 10min! (Exception: " + ex.GetType().ToString() + ")");
                        SetLastShiftState("P");
                        return false;
                    }

                    return true;
                }
            }

            return false;
        }

        private void ExceptionWriter(Exception ex, string inhalt)
        {
            try
            {
                if (inhalt != null)
                {
                    if (inhalt.Contains("vehicle unavailable:"))
                    {
                        Log("vehicle unavailable");
                        System.Threading.Thread.Sleep(5000);
                        return;
                    }
                    else if (inhalt.Contains("upstream internal error"))
                    {
                        Log("upstream internal error");
                        System.Threading.Thread.Sleep(10000);
                        return;
                    }
                    else if (inhalt.Contains("Connection refused"))
                    {
                        Log("Connection refused");
                        System.Threading.Thread.Sleep(30000);
                        return;
                    }
                    else if (inhalt.Contains("No route to host"))
                    {
                        Log("No route to host");
                        System.Threading.Thread.Sleep(60000);
                        return;
                    }
                    else if (inhalt.Contains("You have been temporarily blocked for making too many requests!"))
                    {
                        Log("temporarily blocked for making too many requests!");
                        System.Threading.Thread.Sleep(30000);
                        return;
                    }
                }

                Logfile.ExceptionWriter(ex, inhalt);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
        }

        Thread streamThread; // defaults to null;
        public void StartStreamThread()
        {
            if (File.Exists("DONTUSESTREAMINGAPI"))
                return;

            if (car.FleetAPI) // Fleet API doesn't support streaming now
                return;

            if (streamThread == null)
            {
                streamThread = new System.Threading.Thread(() => StartStream());
                streamThread.Name = "StreamAPIThread_" + car.CarInDB;
                streamThread.Start();
            }
        }

        private void StartStream()
        {
            string resultContent = null;
            byte[] buffer = new byte[1024];

            car.Log("StartStream");
            stopStreaming = false;
            string line = "";
            while (!stopStreaming)
            {
                System.Net.WebSockets.ClientWebSocket ws = null;
                try
                {
                    // get streaming data if
                    // * car is fallig asleep to interrupt the "let the car fall asleep" cycle
                    // or
                    // * StreamingPos is true in settings.json and the car is driving
                    // or
                    // * car is in service
                    // otherwise skip
                    if (!car.IsInService() && !car.CurrentJSON.current_falling_asleep && !(Tools.StreamingPos() && car.CurrentJSON.current_driving))
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    // skip if car is asleep, streaming API will just timeout all the time

                    if (car.GetCurrentState() == Car.TeslaState.Sleep)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    // string online = IsOnline().Result;

                    ws = new System.Net.WebSockets.ClientWebSocket();

                    // byte[] byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", ApplicationSettings.Default.TeslaName, Tesla_Streamingtoken));
                    Uri serverUri = new Uri($"wss://streaming.vn.teslamotors.com/streaming/");

                    string connectmsg = "{\n" +
                        "    \"msg_type\": \"data:subscribe_oauth\",\n" +
                        "    \"token\": \"" + Tesla_token + "\",\n" +
                        "    \"tag\": \"" + Tesla_vehicle_id + "\",\n" +
                        "    \"value\": \"speed,odometer,soc,elevation,est_heading,est_lat,est_lng,power,shift_state,range,est_range,heading\"\n" +
                        "}";


                    Task result = ws.ConnectAsync(serverUri, CancellationToken.None);

                    while (!stopStreaming && ws.State == System.Net.WebSockets.WebSocketState.Connecting)
                    {
                        System.Diagnostics.Debug.WriteLine("Connecting");
                        Thread.Sleep(1000);
                    }

                    ArraySegment<byte> bufferPing = new ArraySegment<byte>(Encoding.ASCII.GetBytes("PING"));
                    ArraySegment<byte> bufferMSG = new ArraySegment<byte>(Encoding.ASCII.GetBytes(connectmsg));

                    if (!stopStreaming && ws.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        ws.SendAsync(bufferMSG, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                    }

                    while (!stopStreaming && ws.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        Thread.Sleep(100);
                        var cts = new CancellationTokenSource(10000);
                        try
                        {
                            Array.Clear(buffer, 0, buffer.Length);
                            Task<System.Net.WebSockets.WebSocketReceiveResult> response = ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                            response.Wait();
                            cts.Dispose();
                            cts = null;

                            resultContent = Encoding.UTF8.GetString(buffer);

                            if (!String.IsNullOrEmpty(resultContent))
                            {
                                resultContent = resultContent.Trim('\0');
                                // System.Diagnostics.Debug.WriteLine("Stream: " + resultContent);

                                dynamic j = JsonConvert.DeserializeObject(resultContent);

                                string msg_type = j["msg_type"];

                                switch (msg_type)
                                {
                                    case "control:hello":
                                        // car.Log("Stream Hello");
                                        break;
                                    case "data:error":
                                        string error_type = j["error_type"];

                                        if (error_type == "vehicle_disconnected")
                                        {
                                            throw new Exception("vehicle_disconnected");
                                        }
                                        else if (error_type == "vehicle_error")
                                        {
                                            string v = j["value"];
                                            if (v == "Vehicle is offline")
                                            {
                                                throw new Exception("Vehicle is offline");
                                            }
                                            else
                                            {
                                                car.Log("Stream Data Error: " + resultContent);
                                                throw new Exception("unhandled vehicle_error: " + v);
                                            }
                                        }
                                        else if (error_type == "client_error")
                                        {
                                            string v = j["value"];
                                            if (v.Contains("Can't validate token"))
                                            {
                                                Tools.DebugLog("StreamingAPI: " + v);
                                                car.Log("StreamingApi: " + v);

                                                // Suspend Streaming API
                                                var lastToken = Tesla_Streamingtoken;
                                                var lastTeslaToken = Tesla_token;
                                                var TimeOut = DateTime.UtcNow;

                                                while (!stopStreaming)
                                                {
                                                    Thread.Sleep(10000);

                                                    if (lastTeslaToken != Tesla_token)
                                                    {
                                                        car.Log("Restart Streaming because Token changed");
                                                        break;
                                                    }

                                                    var ts = DateTime.UtcNow - lastRefreshToken;
                                                    if (ts.TotalMinutes >= 5)
                                                    {
                                                        car.Log("Restart Streaming because get token lock suspended");
                                                        UpdateTeslaTokenFromRefreshToken();
                                                        break;
                                                    }

                                                    var to = DateTime.UtcNow - TimeOut;
                                                    if (ts.TotalMinutes > 10)
                                                    {
                                                        car.Log("Restart Streaming because timeout");
                                                        break;
                                                    }

                                                    if (car.GetCurrentState() == Car.TeslaState.Sleep)
                                                    {
                                                        car.Log("Restart Streaming because car is sleeping");
                                                        break;
                                                    }
                                                }
                                                car.Log("Exit streaming while loop wait for token refresh");

                                                Thread.Sleep(10000);
                                            }
                                        }
                                        else
                                        {
                                            car.Log("Stream Data Error: " + resultContent);
                                            throw new Exception("unhandled error_type: " + error_type);
                                        }
                                        break;
                                    case "data:update":
                                        string value = j["value"];
                                        StreamDataUpdate(value);
                                        break;
                                    default:
                                        car.Log("unhandled: " + resultContent);
                                        break;
                                }
                            }
                        }
                        finally
                        {
                            if (cts != null)
                            {
                                cts.Dispose();
                                cts = null;
                            }
                        }

                        Thread.Sleep(10);
                        //ws.SendAsync(bufferPing, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                        // Logfile.ExceptionWriter(null, r);
                    }


                    Log("StreamEnd");
                    System.Diagnostics.Debug.WriteLine("StreamEnd");
                }
                catch (TaskCanceledException e)
                {
                    // car.CreateExceptionlessClient(e).AddObject(resultContent, "ResultContent").Submit();
                    DrivingOrChargingByStream = false;
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    Log("Stream: Timeout");
                    Thread.Sleep(10000);
                }
                catch (AggregateException e)
                {
                    e.Handle(ex =>
                    {
                        if (ex is TaskCanceledException)
                        {
                            DrivingOrChargingByStream = false;
                            System.Diagnostics.Debug.WriteLine(e.Message);
                            Log("Stream: Timeout");
                            Thread.Sleep(10000);
                        }
                        else
                        {
                            if (!WebHelper.FilterNetworkoutage(ex))
                                car.CreateExceptionlessClient(e).AddObject(resultContent, "ResultContent").Submit();

                            Logfile.Log("Streaming Error: " + ex.Message);
                        }

                        return true;
                    });

                }
                catch (Exception ex)
                {
                    DrivingOrChargingByStream = false;

                    if (ex.Message == "vehicle_disconnected")
                    {
                        vehicleDisconnectedCounter++;

                        if ((DateTime.UtcNow - lastStreamingAPIData).TotalSeconds > 180 || vehicleDisconnectedCounter % 10 == 0)
                            car.Log("Stream Data Error: vehicle_disconnected " + vehicleDisconnectedCounter);
                    }
                    else if (ex.Message == "Vehicle is offline")
                    {
                        car.Log("Stream Data Error: Vehicle is offline");
                        Thread.Sleep(30000);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                        Logfile.Log("Streaming Error: " + ex.Message);
                        if (ex.InnerException != null)
                            Logfile.Log("Streaming Error: " + ex.InnerException.Message);

                        Logfile.ExceptionWriter(ex, line);

                        if (!WebHelper.FilterNetworkoutage(ex))
                            SubmitExceptionlessClientWithResultContent(ex, resultContent);
                    }

                    Thread.Sleep(10000);
                }
                finally
                {
                    if (ws != null)
                    {
                        ws.Abort();
                        ws.Dispose();
                    }

                    DrivingOrChargingByStream = false;
                }
            }

            Log("StartStream Ende");

        }

        string lastStreamingAPIShiftState; // defaults to null;
        DateTime lastStreamingAPILog = DateTime.UtcNow;
        DateTime lastStreamingAPIData = DateTime.UtcNow;
        int vehicleDisconnectedCounter; // defaults to 0;


        private void StreamDataUpdate(string data)
        {
            lastStreamingAPIData = DateTime.UtcNow;

            string[] v = data.Split(',');
            string speed = v[1];
            string odometer = v[2];
            string soc = v[3];
            string elevation = v[4];
            string est_heading = v[5];
            string est_lat = v[6];
            string est_lng = v[7];
            string power = v[8];
            string shift_state = v[9];
            string range = v[10];
            string est_range = v[11];
            string heading = v[12];

            DateTime dt = DBHelper.UnixToDateTime(Convert.ToInt64(v[0]));

            if (lastStreamingAPIShiftState != shift_state || (DateTime.UtcNow - lastStreamingAPILog).TotalSeconds > 30)
            {
                Log("shift_state: " + shift_state + " Power: " + power + " Datetime: " + dt.ToString(Tools.ciDeDE));
                lastStreamingAPILog = DateTime.UtcNow;
                lastStreamingAPIShiftState = shift_state;
            }

            if (int.TryParse(power, out int iPower))
            {
                if (iPower > 0 || iPower < 0)
                {
                    DrivingOrChargingByStream = true;
                }
            }
            else if (shift_state != null && (shift_state == "D" || shift_state == "R" || shift_state == "N"))
            {
                DrivingOrChargingByStream = true;
            }
            else
            {
                DrivingOrChargingByStream = false;
            }
            if (int.TryParse(speed, out int ispeed) // speed in mph
                && double.TryParse(odometer, out double dodometer) // odometer in miles
                && double.TryParse(soc, out double isoc)
                && double.TryParse(est_lat, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude)
                && double.TryParse(est_lng, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude)
                && decimal.TryParse(power, out decimal dpower) // power in kW
                && int.TryParse(range, out int irange))
            {
                // speed is converted by InsertPos
                // power is converted by InsertPos
                double dodometer_km = Tools.MlToKm(dodometer, 3);
                // battery_range_km = range in ml to km
                double battery_range_km = Tools.MlToKm(irange, 1);
                // ideal_battery_range_km = ideal_battery_range_km * car specific factor
                double ideal_battery_range_km = battery_range_km * battery_range2ideal_battery_range;
                double? outside_temp = car.CurrentJSON.current_outside_temperature;
                if (!string.IsNullOrEmpty(shift_state) && shift_state.Equals("D") &&
                    (latitude != last_latitude_streaming || longitude != last_longitude_streaming || dpower != last_power_streaming))
                {
                    last_latitude_streaming = latitude;
                    last_longitude_streaming = longitude;
                    last_power_streaming = dpower;

                    //Tools.DebugLog($"Stream: InsertPos({v[0]}, {latitude}, {longitude}, {ispeed}, {dpower}, {dodometer_km}, {ideal_battery_range_km}, {battery_range_km}, {isoc}, {outside_temp}, String.Empty)");
                    car.DbHelper.InsertPos(v[0], latitude, longitude, ispeed, dpower, dodometer_km, ideal_battery_range_km, battery_range_km, isoc, outside_temp, String.Empty);
                }
            }
            if (int.TryParse(heading, out int iheading)) {  // heading in degrees
                car.CurrentJSON.heading = iheading;
            }
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

                object jsonResult = JsonConvert.DeserializeObject(resultContent);
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

        internal static async Task<string> ReverseGecocodingAsync(Car c, double latitude, double longitude, bool forceGeocoding = false, bool insertGeocodecache = true)
        {
            string url = "";
            string resultContent = "";
            try
            {
                if (!forceGeocoding)
                {
                    Address a = null;
                    a = Geofence.GetInstance().GetPOI(latitude, longitude);
                    if (a != null)
                    {
                        Logfile.Log("Reverse geocoding by Geofence");
                        return a.name;
                    }

                    string value = GeocodeCache.Search(latitude, longitude);
                    if (!string.IsNullOrEmpty(value))
                    {
                        Logfile.Log("Reverse geocoding by GeocodeCache");
                        return value;
                    }
                }

                Tools.SetThreadEnUS();

                Thread.Sleep(5000); // Sleep to not get banned by Nominatim

                using (WebClient webClient = new WebClient())
                {

                    webClient.Headers.Add("User-Agent: TL 1.1");
                    webClient.Encoding = Encoding.UTF8;

                    url = !string.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey)
                        ? "http://www.mapquestapi.com/geocoding/v1/reverse"
                        : "http://nominatim.openstreetmap.org/reverse";



                    if (!string.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                    {
                        url += "?location=";
                        url += latitude.ToString();
                        url += ",";
                        url += longitude.ToString();
                        url += "&key=";
                        url += ApplicationSettings.Default.MapQuestKey;
                    }
                    else
                    {
                        url += "?format=jsonv2&lat=";
                        url += latitude.ToString();
                        url += "&lon=";
                        url += longitude.ToString();
                        url += "&email=mail";
                        url += "@";
                        url += "teslalogger";
                        url += ".de";
                    }

                    DateTime start = DateTime.UtcNow;
                    resultContent = await webClient.DownloadStringTaskAsync(new Uri(url));
                    DBHelper.AddMothershipDataToDB("ReverseGeocoding", start, 0, c.CarInDB);

                    dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                    string adresse = "";

                    if (!string.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                    {
                        dynamic res = jsonResult["results"];
                        dynamic res0 = res[0];
                        dynamic loc = res0["locations"];
                        dynamic loc0 = loc[0];
                        string postcode = "";

                        if (loc0.ContainsKey("postalCode"))
                            postcode = loc0["postalCode"].ToString();

                        string country_code = "";

                        if (loc0.ContainsKey("adminArea1") && loc0["adminArea1Type"].ToString() == "Country")
                            country_code = loc0["adminArea1"].ToString().ToLower();

                        if (country_code.Length > 0 && c != null)
                        {
                            c.CurrentJSON.current_country_code = country_code;
                            c.CurrentJSON.current_state = loc0.ContainsKey("adminArea3") ? loc0["adminArea3"].ToString() : "";
                        }

                        string road = "";
                        if (loc0.ContainsKey("street"))
                        {
                            road = loc0["street"].ToString();

                            try
                            {
                                if (country_code != "us")
                                    road = Regex.Replace(road, "^([0-9]+)?\\s?(.+)", "$2 $1").Trim(); // swap house number
                            }
                            catch (Exception ex)
                            {
                                ex.ToExceptionless().FirstCarUserID().AddObject(road, "road").Submit();
                            }
                        }


                        string city = "";

                        if (loc0.ContainsKey("adminArea5"))
                            city = loc0["adminArea5"].ToString();

                        if (country_code != "de")
                        {
                            adresse += country_code + "-";
                        }

                        adresse += postcode + " " + city + ", " + road;

                        System.Diagnostics.Debug.WriteLine("MapquestGeocode: " + adresse);

                    }
                    else
                    {
                        dynamic r2 = jsonResult["address"];
                        string postcode = "";
                        if (r2.ContainsKey("postcode"))
                            postcode = r2["postcode"].ToString();

                        string country_code = "";

                        if (r2.ContainsKey("country_code"))
                            country_code = r2["country_code"].ToString();

                        if (country_code.Length > 0 && c != null)
                        {
                            c.CurrentJSON.current_country_code = country_code;
                            c.CurrentJSON.current_state = r2.ContainsKey("state") ? r2["state"].ToString() : "";
                        }

                        string road = "";
                        if (r2.ContainsKey("road"))
                            road = r2["road"].ToString();

                        string city = "";
                        if (r2.ContainsKey("city"))
                            city = r2["city"].ToString();
                        else if (r2.ContainsKey("town"))
                            city = r2["town"].ToString();
                        else if (r2.ContainsKey("village"))
                            city = r2["village"].ToString();

                        string house_number = "";
                        if (r2.ContainsKey("house_number"))
                            house_number = r2["house_number"].ToString();

                        string name = "";
                        if (r2.ContainsKey("name") && r2["name"] != null)
                            name = r2["name"].ToString();

                        string address29 = "";
                        if (r2.ContainsKey("address29") && r2["address29"] != null)
                        {
                            address29 = r2["address29"].ToString();
                        }

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
                    }

                    System.Diagnostics.Debug.WriteLine(url + "\r\n" + adresse);

                    if (insertGeocodecache)
                    {
                        GeocodeCache.Insert(latitude, longitude, adresse);
                    }

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
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().AddObject(resultContent, "ResultContent").AddObject(url, "Url").Submit();

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

        public static async Task<string> ReverseGecocodingCountryAsync(double latitude, double longitude)
        {
            string url = "";
            string resultContent = "";
            try
            {
                Tools.SetThreadEnUS();

                Thread.Sleep(5000); // Sleep to not get banned by Nominatim

                using (WebClient webClient = new WebClient())
                {

                    webClient.Headers.Add("User-Agent: TL 1.1");
                    webClient.Encoding = Encoding.UTF8;

                    url = "http://nominatim.openstreetmap.org/reverse";

                    url += "?format=jsonv2&lat=";
                    url += latitude.ToString();
                    url += "&lon=";
                    url += longitude.ToString();
                    url += "&email=mail";
                    url += "@";
                    url += "teslalogger";
                    url += ".de";

                    DateTime start = DateTime.UtcNow;
                    resultContent = await webClient.DownloadStringTaskAsync(new Uri(url));
                    DBHelper.AddMothershipDataToDB("ReverseGeocoding", start, 0, 0);

                    dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);

                    dynamic r2 = jsonResult["address"];

                    string country_code = "";

                    if (r2.ContainsKey("country_code"))
                    {
                        country_code = r2["country_code"].ToString();
                        return country_code;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().AddObject(resultContent, "ResultContent").AddObject(url, "Url").Submit();

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
                using (SqlCommand cmd = new SqlCommand("Select lat, lng, id from pos where address = ''", con))
                {
                    SqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        Thread.Sleep(10000); // Sleep to not get banned by Nominatim !

                        double lat = (double)dr[0];
                        double lng = (double)dr[1];
                        int id = (int)dr[2];
                        Task<string> adress = ReverseGecocodingAsync(car, lat, lng);
                        //var altitude = AltitudeAsync(lat, lng);
                        //UpdateAddressByPosId(id, adress.Result, altitude.Result);
                        UpdateAddressByPosId(id, adress.Result, 0);
                    }
                }
            }
        }

        private static void UpdateAddressByPosId(int id, string address, double altitude)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("update pos set address=@address, altitude=@altitude where id = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@address", address);
                        cmd.Parameters.AddWithValue("@altitude", altitude);
                        _ = SQLTracer.TraceNQ(cmd, out _);

                        System.Diagnostics.Debug.WriteLine("id updateed: " + id + " address: " + address);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, "UpdateAddressByPosId");
            }
        }

        public void UpdateAllEmptyAddresses()
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"SELECT  
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
                    ((pos_end.odometer - pos_start.odometer) > 0.1) and (pos_start.address IS null or pos_end.address IS null or pos_start.address = '' or pos_end.address = '')", con))
                {

                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
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
                                Task<string> address = ReverseGecocodingAsync(car, lat, lng);
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
                                Task<string> address = ReverseGecocodingAsync(car, lat, lng);
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
                            car.CreateExceptionlessClient(ex).Submit();
                            ExceptionWriter(ex, "");
                        }
                    }
                }
            }

            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"SELECT pos.id, lat, lng FROM chargingstate join pos on chargingstate.Pos = pos.id where address IS null OR address = '' or pos.id = ''", con))
                {

                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    while (dr.Read())
                    {
                        Thread.Sleep(10000); // Sleep to not get banned by Nominatim !
                        try
                        {
                            int id = (int)dr[0];
                            double lat = (double)dr[1];
                            double lng = (double)dr[2];
                            Task<string> address = ReverseGecocodingAsync(car, lat, lng);
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
                            car.CreateExceptionlessClient(ex).Submit();
                            ExceptionWriter(ex, "");
                        }
                    }
                }
            }
        }

        public static void UpdateAllPOIAddresses()
        {
            try
            {
                if (Geofence.GetInstance().RacingMode)
                {
                    return;
                }

                int t = Environment.TickCount;
                int count = 0;
                Logfile.Log("UpdateAllPOIAddresses start");

                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();

                    using (MySqlCommand cmdBucket = new MySqlCommand(@"
SELECT DISTINCT
    Pos
FROM
    chargingstate
UNION DISTINCT
SELECT
    StartPos
FROM
    drivestate
UNION DISTINCT
SELECT
    EndPos
FROM
    drivestate
ORDER BY
    Pos
DESC", con))
                    {
                        Tools.DebugLog(cmdBucket);
                        var bucketdr = SQLTracer.TraceDR(cmdBucket);
                        var loop = true;

                        do
                        {
                            StringBuilder bucket = new StringBuilder();
                            for (int x = 0; x < 100; x++)
                            {
                                if (!bucketdr.Read())
                                {
                                    loop = false;
                                    break;
                                }

                                if (bucket.Length > 0)
                                    bucket.Append(",");

                                string posid = bucketdr[0].ToString();
                                bucket.Append(posid);
                            }

                            count = UpdateAllPOIAddresses(count, bucket.ToString());
                        }
                        while (loop);
                    }


                    t = Environment.TickCount - t;
                    Logfile.Log($"UpdateAllPOIAddresses end {t}ms count:{count}");
                }
            }
            catch (MySql.Data.MySqlClient.MySqlException mex)
            {
                Tools.DebugLog(mex.ToString());
                Tools.DebugLog("SQLState: <" + mex.SqlState + ">");
                foreach (var key in mex.Data.Keys)
                {
                    Tools.DebugLog("SQL Data key:<" + key + "> value:<" + mex.Data[key] + ">");
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        internal static int UpdateAllPOIAddresses(int count, string bucket)
        {
            if (bucket.Length == 0)
                return count;

            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(@"Select lat, lng, pos.id, address, fast_charger_brand, max_charger_power 
                        from pos    
                        left join chargingstate on pos.id = chargingstate.pos
                        where pos.id in (" + MySql.Data.MySqlClient.MySqlHelper.EscapeString(bucket) + ")", con))
                {
                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);

                    while (dr.Read())
                    {
                        count = UpdatePOIAdress(count, dr);
                    }
                }
            }

            return count;
        }

        internal void UpdateLastChargingAdress()
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"Select lat, lng, pos.id, address, fast_charger_brand, max_charger_power 
                        from chargingstate join pos on pos.id = chargingstate.pos
                        where chargingstate.CarID=@CarID 
                        order by chargingstate.id desc limit 1", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);

                    MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                    int count = 0;
                    while (dr.Read())
                    {
                        count = UpdatePOIAdress(count, dr);
                    }
                }
            }
        }

        private static int UpdatePOIAdress(int count, MySqlDataReader dr)
        {
            try
            {
                Thread.Sleep(1);
                double lat = (double)dr["lat"];
                double lng = (double)dr["lng"];
                int id = (int)dr["id"];
                string brand = dr["fast_charger_brand"] as String ?? "";
                int max_power = dr["max_charger_power"] as int? ?? 0;
                object name = dr[3];

                Address a = Geofence.GetInstance().GetPOI(lat, lng, false, brand, max_power);
                if (a == null)
                {
                    if (name == DBNull.Value || name.ToString().Length == 0)
                    {
                        String newName = ReverseGecocodingAsync(null, lat, lng, true, true).Result;
                        if (newName != null && newName.Length > 0)
                        {
                            UpdatePosAddressName(id, newName);
                            count++;
                        }
                        else
                            DBHelper.UpdateAddress(null, id);
                    }
                    return count;
                }

                if (name == DBNull.Value || a.name != name.ToString())
                {
                    count++;
                    UpdatePosAddressName(id, a.name);
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(" Exception in UpdateAllPOIAddresses: " + ex.Message);
            }
            return count;
        }

        private static void UpdatePosAddressName(int id, string addressname)
        {
            using (MySqlConnection con2 = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con2.Open();
                using (MySqlCommand cmd2 = new MySqlCommand("update pos set address=@address where id = @id", con2))
                {
                    cmd2.Parameters.AddWithValue("@id", id);
                    cmd2.Parameters.AddWithValue("@address", addressname);
                    _ = SQLTracer.TraceNQ(cmd2, out _);
                }
            }
        }

        private double GetIdealBatteryRangekm(out double battery_level, out double battery_range_km)
        {
            string resultContent = "";
            battery_level = -1;
            battery_range_km = -1;

            try
            {
                // resultContent = GetCommand("charge_state").Result;
                // resultContent = GetCommand("vehicle_data?endpoints=charge_state&let_sleep=true").Result;
                resultContent = GetCommand(vehicle_data_everything).Result;

                if (resultContent == null || resultContent == "NULL")
                    return -1;

                Tools.SetThreadEnUS();
                dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                dynamic r2 = jsonResult["response"]["charge_state"];

                if (r2["ideal_battery_range"] == null)
                {
                    return -1;
                }

                decimal ideal_battery_range = (decimal)r2["ideal_battery_range"];
                if (ideal_battery_range == 999)
                {
                    ideal_battery_range = (decimal)r2["battery_range"];
                    if (!car.Raven)
                    {
                        car.Raven = true;
                        car.WriteSettings();
                        Log("Raven Model!");
                    }
                }

                if (r2["battery_range"] != null)
                {
                    battery_range_km = Tools.MlToKm(Convert.ToDouble(r2["battery_range"]), 1);
                }

                if (r2["battery_level"] != null)
                {
                    battery_level = Convert.ToDouble(r2["battery_level"]);
                    car.CurrentJSON.current_battery_level = battery_level;
                }
                battery_range2ideal_battery_range = (double)ideal_battery_range / Convert.ToDouble(r2["battery_range"]);
                return Tools.MlToKm((double)ideal_battery_range, 1);
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException)
                {
                    Log("GetIdealBatteryRangekm: TaskCanceledException");
                    Thread.Sleep(1000);
                }
                else
                {
                    SubmitExceptionlessClientWithResultContent(ex, resultContent);
                    ExceptionWriter(ex, resultContent);
                }
            }
            return -1;
        }

        internal async Task<double> GetOdometerAsync()
        {
            string resultContent = "";
            try
            {
                // resultContent = await GetCommand("vehicle_state");
                resultContent = await GetCommand(vehicle_data_everything);

                Tools.SetThreadEnUS();

                if (resultContent == null || resultContent == "NULL" || resultContent == INSERVICE)
                    return lastOdometerKM;

                dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                dynamic vehicle_state = jsonResult["response"]["vehicle_state"];
                _ = long.TryParse(vehicle_state["timestamp"].ToString(), out long ts);

                if (vehicle_state.ContainsKey("sentry_mode") && vehicle_state["sentry_mode"] != null)
                {
                    try
                    {
                        bool sentry_mode = (bool)vehicle_state["sentry_mode"];
                        if (sentry_mode != is_sentry_mode)
                        {
                            is_sentry_mode = sentry_mode;
                            Log("sentry_mode: " + sentry_mode);
                        }

                        car.CurrentJSON.current_is_sentry_mode = sentry_mode;
                    }
                    catch (Exception ex)
                    {
                        if (resultContent != null)
                            SubmitExceptionlessClientWithResultContent(ex, resultContent);

                        ExceptionWriter(ex, resultContent);
                        Log(ex.Message);
                    }
                }

                if (vehicle_state["odometer"] == null)
                {
                    Log("odometer = NULL");
                    return lastOdometerKM;
                }

                double odometer = (double)vehicle_state["odometer"];


                try
                {
                    string car_version = vehicle_state["car_version"].ToString();
                    if (car.CurrentJSON.current_car_version != car_version)
                    {
                        Log("Car Version: " + car_version);
                        car.CurrentJSON.current_car_version = car_version;

                        car.DbHelper.SetCarVersion(car_version);

                        TaskerWakeupfile(true);
                    }
                }
                catch (Exception ex)
                {
                    SubmitExceptionlessClientWithResultContent(ex, resultContent);
                    Log(ex.ToString());
                }

                lastOdometerKM = Tools.MlToKm(odometer, 3);
                return lastOdometerKM;
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException)
                {
                    Log("GetOdometerAsync: TaskCanceledException");
                    Thread.Sleep(1000);
                }
                else
                {
                    SubmitExceptionlessClientWithResultContent(ex, resultContent);
                    ExceptionWriter(ex, resultContent);
                }

                return lastOdometerKM;
            }
            //return 0;
        }

        internal async Task<double?> GetOutsideTempAsync()
        {
            string cacheKey = Program.TLMemCacheKey.GetOutsideTempAsync.ToString() + car.CarInDB;
            object cacheValue = MemoryCache.Default.Get(cacheKey);
            if (cacheValue != null)
            {
                return (double)cacheValue;
            }

            string resultContent = null;
            try
            {
                // resultContent = await GetCommand("climate_state");

                resultContent = GetCommand(vehicle_data_everything).Result;

                if (resultContent == null || resultContent.Length == 0 || resultContent == "NULL")
                {
                    Log("GetOutsideTempAsync: NULL");
                    return null;
                }

                Tools.SetThreadEnUS();
                dynamic jsonResult = JsonConvert.DeserializeObject(resultContent);
                dynamic climate_state = jsonResult["response"]["climate_state"];
                _ = long.TryParse(climate_state["timestamp"].ToString(), out long ts);
                try
                {
                    if (climate_state["inside_temp"] != null)
                    {
                        car.CurrentJSON.current_inside_temperature = Convert.ToDouble(climate_state["inside_temp"]);
                    }
                }
                catch (Exception) { }

                decimal? outside_temp = null;
                if (climate_state["outside_temp"] != null)
                {
                    outside_temp = (decimal)climate_state["outside_temp"];
                    car.CurrentJSON.current_outside_temperature = (double)outside_temp;
                }
                else
                {
                    return null;
                }

                try
                {
                    bool? battery_heater = null;
                    if (climate_state["battery_heater"] != null)
                    {
                        battery_heater = (bool)climate_state["battery_heater"];
                        if (car.CurrentJSON.current_battery_heater != battery_heater)
                        {
                            car.CurrentJSON.current_battery_heater = (bool)battery_heater;

                            Log("Battery heater: " + battery_heater);
                            car.CurrentJSON.CreateCurrentJSON();

                            // write into Database
                            Thread.Sleep(5000);
                            IsDriving(true);
                            Thread.Sleep(5000);
                        }
                    }
                }
                catch (Exception) { }


                bool preconditioning = climate_state["is_preconditioning"] != null && (bool)climate_state["is_preconditioning"];
                if (preconditioning != car.CurrentJSON.current_is_preconditioning)
                {
                    car.CurrentJSON.current_is_preconditioning = preconditioning;
                    Log("Preconditioning: " + preconditioning);
                    car.CurrentJSON.CreateCurrentJSON();

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
                    Log("GetOutsideTempAsync: NULL");
                    return null;
                }
                else if (ex is TaskCanceledException)
                {
                    Log("GetOutsideTempAsync: TaskCanceledException");
                }
                else if (!resultContent.Contains("upstream internal error"))
                {
                    SubmitExceptionlessClientWithResultContent(ex, resultContent);
                    ExceptionWriter(ex, resultContent);
                }
            }
            return null;
        }

        public async Task<string> GetCommand(string cmd, bool noMemcache = false)
        {
            if (car.FleetAPI)
            {
                Log("*** FleetAPI no Datacalls allowed! ***");
                return "";
            }


            string resultContent = "";
            try
            {
                string cacheKey = "GetCommand_" + cmd + "_" + cacheGUID;

                string cachedValue = MemoryCache.Default[cacheKey] as string;
                if (cachedValue != null)
                {
                    // Log("GetCommand Cache");
                    return cachedValue;
                }

                string cacheKeyNotFound = "HttpNotFoundCounter_" + cmd + "_" + cacheGUID;
                HttpClient httpClientTeslaAPI = GetHttpClientTeslaAPI();

                string adresse = apiaddress + "api/1/vehicles/" + Tesla_id + "/" + cmd;

                DateTime start = DateTime.UtcNow;
                using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(adresse)))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Tesla_token);
                    Tools.DebugLog($"GetCommand #{car.CarInDB} request: {adresse}");
                    HttpResponseMessage result = await httpClientTeslaAPI.SendAsync(request);

                    car.Log("Command: " + cmd + " [" + commandCounter + "]");

                    if (result.IsSuccessStatusCode)
                    {
                        startRequestTimeout = null;
                        MemoryCache.Default.Remove(cacheKeyNotFound);

                        resultContent = await result.Content.ReadAsStringAsync();
                        //Tools.DebugLog($"GetCommand request: {adresse} result: {new Tools.JsonFormatter(resultContent).Format()}");

                        if (cmd.Contains("vehicle_data") && noMemcache == false)
                        {
                            MemoryCache.Default.Add(cacheKey, resultContent, DateTime.Now.AddSeconds(4));
                        }

                        DBHelper.AddMothershipDataToDB("GetCommand(" + cmd + ")", start, (int)result.StatusCode, car.CarInDB);
                        _ = car.GetTeslaAPIState().ParseAPI(resultContent, cmd);
                        if (TeslaAPI_Commands.ContainsKey(cmd))
                        {
                            TeslaAPI_Commands.TryGetValue(cmd, out string old_value);
                            TeslaAPI_Commands.TryUpdate(cmd, resultContent, old_value);
                        }
                        else
                        {
                            TeslaAPI_Commands.TryAdd(cmd, resultContent);
                        }

                        /*
                        if (result.Headers.TryGetValues("ratelimit-remaining", out var v2))
                            Log("ratelimit-remaining: " + v2.First());
                        */

                        ResetCommandCounterEveryDay();

                        commandCounter++;
                        KVS.InsertOrUpdate($"commandCounter_{car.CarInDB}", commandCounter);

                        switch (car.GetCurrentState())
                        {
                            case TeslaState.Drive:
                                commandCounterDrive++;
                                KVS.InsertOrUpdate($"commandCounterDrive_{car.CarInDB}", commandCounterDrive);
                                break;
                            case TeslaState.Charge:
                                commandCounterCharging++;
                                KVS.InsertOrUpdate($"commandCounterCharging_{car.CarInDB}", commandCounterCharging);
                                break;
                            default:
                                commandcounterOnline++;
                                KVS.InsertOrUpdate($"commandCounterOnline_{car.CarInDB}", commandcounterOnline);
                                break;
                        }

                        if (commandCounter % 100 == 0)
                            Log("Command counter: " + commandCounter);

                        return resultContent;
                    }

                    if (cmd.Contains("vehicle_data") && noMemcache == false && result.StatusCode == HttpStatusCode.RequestTimeout)
                    {
                        MemoryCache.Default.Add(cacheKey, "NULL", DateTime.Now.AddSeconds(15));
                    }

                    DBHelper.AddMothershipDataToDB("GetCommand(" + cmd + ")", double.Parse("-1." + (int)result.StatusCode, Tools.ciEnUS), (int)result.StatusCode, car.CarInDB);
                    if (result.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        LoginRetry(result);
                    }
                    else if (result.StatusCode == HttpStatusCode.MethodNotAllowed)
                    {
                        if (car.IsInService())
                        {
                            return INSERVICE;
                        }
                        else
                        {
                            Log("Result.Statuscode: " + (int)result.StatusCode + " (" + result.StatusCode.ToString() + ") cmd: " + cmd);
                        }
                    }
                    else if (result.StatusCode == HttpStatusCode.RequestTimeout)
                    {
                        if (startRequestTimeout == null)
                            startRequestTimeout = DateTime.UtcNow;

                        Log("Result.Statuscode: " + (int)result.StatusCode + " (" + result.StatusCode.ToString() + ") cmd: " + cmd);
                        Thread.Sleep(1000);
                    }
                    else if (result.StatusCode == HttpStatusCode.NotFound)
                    {
                        int HttpNotFoundCounter = (int)(MemoryCache.Default.Get(cacheKeyNotFound) ?? 0);
                        HttpNotFoundCounter++;
                        MemoryCache.Default.Set(cacheKeyNotFound, HttpNotFoundCounter, DateTime.Now.AddMinutes(10));

                        Log("Result.Statuscode: " + (int)result.StatusCode + " (" + result.StatusCode.ToString() + ") cmd: " + cmd + " Retry: " + HttpNotFoundCounter);

                        Thread.Sleep(1000);

                        if (HttpNotFoundCounter > 5)
                        {
                            car.CreateExeptionlessLog("WebHelper", $"404 Error ({cmd}) -> Restart Car Thread", Exceptionless.Logging.LogLevel.Warn).Submit();
                            car.Restart("404 Error", 0);
                        }
                    }
                    else if ((int)result.StatusCode == 429) // TooManyRequests
                    {
                        // Retry-After: time in seconds
                        Tools.DebugLog($"429: response Retry-After:{result.Headers.RetryAfter}");
                        if (int.TryParse(result.Headers.RetryAfter.ToString(), out int sleep))
                        {
                            sleep = sleep * 1000;
                        }
                        else
                        {
                            sleep = (random.Next(5000) + 60000) * 1000;
                        }
                        string l1 = "Result.Statuscode: " + (int)result.StatusCode + " (" + result.StatusCode.ToString() + ") cmd: " + cmd + " Sleep: " + sleep + "ms";
                        string l = "";

                        if (result.Headers.TryGetValues("ratelimit-limit", out var v))
                            l += ", ratelimit-limit: " + v.First();

                        if (result.Headers.TryGetValues("ratelimit-remaining", out var v2))
                            l += ", ratelimit-remaining: " + v2.First();

                        if (result.Headers.TryGetValues("ratelimit-reset", out var v3))
                            l += ", ratelimit-reset: " + v3.First();

                        l += ", sleep till: " + DateTime.Now.AddMilliseconds(sleep).ToString(Tools.ciDeDE);

                        l += $", CommandCounter: {commandCounter} Drive: {commandCounterDrive} Charge: {commandCounterCharging} Online: {commandcounterOnline}";

                        if (car.FleetAPI)
                            l += ", FleetAPI";
                        else
                            l += ", OwnersAPI";

                        car.CreateExeptionlessLog("TooManyRequests", l, LogLevel.Warn).Submit();

                        Log(l1 + l);
                        car.CurrentJSON.FatalError += " TooManyRequests";
                        car.CurrentJSON.CreateCurrentJSON();
                        Thread.Sleep(sleep);
                        car.CurrentJSON.FatalError = car.CurrentJSON.FatalError.Replace(" TooManyRequests", "");
                        car.CurrentJSON.CreateCurrentJSON();
                    }
                    else
                    {
                        Log("Result.Statuscode: " + (int)result.StatusCode + " (" + result.StatusCode.ToString() + ") cmd: " + cmd);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Log("Timeout: " + cmd);
            }
            catch (Exception ex)
            {
                SubmitExceptionlessClientWithResultContent(ex, resultContent);
                ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        private void ResetCommandCounterEveryDay()
        {
            if (DateTime.UtcNow.Day != commandCounterDay)
            {
                UpdateCommandConterAsync().Wait();

                commandCounterDay = DateTime.UtcNow.Day;
                Log($"Total Commands Today: {commandCounter} Drive: {commandCounterDrive} Charge: {commandCounterCharging} Online: {commandcounterOnline}");
                commandCounter = 0;
                commandCounterDrive = 0;
                commandCounterCharging = 0;
                commandcounterOnline = 0;
                KVS.InsertOrUpdate($"commandCounter_{car.CarInDB}", commandCounter);
                KVS.InsertOrUpdate($"commandCounterDrive_{car.CarInDB}", commandCounterDrive);
                KVS.InsertOrUpdate($"commandCounterCharging_{car.CarInDB}", commandCounterCharging);
                KVS.InsertOrUpdate($"commandCounterOnline_{car.CarInDB}", commandcounterOnline);
                KVS.InsertOrUpdate($"commandCounterDay{car.CarInDB}", commandCounterDay);
            }
        }

        private async Task UpdateCommandConterAsync()
        {
            try
            {
                int fleetapi = car.FleetAPI ? 1 : 0;
                HttpResponseMessage response = await  httpclient_teslalogger_de.GetAsync($"https://teslalogger.de/update-commandcounter2.php?token={car.TaskerHash}&drive={commandCounterDrive}&charging={commandCounterCharging}&online={commandcounterOnline}&fleetapi={fleetapi}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                Log($"UpdateTaskerToken: {responseBody}");
            }
            catch (HttpRequestException e)
            {
                Log($"UpdateTaskerToken error: {e.Message}");
            }
        }


        public bool LoginRetry(HttpResponseMessage result)
        {
            if (result?.StatusCode == HttpStatusCode.Unauthorized)
            {
                Log("HttpStatusCode = Unauthorized. Password changed or still valid? " + car.LoginRetryCounter);

                if (car.LoginRetryCounter < 32)
                {
                    System.Threading.Thread.Sleep(60000 + 30000 * car.LoginRetryCounter);

                    car.LoginRetryCounter++;

                    string tempToken = UpdateTeslaTokenFromRefreshToken();

                    return true;
                }
                else
                {
                    car.CurrentJSON.current_state = "ERROR: Login retries exeeded";
                    car.ExitCarThread("Login retries exeeded!");
                }
            }
            return false;
        }

        // for classic Owner API
        public string GetNearbyChargingSitesOwnerAPI()
        {
            string resultContent = "";
            try
            {
                resultContent = GetCommand("nearby_charging_sites").Result;
                return resultContent;
            }
            catch (Exception ex)
            {
                // SubmitExceptionlessClientWithResultContent(ex, resultContent);
                if (!WebHelper.FilterNetworkoutage(ex))
                    CreateExceptionlessClientWithResultContent(ex, resultContent).AddObject(car.GetCurrentState().ToString(), "CarState").Submit();

                car.Log(ex.Message);
                Thread.Sleep(30000);
            }

            return "NULL";
        }



        public async Task<string> GetChargingHistoryV2(int pageNumber = 1) => await GetChargingHistoryV2(null, pageNumber);

        public async Task<string> GetChargingHistoryV2(string vin, int pageNumber = 1)
        {
            string resultContent = "";
            try
            {
                HttpClient httpclientgetChargingHistoryV2 = GethttpclientgetChargingHistoryV2();
                using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{apiaddress}api/1/dx/charging/history?pageNo={pageNumber}{(!string.IsNullOrEmpty(vin)?"&vin="+vin:"")}")))
                {
                    Tools.DebugLog($"GetChargingHistoryV2 #{car.CarInDB} request: {request.RequestUri}");
                    request.Headers.Add("Authorization", "Bearer " + Tesla_token);
                    // xxx request.Content = new StringContent("");
                    if (apiaddress.StartsWith("https://") && apiaddress.EndsWith("/"))
                    {
                        request.Headers.Host = apiaddress.Replace("https://", "").Replace("/", "");
                    }
                    // request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    DateTime start = DateTime.UtcNow;
                    HttpResponseMessage result = await httpclientgetChargingHistoryV2.SendAsync(request);
                    resultContent = await result.Content.ReadAsStringAsync();
                    DBHelper.AddMothershipDataToDB("GetChargingHistoryV2", start, (int)result.StatusCode, car.CarInDB);

                    if (!result.IsSuccessStatusCode)
                    {
                        car.webhelper.getChargingHistoryV2Fail++;
                        throw new Exception("GetChargingHistoryV2: " + result.StatusCode.ToString() + " CarState: " + car.GetCurrentState().ToString() + " (OK: " + car.webhelper.getChargingHistoryV2OK + " - Fail: " + car.webhelper.getChargingHistoryV2Fail + ")");
                    }
                    getChargingHistoryV2OK++;
                    return resultContent;
                }
            }
            catch (Exception ex)
            {
                // SubmitExceptionlessClientWithResultContent(ex, resultContent);
                if (!WebHelper.FilterNetworkoutage(ex))
                    CreateExceptionlessClientWithResultContent(ex, resultContent).AddObject(car.GetCurrentState().ToString(), "CarState").Submit();

                car.Log(ex.Message);
            }
            return "{}";
        }

        public async Task<byte[]> GetChargingHistoryInvoicePDF(string contentId)
        {
            byte[] PDF = null;
            try
            {
                HttpClient httpclientgetChargingHistoryIonvoicePDF = GethttpclientgetChargingHistoryV2();
                using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{apiaddress}api/1/dx/charging/invoice/{contentId}")))
                {
                    Tools.DebugLog($"GetChargingHistoryInvoicePDF #{car.CarInDB} request: {request.RequestUri}");
                    request.Headers.Add("Authorization", "Bearer " + Tesla_token);
                    // xxx request.Content = new StringContent("");
                    if (apiaddress.StartsWith("https://") && apiaddress.EndsWith("/"))
                    {
                        request.Headers.Host = apiaddress.Replace("https://", "").Replace("/", "");
                    }
                    // request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    DateTime start = DateTime.UtcNow;
                    HttpResponseMessage result = await httpclientgetChargingHistoryIonvoicePDF.SendAsync(request);
                    PDF = await result.Content.ReadAsByteArrayAsync();
                    DBHelper.AddMothershipDataToDB("GetChargingHistoryInvoicePDF", start, (int)result.StatusCode, car.CarInDB);

                    if (!result.IsSuccessStatusCode)
                    {
                        PDF = null;
                        throw new Exception("GetChargingHistoryInvoicePDF: " + result.StatusCode.ToString() + " CarState: " + car.GetCurrentState().ToString() + " (OK: " + car.webhelper.getChargingHistoryV2OK + " - Fail: " + car.webhelper.getChargingHistoryV2Fail + ")");
                    }
                    return PDF;
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.Message);
            }
            return PDF;
        }

        public async Task<string> PostCommand(string cmd, string data, bool _json = false)
        {
            bool proxyServer = car.UseCommandProxyServer();

            Log("PostCommand: " + cmd + " - " + data);
            string cacheKey = "PostCommand" + car.CarInDB;
            object cacheValue = MemoryCache.Default.Get(cacheKey);
            // prevent parallel execution of command
            while (cacheValue != null)
            {
                Log($"waiting ... another command is still running: {cacheValue.ToString()}");
                Thread.Sleep(1000);
                cacheValue = MemoryCache.Default.Get(cacheKey);
            }
            MemoryCache.Default.Add(cacheKey, cmd, DateTime.Now.AddSeconds(2.5));
            string resultContent = "";
            try
            {
                HttpClient httpClientTeslaAPI = GetHttpClientTeslaAPI();

                string url = apiaddress + "api/1/vehicles/" + Tesla_id + "/" + cmd;

                if (proxyServer)
                {
                    car.Log("Use ProxyServer");
                    url = ApplicationSettings.Default.TeslaHttpProxyURL + "/api/1/vehicles/" + car.Vin + "/" + cmd;
                }
                else if (car.FleetAPI) // pre 2021 Model S / X
                {
                    // Maybe we neet to use the fleet telemetry server in future. Now it seems to work fine.
                }

                DateTime start = DateTime.UtcNow;
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, new Uri(url)))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Tesla_token);
                    if (_json)
                    {
                        request.Content = new StringContent(data);
                    }
                    else
                    {
                        request.Content = new StringContent("{}");
                    }
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    HttpResponseMessage result = await httpClientTeslaAPI.SendAsync(request);
                    resultContent = await result.Content.ReadAsStringAsync();
                    DBHelper.AddMothershipDataToDB("PostCommand(" + cmd + ")", start, (int)result.StatusCode, car.CarInDB);
                    int position = cmd.LastIndexOf('/');
                    if (position > -1)
                    {
                        string command = cmd.Substring(position + 1);
                        if (TeslaAPI_Commands.ContainsKey(command))
                        {
                            TeslaAPI_Commands.TryGetValue(command, out string drive_state);
                            TeslaAPI_Commands.TryUpdate(command, resultContent, drive_state);
                        }
                        else
                        {
                            TeslaAPI_Commands.TryAdd(command, resultContent);
                        }
                    }
                }

                car.Log("Response: " + resultContent);

                if (resultContent != null)
                {
                    if (resultContent.Contains("vehicle rejected request: your public key has not been paired with the vehicle"))
                    {
                        car.DbHelper.UpdateCarColumn("needVirtualKey", "1");
                        car.CreateExeptionlessLog("NeedVirtualKey", "", Exceptionless.Logging.LogLevel.Warn).Submit();
                    }
                    else if (resultContent.Contains("Tesla Vehicle Command Protocol required"))
                    {
                        car.DbHelper.UpdateCarColumn("needFleetAPI", "1");
                        car.CreateExeptionlessLog("NeedFleetAPI", "", Exceptionless.Logging.LogLevel.Warn).Submit();
                    }
                    else if (resultContent.Contains("Unauthorized missing scopes"))
                    {
                        car.DbHelper.UpdateCarColumn("needCommandPermission", "1");
                        car.CreateExeptionlessLog("NeedCommandPermission", "", Exceptionless.Logging.LogLevel.Warn).Submit();
                    }
                }

                return resultContent;

            }
            catch (Exception ex)
            {
                SubmitExceptionlessClientWithResultContent(ex, resultContent);
                ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public async Task<string> Wakeup()
        {
            return await PostCommand("wake_up", "");
        }


        public static string GetCachedRollupData()
        {
            /*
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
                object jsonResult = JsonConvert.DeserializeObject(resultContent);
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
                    DBHelper.InsertPos(car, timestamp, latitude, longitude, speed, power, 0, 0, 0, 0, 0.0, "0"); // TODO: ODOMETER, ideal battery range, address
                }

                return resultContent;
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, resultContent);
            }

            */
            return "NULL";
        }

        public void StopStreaming()
        {
            if (car.FleetAPI)
                return;

            Log("Request StopStreaming");
            stopStreaming = true;
            DrivingOrChargingByStream = false;
        }

        private DateTime lastTaskerWakeupfile = DateTime.Today;
        private volatile bool stopStreaming; // defaults to false;
        public DateTime? startRequestTimeout = null;

        public bool TaskerWakeupfile(bool force = false)
        {
            try
            {
                Tools.SetThreadEnUS();

                Tools.GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin, out string Range, out _, out _, out _);

                TimeSpan ts = DateTime.Now - lastTaskerWakeupfile;

                int secBetweenTaskerWakeupFile = 20;
                if (!car.UseTaskerToken)
                    secBetweenTaskerWakeupFile = 120;

                if (!force && ts.TotalSeconds < secBetweenTaskerWakeupFile)
                {
                    return false;
                }

                //Log("Check Tasker Webservice");

                lastTaskerWakeupfile = DateTime.Now;

                string name = car.ModelName;
                if (car.Raven && !name.Contains("Raven"))
                {
                    name += " Raven";
                }

                var obfuscatedVin = car.Vin ?? "";
                if (obfuscatedVin?.Length > 11)
                    obfuscatedVin = obfuscatedVin.Substring(0, 11);

                Dictionary<string, string> d = new Dictionary<string, string>
                {
                    { "t", car.TaskerHash },
                    { "v", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() },
                    { "cv", car.CurrentJSON.current_car_version },
                    { "m", car.Model },
                    { "bt", car.Battery },
                    { "n", name },
                    { "eff", car.WhTR.ToString(Tools.ciEnUS) },
                    { "oc", vehicle_config },

                    { "db_eff", car.DBWhTR.ToString(Tools.ciEnUS)},
                    { "db_eff_cnt", car.DBWhTRcount.ToString(Tools.ciEnUS) },

                    { "pw", power },
                    { "temp", temperature },
                    { "le", length },
                    { "ln", language },

                    { "CT", car.CarType },
                    { "CST", car.CarSpecialType },
                    { "TB", car.TrimBadging },

                    { "G", Tools.GetGrafanaVersion() },

                    { "D", Tools.IsDocker() ? "1" : "0" },
                    { "SMT", Tools.UseScanMyTesla() ? "1" : "0" },
                    { "SMTs", car.DbHelper.GetScanMyTeslaSignalsLastWeek().ToString() },
                    { "SMTp", car.DbHelper.GetScanMyTeslaPacketsLastWeek().ToString() },
                    { "TR", car.DbHelper.GetAvgMaxRage().ToString() },

                    { "OS", Tools.GetOsVersion() },
                    { "OSR", Tools.GetOsRelease() },
                    { "CC", car.CurrentJSON.current_country_code },
                    { "ST", car.CurrentJSON.current_state },
                    { "UP", Tools.GetOnlineUpdateSettings().ToString() },
                    { "sumkm", car.Sumkm.ToString() },
                    { "avgkm", car.Avgkm.ToString() },
                    { "kwh100km", car.Kwh100km.ToString() },
                    { "avgsocdiff", car.Avgsocdiff.ToString() },
                    { "maxkm", car.Maxkm.ToString() },
                    { "SOC50V", ((int)car.CarVoltageAt50SOC).ToString()},
                    { "AWD" , car.AWD ? "1" : "0" },
                    { "MIC" , car.MIC ? "1" : "0" },
                    { "year" , car.Year.ToString() },
                    { "motor" , car.Motor } ,
                    { "wt" , car.wheel_type } ,
                    { "vin" , obfuscatedVin} // just the first 11 chars of the vin will be sent. The serial number is truncated!
                };

                using (FormUrlEncodedContent content = new FormUrlEncodedContent(d))
                {
                    string query = content.ReadAsStringAsync().Result;

                    DateTime start = DateTime.UtcNow;
                    Task<HttpResponseMessage> resultTask = httpclient_teslalogger_de.PostAsync("http://teslalogger.de/wakefile.php", content);

                    HttpResponseMessage result = resultTask.Result;
                    string resultContent = result.Content.ReadAsStringAsync().Result;

                    DBHelper.AddMothershipDataToDB("teslalogger.de/wakefile.php", start, (int)result.StatusCode, car.CarInDB);

                    if (resultContent.Contains("wakeupfile"))
                    {
                        try
                        {
                            string lasttaskerwakeupfilepaht = System.IO.Path.Combine(FileManager.GetExecutingPath(), "LASTTASKERWAKEUPFILE_" + car.CarInDB);
                            string ltwf = resultContent.Replace("wakeupfile", "").Trim();
                            System.IO.File.WriteAllText(lasttaskerwakeupfilepaht, ltwf);
                        }
                        catch (Exception)
                        { }

                        Log("TaskerWakeupfile available! [Webservice]" + resultContent.Replace("wakeupfile", ""));
                        if (!car.UseTaskerToken)
                        {
                            Log("Start using fast TaskerToken request!");
                            car.UseTaskerToken = true;
                        }
                        return true;
                    }
                }

            }
            catch (Exception ex)
            {
                car.CreateExceptionlessClient(ex).Submit();
                Log("TaskerWakeupToken Exception: " + ex.Message);
                ExceptionWriter(ex, "TaskerWakeupToken Exception");
                Logfile.Log("TaskerWakeupToken Exception: " + ex.ToString());
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
                System.IO.File.Delete(FileManager.GetWakeupTeslaloggerPath(car.CarInDB));
                ret = true;
            }

            return ret;
        }

        public static string GetOnlineTeslaloggerVersion()
        {
            string contents = "";

            try
            {

                using (WebClient wc = new WebClient())
                {
                    contents = wc.DownloadString("https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/Properties/AssemblyInfo.cs");
                }

                Match m = regexAssemblyVersion.Match(contents);
                string version = m.Groups[1].Value;

                return version;
            }
            catch (WebException wex)
            {
                if (!WebHelper.FilterNetworkoutage(wex))
                    wex.ToExceptionless().AddObject(contents, "ResultContent").Submit();

                return "Error during online version check: " + wex.Message;
            }
            catch (Exception ex)
            {
                if (!WebHelper.FilterNetworkoutage(ex))
                    ex.ToExceptionless().AddObject(contents, "ResultContent").Submit();

                Logfile.Log(ex.ToString());
            }
            return "";
        }

        public bool ExistsWakeupFile => System.IO.File.Exists(FileManager.GetWakeupTeslaloggerPath(car.CarInDB)) || TaskerWakeupfile();

        public bool DrivingOrChargingByStream
        {
            get => _drivingOrChargingByStream;
            set
            {
                if (_drivingOrChargingByStream != value)
                {
                    _drivingOrChargingByStream = value;
                    car.Log("DrivingOrChargingByStream: " + _drivingOrChargingByStream.ToString());
                }
            }
        }

        internal string Tesla_token
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get => tesla_token;
            [MethodImpl(MethodImplOptions.Synchronized)]
            set
            {
                tesla_token = StringCipher.Decrypt(value);
                if (car.FleetAPI)
                {
                    try
                    {
                        var ok = CheckJWT(tesla_token, out bool vehicle_location, out bool offline_access);
                        if (ok)
                        {
                            car.Log("vehicle_location: " + vehicle_location);
                            car.Log("offline_access: " + offline_access);
                            car.vehicle_location = vehicle_location;
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.ToExceptionless().Submit();
                        Logfile.Log(ex.ToString());
                    }
                }
            }
        }

        public static bool CheckJWT(string jwt, out bool vehicle_location, out bool offline_access)
        {
            vehicle_location = false;
            offline_access = false;

            if (jwt == "NULL")
                return false;

            try
            {
                var j = jwt.Split('.');
                var pl = j[1].Replace('-', '+').Replace('_', '/');
                switch (pl.Length % 4)
                {
                    case 2: pl += "=="; break;
                    case 3: pl += "="; break;
                }
                var payload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(pl));
                dynamic d = JsonConvert.DeserializeObject(payload);
                JArray scp = d["scp"];

                List<string> scopes = scp.ToObject<List<string>>();

                if (scopes.Contains("vehicle_location"))
                    vehicle_location = true;

                if (scopes.Contains("offline_access"))
                    offline_access = true;

                return true;
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().Submit();
                Logfile.Log(ex.ToString());
            }

            return false;
        }

        private void Log(string text)
        {
            car.Log(text);
        }

        internal async Task SendDataToAbetterrouteplannerAsync(long utc, double soc, double speed_mph, bool is_charging, double power, double lat, double lon)
        {
            try
            {
                // plausibility check
                if (soc < 0)
                {
                    // throw new Exception($"SoC < 0! soc:{soc}");
                    return;
                }

                if (car.ABRPMode <= 0 || String.IsNullOrEmpty(car.ABRPToken))
                    return;

                lock (httpClientLock)
                {
                    if (httpClientABRP == null)
                    {
                        CreateHttpClientABRP();
                    }
                }

                double speed_kmh = (int)Tools.MphToKmhRounded(speed_mph);

                Dictionary<string, object> values = new Dictionary<string, object>
                    {
                        { "utc", utc / 1000 },
                        { "soc", soc },
                        { "speed", speed_kmh },
                        { "is_charging", is_charging ? 1 : 0},
                        { "power", power },
                        { "lat", lat },
                        { "lon", lon },
                    };

                if (car.GetTeslaAPIState().GetInt("heading", out int heading))
                {
                    values.Add("heading", heading);
                }

                string json = JsonConvert.SerializeObject(values);

                DateTime start = DateTime.UtcNow;
                using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
                {
                    var result = await httpClientABRP.PostAsync("https://api.iternio.com/1/tlm/send?token=" + car.ABRPToken + "&tlm=" + json, null);

                    DBHelper.AddMothershipDataToDB("SendDataToAbetterrouteplanner", start, (int)result.StatusCode, car.CarInDB);
                    if (result.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        string response = result.Content.ReadAsStringAsync().Result;
                        Logfile.Log("SendDataToAbetterrouteplanner response: " + response);
                        car.ABRPMode = -1;
                    }
                    else if (result.StatusCode != HttpStatusCode.OK)
                    {
                        string response = result.Content.ReadAsStringAsync().Result;
                        Logfile.Log("SendDataToAbetterrouteplanner response: " + response);
                    }
                    else if (result.StatusCode == HttpStatusCode.OK)
                    {
                        var diff = DateTime.UtcNow - lastABRPActive;
                        if (diff.TotalHours > 12)
                        {
                            car.CreateExeptionlessFeature("ABRP").Submit();
                            lastABRPActive = DateTime.UtcNow;
                        }
                    }
                }
            }
            catch (TaskCanceledException tce)
            {
                car.CreateExceptionlessClient(tce).Submit();
                // Logfile.Log(tce.ToString());
                ABRPtimeouts++;
                if (ABRPtimeouts > 10)
                {
                    ABRPtimeouts = 0;
                    lock (httpClientLock)
                    {
                        httpClientABRP.Dispose();
                        CreateHttpClientABRP();
                    }
                }
            }
            catch (Exception ex)
            {
                car.CreateExceptionlessClient(ex).Submit();

                Logfile.Log(ex.ToString());
                Tools.DebugLog("SendDataToAbetterrouteplannerAsync exception: " + ex.ToString() + Environment.NewLine + ex.StackTrace);
            }
        }

        private static void CreateHttpClientABRP()
        {
            HttpClient c = new HttpClient();
            c.Timeout = TimeSpan.FromSeconds(10);
            c.DefaultRequestHeaders.Add("User-Agent", ApplicationSettings.Default.UserAgent);
            c.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            c.DefaultRequestHeaders.Connection.Add("keep-alive");
            c.DefaultRequestHeaders.Add("Authorization", "APIKEY 54ac054f-0412-4747-b788-bcc8c6b60f27");
            c.DefaultRequestHeaders.ConnectionClose = true;
            httpClientABRP = c;

            Logfile.Log("ABRP initialized!");
        }

        internal async Task SuperchargeBingoCheckin(double latitude, double longitude)
        {
            try
            {
                DateTime start = DateTime.UtcNow;

                lock (httpClientLock)
                {
                    if (httpClientSuCBingo == null)
                    {
                        HttpClient c = new HttpClient();
                        c.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        c.DefaultRequestHeaders.ConnectionClose = true;
                        httpClientSuCBingo = c;

                        Logfile.Log("SuperchargeBingo: initialized!");
                    }
                }

                Dictionary<string, object> values = new Dictionary<string, object>
                    {
                        { "user", car.SuCBingoUser },
                        { "key", car.SuCBingoApiKey},
                        { "lat", latitude.ToString(Tools.ciEnUS) },
                        { "long", longitude.ToString(Tools.ciEnUS) },
                        { "type", "teslalogger" },
                    };

                string json = JsonConvert.SerializeObject(values);
                using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
                {
                    Tools.SetThreadEnUS();
                    var result = await httpClientSuCBingo.PostAsync(new Uri("https://supercharge.bingo/v1.php/api/v1/checkin"), content).ConfigureAwait(false);
                    string response = result.Content.ReadAsStringAsync().Result;

                    DBHelper.AddMothershipDataToDB("SuperchargeBingoCheckin()", start, (int)result.StatusCode, car.CarInDB);

                    int checkinID = 0;
                    try
                    {
                        _ = int.TryParse(response, out checkinID);
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.Message);
                    }
                    finally
                    {
                        if (checkinID != 0)
                        {
                            Logfile.Log("SuperchargeBingo: Checkin OK, Checkin ID: " + checkinID.ToString());
                        }
                        else
                        {
                            //Logfile.Log("SuperchargeBingo: Checkin not OK, response: " + response);
                            dynamic jsonResult = JsonConvert.DeserializeObject(response);
                            dynamic message = jsonResult["message"];
                            Logfile.Log("SuperchargeBingo: Checkin Error: " + message);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
                car.SendException2Exceptionless(ex);
                Tools.DebugLog("SuperchargeBingo: Checkin exception: " + ex.ToString() + Environment.NewLine);
            }
        }

        public bool? CheckVirtualKey()
        {
            try
            {
                if (!car.FleetAPI)
                {
                    return false;
                }

                string json = "{\"vins\": [\"" + car.Vin + "\"]}";
                using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
                {
                    Tools.SetThreadEnUS();
                    HttpClient httpClientTeslaAPI = GetHttpClientTeslaAPI();
                    using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri( ApplicationSettings.Default.TeslaHttpProxyURL + "/api/1/vehicles/fleet_status")))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Tesla_token);
                        request.Content = content;
                        Tools.DebugLog($"CheckVirtualKey #{car.CarInDB} request: {request.RequestUri}");
                        var httpResponse = GetHttpClientTeslaAPI().SendAsync(request).Result;
                        string result = httpResponse.Content.ReadAsStringAsync().Result;
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            if (result.Contains("\"error\""))
                            {
                                car.Log(result);
                                return null;
                            }

                            dynamic jsonResult = JsonConvert.DeserializeObject(result);
                            dynamic response = jsonResult["response"];
                            JArray key_paired_vins = response["key_paired_vins"];
                            var kpv = key_paired_vins.Any(t => t.Value<String>() == car.Vin);

                            if (kpv)
                            {
                                car.Virtual_key = true;
                                Log("*** Virtual Key available");
                                return true;
                            }

                            JArray unpaired_vins = response["unpaired_vins"];
                            var upv = unpaired_vins.Any(t => t.Value<String>() == car.Vin);

                            if (upv)
                            {
                                car.CurrentJSON.FatalError = "No Virtual Key!!! Go to: <a href='https://www.tesla.com/_ak/teslalogger.de'>https://www.tesla.com/_ak/teslalogger.de</a>";
                                car.CurrentJSON.CreateCurrentJSON();
                                Log("*** No Virtual Key. Teslalogger won't work!!! go to https://www.tesla.com/_ak/teslalogger.de");
                                car.Virtual_key = false;
                                return false;
                            }

                            return null;
                        }
                        else
                        {
                            car.CreateExeptionlessLog("CheckVirtualKey", "Error", LogLevel.Fatal).AddObject((int)httpResponse.StatusCode + " / " + httpResponse.StatusCode.ToString(), "StatusCode").Submit();
                            Log("CheckVirtualKey: " + (int)httpResponse.StatusCode + " / " + httpResponse.StatusCode.ToString());
                            return null;
                        }
                    }
                }

            }
            catch (ThreadAbortException ex)
            {
                System.Diagnostics.Debug.WriteLine("Thread Stop!");
                car.CreateExceptionlessClient(ex).MarkAsCritical().Submit();
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
                car.CreateExceptionlessClient(ex).MarkAsCritical().Submit();
                ExceptionlessClient.Default.ProcessQueueAsync();
            }

            return null;
        }

        internal static bool BranchExists(string branch, out HttpStatusCode statusCode)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.ConnectionClose = true;
                ProductInfoHeaderValue userAgent = new ProductInfoHeaderValue("Teslalogger", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                client.DefaultRequestHeaders.UserAgent.Add(userAgent);
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(00000000; " + Thread.CurrentThread.ManagedThreadId + ")"));

                var g = client.GetAsync("https://api.github.com/repos/bassmaster187/TeslaLogger/branches/" + branch).Result;
                statusCode = g.StatusCode;
                if (g.IsSuccessStatusCode)
                {
                    string res = g.Content.ReadAsStringAsync().Result;
                    return res.Contains("signature");
                }
                else if (g.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            statusCode = 0;
            return false;
        }

    }

    class Account
    {
        public int id;
        public string tesla_token;
        public string display_name;
        public bool fleetAPI;
    }
}
