using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace TeslaLogger
{
    public class WebHelper
    {
        public static readonly string apiaddress = "https://owner-api.teslamotors.com/";
        public const string TeslaloggerUserAgent = "TL V1";

        public string Tesla_token = "";
        public string Tesla_id = "";
        public string Tesla_vehicle_id = "";
        public string Tesla_Streamingtoken = "";
        public string option_codes = "";
        public bool is_sentry_mode = false;
        public string fast_charger_brand = "";
        public string fast_charger_type = "";
        public string conn_charge_cable = "";
        public bool fast_charger_present = false;
        //private bool stopStreaming = false;
        private string elevation = "";
        private DateTime elevation_time = DateTime.Now;
        public DateTime lastTokenRefresh = DateTime.Now;
        public DateTime lastIsDriveTimestamp = DateTime.Now;
        public DateTime lastUpdateEfficiency = DateTime.Now.AddDays(-1);
        private static int MapQuestCount = 0;
        private static int NominatimCount = 0;

        string authHost = "https://auth.tesla.com";

        private bool _drivingOrChargingByStream = false;

        const string TESLA_CLIENT_ID = "81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384";
        const string TESLA_CLIENT_SECRET = "c7257eb71a564034f9419ee651c7d0e5f7aa6bfbd18bafb5c5c033b093bb2fa3";

        public ScanMyTesla scanMyTesla;
        private string _lastShift_State = "P";
        private static readonly Regex regexAssemblyVersion = new Regex("\n\\[assembly: AssemblyVersion\\(\"([0-9\\.]+)\"", RegexOptions.Compiled);

        internal ConcurrentDictionary<string, string> TeslaAPI_Commands = new ConcurrentDictionary<string, string>();
        internal Car car;

        static WebHelper()
        {
            //Damit Mono keine Zertifikatfehler wirft :-(
            ServicePointManager.ServerCertificateValidationCallback += (p1, p2, p3, p4) => true;
        }

        public WebHelper(Car car)
        {
            this.car = car;

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
                    DBHelper.AddMothershipDataToDB("tasker_date.php", start, 200);

                    if (reply.Contains("not found") || reply.Contains("never!"))
                    {
                        Log("LastTaskerToken not found - Stop using fast TaskerToken request! Reply: " + reply);
                        car.useTaskerToken = false;
                        return;
                    }

                    DateTime dt = DateTime.Parse(reply, Tools.ciEnUS);
                    var ts = DateTime.Now - dt;
                    if (ts.TotalDays > 2)
                    {
                        Log("LastTaskerToken: " + reply + " Stop using fast TaskerToken request!");
                        car.useTaskerToken = false;
                    }
                    else
                    {
                        if (!car.useTaskerToken)
                        {
                            Log("LastTaskerToken: " + reply + " Start using fast TaskerToken request!");
                            car.useTaskerToken = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                reply = reply ?? "NULL";
                Log("Reply: " + reply + "\r\n" + ex.Message);
            }
        }

        internal string GetLastShiftState()
        {
            return _lastShift_State;
        }

        internal void SetLastShiftState(string _newState)
        {
            if (!_newState.Equals(_lastShift_State))
            {
                car.HandleShiftStateChange(_lastShift_State, _newState);
                _lastShift_State = _newState;
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


                TimeSpan ts = DateTime.Now - car.Tesla_Token_Expire;

                if (ts.TotalDays < 30)
                {
                    Tesla_token = car.Tesla_Token;
                    lastTokenRefresh = car.Tesla_Token_Expire;

                    Log("Restore Token OK. Age: " + car.Tesla_Token_Expire.ToString());
                    return true;
                }
                else
                {
                    Log("Restore Token too old! " + car.Tesla_Token_Expire.ToString());
                }

            }
            catch (Exception ex)
            {
                Log("Error in RestoreToken: " + ex.Message);
            }


            return false;
        }
        static readonly char[] padding = { '=' };
        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
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
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));
            return result.ToString();
        }

        string GetNameValue(string text, string name)
        {
            Regex x = new Regex("name=\\\"" + name + "\\\".+value=\\\"(.+)\\\"");
            var m = x.Match(text);
            if (m.Groups.Count > 1)
            {
                return m.Groups[1].Value;
            }

            return "";
        }

        public string GetToken()
        {
            string resultContent = "";
            string cookie = "";
            MatchCollection m;

            try
            {
                car.passwortinfo.Append("Start getting Token<br>");

                string tempToken = UpdateTeslaTokenFromRefreshToken();

                if (!String.IsNullOrEmpty(tempToken))
                    return tempToken;


                string hiddenPassword = "";
                for (int x = 0; x < car.TeslaPasswort.Length; x++)
                {
                    hiddenPassword += "x";
                }

                Log("Login with : '" + Tools.ObfuscateString(car.TeslaName) + "' / '" + hiddenPassword + "'");

                if (car.TeslaName.Length == 0 || car.TeslaPasswort.Length == 0)
                {
                    car.passwortinfo.Append("ERROR: NO Credentials!<br>");
                    Log("NO Credentials");
                    throw new Exception("NO Credentials");
                }

                var code_verifier = RandomString(86);
                // car.Log("code_verifier:" + code_verifier);

                var code_challenge_SHA256 = ComputeSHA256Hash(code_verifier);
                var code_challenge = Convert.ToBase64String(Encoding.Default.GetBytes(code_challenge_SHA256)); 

                var state = RandomString(20);

                // car.Log("code_challenge:" + code_challenge);
                // car.Log("state:" + state);

                using (HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    AllowAutoRedirect = false
                })
                {

                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        // client.DefaultRequestHeaders.Add("User-Agent", TeslaloggerUserAgent);
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Connection.Add("keep-alive");

                        Dictionary<string, string> values = new Dictionary<string, string>
                        {
                           { "client_id", "ownerapi" },
                           { "code_challenge", code_challenge },
                           { "code_challenge_method", "S256" },
                           { "redirect_uri", "https://auth.tesla.com/void/callback" },
                           { "response_type", "code" },
                           { "scope", "openid email offline_access" },
                           { "state", state },
                           { "login_hint",  car.TeslaName }
                        };

                        string json = new JavaScriptSerializer().Serialize(values);
                        using (StringContent content = new StringContent(json.ToString(), Encoding.UTF8, "application/json"))
                        {
                            UriBuilder b = new UriBuilder(authHost + "/oauth2/v3/authorize");
                            b.Port = -1;
                            var q = HttpUtility.ParseQueryString(b.Query);
                            foreach (var v in values)
                            {
                                q[v.Key] = v.Value;
                            }
                            b.Query = q.ToString();
                            string url = b.ToString();

                            DateTime start = DateTime.UtcNow;
                            HttpResponseMessage result = client.GetAsync(url).Result;
                            resultContent = result.Content.ReadAsStringAsync().Result;

                            m = Regex.Matches(resultContent, "type=\\\"hidden\\\" name=\\\"(.*?)\\\" value=\\\"(.*?)\\\"");

                            IEnumerable<string> cookies = result.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;

                            DBHelper.AddMothershipDataToDB("GetTokenAsync()", start, (int)result.StatusCode);

                            cookie = cookies.ToList()[0];
                            cookie = cookie.Substring(0, cookie.IndexOf(" "));
                            cookie = cookie.Trim();

                            // car.Log("cookie:" + cookie);

                            if (resultContent.Contains("authorization_required"))
                            {
                                car.passwortinfo.Append("ERROR: Wrong Credentials!<br>");

                                Log("Wrong Credentials");

                                if (Tools.IsDocker())
                                {
                                    Thread.Sleep(5 * 60000);
                                }

                                throw new Exception("Wrong Credentials");
                            }

                            if (result.StatusCode == HttpStatusCode.RedirectMethod)
                            {
                                if (result.Headers.Location.Host == "auth.tesla.cn" && authHost != "https://auth.tesla.cn")
                                {
                                    authHost = "https://auth.tesla.cn";
                                    Log("Use chinese auth server: auth.tesla.cn!");
                                    return GetToken();
                                }
                            }
                        }
                    }

                    return GetTokenAsync2(code_challenge, cookie, m, state, code_verifier);
                }
            }
            catch (Exception ex)
            {
                car.passwortinfo.Append("Error in GetTokenAsync: " + ex.Message + "<br>");
                
                if (ex.InnerException != null)
                    car.passwortinfo.Append("Error in GetTokenAsync: " + ex.InnerException.Message + "<br>");

                Log("Error in GetTokenAsync: " + ex.Message);
                ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        private string UpdateTeslaTokenFromRefreshToken()
        {
            string refresh_token = car.dbHelper.GetRefreshToken(out string tesla_token);

            if (tesla_token.StartsWith("cn-"))
                authHost = "https://auth.tesla.cn";

            if (String.IsNullOrEmpty(refresh_token))
            {
                car.passwortinfo.Append("No Refresh Token<br>");
                Log("No Refresh Token");
                return "";
            }

            try
            {
                Log("Update Tesla Token From Refresh Token!");
                var d = new Dictionary<string, string>();
                d.Add("grant_type", "refresh_token");
                d.Add("client_id", "ownerapi");
                d.Add("refresh_token", refresh_token);
                d.Add("scope", "openid email offline_access");

                string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(d);

                using (HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                })
                {

                    DateTime start = DateTime.UtcNow;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        // client.DefaultRequestHeaders.Add("User-Agent", TeslaloggerUserAgent);
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Connection.Add("keep-alive");

                        using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
                        {
                            HttpResponseMessage result = client.PostAsync(authHost + "/oauth2/v3/token", content).Result;
                            string resultContent = result.Content.ReadAsStringAsync().Result;

                            DBHelper.AddMothershipDataToDB("UpdateTeslaTokenFromRefreshToken()", start, (int)result.StatusCode);

                            car.Log("HttpStatus: " + result.StatusCode.ToString());

                            dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                            string access_token = jsonResult["access_token"];

                            string new_refresh_token = jsonResult["refresh_token"];
                            if (new_refresh_token == refresh_token)
                                Log("refresh_token not changed");
                            else
                                car.dbHelper.UpdateRefreshToken(new_refresh_token);

                            return GetTokenAsync4(access_token);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
            }
            return "";
        }

        private string GetTokenAsync2(string code_challenge, string cookie, MatchCollection mc, string state, string code_verifier)
        {
            int length = 0;
            string transaction_id = "";

            var d = new Dictionary<string, string>();
            foreach (Match m in mc)
            {
                string key = m.Groups[1].Value;
                string value = m.Groups[2].Value;
                d.Add(key , value);

                if (key == "transaction_id")
                    transaction_id = value;

                // car.Log("Key: " + key +  " Value: " + value);

                length += m.Groups[1].Value.Length;
                length += m.Groups[2].Value.Length;
                length += 4;
            }

            d.Add("identity", car.TeslaName);
            d.Add("credential", car.TeslaPasswort);

            try
            {
                string code = "";

                using (HttpClientHandler ch = new HttpClientHandler())
                {
                    ch.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    ch.AllowAutoRedirect = false;
                    ch.UseCookies = false;
                    using (HttpClient client = new HttpClient(ch))
                    {
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Connection.Add("keep-alive");
                        // client.Timeout = TimeSpan.FromSeconds(10);
                        client.BaseAddress = new Uri(authHost);
                        // client.DefaultRequestHeaders.Add("User-Agent", TeslaloggerUserAgent);
                        client.DefaultRequestHeaders.Add("Cookie", cookie);
                        DateTime start = DateTime.UtcNow;

                        using (FormUrlEncodedContent content = new FormUrlEncodedContent(d))
                        {
                            UriBuilder b = new UriBuilder(authHost +"/oauth2/v3/authorize");
                            b.Port = -1;
                            var q = HttpUtility.ParseQueryString(b.Query);
                            q["client_id"] = "ownerapi";
                            q["code_challenge"] = code_challenge;
                            q["code_challenge_method"] = "S256";
                            q["redirect_uri"] = "https://auth.tesla.com/void/callback";
                            q["response_type"] = "code";
                            q["scope"] = "openid email offline_access";
                            q["state"] = state;
                            b.Query = q.ToString();
                            string url = b.ToString();

                            var temp = content.ReadAsStringAsync().Result;

                            // car.Log("FormUrlEncodedContent: " + temp.Substring(0, temp.Length - 6));

                            // car.Log("URL: " + url);

                            HttpResponseMessage result = client.PostAsync(url, content).Result;
                            string resultContent = result.Content.ReadAsStringAsync().Result;

                            DBHelper.AddMothershipDataToDB("GetTokenAsync2()", start, (int)result.StatusCode);

                            Uri location = result.Headers.Location;

                            bool isMFA = false;

                            if (result.StatusCode != HttpStatusCode.Redirect)
                            {
                                if (result.StatusCode == HttpStatusCode.OK && resultContent.Contains("passcode"))
                                {
                                    isMFA = true;
                                    car.passwortinfo.Append("Wait for MFA code<br>");
                                    code = WaitForMFA_Code(cookie, transaction_id, code_challenge, state);

                                    if (String.IsNullOrEmpty(code))
                                        return "NULL";
                                }
                                else
                                {
                                    car.passwortinfo.Append("Error: GetTokenAsync2 Redirect Location = null!!! Wrong credentials?<br>");
                                    car.Log("Error: GetTokenAsync2 HttpStatus: " + result.StatusCode.ToString() + " / Expecting: Redirect !!!");
                                }
                            }

                            if (!isMFA)
                            {
                                if (location == null)
                                {
                                    car.passwortinfo.Append("Error: GetTokenAsync2 Redirect Location = null!!! Wrong credentials?<br>");
                                    car.Log("Error: GetTokenAsync2 Redirect Location = null!!! Wrong credentials?");
                                    // car.Log(resultContent);
                                }

                                if (result.StatusCode == HttpStatusCode.Redirect && location != null)
                                {
                                    code = HttpUtility.ParseQueryString(location.Query).Get("code");
                                    // car.Log("Code: " + code);
                                }
                                else
                                {
                                    return "NULL";
                                }
                            }
                        }
                    }
                }

                return GetTokenAsync3(code, code_verifier);

            }
            catch (Exception ex)
            {
                car.passwortinfo.Append("Exception in GetTokenAsync2 !!!: " + ex.Message + "<br>");
                if (ex.InnerException != null)
                    car.passwortinfo.Append("Exception in GetTokenAsync2 !!!: " + ex.InnerException.Message + "<br>");

                car.Log(ex.ToString());
            }

            return "";            
        }

        private string WaitForMFA_Code(string cookie, string transaction_id, string code_challenge, string state)
        {
            car.Log("Start waiting for MFA code !!!");
            DateTime timeout = DateTime.UtcNow;

            while (car.MFA_Code == null || car.MFA_Code.Length != 6)
            {
                Thread.Sleep(10);

                if (DateTime.UtcNow - timeout > TimeSpan.FromSeconds(10))
                {
                    timeout = DateTime.UtcNow;
                    car.Log("Wait for MFA code !!!");
                }
            }

            car.Log("MFA Code: " + car.MFA_Code);

            // while (true)
            {
                Log("transaction_id: " + transaction_id);

                string code = MFA1(cookie, transaction_id, code_challenge, state);

                if (code.Length > 0)
                    return code;

                car.passwortinfo.Append("Code received from Tesla server<br>");

                System.Threading.Thread.Sleep(500);
            }

            return "";
        }

        private string MFA1(string cookie, string transaction_id, string code_challenge, string state)
        {
            string resultContent;
            using (HttpClientHandler ch = new HttpClientHandler())
            {
                ch.UseCookies = false;
                using (HttpClient client = new HttpClient(ch))
                {
                    // client.DefaultRequestHeaders.Add("User-Agent", TeslaloggerUserAgent);
                    client.DefaultRequestHeaders.Add("Cookie", cookie);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                    UriBuilder b = new UriBuilder("https://auth.tesla.com/oauth2/v3/authorize/mfa/factors");
                    b.Port = -1;

                    var q = HttpUtility.ParseQueryString(b.Query);
                    q.Add("transaction_id", transaction_id);
                    b.Query = q.ToString();
                    string url = b.ToString();

                    DateTime start = DateTime.UtcNow;
                    HttpResponseMessage result = client.GetAsync(url).Result;
                    resultContent = result.Content.ReadAsStringAsync().Result;

                    Log("MFA1 Result: " + resultContent);

                    dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                        
                    string factor_id = null;
                    try
                    {
                        var authentificatorDevices = jsonResult["data"];
                        car.passwortinfo.Append("Found " + authentificatorDevices.Length + " Authentificator Devices<br>");
                        
                        for (int ad = 0; ad < authentificatorDevices.Length; ad++)
                        {
                            try
                            {
                                car.passwortinfo.Append("Try Device:  " + jsonResult["data"][ad]["name"] + "<br>");
                                factor_id = jsonResult["data"][ad]["id"];
                                string c = MFA2(cookie, code_challenge, state, transaction_id, factor_id);

                                if (c.Length > 10)
                                {
                                    car.passwortinfo.Append("factor_id received from Tesla server<br>");
                                    return c;
                                }
                            }
                            catch (Exception ex)
                            {
                                car.passwortinfo.Append("Exception in MFA1 Try Device!!!: " + ex.Message + "<br>");
                                
                                if (ex.InnerException != null)
                                    car.passwortinfo.Append("Exception in MFA1 Try Device!!!: " + ex.InnerException.Message + "<br>");

                                car.Log("MFA1 ResultContent: " + resultContent);
                                car.Log(ex.ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        car.passwortinfo.Append("Exception in MFA1 : "+ ex.Message + "<br>");
                        car.Log("MFA1 ResultContent: " + resultContent);
                        car.Log(ex.ToString());
                    }

                    return "";
                    
                }
            }
            return "";
        }

        private string MFA2(string cookie, string code_challenge, string state, string transaction_id, string factor_id)
        {
            using (HttpClientHandler ch = new HttpClientHandler())
            {
                ch.AllowAutoRedirect = false;
                ch.UseCookies = false;
                using (HttpClient client = new HttpClient(ch))
                {
                    // client.Timeout = TimeSpan.FromSeconds(10);
                    client.BaseAddress = new Uri("https://auth.tesla.com");
                    // client.DefaultRequestHeaders.Add("User-Agent", TeslaloggerUserAgent);
                    client.DefaultRequestHeaders.Add("Cookie", cookie);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Referrer = new Uri("https://auth.tesla.com");
                    DateTime start = DateTime.UtcNow;

                    Dictionary<string, string> d = new Dictionary<string, string>();
                    d.Add("factor_id", factor_id);
                    d.Add("passcode", car.MFA_Code);
                    d.Add("transaction_id", transaction_id);

                    string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(d);

                    using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
                    {
                        HttpResponseMessage result = client.PostAsync("https://auth.tesla.com/oauth2/v3/authorize/mfa/verify", content).Result;
                        string resultContent = result.Content.ReadAsStringAsync().Result;

                        DBHelper.AddMothershipDataToDB("MFA2()", start, (int)result.StatusCode);

                        try
                        {
                            dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                            object o = jsonResult["data"]["valid"];
                            
                            if ((bool)o)
                                return MFA3(cookie, code_challenge, state, transaction_id);
                        }
                        catch (Exception ex)
                        {
                            car.passwortinfo.Append("Error: MFA2! <br>");
                            car.Log("Error: MFA2 : " + resultContent);
                        }
                    }
                }
            }

            return "NULL";
        }

        private string MFA3(string cookie, string code_challenge, string state, string transaction_id)
        {
            using (HttpClientHandler ch = new HttpClientHandler())
            {
                ch.AllowAutoRedirect = false;
                ch.UseCookies = false;
                ch.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (HttpClient client = new HttpClient(ch))
                {
                    // client.Timeout = TimeSpan.FromSeconds(10);
                    client.BaseAddress = new Uri("https://auth.tesla.com");
                    // client.DefaultRequestHeaders.Add("User-Agent", TeslaloggerUserAgent);
                    client.DefaultRequestHeaders.Add("Cookie", cookie);
                    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Connection.Add("keep-alive");

                    DateTime start = DateTime.UtcNow;

                    Dictionary<string, string> d = new Dictionary<string, string>();
                    d.Add("transaction_id", transaction_id);

                    using (FormUrlEncodedContent content = new FormUrlEncodedContent(d))
                    {
                        UriBuilder b = new UriBuilder("https://auth.tesla.com/oauth2/v3/authorize");
                        b.Port = -1;
                        var q = HttpUtility.ParseQueryString(b.Query);
                        q.Add("client_id", "ownerapi");
                        q.Add("code_challenge", code_challenge);
                        q.Add("code_challenge_method", "S256");
                        q.Add("redirect_uri", "https://auth.tesla.com/void/callback");
                        q.Add("response_type", "code");
                        q.Add("scope", "openid email offline_access");
                        q.Add("state", state);
                        b.Query = q.ToString();
                        string url = b.ToString();

                        var temp = content.ReadAsStringAsync().Result;

                        // car.Log("FormUrlEncodedContent: " + temp.Substring(0, temp.Length - 6));

                        // car.Log("URL: " + url);

                        HttpResponseMessage result = client.PostAsync(url, content).Result;
                        string resultContent = result.Content.ReadAsStringAsync().Result;

                        DBHelper.AddMothershipDataToDB("MFA3()", start, (int)result.StatusCode);

                        Uri location = result.Headers.Location;

                        if (result.StatusCode == HttpStatusCode.Redirect && location != null)
                        {
                            string code = HttpUtility.ParseQueryString(location.Query).Get("code");
                            car.Log("Code: " + code);
                            return code;
                        }
                        else
                        {
                            car.Log("Error: MFA2 Fail!");
                            return "NULL";
                        }
                    }
                }
            }

        }

        private string GetTokenAsync3(string code, string code_verifier)
        {
            try
            {
                string access_token = "";

                var d = new Dictionary<string, string>();
                d.Add("grant_type", "authorization_code");
                d.Add("client_id", "ownerapi");
                d.Add("code", code);
                d.Add("code_verifier", code_verifier);
                d.Add("redirect_uri", "https://auth.tesla.com/void/callback");

                string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(d);

                using (HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                })
                {

                    DateTime start = DateTime.UtcNow;

                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.BaseAddress = new Uri("https://auth.tesla.com");
                        // client.DefaultRequestHeaders.Add("User-Agent", TeslaloggerUserAgent);
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Connection.Add("keep-alive");

                        using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
                        {
                            HttpResponseMessage result = client.PostAsync(authHost + "/oauth2/v3/token", content).Result;
                            string resultContent = result.Content.ReadAsStringAsync().Result;

                            DBHelper.AddMothershipDataToDB("GetTokenAsync3()", start, (int)result.StatusCode);

                            // car.Log("HttpStatus: " + result.StatusCode.ToString());

                            dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);

                            if (Tools.IsPropertyExist(jsonResult, "error"))
                            {
                                string error = jsonResult["error"];
                                car.passwortinfo.Append("Error: " + error + " <br>");
                                car.Log("Error: GetTokenAsync3(): " + error);
                            }
                            else
                            {
                                string refresh_token = jsonResult["refresh_token"];
                                access_token = jsonResult["access_token"];

                                car.dbHelper.UpdateRefreshToken(refresh_token);

                                car.passwortinfo.Append("Access Token received. Everything is OK!!!<br>");
                            }

                            // car.Log(resultContent);
                        }
                    }
                }

                return GetTokenAsync4(access_token);
            }
            catch (Exception ex)
            {
                car.passwortinfo.Append("Error: GetTokenAsync3! <br>");
                car.Log(ex.ToString());
            }

            return "";
        }

        private string GetTokenAsync4(string access_token)
        {
            try
            {
                var d = new Dictionary<string, string>();
                d.Add("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer");
                d.Add("client_id", TESLA_CLIENT_ID);
                d.Add("client_secret", TESLA_CLIENT_SECRET);
                

                string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(d);

                DateTime start = DateTime.UtcNow;

                using (HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                })
                {
                    using (HttpClient client = new HttpClient(handler))
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        // client.DefaultRequestHeaders.Add("User-Agent", TeslaloggerUserAgent);
                        client.DefaultRequestHeaders.Connection.Add("keep-alive");
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + access_token);

                        using (var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"))
                        {
                            HttpResponseMessage result = client.PostAsync("https://owner-api.teslamotors.com/oauth/token", content).Result;
                            string resultContent = result.Content.ReadAsStringAsync().Result;

                            DBHelper.AddMothershipDataToDB("GetTokenAsync4()", start, (int)result.StatusCode);

                            car.Log("HttpStatus: " + result.StatusCode.ToString());

                            dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                            if (Tools.IsPropertyExist(jsonResult, "error"))
                            {
                                string error = jsonResult["error"];
                                car.passwortinfo.Append("Error: " + error + " <br>");
                                car.Log("Error: GetTokenAsync4(): " + error);
                                return "NULL";
                            }
                            else
                            {

                                string access_token2 = jsonResult["access_token"];
                                int created_at = jsonResult["created_at"];
                                int expires_in = jsonResult["expires_in"];

                                Tesla_token = jsonResult["access_token"];
                                car.dbHelper.UpdateTeslaToken();
                                car.LoginRetryCounter = 0;
                                return Tesla_token;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
            }
            return "";
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

                if (r2["charging_state"] == null || (resultContent != null && resultContent.Contains("vehicle unavailable")))
                {
                    if (justCheck)
                    {
                        return false;
                    }

                    if (r2["charging_state"] == null)
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

                string charging_state = r2["charging_state"].ToString();
                _ = long.TryParse(r2["timestamp"].ToString(), out long ts);
                decimal ideal_battery_range = (decimal)r2["ideal_battery_range"];
                if (ideal_battery_range == 999)
                {
                    ideal_battery_range = (decimal)r2["battery_range"];
                }

                decimal battery_range = (decimal)r2["battery_range"];

                string battery_level = r2["battery_level"].ToString();
                if (battery_level != null && Convert.ToInt32(battery_level) != car.currentJSON.current_battery_level)
                {
                    car.currentJSON.current_battery_level = Convert.ToInt32(battery_level);
                    car.currentJSON.CreateCurrentJSON();
                }
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

                if (r2["charge_rate"] != null)
                {
                    car.currentJSON.current_charge_rate_km = Convert.ToDouble(r2["charge_rate"]) * 1.609344;
                }

                if (r2["charge_limit_soc"] != null)
                {
                    if (car.currentJSON.charge_limit_soc != Convert.ToInt32(r2["charge_limit_soc"]))
                    {
                        car.currentJSON.charge_limit_soc = Convert.ToInt32(r2["charge_limit_soc"]);
                        car.currentJSON.CreateCurrentJSON();
                    }
                }

                if (r2["time_to_full_charge"] != null)
                {
                    if (car.currentJSON.current_time_to_full_charge != Convert.ToDouble(r2["time_to_full_charge"], Tools.ciEnUS))
                    {
                        car.currentJSON.current_time_to_full_charge = Convert.ToDouble(r2["time_to_full_charge"], Tools.ciEnUS);
                        car.currentJSON.CreateCurrentJSON();
                    }
                }

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
                            car.dbHelper.InsertCharging(ts.ToString(), battery_level, charge_energy_added, charger_power, (double)ideal_battery_range, (double)battery_range, charger_voltage, charger_phases, charger_actual_current, outside_temp.Result, car.IsHighFrequenceLoggingEnabled(true), charger_pilot_current, charge_current_request);
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
                    lastCharging_State = charging_state;
                    car.dbHelper.InsertCharging(ts.ToString(), battery_level, charge_energy_added, charger_power, (double)ideal_battery_range, (double)battery_range, charger_voltage, charger_phases, charger_actual_current, outside_temp.Result, car.IsHighFrequenceLoggingEnabled(true), charger_pilot_current, charge_current_request);
                    return true;
                }
                else if (charging_state == "Complete")
                {
                    if (lastCharging_State != "Complete")
                    {
                        car.dbHelper.InsertCharging(ts.ToString(), battery_level, charge_energy_added, charger_power, (double)ideal_battery_range, (double)battery_range, charger_voltage, charger_phases, charger_actual_current, outside_temp.Result, true, charger_pilot_current, charge_current_request);
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
                else if (!resultContent.Contains("upstream internal error"))
                {
                    ExceptionWriter(ex, resultContent);
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
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                        string adresse = apiaddress + "api/1/vehicles";
                        Task<HttpResponseMessage> resultTask;
                        HttpResponseMessage result;
                        DoGetVehiclesRequest(out resultContent, client, adresse, out resultTask, out result);

                        if (result.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            if (LoginRetry(result))
                            {
                                client.DefaultRequestHeaders.Remove("Authorization");
                                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);
                                DoGetVehiclesRequest(out resultContent, client, adresse, out resultTask, out result);
                            }
                        }

                        object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                        object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                        object[] r1temp = (object[])r1;

                        if (car.CarInAccount >= r1temp.Length)
                        {
                            Log("Car # " + car.CarInAccount + " not exists!");
                            return "NULL";
                        }
                        Dictionary<string, object> r2 = SearchCarDictionary(r1temp);

                        if (r2 == null)
                            return "NULL";

                        string OnlineState = r2["state"].ToString();
                        System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString() + " : " + OnlineState);

                        string display_name = r2["display_name"].ToString();
                        car.display_name = display_name;

                        if (car.display_name != display_name)
                        {
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

                        if (car.vin != vin)
                        {
                            car.vin = vin;
                            Tools.VINDecoder(vin, out car.year, out _, out car.AWD, out car.MIC, out _, out car.motor);
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
                            dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                            token = jsonResult["access_token"];
                            */

                            return resultContent;
                        }
                    }

                }
                catch (Exception ex)
                {
                    ExceptionWriter(ex, resultContent);

                    while (ex != null)
                    {
                        if (!(ex is AggregateException))
                        {
                            Log("GetVehicles Error: " + ex.Message);
                        }

                        ex = ex.InnerException;
                    }

                    Thread.Sleep(30000);
                }
            }
        }

        private Dictionary<string, object> SearchCarDictionary(object[] cars)
        {
            if (car.vin?.Length > 0)
            {
                for (int x = 0; x < cars.Length; x++)
                {
                    var cc = (Dictionary<string, object>)cars[x];
                    var ccVin = cc["vin"].ToString();

                    if (ccVin == car.vin)
                        return cc;
                }

                Logfile.Log("Car with VIN: " + car.vin + " not found!");
                return null;
            }
            else
                return (Dictionary<string, object>)cars[car.CarInAccount];
        }

        private void DoGetVehiclesRequest(out string resultContent, HttpClient client, string adresse, out Task<HttpResponseMessage> resultTask, out HttpResponseMessage result)
        {
            DateTime start = DateTime.UtcNow;
            resultTask = client.GetAsync(adresse);
            result = resultTask.Result;
            resultContent = result.Content.ReadAsStringAsync().Result;
            _ = car.GetTeslaAPIState().ParseAPI(resultContent, "vehicles", car.CarInAccount);
            DBHelper.AddMothershipDataToDB("GetVehicles()", start, (int)result.StatusCode);

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

        private int unknownStateCounter = 0;

        public async Task<string> IsOnline()
        {
            string resultContent = "";
            try
            {
                using (HttpClient client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(11)
                })
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                    string adresse = apiaddress + "api/1/vehicles";

                    DateTime start = DateTime.UtcNow;
                    HttpResponseMessage result = await client.GetAsync(adresse);

                    if (result.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        if (LoginRetry(result))
                            return "NULL";
                    }

                    resultContent = await result.Content.ReadAsStringAsync();
                    _ = car.GetTeslaAPIState().ParseAPI(resultContent, "vehicles", car.CarInAccount);
                    DBHelper.AddMothershipDataToDB("IsOnline()", start, (int)result.StatusCode);
                    if (TeslaAPI_Commands.ContainsKey("vehicles"))
                    {
                        TeslaAPI_Commands.TryGetValue("vehicles", out string drive_state);
                        TeslaAPI_Commands.TryUpdate("vehicles", resultContent, drive_state);
                    }
                    else
                    {
                        TeslaAPI_Commands.TryAdd("vehicles", resultContent);
                    }

                    object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);

                    object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                    object[] r2 = (object[])r1;
                    object r3 = r2[car.CarInAccount];
                    Dictionary<string, object> r4 = (Dictionary<string, object>)r3;
                    string state = r4["state"].ToString();
                    object[] tokens = (object[])r4["tokens"];
                    Tesla_Streamingtoken = tokens[0].ToString();

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

                            if (unknownStateCounter == 0)
                            {
                                string r = Wakeup().Result;
                                Log("WakupResult: " + r);
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
                            if (state == "offline" || state == "asleep")
                                return state;

                            string resultContent2 = GetCommand("vehicle_config").Result;

                            if (resultContent2 == "INSERVICE")
                                return "INSERVICE";

                            dynamic jBadge = new JavaScriptSerializer().DeserializeObject(resultContent2);
                            dynamic jBadgeResult = jBadge["response"];

                            if (jBadgeResult != null)
                            {
                                string car_type = car.car_type;
                                string car_special_type = car.car_special_type;
                                string trim_badging = car.trim_badging;


                                if (Tools.IsPropertyExist(jBadgeResult, "car_type"))
                                {
                                    car.car_type = jBadgeResult["car_type"].ToString().ToLower().Trim();
                                }

                                if (Tools.IsPropertyExist(jBadgeResult, "car_special_type"))
                                {
                                    car.car_special_type = jBadgeResult["car_special_type"].ToString().ToLower().Trim();
                                }

                                car.trim_badging = Tools.IsPropertyExist(jBadgeResult, "trim_badging")
                                    ? (string)jBadgeResult["trim_badging"].ToString().ToLower().Trim()
                                    : "";

                                UpdateEfficiency();
                                lastUpdateEfficiency = DateTime.Now;

                                if (car_type != car.car_type || car_special_type != car.car_special_type || trim_badging != car.trim_badging)
                                    car.dbHelper.WriteCarSettings();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionWriter(ex, resultContent);
                    }

                    return state;
                }
            }
            catch (Exception ex)
            {
                ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public void UpdateEfficiency()
        {
            //string eff = "0.190052356";
            String vinCarType = "";
            if (String.IsNullOrEmpty(car.car_type))
                Tools.VINDecoder(car.vin, out _, out vinCarType, out bool AWD, out _, out string battery, out _);

            if (car.car_type == "model3" || vinCarType == "Model 3")
            {
                Tools.VINDecoder(car.vin, out int year, out _, out bool AWD, out _, out string battery, out _);

                int maxRange = car.dbHelper.GetAvgMaxRage();
                if (maxRange > 430)
                {
                    try
                    {
                        if (!AWD)
                        {
                            WriteCarSettings("0.145", "M3 LR RWD");
                            return;
                        }
                        else if (car.DB_Wh_TR >= 0.135 && car.DB_Wh_TR <= 0.142 && AWD)
                        {
                            WriteCarSettings("0.139", "M3 LR FL");
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(ex.ToString());
                    }

                    WriteCarSettings("0.152", "M3 LR");
                    return;
                }
                else
                {
                    if (battery == "LFP")
                        WriteCarSettings("0.133", "M3 SR+ LFP");
                    else if (year == 2021)
                        WriteCarSettings("0.126", "M3 SR+ 2021");
                    else
                        WriteCarSettings("0.137", "M3 SR+");
                    return;
                }
            }
            else if (car.car_type == "models2" && car.car_special_type == "base")
            {
                if (car.trim_badging == "60")
                {
                    WriteCarSettings("0.200", "S 60");
                    return;
                }
                else if (car.trim_badging == "60d")
                {
                    WriteCarSettings("0.187", "S 60D");
                    return;
                }
                else if (car.trim_badging == "75d")
                {
                    WriteCarSettings("0.186", "S 75D");
                    return;
                }
                else if (car.trim_badging == "75")
                {
                    WriteCarSettings("0.195", "S 75");
                    return;
                }
                else if (car.trim_badging == "90d")
                {
                    WriteCarSettings("0.188", "S 90D");
                    return;
                }
                else if (car.trim_badging == "p90")
                {
                    WriteCarSettings("0.201", "S P90");
                    return;
                }
                else if (car.trim_badging == "p90d")
                {
                    WriteCarSettings("0.201", "S P90D");
                    return;
                }
                else if (car.trim_badging == "100d")
                {
                    WriteCarSettings("0.189", "S 100D");
                    return;
                }
                else if (car.trim_badging == "p100d")
                {
                    WriteCarSettings("0.200", "S P100D");
                    return;
                }
                else if (car.trim_badging.Length == 0)
                {
                    Tools.VINDecoder(car.vin, out _, out _, out bool AWD, out _, out _, out string motor);
                    int maxRange = car.dbHelper.GetAvgMaxRage();
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
            else if (car.car_type == "models" && (car.car_special_type == "base" || car.car_special_type == "signature"))
            {
                if (car.trim_badging == "60")
                {
                    WriteCarSettings("0.200", "S 60");
                    return;
                }
                else if (car.trim_badging == "70")
                {
                    WriteCarSettings("0.200", "S 70");
                    return;
                }
                else if (car.trim_badging == "70d")
                {
                    WriteCarSettings("0.194", "S 70D");
                    return;
                }
                else if (car.trim_badging == "p85d")
                {
                    WriteCarSettings("0.201", "S P85D");
                    return;
                }
                else if (car.trim_badging == "p85+")
                {
                    WriteCarSettings("0.201", "S P85+");
                    return;
                }
                else if (car.trim_badging == "85d")
                {
                    WriteCarSettings("0.186", "S 85D");
                    return;
                }
                else if (car.trim_badging == "p85")
                {
                    WriteCarSettings("0.201", "S P85");
                    return;
                }
                else if (car.trim_badging == "85")
                {
                    WriteCarSettings("0.201", "S 85");
                    return;
                }
                else if (car.trim_badging == "90")
                {
                    WriteCarSettings("0.201", "S 90");
                    return;
                }
                else if (car.trim_badging == "90d")
                {
                    WriteCarSettings("0.187", "S 90D");
                    return;
                }
                else if (car.trim_badging == "p90")
                {
                    WriteCarSettings("0.201", "S P90");
                    return;
                }
                else if (car.trim_badging == "p90d")
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
            else if (car.car_type == "modelx" && car.car_special_type == "base")
            {
                if (car.trim_badging == "75d")
                {
                    WriteCarSettings("0.224", "X 75D");
                    return;
                }
                else if (car.trim_badging == "100d")
                {
                    WriteCarSettings("0.217", "X 100D");
                    return;
                }
                else if (car.trim_badging == "90d")
                {
                    WriteCarSettings("0.212", "X 90D");
                    return;
                }
                else if (car.trim_badging == "p100d")
                {
                    WriteCarSettings("0.226", "X P100D");
                    return;
                }
                else if (car.trim_badging == "p90d")
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
            else if (car.car_type == "modely" && car.car_special_type == "base")
            {
                if (car.trim_badging == "74d")
                {
                    WriteCarSettings("0.148", "Y LR AWD");
                    return;
                }
                else if (car.trim_badging == "p74d")
                {
                    WriteCarSettings("0.148", "Y P");
                    return;
                }
            }

            return;
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
            if (car.ModelName != ModelName || car.Wh_TR.ToString(Tools.ciEnUS) != eff)
            {
                Log("WriteCarSettings -> ModelName: " + ModelName + " eff: " + eff);

                car.ModelName = ModelName;
                car.Wh_TR = Convert.ToDouble(eff, Tools.ciEnUS);
                car.WriteSettings();
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
                _ = long.TryParse(r2["timestamp"].ToString(), out long ts);
                decimal dLatitude = (decimal)r2["latitude"];
                decimal dLongitude = (decimal)r2["longitude"];

                double latitude = (double)dLatitude;
                double longitude = (double)dLongitude;

                car.currentJSON.latitude = latitude;
                car.currentJSON.longitude = longitude;

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

                if (justinsertdb || shift_state == "D" || shift_state == "R" || shift_state == "N" || car.currentJSON.current_is_preconditioning)
                {
                    // var address = ReverseGecocodingAsync(latitude, longitude);
                    //var altitude = AltitudeAsync(latitude, longitude);
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

                    double ideal_battery_range_km = GetIdealBatteryRangekm(out int battery_level, out double battery_range_km);

                    if (t_outside_temp != null)
                    {
                        outside_temp = t_outside_temp.Result;
                    }

                    car.dbHelper.InsertPos(ts.ToString(), latitude, longitude, speed, power, odometer.Result, ideal_battery_range_km, battery_range_km, battery_level, outside_temp, elevation);

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
                    Log("IsDriving = NULL!");
                    Thread.Sleep(10000);
                }
                else
                {
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

        Thread streamThread = null;
        public void StartStreamThread()
        {
            if (streamThread == null)
            {
                streamThread = new System.Threading.Thread(() => StartStream());
                streamThread.Name = "StreamAPIThread";
                streamThread.Start();
            }
        }

        private void StartStream()
        {
            string resultContent = null;
            byte[] buffer = new byte[1024];

            Log("StartStream");
            stopStreaming = false;
            string line = "";
            while (!stopStreaming)
            {
                System.Net.WebSockets.ClientWebSocket ws = null;
                try
                {
                    if (!car.currentJSON.current_falling_asleep)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }   

                    // string online = IsOnline().Result;

                    ws = new System.Net.WebSockets.ClientWebSocket();

                    byte[] byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", ApplicationSettings.Default.TeslaName, Tesla_Streamingtoken));
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

                    if (ws.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        ws.SendAsync(bufferMSG, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                    }

                    while (ws.State == System.Net.WebSockets.WebSocketState.Open)
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

                                dynamic j = new JavaScriptSerializer().DeserializeObject(resultContent);

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
                                                throw new Exception("Vehicle is offline");
                                            else
                                            {
                                                car.Log("Stream Data Error: " + resultContent);
                                                throw new Exception("unhandled vehicle_error: " + v);
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
                            Logfile.Log("Streaming Error: " + ex.Message);

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
                }
            }

            Log("StartStream Ende");
            
        }

        string lastStreamingAPIShiftState = null;
        DateTime lastStreamingAPILog = DateTime.UtcNow;
        DateTime lastStreamingAPIData = DateTime.UtcNow;
        int vehicleDisconnectedCounter = 0;


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
                    DrivingOrChargingByStream = true;
            }
            else if (shift_state != null && (shift_state == "D" || shift_state == "R" || shift_state == "N"))
            {
                DrivingOrChargingByStream = true;
            }
            else
                DrivingOrChargingByStream = false; 
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

        public static async Task<string> ReverseGecocodingAsync(Car c, double latitude, double longitude, bool forceGeocoding = false, bool insertGeocodecache = true)
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

                    string value = GeocodeCache.Instance.Search(latitude, longitude);
                    if (value != null)
                    {
                        Logfile.Log("Reverse geocoding by GeocodeCache");
                        return value;
                    }
                }

                Tools.SetThread_enUS();

                Thread.Sleep(5000); // Sleep to not get banned by Nominatim

                using (WebClient webClient = new WebClient())
                {

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

                    if (country_code.Length > 0 && c != null)
                    {
                        c.currentJSON.current_country_code = country_code;
                        c.currentJSON.current_state = r2.ContainsKey("state") ? r2["state"].ToString() : "";
                    }

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

                    if (insertGeocodecache)
                    {
                        GeocodeCache.Instance.Insert(latitude, longitude, adresse);
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
                using (MySqlConnection con2 = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con2.Open();
                    using (MySqlCommand cmd2 = new MySqlCommand("update pos set address=@address, altitude=@altitude where id = @id", con2))
                    {
                        cmd2.Parameters.AddWithValue("@id", id);
                        cmd2.Parameters.AddWithValue("@address", address);
                        cmd2.Parameters.AddWithValue("@altitude", altitude);
                        cmd2.ExecuteNonQuery();

                        System.Diagnostics.Debug.WriteLine("id updateed: " + id + " address: " + address);
                    }
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
                            ExceptionWriter(ex, "");
                        }
                    }
                }
            }

            GeocodeCache.Instance.Write();

            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"SELECT pos.id, lat, lng FROM chargingstate join pos on chargingstate.Pos = pos.id where address IS null OR address = '' or pos.id = ''", con))
                {

                    MySqlDataReader dr = cmd.ExecuteReader();
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
                            ExceptionWriter(ex, "");
                        }
                    }
                }
            }

            GeocodeCache.Instance.Write();
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
                    using (MySqlCommand cmd = new MySqlCommand(@"Select lat, lng, pos.id, address, fast_charger_brand, max_charger_power 
                        from pos    
                        left join chargingstate on pos.id = chargingstate.pos
                        where pos.id in (SELECT Pos FROM chargingstate) or pos.id in (SELECT StartPos FROM drivestate) or pos.id in (SELECT EndPos FROM drivestate)", con))
                    {
                        MySqlDataReader dr = cmd.ExecuteReader();
                        int t2 = Environment.TickCount - t;
                        Logfile.Log($"UpdateAllPOIAddresses Select {t2}ms");

                        while (dr.Read())
                        {
                            count = UpdatePOIAdress(count, dr);
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

                    MySqlDataReader dr = cmd.ExecuteReader();
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

                Address a = Geofence.GetInstance().GetPOI(lat, lng, false, brand, max_power);
                if (a == null)
                {
                    if (dr[3] == DBNull.Value || dr[3].ToString().Length == 0)
                    {
                        DBHelper.UpdateAddress(null, id);
                    }
                    return count;
                }

                if (dr[3] == DBNull.Value || a.name != dr[3].ToString())
                {
                    using (MySqlConnection con2 = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con2.Open();
                        using (MySqlCommand cmd2 = new MySqlCommand("update pos set address=@address where id = @id", con2))
                        {
                            cmd2.Parameters.AddWithValue("@id", id);
                            cmd2.Parameters.AddWithValue("@address", a.name);
                            cmd2.ExecuteNonQuery();

                            count++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(" Exception in UpdateAllPOIAddresses: " + ex.Message);
            }

            return count;
        }

        private double GetIdealBatteryRangekm(out int battery_level, out double battery_range_km)
        {
            string resultContent = "";
            battery_level = -1;
            battery_range_km = -1;

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
                    if (!car.Raven)
                    {
                        car.Raven = true;
                        car.WriteSettings();
                        Log("Raven Model!");
                    }
                }

                if (r2["battery_range"] != null)
                {
                    battery_range_km = Convert.ToDouble(r2["battery_range"]) / (double)0.62137;
                }

                if (r2["battery_level"] != null)
                {
                    battery_level = Convert.ToInt32(r2["battery_level"]);
                    car.currentJSON.current_battery_level = battery_level;
                }

                return (double)ideal_battery_range / (double)0.62137;
            }
            catch (Exception ex)
            {
                ExceptionWriter(ex, resultContent);
            }
            return -1;
        }

        private double lastOdometerKM = 0;

        internal async Task<double> GetOdometerAsync()
        {
            string resultContent = "";
            try
            {
                resultContent = await GetCommand("vehicle_state");
                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;
                _ = long.TryParse(r2["timestamp"].ToString(), out long ts);

                if (r2.ContainsKey("sentry_mode") && r2["sentry_mode"] != null)
                {
                    try
                    {
                        bool sentry_mode = (bool)r2["sentry_mode"];
                        if (sentry_mode != is_sentry_mode)
                        {
                            is_sentry_mode = sentry_mode;
                            Log("sentry_mode: " + sentry_mode);
                        }

                        car.currentJSON.current_is_sentry_mode = sentry_mode;
                    }
                    catch (Exception ex)
                    {
                        ExceptionWriter(ex, resultContent);
                        Log(ex.Message);
                    }
                }

                if (r2["odometer"] == null)
                {
                    Log("odometer = NULL");
                    return lastOdometerKM;
                }

                decimal odometer = (decimal)r2["odometer"];


                try
                {
                    string car_version = r2["car_version"].ToString();
                    if (car.currentJSON.current_car_version != car_version)
                    {
                        Log("Car Version: " + car_version);
                        car.currentJSON.current_car_version = car_version;

                        car.dbHelper.SetCarVersion(car_version);

                        TaskerWakeupfile(true);
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                }

                decimal odometerKM = odometer / 0.62137M;
                lastOdometerKM = (double)odometerKM;
                return lastOdometerKM;
            }
            catch (Exception ex)
            {
                ExceptionWriter(ex, resultContent);
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
                resultContent = await GetCommand("climate_state");
                if (resultContent == null || resultContent.Length == 0 || resultContent == "NULL")
                {
                    Log("GetOutsideTempAsync: NULL");
                    return null;
                }

                Tools.SetThread_enUS();
                object jsonResult = new JavaScriptSerializer().DeserializeObject(resultContent);
                object r1 = ((Dictionary<string, object>)jsonResult)["response"];
                Dictionary<string, object> r2 = (Dictionary<string, object>)r1;
                _ = long.TryParse(r2["timestamp"].ToString(), out long ts);
                try
                {
                    if (r2["inside_temp"] != null)
                    {
                        car.currentJSON.current_inside_temperature = Convert.ToDouble(r2["inside_temp"]);
                    }
                }
                catch (Exception) { }

                decimal? outside_temp = null;
                if (r2["outside_temp"] != null)
                {
                    outside_temp = (decimal)r2["outside_temp"];
                    car.currentJSON.current_outside_temp = (double)outside_temp;
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
                        if (car.currentJSON.current_battery_heater != battery_heater)
                        {
                            car.currentJSON.current_battery_heater = (bool)battery_heater;

                            Log("Battery heater: " + battery_heater);
                            car.currentJSON.CreateCurrentJSON();

                            // write into Database
                            Thread.Sleep(5000);
                            IsDriving(true);
                            Thread.Sleep(5000);
                        }
                    }
                }
                catch (Exception) { }


                bool preconditioning = r2["is_preconditioning"] != null && (bool)r2["is_preconditioning"];
                if (preconditioning != car.currentJSON.current_is_preconditioning)
                {
                    car.currentJSON.current_is_preconditioning = preconditioning;
                    Log("Preconditioning: " + preconditioning);
                    car.currentJSON.CreateCurrentJSON();

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
                else if (!resultContent.Contains("upstream internal error"))
                {
                    ExceptionWriter(ex, resultContent);
                }
            }
            return null;
        }

        public async Task<string> GetCommand(string cmd)
        {
            string resultContent = "";
            try
            {
                using (HttpClient client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(11)
                })
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                    string adresse = apiaddress + "api/1/vehicles/" + Tesla_id + "/data_request/" + cmd;

                    DateTime start = DateTime.UtcNow;
                    HttpResponseMessage result = await client.GetAsync(adresse);

                    if (result.IsSuccessStatusCode)
                    {

                        resultContent = await result.Content.ReadAsStringAsync();
                        DBHelper.AddMothershipDataToDB("GetCommand(" + cmd + ")", start, (int)result.StatusCode);
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


                        return resultContent;
                    }
                    else if (result.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        LoginRetry(result);
                    }
                    else if (result.StatusCode == HttpStatusCode.MethodNotAllowed)
                    {
                        if (car.IsInService())
                            return "INSERVICE";
                        else
                            Log("Result.Statuscode: " + (int)result.StatusCode + " ("+ result.StatusCode.ToString() +") cmd: " + cmd);

                    }
                    else if (result.StatusCode == HttpStatusCode.RequestTimeout)
                    {
                        Log("Result.Statuscode: " + (int)result.StatusCode + " (" + result.StatusCode.ToString() + ") cmd: " + cmd);
                        Thread.Sleep(30000);
                    }
                    else
                    {
                        Log("Result.Statuscode: " + (int)result.StatusCode + " (" + result.StatusCode.ToString() + ") cmd: " + cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public bool LoginRetry(HttpResponseMessage result)
        {
            if (result?.StatusCode == HttpStatusCode.Unauthorized)
            {
                Log("HttpStatusCode = Unauthorized. Password changed or still valid? " + car.LoginRetryCounter);

                if (car.LoginRetryCounter < 2)
                {
                    System.Threading.Thread.Sleep(60000);

                    car.LoginRetryCounter++;
                    Tesla_token = GetToken();
                    return true;
                }
                else
                {
                    car.ExitTeslaLogger("Login retrys exeeded!");
                }
            }
            return false;
        }

        public async Task<string> GetNearbyChargingSites()
        {
            string resultContent = "";
            try
            {
                using (HttpClient client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(11)
                })
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);

                    string adresse = apiaddress + "api/1/vehicles/" + Tesla_id + "/nearby_charging_sites";

                    DateTime start = DateTime.UtcNow;
                    HttpResponseMessage result = await client.GetAsync(new Uri(adresse)).ConfigureAwait(false);
                    resultContent = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
                    DBHelper.AddMothershipDataToDB("GetCommand(nearby_charging_sites)", start, (int)result.StatusCode);
                    return resultContent;
                }
            }
            catch (Exception ex)
            {
                ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public async Task<string> PostCommand(string cmd, string data, bool _json = false)
        {
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
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "C# App");
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Tesla_token);
                    //if (_json)
                    //{
                    //    client.DefaultRequestHeaders.TryAddWithoutValidation("contenttype", "application/json");
                    //}

                    string url = apiaddress + "api/1/vehicles/" + Tesla_id + "/" + cmd;

                    StringContent queryString = null;
                    try
                    {
                        queryString = data != null ? new StringContent(data) : null;

                        if (_json && data != null)
                        {
                            queryString?.Dispose();
                            queryString = new StringContent(data, Encoding.UTF8, "application/json");
                        }

                        DateTime start = DateTime.UtcNow;
                        HttpResponseMessage result = await client.PostAsync(url, data != null ? queryString : null);
                        resultContent = await result.Content.ReadAsStringAsync();
                        DBHelper.AddMothershipDataToDB("PostCommand(" + cmd + ")", start, (int)result.StatusCode);
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

                        return resultContent;
                    }
                    finally
                    {
                        queryString?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionWriter(ex, resultContent);
            }

            return "NULL";
        }

        public async Task<string> Wakeup()
        {
            return await PostCommand("wake_up", "");
        }


        public string GetCachedRollupData()
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

        public DataTable GetEnergyChartData()
        {
            // https://www.energy-charts.de/power/week_2018_46.json
            string resultContent = "";

            using (HttpClient client = new HttpClient())
            {
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

        }

        public void StopStreaming()
        {
            Log("Request StopStreaming");
            //stopStreaming = true;
        }

        private DateTime lastTaskerWakeupfile = DateTime.Today;
        private bool stopStreaming = false;

        public bool TaskerWakeupfile(bool force = false)
        {
            try
            {
                Tools.SetThread_enUS();

                Tools.GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin, out string Range, out _, out _, out _);

                TimeSpan ts = DateTime.Now - lastTaskerWakeupfile;

                int secBetweenTaskerWakeupFile = 20;
                if (!car.useTaskerToken)
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

                using (HttpClient client = new HttpClient())
                {

                    Dictionary<string, string> d = new Dictionary<string, string>
                {
                    { "t", car.TaskerHash },
                    { "v", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() },
                    { "cv", car.currentJSON.current_car_version },
                    { "m", car.Model },
                    { "bt", car.Battery },
                    { "n", name },
                    { "eff", car.Wh_TR.ToString(Tools.ciEnUS) },
                    { "oc", option_codes },

                    { "db_eff", car.DB_Wh_TR.ToString(Tools.ciEnUS)},
                    { "db_eff_cnt", car.DB_Wh_TR_count.ToString(Tools.ciEnUS) },

                    { "pw", power },
                    { "temp", temperature },
                    { "le", length },
                    { "ln", language },

                    { "CT", car.car_type },
                    { "CST", car.car_special_type },
                    { "TB", car.trim_badging },

                    { "G", Tools.GetGrafanaVersion() },

                    { "D", Tools.IsDocker() ? "1" : "0" },
                    { "SMT", Tools.UseScanMyTesla() ? "1" : "0" },
                    { "SMTs", car.dbHelper.GetScanMyTeslaSignalsLastWeek().ToString() },
                    { "SMTp", car.dbHelper.GetScanMyTeslaPacketsLastWeek().ToString() },
                    { "TR", car.dbHelper.GetAvgMaxRage().ToString() },

                    { "OS", Tools.GetOsVersion() },
                    { "CC", car.currentJSON.current_country_code },
                    { "ST", car.currentJSON.current_state },
                    { "UP", Tools.GetOnlineUpdateSettings().ToString() },
                    { "sumkm", car.sumkm.ToString() },
                    { "avgkm", car.avgkm.ToString() },
                    { "kwh100km", car.kwh100km.ToString() },
                    { "avgsocdiff", car.avgsocdiff.ToString() },
                    { "maxkm", car.maxkm.ToString() },
                    { "SOC50V", ((int)car.carVoltageAt50SOC).ToString()},
                    { "AWD" , car.AWD ? "1" : "0" },
                    { "MIC" , car.MIC ? "1" : "0" },
                    { "year" , car.year.ToString() },
                    { "motor" , car.motor }

                };

                    using (FormUrlEncodedContent content = new FormUrlEncodedContent(d))
                    {
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
                                string lasttaskerwakeupfilepaht = System.IO.Path.Combine(FileManager.GetExecutingPath(), "LASTTASKERWAKEUPFILE_" + car.CarInDB);
                                string ltwf = resultContent.Replace("wakeupfile", "").Trim();
                                System.IO.File.WriteAllText(lasttaskerwakeupfilepaht, ltwf);
                            }
                            catch (Exception)
                            { }

                            Log("TaskerWakeupfile available! [Webservice]" + resultContent.Replace("wakeupfile", ""));
                            if (!car.useTaskerToken)
                            {
                                Log("Start using fast TaskerToken request!");
                                car.useTaskerToken = true;
                            }
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
            try
            {
                string contents;
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
                return "Error during online version check: " + wex.Message;
            }
            catch (Exception ex)
            {
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

        private void Log(string text)
        {
            car.Log(text);
        }
    }
}