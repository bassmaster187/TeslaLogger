using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using Exceptionless;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    class ElectricityMeterOpenWB2 : ElectricityMeterBase
    {
        string host;
        string parameter;
        string chargepointid;
        string gridmeterid;
        internal string api_state;

        internal string mockup_version;
        internal string mockup_charge_state;
        internal string mockup_charge_point;
        internal string mockup_grid;
        internal string mockup_hierarchy;

        Guid guid; // defaults to new Guid();
        static WebClient client;

        public ElectricityMeterOpenWB2(string host, string parameter)
        {
            if (client == null)
            {
                client = new WebClient();
            }

            this.host = host;
            this.parameter = parameter;


            /*
            Example parameters: 
            "": grid id will be assumed from hierarchy, first charge point id will be taken from hierarchy
            "CP:26": chargepoind id = 26, grid id will be assumed from hierarchy
            "G:7|CP:27": Grid meter with id = 7 and chargepoind id = 27
            */

            var args = parameter.Split('|');
            foreach (var p in args)
            {
                if (p.StartsWith("CP", StringComparison.InvariantCultureIgnoreCase))
                {
                    string[] parts = p.Split(':');
                    if (!string.IsNullOrEmpty(parts[1]))
                        chargepointid = parts[1];
                }
                if (p.StartsWith("G", StringComparison.InvariantCultureIgnoreCase))
                {
                    string[] parts = p.Split(':');
                    if (!string.IsNullOrEmpty(parts[1]))
                        gridmeterid = parts[1];
                }
            }
        }

        string GetCurrentData(string topic)
        {
            try
            {
                string cacheKey = "owb2_" + topic + guid.ToString();
                object o = MemoryCache.Default.Get(cacheKey);

                if (o != null)
                    return (string)o;

                string lastJSON = null;
                string url = host + "/v1/?topic=" + topic;

                //UnitTests
                if (mockup_hierarchy != null && mockup_version != null && mockup_grid != null && mockup_charge_state != null && mockup_charge_point != null)
                {
                    if (topic.Contains("hierarchy"))
                    {
                        lastJSON = mockup_hierarchy;
                    }
                    else if (topic.Contains("version"))
                    {
                        lastJSON = mockup_version;
                    }
                    else if (topic.Contains("chargepoint") && topic.Contains("charge_state"))
                    {
                        lastJSON = mockup_charge_state;
                    }
                    else if (topic.Contains("chargepoint") && topic.Contains("imported"))
                    {
                        lastJSON = mockup_charge_point;
                    }
                    else if (topic.Contains("counter") && topic.Contains("imported"))
                    {
                        lastJSON = mockup_grid;
                    }
                }
                else
                {
                    //real data
                    lastJSON = client.DownloadString(url);
                }

                dynamic jsonResult = JsonConvert.DeserializeObject(lastJSON);
                if (jsonResult == null)
                    return null;

                if (!Tools.IsPropertyExist(jsonResult, "status"))
                    return null;

                Dictionary<string, object> r1 = jsonResult.ToObject<Dictionary<string, object>>();

                r1.TryGetValue("status", out object status);
                if (status.ToString() != "success")
                    return null;

                if (!r1.ContainsKey("message"))
                {
                    return null;
                }

                string message = null;
                message = r1["message"].ToString();

                MemoryCache.Default.Add(cacheKey, message, DateTime.Now.AddSeconds(10));

                return message;
            }
            catch (Exception ex)
            {
                if (ex is WebException wx)
                {
                    if ((wx.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.NotFound)
                    {
                        Logfile.Log(wx.Message);
                        return null;
                    }

                }
                if (!WebHelper.FilterNetworkoutage(ex))
                    ex.ToExceptionless().FirstCarUserID().Submit();

                Logfile.Log(ex.ToString());
            }

            return null;
        }

        public override double? GetUtilityMeterReading_kWh()
        {
            string h = null;
            string j = null;
            try
            {
                if (string.IsNullOrEmpty(gridmeterid))
                {
                    //get grid meter id via hierarchy:
                    h = GetCurrentData("openWB/counter/get/hierarchy");

                    if (String.IsNullOrEmpty(h))
                        return double.NaN;

                    JArray jsonArray = JArray.Parse(h);
                    JToken firstCounter = jsonArray
                        .FirstOrDefault(item => (string)item["type"] == "counter");

                    if (string.IsNullOrEmpty(firstCounter["id"].ToString()))
                        return double.NaN;

                    gridmeterid = firstCounter["id"].ToString();
                }

                j = GetCurrentData("openWB/counter/" + gridmeterid + "/get/imported");
                
                if (String.IsNullOrEmpty(j))
                    return double.NaN;

                if (double.TryParse(j, out double value))
                {
                    return value / 1000;
                }
                else
                {
                    return double.NaN;
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
            string h = null;
            string j = null;
            try
            {
                if (string.IsNullOrEmpty(chargepointid))
                {
                    //no charge point id provided, get first charge point id via hierarchy:
                    h = GetCurrentData("openWB/counter/get/hierarchy");

                    if (String.IsNullOrEmpty(h))
                        return double.NaN;

                    JToken cpEntry = JArray.Parse(h)
                        .Cast<JObject>()
                        .SelectMany(o => o.DescendantsAndSelf().OfType<JObject>())
                        .FirstOrDefault(t => (string)t["type"] == "cp");

                    if (cpEntry == null)
                    {
                        return double.NaN;
                    }

                    chargepointid = cpEntry["id"].ToString();
                }

                j = GetCurrentData("openWB/chargepoint/" + chargepointid + "/get/imported");

                if (String.IsNullOrEmpty(j))
                    return double.NaN;

                if (double.TryParse(j, out double value))
                {
                    return value / 1000;
                }
                else
                {
                    return double.NaN;
                }

            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, j);
            }

            return double.NaN;
        }

        public override bool? IsCharging()
        {
            string j = null;
            try
            {
                j = GetCurrentData("openWB/chargepoint/" + chargepointid + "/get/charge_state");

                if (String.IsNullOrEmpty(j))
                    return false;

                if (j.ToLower() == "true")
                {
                    return true;
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
                j = GetCurrentData("openWB/system/version");

                return j;
            }
            catch (Exception ex)
            {
                if (!WebHelper.FilterNetworkoutage(ex))
                    ex.ToExceptionless().FirstCarUserID().Submit();

                Logfile.ExceptionWriter(ex, j);
            }

            return null;
        }

    }

}