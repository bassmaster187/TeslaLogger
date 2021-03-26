using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace KML_Import
{
    class Program
    {
        static string DBConnectionstring = "Server=127.0.0.1;Database=teslalogger;Uid=root;Password=teslalogger;";
        public static System.Globalization.CultureInfo ciEnUS = new System.Globalization.CultureInfo("en-US");
        private static int currentPosId;

        static void Main(string[] args)
        {
            Tools.Log(0, "***** Start KML Import " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " Started *****");

            if (Tools.IsDocker())
            {
                Tools.Log(0, "DOCKER Version!");
                DBConnectionstring = "Server=database;Database=teslalogger;Uid=root;Password=teslalogger;";
            }

            if (Settings1.Default.DBConnectionstring.Length > 0)
                DBConnectionstring = Settings1.Default.DBConnectionstring;

            Tools.Log(0, "DBConnectionstring: " + DBConnectionstring);

            LoadAllFiles();

        }

        private static void LoadAllFiles()
        {
            var files = System.IO.Directory.EnumerateFiles(".", "*.kml");

            if (files.Count() == 0)
            {
                Tools.Log(0, "No KML files found!");
            }

            foreach (var file in files)
                LoadData(file);
        }

        private static void LoadData(string file)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;

            using (XmlReader reader = XmlReader.Create(file, settings))
            {
                while (reader.Read())
                {
                    var nodetype = reader.NodeType;

                    switch (nodetype)
                    {
                        case XmlNodeType.Element:
                            {
                                if (reader.Name == "Placemark")
                                {
                                    ReadPlacemark(reader);
                                }
                            }
                            break;
                        default:
                            Tools.Log(0, "Unhandled: " + nodetype);
                            break;
                    }

                }
            }
        }

        private static void ReadPlacemark(XmlReader reader)
        {
            string name = "";
            string coordinates = "";
            string begin = "";
            string end = "";
            string description = "";


            while (reader.Read())
            {
                var nodetype = reader.NodeType;


                switch (nodetype)
                {
                    case XmlNodeType.Element:
                        {
                            System.Diagnostics.Debug.WriteLine("Element Name:" + reader.Name);
                            switch (reader.Name)
                            {
                                case "Placemark":
                                    if (name.Contains("Flying") || name.Contains("On a train"))
                                    {
                                        Tools.Log(0, "Ignore: " + name + " / Description: " + description);
                                    }
                                    else
                                        AddRouter(name, coordinates, begin, end);

                                    ReadPlacemark(reader);
                                    break;

                                case "name":
                                    name = reader.ReadInnerXml();
                                    break;

                                case "description":
                                    name = reader.ReadInnerXml();
                                    break;

                                case "end":
                                    end = reader.ReadInnerXml();
                                    break;

                                case "begin":
                                    begin = reader.ReadInnerXml();

                                    if (reader.Name == "end")
                                        end = reader.ReadInnerXml();

                                    break;

                                case "coordinates":
                                    coordinates = reader.ReadInnerXml();
                                    break;
                            }
                        }
                        break;
                    default:
                        Tools.Log(0, "Unhandled: " + nodetype);
                        break;
                }
            }
        }

        private static void AddRouter(string name, string coordinates, string begin, string end)
        {
            DateTime dtStart = DateTime.Parse(begin);
            DateTime dtEnd = DateTime.Parse(end);

            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();

                MySqlCommand cmd = new MySqlCommand("delete from pos where carid=@carid and Datum between @s and @e and import = 100", con);
                cmd.Parameters.AddWithValue("@carid", Settings1.Default.CarId);
                cmd.Parameters.AddWithValue("@s", dtStart);
                cmd.Parameters.AddWithValue("@e", dtEnd);
                int anz = cmd.ExecuteNonQuery();

                Tools.Log(0, "Deleted Pos: " + anz);
            }

            TimeSpan ts = dtEnd - dtStart;
            DateTime date = dtStart;

            string[] c = coordinates.Split(' ');
            double diffms = ts.TotalMilliseconds / c.Length;


            foreach(string line in c)
            {
                string[] a = line.Split(',');
                if (a.Length < 2)
                    continue;

                date = date.AddMilliseconds(diffms);
                double lat = double.Parse(a[1], ciEnUS);
                double lng = double.Parse(a[0], ciEnUS);

                InsertPos(date, lat, lng, 0, 0, 0, 0, 0, 0, "0", "0", "0", "0", "0");
            }
        }

        internal static void InsertPos(DateTime date, double latitude, double longitude, int speed, decimal power, double odometer, double ideal_battery_range_km, int battery_level, double? outside_temp, string altitude, string inside_temp, string battery_heater, string is_preconditioning, string sentry_mode)
        {
            using (MySqlConnection con = new MySqlConnection(DBConnectionstring))
            {
                con.Open();

                MySqlCommand cmd = new MySqlCommand("insert pos (import, Datum, lat, lng, speed, power, odometer, ideal_battery_range_km, outside_temp, altitude, battery_level, inside_temp, battery_heater, is_preconditioning, sentry_mode, carid) values (100, @Datum, @lat, @lng, @speed, @power, @odometer, @ideal_battery_range_km, @outside_temp, @altitude, @battery_level, @inside_temp, @battery_heater, @is_preconditioning, @sentry_mode, @carid )", con);
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
    }
}
