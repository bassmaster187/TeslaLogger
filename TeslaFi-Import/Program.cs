using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaFi_Import
{
    class Program
    {
        static string DBConnectionstring = "Server=127.0.0.1;Database=teslalogger;Uid=root;Password=teslalogger;";
        public static System.Globalization.CultureInfo ciEnUS = new System.Globalization.CultureInfo("en-US");
        static string[] columnsToCheck = new string[] { "latitude", "longitude", "speed", "power" };

        static int currentPosId = 0;
        static int currentChargeid = 0;
        static int currentStateId = 0;

        static void Main(string[] args)
        {
            int id = 0;
            try
            {
                Tools.Log(0, "***** Start TeslaFi "+ System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " Started *****");
                if (Tools.IsDocker())
                {
                    Tools.Log(0, "DOCKER Version!");
                    DBConnectionstring = "Server=database;Database=teslalogger;Uid=root;Password=teslalogger;";
                }

                if (Settings1.Default.DBConnectionstring.Length > 0)
                    DBConnectionstring = Settings1.Default.DBConnectionstring;

                Tools.Log(0, "DBConnectionstring: " + DBConnectionstring);

                DataTable dt = new DataTable();
                dt.Columns.Add("data_id");
                dt.Columns.Add("Date", typeof(DateTime));
                dt.Columns.Add("battery_level");
                dt.Columns.Add("charge_energy_added");
                dt.Columns.Add("ideal_battery_range");
                dt.Columns.Add("battery_range");
                dt.Columns.Add("charging_state");
                dt.Columns.Add("fast_charger_present");
                dt.Columns.Add("charger_voltage");
                dt.Columns.Add("charge_current_request_max");
                dt.Columns.Add("inside_temp");
                dt.Columns.Add("longitude");
                dt.Columns.Add("latitude");
                dt.Columns.Add("speed");
                dt.Columns.Add("shift_state");
                dt.Columns.Add("outside_temp");
                dt.Columns.Add("odometer");
                dt.Columns.Add("power");
                dt.Columns.Add("elevation");
                dt.Columns.Add("state");
                dt.Columns.Add("charger_power");
                dt.Columns.Add("charger_phases");
                dt.Columns.Add("charger_actual_current");
                dt.Columns.Add("charger_pilot_current");
                dt.Columns.Add("charge_current_request");
                dt.Columns.Add("fast_charger_type");
                dt.Columns.Add("car_version");
                               
                LoadAllFiles(dt);

                int dateColumnID = dt.Columns["Date"].Ordinal;

                Tools.Log(0, "Delete old import");
                String[] tables = new String[] { "car_version", "charging", "chargingstate", "drivestate", "pos", "state" };
                foreach (var table in tables)
                    DeleteData(table);

                DateTime firstTeslaloggerData = GetFirstTeslaloggerData();
                Tools.Log(0, "First Teslalogger Data: " + firstTeslaloggerData.ToLongDateString());

                Tools.Log(0, "start Parsing");

                string oldShiftstate = "P";
                string oldChargingstate = "";
                string oldState = "";
                string oldCar_version = "";
                int lastMovementID = 0;
                DateTime lastMovement = DateTime.MaxValue;

                DataView dv = new DataView(dt, "", "Date", DataViewRowState.CurrentRows);

                foreach (DataRowView drv in dv)
                {
                    try
                    {
                        DataRow dr = drv.Row;
                        DateTime Date = (DateTime)dr["Date"];
                        id = Convert.ToInt32(dr["Data_id"]);

                        if (Date >= firstTeslaloggerData)
                        {
                            Tools.Log(id, "First Teslalogger Data reached. TeslaFi data skipped!");
                            break;
                        }

                        if (dr["car_version"] != DBNull.Value && dr["car_version"].ToString().Length > 4 && dr["car_version"].ToString() != oldCar_version)
                        {
                            oldCar_version = dr["car_version"].ToString();
                            InsertCarVersion(id, oldCar_version, Date);
                        }


                        if (oldChargingstate != "Charging")
                        {
                            if (InsertPos(dr))
                            {
                                lastMovementID = GetMaxPosid();
                                lastMovement = Date;
                            }

                            if (oldShiftstate != "P" && lastMovement != null)
                            {
                                TimeSpan ts = Date - lastMovement;
                                if (ts.TotalMinutes > 10)
                                {
                                    Tools.Log(id, "End Driving [10 Min no movement] " + lastMovement.ToString());
                                    oldShiftstate = "P";
                                    CloseDriveState(lastMovement, lastMovementID);
                                }
                            }
                        }

                        string newShiftstate = dr["shift_state"].ToString();
                        if (oldShiftstate == "P" && (newShiftstate == "D" || newShiftstate == "R"))
                        {
                            // Driving
                            Tools.Log(id, "Start Driving " + Date.ToString());
                            oldShiftstate = newShiftstate;
                            StartDriveState(Date);
                        }
                        else if (newShiftstate == "P" && (oldShiftstate == "D" || oldShiftstate == "R"))
                        {
                            // End of Driving
                            Tools.Log(id, "End Driving " + Date.ToString());
                            oldShiftstate = newShiftstate;
                            CloseDriveState(Date);
                        }

                        string newChargingstate = dr["charging_state"].ToString();
                        if (newChargingstate == "Charging")
                        {
                            if (oldShiftstate != "P" && lastMovementID > 0)
                            {
                                Tools.Log(id, "End Driving [charging]" + Date.ToString());
                                oldShiftstate = "P";
                                CloseDriveState(lastMovement, lastMovementID);
                            }

                            InsertCharging(dr);
                        }

                        if (oldChargingstate == "" && newChargingstate == "Charging")
                        {
                            Tools.Log(id, "Start Charging " + Date.ToString());
                            oldChargingstate = newChargingstate;
                            StartChargingState(dr);
                        }
                        else if (oldChargingstate == "Charging" && (newChargingstate == "Complete" || newChargingstate == "Disconnected"))
                        {
                            Tools.Log(id, "Stop Charging " + Date.ToString());
                            oldChargingstate = "";
                            CloseChargingState(Date);
                            InsertPos(dr);
                        }

                        string newState = dr["state"].ToString();
                        if (newState.Length > 0 && oldState != newState)
                        {
                            oldState = newState;
                            Tools.Log(id, "state: " + newState);
                            StartState(newState, Date);
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.Log(id, ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.Log(id, ex.ToString());
            }

            Tools.Log(0, "end Parsing");
        }

        private static void InsertCarVersion(int id, string Car_version, DateTime Date)
        {
            Tools.Log(id, "car_version: " + Car_version);
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();

                MySqlCommand cmd = new MySqlCommand("insert car_version (StartDate, version, import, carid) values (@StartDate, @version, 1, @carid)", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                cmd.Parameters.AddWithValue("@StartDate", Date.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@version", Car_version);
                cmd.ExecuteNonQuery();
            }
        }

        private static DateTime GetFirstTeslaloggerData()
        {
            DateTime dtMin = DateTime.Now;

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT StartDate FROM drivestate where import is null and carid=@carid order by id limit 1", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    DateTime dtDrivestate = (DateTime)dr[0];
                    if (dtDrivestate < dtMin)
                        dtMin = dtDrivestate;
                }
                dr.Close();

                cmd = new MySqlCommand("SELECT StartDate FROM chargingstate where import is null and carid=@carid  order by id limit 1", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    DateTime dtChargestate = (DateTime)dr[0];
                    if (dtChargestate < dtMin)
                        dtMin = dtChargestate;
                }
                dr.Close();

            }

            return dtMin;
        }

        private static void LoadAllFiles(DataTable dt)
        {
            var files = System.IO.Directory.EnumerateFiles(".", "TeslaFi*.csv");  
            
            if (files.Count() == 0)
            {
                Tools.Log(0,"No Teslafi files found!");
            }

            foreach (var file in files)
                LoadData(dt, file);
        }

        static void StartChargingState(DataRow dr)
        {
            DateTime Date = (DateTime)dr["Date"];
            string fast_charger_type = dr["fast_charger_type"].ToString();
            string fast_charger_brand = ""; //  dr["fast_charger_brand"].ToString();
            string conn_charge_cable = ""; //  dr["conn_charge_cable"].ToString();
            string fast_charger_present = dr["fast_charger_present"].ToString();

            if (fast_charger_present == "False")
                fast_charger_present = "0";
            else if (fast_charger_present == "True")
                fast_charger_present = "1";

            StartChargingState(Date, fast_charger_brand, fast_charger_type, conn_charge_cable, fast_charger_present);
        }

        public static void CloseChargingState(DateTime Date)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("update chargingstate set EndDate = @EndDate, EndChargingID = @EndChargingID where EndDate is null  and carid=@carid ", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                cmd.Parameters.AddWithValue("@EndDate", Date);
                cmd.Parameters.AddWithValue("@EndChargingID", GetMaxChargeid());
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertCharging(DataRow dr)
        {
            DateTime Date = (DateTime)dr["Date"];
            string battery_level = dr["battery_level"].ToString();
            string charge_energy_added = dr["charge_energy_added"].ToString();
            string charger_power = dr["charger_power"].ToString();

            double ideal_battery_range = 0;
            if (dr["ideal_battery_range"].ToString() == "999.0") // Raven
                ideal_battery_range = Convert.ToDouble(dr["battery_range"], ciEnUS);
            else
                ideal_battery_range = Convert.ToDouble(dr["ideal_battery_range"], ciEnUS);

            ideal_battery_range = ideal_battery_range / (double)0.62137;

            string charger_voltage = dr["charger_voltage"].ToString();
            string charger_phases = dr["charger_phases"].ToString();
            string charger_actual_current = dr["charger_actual_current"].ToString();
            string charger_pilot_current = dr["charger_pilot_current"].ToString();
            string charge_current_request = dr["charge_current_request"].ToString();

            double? outside_temp = null;
            if (dr["outside_temp"] != DBNull.Value && dr["outside_temp"].ToString().Length > 0)
                outside_temp = Convert.ToDouble(dr["outside_temp"], ciEnUS);

            InsertCharging(Date, battery_level, charge_energy_added, charger_power, ideal_battery_range, charger_voltage, charger_phases, charger_actual_current, outside_temp, true, charger_pilot_current, charge_current_request);
        }

        static bool CheckColumn(string columnname, DataRow dr)
        {
            if (dr[columnname] == DBNull.Value || dr[columnname].ToString().Length == 0 || dr[columnname].ToString() == "None")
                return false;

            return true;
        }

        static bool CheckAllColumns(DataRow dr)
        {
            foreach (var c in columnsToCheck)
            {
                bool ret = CheckColumn(c, dr);
                if (!ret)
                    return false;
            }
            return true;
        }

        private static bool InsertPos(DataRow dr)
        {
            if (!CheckAllColumns(dr))
                return false;

            DateTime Date = (DateTime)dr["Date"];

            double latitude = Convert.ToDouble(dr["latitude"], ciEnUS);
            double longitude = Convert.ToDouble(dr["longitude"], ciEnUS);

            if (latitude > 90 || latitude < -90)
                return false;

            if (longitude > 180 || longitude < -180)
                return false;

            int speed = (int)(Convert.ToDecimal(dr["speed"])); 
            int power = (int)(Convert.ToDecimal(dr["power"])); 
            double odometerKM = (Double)(Convert.ToDecimal(dr["odometer"], ciEnUS) / 0.62137M);

            double ideal_battery_range = 0;
            if (dr["ideal_battery_range"].ToString() == "999.0") // Raven
                ideal_battery_range = Convert.ToDouble(dr["battery_range"], ciEnUS);
            else
                ideal_battery_range = Convert.ToDouble(dr["ideal_battery_range"], ciEnUS);

            ideal_battery_range = ideal_battery_range / (double)0.62137;

            int battery_level = Convert.ToInt32(dr["battery_level"]);

            string elevation = dr["elevation"].ToString();
            string inside_temp = dr["inside_temp"].ToString();

            double ? outside_temp = null;
            if (dr["outside_temp"] != DBNull.Value && dr["outside_temp"].ToString().Length > 0)
                outside_temp = Convert.ToDouble(dr["outside_temp"], ciEnUS);

            InsertPos(Date, latitude, longitude, speed, power, odometerKM, ideal_battery_range, battery_level, outside_temp, elevation, inside_temp, "0", "0","0");

            if (speed > 0)
                return true;

            return false;
        }

        private static void DeleteData(string table)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand($"alter table {table} ADD column IF NOT EXISTS import TINYINT(1) NULL", con);
                cmd.CommandTimeout = 300;
                cmd.ExecuteNonQuery();
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand($"delete from {table} where import=1", con);
                cmd.CommandTimeout = 300;
                int cnt = cmd.ExecuteNonQuery();
                Tools.Log(0, $"Deleted {cnt} Rows from Table {table }");
            }
        }

        private static void LoadData(DataTable dt, string Filename)
        {
            Tools.Log(0, "Load csv File " + Filename);

            string[] columns = {""};

            bool firstline = true;
            int dateColumnID = 0;

            var lines = System.IO.File.ReadLines(Filename);
            Tools.Log(0, "Write into DataTable");

            foreach (var line in lines)
            {
                if (firstline)
                {
                    firstline = false;
                    columns = Tools.SmartSplit(line);

                    for (int c = 0; c < columns.Length; c++)
                    {
                        string column = columns[c];
                        if (column == "Date")
                        {
                            dateColumnID = c;
                            break;
                        }
                    }

                    continue;
                }

                DataRow dr = dt.NewRow();
                string l = line;

                // Some files are starting with " and that's no valid CSV file
                if (l.StartsWith("\""))
                    l = l.Substring(1);

                string[] csv = Tools.SmartSplit(l);
                for (int y = 0; y < csv.Length; y++)
                {
                    if (y == dateColumnID)
                        dr[y] = DateTime.Parse(csv[y].Replace("\"", ""));
                    else
                    {
                        string columnname = columns[y];

                        if (dt.Columns.Contains(columnname))
                            dr[columnname] = csv[y];
                    }
                }
                dt.Rows.Add(dr);
            }

            dt.AcceptChanges();

            Tools.Log(0, "Load CSV File finished");
        }

        internal static void InsertPos(DateTime date, double latitude, double longitude, int speed, decimal power, double odometer, double ideal_battery_range_km, int battery_level, double? outside_temp, string altitude, string inside_temp, string battery_heater, string is_preconditioning, string sentry_mode)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();

                MySqlCommand cmd = new MySqlCommand("insert pos (import, Datum, lat, lng, speed, power, odometer, ideal_battery_range_km, outside_temp, altitude, battery_level, inside_temp, battery_heater, is_preconditioning, sentry_mode, carid) values (1, @Datum, @lat, @lng, @speed, @power, @odometer, @ideal_battery_range_km, @outside_temp, @altitude, @battery_level, @inside_temp, @battery_heater, @is_preconditioning, @sentry_mode, @carid )", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                cmd.Parameters.AddWithValue("@Datum", date.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@lat", latitude.ToString(ciEnUS));
                cmd.Parameters.AddWithValue("@lng", longitude.ToString(ciEnUS));
                cmd.Parameters.AddWithValue("@speed", (int)((decimal)speed * 1.60934M));
                cmd.Parameters.AddWithValue("@power", (int)(power * 1.35962M));
                cmd.Parameters.AddWithValue("@odometer", odometer.ToString(ciEnUS));

                if (ideal_battery_range_km == -1)
                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", ideal_battery_range_km.ToString(ciEnUS));

                if (outside_temp == null)
                    cmd.Parameters.AddWithValue("@outside_temp", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@outside_temp", ((double)outside_temp).ToString(ciEnUS));

                if (altitude.Length == 0)
                    cmd.Parameters.AddWithValue("@altitude", DBNull.Value);
                else
                {
                    double tempAltituge = Convert.ToDouble(altitude, ciEnUS);
                    if (tempAltituge < 7000)
                        cmd.Parameters.AddWithValue("@altitude", altitude);
                    else
                        cmd.Parameters.AddWithValue("@altitude", DBNull.Value);
                }

                if (battery_level == -1)
                    cmd.Parameters.AddWithValue("@battery_level", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@battery_level", battery_level.ToString());

                if (inside_temp == null)
                    cmd.Parameters.AddWithValue("@inside_temp", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@inside_temp", inside_temp);

                cmd.Parameters.AddWithValue("@battery_heater", battery_heater);
                cmd.Parameters.AddWithValue("@is_preconditioning", is_preconditioning);
                cmd.Parameters.AddWithValue("@sentry_mode", sentry_mode);

                cmd.ExecuteNonQuery();

                cmd = new MySqlCommand("SELECT LAST_INSERT_ID();", con);
                cmd.Parameters.Clear();
                currentPosId = Convert.ToInt32(cmd.ExecuteScalar());

            }
        }

        public static DateTime UnixToDateTime(long t)
        {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddMilliseconds(t);
            dt = dt.ToLocalTime();
            return dt;

        }

        public static void StartDriveState(DateTime date)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("insert drivestate (import, StartDate, StartPos, carid) values (1, @StartDate, @Pos, @carid)", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                cmd.Parameters.AddWithValue("@StartDate", date);
                cmd.Parameters.AddWithValue("@Pos", GetMaxPosid());
                cmd.ExecuteNonQuery();
            }
        }

        public static void CloseDriveState(DateTime EndDate)
        {
            CloseDriveState(EndDate, GetMaxPosid());
        }

        public static void CloseDriveState(DateTime EndDate, int posId)
        {
            int StartPos = 0;

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("select StartPos from drivestate where EndDate is null and carid=@carid ", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
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
                MySqlCommand cmd = new MySqlCommand("update drivestate set EndDate = @EndDate, EndPos = @Pos where EndDate is null  and carid=@carid ", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                cmd.Parameters.AddWithValue("@EndDate", EndDate);
                cmd.Parameters.AddWithValue("@Pos", posId);
                cmd.ExecuteNonQuery();
            }

            if (StartPos != 0)
                UpdateDriveStatistics(StartPos, posId);
        }

        public static int GetMaxPosid(bool withReverseGeocoding = true)
        {
            return currentPosId;
        }

        private static void UpdateDriveStatistics(int startPos, int endPos, bool logging = false)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT avg(outside_temp) as outside_temp_avg, max(speed) as speed_max, max(power) as power_max, min(power) as power_min, avg(power) as power_avg FROM pos where id between @startpos and @endpos  and carid=@carid ", con);
                    cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                    cmd.Parameters.AddWithValue("@startpos", startPos);
                    cmd.Parameters.AddWithValue("@endpos", endPos);

                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        using (MySqlConnection con2 = new MySqlConnection(DBConnectionstring))
                        {
                            con2.Open();
                            MySqlCommand cmd2 = new MySqlCommand("update drivestate set outside_temp_avg=@outside_temp_avg, speed_max=@speed_max, power_max=@power_max, power_min=@power_min, power_avg=@power_avg where StartPos=@StartPos and EndPos=@EndPos and carid=@carid ", con2);
                            cmd2.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
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

                            cmd = new MySqlCommand("SELECT * FROM pos where id > @startPos and ideal_battery_range_km is not null and battery_level is not null and carid=@carid  order by id asc limit 1", con);
                            cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
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

                                    // Logfile.Log($"Trip from {dt1} ideal_battery_range_km updated!");
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

                            cmd = new MySqlCommand("SELECT * FROM pos where id < @endpos and ideal_battery_range_km is not null and battery_level is not null  and carid=@carid order by id desc limit 1", con);
                            cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
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

                                    // Logfile.Log($"Trip from {dt1} ideal_battery_range_km updated!");
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
                // Logfile.Log(ex.ToString());
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        internal static void InsertCharging(DateTime Date, string battery_level, string charge_energy_added, string charger_power, double ideal_battery_range, string charger_voltage, string charger_phases, string charger_actual_current, double? outside_temp, bool forceinsert, string charger_pilot_current, string charge_current_request)
        {

            if (charger_phases == "" || charger_phases == "None")
                charger_phases = "1";

            double kmRange = ideal_battery_range;

            double powerkW = Convert.ToDouble(charger_power);
            double waitbetween2pointsdb = 1000.0 / powerkW;

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("insert charging (import, Datum, battery_level, charge_energy_added, charger_power, ideal_battery_range_km, charger_voltage, charger_phases, charger_actual_current, outside_temp, charger_pilot_current, charge_current_request, battery_heater, carid) values (1, @Datum, @battery_level, @charge_energy_added, @charger_power, @ideal_battery_range_km, @charger_voltage, @charger_phases, @charger_actual_current, @outside_temp, @charger_pilot_current, @charge_current_request, @battery_heater, @carid)", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                cmd.Parameters.AddWithValue("@Datum", Date.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@battery_level", battery_level);
                cmd.Parameters.AddWithValue("@charge_energy_added", charge_energy_added);
                cmd.Parameters.AddWithValue("@charger_power", charger_power);
                cmd.Parameters.AddWithValue("@ideal_battery_range_km", kmRange.ToString(ciEnUS));
                cmd.Parameters.AddWithValue("@charger_voltage", int.Parse(charger_voltage));
                cmd.Parameters.AddWithValue("@charger_phases", charger_phases);
                cmd.Parameters.AddWithValue("@charger_actual_current", charger_actual_current);
                cmd.Parameters.AddWithValue("@battery_heater", "0");

                int i = 0;

                if (charger_pilot_current != null && int.TryParse(charger_pilot_current, out i))
                    cmd.Parameters.AddWithValue("@charger_pilot_current", i);
                else
                    cmd.Parameters.AddWithValue("@charger_pilot_current", DBNull.Value);

                if (charge_current_request != null && int.TryParse(charge_current_request, out i))
                    cmd.Parameters.AddWithValue("@charge_current_request", i);
                else
                    cmd.Parameters.AddWithValue("@charge_current_request", DBNull.Value);

                if (outside_temp == null)
                    cmd.Parameters.AddWithValue("@outside_temp", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@outside_temp", ((double)outside_temp).ToString(ciEnUS));

                cmd.ExecuteNonQuery();

                cmd = new MySqlCommand("SELECT LAST_INSERT_ID();", con);
                cmd.Parameters.Clear();
                currentChargeid = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void StartChargingState(DateTime date, string fast_charger_brand, string fast_charger_type, string conn_charge_cable, string fast_charger_present)
        {
            
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("insert chargingstate (import, StartDate, Pos, StartChargingID, fast_charger_brand, fast_charger_type, conn_charge_cable , fast_charger_present, carid ) values (1, @StartDate, @Pos, @StartChargingID, @fast_charger_brand, @fast_charger_type, @conn_charge_cable , @fast_charger_present, @carid)", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                cmd.Parameters.AddWithValue("@StartDate", date);
                cmd.Parameters.AddWithValue("@Pos", GetMaxPosid());
                cmd.Parameters.AddWithValue("@StartChargingID", GetMaxChargeid() + 1);
                cmd.Parameters.AddWithValue("@fast_charger_brand", fast_charger_brand);
                cmd.Parameters.AddWithValue("@fast_charger_type", fast_charger_type);
                cmd.Parameters.AddWithValue("@conn_charge_cable", conn_charge_cable);
                cmd.Parameters.AddWithValue("@fast_charger_present", fast_charger_present);
                cmd.ExecuteNonQuery();
            }
            
        }

        static int GetMaxChargeid()
        {
            return currentChargeid;
        }

        public static void StartState(string state, DateTime Date)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();

                MySqlCommand cmd1 = new MySqlCommand("select state from state where EndDate is null  and carid=@carid and id < " + currentStateId, con);
                cmd1.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                MySqlDataReader dr = cmd1.ExecuteReader();
                if (dr.Read())
                {
                    if (dr[0].ToString() == state)
                        return;
                }
                dr.Close();

                int MaxPosid = GetMaxPosid();
                CloseState(MaxPosid, Date);

                MySqlCommand cmd = new MySqlCommand("insert state (import, StartDate, state, StartPos, carid) values (1, @StartDate, @state, @StartPos, @carid)", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                cmd.Parameters.AddWithValue("@StartDate", Date);
                cmd.Parameters.AddWithValue("@state", state);
                cmd.Parameters.AddWithValue("@StartPos", MaxPosid);
                cmd.ExecuteNonQuery();

                cmd = new MySqlCommand("SELECT LAST_INSERT_ID();", con);
                cmd.Parameters.Clear();
                currentStateId = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public static void CloseState(int maxPosid, DateTime Date)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("update state set EndDate = @enddate, EndPos = @EndPos where EndDate is null  and carid=@carid", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                cmd.Parameters.AddWithValue("@enddate", Date);
                cmd.Parameters.AddWithValue("@EndPos", maxPosid);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
