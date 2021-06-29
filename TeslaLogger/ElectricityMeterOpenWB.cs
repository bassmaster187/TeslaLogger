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
    class ElectricityMeterOpenWB : ElectricityMeterBase
    {
        string host;
        string parameter;
        int LP = 1;

        Guid guid = new Guid();
        static WebClient client; 

        public ElectricityMeterOpenWB(string host, string parameter)
        {
            if (client == null)
                client = new WebClient();

            this.host = host;
            this.parameter = parameter;
        }

        string GetCurrentData()
        {
            try
            {
                string cacheKey = "openwb_" + guid.ToString();
                object o = MemoryCache.Default.Get(cacheKey);

                if (o != null)
                    return (string)o;

                string url = host + "/openWB/web/api.php?get=all";
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
            string j = null;
            try
            {
                j = GetCurrentData();

                dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(j);
                string value = jsonResult["evubezugWh"];

                double v = Double.Parse(value, Tools.ciEnUS);
                v = v / 1000;

                return v;
            }
            catch (Exception ex)
            {
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

                dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(j);
                string key = "llkwhLP" + LP;
                string value = jsonResult[key];

                double v = Double.Parse(value, Tools.ciEnUS);

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
                string key = "ladungaktivLP" + LP;
                string value = jsonResult[key];

                return value == "1";
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, j);
            }

            return null;
        }
    }
}
