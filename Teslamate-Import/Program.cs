using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Teslamate_Import
{
    class Program
    {
        static string pgConnectionString = Settings1.Default.TeslamateDB;
        static string DBConnectionstring = Settings1.Default.TeslaloggerDB;
        public static System.Globalization.CultureInfo ciEnUS = new System.Globalization.CultureInfo("en-US");
        static DateTime firstTeslaloggerData;

        static void Main(string[] args)
        {
            Tools.Log(0, "***** Teslamate Import " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " Started *****");
            try
            {
                Tools.Log(0,"Teslamate DB:" + pgConnectionString);
                Tools.Log(0,"Teslalogger DB:" + DBConnectionstring);

                AlterTables();

                firstTeslaloggerData = GetFirstTeslaloggerData();
                Tools.Log(0, "First Teslalogger Data: " + firstTeslaloggerData.ToString());

                CopyPositions();
                CopyTrips();
                CopyCharging();
                CopyChargingStates();
                CopyCarVersion();
                CopyGeofence();
            }
            catch (Exception ex)
            {
                Tools.Log(0,ex.ToString());
            }

            Tools.Log(0, "***** Teslamate Import " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " Finish *****");
        }

        private static void CopyTrips()
        {
            ExecuteNonQuery(@"delete from drivestate where import = 3");

            int id = 0;

            using (var con = new NpgsqlConnection(pgConnectionString))
            {
                con.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand("select * from drives order by id", con))
                {
                    using (var conTL = new MySqlConnection(DBConnectionstring))
                    {
                        conTL.Open();
                        NpgsqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            try
                            {
                                id = (int)dr["id"];

                                using (var cmdTL = new MySqlCommand(@"INSERT INTO drivestate (StartDate, StartPos, EndDate, EndPos, outside_temp_avg, speed_max, power_max, power_min, power_avg, import, CarID) 
                                    VALUES(@StartDate, @StartPos, @EndDate, @EndPos, @outside_temp_avg, @speed_max, @power_max, @power_min, @power_avg, 3, @CarID);", conTL))
                                {
                                    int carid = Convert.ToInt32(dr["Car_ID"]);

                                    DateTime Date = (DateTime)dr["start_date"];
                                    if (Date >= firstTeslaloggerData)
                                    {
                                        Tools.Log(id, "First Teslalogger Data reached. Import skipped!");
                                        break;
                                    }

                                    cmdTL.Parameters.AddWithValue("@StartDate", dr["start_date"]);
                                    cmdTL.Parameters.AddWithValue("@EndDate", dr["end_date"]);
                                    cmdTL.Parameters.AddWithValue("@outside_temp_avg", dr["outside_temp_avg"]);
                                    cmdTL.Parameters.AddWithValue("@speed_max", dr["speed_max"]);
                                    cmdTL.Parameters.AddWithValue("@power_max", dr["power_max"]);
                                    cmdTL.Parameters.AddWithValue("@power_min", dr["power_min"]);

                                    int? StartPosId = GetPosId(dr["start_date"] as DateTime?, carid);
                                    int? EndPosId = GetPosId(dr["end_date"] as DateTime?, carid);

                                    cmdTL.Parameters.AddWithValue("@StartPos", StartPosId);
                                    cmdTL.Parameters.AddWithValue("@EndPos", EndPosId);

                                    cmdTL.Parameters.AddWithValue("@power_avg", GetPowerAVGfromPos(StartPosId, EndPosId));

                                    cmdTL.Parameters.AddWithValue("@CarID", carid);
                                    cmdTL.ExecuteNonQuery();
                                }

                                if (id % 10 == 0)
                                {
                                    Tools.Log(id, "Drivestate " + dr["start_date"].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Tools.Log(id, "Drivestate " + ex.ToString());
                            }
                        }
                    }
                }
            }
        }

        private static void CopyCarVersion()
        {
            ExecuteNonQuery(@"delete from car_version where import = 3");

            int id = 0;

            using (var con = new NpgsqlConnection(pgConnectionString))
            {
                con.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand("select * from updates order by id", con))
                {
                    using (var conTL = new MySqlConnection(DBConnectionstring))
                    {
                        conTL.Open();
                        NpgsqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            try
                            {
                                id = (int)dr["id"];

                                DateTime Date = (DateTime)dr["start_date"];
                                if (Date >= firstTeslaloggerData)
                                {
                                    Tools.Log(id, "First Teslalogger Data reached. Import skipped!");
                                    break;
                                }

                                using (var cmdTL = new MySqlCommand(@"INSERT INTO car_version (StartDate, version, import, CarID) 
                                VALUES(@StartDate, @version, 3, @CarID);", conTL))
                                {
                                    cmdTL.Parameters.AddWithValue("@StartDate", dr["start_date"]);
                                    cmdTL.Parameters.AddWithValue("@version", dr["version"]);
                                    cmdTL.Parameters.AddWithValue("@CarID", dr["car_id"]);
                                    cmdTL.ExecuteNonQuery();
                                }

                                if (id % 2 == 0)
                                {
                                    Tools.Log(id, "car_version " + dr["start_date"].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Tools.Log(id, "CarVersion " + ex.ToString());
                            }
                        }
                    }
                }
            }
        }

        private static decimal GetPowerAVGfromPos(int? startPosId, int? endPosId)
        {
            using (var conTL = new MySqlConnection(DBConnectionstring))
            {
                conTL.Open();

                using (var cmd = new MySqlCommand("Select avg(power) from pos where id between @start and @end", conTL))
                {
                    cmd.Parameters.AddWithValue("@start", startPosId);
                    cmd.Parameters.AddWithValue("@end", endPosId);
                    decimal o = cmd.ExecuteScalar() as decimal? ?? 0;

                    return o;
                }
            }
        }

        private static void CopyChargingStates()
        {
            ExecuteNonQuery(@"delete from chargingstate where import = 3");

            int id = 0;

            using (var con = new NpgsqlConnection(pgConnectionString))
            {
                con.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand("select * from charging_processes order by id", con))
                {
                    using (var conTL = new MySqlConnection(DBConnectionstring))
                    {
                        conTL.Open();
                        NpgsqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            try
                            {
                                id = (int)dr["id"];

                                using (var cmdTL = new MySqlCommand(@"INSERT INTO chargingstate (StartDate, EndDate, UnplugDate, Pos, charge_energy_added, StartChargingID, EndChargingID, conn_charge_cable, fast_charger_brand, fast_charger_type, fast_charger_present, import, max_charger_power, cost_total, cost_per_session, CarID) 
                                VALUES(@StartDate, @EndDate, @UnplugDate, @Pos, @charge_energy_added, @StartChargingID, @EndChargingID, @conn_charge_cable, @fast_charger_brand, @fast_charger_type, @fast_charger_present, 3, @max_charger_power, @cost_total, @cost_per_session, @CarID);", conTL))
                                {
                                    int carid = Convert.ToInt32(dr["Car_ID"]);

                                    DateTime Date = (DateTime)dr["start_date"];
                                    if (Date >= firstTeslaloggerData)
                                    {
                                        Tools.Log(id, "First Teslalogger Data reached. Import skipped!");
                                        break;
                                    }

                                    cmdTL.Parameters.AddWithValue("@StartDate", dr["start_date"]);
                                    cmdTL.Parameters.AddWithValue("@EndDate", dr["end_date"]);
                                    cmdTL.Parameters.AddWithValue("@UnplugDate", DBNull.Value);

                                    cmdTL.Parameters.AddWithValue("@Pos", GetPosId(dr["start_date"] as DateTime?, carid));

                                    cmdTL.Parameters.AddWithValue("@charge_energy_added", DBNull.Value);

                                    int StartId = Convert.ToInt32(GetChargingIdStart(dr["start_date"] as DateTime?, carid));
                                    int EndId = Convert.ToInt32(GetChargingIdEnd(dr["end_date"] as DateTime?, carid));

                                    cmdTL.Parameters.AddWithValue("@StartChargingID", StartId);
                                    cmdTL.Parameters.AddWithValue("@EndChargingID", EndId);

                                    cmdTL.Parameters.AddWithValue("@conn_charge_cable", GetChargingField("conn_charge_cable", dr["start_date"] as DateTime?, carid));
                                    cmdTL.Parameters.AddWithValue("@fast_charger_brand", GetChargingField("fast_charger_brand", dr["start_date"] as DateTime?, carid));
                                    cmdTL.Parameters.AddWithValue("@fast_charger_type", GetChargingField("fast_charger_type", dr["start_date"] as DateTime?, carid));
                                    cmdTL.Parameters.AddWithValue("@fast_charger_present", GetChargingField("fast_charger_present", dr["start_date"] as DateTime?, carid));

                                    cmdTL.Parameters.AddWithValue("@max_charger_power", GetMaxChargerPower(StartId, EndId));

                                    cmdTL.Parameters.AddWithValue("@cost_total", dr["cost"]);
                                    cmdTL.Parameters.AddWithValue("@cost_per_session", dr["cost"]);

                                    cmdTL.Parameters.AddWithValue("@CarID", dr["Car_ID"]);
                                    cmdTL.ExecuteNonQuery();
                                }

                                if (id % 25 == 0)
                                {
                                    Tools.Log(id, "chargingstate " + dr["start_date"].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Tools.Log(id, "chargingstate " + ex.ToString());
                            }
                        }
                    }
                }
            }
        }

        private static double GetMaxChargerPower(int startId, int endId)
        {
            using (var conTL = new MySqlConnection(DBConnectionstring))
            {
                conTL.Open();

                using (var cmd = new MySqlCommand("Select max(charger_power) from charging where id between @start and @end", conTL))
                {
                    cmd.Parameters.AddWithValue("@start", startId);
                    cmd.Parameters.AddWithValue("@end", endId);
                    double o = cmd.ExecuteScalar() as double? ?? 0;

                    return o;
                }
            }
        }

        private static object GetChargingField(string column, DateTime? date, int carid)
        {
            if (date == null)
                return null;

            using (var con = new NpgsqlConnection(pgConnectionString))
            {
                con.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand($"select {column} from charges join charging_processes on charges.charging_process_id=charging_processes.id where date >= @datetime and car_id = @carid limit 1", con))
                {
                    cmd.Parameters.AddWithValue("@datetime", date);
                    cmd.Parameters.AddWithValue("@carid", carid);
                    object o = cmd.ExecuteScalar();

                    return o;
                }
            }
        }

        private static object GetChargingIdStart(DateTime? date, int carid)
        {
            if (date == null)
                return null;

            using (var conTL = new MySqlConnection(DBConnectionstring))
            {
                conTL.Open();

                using (var cmd = new MySqlCommand("Select id from charging where Datum >= @datetime and carid=@carid limit 1", conTL))
                {
                    cmd.Parameters.AddWithValue("@datetime", date);
                    cmd.Parameters.AddWithValue("@carid", carid);
                    int? id = cmd.ExecuteScalar() as int?;

                    return id;
                }
            }
        }

        private static object GetChargingIdEnd(DateTime? date, int carid)
        {
            if (date == null)
                return null;

            using (var conTL = new MySqlConnection(DBConnectionstring))
            {
                conTL.Open();

                using (var cmd = new MySqlCommand("Select id from charging where Datum <= @datetime and carid=@carid order by id desc limit 1", conTL))
                {
                    cmd.Parameters.AddWithValue("@datetime", date);
                    cmd.Parameters.AddWithValue("@carid", carid);
                    int? id = cmd.ExecuteScalar() as int?;

                    return id;
                }
            }
        }


        private static int? GetPosId(DateTime? datetime, int carid)
        {
            if (datetime == null)
                return null;

            using (var conTL = new MySqlConnection(DBConnectionstring))
            {
                conTL.Open();

                using (var cmd = new MySqlCommand("Select id from pos where Datum >= @datetime and carid=@carid limit 1", conTL))
                {
                    cmd.Parameters.AddWithValue("@datetime", datetime);
                    cmd.Parameters.AddWithValue("@carid", carid);
                    int? id = cmd.ExecuteScalar() as int?;

                    return id;
                }
            }
        }

        private static void CopyCharging()
        {
            int id = 0;

            ExecuteNonQuery(@"delete from charging where import = 3");

            using (var con = new NpgsqlConnection(pgConnectionString))
            {
                con.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand("select charges.*, car_id from charges join charging_processes on charges.charging_process_id = charging_processes.id order by id", con))
                {
                    using (var conTL = new MySqlConnection(DBConnectionstring))
                    {
                        conTL.Open();

                        NpgsqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            try
                            {
                                id = (int)dr["id"];

                                DateTime Date = (DateTime)dr["date"];
                                if (Date >= firstTeslaloggerData)
                                {
                                    Tools.Log(id, "First Teslalogger Data reached. Import skipped!");
                                    break;
                                }


                                using (var cmdTL = new MySqlCommand(@"INSERT INTO charging (battery_level, charge_energy_added, charger_power, Datum, ideal_battery_range_km, charger_voltage, charger_phases, charger_actual_current, outside_temp, charger_pilot_current, charge_current_request, battery_heater, import, battery_range_km, CarID) 
                                    VALUES(@battery_level, @charge_energy_added, @charger_power, @Datum, @ideal_battery_range_km, @charger_voltage, @charger_phases, @charger_actual_current, @outside_temp, @charger_pilot_current, @charge_current_request, @battery_heater, 3, @battery_range_km, @CarID);", conTL))
                                {
                                    cmdTL.Parameters.AddWithValue("@battery_level", dr["battery_level"]);
                                    cmdTL.Parameters.AddWithValue("@charge_energy_added", dr["charge_energy_added"]);
                                    cmdTL.Parameters.AddWithValue("@charger_power", dr["charger_power"]);
                                    cmdTL.Parameters.AddWithValue("@Datum", dr["date"]);
                                    cmdTL.Parameters.AddWithValue("@ideal_battery_range_km", dr["ideal_battery_range_km"]);
                                    cmdTL.Parameters.AddWithValue("@charger_voltage", dr["charger_voltage"]);
                                    cmdTL.Parameters.AddWithValue("@charger_phases", dr["charger_phases"]);
                                    cmdTL.Parameters.AddWithValue("@charger_actual_current", dr["charger_actual_current"]);
                                    cmdTL.Parameters.AddWithValue("@outside_temp", dr["outside_temp"]);
                                    cmdTL.Parameters.AddWithValue("@charger_pilot_current", dr["charger_pilot_current"]);
                                    cmdTL.Parameters.AddWithValue("@charge_current_request", DBNull.Value);
                                    cmdTL.Parameters.AddWithValue("@inside_temp", DBNull.Value);
                                    cmdTL.Parameters.AddWithValue("@battery_heater", dr["battery_heater"]);
                                    cmdTL.Parameters.AddWithValue("@battery_range_km", dr["rated_battery_range_km"]);
                                    cmdTL.Parameters.AddWithValue("@CarID", dr["Car_ID"]);
                                    cmdTL.ExecuteNonQuery();
                                }

                                if (id % 100 == 0)
                                {
                                    Tools.Log(id, "Charging " + dr["Date"].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Tools.Log(id, ex.ToString());
                            }
                        }
                    }
                }
            }
        }

        private static void CopyPositions()
        {
            ExecuteNonQuery(@"delete from pos where import = 3");

            int id = 0;

            using (var con = new NpgsqlConnection(pgConnectionString))
            {
                con.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand("select * from positions order by car_id, id", con))
                {
                    using (var conTL = new MySqlConnection(DBConnectionstring))
                    {
                        conTL.Open();

                        decimal lastIdeal_battery_range_km = 0;

                        NpgsqlDataReader dr = cmd.ExecuteReader();
                        while (dr.Read())
                        {
                            try
                            {
                                id = (int)dr["id"];

                                DateTime Date = (DateTime)dr["Date"];
                                if (Date >= firstTeslaloggerData)
                                {
                                    Tools.Log(id, "First Teslalogger Data reached. Import skipped!");
                                    break;
                                }

                                lastIdeal_battery_range_km = dr["ideal_battery_range_km"] as decimal? ?? lastIdeal_battery_range_km;

                                using (var cmdTL = new MySqlCommand(@"INSERT INTO pos (Datum, lat, lng, speed, power, odometer, ideal_battery_range_km, outside_temp, altitude, battery_level, inside_temp, battery_heater, import, battery_range_km, CarID, address)
                                VALUES(@Datum, @lat, @lng, @speed, @power, @odometer, @ideal_battery_range_km, @outside_temp, @altitude, @battery_level, @inside_temp, @battery_heater, @import, @battery_range_km, @CarID, '');", conTL))
                                {
                                    cmdTL.Parameters.AddWithValue("@Datum", dr["Date"]);
                                    cmdTL.Parameters.AddWithValue("@lat", dr["latitude"]);
                                    cmdTL.Parameters.AddWithValue("@lng", dr["longitude"]);
                                    cmdTL.Parameters.AddWithValue("@speed", dr["speed"]);
                                    cmdTL.Parameters.AddWithValue("@power", dr["power"]);
                                    cmdTL.Parameters.AddWithValue("@odometer", dr["odometer"]);
                                    cmdTL.Parameters.AddWithValue("@ideal_battery_range_km", lastIdeal_battery_range_km);
                                    cmdTL.Parameters.AddWithValue("@battery_level", dr["battery_level"]);
                                    cmdTL.Parameters.AddWithValue("@outside_temp", dr["outside_temp"]);
                                    cmdTL.Parameters.AddWithValue("@altitude", dr["elevation"]);
                                    cmdTL.Parameters.AddWithValue("@inside_temp", dr["inside_temp"]);
                                    cmdTL.Parameters.AddWithValue("@battery_heater", dr["battery_heater"]);
                                    cmdTL.Parameters.AddWithValue("@import", 3);
                                    cmdTL.Parameters.AddWithValue("@battery_range_km", dr["rated_battery_range_km"]);
                                    cmdTL.Parameters.AddWithValue("@CarID", dr["Car_ID"]);
                                    cmdTL.ExecuteNonQuery();
                                }

                                if (id % 1000 == 0)
                                {
                                    Tools.Log(id, "Pos " + dr["Date"].ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Tools.Log(id, ex.ToString());
                            }
                        }
                    }
                }
            }
        }

        private static void CopyGeofence()
        {
            int i = 0;

            string geofence = "";
            if (File.Exists("geofence-private.csv"))
                geofence = File.ReadAllText("geofence-private.csv");

            using (var con = new NpgsqlConnection(pgConnectionString))
            {
                con.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand("select * from geofences", con))
                {
                    NpgsqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        i++;

                        String name = dr["name"].ToString().Replace(",", " ").Trim();
                        String lat = ((decimal)dr["latitude"]).ToString(ciEnUS);
                        String lng = ((decimal)dr["longitude"]).ToString(ciEnUS);
                        String radius = dr["radius"].ToString();

                        bool found = false;

                        string[] lines = geofence.Split('\n');

                        foreach (string line in lines)
                        {
                            string[] a = line.Split(',');

                            if (a.Length > 3)
                            {
                                if (a[0].Trim() == name && a[1].Trim() == lat && a[2].Trim() == lng && a[3].Trim() == radius)
                                {
                                    found = true;
                                    break;
                                }
                            }
                        }

                        if (!found)
                            geofence += $"\n{name},{lat},{lng},{radius}";

                        if (i % 2 == 0)
                        {
                            Tools.Log(i, "Geofence: " + dr["name"].ToString());
                        }
                    }

                    File.WriteAllText("geofence-private.csv", geofence, Encoding.UTF8);
                }
            }
        }

        private static void ExecuteNonQuery(string sql)
        {
            using (var conTL = new MySqlConnection(DBConnectionstring))
            {
                conTL.Open();
                using (var cmdTL = new MySqlCommand(sql, conTL))
                {
                    cmdTL.CommandTimeout = 600;
                    cmdTL.ExecuteNonQuery();
                }
            }
        }

        private static DateTime GetFirstTeslaloggerData()
        {
            DateTime dtMin = DateTime.Now;

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT StartDate FROM drivestate where import is null order by id limit 1", con);
                var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    DateTime dtDrivestate = (DateTime)dr[0];
                    if (dtDrivestate < dtMin)
                        dtMin = dtDrivestate;
                }
                dr.Close();

                cmd = new MySqlCommand("SELECT StartDate FROM chargingstate where import is null order by id limit 1", con);
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

        private static void AlterTables()
        {
            String[] tables = new String[] { "car_version", "charging", "chargingstate", "drivestate", "pos", "state" };
            foreach (var table in tables)
            {
                try
                {
                    using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
                    {
                        con.Open();
                        MySqlCommand cmd = new MySqlCommand($"alter table {table} ADD column IF NOT EXISTS import TINYINT(1) NULL", con);
                        cmd.CommandTimeout = 300;
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Tools.Log(0, ex.ToString());
                }
            }
        }
    }
}
