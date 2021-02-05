﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TeslaLogger
{
    public class DBHelper
    {
        private static Dictionary<string, int> mothershipCommands = new Dictionary<string, int>();
        private static bool mothershipEnabled = false;
        private Car car;

        internal static string Database = "teslalogger";

        public static string DBConnectionstring => GetDBConnectionstring();

        private static string _DBConnectionstring = string.Empty;
        private static string GetDBConnectionstring()
        {
            if (!string.IsNullOrEmpty(_DBConnectionstring))
            {
                return _DBConnectionstring;
            }
            string DBConnectionstring = string.IsNullOrEmpty(ApplicationSettings.Default.DBConnectionstring)
? "Server=127.0.0.1;Database=teslalogger;Uid=root;Password=teslalogger;CharSet=utf8mb4;"
: ApplicationSettings.Default.DBConnectionstring;
            if (DBConnectionstring.ToLower().Contains("charset="))
            {
                Match m = Regex.Match(DBConnectionstring.ToLower(), "charset(=.+?);");
                if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
                {
                    DBConnectionstring = DBConnectionstring.Replace(m.Groups[1].Captures[0].ToString(), "=utf8mb4");
                    _DBConnectionstring = DBConnectionstring;
                }
                else
                {
                    m = Regex.Match(DBConnectionstring.ToLower(), "charset(=.+)$");
                    if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
                    {
                        DBConnectionstring = DBConnectionstring.Replace(m.Groups[1].Captures[0].ToString(), "=utf8mb4");
                        _DBConnectionstring = DBConnectionstring;
                    }
                }
            }
            if (!DBConnectionstring.ToLower().Contains("charset="))
            {
                if (!DBConnectionstring.EndsWith(";"))
                {
                    DBConnectionstring += ";";
                }
                DBConnectionstring += "charset=utf8mb4";
            }
            if (DBConnectionstring.ToLower().Contains("database="))
            {
                Match m = Regex.Match(DBConnectionstring.ToLower(), "database=(.+?);");
                if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
                {
                    Database = m.Groups[1].Captures[0].ToString();
                }
            }
            _DBConnectionstring = DBConnectionstring;
            return _DBConnectionstring;
        }

        public DBHelper(Car car)
        {
            this.car = car;
        }

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
                    using (MySqlCommand cmd = new MySqlCommand("insert IGNORE httpcodes (id, text) values (@id, @text)", con))
                    {
                        cmd.Parameters.AddWithValue("@id", (int)hsc);
                        cmd.Parameters.AddWithValue("@text", hsc.ToString());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void CloseState(int maxPosid)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("update state set EndDate = @enddate, EndPos = @EndPos where EndDate is null and CarID=@CarID", con))
                {
                    cmd.Parameters.AddWithValue("@enddate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@EndPos", maxPosid);
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    cmd.ExecuteNonQuery();
                }
            }

            car.currentJSON.CreateCurrentJSON();
        }

        public void StartState(string state)
        {
            if (state != null)
            {
                if (state == "online")
                {
                    car.currentJSON.current_online = true;
                    car.currentJSON.current_sleeping = false;
                }
                else if (state == "asleep")
                {
                    car.currentJSON.current_online = false;
                    car.currentJSON.current_sleeping = true;
                }
                else if (state == "offline")
                {
                    car.currentJSON.current_online = false;
                    car.currentJSON.current_sleeping = false;
                }

                car.currentJSON.CreateCurrentJSON();
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();

                using (MySqlCommand cmd1 = new MySqlCommand("select state from state where EndDate is null and CarID=@carid", con))
                {
                    cmd1.Parameters.AddWithValue("@carid", car.CarInDB);
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

                    car.Log("state: " + state);

                    using (MySqlCommand cmd = new MySqlCommand("insert state (StartDate, state, StartPos, CarID) values (@StartDate, @state, @StartPos, @CarID)", con))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@state", state);
                        cmd.Parameters.AddWithValue("@StartPos", MaxPosid);
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.ExecuteNonQuery();
                    }
                }
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
                using (MySqlCommand cmd = new MySqlCommand("insert mothership (ts, commandid, duration, httpcode) values (@ts, @commandid, @duration, @httpcode)", con))
                {
                    cmd.Parameters.AddWithValue("@ts", DateTime.Now);
                    cmd.Parameters.AddWithValue("@commandid", mothershipCommands[command]);
                    cmd.Parameters.AddWithValue("@duration", duration);
                    cmd.Parameters.AddWithValue("@httpcode", httpcode);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        internal static void SetCost(string[] args)
        {
            try
            {
                Logfile.Log("SetCost");

                string json = System.IO.File.ReadAllText(FileManager.GetSetCostPath);
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("update chargingstate set cost_total = @cost_total, cost_currency=@cost_currency, cost_per_kwh=@cost_per_kwh, cost_per_session=@cost_per_session, cost_per_minute=@cost_per_minute, cost_idle_fee_total=@cost_idle_fee_total where id= @id", con))
                    {

                        if (j["cost_total"] == null || j["cost_total"] == "" || j["cost_total"] == "0" || j["cost_total"] == "0.00")
                        {
                            cmd.Parameters.AddWithValue("@cost_total", DBNull.Value);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@cost_total", j["cost_total"]);
                        }

                        cmd.Parameters.AddWithValue("@cost_currency", j["cost_currency"]);
                        cmd.Parameters.AddWithValue("@cost_per_kwh", j["cost_per_kwh"]);
                        cmd.Parameters.AddWithValue("@cost_per_session", j["cost_per_session"]);
                        cmd.Parameters.AddWithValue("@cost_per_minute", j["cost_per_minute"]);
                        cmd.Parameters.AddWithValue("@cost_idle_fee_total", j["cost_idle_fee_total"]);
                        cmd.Parameters.AddWithValue("@id", j["id"]);
                        int done = cmd.ExecuteNonQuery();

                        Logfile.Log("SetCost OK: " + done);
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal static void UpdateCarIDNull()
        {
            string[] tables = { "can", "car_version", "charging", "chargingstate", "drivestate", "pos", "shiftstate", "state" };
            foreach (string table in tables) {
                try
                {
                    int rows = 0;
                    int t = Environment.TickCount;
                    using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                    {
                        con.Open();
                        using (MySqlCommand cmd = new MySqlCommand($"update {table} set carid = 1 where carid is null", con))
                        {
                            cmd.CommandTimeout = 6000;
                            rows = cmd.ExecuteNonQuery();
                        }
                    }
                    t = Environment.TickCount - t;

                    if (rows > 0)
                        Logfile.Log($"update {table} set carid = 1 where carid is null; ms: {t} / Rows: {rows}");

                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }
            }
        }

        internal string GetRefreshToken()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT refresh_token FROM cars where id = @CarID", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);

                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        string refresh_token = dr[0].ToString();
                        return refresh_token;
                    }
                }
            }

            return "";
        }

        internal void UpdateTeslaToken()
        {
            try
            {
                car.Log("UpdateTeslaToken");
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("update cars set tesla_token = @tesla_token, tesla_token_expire=@tesla_token_expire where id=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", car.CarInDB);
                        cmd.Parameters.AddWithValue("@tesla_token", car.webhelper.Tesla_token);
                        cmd.Parameters.AddWithValue("@tesla_token_expire", DateTime.Now);
                        int done = cmd.ExecuteNonQuery();

                        car.Log("update tesla_token OK: " + done);
                    }
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
            }
        }

        internal void WriteCarSettings()
        {
            try
            {
                car.Log("UpdateTeslaToken");
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("update cars set display_name=@display_name, Raven=@Raven, Wh_TR=@Wh_TR, DB_Wh_TR=@DB_Wh_TR, DB_Wh_TR_count=@DB_Wh_TR_count, car_type=@car_type, car_special_type=@car_special_type, car_trim_badging=@trim_badging, model_name=@model_name, Battery=@Battery, tasker_hash=@tasker_hash, vin=@vin where id=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", car.CarInDB);
                        cmd.Parameters.AddWithValue("@Raven", car.Raven);
                        cmd.Parameters.AddWithValue("@Wh_TR", car.Wh_TR);
                        cmd.Parameters.AddWithValue("@DB_Wh_TR", car.DB_Wh_TR);
                        cmd.Parameters.AddWithValue("@DB_Wh_TR_count", car.DB_Wh_TR_count);
                        cmd.Parameters.AddWithValue("@car_type", car.car_type);
                        cmd.Parameters.AddWithValue("@car_special_type", car.car_special_type);
                        cmd.Parameters.AddWithValue("@trim_badging", car.trim_badging);
                        cmd.Parameters.AddWithValue("@model_name", car.ModelName);
                        cmd.Parameters.AddWithValue("@Battery", car.Battery);
                        cmd.Parameters.AddWithValue("@display_name", car.display_name);
                        cmd.Parameters.AddWithValue("@tasker_hash", car.TaskerHash);
                        cmd.Parameters.AddWithValue("@vin", car.vin);

                        int done = cmd.ExecuteNonQuery();

                        car.Log("update tesla_token OK: " + done);
                    }
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
            }
        }

        internal static void GetChargingstateStdOut(string[] args)
        {
            try
            {
                using (DataTable dt = new DataTable())
                {
                    using (MySqlDataAdapter da = new MySqlDataAdapter("SELECT chargingstate.*, lat, lng, address, charging.charge_energy_added as kWh FROM chargingstate join pos on chargingstate.pos = pos.id join charging on chargingstate.EndChargingID = charging.id where chargingstate.id = @id", DBConnectionstring))
                    {
                        da.SelectCommand.Parameters.AddWithValue("@id", args[1]);
                        da.Fill(dt);

                        if (dt.Rows.Count > 0)
                        {
                            string json = Tools.DataTableToJSONWithJavaScriptSerializer(dt);
                            Console.OutputEncoding = Encoding.UTF8;
                            Console.WriteLine(json);
                        }
                        else
                        {
                            Console.WriteLine("not found!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void AddCommandToDB(string command)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("insert mothershipcommands (command) values (@command)", con))
                {
                    cmd.Parameters.AddWithValue("@command", command);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void GetMothershipCommandsFromDB()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT id, command FROM mothershipcommands", con))
                {
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
        }

        internal void UpdateRefreshToken(string refresh_token)
        {
            try
            {
                car.Log("UpdateRefreshToken");
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("update cars set refresh_token = @refresh_token where id=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", car.CarInDB);
                        cmd.Parameters.AddWithValue("@refresh_token", refresh_token);
                        int done = cmd.ExecuteNonQuery();

                        car.Log("UpdateRefreshToken OK: " + done);
                    }
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
            }
        }

        internal string GetFirmwareFromDate(DateTime dateTime)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT version FROM car_version where StartDate < @date and CarID=@CarID order by StartDate desc limit 1", con))
                {
                    cmd.Parameters.AddWithValue("@date", dateTime);
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);

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
            }

            return "";
        }

        public void CloseChargingState()
        {
            car.Log("CloseChargingState()");
            bool hasFreeSuc = car.HasFreeSuC();
            if (hasFreeSuc)
            {
                // get open SuC charging sessions and apply HasFreeSuC
                try
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
                        using (MySqlCommand cmd = new MySqlCommand(
@"UPDATE 
  chargingstate 
SET 
  cost_total= @cost_total
WHERE 
  CarID = @carid 
  AND EndDate is null 
  AND fast_charger_brand = 'Tesla'", con))
                        {
                            cmd.Parameters.AddWithValue("@carid", car.CarInDB);
                            cmd.Parameters.AddWithValue("@cost_total", 0.0);
                            int rowsUpdated = cmd.ExecuteNonQuery();
                            if (rowsUpdated > 0)
                            {
                                Logfile.Log($"CloseChargingState: car has FreeSuC - update open charging sessions ({rowsUpdated}): cost_total 0.0");
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Tools.DebugLog($"Exception during DBHelper.CloseChargingState(): {ex}");
                    Logfile.ExceptionWriter(ex, "Exception during DBHelper.CloseChargingState()");
                }
            }

            int chargeID = GetMaxChargeid(out DateTime chargeEnd);
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("update chargingstate set EndDate = @EndDate, EndChargingID = @EndChargingID where EndDate is null and CarID=@CarID", con))
                {
                    cmd.Parameters.AddWithValue("@EndDate", chargeEnd);
                    cmd.Parameters.AddWithValue("@EndChargingID", chargeID);
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    cmd.ExecuteNonQuery();
                }
            }

            car.currentJSON.current_charging = false;
            car.currentJSON.current_charger_power = 0;
            car.currentJSON.current_charger_voltage = 0;
            car.currentJSON.current_charger_phases = 0;
            car.currentJSON.current_charger_actual_current = 0;
            car.currentJSON.current_charge_rate_km = 0;

            UpdateMaxChargerPower();

            // As charging point name is depending on the max charger power, it will be updated after "MaxChargerPower" was computed
            car.webhelper.UpdateLastChargingAdress();

            Task.Factory.StartNew(() => CheckForInterruptedCharging(false));
        }

        public void UpdateMaxChargerPower()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("select id, StartChargingID, EndChargingID from chargingstate where CarID=@CarID order by id desc limit 1", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
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
            }
            catch (Exception ex)
            {
                car.Log(ex.Message);
            }
        }

        [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
        internal static bool IndexExists(string index, string table)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM information_schema.statistics where table_name = '" + table + "' and INDEX_NAME ='" + index + "'", con))
                {
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void UpdateMaxChargerPower(int id, int startChargingID, int endChargingID)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("select max(charger_power) from charging where id >= @startChargingID and id <= @endChargingID and CarID=@CarID", con))
                {
                    cmd.Parameters.AddWithValue("@startChargingID", startChargingID);
                    cmd.Parameters.AddWithValue("@endChargingID", endChargingID);
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);

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
        }

        internal void GetEconomy_Wh_km(WebHelper wh)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"SELECT  count(*) as anz, round(charging_End.charge_energy_added / (charging_End.ideal_battery_range_km - charging.ideal_battery_range_km), 3) AS economy_Wh_km
                        FROM charging inner JOIN chargingstate ON charging.id = chargingstate.StartChargingID
                        LEFT OUTER JOIN charging AS charging_End ON chargingstate.EndChargingID = charging_End.id
                        where TIMESTAMPDIFF(MINUTE, chargingstate.StartDate, chargingstate.EndDate) > 100
                        and chargingstate.EndChargingID - chargingstate.StartChargingID > 4
                        and charging_End.battery_level <= 90
                        and chargingstate.CarID = @CarID
                        group by economy_Wh_km
                        order by anz desc
                        limit 1 ", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);

                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            int anz = Convert.ToInt32(dr["anz"]);
                            double wh_km = (double)dr["economy_Wh_km"];

                            car.Log($"Economy from DB: {wh_km} Wh/km - count: {anz}");

                            wh.car.DB_Wh_TR = wh_km;
                            wh.car.DB_Wh_TR_count = anz;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
            }
        }

        internal double GetLatestOdometer()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand($"SELECT EndKm FROM trip where CarID = @carid order by StartDate desc Limit 1", con))
                    {
                        cmd.Parameters.AddWithValue("@carid", car.CarInDB);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            return (double)dr[0];
                        }
                    }
                }
            }
            catch (Exception ex)
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
                    using (MySqlCommand cmd = new MySqlCommand("select id, StartChargingID, EndChargingID, CarId from chargingstate where max_charger_power is null", con))
                    {
                        MySqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            int id = Convert.ToInt32(dr["id"]);
                            int StartChargingID = Convert.ToInt32(dr["StartChargingID"]);
                            int EndChargingID = Convert.ToInt32(dr["EndChargingID"]);
                            int carid = dr["CarId"] as Int32? ?? 1;

                            Car c = Car.GetCarByID(carid);
                            if (c != null)
                                c.dbHelper.UpdateMaxChargerPower(id, StartChargingID, EndChargingID);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.Message);
            }
        }

        internal void GetLastTrip()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand($"SELECT * FROM trip where CarID = @carid order by StartDate desc limit 1", con))
                    {
                        cmd.Parameters.AddWithValue("@carid", car.CarInDB);
                        MySqlDataReader dr = cmd.ExecuteReader();

                        if (dr.Read())
                        {
                            car.currentJSON.current_trip_start = (DateTime)dr["StartDate"];
                            car.currentJSON.current_trip_end = (DateTime)dr["EndDate"];

                            if (dr["StartKm"] != DBNull.Value)
                            {
                                car.currentJSON.current_trip_km_start = Convert.ToDouble(dr["StartKm"]);
                            }

                            if (dr["EndKm"] != DBNull.Value)
                            {
                                car.currentJSON.current_trip_km_end = Convert.ToDouble(dr["EndKm"]);
                            }

                            if (dr["speed_max"] != DBNull.Value)
                            {
                                car.currentJSON.current_trip_max_speed = Convert.ToDouble(dr["speed_max"]);
                            }

                            if (dr["power_max"] != DBNull.Value)
                            {
                                car.currentJSON.current_trip_max_power = Convert.ToDouble(dr["power_max"]);
                            }

                            if (dr["StartRange"] != DBNull.Value)
                            {
                                car.currentJSON.current_trip_start_range = Convert.ToDouble(dr["StartRange"]);
                            }

                            if (dr["EndRange"] != DBNull.Value)
                            {
                                car.currentJSON.current_trip_end_range = Convert.ToDouble(dr["EndRange"]);
                            }
                        }
                        dr.Close();
                    }
                    

                    using (MySqlCommand cmd = new MySqlCommand("SELECT ideal_battery_range_km, battery_range_km, battery_level, lat, lng FROM pos where CarID=@CarID order by id desc limit 1", con))
                    {                        
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            if (dr["ideal_battery_range_km"] != DBNull.Value)
                            {
                                car.currentJSON.current_ideal_battery_range_km = Convert.ToDouble(dr["ideal_battery_range_km"]);
                            }

                            if (dr["battery_range_km"] != DBNull.Value)
                            {
                                car.currentJSON.current_battery_range_km = Convert.ToDouble(dr["battery_range_km"]);
                            }

                            if (dr["battery_level"] != DBNull.Value)
                            {
                                car.currentJSON.current_battery_level = Convert.ToInt32(dr["battery_level"]);
                            }

                            if (dr["lat"] != DBNull.Value)
                            {
                                car.currentJSON.latitude = Convert.ToDouble(dr["lat"]);
                            }

                            if (dr["lng"] != DBNull.Value)
                            {
                                car.currentJSON.longitude = Convert.ToDouble(dr["lng"]);
                            }
                        }
                        dr.Close();
                    }
                    

                    car.currentJSON.CreateCurrentJSON();
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
            }
        }

        public void StartChargingState(WebHelper wh)
        {
            int chargeID = GetMaxChargeid(out DateTime chargeStart);
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("insert chargingstate (CarID, StartDate, Pos, StartChargingID, fast_charger_brand, fast_charger_type, conn_charge_cable , fast_charger_present ) values (@CarID, @StartDate, @Pos, @StartChargingID, @fast_charger_brand, @fast_charger_type, @conn_charge_cable , @fast_charger_present)", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", wh.car.CarInDB);
                    cmd.Parameters.AddWithValue("@StartDate", chargeStart);
                    cmd.Parameters.AddWithValue("@Pos", GetMaxPosid());
                    cmd.Parameters.AddWithValue("@StartChargingID", chargeID);
                    cmd.Parameters.AddWithValue("@fast_charger_brand", wh.fast_charger_brand);
                    cmd.Parameters.AddWithValue("@fast_charger_type", wh.fast_charger_type);
                    cmd.Parameters.AddWithValue("@conn_charge_cable", wh.conn_charge_cable);
                    cmd.Parameters.AddWithValue("@fast_charger_present", wh.fast_charger_present);
                    Tools.DebugLog(cmd);
                    cmd.ExecuteNonQuery();
                }
            }

            wh.car.currentJSON.current_charging = true;
            wh.car.currentJSON.CreateCurrentJSON();

            #pragma warning disable CA2008 // Keine Tasks ohne Übergabe eines TaskSchedulers erstellen
            _ = Task.Factory.StartNew(() =>
            {
                // give TL some time to enter charge state
                Thread.Sleep(30000);
                // try to update chargingstate.pos
                // are we still charging?
                car.Log($"StartChargingState Task start");
                int latestPos = GetMaxPosidLatLng(out double poslat, out double poslng);
                car.Log($"StartChargingState Task latestPos: {latestPos}");
                if (car.GetCurrentState() == Car.TeslaState.Charge)
                {
                    // now get a new entry in pos
                    wh.IsDriving(true);
                    // get lat, lng from max pos id
                    int newPos = GetMaxPosidLatLng(out poslat, out poslng);
                    car.Log($"StartChargingState Task newPos: {newPos}");
                    if (!double.IsNaN(poslat) && !double.IsNaN(poslng))
                    {
                        int chargingstateId = GetMaxChargingstateId(out double chglat, out double chglng);
                        if (!double.IsNaN(chglat) && !double.IsNaN(chglng))
                        {
                            car.Log($"StartChargingState Task (poslng, poslat, chglng, chglat) ({poslng}, {poslat}, {chglng}, {chglat})");
                            double distance = Geofence.GetDistance(poslng, poslat, chglng, chglat);
                            car.Log($"StartChargingState Task distance: {distance}");
                            if (distance > 10)
                            {
                                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                                {
                                    con.Open();
                                    using (MySqlCommand cmd = new MySqlCommand("UPDATE chargingstate SET Pos = @latestPos WHERE chargingstate.id = @chargingstateId", con))
                                    {
                                        cmd.Parameters.AddWithValue("@latestPos", newPos);
                                        cmd.Parameters.AddWithValue("@chargingstateId", chargingstateId);
                                        Tools.DebugLog(cmd);
                                        int updatedRows = cmd.ExecuteNonQuery();
                                        car.Log($"updated chargingstate {chargingstateId} to pos.id {newPos}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            car.Log($"StartChargingState Task chglat: {chglat} chglng: {chglng}");
                        }
                    }
                    else
                    {
                        car.Log($"StartChargingState Task poslat: {poslat} poslng: {poslng}");
                    }
                }
                else
                {
                    car.Log($"StartChargingState Task GetCurrentState(): {car.GetCurrentState()}");
                }
            });
            #pragma warning restore CA2008 // Keine Tasks ohne Übergabe eines TaskSchedulers erstellen
        }

        public void CloseDriveState(DateTime EndDate)
        {
            int StartPos = 0;
            int MaxPosId = GetMaxPosid();

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("select StartPos from drivestate where EndDate is null and CarID=@carid", con))
                {
                    cmd.Parameters.AddWithValue("@carid", car.CarInDB);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        StartPos = Convert.ToInt32(dr[0]);
                    }
                    dr.Close();
                }
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("update drivestate set EndDate = @EndDate, EndPos = @Pos where EndDate is null and CarID=@CarID", con))
                {
                    cmd.Parameters.AddWithValue("@EndDate", EndDate);
                    cmd.Parameters.AddWithValue("@Pos", MaxPosId);
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    cmd.ExecuteNonQuery();
                }
            }

            if (StartPos != 0)
            {
                UpdateDriveStatistics(StartPos, MaxPosId);
            }

            car.currentJSON.current_driving = false;
            car.currentJSON.current_speed = 0;
            car.currentJSON.current_power = 0;

            Task.Factory.StartNew(() =>
              {
                  UpdateTripElevation(StartPos, MaxPosId, " (Task)");
              });
        }

        public static void UpdateTripElevation(int startPos, int maxPosId, string comment = "")
        {
            if (Geofence.GetInstance().RacingMode)
            {
                return;
            }

            if (startPos == 0 || maxPosId == 0)
            {
                return;
            }

            Logfile.Log($"UpdateTripElevation{comment} start:{startPos} ende:{maxPosId}");

            string inhalt = "";
            try
            {
                //SRTM.Logging.LogProvider.SetCurrentLogProvider(SRTM.Logging.Logger.)
                SRTM.SRTMData srtmData = new SRTM.SRTMData(FileManager.GetSRTMDataPath());

                using (DataTable dt = new DataTable())
                {
                    using (MySqlDataAdapter da = new MySqlDataAdapter($"SELECT id, lat, lng FROM pos where id >= @startPos and id <= @maxPosId and altitude is null and lat is not null and lng is not null", DBConnectionstring))
                    {
                        da.SelectCommand.Parameters.AddWithValue("@startPos", startPos);
                        da.SelectCommand.Parameters.AddWithValue("@maxPosId", maxPosId);
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
                                else
                                {
                                    if (Tools.UseOpenTopoData() && long.TryParse($"{dr[0]}", out long posID))
                                    {
                                        OpenTopoDataService.GetSingleton().Enqueue(posID, latitude, longitude);
                                    }
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

                using (WebClient webClient = new WebClient())
                {

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
                    using (MySqlCommand cmd = new MySqlCommand("SELECT max(id) FROM pos where altitude > 0", con))
                    {
                        object o = cmd.ExecuteScalar();

                        if (o != null && o != DBNull.Value)
                        {
                            startid = Convert.ToInt32(o);
                        }
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

                // TODO UpdateTripElevation(startid, GetMaxPosid(this)); // get elevation for all points
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static void UpdateAddress(Car c, int posid)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("select lat, lng from pos where id = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", posid);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            double lat = Convert.ToDouble(dr[0]);
                            double lng = Convert.ToDouble(dr[1]);
                            dr.Close();

                            WebHelper.ReverseGecocodingAsync(c, lat, lng).ContinueWith(task =>
                            {
                                try
                                {
                                    using (MySqlConnection con2 = new MySqlConnection(DBConnectionstring))
                                    {
                                        con2.Open();
                                        using (MySqlCommand cmd2 = new MySqlCommand("update pos set address = @adress where id = @id", con2))
                                        {
                                            cmd2.Parameters.AddWithValue("@id", posid);
                                            cmd2.Parameters.AddWithValue("@adress", task.Result);
                                            cmd2.ExecuteNonQuery();

                                            GeocodeCache.Instance.Write();
                                        }
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
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private void UpdateDriveStatistics(int startPos, int endPos, bool logging = false)
        {
            try
            {
                if (logging)
                {
                    car.Log("UpdateDriveStatistics");
                }

                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT avg(outside_temp) as outside_temp_avg, max(speed) as speed_max, max(power) as power_max, min(power) as power_min, avg(power) as power_avg FROM pos where id between @startpos and @endpos and CarID=@CarID", con))
                    {
                        cmd.Parameters.AddWithValue("@startpos", startPos);
                        cmd.Parameters.AddWithValue("@endpos", endPos);
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);

                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            using (MySqlConnection con2 = new MySqlConnection(DBConnectionstring))
                            {
                                con2.Open();
                                using (MySqlCommand cmd2 = new MySqlCommand("update drivestate set outside_temp_avg=@outside_temp_avg, speed_max=@speed_max, power_max=@power_max, power_min=@power_min, power_avg=@power_avg where StartPos=@StartPos and EndPos=@EndPos  ", con2))
                                {
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
                    }
                }

                // If Startpos doesn't have an "ideal_battery_rage_km", it will be updated from the first valid dataset
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM pos where id = @startpos", con))
                    {
                        cmd.Parameters.AddWithValue("@startpos", startPos);

                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            if (dr["ideal_battery_range_km"] == DBNull.Value)
                            {
                                DateTime dt1 = (DateTime)dr["Datum"];
                                dr.Close();

                                using (var cmd2 = new MySqlCommand("SELECT * FROM pos where id > @startPos and ideal_battery_range_km is not null and battery_level is not null and CarID=@CarID order by id asc limit 1", con))
                                {
                                    cmd2.Parameters.AddWithValue("@startPos", startPos);
                                    cmd2.Parameters.AddWithValue("@CarID", car.CarInDB);
                                    dr = cmd2.ExecuteReader();

                                    if (dr.Read())
                                    {
                                        DateTime dt2 = (DateTime)dr["Datum"];
                                        TimeSpan ts = dt2 - dt1;

                                        object ideal_battery_range_km = dr["ideal_battery_range_km"];
                                        object battery_level = dr["battery_level"];

                                        if (ts.TotalSeconds < 120)
                                        {
                                            dr.Close();

                                            using (var cmd3 = new MySqlCommand("update pos set ideal_battery_range_km = @ideal_battery_range_km, battery_level = @battery_level where id = @startPos", con))
                                            {
                                                cmd3.Parameters.AddWithValue("@startPos", startPos);
                                                cmd3.Parameters.AddWithValue("@ideal_battery_range_km", ideal_battery_range_km.ToString());
                                                cmd3.Parameters.AddWithValue("@battery_level", battery_level.ToString());
                                                cmd3.ExecuteNonQuery();

                                                car.Log($"Trip from {dt1} ideal_battery_range_km updated!");
                                            }
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
                }


                // If Endpos doesn't have an "ideal_battery_rage_km", it will be updated from the last valid dataset
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM pos where id = @endpos", con))
                    {
                        cmd.Parameters.AddWithValue("@endpos", endPos);

                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            if (dr["ideal_battery_range_km"] == DBNull.Value)
                            {
                                DateTime dt1 = (DateTime)dr["Datum"];
                                dr.Close();

                                using (var cmd2 = new MySqlCommand("SELECT * FROM pos where id < @endpos and ideal_battery_range_km is not null and battery_level is not null and CarID=@CarID order by id desc limit 1", con))
                                {
                                    cmd2.Parameters.AddWithValue("@endpos", endPos);
                                    cmd2.Parameters.AddWithValue("@CarID", car.CarInDB);
                                    dr = cmd2.ExecuteReader();

                                    if (dr.Read())
                                    {
                                        DateTime dt2 = (DateTime)dr["Datum"];
                                        TimeSpan ts = dt1 - dt2;

                                        object ideal_battery_range_km = dr["ideal_battery_range_km"];
                                        object battery_level = dr["battery_level"];

                                        if (ts.TotalSeconds < 120)
                                        {
                                            dr.Close();

                                            using (var cmd3 = new MySqlCommand("update pos set ideal_battery_range_km = @ideal_battery_range_km, battery_level = @battery_level where id = @endpos", con))
                                            {
                                                cmd3.Parameters.AddWithValue("@endpos", endPos);
                                                cmd3.Parameters.AddWithValue("@ideal_battery_range_km", ideal_battery_range_km.ToString());
                                                cmd3.Parameters.AddWithValue("@battery_level", battery_level.ToString());
                                                cmd3.ExecuteNonQuery();

                                                car.Log($"Trip from {dt1} ideal_battery_range_km updated!");
                                            }
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
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
            }
        }

        public static bool UpdateIncompleteTrips()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"SELECT pos_start.id as StartPos, pos_end.id as EndPos
                         FROM drivestate
                         JOIN pos pos_start ON drivestate . StartPos = pos_start. id
                         JOIN pos pos_end ON  drivestate . EndPos = pos_end. id
                         WHERE
                         (pos_end. odometer - pos_start. odometer ) > 0.1 and
                         (( pos_start. ideal_battery_range_km is null) or ( pos_end. ideal_battery_range_km is null))", con))
                    {
                        MySqlDataReader dr = cmd.ExecuteReader();

                        while (dr.Read())
                        {
                            try
                            {
                                int StartPos = Convert.ToInt32(dr[0]);
                                int EndPos = Convert.ToInt32(dr[1]);

                                // TODO UpdateDriveStatistics(StartPos, EndPos, false);
                            }
                            catch (Exception ex)
                            {
                                Logfile.Log(ex.ToString());
                            }
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
                using (MySqlCommand cmd = new MySqlCommand("select StartPos,EndPos, carid from drivestate", con))
                {
                    MySqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        try
                        {
                            int StartPos = Convert.ToInt32(dr[0]);
                            int EndPos = Convert.ToInt32(dr[1]);
                            int CarId = Convert.ToInt32(dr[2]);

                            Car c = Car.GetCarByID(CarId);
                            if (c != null)
                            {
                                c.dbHelper.UpdateDriveStatistics(StartPos, EndPos, false);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logfile.Log(ex.ToString());
                        }
                    }
                }
            }

            Logfile.Log("UpdateAllDrivestateData end");
        }

        public void StartDriveState()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("insert drivestate (StartDate, StartPos, CarID) values (@StartDate, @Pos, @CarID)", con))
                {
                    cmd.Parameters.AddWithValue("@StartDate", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Pos", GetMaxPosid());
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    cmd.ExecuteNonQuery();
                }
            }

            car.currentJSON.current_driving = true;
            car.currentJSON.current_charge_energy_added = 0;
            car.currentJSON.current_trip_start = DateTime.Now;
            car.currentJSON.current_trip_end = DateTime.MinValue;
            car.currentJSON.current_trip_km_start = 0;
            car.currentJSON.current_trip_km_end = 0;
            car.currentJSON.current_trip_max_speed = 0;
            car.currentJSON.current_trip_max_power = 0;
            car.currentJSON.current_trip_start_range = 0;
            car.currentJSON.current_trip_end_range = 0;

            car.currentJSON.CreateCurrentJSON();
        }


        public void InsertPos(string timestamp, double latitude, double longitude, int speed, decimal power, double odometer, double ideal_battery_range_km, double battery_range_km, int battery_level, double? outside_temp, string altitude)
        {
            double? inside_temp = car.currentJSON.current_inside_temperature;

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand("insert pos (CarID, Datum, lat, lng, speed, power, odometer, ideal_battery_range_km, battery_range_km, outside_temp, altitude, battery_level, inside_temp, battery_heater, is_preconditioning, sentry_mode) values (@CarID, @Datum, @lat, @lng, @speed, @power, @odometer, @ideal_battery_range_km, @battery_range_km, @outside_temp, @altitude, @battery_level, @inside_temp, @battery_heater, @is_preconditioning, @sentry_mode )", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    cmd.Parameters.AddWithValue("@Datum", UnixToDateTime(long.Parse(timestamp)).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@lat", latitude.ToString());
                    cmd.Parameters.AddWithValue("@lng", longitude.ToString());
                    cmd.Parameters.AddWithValue("@speed", (int)(speed * 1.60934M));
                    cmd.Parameters.AddWithValue("@power", (int)(power * 1.35962M));
                    cmd.Parameters.AddWithValue("@odometer", odometer);

                    if (ideal_battery_range_km == -1)
                    {
                        cmd.Parameters.AddWithValue("@ideal_battery_range_km", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@ideal_battery_range_km", ideal_battery_range_km.ToString());
                    }

                    if (battery_range_km == -1)
                    {
                        cmd.Parameters.AddWithValue("@battery_range_km", DBNull.Value);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("@battery_range_km", battery_range_km.ToString());
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

                    cmd.Parameters.AddWithValue("@battery_heater", car.currentJSON.current_battery_heater ? 1 : 0);
                    cmd.Parameters.AddWithValue("@is_preconditioning", car.currentJSON.current_is_preconditioning ? 1 : 0);
                    cmd.Parameters.AddWithValue("@sentry_mode", car.currentJSON.current_is_sentry_mode ? 1 : 0);

                    cmd.ExecuteNonQuery();

                    try
                    {
                        car.currentJSON.current_speed = (int)(speed * 1.60934M);
                        car.currentJSON.current_power = (int)(power * 1.35962M);

                        if (odometer > 0)
                        {
                            car.currentJSON.current_odometer = odometer;
                        }

                        if (ideal_battery_range_km >= 0)
                        {
                            car.currentJSON.current_ideal_battery_range_km = ideal_battery_range_km;
                        }

                        if (battery_range_km >= 0)
                        {
                            car.currentJSON.current_battery_range_km = battery_range_km;
                        }

                        if (car.currentJSON.current_trip_km_start == 0)
                        {
                            car.currentJSON.current_trip_km_start = odometer;
                            car.currentJSON.current_trip_start_range = car.currentJSON.current_ideal_battery_range_km;
                        }

                        car.currentJSON.current_trip_max_speed = Math.Max(car.currentJSON.current_trip_max_speed, car.currentJSON.current_speed);
                        car.currentJSON.current_trip_max_power = Math.Max(car.currentJSON.current_trip_max_power, car.currentJSON.current_power);

                    }
                    catch (Exception ex)
                    {
                        car.Log(ex.ToString());
                    }
                }
            }

            car.currentJSON.CreateCurrentJSON();
        }

        private DateTime lastChargingInsert = DateTime.Today;


        internal void InsertCharging(string timestamp, string battery_level, string charge_energy_added, string charger_power, double ideal_battery_range, double battery_range, string charger_voltage, string charger_phases, string charger_actual_current, double? outside_temp, bool forceinsert, string charger_pilot_current, string charge_current_request)
        {
            Tools.SetThread_enUS();

            if (charger_phases.Length == 0)
            {
                charger_phases = "1";
            }

            double kmIdeal_Battery_Range = ideal_battery_range / (double)0.62137;
            double kmBattery_Range = battery_range / (double)0.62137;

            double powerkW = Convert.ToDouble(charger_power);

            // default waitbetween2pointsdb
            double waitbetween2pointsdb = 1000.0 / powerkW;
            // if charging started less than 5 minutes ago, insert one charging data point every ~60 seconds
            try
            {
                // get charging_state, must not be older than 5 minutes = 300 seconds = 300000 milliseconds
                if (car.GetTeslaAPIState().GetState("charging_state", out Dictionary<TeslaAPIState.Key, object> charging_state, 300000)) {
                    if (charging_state[TeslaAPIState.Key.Value].ToString().Equals("Charging"))
                    {
                        // check if charging_state value Charging is not older than 5 minutes
                        long now = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
                        if (long.TryParse(charging_state[TeslaAPIState.Key.ValueLastUpdate].ToString(), out long valueLastUpdate))
                        {
                            if (now - valueLastUpdate < 300000)
                            {
                                // charging_state changed to Charging less than 5 minutes ago
                                // set waitbetween2pointsdb to 60 seconds
                                waitbetween2pointsdb = 60;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog("Exception waitbetween2pointsdb", ex);
            }

            double deltaSeconds = (DateTime.Now - lastChargingInsert).TotalSeconds;

            if (forceinsert || deltaSeconds > waitbetween2pointsdb)
            {
                lastChargingInsert = DateTime.Now;

                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("insert charging (CarID, Datum, battery_level, charge_energy_added, charger_power, ideal_battery_range_km, battery_range_km, charger_voltage, charger_phases, charger_actual_current, outside_temp, charger_pilot_current, charge_current_request, battery_heater) values (@CarID, @Datum, @battery_level, @charge_energy_added, @charger_power, @ideal_battery_range_km, @battery_range_km, @charger_voltage, @charger_phases, @charger_actual_current, @outside_temp, @charger_pilot_current, @charge_current_request, @battery_heater)", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.Parameters.AddWithValue("@Datum", UnixToDateTime(long.Parse(timestamp)).ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@battery_level", battery_level);
                        cmd.Parameters.AddWithValue("@charge_energy_added", charge_energy_added);
                        cmd.Parameters.AddWithValue("@charger_power", charger_power);
                        cmd.Parameters.AddWithValue("@ideal_battery_range_km", kmIdeal_Battery_Range.ToString());
                        cmd.Parameters.AddWithValue("@battery_range_km", kmBattery_Range.ToString());
                        cmd.Parameters.AddWithValue("@charger_voltage", int.Parse(charger_voltage));
                        cmd.Parameters.AddWithValue("@charger_phases", charger_phases);
                        cmd.Parameters.AddWithValue("@charger_actual_current", charger_actual_current);
                        cmd.Parameters.AddWithValue("@battery_heater", car.currentJSON.current_battery_heater ? 1 : 0);

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
            }

            try
            {
                if (Convert.ToInt32(battery_level) >= 0)
                {
                    car.currentJSON.current_battery_level = Convert.ToInt32(battery_level);
                }

                car.currentJSON.current_charge_energy_added = Convert.ToDouble(charge_energy_added);
                car.currentJSON.current_charger_power = Convert.ToInt32(charger_power);
                if (kmIdeal_Battery_Range >= 0)
                {
                    car.currentJSON.current_ideal_battery_range_km = kmIdeal_Battery_Range;
                }

                if (kmBattery_Range >= 0)
                {
                    car.currentJSON.current_battery_range_km = kmBattery_Range;
                }

                car.currentJSON.current_charger_voltage = int.Parse(charger_voltage);
                car.currentJSON.current_charger_phases = Convert.ToInt32(charger_phases);
                car.currentJSON.current_charger_actual_current = Convert.ToInt32(charger_actual_current);
                car.currentJSON.CreateCurrentJSON();
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
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
                using (MySqlCommand cmd = new MySqlCommand("Select count(*) from pos", con))
                {
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        return Convert.ToInt32(dr[0]);
                    }
                }
            }

            return 0;
        }

        public int GetMaxPosid(bool withReverseGeocoding = true)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("Select max(id) from pos where CarID=@CarID", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read() && dr[0] != DBNull.Value)
                    {
                        int pos = Convert.ToInt32(dr[0]);
                        if (withReverseGeocoding)
                        {
                            UpdateAddress(car, pos);
                        }

                        return pos;
                    }
                }
            }

            return 0;
        }

        public int GetMaxPosidLatLng(out double lat, out double lng)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("select id,lat,lng from pos where id in (Select max(id) from pos where CarID=@CarID)", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read() && dr[0] != DBNull.Value)
                    {
                        double.TryParse(dr[1].ToString(), out lat);
                        double.TryParse(dr[2].ToString(), out lng);
                        return Convert.ToInt32(dr[0]);
                    }
                }
            }
            lat = double.NaN;
            lng = double.NaN;
            return 0;
        }

        private int GetMaxChargeid(out DateTime chargeStart)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT id, datum FROM charging WHERE CarID=@CarID ORDER BY datum DESC LIMIT 1", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read() && dr[0] != DBNull.Value && dr[1] != DBNull.Value)
                    {
                        if (!DateTime.TryParse(dr[1].ToString(), out chargeStart))
                        {
                            chargeStart = DateTime.Now;
                        }
                        return Convert.ToInt32(dr[0]);
                    }
                }
            }
            chargeStart = DateTime.Now;
            return 0;
        }

        private int GetMaxChargingstateId(out double lat, out double lng)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("select chargingstate.id, lat, lng from chargingstate join pos on chargingstate.pos = pos.id where chargingstate.id in (select max(id) from chargingstate where carid=@CarID)", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    Tools.DebugLog(cmd);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read() && dr[0] != DBNull.Value && dr[1] != DBNull.Value && dr[2] != DBNull.Value)
                    {
                        double.TryParse(dr[1].ToString(), out lat);
                        double.TryParse(dr[2].ToString(), out lng);
                        return Convert.ToInt32(dr[0]);
                    }
                }
            }
            lat = double.NaN;
            lng = double.NaN;
            return 0;
        }

        internal void SetCarVersion(string car_version)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("insert car_version (StartDate, version, CarID) values (@StartDate, @version, @CarID)", con))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@version", car_version);
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, car_version);
            }
        }

        internal string GetLastCarVersion()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand($"select version from car_version where CarId=@CarID order by id desc limit 1", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            return dr[0].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "GetLastCarVersion");
                car.Log(ex.ToString());
            }

            return "";
        }



        public static string GetVersion()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT @@version", con))
                {
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        return dr[0].ToString();
                    }
                }
            }

            return "NULL";
        }

        [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static bool TableExists(string table)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM information_schema.tables where table_name = '" + table + "'", con))
                {
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static string GetColumnType(string table, string column)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand($"SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE table_name = '{table}' AND COLUMN_NAME = '{column}'", con))
                {
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        return dr[0].ToString();
                    }
                }
            }

            return string.Empty;
        }

        [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static bool ColumnExists(string table, string column)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("SHOW COLUMNS FROM `" + table + "` LIKE '" + column + "';", con))
                {
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        return true;
                    }
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
                    using (MySqlCommand cmd = new MySqlCommand(sql, con))
                    {
                        if (timeout != 30)
                        {
                            cmd.CommandTimeout = timeout;
                        }

                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("Error in: " + sql);
                Logfile.ExceptionWriter(ex, sql);
                throw;
            }
        }

        public void CheckForInterruptedCharging(bool logging)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"SELECT chargingstate.id as chargingstate_id , StartDate, EndDate, charging.charge_energy_added as start_charge_energy_added,
                          charging_End.charge_energy_added,
                          charging.ideal_battery_range_km AS SOC,
                          charging_End.ideal_battery_range_km AS EndSOC,
                          charging_End.battery_level as End_battery_level,
                          pos.odometer
                            FROM charging inner JOIN chargingstate ON charging.id = chargingstate.StartChargingID INNER JOIN
                             pos ON chargingstate.pos = pos.id
                             LEFT OUTER JOIN
                             charging AS charging_End ON chargingstate.EndChargingID = charging_End.id
                        where TIMESTAMPDIFF(MINUTE, chargingstate.StartDate, chargingstate.EndDate) > 3 and chargingstate.EndChargingID - chargingstate.StartChargingID > 4 and chargingstate.CarId = @CarID
                        and charging.charge_energy_added > 1
                        order by StartDate desc", con))
                    {

                        double old_odometer = 0;
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);

                        MySqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            double odometer = (double)dr["odometer"];
                            int chargingstate_id = (int)dr["chargingstate_id"];

                            if (old_odometer != odometer)
                            {
                                double lastCharging_start_charge_energy_added = Convert.ToDouble(dr["start_charge_energy_added"]);

                                CombineChargingifNecessary(chargingstate_id, odometer, logging, lastCharging_start_charge_energy_added);
                                old_odometer = odometer;
                            }
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

        private void CombineChargingifNecessary(int chargingstate_id, double odometer, bool logging, double lastCharging_start_charge_energy_added)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"SELECT        chargingstate.id as chargingstate_id , StartDate, EndDate, charging.charge_energy_added as start_charge_energy_added,
                      charging_End.charge_energy_added,
                      charging.ideal_battery_range_km AS SOC,
                      charging_End.ideal_battery_range_km AS EndSOC,
                      charging_End.battery_level as End_battery_level,
                      pos.odometer, chargingstate.StartChargingID, chargingstate.EndChargingID
                        FROM charging inner JOIN chargingstate ON charging.id = chargingstate.StartChargingID INNER JOIN
                         pos ON chargingstate.pos = pos.id
                         LEFT OUTER JOIN
                         charging AS charging_End ON chargingstate.EndChargingID = charging_End.id
                    where TIMESTAMPDIFF(MINUTE, chargingstate.StartDate, chargingstate.EndDate) > 3 and chargingstate.EndChargingID - chargingstate.StartChargingID > 4 and chargingstate.CarId = @CarID
                    and odometer = @odometer and chargingstate.id < @chargingstate_id
                    order by StartDate desc", con))
                {

                    cmd.Parameters.AddWithValue("@odometer", odometer);
                    cmd.Parameters.AddWithValue("@chargingstate_id", chargingstate_id);
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);

                    int newId = 0;
                    DateTime newStartdate = DateTime.MinValue;
                    int newStartChargingID = 0;

                    MySqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        newId = (int)dr["chargingstate_id"];
                        newStartdate = (DateTime)dr["StartDate"];
                        newStartChargingID = (int)dr["StartChargingID"];

                        double start_charge_energy_added = Convert.ToDouble(dr["start_charge_energy_added"]);
                        double charge_energy_added = Convert.ToDouble(dr["charge_energy_added"]);

                        if (charge_energy_added <= lastCharging_start_charge_energy_added)
                        {
                            DeleteChargingstate(newId);
                            UpdateChargingstate(chargingstate_id, newStartdate, newStartChargingID, charge_energy_added, lastCharging_start_charge_energy_added);
                        }
                    }
                }
            }
        }

        private void UpdateChargingstate(int chargingstate_id, DateTime StartDate, int StartChargingID, double charge_energy_added, double lastCharging_start_charge_energy_added)
        {
            try
            {
                car.Log($"Update Chargingstate {chargingstate_id} with new StartDate: {StartDate} /  StartChargingID: {StartChargingID} / charge_energy_added: {charge_energy_added} / lastCharging_start_charge_energy_added: {lastCharging_start_charge_energy_added}");

                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"update chargingstate set StartDate=@StartDate, StartChargingID=@StartChargingID where id = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", chargingstate_id);
                        cmd.Parameters.AddWithValue("@StartDate", StartDate);
                        cmd.Parameters.AddWithValue("@StartChargingID", StartChargingID);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, chargingstate_id.ToString());
                car.Log(ex.ToString());
            }
        }

        private void DeleteChargingstate(int chargingstate_id)
        {
            try
            {
                car.Log("Delete Chargingstate " + chargingstate_id.ToString());

                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"delete from chargingstate where id = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", chargingstate_id);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, chargingstate_id.ToString());
                car.Log(ex.ToString());
            }
        }

        internal int GetScanMyTeslaSignalsLastWeek()
        {
            string cacheKey = "GetScanMyTeslaSignalsLastWeek_" + car.CarInDB;
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
                    using (MySqlCommand cmd = new MySqlCommand("SELECT count(*) FROM teslalogger.can where CarID=@CarID and datum >= DATE(NOW()) - INTERVAL 7 DAY", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);

                        MySqlDataReader r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            int count = Convert.ToInt32(r[0]);

                            MemoryCache.Default.Add(cacheKey, count, DateTime.Now.AddHours(4));
                            return count;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "GetScanMyTeslaPacketsLastWeek");
                car.Log(ex.ToString());
            }
            return 0;
        }

        internal int GetScanMyTeslaPacketsLastWeek()
        {
            string cacheKey = "GetScanMyTeslaPacketsLastWeek_" + car.CarInDB;
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
                    using (MySqlCommand cmd = new MySqlCommand("select count(*) from (SELECT count(*) as cnt FROM can where CarID=@CarID and datum >= DATE(NOW()) - INTERVAL 7 DAY group by UNIX_TIMESTAMP(datum)) as T1", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        MySqlDataReader r = cmd.ExecuteReader();
                        if (r.Read())
                        {
                            int count = Convert.ToInt32(r[0]);

                            MemoryCache.Default.Add(cacheKey, count, DateTime.Now.AddHours(4));
                            return count;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "GetScanMyTeslaPacketsLastWeek");
                car.Log(ex.ToString());
            }
            return 0;
        }

        public int GetAvgMaxRage()
        {
            string cacheKey = "GetAvgMaxRage_" + car.CarInDB;
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
                    using (MySqlCommand cmd = new MySqlCommand(@"SELECT AVG(charging_End.ideal_battery_range_km / charging_End.battery_level * 100) AS 'TRmax'
                        FROM charging
                        INNER JOIN chargingstate ON charging.id = chargingstate.StartChargingID
                        INNER JOIN pos ON chargingstate.pos = pos.id
                        LEFT OUTER JOIN charging AS charging_End ON chargingstate.EndChargingID = charging_End.id
                        WHERE chargingstate.CarID=@CarID and chargingstate.StartDate > SUBDATE(Now(), INTERVAL 60 DAY) AND TIMESTAMPDIFF(MINUTE, chargingstate.StartDate, chargingstate.EndDate) > 3 and pos.odometer > 1
                    ", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);

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
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "GetAvgMaxRage");
                car.Log(ex.ToString());
            }
            return 0;
        }

        public string UpdateCountryCode()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand($"select lat, lng from pos where id = (select max(id) from pos where CarID=@CarID)", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            double lat = Convert.ToDouble(dr[0]);
                            double lng = Convert.ToDouble(dr[1]);
                            dr.Close();

                            WebHelper.ReverseGecocodingAsync(car, lat, lng, true, false).Wait();
                            return car.currentJSON.current_country_code;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
            }

            return "";
        }

        public static DataTable GetCars()
        {
            DataTable dt = new DataTable();

            try
            {
                using (MySqlDataAdapter da = new MySqlDataAdapter("SELECT * from cars order by id", DBConnectionstring))
                {
                    da.Fill(dt);
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            return dt;
        }

        public void GetAvgConsumption(out double sumkm, out double avgkm, out double kwh100km, out double avgsocdiff, out double maxkm)
        {
            sumkm = 0;
            avgkm = 0;
            kwh100km = 0;
            avgsocdiff = 0;
            maxkm = 0;
            
            try
            {
                DataTable dt = new DataTable();
                string sql = @"SELECT sum(km_diff) as sumkm, avg(km_diff) as avgkm, avg(avg_consumption_kwh_100km) as kwh100km , avg(pos.battery_level-posend.battery_level) as avgsocdiff, avg(km_diff / (pos.battery_level-posend.battery_level) * 100) as maxkm 
                    FROM trip 
                    join pos on trip.startposid = pos.id 
                    join pos as posend on trip.endposid = posend.id
                    where km_diff between 100 and 800 and pos.battery_level is not null and trip.carid=" + car.CarInDB;

                using (MySqlDataAdapter da = new MySqlDataAdapter(sql, DBConnectionstring))
                {
                    da.Fill(dt);

                    if (dt.Rows.Count == 1)
                    {
                        var r = dt.Rows[0];

                        if (r["sumkm"] == DBNull.Value)
                        {
                            car.Log($"GetAvgConsumption: nothing found!!!");
                            return;
                        }

                        sumkm = Math.Round((double)r["sumkm"],1);
                        avgkm = Math.Round((double)r["avgkm"], 1);
                        kwh100km = Math.Round((double)r["kwh100km"], 1);
                        avgsocdiff = Math.Round((double)r["avgsocdiff"], 1);
                        maxkm = Math.Round((double)r["maxkm"], 1);

                        car.Log($"GetAvgConsumption: sumkm:{sumkm} avgkm:{avgkm} kwh/100km:{kwh100km} avgsocdiff:{avgsocdiff} maxkm:{maxkm}");
                    }
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
            }
        }

        public static object DBNullIfEmptyOrZero(string val)
        {
            if (val == null || val.Length == 0 || val == "0" || val == "0.00")
            {
                return DBNull.Value;
            }

            return val;
        }

        public static object DBNullIfEmpty(string val)
        {
            if (val == null || val.Length == 0)
            {
                return DBNull.Value;
            }

            return val;
        }

        public static bool IsZero(string val)
        {
            if (val == null || val.Length == 0)
            {
                return false;
            }

            if (double.TryParse(val, out double v))
            {
                if (v == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static void Enable_utf8mb4()
        {
            // https://mathiasbynens.be/notes/mysql-utf8mb4
            // check database
            Enable_utf8mb4_check_database("teslalogger");
            // check tables
            Enable_utf8mb4_check_tables("teslalogger");
        }

        [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private static void Enable_utf8mb4_check_database(string dbname)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand($"SELECT default_character_set_name, default_collation_name FROM information_schema.schemata WHERE schema_name = '{dbname}'", con))
                    {
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            if (dr.HasRows && dr[0] != null && dr[1] != null)
                            {
                                if (!dr[0].ToString().Equals("utf8mb4") || !dr[1].ToString().Equals("utf8mb4_unicode_ci"))
                                {
                                    Enable_utf8mb4_alter_database(dbname);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "");
            }
        }

        private static void Enable_utf8mb4_alter_database(string dbname)
        {
            // ALTER DATABASE database_name CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci;
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    Logfile.Log($"ALTER DATABASE {dbname} CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci");
                    _ = ExecuteSQLQuery($"ALTER DATABASE {dbname} CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci", 300);
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "");
            }
        }

        [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private static void Enable_utf8mb4_check_tables(string dbname)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand($"SELECT TABLE_NAME, TABLE_COLLATION FROM information_schema.TABLES WHERE TABLE_SCHEMA = '{dbname}' AND TABLE_TYPE = 'BASE TABLE'", con))
                    {
                        MySqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            if (dr.HasRows && dr[0] != null && dr[1] != null)
                            {
                                if (!dr[1].ToString().Equals("utf8mb4_unicode_ci"))
                                {
                                    Enable_utf8mb4_alter_table(dbname, dr[0].ToString());
                                }
                                // check columns in table
                                Enable_utf8mb4_check_columns(dbname, dr[0].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "");
            }
        }

        private static void Enable_utf8mb4_alter_table(string dbname, string tablename)
        {
            // ALTER TABLE table_name CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    Logfile.Log($"ALTER TABLE {dbname}.{tablename} CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");
                    _ = ExecuteSQLQuery($"ALTER TABLE {dbname}.{tablename} CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci", 3000);
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "");
            }
        }

        [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private static void Enable_utf8mb4_check_columns(string dbname, string tablename)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand($"SELECT COLUMN_NAME, CHARACTER_SET_NAME, COLLATION_NAME, COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS where TABLE_SCHEMA = '{dbname}' AND TABLE_NAME = '{tablename}' AND DATA_TYPE = 'varchar'", con))
                    {
                        MySqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            if (dr.HasRows && dr[0] != null && dr[1] != null && dr[2] != null)
                            {
                                if (!dr[1].ToString().Equals("utf8mb4") || !dr[2].ToString().Equals("utf8mb4_unicode_ci"))
                                {
                                    Enable_utf8mb4_alter_column(dbname, tablename, dr[0].ToString(), dr[3].ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "");
            }
        }

        private static void Enable_utf8mb4_alter_column(string dbname, string tablename, string columnname, string columntype)
        {
            // ALTER TABLE `shiftstate` CHANGE `state` `state` VARCHAR(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL;
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    Logfile.Log($"ALTER TABLE {dbname}.{tablename} CHANGE {columnname} {columnname} {columntype} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL");
                    _ = ExecuteSQLQuery($"ALTER TABLE {dbname}.{tablename} CHANGE {columnname} {columnname} {columntype} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL", 3000);
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "");
            }
        }
    }
}
