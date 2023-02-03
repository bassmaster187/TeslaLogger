using Exceptionless;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace TeslaLogger
{
    /**
     * https://shelly-api-docs.shelly.cloud/gen1/#shelly-3em
     */
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    class ElectricityMeterShellyEM : ElectricityMeterBase
    {
        private string host;
        private string paramater;

        internal string mockup_status, mockup_shelly;

        int channel = 0;

        Guid guid = new Guid();
        static WebClient client;

        public ElectricityMeterShellyEM(string host, string paramater)
        {
            this.host = host;
            this.paramater = paramater;

            if (paramater.IndexOf("C2", StringComparison.OrdinalIgnoreCase) >= 0)
                channel = 1;

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

                string cacheKey = "EM_" + guid.ToString();
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
                
                decimal value1 = jsonResult["emeters"][channel]["total"];

                return (double?)(value1) / 1000.0;
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
                decimal value1 = jsonResult["emeters"][channel]["power"];

                return value1 > 900;
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

        public override string ToString()
        {
            var isCharging = IsCharging();
            var vm = GetVehicleMeterReading_kWh();
            var evu = GetUtilityMeterReading_kWh();
            var version = GetVersion();

            string ret = $"IsCharging: {isCharging} / Vehicle Meter: {vm} kWh / Utility Meter: {evu ?? Double.NaN} kWh / Channel: {channel + 1} / Class: {this.GetType().Name} / Channel: {channel+1} / Version: {version}";
            return ret;
        }
    }
}
