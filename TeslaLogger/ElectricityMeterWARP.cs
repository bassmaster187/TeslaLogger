using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Linq;
using System.Runtime.Caching;
using Exceptionless;
using Newtonsoft.Json;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    class ElectricityMeterWARP : ElectricityMeterBase
    {
        string host;
        string parameter;
        internal int wallboxMeterId = 0;
        internal int wallboxValueId = 209;
        internal int gridMeterId = 1;
        internal int gridValueId = 209;

        internal string mockup_info_version;
        internal string mockup_evse_state;
        internal string mockup_wallbox_value_ids;
        internal string mockup_wallbox_values;
        internal string mockup_grid_value_ids;
        internal string mockup_grid_values;


        static WebClient client;

        public ElectricityMeterWARP(string host, string parameter)
        {
            if (client == null)
            {
                client = new WebClient();
            }

            this.host = host;
            this.parameter = parameter;

            /*Example parameters: 
            "": wallbox internal meter with ID 0, value_id=209 and grid meter ID 1, value_id=209
            "W:2:210": only wallbox meter with ID 2 and value_id=210
            "G:2|W:0:210": Grid meter with ID 2 and default value_id=209, wallbox meter with ID 0 and value_id=210
            
            Default wallbox meter ID is 0
            Default grid meter ID is 1
            Default "energy import" ID is 209
            */

            var args = parameter.Split('|');
            foreach (var p in args)
            {
                if (p.StartsWith("W", StringComparison.InvariantCultureIgnoreCase))
                {
                    string[] parts = p.Split(':');
                    wallboxMeterId = parts.Length > 1 && int.TryParse(parts[1], out var tempMeterId) ? tempMeterId : 0;
                    wallboxValueId = parts.Length > 2 && int.TryParse(parts[2], out var tempValueId) ? tempValueId : 209;
                }
                if (p.StartsWith("G", StringComparison.InvariantCultureIgnoreCase))
                {
                    string[] parts = p.Split(':');
                    gridMeterId = parts.Length > 1 && int.TryParse(parts[1], out var tempMeterId) ? tempMeterId : 1;
                    gridValueId = parts.Length > 2 && int.TryParse(parts[2], out var tempValueId) ? tempValueId : 209;
                }
            }
        }

        public override double? GetUtilityMeterReading_kWh()
        {
            string value_ids = null;
            string values = null;

            try
            {
                if (mockup_grid_value_ids == null && mockup_grid_values == null)
                {
                    if (host == null)
                        return double.NaN;

                    string url1 = host + $"/meters/{gridMeterId}/value_ids";
                    value_ids = client.DownloadString(url1).Trim();

                    string url2 = host + $"/meters/{gridMeterId}/values";
                    values = client.DownloadString(url2).Trim();
                }
                else
                {
                    value_ids = mockup_grid_value_ids;
                    values = mockup_grid_values;
                }

                List<int> ids = value_ids.Trim('[', ']').Split(',').Select(int.Parse).ToList();
                int position = ids.IndexOf(gridValueId);

                List<double> value = values.Trim('[', ']').Split(',').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToList();

                if (position >= 0 && position < value.Count)
                {
                    return value[position];
                }

                return double.NaN;

            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, values);
            }
            return double.NaN;
        }

        public override double? GetVehicleMeterReading_kWh()
        {
            string value_ids = null;
            string values = null;

            try
            {
                if (mockup_wallbox_value_ids == null && mockup_wallbox_values == null)
                {
                    if (host == null)
                        return double.NaN;

                    string url1 = host + $"/meters/{wallboxMeterId}/value_ids";
                    value_ids = client.DownloadString(url1).Trim();

                    string url2 = host + $"/meters/{wallboxMeterId}/values";
                    values = client.DownloadString(url2).Trim();
                }
                else
                {
                    value_ids = mockup_wallbox_value_ids;
                    values = mockup_wallbox_values;
                }

                List<int> ids = value_ids.Trim('[', ']').Split(',').Select(int.Parse).ToList();
                int position = ids.IndexOf(wallboxValueId);

                List<double> value = values.Trim('[', ']').Split(',').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToList();

                if (position >= 0 && position < value.Count)
                {
                    return value[position];
                }

                return double.NaN;

            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, values);
            }
            return double.NaN;
        }

        public override bool? IsCharging()
        {
            string evse_state = null;

            try
            {
                if (mockup_evse_state == null && host != null)
                {
                    string url = host + "/evse/state";
                    evse_state = client.DownloadString(url).Trim();
                }
                else
                {
                    evse_state = mockup_evse_state;
                }


                dynamic jsonResult = JsonConvert.DeserializeObject(evse_state);

                if (jsonResult == null)
                    return null;

                return jsonResult["charger_state"] == 3 ? true : false;
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, evse_state);
            }
            return null;
        }

        public override string GetVersion()
        {
            string info_version = null;
            
            try
            {
                if (mockup_info_version == null && host != null)
                {
                    string url = host + "/info/version";
                    info_version = client.DownloadString(url).Trim();
                }
                else
                {
                    info_version = mockup_info_version;
                }

                
                dynamic jsonResult = JsonConvert.DeserializeObject(info_version);
                string value = jsonResult["firmware"];

                if (value == null)
                    return "";

                return value;
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, info_version);
            }

            return "";
        }
    }
}
