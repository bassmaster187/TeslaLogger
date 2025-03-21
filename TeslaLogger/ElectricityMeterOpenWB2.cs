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
            "G:7|CP:27": Grid meter with id = 7 and chargepoind id = 26
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

                string url = host + "/v1/?topic=" + topic;
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
            string h = null;
            string j = null;
            try
            {
                if (string.IsNullOrEmpty(gridmeterid))
                {
                    //get grid meter id via hierarchy:
                    if (mockup_hierarchy != null)
                    {
                        h = mockup_hierarchy;
                    }
                    else
                    {
                        h = GetCurrentData("openWB/counter/get/hierarchy");
                    }

                    JArray jsonArray = JArray.Parse(h);
                    JToken firstCounter = jsonArray
                        .FirstOrDefault(item => (string)item["type"] == "counter");

                    if (string.IsNullOrEmpty(firstCounter["id"].ToString()))
                        return null;

                    gridmeterid = firstCounter["id"].ToString();
                }

                if (mockup_grid != null)
                {
                    j = mockup_grid;
                }
                else
                {
                    j = GetCurrentData("openWB/counter/" + gridmeterid + "/get/imported");
                }
                
                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                if (jsonResult == null)
                    return null;

                if (!Tools.IsPropertyExist(jsonResult, "status"))
                    return null;

                Dictionary<string, object> r1 = jsonResult.ToObject<Dictionary<string, object>>();

                r1.TryGetValue("status", out object status);
                if (status.ToString() != "success")
                    return null;

                if (r1.ContainsKey("message"))
                {
                    double.TryParse(r1["message"].ToString(), out double value);
                    return value/1000;
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
                    if (mockup_hierarchy != null)
                    {
                        h = mockup_hierarchy;
                    }
                    else
                    {
                        h = GetCurrentData("openWB/counter/get/hierarchy");
                    }

                    JArray jsonArray = JArray.Parse(h);
                    JObject firstItem = (JObject)jsonArray[0];
                    JArray childrenArray = (JArray)firstItem["children"];
                    JToken cpEntry = childrenArray
                                        .Where(child => (string)child["type"] == "counter" && child["children"] != null)
                                        .SelectMany(child => child["children"])
                                        .Where(grandchild => (string)grandchild["type"] == "cp")
                                        .FirstOrDefault();

                    if (cpEntry == null)
                    {
                        return null;
                    }

                    chargepointid = cpEntry["id"].ToString();
                }

                if (mockup_charge_point != null)
                {
                    j = mockup_charge_point;
                }
                else
                {
                    j = GetCurrentData("openWB/chargepoint/" + chargepointid + "/get/imported");
                }

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                if (jsonResult == null)
                    return null;

                if (!Tools.IsPropertyExist(jsonResult, "status"))
                    return null;

                Dictionary<string, object> r1 = jsonResult.ToObject<Dictionary<string, object>>();

                r1.TryGetValue("status", out object status);
                if (status.ToString() != "success")
                    return null;

                if (r1.ContainsKey("message"))
                {
                    double.TryParse(r1["message"].ToString(), out double value);
                    return value/1000;
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

        public override bool? IsCharging()
        {
            string j = null;
            try
            {
                if (mockup_charge_state != null)
                {
                    j = mockup_charge_state;
                }
                else
                {
                    j = GetCurrentData("openWB/chargepoint/" + chargepointid + "/get/charge_state");
                }

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                if (jsonResult == null)
                    return null;

                if (!Tools.IsPropertyExist(jsonResult, "status"))
                    return null;

                Dictionary<string, object> r1 = jsonResult.ToObject<Dictionary<string, object>>();

                r1.TryGetValue("status", out object status);
                if (status.ToString() != "success")
                    return null;

                r1.TryGetValue("message", out object message);
                if (message.ToString().ToLower() == "true")
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
                if (mockup_version != null)
                {
                    j = mockup_version;
                }
                else
                {
                    j = GetCurrentData("openWB/system/version");
                }

                dynamic jsonResult = JsonConvert.DeserializeObject(j);
                if (jsonResult == null)
                    return null;

                if (!Tools.IsPropertyExist(jsonResult, "status"))
                    return null;

                Dictionary<string, object> r1 = jsonResult.ToObject<Dictionary<string, object>>();

                r1.TryGetValue("status", out object status);
                if (status.ToString() != "success")
                    return null;

                r1.TryGetValue("message", out object version);
                return version.ToString();
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
