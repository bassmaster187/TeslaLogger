using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TeslaLogger
{
    class ElectricityMeterGoE : ElectricityMeterBase
    {
        private string host;
        private string paramater;

        Guid guid = new Guid();
        static WebClient client;

        public ElectricityMeterGoE(string host, string paramater)
        {
            this.host = host;
            this.paramater = paramater;

            if (client == null)
                client = new WebClient();
        }


        string GetCurrentData()
        {
            try
            {
                string cacheKey = "goe_" + guid.ToString();
                object o = MemoryCache.Default.Get(cacheKey);

                if (o != null)
                    return (string)o;

                string url = host + "/status";
                string lastJSON = client.DownloadString(url);

                MemoryCache.Default.Add(cacheKey, lastJSON, DateTime.Now.AddSeconds(10));
                return lastJSON;
            }
            catch (Exception ex)
            {
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

                dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(j);
                string key = "eto";
                string value = jsonResult[key];

                double v = Double.Parse(value, Tools.ciEnUS);
                v = v / (double)10.0;

                return v;
            }
            catch (Exception ex)
            {
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

                dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(j);
                string key = "car";
                string value = jsonResult[key];

                return value == "2";
            }
            catch (Exception ex)
            {
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

                dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(j);
                string key = "fwv";
                string value = jsonResult[key];

                return value;
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, j);
            }

            return "";
        }
    }
}
