using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TeslaLogger
{
    public class DBHelper
    {
        public static CurrentJSON currentJSON = new CurrentJSON();

        private static Dictionary<string, int> mothershipCommands = new Dictionary<string, int>();
        private static bool mothershipEnabled = false;

        public static string DBConnectionstring => string.IsNullOrEmpty(ApplicationSettings.Default.DBConnectionstring)
                    ? "Server=127.0.0.1;Database=teslalogger;Uid=root;Password=teslalogger;CharSet=utf8;"
                    : ApplicationSettings.Default.DBConnectionstring;

        public static void EnableMothership()
        {
            GetMothershipCommandsFromDB();
            mothershipEnabled = true;
        }

        public static void UpdateHTTPStatusCodes()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                foreach (HttpStatusCode hsc in Enum.GetValues(typeof(HttpStatusCode)))
                {
                    MySqlCommand cmd = new MySqlCommand("insert IGNORE httpcodes (id, text) values (@id, @text)", con);
                    cmd.Parameters.AddWithValue("@id", (int)hsc);
                    cmd.Parameters.AddWithValue("@text", hsc.ToString());
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void CloseState(int maxPosid)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("update state set EndDate = @enddate, EndPos = @EndPos where EndDate is null", con);
                cmd.Parameters.AddWithValue("@enddate", DateTime.Now);
                cmd.Parameters.AddWithValue("@EndPos", maxPosid);
                cmd.ExecuteNonQuery();
            }

            currentJSON.CreateCurrentJSON();
        }

        public static void StartState(string state)
        {
            if (state != null)
            {
                if (state == "online")
                {
                    currentJSON.current_online = true;
                    currentJSON.current_sleeping = false;
                }
                else if (state == "asleep")
                {
                    currentJSON.current_online = false;
                    currentJSON.current_sleeping = true;
                }
                else if (state == "offline")
                {
                    currentJSON.current_online = false;
                    currentJSON.current_sleeping = false;
                }

                currentJSON.CreateCurrentJSON();
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();

                MySqlCommand cmd1 = new MySqlCommand("select state from state where EndDate is null", con);
                MySqlDataReader dr = cmd1.ExecuteReader();
                if (dr.Read())
                {
                    if (dr[0].ToString() == state)
                    {
                        return;
                    }
                }
                dr.Close();

                int MaxPosid = GetMaxPosid();
                CloseState(MaxPosid);

                //Logfile.Log("state: " + state);

                MySqlCommand cmd = new MySqlCommand("insert state (StartDate, state, StartPos) values (@StartDate, @state, @StartPos)", con);
                cmd.Parameters.AddWithValue("@StartDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@state", state);
                cmd.Parameters.AddWithValue("@StartPos", MaxPosid);
                cmd.ExecuteNonQuery();
            }
        }

        public static void AddMothershipDataToDB(string command, DateTime start, int httpcode)
        {
            if (mothershipEnabled == false)
            {
                return;
            }

            DateTime end = DateTime.UtcNow;
            TimeSpan ts = end - start;
            double duration = ts.TotalSeconds;
            AddMothershipDataToDB(command, duration, httpcode);
        }

        public static void AddMothershipDataToDB(string command, double duration, int httpcode)
        {

            if (!mothershipCommands.ContainsKey(command))
            {
                AddCommandToDB(command);
                GetMothershipCommandsFromDB();
            }
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("insert mothership (ts, commandid, duration, httpcode) values (@ts, @commandid, @duration, @httpcode)", con);
                cmd.Parameters.AddWithValue("@ts", DateTime.Now);
                cmd.Parameters.AddWithValue("@commandid", mothershipCommands[command]);
                cmd.Parameters.AddWithValue("@duration", duration);
                cmd.Parameters.AddWithValue("@httpcode", httpcode);
                cmd.ExecuteNonQuery();
            }
        }

        private static void AddCommandToDB(string command)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("insert mothershipcommands (command) values (@command)", con);
                cmd.Parameters.AddWithValue("@command", command);
                cmd.ExecuteNonQuery();
            }
        }

        private static void GetMothershipCommandsFromDB()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT id, command FROM mothershipcommands", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    int id = Convert.ToInt32(dr["id"]);
                    string command = dr[1].ToString();
                    if (!mothershipCommands.ContainsKey(command))
                    {
                        mothershipCommands.Add(command, id);
                    }
                }
            }
        }

        internal static string GetFirmwareFromDate(DateTime dateTime)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT version FROM car_version where StartDate < @date order by StartDate desc limit 1", con);
                cmd.Parameters.AddWithValue("@date", dateTime);

                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    string version = dr[0].ToString();

                    if (version.Contains(" "))
                    {
                        version = version.Substring(0, version.IndexOf(" "));
                    }

                    return version;
                }
            }

            return "";
        }

        public static void CloseChargingState()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("update chargingstate set EndDate = @EndDate, EndChargingID = @EndChargingID where EndDate is null", con);
                cmd.Parameters.AddWithValue("@EndDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@EndChargingID", GetMaxChargeid());
                cmd.ExecuteNonQuery();
            }

            currentJSON.current_charging = false;
            currentJSON.current_charger_power = 0;
            currentJSON.current_charger_voltage = 0;
            currentJSON.current_charger_phases = 0;
            currentJSON.current_charger_actual_current = 0;

            UpdateMaxChargerPower();

            Task.Factory.StartNew(() => CheckForInterruptedCharging(false));
        }

        public static void UpdateMaxChargerPower()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("select id, StartChargingID, EndChargingID from chargingstate order by id desc limit 1", con);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        int id = Convert.ToInt32(dr["id"]);
                        int StartChargingID = Convert.ToInt32(dr["StartChargingID"]);
                        int EndChargingID = Convert.ToInt32(dr["EndChargingID"]);

                        UpdateMaxChargerPower(id, StartChargingID, EndChargingID);
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.Message);
            }
        }

        internal static bool IndexExists(string index, string table)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM information_schema.statistics where table_name = '" + table + "' and INDEX_NAME ='" + index +"'", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    return true;
                }
            }

            return false;
        }

        private static void UpdateMaxChargerPower(int id, int startChargingID, int endChargingID)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("select max(charger_power) from charging where id >= @startChargingID and id <= @endChargingID ", con);
                cmd.Parameters.AddWithValue("@startChargingID", startChargingID);
                cmd.Parameters.AddWithValue("@endChargingID", endChargingID);

                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    if (dr[0] != DBNull.Value)
                    {
                        int max_charger_power = Convert.ToInt32(dr[0]);
                        ExecuteSQLQuery($"update chargingstate set max_charger_power={max_charger_power} where id = {id}");
                    }
                }

            }
        }

        internal static void GetEconomy_Wh_km(WebHelper wh)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand(@"SELECT  count(*) as anz, round(charging_End.charge_energy_added / (charging_End.ideal_battery_range_km - charging.ideal_battery_range_km), 3) AS economy_Wh_km
                        FROM charging inner JOIN chargingstate ON charging.id = chargingstate.StartChargingID
                        LEFT OUTER JOIN charging AS charging_End ON chargingstate.EndChargingID = charging_End.id
                        where TIMESTAMPDIFF(MINUTE, chargingstate.StartDate, chargingstate.EndDate) > 100
                        and chargingstate.EndChargingID - chargingstate.StartChargingID > 4
                        and charging_End.battery_level <= 90
                        group by economy_Wh_km
                        order by anz desc
                        limit 1 ", con);
                    MySqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        long anz = (long)dr["anz"];
                        double wh_km = (double)dr["economy_Wh_km"];

                        Logfile.Log($"Economy from DB: {wh_km} Wh/km - count: {anz}");

                        wh.carSettings.DB_Wh_TR = wh_km.ToString();
                        wh.carSettings.DB_Wh_TR_count = anz.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        internal static double GetLatestOdometer()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT EndKm FROM trip order by StartDate desc Limit 1", con);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        return (double)dr[0];
                    }
                }
            } catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "getLatestOdometer");
            }

            return 0;
        }

        internal static void UpdateAllChargingMaxPower()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("select id, StartChargingID, EndChargingID from chargingstate where max_charger_power is null", con);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        int id = Convert.ToInt32(dr["id"]);
                        int StartChargingID = Convert.ToInt32(dr["StartChargingID"]);
                        int EndChargingID = Convert.ToInt32(dr["EndChargingID"]);

                        UpdateMaxChargerPower(id, StartChargingID, EndChargingID);
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.Message);
            }
        }

        internal static void GetLastTrip()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM trip order by StartDate desc limit 1", con);
                    MySqlDataReader dr = cmd.ExecuteReader();

                    if (dr.Read())
                    {
                        currentJSON.current_trip_start = (DateTime)dr["StartDate"];
                        currentJSON.current_trip_end = (DateTime)dr["EndDate"];

                        if (dr["StartKm"] != DBNull.Value)
                        {
                            currentJSON.current_trip_km_start = Convert.ToDouble(dr["StartKm"]);
                        }

                        if (dr["EndKm"] != DBNull.Value)
                        {
                            currentJSON.current_trip_km_end = Convert.ToDouble(dr["EndKm"]);
                        }

                        if (dr["speed_max"] != DBNull.Value)
                        {
                            currentJSON.current_trip_max_speed = Convert.ToDouble(dr["speed_max"]);
                        }

                        if (dr["power_max"] != DBNull.Value)
                        {
                            currentJSON.current_trip_max_power = Convert.ToDouble(dr["power_max"]);
                        }

                        if (dr["StartRange"] != DBNull.Value)
                        {
                            currentJSON.current_trip_start_range = Convert.ToDouble(dr["StartRange"]);
                        }

                        if (dr["EndRange"] != DBNull.Value)
                        {
                            currentJSON.current_trip_end_range = Convert.ToDouble(dr["EndRange"]);
                        }
                    }
                    dr.Close();

                    cmd = new MySqlCommand("SELECT ideal_battery_range_km, battery_level, lat, lng FROM pos order by id desc limit 1", con);
                    dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        if (dr["ideal_battery_range_km"] != DBNull.Value)
                        {
                            currentJSON.current_ideal_battery_range_km = Convert.ToDouble(dr["ideal_battery_range_km"]);
                        }

                        if (dr["battery_level"] != DBNull.Value)
                        {
                            currentJSON.current_battery_level = Convert.ToInt32(dr["battery_level"]);
                        }

                        if (dr["lat"] != DBNull.Value)
                        {
                            currentJSON.latitude = Convert.ToDouble(dr["lat"]);
                        }

                        if (dr["lng"] != DBNull.Value)
                        {
                            currentJSON.longitude = Convert.ToDouble(dr["lng"]);
                        }
                    }
                    dr.Close();

                    currentJSON.CreateCurrentJSON();

                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static void StartChargingState(WebHelper wh)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("insert chargingstate (StartDate, Pos, StartChargingID, fast_charger_brand, fast_charger_type, conn_charge_cable , fast_charger_present ) values (@StartDate, @Pos, @StartChargingID, @fast_charger_brand, @fast_charger_type, @conn_charge_cable , @fast_charger_present)", con);
                cmd.Parameters.AddWithValue("@StartDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@Pos", GetMaxPosid());
                cmd.Parameters.AddWithValue("@StartChargingID", GetMaxChargeid() + 1);
                cmd.Parameters.AddWithValue("@fast_charger_brand", wh.fast_charger_brand);
                cmd.Parameters.AddWithValue("@fast_charger_type", wh.fast_charger_type);
                cmd.Parameters.AddWithValue("@conn_charge_cable", wh.conn_charge_cable);
                cmd.Parameters.AddWithValue("@fast_charger_present", wh.fast_charger_present);
                cmd.ExecuteNonQuery();
            }

            currentJSON.current_charging = true;
            currentJSON.CreateCurrentJSON();
        }

        public static void CloseDriveState(DateTime EndDate)
        {
            int StartPos = 0;
            int MaxPosId = GetMaxPosid();

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("select StartPos from drivestate where EndDate is null", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    StartPos = Convert.ToInt32(dr[0]);
                }
                dr.Close();
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("update drivestate set EndDate = @EndDate, EndPos = @Pos where EndDate is null", con);
                cmd.Parameters.AddWithValue("@EndDate", EndDate);
                cmd.Parameters.AddWithValue("@Pos", MaxPosId);
                cmd.ExecuteNonQuery();
            }

            if (StartPos != 0)
            {
                UpdateDriveStatistics(StartPos, MaxPosId);
            }

            currentJSON.current_driving = false;
            currentJSON.current_speed = 0;
            currentJSON.current_power = 0;

            Task.Factory.StartNew(() =>
              {
                  UpdateTripElevation(StartPos, MaxPosId);
              });
        }

        public static void UpdateTripElevation(int startPos, int maxPosId)
        {
            if (WebHelper.geofence.RacingMode)
            {
                return;
            }

            if (startPos == 0 || maxPosId == 0)
            {
                return;
            }

            Logfile.Log($"UpdateTripElevation start:{startPos} ende:{maxPosId}");

            string inhalt = "";
            try
            {
                //SRTM.Logging.LogProvider.SetCurrentLogProvider(SRTM.Logging.Logger.)
                SRTM.SRTMData srtmData = new SRTM.SRTMData(FileManager.GetSRTMDataPath());

                DataTable dt = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter($"SELECT id, lat, lng, odometer FROM pos where id > {startPos} and id < {maxPosId} and speed > 0 and altitude is null and lat is not null and lng is not null and lat > 0 and lng > 0 order by id", DBConnectionstring);
                da.Fill(dt);

                int x = 0;

                foreach (DataRow dr in dt.Rows)
                {
                    string sql = null;
                    try
                    {
                        double latitude = (double)dr[1];
                        double longitude = (double)dr[2];

                        int? height = srtmData.GetElevation(latitude, longitude);

                        if (height != null && height < 8000 && height > -428)
                        {
                            ExecuteSQLQuery($"update pos set altitude={height} where id={dr[0]}");
                        }

                        x++;

                        if (x > 250)
                        {
                            x = 0;
                            Logfile.Log($"UpdateTripElevation ID:{dr[0]}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.ExceptionWriter(ex, sql);
                    }
                }

            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, inhalt);
                Logfile.Log(ex.ToString());
            }

            Logfile.Log($"UpdateTripElevation finished start:{startPos} ende:{maxPosId}");
        }

        private static void UpdateTripElevationSubcall(DataTable dt, int Start, int End)
        {
            string resultContent = null;
            string url = null;

            try
            {
                Logfile.Log($"UpdateTripElevationSubcall start: {Start} end: {End} count:{dt.Rows.Count}");

                CultureInfo ci = CultureInfo.CreateSpecificCulture("en-US");
                StringBuilder sb = new StringBuilder();
                sb.Append("http://open.mapquestapi.com/elevation/v1/profile?key=");
                sb.Append(ApplicationSettings.Default.MapQuestKey);
                sb.Append("&latLngCollection=");

                bool first = true;
                for (int p = Start; p < End; p++)
                {
                    DataRow dr = dt.Rows[p];
                    if (!first)
                    {
                        sb.Append(",");
                    }

                    first = false;

                    double lat = (double)dr[1];
                    double lng = (double)dr[2];

                    sb.Append(lat.ToString(ci));
                    sb.Append(",");
                    sb.Append(lng.ToString(ci));
                }

                url = sb.ToString();

                WebClient webClient = new WebClient();

                webClient.Headers.Add("User-Agent: TeslaLogger");
                webClient.Encoding = Encoding.UTF8;
                resultContent = webClient.DownloadStringTaskAsync(new Uri(url)).Result;

                dynamic j = new JavaScriptSerializer().DeserializeObject(resultContent);
                System.Diagnostics.Debug.WriteLine("decode");

                if (!(resultContent.Contains("elevationProfile") && resultContent.Contains("shapePoints")))
                {
                    Logfile.Log("Mapquest Response: " + resultContent);
                    Logfile.ExceptionWriter(null, url + "\r\n\r\nResultContent:" + resultContent);
                    return;
                }

                dynamic sp = j["shapePoints"];

                object[] e = j["elevationProfile"];
                for (int i = 0; i < e.Length; i++)
                {
                    dynamic ep = e[i];
                    int height = ep["height"];
                    if (height == -32768) // no height data for this point
                    {
                        continue;
                    }

                    decimal lat = sp[i * 2];
                    decimal lng = sp[(i * 2) + 1];

                    DataRow[] drs = dt.Select($"lat={lat} and lng={lng}");
                    foreach (DataRow dr in drs)
                    {
                        string sql = null;
                        try
                        {
                            sql = $"update pos set altitude={height} where id={dr[0]}";
                            ExecuteSQLQuery(sql);
                        }
                        catch (Exception ex)
                        {
                            Logfile.ExceptionWriter(ex, sql);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("Mapquest Response: " + resultContent);
                Logfile.ExceptionWriter(ex, url + "\r\n\r\nResultContent:" + resultContent);
            }
        }

        public static void UpdateElevationForAllPoints()
        {
            try
            {
                /*
                if (String.IsNullOrEmpty(ApplicationSettings.Default.MapQuestKey))
                    return;
                    */

                int startid = 1;
                int count = 0;
                count = ExecuteSQLQuery($"update pos set altitude=null where altitude > 8000");
                if (count > 0)
                {
                    Logfile.Log($"Positions above 8000m updated: {count}");
                }

                count = ExecuteSQLQuery($"update pos set altitude=null where altitude < -428");
                if (count > 0)
                {
                    Logfile.Log($"Positions below -428m updated: {count}");
                }

                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT max(id) FROM pos where altitude > 0", con);
                    object o = cmd.ExecuteScalar();

                    if (o != null && o != DBNull.Value)
                    {
                        startid = Convert.ToInt32(o);
                    }
                }

                foreach (string f in System.IO.Directory.EnumerateFiles(FileManager.GetSRTMDataPath(), "*.txt"))
                {
                    try
                    {
                        if (new System.IO.FileInfo(f).Length < 100)
                        {
                            Logfile.Log("Found Empty SRTM File: " + f);
                            startid = 1;

                            System.IO.File.Delete(f);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.ToString());
                    }
                }

                UpdateTripElevation(startid, GetMaxPosid()); // get elevation for all points
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static void UpdateAddress(int posid)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("select lat, lng from pos where id = @id", con);
                    cmd.Parameters.AddWithValue("@id", posid);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        double lat = Convert.ToDouble(dr[0]);
                        double lng = Convert.ToDouble(dr[1]);
                        dr.Close();

                        WebHelper.ReverseGecocodingAsync(lat, lng).ContinueWith(task =>
                        {
                            try
                            {
                                using (MySqlConnection con2 = new MySqlConnection(DBConnectionstring))
                                {
                                    con2.Open();
                                    MySqlCommand cmd2 = new MySqlCommand("update pos set address = @adress where id = @id", con2);
                                    cmd2.Parameters.AddWithValue("@id", posid);
                                    cmd2.Parameters.AddWithValue("@adress", task.Result);
                                    cmd2.ExecuteNonQuery();

                                    GeocodeCache.Instance.Write();
                                }
                            }
                            catch (Exception ex)
                            {
                                Logfile.Log(ex.ToString());
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void UpdateDriveStatistics(int startPos, int endPos, bool logging = false)
        {
            try
            {
                if (logging)
                {
                    Logfile.Log("UpdateDriveStatistics");
                }

                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT avg(outside_temp) as outside_temp_avg, max(speed) as speed_max, max(power) as power_max, min(power) as power_min, avg(power) as power_avg FROM pos where id between @startpos and @endpos", con);
                    cmd.Parameters.AddWithValue("@startpos", startPos);
                    cmd.Parameters.AddWithValue("@endpos", endPos);

                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        using (MySqlConnection con2 = new MySqlConnection(DBConnectionstring))
                        {
                            con2.Open();
                            MySqlCommand cmd2 = new MySqlCommand("update drivestate set outside_temp_avg=@outside_temp_avg, speed_max=@speed_max, power_max=@power_max, power_min=@power_min, power_avg=@power_avg where StartPos=@StartPos and EndPos=@EndPos  ", con2);
                            cmd2.Parameters.AddWithValue("@StartPos", startPos);
                            cmd2.Parameters.AddWithValue("@EndPos", endPos);

                            cmd2.Parameters.AddWithValue("@outside_temp_avg", dr["outside_temp_avg"]);
                            cmd2.Parameters.AddWithValue("@speed_max", dr["speed_max"]);
                            cmd2.Parameters.AddWithValue("@power_max", dr["power_max"]);
                            cmd2.Parameters.AddWithValue("@power_min", dr["power_min"]);
                            cmd2.Parameters.AddWithValue("@power_avg", dr["power_avg"]);

                            cmd2.ExecuteNonQuery();
                        }
                    }
                }

                // If Startpos doesn't have an "ideal_battery_rage_km", it will be updated from the first valid dataset
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM pos where id = @startpos", con);
                    cmd.Parameters.AddWithValue("@startpos", startPos);

                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        if (dr["ideal_battery_range_km"] == DBNull.Value)
                        {
                            DateTime dt1 = (DateTime)dr["Datum"];
                            dr.Close();

                            cmd = new MySqlCommand("SELECT * FROM pos where id > @startPos and ideal_battery_range_km is not null and battery_level is not null order by id asc limit 1", con);
                            cmd.Parameters.AddWithValue("@startPos", startPos);
                            dr = cmd.ExecuteReader();

                            if (dr.Read())
                            {
                                DateTime dt2 = (DateTime)dr["Datum"];
                                TimeSpan ts = dt2 - dt1;

                                object ideal_battery_range_km = dr["ideal_battery_range_km"];
                                object battery_level = dr["battery_level"];

                                if (ts.TotalSeconds < 120)
                                {
                                    dr.Close();

                                    cmd = new MySqlCommand("update pos set ideal_battery_range_km = @ideal_battery_range_km, battery_level = @battery_level where id = @startPos", con);
                                    cmd.Parameters.AddWithValue("@startPos", startPos);
                                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", ideal_battery_range_km.ToString());
                                    cmd.Parameters.AddWithValue("@battery_level", battery_level.ToString());
                                    cmd.ExecuteNonQuery();

                                    Logfile.Log($"Trip from {dt1} ideal_battery_range_km updated!");
                                }
                                else
                                {
                                    // Logfile.Log($"Trip from {dt1} ideal_battery_range_km is NULL, but last valid data is too old: {dt2}!");
                                }
                            }
                        }
                    }
                }


                // If Endpos doesn't have an "ideal_battery_rage_km", it will be updated from the last valid dataset
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT * FROM pos where id = @endpos", con);
                    cmd.Parameters.AddWithValue("@endpos", endPos);

                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        if (dr["ideal_battery_range_km"] == DBNull.Value)
                        {
                            DateTime dt1 = (DateTime)dr["Datum"];
                            dr.Close();

                            cmd = new MySqlCommand("SELECT * FROM pos where id < @endpos and ideal_battery_range_km is not null and battery_level is not null order by id desc limit 1", con);
                            cmd.Parameters.AddWithValue("@endpos", endPos);
                            dr = cmd.ExecuteReader();

                            if (dr.Read())
                            {
                                DateTime dt2 = (DateTime)dr["Datum"];
                                TimeSpan ts = dt1 - dt2;

                                object ideal_battery_range_km = dr["ideal_battery_range_km"];
                                object battery_level = dr["battery_level"];

                                if (ts.TotalSeconds < 120)
                                {
                                    dr.Close();

                                    cmd = new MySqlCommand("update pos set ideal_battery_range_km = @ideal_battery_range_km, battery_level = @battery_level where id = @endpos", con);
                                    cmd.Parameters.AddWithValue("@endpos", endPos);
                                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", ideal_battery_range_km.ToString());
                                    cmd.Parameters.AddWithValue("@battery_level", battery_level.ToString());
                                    cmd.ExecuteNonQuery();

                                    Logfile.Log($"Trip from {dt1} ideal_battery_range_km updated!");
                                }
                                else
                                {
                                    // Logfile.Log($"Trip from {dt1} ideal_battery_range_km is NULL, but last valid data is too old: {dt2}!");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static bool UpdateIncompleteTrips()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand(@"SELECT pos_start.id as StartPos, pos_end.id as EndPos
                     FROM drivestate
                     JOIN pos pos_start ON drivestate . StartPos = pos_start. id
                     JOIN pos pos_end ON  drivestate . EndPos = pos_end. id
                     WHERE
                     (pos_end. odometer - pos_start. odometer ) > 0.1 and
                     (( pos_start. ideal_battery_range_km is null) or ( pos_end. ideal_battery_range_km is null))", con);
                    MySqlDataReader dr = cmd.ExecuteReader();

                    while (dr.Read())
                    {
                        try
                        {
                            int StartPos = Convert.ToInt32(dr[0]);
                            int EndPos = Convert.ToInt32(dr[1]);

                            UpdateDriveStatistics(StartPos, EndPos, false);
                        }
                        catch (Exception ex)
                        {
                            Logfile.Log(ex.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "UpdateIncompleteTrips");
            }

            return false;
        }

        public static void UpdateAllDrivestateData()
        {
            Logfile.Log("UpdateAllDrivestateData start");

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("select StartPos,EndPos from drivestate", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    try
                    {
                        int StartPos = Convert.ToInt32(dr[0]);
                        int EndPos = Convert.ToInt32(dr[1]);

                        UpdateDriveStatistics(StartPos, EndPos, false);
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.ToString());
                    }
                }
            }

            Logfile.Log("UpdateAllDrivestateData end");
        }

        public static void StartDriveState()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("insert drivestate (StartDate, StartPos) values (@StartDate, @Pos)", con);
                cmd.Parameters.AddWithValue("@StartDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@Pos", GetMaxPosid());
                cmd.ExecuteNonQuery();
            }

            currentJSON.current_driving = true;
            currentJSON.current_charge_energy_added = 0;
            currentJSON.current_trip_start = DateTime.Now;
            currentJSON.current_trip_end = DateTime.MinValue;
            currentJSON.current_trip_km_start = 0;
            currentJSON.current_trip_km_end = 0;
            currentJSON.current_trip_max_speed = 0;
            currentJSON.current_trip_max_power = 0;
            currentJSON.current_trip_start_range = 0;
            currentJSON.current_trip_end_range = 0;

            currentJSON.CreateCurrentJSON();
        }


        public static void InsertPos(string timestamp, double latitude, double longitude, int speed, decimal power, double odometer, double ideal_battery_range_km, int battery_level, double? outside_temp, string altitude)
        {
            double? inside_temp = currentJSON.current_inside_temperature;

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();

                MySqlCommand cmd = new MySqlCommand("insert pos (Datum, lat, lng, speed, power, odometer, ideal_battery_range_km, outside_temp, altitude, battery_level, inside_temp, battery_heater, is_preconditioning, sentry_mode) values (@Datum, @lat, @lng, @speed, @power, @odometer, @ideal_battery_range_km, @outside_temp, @altitude, @battery_level, @inside_temp, @battery_heater, @is_preconditioning, @sentry_mode )", con);
                cmd.Parameters.AddWithValue("@Datum", UnixToDateTime(long.Parse(timestamp)).ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@lat", latitude.ToString());
                cmd.Parameters.AddWithValue("@lng", longitude.ToString());
                cmd.Parameters.AddWithValue("@speed", (int)(speed * 1.60934M));
                cmd.Parameters.AddWithValue("@power", (int)(power * 1.35962M));
                cmd.Parameters.AddWithValue("@odometer", odometer.ToString());

                if (ideal_battery_range_km == -1)
                {
                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", ideal_battery_range_km.ToString());
                }

                if (outside_temp == null)
                {
                    cmd.Parameters.AddWithValue("@outside_temp", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@outside_temp", ((double)outside_temp).ToString());
                }

                if (altitude.Length == 0)
                {
                    cmd.Parameters.AddWithValue("@altitude", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@altitude", altitude);
                }

                if (battery_level == -1)
                {
                    cmd.Parameters.AddWithValue("@battery_level", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@battery_level", battery_level.ToString());
                }

                if (inside_temp == null)
                {
                    cmd.Parameters.AddWithValue("@inside_temp", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@inside_temp", ((double)inside_temp).ToString());
                }

                cmd.Parameters.AddWithValue("@battery_heater", currentJSON.current_battery_heater ? 1 : 0);
                cmd.Parameters.AddWithValue("@is_preconditioning", currentJSON.current_is_preconditioning ? 1 : 0);
                cmd.Parameters.AddWithValue("@sentry_mode", currentJSON.current_is_sentry_mode ? 1 : 0);

                cmd.ExecuteNonQuery();

                try
                {
                    currentJSON.current_speed = (int)(speed * 1.60934M);
                    currentJSON.current_power = (int)(power * 1.35962M);

                    if (odometer > 0)
                    {
                        currentJSON.current_odometer = odometer;
                    }

                    if (ideal_battery_range_km >= 0)
                    {
                        currentJSON.current_ideal_battery_range_km = ideal_battery_range_km;
                    }

                    if (currentJSON.current_trip_km_start == 0)
                    {
                        currentJSON.current_trip_km_start = odometer;
                        currentJSON.current_trip_start_range = currentJSON.current_ideal_battery_range_km;
                    }

                    currentJSON.current_trip_max_speed = Math.Max(currentJSON.current_trip_max_speed, currentJSON.current_speed);
                    currentJSON.current_trip_max_power = Math.Max(currentJSON.current_trip_max_power, currentJSON.current_power);

                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }
            }

            currentJSON.CreateCurrentJSON();
        }

        private static DateTime lastChargingInsert = DateTime.Today;


        internal static void InsertCharging(string timestamp, string battery_level, string charge_energy_added, string charger_power, double ideal_battery_range, string charger_voltage, string charger_phases, string charger_actual_current, double? outside_temp, bool forceinsert, string charger_pilot_current, string charge_current_request)
        {
            Tools.SetThread_enUS();

            if (charger_phases == "")
            {
                charger_phases = "1";
            }

            double kmRange = ideal_battery_range / (double)0.62137;

            double powerkW = Convert.ToDouble(charger_power);
            double waitbetween2pointsdb = 1000.0 / powerkW;

            double deltaSeconds = (DateTime.Now - lastChargingInsert).TotalSeconds;

            if (forceinsert || deltaSeconds > waitbetween2pointsdb)
            {
                lastChargingInsert = DateTime.Now;

                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("insert charging (Datum, battery_level, charge_energy_added, charger_power, ideal_battery_range_km, charger_voltage, charger_phases, charger_actual_current, outside_temp, charger_pilot_current, charge_current_request, battery_heater) values (@Datum, @battery_level, @charge_energy_added, @charger_power, @ideal_battery_range_km, @charger_voltage, @charger_phases, @charger_actual_current, @outside_temp, @charger_pilot_current, @charge_current_request, @battery_heater)", con);
                    cmd.Parameters.AddWithValue("@Datum", UnixToDateTime(long.Parse(timestamp)).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@battery_level", battery_level);
                    cmd.Parameters.AddWithValue("@charge_energy_added", charge_energy_added);
                    cmd.Parameters.AddWithValue("@charger_power", charger_power);
                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", kmRange.ToString());
                    cmd.Parameters.AddWithValue("@charger_voltage", int.Parse(charger_voltage));
                    cmd.Parameters.AddWithValue("@charger_phases", charger_phases);
                    cmd.Parameters.AddWithValue("@charger_actual_current", charger_actual_current);
                    cmd.Parameters.AddWithValue("@battery_heater", currentJSON.current_battery_heater ? 1 : 0);

                    if (charger_pilot_current != null && int.TryParse(charger_pilot_current, out int i))
                    {
                        cmd.Parameters.AddWithValue("@charger_pilot_current", i);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@charger_pilot_current", DBNull.Value);
                    }

                    if (charge_current_request != null && int.TryParse(charge_current_request, out i))
                    {
                        cmd.Parameters.AddWithValue("@charge_current_request", i);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@charge_current_request", DBNull.Value);
                    }

                    if (outside_temp == null)
                    {
                        cmd.Parameters.AddWithValue("@outside_temp", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@outside_temp", ((double)outside_temp).ToString());
                    }

                    cmd.ExecuteNonQuery();
                }
            }

            try
            {
                if (Convert.ToInt32(battery_level) >= 0 )
                {
                    currentJSON.current_battery_level = Convert.ToInt32(battery_level);
                }

                currentJSON.current_charge_energy_added = Convert.ToDouble(charge_energy_added);
                currentJSON.current_charger_power = Convert.ToInt32(charger_power);
                if (kmRange >= 0)
                {
                    currentJSON.current_ideal_battery_range_km = kmRange;
                }

                currentJSON.current_charger_voltage = int.Parse(charger_voltage);
                currentJSON.current_charger_phases = Convert.ToInt32(charger_phases);
                currentJSON.current_charger_actual_current = Convert.ToInt32(charger_actual_current);
                currentJSON.CreateCurrentJSON();
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static DateTime UnixToDateTime(long t)
        {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddMilliseconds(t);
            dt = dt.ToLocalTime();
            return dt;

        }

        public static int CountPos()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("Select count(*) from pos", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    return Convert.ToInt32(dr[0]);
                }
            }

            return 0;
        }

        public static int GetMaxPosid(bool withReverseGeocoding = true)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("Select max(id) from pos", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read() && dr[0] != DBNull.Value)
                {
                    int pos = Convert.ToInt32(dr[0]);
                    if (withReverseGeocoding)
                    {
                        UpdateAddress(pos);
                    }

                    return pos;
                }
            }

            return 0;
        }

        private static int GetMaxChargeid()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("Select max(id) from charging", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read() && dr[0] != DBNull.Value)
                {
                    return Convert.ToInt32(dr[0]);
                }
            }

            return 0;
        }

        internal static void SetCarVersion(string car_version)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("insert car_version (StartDate, version) values (@StartDate, @version)", con);
                    cmd.Parameters.AddWithValue("@StartDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@version", car_version);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, car_version);
            }
        }

        internal static string GetLastCarVersion()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("select version from car_version order by id desc limit 1", con);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        return dr[0].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "GetLastCarVersion");
                Logfile.Log(ex.ToString());
            }

            return "";
        }



        public static string GetVersion()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT @@version", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    return dr[0].ToString();
                }
            }

            return "NULL";
        }

        public static bool TableExists(string table)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM information_schema.tables where table_name = '" + table + "'", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    return true;
                }
            }

            return false;
        }


        public static bool ColumnExists(string table, string column)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SHOW COLUMNS FROM `"+ table +"` LIKE '"+ column +"';", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    return true;
                }
            }

            return false;
        }

        public static int ExecuteSQLQuery(string sql, int timeout = 30)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    if (timeout != 30)
                    {
                        cmd.CommandTimeout = timeout;
                    }

                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("Error in: " + sql);
                Logfile.ExceptionWriter(ex, sql);
                throw;
            }
        }

        public static void CheckForInterruptedCharging(bool logging)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand(@"SELECT chargingstate.id as chargingstate_id , StartDate, EndDate, charging.charge_energy_added as start_charge_energy_added,
                      charging_End.charge_energy_added,
                      charging.ideal_battery_range_km AS SOC,
                      charging_End.ideal_battery_range_km AS EndSOC,
                      charging_End.battery_level as End_battery_level,
                      pos.odometer
                        FROM charging inner JOIN chargingstate ON charging.id = chargingstate.StartChargingID INNER JOIN
                         pos ON chargingstate.pos = pos.id
                         LEFT OUTER JOIN
                         charging AS charging_End ON chargingstate.EndChargingID = charging_End.id
                    where TIMESTAMPDIFF(MINUTE, chargingstate.StartDate, chargingstate.EndDate) > 3 and chargingstate.EndChargingID - chargingstate.StartChargingID > 4
                    and charging.charge_energy_added > 1
                    order by StartDate desc", con);

                    double old_odometer = 0;

                    MySqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        double odometer = (double)dr["odometer"];
                        int chargingstate_id = (int)dr["chargingstate_id"];

                        if (old_odometer != odometer)
                        {
                            CombineChargingifNecessary(chargingstate_id, odometer, logging);
                            old_odometer = odometer;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "");
                throw;
            }
        }

        private static void CombineChargingifNecessary(int chargingstate_id, double odometer, bool logging)
        { 
            if (logging)
            {
                Logfile.Log($"CombineChargingifNecessary ID: {chargingstate_id} / Odometer: {odometer}");
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand(@"SELECT        chargingstate.id as chargingstate_id , StartDate, EndDate, charging.charge_energy_added as start_charge_energy_added,
                      charging_End.charge_energy_added,
                      charging.ideal_battery_range_km AS SOC,
                      charging_End.ideal_battery_range_km AS EndSOC,
                      charging_End.battery_level as End_battery_level,
                      pos.odometer, chargingstate.StartChargingID, chargingstate.EndChargingID
                        FROM charging inner JOIN chargingstate ON charging.id = chargingstate.StartChargingID INNER JOIN
                         pos ON chargingstate.pos = pos.id
                         LEFT OUTER JOIN
                         charging AS charging_End ON chargingstate.EndChargingID = charging_End.id
                    where TIMESTAMPDIFF(MINUTE, chargingstate.StartDate, chargingstate.EndDate) > 3 and chargingstate.EndChargingID - chargingstate.StartChargingID > 4
                    and odometer = @odometer and chargingstate.id < @chargingstate_id
                    order by StartDate desc", con);

                cmd.Parameters.AddWithValue("@odometer", odometer);
                cmd.Parameters.AddWithValue("@chargingstate_id", chargingstate_id);

                int newId = 0;
                DateTime newStartdate = DateTime.MinValue;
                int newStartChargingID = 0;

                MySqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    newId = (int)dr["chargingstate_id"];
                    newStartdate = (DateTime)dr["StartDate"];
                    newStartChargingID = (int)dr["StartChargingID"];

                    DeleteChargingstate(newId);
                    UpdateChargingstate(chargingstate_id, newStartdate, newStartChargingID);
                }
            }
        }

        private static void UpdateChargingstate(int chargingstate_id, DateTime StartDate, int StartChargingID)
        {
            try
            {
                Logfile.Log($"Update Chargingstate {chargingstate_id} with new StartDate: {StartDate} /  StartChargingID: {StartChargingID}");

                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand(@"update chargingstate set StartDate=@StartDate, StartChargingID=@StartChargingID where id = @id", con);
                    cmd.Parameters.AddWithValue("@id", chargingstate_id);
                    cmd.Parameters.AddWithValue("@StartDate", StartDate);
                    cmd.Parameters.AddWithValue("@StartChargingID", StartChargingID);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, chargingstate_id.ToString());
                Logfile.Log(ex.ToString());
            }
        }

        private static void DeleteChargingstate(int chargingstate_id)
        {
            try
            {
                Logfile.Log("Delete Chargingstate " + chargingstate_id.ToString());

                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand(@"delete from chargingstate where id = @id", con);
                    cmd.Parameters.AddWithValue("@id", chargingstate_id);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, chargingstate_id.ToString());
                Logfile.Log(ex.ToString());
            }
        }

        internal static int GetScanMyTeslaSignalsLastWeek()
        {
            string cacheKey = "GetScanMyTeslaSignalsLastWeek";
            object cacheValue = MemoryCache.Default.Get(cacheKey);
            if (cacheValue != null)
            {
                return (int)cacheValue;
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT count(*) FROM teslalogger.can where datum >= DATE(NOW()) - INTERVAL 7 DAY", con);
                    MySqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        int count = Convert.ToInt32(r[0]);

                        MemoryCache.Default.Add(cacheKey, count, DateTime.Now.AddHours(4));
                        return count;
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "GetScanMyTeslaPacketsLastWeek");
                Logfile.Log(ex.ToString());
            }
            return 0;
        }

        internal static int GetScanMyTeslaPacketsLastWeek()
        {
            string cacheKey = "GetScanMyTeslaPacketsLastWeek";
            object cacheValue = MemoryCache.Default.Get(cacheKey);
            if (cacheValue != null)
            {
                return (int)cacheValue;
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("select count(*) from (SELECT count(*) as cnt FROM teslalogger.can where datum >= DATE(NOW()) - INTERVAL 7 DAY group by UNIX_TIMESTAMP(datum)) as T1", con);
                    MySqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        int count = Convert.ToInt32(r[0]);

                        MemoryCache.Default.Add(cacheKey, count, DateTime.Now.AddHours(4));
                        return count;
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "GetScanMyTeslaPacketsLastWeek");
                Logfile.Log(ex.ToString());
            }
            return 0;
        }
        
        public static int GetAvgMaxRage()
        {
            string cacheKey = "GetAvgMaxRage";
            object cacheValue = MemoryCache.Default.Get(cacheKey);
            if (cacheValue != null)
            {
                return (int)cacheValue;
            }

            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand(@"SELECT AVG(charging_End.ideal_battery_range_km / charging_End.battery_level * 100) AS 'TRmax'
                        FROM charging
                        INNER JOIN chargingstate ON charging.id = chargingstate.StartChargingID
                        INNER JOIN pos ON chargingstate.pos = pos.id
                        LEFT OUTER JOIN charging AS charging_End ON chargingstate.EndChargingID = charging_End.id
                        WHERE chargingstate.StartDate > SUBDATE(Now(), INTERVAL 60 DAY) AND TIMESTAMPDIFF(MINUTE, chargingstate.StartDate, chargingstate.EndDate) > 3 and pos.odometer > 1
                    ", con);

                    MySqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        if (r[0] == DBNull.Value)
                        {
                            MemoryCache.Default.Add(cacheKey, 0, DateTime.Now.AddMinutes(5));
                            return 0;
                        }

                        int count = Convert.ToInt32(r[0]);
                        MemoryCache.Default.Add(cacheKey, count, DateTime.Now.AddHours(1));
                        return count;
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "GetAvgMaxRage");
                Logfile.Log(ex.ToString());
            }
            return 0;
        }

        public static string UpdateCountryCode()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("select lat, lng from pos where id = (select max(id) from pos)", con);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        double lat = Convert.ToDouble(dr[0]);
                        double lng = Convert.ToDouble(dr[1]);
                        dr.Close();

                        WebHelper.ReverseGecocodingAsync(lat, lng, true, false).Wait();
                        return currentJSON.current_country_code;
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            return "";
        }
    }
}
