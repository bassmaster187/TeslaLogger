using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
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
                    {
                        Logfile.Log($"update {table} set carid = 1 where carid is null; ms: {t} / Rows: {rows}");
                    }

                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }
            }
        }

        internal string GetRefreshToken(out string tesla_token)
        {
            tesla_token = "";

            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT refresh_token, tesla_token FROM cars where id = @CarID", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);

                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            string refresh_token = dr[0].ToString();
                            tesla_token = dr[1].ToString();
                            return refresh_token;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            return "";
}
      
        internal void UpdateEmptyChargeEnergy()
        {
            Queue<int> emptyChargeEnergy = new Queue<int>();
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  id
FROM
  chargingstate
WHERE
  CarID=@CarID
  AND charge_energy_added IS NULL
  AND EndDate IS NOT NULL", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (int.TryParse(dr[0].ToString(), out int id))
                            {
                                emptyChargeEnergy.Enqueue(id);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog($"Exception during UpdateEmptyChargeEnergy(): {ex}");
                Logfile.ExceptionWriter(ex, "Exception during UpdateEmptyChargeEnergy()");
            }
            foreach (int ID in emptyChargeEnergy)
            {
                UpdateChargeEnergyAdded(ID);
            }
        }

        internal bool CombineChangingStatesAt(int sessionid)
        {
            bool doCombine = Tools.CombineChargingStates(); // use default
            // check if combine is disabled globally
            if (!doCombine)
            {
                Tools.DebugLog("CombineChargingStates disabled globally");
                Address addr = GetAddressFromChargingState(sessionid);
                // combine disabled, but check pos for special flag do combine
                if (addr != null && addr.specialFlags != null && addr.specialFlags.Count > 0 && addr.specialFlags.ContainsKey(Address.SpecialFlags.CombineChargingStates))
                {
                    Tools.DebugLog($"CombineChargingStates disabled globally, but enabled at POI '{addr.name}'");
                    doCombine = true;
                }
            }
            else
            {
                Address addr = GetAddressFromChargingState(sessionid);
                // check pos for special flag do not combine
                if (addr != null && addr.specialFlags != null && addr.specialFlags.Count > 0)
                {
                    // check if DoNotCombineChargingStates is enabled
                    if (addr.specialFlags.ContainsKey(Address.SpecialFlags.DoNotCombineChargingStates))
                    {
                        Tools.DebugLog($"CombineChargingStates enabled globally, but disabled at POI '{addr.name}'");
                        doCombine = false;
                    }
                }
            }
            return doCombine;
        }

        internal void CombineChangingStates()
        {
            // find candidates to combine
            // find chargingstates with exactly the same odometer -> car did no move between charging states
            Queue<int> combineCandidates = FindCombineCandidates();
            foreach (int candidate in combineCandidates)
            {
                Tools.DebugLog($"FindCombineCandidates: {candidate}");

                // check if combine is disabled globally or locally
                if (!CombineChangingStatesAt(candidate))
                {
                    continue;
                }

                Queue<int> similarChargingStates = FindSimilarChargingStates(candidate);
                foreach (int similarChargingState in similarChargingStates)
                {
                    Tools.DebugLog($"FindSimilarChargingStates: {similarChargingState}");
                }
                if (similarChargingStates.Count > 0)
                {
                    // find max ID in similarChargingStates and candidate
                    int maxID = candidate;
                    foreach (int similarChargingState in similarChargingStates)
                    {
                        maxID = Math.Max(maxID, similarChargingState);
                    }
                    // find min ID in similarChargingStates and candidate
                    int minID = candidate;
                    foreach (int similarChargingState in similarChargingStates)
                    {
                        minID = Math.Min(minID, similarChargingState);
                    }
                    // build deletion list: all IDs from minID to maxID including minID excluding maxID
                    List<int> IDsToDelete = new List<int>();
                    if (candidate != maxID)
                    {
                        IDsToDelete.Add(candidate);
                    }
                    foreach (int similarChargingState in similarChargingStates)
                    {
                        if (similarChargingState != maxID)
                        {
                            IDsToDelete.Add(similarChargingState);
                        }
                    }
                    GetStartValuesFromChargingState(minID, out DateTime startDate, out int startdID, out int posID);
                    List<int> toBeAnalyzed = new List<int>();
                    toBeAnalyzed.AddRange(IDsToDelete);
                    toBeAnalyzed.Add(maxID);
                    AnalyzeCombineCandidates(toBeAnalyzed);
                    car.Log($"Combine charging state{(similarChargingStates.Count > 1 ? "s" : "")} {string.Join(", ", IDsToDelete)} into {maxID}");
                    Tools.DebugLog($"GetStartValuesFromChargingState: id:{minID} startDate:{startDate} startID:{startdID} posID:{posID}");
                    // update current charging state with startdate, startID, pos
                    Tools.DebugLog($"UpdateChargingState: id:{maxID} to startDate:{startDate} startID:{startdID} posID:{posID}");
                    UpdateChargingstate(maxID, startDate, startdID, 0.0, 0.0);
                    // delete all older charging states
                    foreach (int chargingState in IDsToDelete)
                    {
                        Tools.DebugLog($"delete combined chargingState id:{chargingState}");
                        DeleteChargingstate(chargingState);
                    }

                    // get charging cost calculation data
                    string ref_cost_currency = string.Empty;
                    double ref_cost_per_kwh = double.NaN;
                    bool ref_cost_per_kwh_found = false;
                    double ref_cost_per_minute = double.NaN;
                    bool ref_cost_per_minute_found = false;
                    double ref_cost_per_session = double.NaN;
                    bool ref_cost_per_session_found = false;
                    GetChargeCostData(maxID, ref ref_cost_currency, ref ref_cost_per_kwh, ref ref_cost_per_kwh_found, ref ref_cost_per_minute, ref ref_cost_per_minute_found, ref ref_cost_per_session, ref ref_cost_per_session_found);

                    // calculate chargingstate.charge_energy_added from endchargingid - startchargingid
                    UpdateChargeEnergyAdded(maxID);

                    // calculate charging price if per_kwh and/or per_minute and/or per_session is available
                    UpdateChargePrice(maxID, ref_cost_currency, ref_cost_per_kwh, ref_cost_per_kwh_found, ref_cost_per_minute, ref_cost_per_minute_found, ref_cost_per_session, ref_cost_per_session_found);

                    // update chargingsession stats
                    UpdateMaxChargerPower(maxID);
                }
            }
        }

        private void AnalyzeCombineCandidates(List<int> combineCandidates)
        {
            // analyze time passed between n.end and n+1.start
            if (combineCandidates.Count > 1)
            {
                Queue<Tuple<int, DateTime, DateTime>> tuples = new Queue<Tuple<int, DateTime, DateTime>>();
                foreach (int candidate in combineCandidates)
                {
                    tuples.Enqueue(GetStartEndFromCharginState(candidate));
                }
                if (tuples.Count > 1)
                {
                    for (int i = 1; i < tuples.Count; i++)
                    {
                        if (tuples.ElementAt(i - 1).Item1 != -1 && tuples.ElementAt(i).Item1 != -1)
                        {
                            Tools.DebugLog($"time between id {tuples.ElementAt(i - 1).Item1} and id {tuples.ElementAt(i).Item1}: {(tuples.ElementAt(i - 1).Item3 - tuples.ElementAt(i).Item2).TotalSeconds} seconds");
                        }
                    }
                }
            }
        }

        private Tuple<int, DateTime, DateTime> GetStartEndFromCharginState(int id)
        {
            Tuple<int, DateTime, DateTime> tuple = Tuple.Create(-1, DateTime.MinValue, DateTime.MinValue);
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  chargingstate.id,
  chargingstate.StartDate,
  chargingstate.EndDate
FROM
  chargingstate
WHERE
  chargingstate.CarId = @CarID
  AND chargingstate.id = @ChargingStateID", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.Parameters.AddWithValue("@ChargingStateID", id);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            if (DateTime.TryParse(dr[1].ToString(), out DateTime StartDate)
                                && DateTime.TryParse(dr[2].ToString(), out DateTime EndDate))
                            {
                                tuple = Tuple.Create(id, StartDate, EndDate);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "GetStartEndFromCharginState");
                car.Log(ex.ToString());
            }

            return tuple;
        }

        private Queue<int> FindCombineCandidates()
        {
                Queue<int> combineCandidates = new Queue<int>();
                try
                {
                    using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                    {
                        con.Open();
                        using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  chargingstate.id
FROM
  chargingstate,
  pos
WHERE
  chargingstate.pos = pos.id
  AND pos.CarID=@CarID
GROUP BY
  pos.odometer
HAVING
  COUNT(chargingstate.id) > 1", con))
                        {
                            cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                            Tools.DebugLog(cmd);
                            MySqlDataReader dr = cmd.ExecuteReader();
                            while (dr.Read() && dr[0] != DBNull.Value)
                            {
                                if (int.TryParse(dr[0].ToString(), out int id))
                                {
                                    combineCandidates.Enqueue(id);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Tools.DebugLog($"Exception during FindCombineCandidates(): {ex}");
                    Logfile.ExceptionWriter(ex, "Exception during FindCombineCandidates()");
                }
                return combineCandidates;
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

        internal void CloseChargingStates()
        {
            DateTime dtstart = DateTime.UtcNow;
            // find open charging states (EndDate == NULL) order by oldest first
            Queue<int> openchargingstates = FindOpenChargingStates();

            // foreach open charging state (identified by id)
            foreach (int openChargingState in openchargingstates)
            {
                // close charging state with enddate, endID from max charging
                CloseChargingState(openChargingState);

                // if charging was interrupted, maybe combine it with the previous session
                if (CombineChangingStatesAt(openChargingState))
                {
                    // get pos.odometer from openChargingState
                    double odometer = GetOdometerFromChargingstate(openChargingState);
                    if (!double.IsNaN(odometer))
                    {
                        Tools.DebugLog($"openChargingState id:{openChargingState} odometer:{odometer}");
                        // find charging state(s) with identical pos.odometer
                        Queue<int> chargingStates = FindSimilarChargingStates(openChargingState);
                        foreach (int chargingState in chargingStates)
                        {
                            Tools.DebugLog($"FindSimilarChargingStates: {chargingState}:{odometer}");
                        }
                        // get startdate, startID, posID from oldest
                        if (chargingStates.Count > 0 && GetStartValuesFromChargingState(chargingStates.First(), out DateTime startDate, out int startdID, out int posID))
                        {
                            car.Log($"Combine charging states {string.Join(", ", chargingStates)} into {openChargingState}");
                            Tools.DebugLog($"GetStartValuesFromChargingState: id:{chargingStates.First()} startDate:{startDate} startID:{startdID} posID:{posID}");
                            // update current charging state with startdate, startID, pos
                            Tools.DebugLog($"UpdateChargingState: id:{openChargingState} to startDate:{startDate} startID:{startdID} posID:{posID}");
                            UpdateChargingstate(openChargingState, startDate, startdID, 0.0, 0.0);
                            // delete all older charging states
                            foreach (int chargingState in chargingStates)
                            {
                                Tools.DebugLog($"delete combined chargingState id:{chargingState}");
                                DeleteChargingstate(chargingState);
                            }
                        }
                    }
                }
                // get charging cost calculation data
                string ref_cost_currency = string.Empty;
                double ref_cost_per_kwh = double.NaN;
                bool ref_cost_per_kwh_found = false;
                double ref_cost_per_minute = double.NaN;
                bool ref_cost_per_minute_found = false;
                double ref_cost_per_session = double.NaN;
                bool ref_cost_per_session_found = false;
                GetChargeCostData(openChargingState, ref ref_cost_currency, ref ref_cost_per_kwh, ref ref_cost_per_kwh_found, ref ref_cost_per_minute, ref ref_cost_per_minute_found, ref ref_cost_per_session, ref ref_cost_per_session_found);

                // calculate chargingstate.charge_energy_added from endchargingid - startchargingid
                UpdateChargeEnergyAdded(openChargingState);

                // calculate charging price if per_kwh and/or per_minute and/or per_session is available
                UpdateChargePrice(openChargingState, ref_cost_currency, ref_cost_per_kwh, ref_cost_per_kwh_found, ref_cost_per_minute, ref_cost_per_minute_found, ref_cost_per_session, ref_cost_per_session_found);
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

            DateTime dtend = DateTime.UtcNow;
            TimeSpan ts = dtend - dtstart;
            Tools.DebugLog($"CloseChargingStates took {ts.TotalMilliseconds}ms");
            if (ts.TotalMilliseconds > 1000)
            {
                car.Log($"CloseChargingStates took {ts.TotalMilliseconds}ms");
            }
        }

        private void GetChargeCostData(int ChargingStateID, ref string ref_cost_currency, ref double ref_cost_per_kwh, ref bool ref_cost_per_kwh_found, ref double ref_cost_per_minute, ref bool ref_cost_per_minute_found, ref double ref_cost_per_session, ref bool ref_cost_per_session_found)
        {
            if (car.HasFreeSuC() && ChargingStateLocationIsSuC(ChargingStateID))
            {
                ref_cost_per_kwh = 0.0;
                ref_cost_per_kwh_found = true;
                ref_cost_per_minute = 0.0;
                ref_cost_per_minute_found = true;
                ref_cost_per_session = 0.0;
                ref_cost_per_session_found = true;
            }
            else
            {
                // get addr for chargingstate.pos
                Address addr = GetAddressFromChargingState(ChargingStateID);
                if (addr != null && addr.specialFlags != null && addr.specialFlags.Count > 0)
                {
                    // check if +ccp is enabled
                    if (addr.specialFlags.ContainsKey(Address.SpecialFlags.CopyChargePrice))
                    {
                        car.Log($"CopyChargePrice enabled for '{addr.name}'");
                        // find reference charge session for addr
                        int refChargingState = FindReferenceChargingState(addr.name, out ref_cost_currency, out ref_cost_per_kwh, out ref_cost_per_kwh_found, out ref_cost_per_session, out ref_cost_per_session_found, out ref_cost_per_minute, out ref_cost_per_minute_found);
                        // if exists, copy curreny, per_kwh, per_minute, per_session to current charging state
                        if (refChargingState != int.MinValue)
                        {
                            car.Log($"CopyChargePrice: reference charging session found for '{addr.name}', ID {refChargingState} - cost_per_kwh:{ref_cost_per_kwh} cost_per_session:{ref_cost_per_session} cost_per_minute:{ref_cost_per_minute}");
                        }
                    }
                }
            }
        }

        private bool ChargingStateLocationIsSuC(int ChargingStateID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  fast_charger_brand,
  fast_charger_type
FROM
  chargingstate
WHERE
  CarID = @CarID
  AND id = @referenceID", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.Parameters.AddWithValue("@referenceID", ChargingStateID);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read() && dr[0] != DBNull.Value && dr[1] != DBNull.Value)
                        {
                            if (dr[0].ToString().Equals("Tesla") && (dr[1].ToString().Equals("Tesla") || dr[1].ToString().Equals("Combo")))
                            {
                                Tools.DebugLog("ChargingStateLocationIsSuC: true");
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog($"Exception during DBHelper.ChargingStateLocationIsSuC(): {ex}");
                Logfile.ExceptionWriter(ex, "Exception during DBHelper.ChargingStateLocationIsSuC()");
            }
            Tools.DebugLog("ChargingStateLocationIsSuC: false");
            return false;
        }

        private void UpdateChargePrice(int ChargingStateID, string ref_cost_currency, double ref_cost_per_kwh, bool ref_cost_per_kwh_found, double ref_cost_per_minute, bool ref_cost_per_minute_found, double ref_cost_per_session, bool ref_cost_per_session_found)
        {
            if (ref_cost_per_kwh_found || ref_cost_per_minute_found || ref_cost_per_session_found)
            {
                double cost_total = double.NaN;
                double charge_energy_added = double.NaN;
                DateTime startDate = DateTime.MinValue;
                DateTime endDate = DateTime.MinValue;

                // read values from openChargingState
                try
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
                        using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  charge_energy_added,
  startdate,
  enddate
FROM
  chargingstate
WHERE
  CarID = @CarID
  AND id = @referenceID", con))
                        {
                            cmd.Parameters.Add("@CarID", MySqlDbType.UByte).Value = car.CarInDB;
                            cmd.Parameters.Add("@referenceID", MySqlDbType.Int32).Value = ChargingStateID;
                            Tools.DebugLog(cmd);
                            MySqlDataReader dr = cmd.ExecuteReader();
                            if (dr.Read() && dr[0] != DBNull.Value) {
                                if (double.TryParse(dr[0].ToString(), out charge_energy_added)
                                    && DateTime.TryParse(dr[1].ToString(), out startDate)
                                    && DateTime.TryParse(dr[2].ToString(), out endDate))
                                {
                                    cost_total = 0.0;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Tools.DebugLog($"Exception during DBHelper.UpdateChargePrice(): {ex}");
                    Logfile.ExceptionWriter(ex, "Exception during DBHelper.UpdateChargePrice()");
                }

                // calculate and update cost_per_kwh
                if (ref_cost_per_kwh_found)
                {
                    car.Log($"UpdateChargePrice id:{ChargingStateID} cost_per_kwh:{charge_energy_added}kWh * {ref_cost_per_kwh}{ref_cost_currency} = {ref_cost_per_kwh * charge_energy_added}");
                    if (!double.IsNaN(cost_total))
                    {
                        cost_total += ref_cost_per_kwh * charge_energy_added;
                    }
                    else
                    {
                        cost_total = ref_cost_per_kwh * charge_energy_added;
                    }
                    try
                    {
                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();
                            using (MySqlCommand cmd = new MySqlCommand(
        @"UPDATE 
  chargingstate 
SET 
  cost_per_kwh=@cost_per_kwh
WHERE 
  CarID = @CarID
  AND id=@ChargingStateID", con))
                            {
                                cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                                cmd.Parameters.AddWithValue("@ChargingStateID", ChargingStateID);
                                cmd.Parameters.AddWithValue("@cost_per_kwh", ref_cost_per_kwh);
                                Tools.DebugLog(cmd);
                                int rowsUpdated = cmd.ExecuteNonQuery();
                                car.Log($"UpdateChargePrice: {rowsUpdated} rows updated to cost_per_kwh {ref_cost_per_kwh}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.DebugLog($"Exception during DBHelper.UpdateChargePrice(): {ex}");
                        Logfile.ExceptionWriter(ex, "Exception during DBHelper.UpdateChargePrice()");
                    }
                }

                // calculate and update cost_per_minute
                if (ref_cost_per_minute_found)
                {
                    double duration = (endDate - startDate).TotalMinutes;
                    car.Log($"UpdateChargePrice id:{ChargingStateID} cost_per_minute:{duration}min * {ref_cost_per_minute}{ref_cost_currency} = {ref_cost_per_minute * duration}");
                    if (!double.IsNaN(cost_total))
                    {
                        cost_total += ref_cost_per_minute * duration;
                    }
                    else
                    {
                        cost_total = ref_cost_per_minute * duration;
                    }
                    try
                    {
                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();
                            using (MySqlCommand cmd = new MySqlCommand(
        @"UPDATE 
  chargingstate 
SET 
  cost_per_minute=@cost_per_minute
WHERE 
  CarID = @CarID
  AND id=@ChargingStateID", con))
                            {
                                cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                                cmd.Parameters.AddWithValue("@ChargingStateID", ChargingStateID);
                                cmd.Parameters.AddWithValue("@cost_per_minute", ref_cost_per_minute);
                                Tools.DebugLog(cmd);
                                int rowsUpdated = cmd.ExecuteNonQuery();
                                car.Log($"UpdateChargePrice: {rowsUpdated} rows updated to cost_per_minute {ref_cost_per_minute}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.DebugLog($"Exception during DBHelper.UpdateChargePrice(): {ex}");
                        Logfile.ExceptionWriter(ex, "Exception during DBHelper.UpdateChargePrice()");
                    }
                }

                // calculate and update cost_per_session
                if (ref_cost_per_session_found)
                {
                    car.Log($"UpdateChargePrice id:{ChargingStateID} cost_per_session:{ref_cost_per_session}{ref_cost_currency}");
                    if (!double.IsNaN(cost_total))
                    {
                        cost_total += ref_cost_per_session;
                    }
                    else
                    {
                        cost_total = ref_cost_per_session;
                    }
                    try
                    {
                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();
                            using (MySqlCommand cmd = new MySqlCommand(
        @"UPDATE 
  chargingstate 
SET 
  cost_per_session=@cost_per_session
WHERE 
  CarID = @CarID
  AND id=@ChargingStateID", con))
                            {
                                cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                                cmd.Parameters.AddWithValue("@ChargingStateID", ChargingStateID);
                                cmd.Parameters.AddWithValue("@cost_per_session", ref_cost_per_session);
                                Tools.DebugLog(cmd);
                                int rowsUpdated = cmd.ExecuteNonQuery();
                                car.Log($"UpdateChargePrice: {rowsUpdated} rows updated to cost_per_session {ref_cost_per_session}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.DebugLog($"Exception during DBHelper.UpdateChargePrice(): {ex}");
                        Logfile.ExceptionWriter(ex, "Exception during DBHelper.UpdateChargePrice()");
                    }
                }

                // update cost_total
                if (!double.IsNaN(cost_total))
                {
                    car.Log($"UpdateChargePrice id:{ChargingStateID} cost_total:{cost_total}{ref_cost_currency}");
                    try
                    {
                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();
                            using (MySqlCommand cmd = new MySqlCommand(
        @"UPDATE 
  chargingstate 
SET 
  cost_total=@cost_total
WHERE 
  CarID = @CarID
  AND id=@ChargingStateID", con))
                            {
                                cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                                cmd.Parameters.AddWithValue("@ChargingStateID", ChargingStateID);
                                cmd.Parameters.AddWithValue("@cost_total", cost_total);
                                Tools.DebugLog(cmd);
                                int rowsUpdated = cmd.ExecuteNonQuery();
                                car.Log($"UpdateChargePrice: {rowsUpdated} rows updated to cost_total {cost_total}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.DebugLog($"Exception during DBHelper.UpdateChargePrice(): {ex}");
                        Logfile.ExceptionWriter(ex, "Exception during DBHelper.UpdateChargePrice()");
                    }
                }

                // update cost_currency
                if (!string.IsNullOrEmpty(ref_cost_currency))
                {
                    car.Log($"UpdateChargePrice id:{ChargingStateID} cost_currency:{ref_cost_currency}");
                    try
                    {
                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();
                            using (MySqlCommand cmd = new MySqlCommand(
        @"UPDATE 
  chargingstate 
SET 
  cost_currency=@cost_currency
WHERE 
  CarID = @CarID
  AND id=@ChargingStateID", con))
                            {
                                cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                                cmd.Parameters.AddWithValue("@ChargingStateID", ChargingStateID);
                                cmd.Parameters.AddWithValue("@cost_currency", ref_cost_currency);
                                Tools.DebugLog(cmd);
                                int rowsUpdated = cmd.ExecuteNonQuery();
                                car.Log($"UpdateChargePrice: {rowsUpdated} rows updated to cost_currency {ref_cost_currency}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.DebugLog($"Exception during DBHelper.UpdateChargePrice(): {ex}");
                        Logfile.ExceptionWriter(ex, "Exception during DBHelper.UpdateChargePrice()");
                    }
                }
            }
            else
            {
                Tools.DebugLog($"UpdateChargePrice: nothing to do ref_cost_per_kwh_found:{ref_cost_per_kwh_found} ref_cost_per_minute_found:{ref_cost_per_minute_found} ref_cost_per_session_found:{ref_cost_per_session_found}");
            }
        }

        private void UpdateChargeEnergyAdded(int ChargingStateID)
        {
            double startEnergyAdded = GetChargeEnergyAdded(ChargingStateID, "StartChargingID");
            double endEnergyAdded = GetChargeEnergyAdded(ChargingStateID, "EndChargingID");

            double charge_energy_added = endEnergyAdded - startEnergyAdded;

            if (charge_energy_added >= 0)
            {
                try
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
                        using (MySqlCommand cmd = new MySqlCommand(
    @"UPDATE 
  chargingstate 
SET 
  charge_energy_added=@charge_energy_added
WHERE 
  CarID = @CarID
  AND id=@ChargingStateID", con))
                        {
                            cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                            cmd.Parameters.AddWithValue("@charge_energy_added", charge_energy_added);
                            cmd.Parameters.AddWithValue("@ChargingStateID", ChargingStateID);
                            Tools.DebugLog(cmd);
                            int rowsUpdated = cmd.ExecuteNonQuery();
                            car.Log($"UpdateChargeEnergyAdded: {rowsUpdated} rows updated to charge_energy_added {charge_energy_added}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Tools.DebugLog($"Exception during DBHelper.UpdateChargeEnergyAdded(): {ex}");
                    Logfile.ExceptionWriter(ex, "Exception during DBHelper.UpdateChargeEnergyAdded()");
                }
            }
            else
            {
                Tools.DebugLog($"UpdateChargeEnergyAdded error - calculated {charge_energy_added} for ID {ChargingStateID} startEnergyAdded:{startEnergyAdded} endEnergyAdded:{endEnergyAdded} ");
            }
        }

        private double GetChargeEnergyAdded(int openChargingState, string column)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand($"SELECT charging.charge_energy_added FROM charging, chargingstate WHERE chargingstate.CarId = @CarID AND chargingstate.{column} = charging.id and chargingstate.id=@ChargingStateID", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.Parameters.AddWithValue("@ChargingStateID", openChargingState);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            if (double.TryParse(dr[0].ToString(), out double charge_energy_added))
                            {
                                return charge_energy_added;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "GetLastCarVersion");
                car.Log(ex.ToString());
            }

            return -1.0;
        }

        private int FindReferenceChargingState(string name, out string ref_cost_currency, out double ref_cost_per_kwh, out bool ref_cost_per_kwh_found, out double ref_cost_per_session, out bool ref_cost_per_session_found, out double ref_cost_per_minute, out bool ref_cost_per_minute_found)
        {
            int referenceID = int.MinValue;
            ref_cost_currency = string.Empty;
            ref_cost_per_kwh = double.NaN;
            ref_cost_per_kwh_found = false;
            ref_cost_per_minute = double.NaN;
            ref_cost_per_minute_found = false;
            ref_cost_per_session = double.NaN;
            ref_cost_per_session_found = false;
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  chargingstate.id, 
  chargingstate.cost_currency,
  chargingstate.cost_per_kwh,
  chargingstate.cost_per_session,
  chargingstate.cost_per_minute
FROM
  chargingstate,
  pos  
WHERE
  chargingstate.pos = pos.id
  AND pos.address = @addr
  AND chargingstate.cost_total IS NOT NULL
  AND TIMESTAMPDIFF(MINUTE, chargingstate.StartDate, chargingstate.EndDate) > 3
  AND chargingstate.EndChargingID - chargingstate.StartChargingID > 4
  AND chargingstate.CarID = @CarID
ORDER BY id DESC
LIMIT 1", con))
                    {
                        cmd.Parameters.Add("@addr", MySqlDbType.VarChar).Value = name;
                        cmd.Parameters.Add("@CarID", MySqlDbType.UByte).Value = car.CarInDB;
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            int.TryParse(dr[0].ToString(), out referenceID);
                            if (dr[1] != DBNull.Value)
                            {
                                ref_cost_currency = dr.GetString(1);
                            }
                            if (double.TryParse(dr[2].ToString(), out ref_cost_per_kwh))
                            {
                                ref_cost_per_kwh_found = true;
                            }
                            if (double.TryParse(dr[3].ToString(), out ref_cost_per_session))
                            {
                                ref_cost_per_session_found = true;
                            }
                            if (double.TryParse(dr[4].ToString(), out ref_cost_per_minute))
                            {
                                ref_cost_per_minute_found = true;
                            }
                            Tools.DebugLog($"FindReferenceChargingState id:{dr[0]} currency:{dr[1]} cost_per_kwh:{dr[2]} cost_per_session:{dr[3]} cost_per_minute:{dr[4]}");
                        }
                        else
                        {
                            Tools.DebugLog("FindReferenceChargingState dr.read failed");
                        }
                        con.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog($"Exception during FindReferenceChargingState(): {ex}");
                Logfile.ExceptionWriter(ex, "Exception during FindReferenceChargingState()");
            }
            return referenceID;
        }

        private bool GetStartValuesFromChargingState(int ChargingStateID, out DateTime startDate, out int startdID, out int posID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
 StartDate,
 StartChargingID,
 Pos
FROM
 chargingstate
WHERE
 id=@ChargingStateID", con))
                    {
                        cmd.Parameters.AddWithValue("@ChargingStateID", ChargingStateID);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (DateTime.TryParse(dr[0].ToString(), out startDate)
                                && int.TryParse(dr[1].ToString(), out startdID)
                                && int.TryParse(dr[2].ToString(), out posID))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog($"Exception during CloseChargingState(): {ex}");
                Logfile.ExceptionWriter(ex, "Exception during CloseChargingState()");
            }
            startDate = DateTime.MinValue;
            startdID = int.MinValue;
            posID = int.MinValue;
            return false;
        }

        private double GetOdometerFromChargingstate(int openChargingState)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
 pos.odometer
FROM
 chargingstate,
 pos
WHERE
 pos.CarID=@CarID
 AND chargingstate.id=@ChargingStateID
 AND chargingstate.Pos = pos.id", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.Parameters.AddWithValue("@ChargingStateID", openChargingState);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (double.TryParse(dr[0].ToString(), out double odometer))
                            {
                                return odometer;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog($"Exception during CloseChargingState(): {ex}");
                Logfile.ExceptionWriter(ex, "Exception during CloseChargingState()");
            }
            return double.NaN;
        }

        private Queue<int> FindOpenChargingStates()
        {
            Queue<int> openChargingStates = new Queue<int>();
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
 id
FROM
 chargingstate
WHERE
 CarID=@CarID
 AND EndDate IS NULL ORDER BY StartDate ASC", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (int.TryParse(dr[0].ToString(), out int id))
                            {
                                openChargingStates.Enqueue(id);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog($"Exception during FindOpenChargingStates(): {ex}");
                Logfile.ExceptionWriter(ex, "Exception during FindOpenChargingStates()");
            }
            return openChargingStates;
        }

        private Queue<int> FindSimilarChargingStates(int referenceID)
        {
            Queue<int> chargingStates = new Queue<int>();
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
 chargingstate.id
FROM
 chargingstate,
 pos
WHERE
 chargingstate.CarID=@CarID1
 AND chargingstate.Pos = pos.id
 AND chargingstate.cost_per_session IS NULL
 AND chargingstate.id<>@referenceID1
 AND pos.odometer=(
   SELECT
     pos.odometer
   FROM
     chargingstate,
     pos
   WHERE
    pos.CarID=@CarID2
    AND chargingstate.id=@referenceID2
    AND chargingstate.Pos = pos.id)
AND chargingstate.conn_charge_cable = (
  SELECT
    conn_charge_cable
  FROM
    chargingstate
  WHERE
    chargingstate.CarID=@CarID3
    AND id=@referenceID3)
AND chargingstate.fast_charger_brand = (
  SELECT
    fast_charger_brand
  FROM
    chargingstate
  WHERE
    chargingstate.CarID=@CarID4
    AND id=@referenceID4)
AND chargingstate.fast_charger_type = (
  SELECT
    fast_charger_type
  FROM
    chargingstate
  WHERE
    chargingstate.CarID=@CarID5
    AND id=@referenceID5)
ORDER BY chargingstate.id ASC", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID1", car.CarInDB);
                        cmd.Parameters.AddWithValue("@CarID2", car.CarInDB);
                        cmd.Parameters.AddWithValue("@CarID3", car.CarInDB);
                        cmd.Parameters.AddWithValue("@CarID4", car.CarInDB);
                        cmd.Parameters.AddWithValue("@CarID5", car.CarInDB);
                        cmd.Parameters.AddWithValue("@referenceID1", referenceID);
                        cmd.Parameters.AddWithValue("@referenceID2", referenceID);
                        cmd.Parameters.AddWithValue("@referenceID3", referenceID);
                        cmd.Parameters.AddWithValue("@referenceID4", referenceID);
                        cmd.Parameters.AddWithValue("@referenceID5", referenceID);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (int.TryParse(dr[0].ToString(), out int id))
                            {
                                chargingStates.Enqueue(id);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog($"Exception during FindChargingStatesByOdometer(): {ex}");
                Logfile.ExceptionWriter(ex, "Exception during FindChargingStatesByOdometer()");
            }
            return chargingStates;
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

        public void UpdateMaxChargerPower(int chargingstateid)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  id,
  StartChargingID,
  EndChargingID
FROM
  chargingstate
WHERE
  CarID=@CarID
  AND id=@id
ORDER BY id DESC", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.Parameters.AddWithValue("@id", chargingstateid);
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
                  if (StartPos > 0)
                  {
                      UpdateTripElevation(StartPos, MaxPosId, " (Task)");

                      StaticMapService.GetSingleton().Enqueue(car.CarInDB, StartPos, MaxPosId, 0, 0, StaticMapProvider.MapMode.Dark, StaticMapProvider.MapSpecial.None);
                      StaticMapService.GetSingleton().CreateParkingMapFromPosid(StartPos);
                      StaticMapService.GetSingleton().CreateParkingMapFromPosid(MaxPosId);
                  }
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
                    dt.Clear();
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
                    cmd.Parameters.AddWithValue("@speed", MphToKmhRounded(speed));
                    cmd.Parameters.AddWithValue("@power", Convert.ToInt32(power * 1.35962M));
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

        private Address GetAddressFromChargingState(int ChargingStateID)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
 pos.lat,
 pos.lng
FROM
 chargingstate,
 pos
WHERE
 chargingstate.CarID=@CarID
 AND chargingstate.Pos = pos.id
 AND chargingstate.id=@ChargingStateID", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.Parameters.AddWithValue("@ChargingStateID", ChargingStateID);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read() && dr[0] != DBNull.Value && dr[1] != DBNull.Value)
                        {
                            if (double.TryParse(dr[0].ToString(), out double lat)
                                && double.TryParse(dr[1].ToString(), out double lng))
                            {
                                Address addr = Geofence.GetInstance().GetPOI(lat, lng, false);
                                Tools.DebugLog("GetAddressFromChargingState: " + addr);
                                return addr;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog($"Exception during GetAddressFromChargingState(): {ex}");
                Logfile.ExceptionWriter(ex, "Exception during GetAddressFromChargingState()");
            }
            return null;
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
                                // set waitbetween2pointsdb to 15 seconds
                                waitbetween2pointsdb = 15;
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

        public static object ExecuteSQLScalar(string sql, int timeout = 30)
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

                        return cmd.ExecuteScalar();
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
                        Tools.DebugLog(cmd);
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
                        Tools.DebugLog(cmd);
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
                using (DataTable dt = new DataTable())
                {
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

                            sumkm = Math.Round((double)r["sumkm"], 1);
                            avgkm = Math.Round((double)r["avgkm"], 1);
                            kwh100km = Math.Round((double)r["kwh100km"], 1);
                            avgsocdiff = Math.Round((double)r["avgsocdiff"], 1);
                            maxkm = Math.Round((double)r["maxkm"], 1);

                            car.Log($"GetAvgConsumption: sumkm:{sumkm} avgkm:{avgkm} kwh/100km:{kwh100km} avgsocdiff:{avgsocdiff} maxkm:{maxkm}");
                        }
                    }
                    dt.Clear();
                }
            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
            }
        }

        DataTable GetLatestDC_Charging_with_50PercentSOC()
        {
            DataTable dt = new DataTable();
            string sql = @"select c1.Datum as sd, c2.Datum as ed, chargingstate.carid from chargingstate 
                join charging c1 on c1.id = startchargingid 
                join charging c2 on c2.id = endchargingid
                where max_charger_power > 30 and c1.battery_level < 50 and c2.battery_level > 50 and chargingstate.carid = @carid
                order by chargingstate.startdate desc
                limit 5";

            using (MySqlDataAdapter da = new MySqlDataAdapter(sql, DBConnectionstring))
            {
                da.SelectCommand.Parameters.AddWithValue("@carid", car.CarInDB);
                da.Fill(dt);
            }

            return dt;
        }

        public double GetVoltageAt50PercentSOC(out DateTime start, out DateTime ende)
        {
            start = DateTime.MinValue;
            ende = DateTime.MinValue;

            try
            {
                using (DataTable dt = GetLatestDC_Charging_with_50PercentSOC())
                {
                    string sql = "select avg(charger_voltage) from charging where carid = @carid and Datum between @start and @ende and charger_voltage > 300";

                    foreach (DataRow dr in dt.Rows)
                    {
                        using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                        {
                            con.Open();
                            using (MySqlCommand cmd = new MySqlCommand(sql, con))
                            {
                                start = (DateTime)dr["sd"];
                                ende = (DateTime)dr["ed"];

                                cmd.Parameters.AddWithValue("@carid", car.CarInDB);
                                cmd.Parameters.AddWithValue("@start", start);
                                cmd.Parameters.AddWithValue("@ende", ende);
                                object ret = cmd.ExecuteScalar();

                                if (ret == DBNull.Value)
                                    continue;

                                return Convert.ToDouble(ret);
                            }
                        }
                    }
                    dt.Clear();
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            return 0;
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

        private static int MphToKmhRounded(double speed_mph)
        {
            int speed_floor = (int)(speed_mph * 1.60934);
            // handle special speed_floor as Math.Round is off by +1
            if (
                speed_floor == 30
                || speed_floor == 33
                || speed_floor == 83
                || speed_floor == 123
                || speed_floor == 133
                )
            {
                return speed_floor;
            }
            return (int)Math.Round(speed_mph / 0.62137119223733);
        }

        internal static void MigrateFloorRound()
        {
            string migrationstatusfile = "migrate_floor_round.txt";

            /*
             * DB stores speed in km/h
             * API has speed in mph
             * rounding takes place twice: car display in km/h -> API speed in mph -> mpt to km/h in TeslaLogger
             * 
             * migrate errors coming from older versions originating from floor() vs. round()
             * 
             */

            if (!File.Exists(migrationstatusfile))
            {
                try
                {
                    StringBuilder migrationlog = new StringBuilder();
                    Logfile.Log("MigrateFloorRound() start");
                    migrationlog.Append($"{DateTime.Now} MigrateFloorRound() start" + Environment.NewLine);

                    // add indexes to speed up things
                    Logfile.Log("MigrateFloorRound() ADD INDEX speed");
                    migrationlog.Append($"{DateTime.Now} ADD INDEX speed" + Environment.NewLine);
                    int sqlresult = ExecuteSQLQuery("ALTER TABLE pos ADD INDEX idx_migration_speed (speed)", 6000);
                    migrationlog.Append($"{DateTime.Now} sqlresult {sqlresult}" + Environment.NewLine);

                    // get max speed

                    int maxspeed_kmh = 0;

                    using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                    {
                        con.Open();
                        using (MySqlCommand cmd = new MySqlCommand(
@"SELECT
  MAX(speed)
FROM
pos", con))
                        {
                            MySqlDataReader dr = cmd.ExecuteReader();
                            if (dr.Read() && dr[0] != DBNull.Value)
                            {
                                int.TryParse(dr[0].ToString(), out maxspeed_kmh);
                            }
                        }
                        con.Close();
                    }

                    if (maxspeed_kmh == 0)
                    {
                        maxspeed_kmh = 500;
                    }

                    Logfile.Log($"maxspeed_kmh: {maxspeed_kmh}");
                    migrationlog.Append($"maxspeed_kmh: {maxspeed_kmh}" + Environment.NewLine);

                    // migrate floor round error for pos.speed

                    for (int speed_mph = (int)Math.Round(maxspeed_kmh * 0.62137119223733) + 1; speed_mph > 0; speed_mph--)
                    {
                        int speed_floor = (int)(speed_mph * 1.60934); // old conversion
                        int speed_round = MphToKmhRounded(speed_mph); // new conversion
                        if (speed_floor != speed_round)
                        {
                            DateTime start = DateTime.Now;
                            Logfile.Log($"MigrateFloorRound(): speed {speed_floor} -> {speed_round}");
                            migrationlog.Append($"{DateTime.Now} speed {speed_floor} -> {speed_round}" + Environment.NewLine);
                            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                            {
                                con.Open();
                                using (MySqlCommand cmd = new MySqlCommand(
@"UPDATE
  pos
SET
  speed = @speedround
WHERE
  speed = @speedfloor", con))
                                {
                                    cmd.Parameters.Add("speedround", MySqlDbType.Int32).Value = speed_round;
                                    cmd.Parameters.Add("speedfloor", MySqlDbType.Int32).Value = speed_floor;
                                    int updated_rows = cmd.ExecuteNonQuery();
                                    Logfile.Log($" rows updated: {updated_rows} duration: {(DateTime.Now - start).TotalMilliseconds}ms");
                                    migrationlog.Append($"{DateTime.Now} rows updated: {updated_rows} duration: {(DateTime.Now - start).TotalMilliseconds}ms" + Environment.NewLine);
                                }
                                con.Close();
                            }
                        }
                    }

                    // update all drivestate statistics
                    foreach (Car c in Car.allcars)
                    {
                        using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                        {
                            con.Open();
                            using (MySqlCommand cmd = new MySqlCommand(
@"SELECT
  StartPos,
  EndPos
FROM
  drivestate
WHERE
  CarID = @CarID", con))
                            {
                                cmd.Parameters.Add("@CarID", MySqlDbType.UByte).Value = c.CarInDB;
                                MySqlDataReader dr = cmd.ExecuteReader();
                                while (dr.Read())
                                {
                                    if (dr[0] != null && int.TryParse(dr[0].ToString(), out int startpos)
                                        && dr[1] != null && int.TryParse(dr[1].ToString(), out int endpos))
                                    {
                                        DateTime start = DateTime.Now;
                                        c.dbHelper.UpdateDriveStatistics(startpos, endpos, false);
                                        c.Log($"UpdateDriveStatistics: {startpos} -> {endpos} duration: {(DateTime.Now - start).TotalMilliseconds}ms");
                                        migrationlog.Append($"{DateTime.Now} {c.CarInDB}# UpdateDriveStatistics: {startpos} -> {endpos} duration: {(DateTime.Now - start).TotalMilliseconds}ms" + Environment.NewLine);
                                    }
                                }
                            }
                        }
                    }

                    // remove indexes
                    Logfile.Log("MigrateFloorRound() DROP INDEX speed");
                    migrationlog.Append($"{DateTime.Now} DROP INDEX speed" + Environment.NewLine);
                    sqlresult = ExecuteSQLQuery("ALTER TABLE pos DROP INDEX idx_migration_speed", 6000);
                    migrationlog.Append($"{DateTime.Now} sqlresult {sqlresult}" + Environment.NewLine);

                    // cleanup DB files
                    Logfile.Log("MigrateFloorRound() REBUILD");
                    migrationlog.Append($"{DateTime.Now} REBUILD" + Environment.NewLine);
                    sqlresult = ExecuteSQLQuery("ALTER TABLE pos FORCE", 6000);
                    migrationlog.Append($"{DateTime.Now} sqlresult {sqlresult}" + Environment.NewLine);

                    Logfile.Log("MigrateFloorRound() finished");
                    migrationlog.Append($"{DateTime.Now} MigrateFloorRound() finished" + Environment.NewLine);

                    // persist that migration ran successful to prevent another run
                    File.WriteAllText(migrationstatusfile, migrationlog.ToString());
                }
                catch (Exception ex)
                {
                    Tools.DebugLog("Exception MigrateFloorRound()", ex);
                }
            }
        }

        private void CloseChargingState(int openChargingState)
        {
            try
            {
                car.Log($"CloseChargingState id:{openChargingState}");
                StaticMapService.GetSingleton().CreateChargingMapOnChargingCompleted(car.CarInDB);
                int chargeID = GetMaxChargeid(out DateTime chargeEnd);
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
UPDATE
 chargingstate
SET
 EndDate = @EndDate,
 EndChargingID = @EndChargingID
WHERE
 id=@ChargingStateID
 AND CarID=@CarID", con))
                    {
                        cmd.Parameters.AddWithValue("@EndDate", chargeEnd);
                        cmd.Parameters.AddWithValue("@EndChargingID", chargeID);
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.Parameters.AddWithValue("@ChargingStateID", openChargingState);
                        Tools.DebugLog(cmd);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog($"Exception during CloseChargingState(): {ex}");
                Logfile.ExceptionWriter(ex, "Exception during CloseChargingState()");
            }
        }

    }
}
