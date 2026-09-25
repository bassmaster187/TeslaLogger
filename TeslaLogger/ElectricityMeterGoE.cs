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

                string url = host + "/status";
                string lastJSON = client.GetStringAsync(url).GetAwaiter().GetResult();

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

                double v = (double)grid / (double)10.0;
                v = Math.Round(v, 1);

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

                v = v / (double)10.0;

                return v;
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, j);
            }

            return null;
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

