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

        static void Main(string[] args)
        {
            DataTable dt = new DataTable();
            int dateColumnID = -1;
            dateColumnID = LoadData(dt);

            DateTime startDate = (DateTime)dt.Rows[0]["Date"];
            DateTime endDate = (DateTime)dt.Rows[dt.Rows.Count - 1]["Date"];

            DeleteData(startDate, endDate);

            Console.WriteLine("start Parsing");
            foreach (DataRow dr in dt.Rows)
            {
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
            }
        }

        public static DateTime UnixToDateTime(long t)
        {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddMilliseconds(t);
            dt = dt.ToLocalTime();
            return dt;

        }
    }
}
