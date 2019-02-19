using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaLogger
{
    class DBHelper
    {
        public static CurrentJSON currentJSON = new CurrentJSON();

        public static bool current_is_preconditioning = false;

        public static string DBConnectionstring
        {
            get
            {
                if (String.IsNullOrEmpty(ApplicationSettings.Default.DBConnectionstring))
                    return "Server=localhost;Database=teslalogger;Uid=root;Password=teslalogger;";

                return ApplicationSettings.Default.DBConnectionstring;
            }
        }

        public static void CloseState()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("update state set EndDate = @enddate, EndPos = @EndPos where EndDate is null", con);
                cmd.Parameters.AddWithValue("@enddate", DateTime.Now);
                cmd.Parameters.AddWithValue("@EndPos", GetMaxPosid());
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
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();

                MySqlCommand cmd1 = new MySqlCommand("select state from state where EndDate is null", con);
                MySqlDataReader dr = cmd1.ExecuteReader();
                if (dr.Read())
                {
                    if (dr[0].ToString() == state)
                        return;
                }
                dr.Close();

                currentJSON.CreateCurrentJSON();

                CloseState();

                Tools.Log("state: " + state);

                MySqlCommand cmd = new MySqlCommand("insert state (StartDate, state, StartPos) values (@StartDate, @state, @StartPos)", con);
                cmd.Parameters.AddWithValue("@StartDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@state", state);
                cmd.Parameters.AddWithValue("@StartPos", GetMaxPosid());
                cmd.ExecuteNonQuery();
            }
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
            currentJSON.CreateCurrentJSON();
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
                        currentJSON.current_trip_km_start = Convert.ToDouble(dr["StartKm"]);
                        currentJSON.current_trip_km_end = Convert.ToDouble(dr["EndKm"]);
                        currentJSON.current_trip_max_speed = Convert.ToDouble(dr["speed_max"]);
                        currentJSON.current_trip_max_power = Convert.ToDouble(dr["power_max"]);
                        currentJSON.current_trip_start_range = Convert.ToDouble(dr["StartRange"]);
                        currentJSON.current_trip_end_range = Convert.ToDouble(dr["EndRange"]);
                        currentJSON.CreateCurrentJSON();
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
            }
        }

        public static void StartChargingState()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("insert chargingstate (StartDate, Pos, StartChargingID) values (@StartDate, @Pos, @StartChargingID)", con);
                cmd.Parameters.AddWithValue("@StartDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@Pos", GetMaxPosid());
                cmd.Parameters.AddWithValue("@StartChargingID", GetMaxChargeid() + 1);
                cmd.ExecuteNonQuery();
            }

            currentJSON.current_charging = true;
            currentJSON.CreateCurrentJSON();
        }

        public static void CloseDriveState()
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
                cmd.Parameters.AddWithValue("@EndDate", DateTime.Now);
                cmd.Parameters.AddWithValue("@Pos", MaxPosId);
                cmd.ExecuteNonQuery();
            }

            if (StartPos != 0)
                UpdateDriveStatistics(StartPos, MaxPosId);

            currentJSON.current_driving = false;
            currentJSON.current_speed = 0;
            currentJSON.current_power = 0;
            currentJSON.CreateCurrentJSON();
        }

        private static void UpdateDriveStatistics(int startPos, int endPos)
        {
            try
            {
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

                
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
            }
        }

        public static void UpdateAllDrivestateData()
        {
            Tools.Log("UpdateAllDrivestateData start");

            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
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

                        DBHelper.UpdateDriveStatistics(StartPos, EndPos);
                    }
                    catch (Exception ex)
                    {
                        Tools.Log(ex.ToString());
                    }
                }
            }

            Tools.Log("UpdateAllDrivestateData end");
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


        internal static void InsertPos(string timestamp, double latitude, double longitude, int speed, decimal power, double odometer, double ideal_battery_range_km, int battery_level, string address, double? outside_temp, string altitude)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                
                MySqlCommand cmd = new MySqlCommand("insert pos (Datum, lat, lng, speed, power, odometer, ideal_battery_range_km, address, outside_temp, altitude, battery_level) values (@Datum, @lat, @lng, @speed, @power, @odometer, @ideal_battery_range_km, @address, @outside_temp, @altitude, @battery_level)", con);
                cmd.Parameters.AddWithValue("@Datum", UnixToDateTime(long.Parse(timestamp)).ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@lat", latitude.ToString());
                cmd.Parameters.AddWithValue("@lng", longitude.ToString());
                cmd.Parameters.AddWithValue("@speed", (int)((decimal)speed * 1.60934M));
                cmd.Parameters.AddWithValue("@power", (int)(power * 1.35962M));
                cmd.Parameters.AddWithValue("@odometer", odometer.ToString());

                if (ideal_battery_range_km == -1)
                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", ideal_battery_range_km.ToString());

                cmd.Parameters.AddWithValue("@address", address);

                if (outside_temp == null)
                    cmd.Parameters.AddWithValue("@outside_temp", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@outside_temp", ((double)outside_temp).ToString());

                if (altitude.Length == 0)
                    cmd.Parameters.AddWithValue("@altitude", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@altitude", altitude);

                if (battery_level == -1)
                    cmd.Parameters.AddWithValue("@battery_level", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@battery_level", battery_level.ToString());

                cmd.ExecuteNonQuery();

                try
                {
                    currentJSON.current_speed = (int)((decimal)speed * 1.60934M);
                    currentJSON.current_power = (int)(power * 1.35962M);
                    currentJSON.current_odometer = odometer;
                    currentJSON.current_ideal_battery_range_km = ideal_battery_range_km;

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
                    Tools.Log(ex.ToString());
                }
            }

            currentJSON.CreateCurrentJSON();
        }

        static DateTime lastChargingInsert = DateTime.Today;


        internal static void InsertCharging(string timestamp, string battery_level, string charge_energy_added, string charger_power, double ideal_battery_range, string charger_voltage, string charger_phases, string charger_actual_current, double? outside_temp, bool forceinsert)
        {
            Tools.SetThread_enUS();

            if (charger_phases == "")
                charger_phases = "1";

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
                    MySqlCommand cmd = new MySqlCommand("insert charging (Datum, battery_level, charge_energy_added, charger_power, ideal_battery_range_km, charger_voltage, charger_phases, charger_actual_current, outside_temp) values (@Datum, @battery_level, @charge_energy_added, @charger_power, @ideal_battery_range_km, @charger_voltage, @charger_phases, @charger_actual_current, @outside_temp)", con);
                    cmd.Parameters.AddWithValue("@Datum", UnixToDateTime(long.Parse(timestamp)).ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@battery_level", battery_level);
                    cmd.Parameters.AddWithValue("@charge_energy_added", charge_energy_added);
                    cmd.Parameters.AddWithValue("@charger_power", charger_power);
                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", kmRange.ToString());
                    cmd.Parameters.AddWithValue("@charger_voltage", int.Parse(charger_voltage));
                    cmd.Parameters.AddWithValue("@charger_phases", charger_phases);
                    cmd.Parameters.AddWithValue("@charger_actual_current", charger_actual_current);

                    if (outside_temp == null)
                        cmd.Parameters.AddWithValue("@outside_temp", DBNull.Value);
                    else
                        cmd.Parameters.AddWithValue("@outside_temp", ((double)outside_temp).ToString());

                    cmd.ExecuteNonQuery();
                }
            }

            try
            {
                currentJSON.current_battery_level = Convert.ToInt32(battery_level);
                currentJSON.current_charge_energy_added = Convert.ToDouble(charge_energy_added);
                currentJSON.current_charger_power = Convert.ToInt32(charger_power);
                currentJSON.current_ideal_battery_range_km = kmRange;
                currentJSON.current_charger_voltage = int.Parse(charger_voltage);
                currentJSON.current_charger_phases = Convert.ToInt32(charger_phases);
                currentJSON.current_charger_actual_current = Convert.ToInt32(charger_actual_current);
                currentJSON.CreateCurrentJSON();
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
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
                    return Convert.ToInt32(dr[0]);
            }

            return 0;
        }

        public static int GetMaxPosid()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("Select max(id) from pos", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read() && dr[0] != DBNull.Value)
                    return Convert.ToInt32(dr[0]);
            }

            return 0;
        }

        static int GetMaxChargeid()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("Select max(id) from charging", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read() && dr[0] != DBNull.Value)
                    return Convert.ToInt32(dr[0]);
            }

            return 0;
        }

        public static string GetVersion()
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT @@version", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                    return dr[0].ToString();
            }

            return "NULL";
        }

        

        public static bool ColumnExists(string table, string column)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SHOW COLUMNS FROM `"+ table +"` LIKE '"+ column +"';", con);
                MySqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                    return true;
            }

            return false;
        }

        public static int ExecuteSQLQuery(string sql)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand(sql, con);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Tools.ExceptionWriter(ex, sql);
                throw;
            }
        }
    }
}
