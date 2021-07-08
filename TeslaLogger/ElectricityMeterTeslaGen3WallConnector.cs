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
    class ElectricityMeterTeslaGen3WallConnector : ElectricityMeterBase
    {
        string host;
        string parameter;
        internal int LP = 1;

        Guid guid = new Guid();
        static WebClient client;
        internal string mockup_lifetime, mockup_version, mockup_vitals;

        public ElectricityMeterTeslaGen3WallConnector(string host, string parameter)
        {
            if (client == null)
                client = new WebClient();

            this.host = host;
            this.parameter = parameter;
        }

        string GetCurrentDataLifetime()
        {
            try
            {
                if (mockup_lifetime != null)
                    return mockup_lifetime;

                string url = host + "/api/1/lifetime";
                string lastJSON = client.DownloadString(url);

                return lastJSON;
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            return "";
        }

        string GetCurrentDataVitals()
        {
            try
            {
                if (mockup_vitals != null)
                    return mockup_vitals;

                string url = host + "/api/1/vitals";
                string lastJSON = client.DownloadString(url);

                return lastJSON;
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            return "";
        }

        string GetCurrentDataVersion()
        {
            try
            {
                if (mockup_version != null)
                    return mockup_version;

                string url = host + "/api/1/vitals";
                string lastJSON = client.DownloadString(url);

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
                j = GetCurrentDataLifetime();

                dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(j);
                string key = "energy_wh";
                string value = jsonResult[key];

                double v = Double.Parse(value, Tools.ciEnUS);
                v = v / 1000.0;

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
                j = GetCurrentDataVitals();

                dynamic jsonResult = new JavaScriptSerializer().DeserializeObject(j);
                string key = "session_s";
                string value = jsonResult[key];

                return value == "1";
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, j);
            }

            return null;
        }

        public override string GetVersion()
        {
            try
            {
                string url = host + "/openWB/web/version?t=" + new Guid().ToString();
                string v = client.DownloadString(url).Trim();
                return v;
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            return "";
        }
    }
}
