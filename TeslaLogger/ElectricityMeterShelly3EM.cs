using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

using Exceptionless;
using Newtonsoft.Json;

namespace TeslaLogger
{
    /**
     * https://shelly-api-docs.shelly.cloud/gen1/#shelly-3em
     */
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    class ElectricityMeterShelly3EM : ElectricityMeterBase
    {
        private string host;
        private string paramater;

        internal string mockup_status, mockup_shelly;

        Guid guid; // defaults to new Guid();
        static WebClient client;

        public ElectricityMeterShelly3EM(string host, string paramater)
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
                if (mockup_status != null)
                {
                    return mockup_status;
                }

                string cacheKey = "3EM_" + guid.ToString();
                object o = MemoryCache.Default.Get(cacheKey);

                if (o != null)
                {
                    return (string)o;
                }

                string url = host + "/status";
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
            return null;
        }

        public override double? GetVehicleMeterReading_kWh()
        {
            string j = null;
            try
            {
                j = GetCurrentData();

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                decimal value1 = jsonResult["emeters"][0]["total"];
                decimal value2 = jsonResult["emeters"][1]["total"];
                decimal value3 = jsonResult["emeters"][2]["total"];
                
                return (double?)(value1 + value2 + value3)/1000.0;
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
                decimal value1 = jsonResult["emeters"][0]["power"];
                decimal value2 = jsonResult["emeters"][1]["power"];
                decimal value3 = jsonResult["emeters"][2]["power"];
                decimal watt_total = (value1 + value2 + value3);

                return watt_total > 3000;
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
                if (mockup_shelly == null)
                {
                    string url = host + "/shelly";
                    j = client.DownloadString(url);
                }
                else
                {
                    j = mockup_shelly;
                }

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                string key = "fw";
                string value = jsonResult[key];

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
