using System;
using System.Net;
using System.Runtime.Caching;

using Exceptionless;
using Newtonsoft.Json;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    class ElectricityMeterOpenWB : ElectricityMeterBase
    {
        string host;
        string parameter;
        internal int LP = 1;

        Guid guid = new Guid();
        static WebClient client; 

        public ElectricityMeterOpenWB(string host, string parameter)
        {
            if (client == null)
            {
                client = new WebClient();
            }

            this.host = host;
            this.parameter = parameter;

            var args = parameter.Split('|');
            foreach (var p in args)
            {
                if (p.StartsWith("LP", StringComparison.InvariantCultureIgnoreCase))
                {
                    LP = int.Parse(p.Substring(2), Tools.ciDeDE);
                }
            }
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

                string value = jsonResult["evubezugWh"];               

                double v = Double.Parse(value, Tools.ciEnUS);
                v = v / 1000;

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
                string key = "llkwhLP" + LP;
                string value = jsonResult[key];

                double v = Double.Parse(value, Tools.ciEnUS);

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
                j = GetCurrentData();

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                if (jsonResult == null)
                    return null;

                string key = "ladungaktivLP" + LP;
                string value = jsonResult[key];

                return value == "1";
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
            try
            {
                string url = host + "/openWB/web/version?t="+new Guid().ToString();
                string v = client.DownloadString(url).Trim();
                return v;
            }
            catch (Exception ex)
            {
                if (!WebHelper.FilterNetworkoutage(ex))
                    ex.ToExceptionless().FirstCarUserID().Submit();

                Logfile.Log(ex.ToString());
            }

            return "";
        }

        public override string ToString()
        {
            string b = base.ToString();
            b += " / LP" + LP;

            return b;
        }
    }
}
