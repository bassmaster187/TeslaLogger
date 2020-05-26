using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Text;

namespace TeslaLogger
{
    internal class ShareData
    {
        private string TaskerToken;
        private string TeslaloggerVersion;
        private static bool logwritten = false;
        private bool shareData = false;

        public ShareData(string TaskerToken)
        {
            this.TaskerToken = TaskerToken;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.TaskerToken = "00000000";
            }

            TeslaloggerVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            UpdateDataTable("chargingstate");

            if (!Tools.IsShareData())
            {
                if (!logwritten)
                {
                    logwritten = true;
                    Logfile.Log("ShareData: NOT Sharing Data! :-(");
                }

                shareData = false;
                return;
            }

            if (!logwritten)
            {
                logwritten = true;
                Logfile.Log("ShareData: Your charging data / degradation data will be shared anonymously to the community. Thank you!");
            }

            shareData = true;
        }

        private void UpdateDataTable(string table)
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand($"alter table {table} ADD column IF NOT EXISTS export TINYINT(1) NULL", con)
                {
                    CommandTimeout = 600
                };
                cmd.ExecuteNonQuery();
            }

        }

        public void SendAllChargingData()
        {
            if (!shareData)
            {
                return;
            }

            try
            {
                Logfile.Log("ShareData: SendAllChargingData start");

                int ProtocolVersion = 4;
                string sql = @"SELECT chargingstate.id as HostId, StartDate, EndDate, charging.charge_energy_added, conn_charge_cable, fast_charger_brand, fast_charger_type, fast_charger_present, address as pos_name, lat, lng, odometer, charging.outside_temp, StartChargingID, EndChargingID
                FROM chargingstate
                join pos on chargingstate.Pos = pos.id
                join charging on charging.id = chargingstate.EndChargingID
                where(export is null or export < " + ProtocolVersion + @") and (fast_charger_present or address like 'Supercharger%' or address like 'Ionity%' or max_charger_power > 25)
                order by StartDate
                ";

                DataTable dt = new DataTable();

                int ms = Environment.TickCount;

                MySqlDataAdapter da = new MySqlDataAdapter(sql, DBHelper.DBConnectionstring);
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(dt);
                ms = Environment.TickCount - ms;
                Logfile.Log("ShareData: SELECT chargingstate ms: " + ms);

                foreach (DataRow dr in dt.Rows)
                {
                    int HostId = Convert.ToInt32(dr["HostId"]);

                    Dictionary<string, object> d = new Dictionary<string, object>
                    {
                        { "ProtocolVersion", ProtocolVersion }
                    };
                    string Firmware = DBHelper.GetFirmwareFromDate((DateTime)dr["StartDate"]);
                    d.Add("Firmware", Firmware);

                    d.Add("TaskerToken", TaskerToken); // TaskerToken and HostId is the primary key and is used to make sure data won't be imported twice
                    foreach (DataColumn col in dt.Columns)
                    {
                        if (col.Caption.EndsWith("ChargingID"))
                        {
                            continue;
                        }

                        if (col.Caption.EndsWith("Date"))
                        {
                            d.Add(col.Caption, ((DateTime)dr[col.Caption]).ToString("s"));
                        }
                        else
                        {
                            d.Add(col.Caption, dr[col.Caption]);
                        }
                    }

                    List<object> l = GetChargingDT(Convert.ToInt32(dr["StartChargingID"]), Convert.ToInt32(dr["EndChargingID"]), out int count);
                    d.Add("teslalogger_version", TeslaloggerVersion);
                    d.Add("charging", l);

                    string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(d);

                    //string resultContent = "";
                    try
                    {
                        HttpClient client = new HttpClient();
                        StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                        DateTime start = DateTime.UtcNow;
                        HttpResponseMessage result = client.PostAsync("http://teslalogger.de/share_charging.php", content).Result;
                        string r = result.Content.ReadAsStringAsync().Result;
                        DBHelper.AddMothershipDataToDB("teslalogger.de/share_charging.php", start, (int)result.StatusCode);

                        //resultContent = result.Content.ReadAsStringAsync();
                        Logfile.Log("ShareData: " + r);

                        if (r.Contains("ERROR"))
                        {
                            Logfile.WriteException(r + "\r\n" + json);
                        }
                        else if (r.Contains("Insert OK:"))
                        {
                            DBHelper.ExecuteSQLQuery("update chargingstate set export=" + ProtocolVersion + "  where id = " + HostId);
                        }

                    }
                    catch (Exception ex)
                    {
                        Logfile.Log("ShareData: " + ex.Message);
                    }
                }

                Logfile.Log("ShareData: SendAllChargingData finished");
            }
            catch (Exception ex)
            {
                Logfile.Log("Error in ShareData:SendAllChargingData " + ex.Message);
                Logfile.WriteException(ex.ToString());
            }
        }

        private List<object> GetChargingDT(int startid, int endid, out int count)
        {
            count = 0;
            string sql = @"SELECT avg(unix_timestamp(Datum)) as Datum, avg(battery_level), avg(charger_power), avg(ideal_battery_range_km), avg(charger_voltage), avg(charger_phases), avg(charger_actual_current), max(battery_heater)
                FROM charging
                where id between @startid and @endid
                group by battery_level
                order by battery_level";

            DataTable dt = new DataTable();
            List<object> l = new List<object>();

            MySqlDataAdapter da = new MySqlDataAdapter(sql, DBHelper.DBConnectionstring);
            da.SelectCommand.Parameters.AddWithValue("@startid", startid);
            da.SelectCommand.Parameters.AddWithValue("@endid", endid);
            da.Fill(dt);

            foreach (DataRow dr in dt.Rows)
            {
                Dictionary<string, object> d = new Dictionary<string, object>();

                foreach (DataColumn col in dt.Columns)
                {
                    string name = col.Caption;
                    name = name.Replace("avg(","");
                    name = name.Replace("max(","");
                    name = name.Replace(")","");

                    if (name == "Datum")
                    {
                        long date = Convert.ToInt64(dr[col.Caption]) * 1000;
                        d.Add(name, DBHelper.UnixToDateTime(date).ToString("s"));
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

        public void SendDegradationData()
        {

            if (!shareData)
            {
                return;
            }

            try
            {
                int ProtocolVersion = 1;
                Logfile.Log("ShareData: SendDegradationData start");

                string sql = @"SELECT min(chargingstate.StartDate) as Date, 
                    (select LEFT(version,LOCATE(' ',version) - 1) from car_version where car_version.StartDate < min(chargingstate.StartDate) order by id desc limit 1) as v,  
                    odometer DIV 500 * 500 as odo, 
                    round(AVG(charging_End.ideal_battery_range_km / charging_End.battery_level * 100),0) AS 'TR',
                    round(avg(pos.outside_temp),0) as temp
                    FROM charging INNER JOIN chargingstate ON charging.id = chargingstate.StartChargingID
                    INNER JOIN pos ON chargingstate.pos = pos.id 
                    LEFT OUTER JOIN charging AS charging_End ON chargingstate.EndChargingID = charging_End.id
                    where odometer > 0
                    group by odo
                ";

                DataTable dt = new DataTable();

                int ms = Environment.TickCount;

                MySqlDataAdapter da = new MySqlDataAdapter(sql, DBHelper.DBConnectionstring);
                da.SelectCommand.CommandTimeout = 600;
                da.Fill(dt);
                ms = Environment.TickCount - ms;
                Logfile.Log("ShareData: SELECT degradation Data ms: " + ms);

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
                        if (col.Caption.EndsWith("Date"))
                        {
                            d.Add(col.Caption, ((DateTime)dr[col.Caption]).ToString("s"));
                        }
                        else
                        {
                            d.Add(col.Caption, dr[col.Caption]);
                        }
                    }
                    t.Add(d);
                }

                string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(d1);

                try
                {
                    HttpClient client = new HttpClient();
                    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                    DateTime start = DateTime.UtcNow;
                    HttpResponseMessage result = client.PostAsync("http://teslalogger.de/share_degradation.php", content).Result;
                    string r = result.Content.ReadAsStringAsync().Result;
                    DBHelper.AddMothershipDataToDB("teslalogger.de/share_degradation.php", start, (int)result.StatusCode);

                    //resultContent = result.Content.ReadAsStringAsync();
                    Logfile.Log("ShareData: " + r);

                    Logfile.Log("ShareData: SendDegradationData end");
                }
                catch (Exception ex)
                {
                    Logfile.Log("Error in ShareData:SendDegradationData " + ex.Message);
                }

            }
            catch (Exception ex)
            {
                Logfile.Log("Error in ShareData:SendDegradationData " + ex.Message);
                Logfile.WriteException(ex.ToString());
            }
        }
    }
}
