using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

using Exceptionless;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TeslaLogger
{
    /**
     * https://www.cfos-emobility.de/de/cfos-power-brain/http-api.htm
     */
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    class ElectricityMeterCFos : ElectricityMeterBase
    {
        private string host;
        private string paramater;

        internal string get_dev_info;

        Guid guid; // defaults to new Guid();
        static WebClient client;

        public ElectricityMeterCFos(string host, string paramater)
        {
            this.host = host;
            this.paramater = paramater;

            if (client == null)
            {
                client = new WebClient();
            }
        }

        string GetCurrentData()
        {
            try
            {
                if (get_dev_info != null)
                {
                    return get_dev_info;
                }

                string cacheKey = "CFos_" + guid.ToString();
                object o = MemoryCache.Default.Get(cacheKey);

                if (o != null)
                {
                    return (string)o;
                }

                string url = host + "/cnf?cmd=get_dev_info";
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

                if (!Tools.IsPropertyExist(jsonResult, "devices"))
                    return null;

                JToken acme = jsonResult.SelectToken($"$.devices[?(@.role == 3)]"); //3 - grid

                Dictionary<string, object> r1 = acme.ToObject<Dictionary<string, object>>();


                if (r1.ContainsKey("import"))
                {
                    double.TryParse(r1["import"].ToString(), out double value);
                    return value / 1000; //Wh -> kWh
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

                if (!Tools.IsPropertyExist(jsonResult, "devices"))
                    return null;

                               
                JToken acme = jsonResult.SelectToken($"$.devices[?(@.is_evse == true)]");

                Dictionary<string, object> r1 = acme.ToObject<Dictionary<string, object>>();

                if (r1.ContainsKey("total_energy"))
                {
                    double.TryParse(r1["total_energy"].ToString(), out double value);
                    return value / 1000; //Wh -> kWh
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

                if (!Tools.IsPropertyExist(jsonResult, "devices"))
                    return null;

                JToken acme = jsonResult.SelectToken($"$.devices[?(@.is_evse == true)].evse");

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
                string value = jsonResult["params"]["version"];

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
