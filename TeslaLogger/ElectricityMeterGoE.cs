using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Exceptionless;
using Newtonsoft.Json;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    class ElectricityMeterGoE : ElectricityMeterBase
    {
        private string host;
        private string paramater;

        internal string status;

        Guid guid; // defaults to new Guid();
        static readonly HttpClient client = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (p1, p2, p3, p4) => true
        }, true);

        public ElectricityMeterGoE(string host, string paramater)
        {
            host = host.TrimEnd('/');
            if (host.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                host = host.Substring(0, host.Length - 4);
            this.host = host;
            this.paramater = paramater;
        }


        string GetCurrentData()
        {
            try
            {

                if (status != null)
                {
                    return status;
                }

                string cacheKey = "goe_" + guid.ToString();
                object o = MemoryCache.Default.Get(cacheKey);

                if (o != null)
                    return (string)o;

                // New firmware (>= 60.x): /api/status?filter=...
                // Old firmware:            /status
                string lastJSON = "";
                bool gotData = false;

                try
                {
                    string newUrl = host + "/api/status?filter=wh,whg,whs,whb,who,eto,car,fwv";
                    lastJSON = client.GetStringAsync(newUrl).GetAwaiter().GetResult();
                    gotData = lastJSON.Contains("\"eto\"") || lastJSON.Contains("\"car\"") || lastJSON.Contains("\"whg\"");
                }
                catch (Exception)
                {
                    gotData = false;
                }

                if (!gotData)
                {
                    lastJSON = client.GetStringAsync(host + "/status").GetAwaiter().GetResult();
                }

                MemoryCache.Default.Add(cacheKey, lastJSON, DateTime.Now.AddSeconds(10));
                return lastJSON;
            }
            catch (Exception ex)
            {
                if (ex is WebException wx)
                {
                    if ((wx.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                    {
                        Logfile.Log(wx.Message);
                        return "";
                    }

                }
                if (!WebHelper.FilterNetworkoutage(ex))
                    ex.ToExceptionless().FirstCarUserID().Submit();

                Logfile.Log(ex.ToString());
            }

            return "";
        }


        public override double? GetUtilityMeterReading_kWh()
        {
            string j = null;
            try
            {
                j = GetCurrentData();

                dynamic jsonResult = JsonConvert.DeserializeObject(j);

                // energy GRID in Wh since car connected
                double? grid = GetDoubleValue(jsonResult, "whg");

                // if grid energy is not reported (e.g. no go-e Controller data),
                // calculate it from the energy balance: wh = whs + whb + whg + who
                if (grid == null)
                {
                    double? total = GetDoubleValue(jsonResult, "wh");
                    double? solar = GetDoubleValue(jsonResult, "whs");
                    double? battery = GetDoubleValue(jsonResult, "whb");
                    double? other = GetDoubleValue(jsonResult, "who");

                    if (total != null && solar != null && battery != null && other != null)
                        grid = total - solar - battery - other;
                }

                if (grid == null)
                    return null;

                double divisor = GetWhToKwhDivisor(jsonResult);
                double v = (double)grid / divisor;
                v = Math.Round(v, divisor >= 1000.0 ? 3 : 1);

                return v;
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, j);
            }

            return null;
        }

        public override double? GetVehicleMeterReading_kWh()
        {
            string j = null;
            try
            {
                j = GetCurrentData();

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                double? v = GetDoubleValue(jsonResult, "eto");
                if (v == null)
                    return null;

                v = v / GetWhToKwhDivisor(jsonResult);

                return v;
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, j);
            }

            return null;
        }

        static double GetWhToKwhDivisor(dynamic jsonResult)
        {
            try
            {
                string fwv = jsonResult["fwv"];
                if (fwv != null && !string.IsNullOrEmpty(fwv))
                {
                    string majorPart = fwv.Split('.')[0];
                    if (double.TryParse(majorPart, out double majorVersion) && majorVersion >= 60.0)
                        return 1000.0;
                }
            }
            catch (Exception)
            {
            }
            return 10.0;
        }

        private double? GetDoubleValue(dynamic jsonResult, string key)
        {
            try
            {
                string value = jsonResult[key];
                if (value == null)
                    return null;

                if (string.IsNullOrEmpty(value))
                    return null;

                double d = Double.Parse(value, Tools.ciEnUS);
                if (double.IsNaN(d) || d == double.PositiveInfinity || d == double.NegativeInfinity)
                    return null;

                return d;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override bool? IsCharging()
        {
            string j = null;
            try
            {
                j = GetCurrentData();

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                string key = "car";
                string value = jsonResult[key];
                if (value == null)
                    return null;

                return value == "2";
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, j);
            }

            return null;
        }

        public override string GetVersion()
        {
            string j = null;
            try
            {
                j = GetCurrentData();

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                string key = "fwv";
                string value = jsonResult[key];
                if (value == null)
                    return null;

                return value;
            }
            catch (Exception ex)
            {
                if (!WebHelper.FilterNetworkoutage(ex))
                    ex.ToExceptionless().FirstCarUserID().Submit();

                Logfile.ExceptionWriter(ex, j);
            }

            return "";
        }
    }
}

