using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Caching;
using Exceptionless;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    class ElectricityMeterEVCC : ElectricityMeterBase
    {
        string host;
        string parameter;
        string loadpointname;

        Guid guid; // defaults to new Guid();
        static WebClient client; 

        public ElectricityMeterEVCC(string host, string parameter)
        {
            if (client == null)
            {
                client = new WebClient();
            }

            this.host = host;
            this.parameter = parameter;

            if(parameter != null)
            {
                loadpointname = parameter;
            }
        }

        string GetCurrentData()
        {
            try
            {
                string cacheKey = "evcc_" + guid.ToString();
                object o = MemoryCache.Default.Get(cacheKey);

                if (o != null)
                    return (string)o;

                string url = host + "/api/state";
                string lastJSON = client.DownloadString(url);

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

                if (jsonResult == null)
                    return null;

                if (!Tools.IsPropertyExist(jsonResult, "result"))
                    return null;

                Dictionary<string, object> r1 = jsonResult["result"].ToObject<Dictionary<string, object>>();

                if (r1.ContainsKey("gridEnergy"))
                {
                    double.TryParse(r1["gridEnergy"].ToString(), out double value);

                    return value;
                }
                else
                {
                    return null;
                }
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
                if (jsonResult == null)
                    return null;

                if (!Tools.IsPropertyExist(jsonResult, "result"))
                    return null;

                JToken acme = jsonResult.SelectToken($"$.result.loadpoints[?(@.title == '{loadpointname}')]");

                Dictionary<string, object> r1 = acme.ToObject<Dictionary<string, object>>();

                if (r1.ContainsKey("chargeTotalImport"))
                {
                    double.TryParse(r1["chargeTotalImport"].ToString(), out double value);
                    return value;
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, j);
            }

            return null;
        }

        public override double? GetSessionPrice()
        {
            string j = null;
            try
            {
                j = GetCurrentData();

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                if (jsonResult == null)
                    return null;

                if (!Tools.IsPropertyExist(jsonResult, "result"))
                    return null;

                JToken acme = jsonResult.SelectToken($"$.result.loadpoints[?(@.title == '{loadpointname}')]");

                Dictionary<string, object> r1 = acme.ToObject<Dictionary<string, object>>();

                if (r1.ContainsKey("sessionPrice") && r1["sessionPrice"] != null)
                {
                    if(double.TryParse(r1["sessionPrice"].ToString(), out double value))
                        return value;
                    else
                        return null;
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, j);
            }

            return null;
        }

        public override bool? IsCharging()
        {
            string j = null;
            try
            {
                j = GetCurrentData();

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                if (jsonResult == null)
                    return null;

                if (!Tools.IsPropertyExist(jsonResult, "result"))
                    return null;

                JToken acme = jsonResult.SelectToken($"$.result.loadpoints[?(@.title == '{loadpointname}')]");

                Dictionary<string, object> r1 = acme.ToObject<Dictionary<string, object>>();

                if (r1.ContainsKey("charging"))
                {
                    r1.TryGetValue("charging", out object value);
                    return (bool)value;
                }
                else
                {
                    return false;
                }

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
                if (jsonResult == null)
                    return null;

                if (!Tools.IsPropertyExist(jsonResult, "result"))
                    return null;

                Dictionary<string, object> r1 = jsonResult["result"].ToObject<Dictionary<string, object>>();

                r1.TryGetValue("version", out object value);
                
                return value.ToString();
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
