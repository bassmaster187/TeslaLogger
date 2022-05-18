using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using Exceptionless;
using Newtonsoft.Json;
using System.Linq;

namespace TeslaLogger
{
    [SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    internal class ShareData
    {
        private readonly string TaskerToken;
        private readonly string TeslaloggerVersion;
        private static bool logwritten = false;
        private readonly bool shareData = false;
        readonly Car car;

        public ShareData(Car car)
        {
            this.TaskerToken = car.TaskerHash;
            this.car = car;


            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.TaskerToken = "00000000";
            }

            TeslaloggerVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            UpdateDataTable("chargingstate");
            UpdateDataTable("drivestate");

            if (!Tools.IsShareData())
            {
                if (!logwritten)
                {
                    logwritten = true;
                    car.Log("ShareData: NOT Sharing Data! :-(");
                }

                shareData = false;
                return;
            }

            if (!logwritten)
            {
                logwritten = true;
                car.Log("ShareData: Your charging data / degradation data will be shared anonymously to the community. Thank you!");
            }

            shareData = true;
        }

        [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
        static internal void UpdateDataTable(string table)
        {
            if (!DBHelper.ColumnExists(table, "export"))
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand($"alter table {table} ADD column export TINYINT(1) NULL", con)
                    {
                        CommandTimeout = 6000
                    })
                    {
                        SQLTracer.TraceNQ(cmd);
                    }
                }
            }
        }

        [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public void SendAllChargingData()
        {
            if (!shareData)
            {
                return;
            }

            try
            {
                car.Log("ShareData: SendAllChargingData start");

                int ProtocolVersion = 5;
                string sql = $@"SELECT
    chargingstate.id AS HostId,
    StartDate,
    EndDate,
    charging.charge_energy_added,
    conn_charge_cable,
    fast_charger_brand,
    fast_charger_type,
    fast_charger_present,
    address AS pos_name,
    lat,
    lng,
    odometer,
    charging.outside_temp,
    StartChargingID,
    EndChargingID
FROM
    chargingstate
JOIN
    pos
ON
    chargingstate.Pos = pos.id
JOIN
    charging
ON
    charging.id = chargingstate.EndChargingID
WHERE
    chargingstate.carid = {car.CarInDB} AND(
        EXPORT IS NULL OR EXPORT < {ProtocolVersion}
    ) AND(
        fast_charger_present
        OR address LIKE 'Supercharger%'
        OR address LIKE 'Ionity%'
        OR max_charger_power > 25
    )
ORDER BY
    StartDate";

                using (DataTable dt = new DataTable())
                {
                    int ms = Environment.TickCount;

                    using (MySqlDataAdapter da = new MySqlDataAdapter(sql, DBHelper.DBConnectionstring))
                    {
                        da.SelectCommand.CommandTimeout = 600;
                        SQLTracer.TraceDA(dt, da);
                        ms = Environment.TickCount - ms;
                        car.Log("ShareData: SELECT chargingstate ms: " + ms);

                        foreach (DataRow dr in dt.Rows)
                        {
                            // lat, lng
                            if (double.TryParse(dr["lat"].ToString(), out double lat) && double.TryParse(dr["lng"].ToString(), out double lng))
                            {
                                Address addr = Geofence.GetInstance().GetPOI(lat, lng, false);
                                if (addr != null && addr.IsHome)
                                {
                                    car.Log("Do not share ChargingData for +home (" + addr.name + ")");
                                    continue;
                                }
                                // get raw address w/o automatically added unicode characters
                                if (dr["pos_name"] != null && addr != null)
                                {
                                    dr["pos_name"] = addr.rawName;
                                }
                            }

                            int HostId = Convert.ToInt32(dr["HostId"], Tools.ciEnUS);

                            Dictionary<string, object> d = new Dictionary<string, object>
                    {
                        { "ProtocolVersion", ProtocolVersion }
                    };
                            string Firmware = car.DbHelper.GetFirmwareFromDate((DateTime)dr["StartDate"]);
                            d.Add("Firmware", Firmware);

                            d.Add("TaskerToken", TaskerToken); // TaskerToken and HostId is the primary key and is used to make sure data won't be imported twice
                            foreach (DataColumn col in dt.Columns)
                            {
                                if (col.Caption.EndsWith("ChargingID", StringComparison.Ordinal))
                                {
                                    continue;
                                }

                                if (col.Caption.EndsWith("Date", StringComparison.Ordinal))
                                {
                                    d.Add(col.Caption, ((DateTime)dr[col.Caption]).ToString("s", Tools.ciEnUS));
                                }
                                else
                                {
                                    d.Add(col.Caption, dr[col.Caption]);
                                }
                            }

                            List<object> l = GetChargingDT(Convert.ToInt32(dr["StartChargingID"], Tools.ciEnUS), Convert.ToInt32(dr["EndChargingID"], Tools.ciEnUS), out int count);
                            d.Add("teslalogger_version", TeslaloggerVersion);
                            d.Add("charging", l);

                            string json = JsonConvert.SerializeObject(d);

                            //string resultContent = "";
                            try
                            {
                                using (HttpClient client = new HttpClient())
                                {
                                    client.Timeout = TimeSpan.FromSeconds(30);
                                    using (StringContent content = new StringContent(json, Encoding.UTF8, "application/json"))
                                    {

                                        DateTime start = DateTime.UtcNow;
                                        HttpResponseMessage result = client.PostAsync(new Uri("http://teslalogger.de/share_charging.php"), content).Result;
                                        string r = result.Content.ReadAsStringAsync().Result;
                                        DBHelper.AddMothershipDataToDB("teslalogger.de/share_charging.php", start, (int)result.StatusCode);

                                        //resultContent = result.Content.ReadAsStringAsync();
                                        car.Log("ShareData: " + r);

                                        if (r.Contains("ERROR"))
                                        {
                                            Logfile.WriteException(r + "\r\n" + json);
                                        }
                                        else if (r.Contains("Insert OK:"))
                                        {
                                            DBHelper.ExecuteSQLQuery("update chargingstate set export=" + ProtocolVersion + "  where id = " + HostId);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                car.SendException2Exceptionless(ex);
                                car.Log("ShareData: " + ex.Message);
                            }
                        }

                        car.Log("ShareData: SendAllChargingData finished");
                    }
                    dt.Clear();
                }
            
            }
            catch (Exception ex)
            {
                car.SendException2Exceptionless(ex);

                car.Log("Error in ShareData:SendAllChargingData " + ex.Message);
                Logfile.WriteException(ex.ToString());
            }
        }

        private List<object> GetChargingDT(int startid, int endid, out int count)
        {
            count = 0;
            string sql = @"
SELECT
    AVG(UNIX_TIMESTAMP(Datum)) AS Datum,
    AVG(battery_level),
    AVG(charger_power),
    AVG(ideal_battery_range_km),
    AVG(charger_voltage),
    AVG(charger_phases),
    AVG(charger_actual_current),
    MAX(battery_heater),
    (
    SELECT
        val
    FROM
        can
    WHERE
        can.carid = @CarID
        AND can.datum < charging.Datum
        AND can.datum > DATE_ADD(charging.Datum, INTERVAL -3 MINUTE)
        AND id = 3
    ORDER BY
        can.datum DESC
LIMIT 1
) AS cell_temp
FROM
    charging
WHERE
    id BETWEEN @startid AND @endid
    AND carid = @CarID
GROUP BY
    battery_level
ORDER BY
    battery_level";

            using (DataTable dt = new DataTable())
            {
                List<object> l = new List<object>();

                using (MySqlDataAdapter da = new MySqlDataAdapter(sql, DBHelper.DBConnectionstring))
                {
                    da.SelectCommand.Parameters.AddWithValue("@CarID", car.CarInDB);
                    da.SelectCommand.Parameters.AddWithValue("@startid", startid);
                    da.SelectCommand.Parameters.AddWithValue("@endid", endid);
                    da.SelectCommand.CommandTimeout = 300;
                    SQLTracer.TraceDA(dt, da);

                    foreach (DataRow dr in dt.Rows)
                    {
                        Dictionary<string, object> d = new Dictionary<string, object>();

                        foreach (DataColumn col in dt.Columns)
                        {
                            string name = col.Caption;
                            name = name.Replace("AVG(", "");
                            name = name.Replace("MAX(", "");
                            name = name.Replace(")", "");

                            if (name == "Datum")
                            {
                                long date = Convert.ToInt64(dr[col.Caption], Tools.ciEnUS) * 1000;
                                d.Add(name, DBHelper.UnixToDateTime(date).ToString("s", Tools.ciEnUS));
                            }
                            else
                            {
                                d.Add(name, dr[col.Caption]);
                            }
                        }

                        l.Add(d);
                    }

                    count = dt.Rows.Count;

                    return l;
                }
            }
        }

        public void SendAllDrivingData()
        {
            if (!shareData)
            {
                return;
            }

            try
            {
                car.Log("ShareData: SendAllDrivingData start");

                int ProtocolVersion = 1;
                string sql = $@"SELECT 
    drivestate.id as hostid,
        (`pos_end`.`odometer` - `pos_start`.`odometer`) AS `km_diff`,
        (`pos_end`.`odometer` - `pos_start`.`odometer`) / (pos_start.battery_level - pos_end.battery_level) * 100  as max_range_km,
        pos_start.battery_level as StartSoc,
        pos_end.battery_level as EndSoc,
        ((`pos_start`.`ideal_battery_range_km` - `pos_end`.`ideal_battery_range_km`) * `cars`.`wh_tr`) AS `consumption_kWh`,
        ((((`pos_start`.`ideal_battery_range_km` - `pos_end`.`ideal_battery_range_km`) * `cars`.`wh_tr`) / (`pos_end`.`odometer` - `pos_start`.`odometer`)) * 100) AS `avg_consumption_kWh_100km`,
        TIMESTAMPDIFF(MINUTE,
            `drivestate`.`StartDate`,
            `drivestate`.`EndDate`) AS `DurationMinutes`,
        round(`pos_start`.`odometer`/5000)*5000 as odometer,
        `drivestate`.`outside_temp_avg` AS `outside_temp_avg`,
        `drivestate`.`speed_max` AS `speed_max`,
        `drivestate`.`power_max` AS `power_max`,
        `drivestate`.`power_min` AS `power_min`,
        `drivestate`.`power_avg` AS `power_avg`,
        (select km_diff / durationminutes * 60) as speed_avg,
        meters_up, meters_down, distance_up_km, distance_down_km, distance_flat_km,
        (select version from car_version where car_version.StartDate < drivestate.StartDate and car_version.carid = drivestate.carid order by id desc limit 1) as Firmware
    FROM
        (((`drivestate`
        JOIN `pos` `pos_start` ON ((`drivestate`.`StartPos` = `pos_start`.`id`)))
        JOIN `pos` `pos_end` ON ((`drivestate`.`EndPos` = `pos_end`.`id`)))
        JOIN `cars` ON ((`cars`.`id` = `drivestate`.`CarID`)))
    WHERE
		drivestate.CarID = {car.CarInDB}
        and (drivestate.export <> {ProtocolVersion} or drivestate.export is null)
        and ((`pos_end`.`odometer` - `pos_start`.`odometer`) > 99) 
        and pos_start.battery_level is not null 
        and pos_end.battery_level is not null 
        and `pos_end`.`odometer` - `pos_start`.`odometer` < 1000";

                using (DataTable dt = new DataTable())
                {
                    int ms = Environment.TickCount;

                    using (MySqlDataAdapter da = new MySqlDataAdapter(sql, DBHelper.DBConnectionstring))
                    {
                        da.SelectCommand.CommandTimeout = 600;
                        SQLTracer.TraceDA(dt, da);
                        ms = Environment.TickCount - ms;
                        car.Log("ShareData: SELECT drivestate ms: " + ms);

                        if (dt.Rows.Count == 0)
                            return;

                        Dictionary<string, object> d1 = new Dictionary<string, object>
                        {
                            { "ProtocolVersion", ProtocolVersion },
                            { "TaskerToken", TaskerToken } // TaskerToken is the primary key and is used to make sure data won't be imported twice
                        };

                        List<object> t = new List<object>();
                        d1.Add("T", t);

                        foreach (DataRow dr in dt.Rows)
                        {
                            Dictionary<string, object> d = new Dictionary<string, object>();
                            foreach (DataColumn col in dt.Columns)
                            {
                                d.Add(col.Caption, dr[col.Caption]);   
                            }
                            t.Add(d);
                        }

                        string json = JsonConvert.SerializeObject(d1);

                        try
                        {
                            using (HttpClient client = new HttpClient())
                            {
                                client.Timeout = TimeSpan.FromSeconds(30);
                                using (StringContent content = new StringContent(json, Encoding.UTF8, "application/json"))
                                {

                                    DateTime start = DateTime.UtcNow;
                                    HttpResponseMessage result = client.PostAsync(new Uri("http://teslalogger.de/share_drivestate.php"), content).Result;
                                    string r = result.Content.ReadAsStringAsync().Result;
                                    DBHelper.AddMothershipDataToDB("teslalogger.de/share_drivestate.php", start, (int)result.StatusCode);

                                    //resultContent = result.Content.ReadAsStringAsync();
                                    car.Log("ShareData: " + r);

                                    if (r == "OK" && dt.Rows.Count > 0)
                                    {
                                        var ids = from myrow in dt.AsEnumerable() select myrow["hostid"];
                                        var l =  String.Join(",", ids.ToArray());

                                        DBHelper.ExecuteSQLQuery($"update drivestate set export = {ProtocolVersion} where id in ({l})");
                                    }

                                    car.Log("ShareData: SendAllDrivingData end");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            car.SendException2Exceptionless(ex);

                            car.Log("Error in ShareData:SendAllDrivingData " + ex.Message);
                        }

                        dt.Clear();

                        car.Log("ShareData: SendAllDrivingData finished");
                    }
                }

            }
            catch (Exception ex)
            {
                car.SendException2Exceptionless(ex);

                car.Log("Error in ShareData:SendAllDrivingData " + ex.Message);
                Logfile.WriteException(ex.ToString());
            }
        }

        public void SendDegradationData()
        {

            if (!shareData)
            {
                return;
            }

            try
            {
                int ProtocolVersion = 1;
                car.Log("ShareData: SendDegradationData start");

                string sql = @"
SELECT
    MIN(chargingstate.StartDate) AS Date,
    (SELECT
        LEFT(VERSION, LOCATE(' ', VERSION) - 1)
    FROM
        car_version
    WHERE
        car_version.StartDate < MIN(chargingstate.StartDate)
        AND carid = @carid
    ORDER BY
        id DESC
    LIMIT 1
    ) AS v,
    odometer DIV 500 * 500 AS odo,
    ROUND(AVG(charging_End.ideal_battery_range_km / charging_End.battery_level * 100), 0) AS 'TR',
    ROUND(AVG(pos.outside_temp), 0) AS temp
FROM
    charging
INNER JOIN
    chargingstate
ON
    charging.id = chargingstate.StartChargingID
INNER JOIN
    pos
ON
    chargingstate.pos = pos.id
LEFT OUTER JOIN
    charging AS charging_End
ON
    chargingstate.EndChargingID = charging_End.id
WHERE
    odometer > 0
    AND chargingstate.carid = @carid
GROUP BY
    odo";

                using (DataTable dt = new DataTable())
                {

                    int ms = Environment.TickCount;

                    using (MySqlDataAdapter da = new MySqlDataAdapter(sql, DBHelper.DBConnectionstring))
                    {
                        da.SelectCommand.Parameters.AddWithValue("@carid", car.CarInDB);
                        da.SelectCommand.CommandTimeout = 600;
                        SQLTracer.TraceDA(dt, da);
                        ms = Environment.TickCount - ms;
                        car.Log("ShareData: SELECT degradation Data ms: " + ms);

                        Dictionary<string, object> d1 = new Dictionary<string, object>
                {
                    { "ProtocolVersion", ProtocolVersion },
                    { "TaskerToken", TaskerToken } // TaskerToken is the primary key and is used to make sure data won't be imported twice
                };

                        List<object> t = new List<object>();
                        d1.Add("T", t);

                        foreach (DataRow dr in dt.Rows)
                        {
                            Dictionary<string, object> d = new Dictionary<string, object>();
                            foreach (DataColumn col in dt.Columns)
                            {
                                if (col.Caption.EndsWith("Date", StringComparison.Ordinal))
                                {
                                    d.Add(col.Caption, ((DateTime)dr[col.Caption]).ToString("s", Tools.ciEnUS));
                                }
                                else
                                {
                                    d.Add(col.Caption, dr[col.Caption]);
                                }
                            }
                            t.Add(d);
                        }

                        string json = JsonConvert.SerializeObject(d1);

                        try
                        {
                            using (HttpClient client = new HttpClient())
                            {
                                client.Timeout = TimeSpan.FromSeconds(30);
                                using (StringContent content = new StringContent(json, Encoding.UTF8, "application/json"))
                                {

                                    DateTime start = DateTime.UtcNow;
                                    HttpResponseMessage result = client.PostAsync(new Uri("http://teslalogger.de/share_degradation.php"), content).Result;
                                    string r = result.Content.ReadAsStringAsync().Result;
                                    DBHelper.AddMothershipDataToDB("teslalogger.de/share_degradation.php", start, (int)result.StatusCode);

                                    //resultContent = result.Content.ReadAsStringAsync();
                                    car.Log("ShareData: " + r);

                                    car.Log("ShareData: SendDegradationData end");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            car.SendException2Exceptionless(ex);

                            car.Log("Error in ShareData:SendDegradationData " + ex.Message);
                        }
                    }
                    dt.Clear();
                }

            }
            catch (Exception ex)
            {
                car.SendException2Exceptionless(ex);

                car.Log("Error in ShareData:SendDegradationData " + ex.Message);
                Logfile.WriteException(ex.ToString());
            }
        }
    }
}
