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
        static int currentPosId = 0;

        static void Main(string[] args)
        {
            DataTable dt = new DataTable();
            int dateColumnID = -1;
            dateColumnID = LoadData(dt);

            DateTime startDate = (DateTime)dt.Rows[0]["Date"];
            DateTime endDate = (DateTime)dt.Rows[dt.Rows.Count - 1]["Date"];

            DeleteData(startDate, endDate);

            Console.WriteLine("start Parsing");

            string oldShiftstate = "P";

            foreach (DataRow dr in dt.Rows)
            {
                DateTime Date = (DateTime)dr["Date"];

                string newShiftstate = dr["shift_state"].ToString();
                if (oldShiftstate == "P" && (newShiftstate == "D" || newShiftstate =="R"))
                {
                    // Driving
                    Console.WriteLine("Start Driving " + Date.ToString());
                    oldShiftstate = newShiftstate;
                    StartDriveState(Date);

                }
                else if (newShiftstate == "P" && (oldShiftstate == "D" || oldShiftstate == "R"))
                {
                    // End of Driving
                    Console.WriteLine("End Driving " + Date.ToString());
                    oldShiftstate = newShiftstate;
                    CloseDriveState(Date);
                }
                
                InsertPos(dr);
            }

            Console.WriteLine("end Parsing");
        }

        private static void InsertPos(DataRow dr)
        {
            if (dr["latitude"] == DBNull.Value || dr["latitude"].ToString().Length == 0)
                return;

            if (dr["speed"] == DBNull.Value || dr["speed"].ToString().Length == 0)
                return;

            DateTime Date = (DateTime)dr["Date"];

            double latitude = Convert.ToDouble(dr["latitude"], ciEnUS);
            double longitude = Convert.ToDouble(dr["longitude"], ciEnUS);
            int speed = (int)(Convert.ToDecimal(dr["speed"]) * 1.60934M);
            int power = (int)(Convert.ToDecimal(dr["power"]) * 1.35962M);
            double odometerKM = (Double)(Convert.ToDecimal(dr["odometer"], ciEnUS) / 0.62137M);

            double ideal_battery_range = 0;
            if (Convert.ToDecimal(dr["ideal_battery_range"]) == 999) // Raven
                ideal_battery_range = Convert.ToDouble(dr["battery_range"]);

            ideal_battery_range = ideal_battery_range / (double)0.62137;

            int battery_level = Convert.ToInt32(dr["battery_level"]);

            string elevation = dr["elevation"].ToString();
            string inside_temp = dr["inside_temp"].ToString();

            // todo
            double ? outside_temp = null;

            InsertPos(Date, latitude, longitude, speed, power, odometerKM, ideal_battery_range, battery_level, outside_temp, elevation, inside_temp, "0", "0","0");
        }

        private static void DeleteData(DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("Delete Data in timerange of the File");

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("delete from car_version where StartDate >= @s and StartDate <= @e", con);
                cmd.Parameters.AddWithValue("@s", startDate);
                cmd.Parameters.AddWithValue("@e", endDate);
                cmd.CommandTimeout = 300;
                cmd.ExecuteNonQuery();
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("delete from charging where Datum >= @s and Datum <= @e", con);
                cmd.Parameters.AddWithValue("@s", startDate);
                cmd.Parameters.AddWithValue("@e", endDate);
                cmd.CommandTimeout = 300;
                cmd.ExecuteNonQuery();
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("delete from chargingstate where StartDate >= @s and StartDate <= @e", con);
                cmd.Parameters.AddWithValue("@s", startDate);
                cmd.Parameters.AddWithValue("@e", endDate);
                cmd.CommandTimeout = 300;
                cmd.ExecuteNonQuery();
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("delete from drivestate where StartDate >= @s and StartDate <= @e", con);
                cmd.Parameters.AddWithValue("@s", startDate);
                cmd.Parameters.AddWithValue("@e", endDate);
                cmd.CommandTimeout = 300;
                cmd.ExecuteNonQuery();
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("delete from pos where Datum >= @s and Datum <= @e", con);
                cmd.Parameters.AddWithValue("@s", startDate);
                cmd.Parameters.AddWithValue("@e", endDate);
                cmd.CommandTimeout = 300;
                cmd.ExecuteNonQuery();
            }

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("delete from state where StartDate >= @s and StartDate <= @e", con);
                cmd.Parameters.AddWithValue("@s", startDate);
                cmd.Parameters.AddWithValue("@e", endDate);
                cmd.CommandTimeout = 300;
                cmd.ExecuteNonQuery();
            }
        }

        private static int LoadData(DataTable dt)
        {
            Console.WriteLine("Load csv File");
            int dateColumnID = -1;

            string[] lines = System.IO.File.ReadAllLines("TeslaFi.csv");
            string[] columns = lines[0].Split(',');

            Console.WriteLine("Write into DataTable");
            foreach (string column in columns)
            {
                if (column == "Date")
                {
                    dateColumnID = dt.Columns.Add(column, typeof(DateTime)).Ordinal;
                }
                else
                    dt.Columns.Add(column);
            }

            for (int x = 1; x < lines.Length; x++)
            {
                DataRow dr = dt.NewRow();
                columns = lines[x].Split(',');
                for (int y = 0; y < columns.Length; y++)
                {
                    if (y == dateColumnID)
                        dr[y] = DateTime.Parse(columns[y].Replace("\"", ""));
                    else
                        dr[y] = columns[y];
                }
                dt.Rows.Add(dr);
            }
            dt.AcceptChanges();

            Console.WriteLine("end");
            return dateColumnID;
        }

        internal static void InsertPos(DateTime date, double latitude, double longitude, int speed, decimal power, double odometer, double ideal_battery_range_km, int battery_level, double? outside_temp, string altitude, string inside_temp, string battery_heater, string is_preconditioning, string sentry_mode)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();

                MySqlCommand cmd = new MySqlCommand("insert pos (Datum, lat, lng, speed, power, odometer, ideal_battery_range_km, outside_temp, altitude, battery_level, inside_temp, battery_heater, is_preconditioning, sentry_mode) values (@Datum, @lat, @lng, @speed, @power, @odometer, @ideal_battery_range_km, @outside_temp, @altitude, @battery_level, @inside_temp, @battery_heater, @is_preconditioning, @sentry_mode )", con);
                cmd.Parameters.AddWithValue("@Datum", date.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@lat", latitude.ToString(ciEnUS));
                cmd.Parameters.AddWithValue("@lng", longitude.ToString(ciEnUS));
                cmd.Parameters.AddWithValue("@speed", (int)((decimal)speed * 1.60934M));
                cmd.Parameters.AddWithValue("@power", (int)(power * 1.35962M));
                cmd.Parameters.AddWithValue("@odometer", odometer.ToString(ciEnUS));

                if (ideal_battery_range_km == -1)
                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", DBNull.Value);
                else
                    cmd.Parameters.AddWithValue("@ideal_battery_range_km", ideal_battery_range_km.ToString());

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
                MySqlCommand cmd = new MySqlCommand("insert drivestate (StartDate, StartPos) values (@StartDate, @Pos)", con);
                cmd.Parameters.AddWithValue("@StartDate", date);
                cmd.Parameters.AddWithValue("@Pos", GetMaxPosid());
                cmd.ExecuteNonQuery();
            }
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
                UpdateDriveStatistics(StartPos, MaxPosId);
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
    }
}
