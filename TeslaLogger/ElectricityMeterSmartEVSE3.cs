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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    class ElectricityMeterSmartEVSE3 : ElectricityMeterBase
    {
        private string host;
        private string paramater;

        internal string mockup_status, mockup_shelly;

        Guid guid; // defaults to new Guid();
        static WebClient client;

        public ElectricityMeterSmartEVSE3(string host, string paramater)
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

                string cacheKey = "smartevse3_" + guid.ToString();
                object o = MemoryCache.Default.Get(cacheKey);

                if (o != null)
                {
                    return (string)o;
                }

                string url = host + "/settings";
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

                if (string.IsNullOrEmpty(j))
                    return null;

                dynamic jsonResult = JsonConvert.DeserializeObject(j);

                string value = jsonResult["mains_meter"]["import_active_energy"];

                return Double.Parse(value, Tools.ciEnUS);
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

                if (string.IsNullOrEmpty(j))
                    return null;

                dynamic jsonResult = JsonConvert.DeserializeObject(j);

                string value = jsonResult["ev_meter"]["import_active_energy"];

                return Double.Parse(value, Tools.ciEnUS);
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

                return jsonResult["evse"]["state_id"] == 2 ? true : false;


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

                string fwversion = jsonResult["version"];

                return fwversion;
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