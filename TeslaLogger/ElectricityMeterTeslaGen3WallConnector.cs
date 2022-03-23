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
    class ElectricityMeterTeslaGen3WallConnector : ElectricityMeterBase
    {
        string host;
        string parameter;
        internal int LP = 1;

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
                if (ex is WebException wx)
                {
                    if ((wx.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                    {
                        Logfile.Log(wx.Message);
                        return "";
                    }

                }
                ex.ToExceptionless().FirstCarUserID().Submit();
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
                if (ex is WebException wx)
                {
                    if ((wx.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                    {
                        Logfile.Log(wx.Message);
                        return "";
                    }

                }
                ex.ToExceptionless().FirstCarUserID().Submit();
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

                string url = host + "/api/1/version";
                string lastJSON = client.DownloadString(url);

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
                j = GetCurrentDataLifetime();
                if (string.IsNullOrEmpty(j))
                    return null;

                j = j.Replace("nan,", "null,");

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                string key = "energy_wh";
                string value = jsonResult[key].ToString();

                if (value == null)
                    return null;

                double v = Double.Parse(value, Tools.ciEnUS);
                v = v / 1000.0;

                return v;
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
                j = GetCurrentDataVitals();
                j = j.Replace("nan,", "null,");

                dynamic jsonResult = JsonConvert.DeserializeObject(j);

                bool vehicle_connected = jsonResult["vehicle_connected"];

                return vehicle_connected;
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
            string j = "";
            try
            {
                j = GetCurrentDataVersion();

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                string key = "firmware_version";
                string value = jsonResult[key];
                return value;
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().AddObject(j,"json").Submit();
                Logfile.Log(ex.ToString());
            }

            return "";
        }
    }
}
