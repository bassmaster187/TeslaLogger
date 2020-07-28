using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Reflection;

namespace TeslaLogger
{
    internal class UpdateTeslalogger
    {
        private static readonly string cmd_restart_path = "/tmp/teslalogger-cmd-restart.txt";
        private static bool shareDataOnStartup = false;
        private static System.Threading.Timer timer;

        private static DateTime lastVersionCheck = DateTime.UtcNow;

        public static void Start(WebHelper wh)
        {
            try
            {
                shareDataOnStartup = Tools.IsShareData();

                if (!DBHelper.ColumnExists("pos", "battery_level"))
                {
                    Logfile.Log("ALTER TABLE pos ADD COLUMN battery_level DOUBLE NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN battery_level DOUBLE NULL");
                }

                if (!DBHelper.ColumnExists("drivestate", "outside_temp_avg"))
                {
                    Logfile.Log("ALTER TABLE drivestate ADD COLUMN outside_temp_avg DOUBLE NULL, ADD COLUMN speed_max INT NULL, ADD COLUMN power_max INT NULL, ADD COLUMN power_min INT NULL, ADD COLUMN power_avg DOUBLE NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE drivestate ADD COLUMN outside_temp_avg DOUBLE NULL, ADD COLUMN speed_max INT NULL, ADD COLUMN power_max INT NULL, ADD COLUMN power_min INT NULL, ADD COLUMN power_avg DOUBLE NULL");

                    DBHelper.UpdateAllDrivestateData();
                }

                if (!DBHelper.ColumnExists("charging", "charger_pilot_current"))
                {
                    Logfile.Log("ALTER TABLE charging ADD COLUMN charger_pilot_current INT NULL, ADD COLUMN charge_current_request INT NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE charging ADD COLUMN charger_pilot_current INT NULL, ADD COLUMN charge_current_request INT NULL");
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.TableExists("car_version"))
                {
                    Logfile.Log("CREATE TABLE car_version (id int NOT NULL AUTO_INCREMENT, StartDate datetime NOT NULL, version varchar(50), PRIMARY KEY(id))");
                    DBHelper.ExecuteSQLQuery("CREATE TABLE car_version (id int NOT NULL AUTO_INCREMENT, StartDate datetime NOT NULL, version varchar(50), PRIMARY KEY(id))");
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.TableExists("can"))
                {
                    Logfile.Log("CREATE TABLE `can` (`datum` datetime NOT NULL, `id` mediumint NOT NULL, `val` double DEFAULT NULL, PRIMARY KEY(`datum`,`id`) ) ENGINE = InnoDB DEFAULT CHARSET = latin1;");
                    DBHelper.ExecuteSQLQuery("CREATE TABLE `can` (`datum` datetime NOT NULL, `id` mediumint NOT NULL, `val` double DEFAULT NULL, PRIMARY KEY(`datum`,`id`) ) ENGINE = InnoDB DEFAULT CHARSET = latin1;");
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("pos", "inside_temp"))
                {
                    Logfile.Log("ALTER TABLE pos ADD COLUMN inside_temp DOUBLE NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN inside_temp DOUBLE NULL", 300);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("pos", "battery_heater"))
                {
                    Logfile.Log("ALTER TABLE pos ADD COLUMN battery_heater TINYINT(1) NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN battery_heater TINYINT(1) NULL", 300);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("pos", "is_preconditioning"))
                {
                    Logfile.Log("ALTER TABLE pos ADD COLUMN is_preconditioning TINYINT(1) NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN is_preconditioning TINYINT(1) NULL", 300);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("pos", "sentry_mode"))
                {
                    Logfile.Log("ALTER TABLE pos ADD COLUMN sentry_mode TINYINT(1) NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN sentry_mode TINYINT(1) NULL", 300);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("chargingstate", "conn_charge_cable"))
                {
                    Logfile.Log("ALTER TABLE chargingstate ADD COLUMN conn_charge_cable varchar(50)");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE chargingstate ADD COLUMN conn_charge_cable varchar(50)", 300);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("chargingstate", "fast_charger_brand"))
                {
                    Logfile.Log("ALTER TABLE chargingstate ADD COLUMN fast_charger_brand varchar(50)");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE chargingstate ADD COLUMN fast_charger_brand varchar(50)", 300);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("chargingstate", "fast_charger_type"))
                {
                    Logfile.Log("ALTER TABLE chargingstate ADD COLUMN fast_charger_type varchar(50)");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE chargingstate ADD COLUMN fast_charger_type varchar(50)", 300);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("chargingstate", "fast_charger_present"))
                {
                    Logfile.Log("ALTER TABLE chargingstate ADD COLUMN fast_charger_present TINYINT(1)");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE chargingstate ADD COLUMN fast_charger_present TINYINT(1)", 300);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("charging", "battery_heater"))
                {
                    Logfile.Log("ALTER TABLE charging ADD COLUMN battery_heater TINYINT(1) NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE charging ADD COLUMN battery_heater TINYINT(1) NULL", 600);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("chargingstate", "max_charger_power"))
                {
                    Logfile.Log("ALTER TABLE chargingstate ADD COLUMN max_charger_power int NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE chargingstate ADD COLUMN max_charger_power int NULL", 600);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("trip", "outside_temp_avg"))
                {
                    UpdateDBView(wh);
                }

                if (!DBHelper.TableExists("mothership"))
                {
                    Logfile.Log("CREATE TABLE mothership (id int NOT NULL AUTO_INCREMENT, ts datetime NOT NULL, commandid int NOT NULL, duration DOUBLE NULL, PRIMARY KEY(id))");
                    DBHelper.ExecuteSQLQuery("CREATE TABLE mothership (id int NOT NULL AUTO_INCREMENT, ts datetime NOT NULL, commandid int NOT NULL, duration DOUBLE NULL, PRIMARY KEY(id))");
                    Logfile.Log("CREATE TABLE OK");
                }
                if (!DBHelper.TableExists("mothershipcommands"))
                {
                    Logfile.Log("CREATE TABLE mothershipcommands (id int NOT NULL AUTO_INCREMENT, command varchar(50) NOT NULL, PRIMARY KEY(id))");
                    DBHelper.ExecuteSQLQuery("CREATE TABLE mothershipcommands (id int NOT NULL AUTO_INCREMENT, command varchar(50) NOT NULL, PRIMARY KEY(id))");
                    Logfile.Log("CREATE TABLE OK");
                }
                if (!DBHelper.ColumnExists("mothership", "httpcode"))
                {
                    Logfile.Log("ALTER TABLE mothership ADD COLUMN httpcode int NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE mothership ADD COLUMN httpcode int NULL", 600);
                    Logfile.Log("ALTER TABLE OK");
                }
                if (!DBHelper.TableExists("httpcodes"))
                {
                    Logfile.Log("CREATE TABLE httpcodes (id int NOT NULL, text varchar(50) NOT NULL, PRIMARY KEY(id))");
                    DBHelper.ExecuteSQLQuery("CREATE TABLE httpcodes (id int NOT NULL, text varchar(50) NOT NULL, PRIMARY KEY(id))");
                    Logfile.Log("CREATE TABLE OK");
                }

                if (!DBHelper.IndexExists("can_ix","can"))
                {
                    Logfile.Log("alter table can add index can_ix (id,datum)");
                    DBHelper.ExecuteSQLQuery("alter table can add index can_ix (id,datum)", 600);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("pos", "battery_range_km"))
                {
                    Logfile.Log("ALTER TABLE pos ADD COLUMN battery_range_km DOUBLE NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN battery_range_km DOUBLE NULL", 600);
                }

                if (!DBHelper.ColumnExists("charging", "battery_range_km"))
                {
                    Logfile.Log("ALTER TABLE charging ADD COLUMN battery_range_km DOUBLE NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE charging ADD COLUMN battery_range_km DOUBLE NULL", 600);
                }


                DBHelper.EnableMothership();

                CheckDBCharset();

                DBHelper.UpdateHTTPStatusCodes();

                timer = new System.Threading.Timer(FileChecker, wh, 10000, 5000);

                Chmod("/var/www/html/admin/wallpapers", 777);

                UpdatePHPini();

                try
                {
                    // create empty weather.ini file
                    string filepath = Path.Combine(FileManager.GetExecutingPath(), "weather.ini");
                    if (!File.Exists(filepath))
                    {
                        File.WriteAllText(filepath, "city = \"Berlin, de\"\r\nappid = \"12345678901234567890123456789012\"");
                    }

                    Chmod(filepath, 666, false);
                }
                catch (Exception)
                { }


                if (File.Exists("cmd_updated.txt"))
                {
                    Logfile.Log("Update skipped!");
                    return;
                }

                File.AppendAllText("cmd_updated.txt", DateTime.Now.ToLongTimeString());
                Logfile.Log("Start update");

                if (Tools.IsMono())
                {
                    Chmod("VERSION", 666);
                    Chmod("settings.json", 666);
                    Chmod("cmd_updated.txt", 666);
                    Chmod("MQTTClient.exe.config", 666);

                    if (!Exec_mono("git", "--version", false).Contains("git version"))
                    {
                        Exec_mono("apt-get", "-y install git");
                        Exec_mono("git", "--version");
                    }

                    Exec_mono("rm", "-rf /etc/teslalogger/git/*");

                    Exec_mono("rm", "-rf /etc/teslalogger/git");
                    Exec_mono("mkdir", "/etc/teslalogger/git");
                    Exec_mono("mozroots", "--import --sync --machine");
                    for (int x = 1; x < 10; x++)
                    {
                        Logfile.Log("git clone: try " + x);
                        Exec_mono("git", "clone --progress https://github.com/bassmaster187/TeslaLogger /etc/teslalogger/git/", true, true);

                        if (Directory.Exists("/etc/teslalogger/git/TeslaLogger/GrafanaPlugins"))
                        {
                            Logfile.Log("git clone success!");
                            break;
                        }
                        Logfile.Log("Git failed. Retry in 30 sec!");
                        System.Threading.Thread.Sleep(30000);
                    }

                    Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/GrafanaPlugins"), new DirectoryInfo("/var/lib/grafana/plugins"));
                    Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/www"), new DirectoryInfo("/var/www/html"));
                    Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/bin/geofence.csv", "/etc/teslalogger/geofence.csv");
                    Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/GrafanaConfig/sample.yaml", "/etc/grafana/provisioning/dashboards/sample.yaml");

                    if (!Directory.Exists("/var/lib/grafana/dashboards"))
                    {
                        Directory.CreateDirectory("/var/lib/grafana/dashboards");
                    }

                    Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/bin"), new DirectoryInfo("/etc/teslalogger"));

                    try
                    {
                        if (!File.Exists("/etc/teslalogger/MQTTClient.exe.config"))
                        {
                            Logfile.Log("Copy empty MQTTClient.exe.config file");
                            Tools.CopyFile("/etc/teslalogger/git/MQTTClient/App.config", "/etc/teslalogger/MQTTClient.exe.config");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.ToString());
                    }
                }

                Logfile.Log("End update");

                Logfile.Log("Rebooting");

                Exec_mono("reboot", "");
            }
            catch (Exception ex)
            {
                Logfile.Log("Error in update: " + ex.ToString());
            }
        }

        public static void CheckDBCharset()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT default_character_set_name FROM information_schema.SCHEMATA WHERE schema_name = 'teslalogger'; ", con);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        string charset = dr[0].ToString();

                        if (charset != "utf8mb4")
                        {
                            dr.Close();

                            Logfile.Log("Chage database charset to utf8mb4");
                            cmd = new MySqlCommand("ALTER DATABASE teslalogger CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci", con);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void UpdatePHPini()
        {
            try
            {
                string phpinipath = "/etc/php/7.0/apache2/php.ini";
                if (File.Exists(phpinipath))
                {
                    string phpini = File.ReadAllText("/etc/php/7.0/apache2/php.ini");
                    string newphpini = Regex.Replace(phpini, "(post_max_size\\s*=)(.*)", "$1 50M");
                    newphpini = Regex.Replace(newphpini, "(upload_max_filesize\\s*=)(.*)", "$1 50M");

                    File.WriteAllText(phpinipath, newphpini);

                    if (newphpini != phpini)
                    {
                        Logfile.Log("PHP.ini changed!");
                    }
                }   
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void FileChecker(object state)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine("FileChecker");

                if (File.Exists(cmd_restart_path))
                {
                    string content = File.ReadAllText(cmd_restart_path);
                    if (content.Contains("update"))
                    {
                        Logfile.Log("Update Request!");

                        if (File.Exists("cmd_updated.txt"))
                        {
                            Logfile.Log("delete cmd_updated.txt");

                            File.Delete("cmd_updated.txt");
                        }
                    }

                    File.Delete(cmd_restart_path);

                    if (Tools.IsDocker())
                    {
                        Logfile.Log("Restart Request!");

                        Environment.Exit(0);
                    }
                }

                if (!shareDataOnStartup && Tools.IsShareData())
                {
                    if (state is WebHelper wh)
                    {
                        shareDataOnStartup = true;
                        Logfile.Log("ShareData turned on!");

                        ShareData sd = new ShareData(wh.TaskerHash);
                        sd.SendAllChargingData();
                        sd.SendDegradationData();
                    }
                }

            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void UpdateDBView(WebHelper wh)
        {
            try
            {
                Logfile.Log("update view: trip");
                DBHelper.ExecuteSQLQuery("DROP VIEW IF EXISTS `trip`");
                string s = DBViews.Trip;
                s = s.Replace("0.190052356", wh.carSettings.Wh_TR);

                Tools.GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin, out string Range);
                if (Range == "RR")
                {
                    s = s.Replace("`pos_start`.`ideal_battery_range_km` AS `StartRange`,", "`pos_start`.`battery_range_km` AS `StartRange`,");
                    s = s.Replace("`pos_end`.`ideal_battery_range_km` AS `EndRange`,", "`pos_end`.`battery_range_km` AS `EndRange`,");
                }

                File.WriteAllText("view_trip.txt", s);

                DBHelper.ExecuteSQLQuery(s, 300);
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static Dictionary<string, string> GetLanguageDictionary(string language)
        {
            Dictionary<string, string> ht = new Dictionary<string, string>();

            string filename = Path.Combine(FileManager.GetExecutingPath(), "language-" + language + ".txt");
            string content = null;

            if (File.Exists(filename))
            {
                try
                {
                    string[] lines = File.ReadAllLines(filename);
                    foreach (string line in lines)
                    {
                        content = line;

                        if (line.Length == 0)
                        {
                            continue;
                        }

                        if (line.StartsWith("#"))
                        {
                            continue;
                        }

                        if (!line.Contains("="))
                        {
                            continue;
                        }

                        int pos = line.IndexOf("=");
                        string key = line.Substring(0, pos).Trim();
                        string value = line.Substring(pos + 1);

                        // Logfile.Log("Key insert: " + key);

                        if (ht.ContainsKey(key))
                        {
                            Logfile.Log($"Error Key '{key}' already in Dictionary!!!");
                            continue;
                        }

                        if (value.Trim().Length > 0)
                        {
                            ht.Add(key, value);
                        }
                        else
                        {
                            ht.Add(key, key +" xxx");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.Message);
                    Logfile.ExceptionWriter(ex, content);
                }
            }

            return ht;
        }


        public static void UpdateGrafana(WebHelper wh)
        {
            try
            {
                if (Tools.IsMono())
                {
                    Tools.GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin, out string Range);

                    Dictionary<string, string> dictLanguage = GetLanguageDictionary(language);

                    Logfile.Log("Start Grafana update");

                    if (Tools.GetGrafanaVersion() == "5.5.0-d3b39f39pre1")
                    {
                        Logfile.Log("upgrade Grafana to 6.3.5!");

                        Exec_mono("wget", @"https://dl.grafana.com/oss/release/grafana_6.3.5_armhf.deb");

                        Exec_mono("dpkg", "-i grafana_6.3.5_armhf.deb");

                        Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/GrafanaPlugins"), new DirectoryInfo("/var/lib/grafana/plugins"));
                    }

                    Logfile.Log(" Wh/TR km: " + wh.carSettings.Wh_TR);

                    Exec_mono("rm", "-rf /etc/teslalogger/tmp/*");
                    Exec_mono("rm", "-rf /etc/teslalogger/tmp");

                    Exec_mono("mkdir", "/etc/teslalogger/tmp");
                    Exec_mono("mkdir", "/etc/teslalogger/tmp/Grafana");

                    bool useNewTrackmapPanel = Directory.Exists("/var/lib/grafana/plugins/pR0Ps-grafana-trackmap-panel");

                    UpdateDBView(wh);

                    Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/Grafana"), new DirectoryInfo("/etc/teslalogger/tmp/Grafana"));
                    // changes to dashboards
                    foreach (string f in Directory.GetFiles("/etc/teslalogger/tmp/Grafana"))
                    {
                        Logfile.Log("Update: " + f);
                        string s = File.ReadAllText(f);
                        s = s.Replace("0.190052356", wh.carSettings.Wh_TR);
                        s = s.Replace("TASKERTOKEN", wh.TaskerHash);

                        if (Range == "RR")
                        {
                            if (!(f.EndsWith("Akku Trips.json") || f.EndsWith("Speed Consumption.json")))
                                s = s.Replace("ideal_battery_range_km", "battery_range_km");
                        }

                        if (power == "kw")
                        {
                            //Logfile.Log("Convert to kw");

                            if (f.EndsWith("Verbrauch.json"))
                            {
                                s = s.Replace("power as 'Leistung [PS]'", "power/1.35962 as 'Leistung [kW]'");
                            }
                            else if (f.EndsWith("Trip.json"))
                            {
                                s = s.Replace("max PS", "max kW");
                                s = s.Replace("min PS", "min kW");
                                s = s.Replace("Ø PS", "Ø kW");

                                s = s.Replace(" power_max", "power_max/1.35962 as power_max");
                                s = s.Replace(" power_min", "power_min/1.35962 as power_min");
                                s = s.Replace(" power_avg", "power_avg/1.35962 as power_avg");
                            }
                        }

                        if (temperature == "fahrenheit")
                        {
                            //Logfile.Log("Convert to fahrenheit");

                            if (f.EndsWith("Laden.json"))
                            {
                                s = s.Replace("outside_temp as 'Außentemperatur [°C]'", "outside_temp * 9/5 + 32 as 'Außentemperatur [°F]'");
                            }
                            else if (f.EndsWith("Trip.json"))
                            {
                                s = s.Replace("Ø °C", "Ø °F");

                                s = s.Replace(" outside_temp_avg", "outside_temp_avg * 9/5 + 32 as outside_temp_avg");
                            }
                            else if (f.EndsWith("Verbrauch.json"))
                            {
                                s = s.Replace("outside_temp as 'Außentemperatur [°C]'", "outside_temp * 9/5 + 32 as 'Außentemperatur [°F]'");
                            }
                        }

                        if (length == "mile")
                        {
                            //Logfile.Log("Convert to mile");

                            if (f.EndsWith("Akku Trips.json"))
                            {
                                s = s.Replace("Start km", "Start mi");
                                s = s.Replace("End km", "End mi");

                                s = s.Replace("EndOdometer - StartOdometer AS kmDiff", "(EndOdometer - StartOdometer) / 1.609 AS kmDiff");
                                s = s.Replace("StartOdometer,", " StartOdometer / 1.609 as StartOdometer,");
                                s = s.Replace("EndOdometer,", " EndOdometer / 1.609 as EndOdometer,");
                                s = s.Replace("100 AS MaxRange", "100 / 1.609 AS MaxRange");
                                s = s.Replace("(EndOdometer - StartOdometer) * 100 AS AVGConsumption", "(EndOdometer/1.609 - StartOdometer/1.609) * 100 AS AVGConsumption");
                            }
                            else if (f.EndsWith("Degradation.json"))
                            {
                                s = s.Replace(" as 'Maximalreichweite [km]'", " / 1.609 as 'Maximalreichweite [mi]'");
                                s = s.Replace(" AS 'Max. Reichweite (Monatsmittel) [km]'", " / 1.609 AS 'Max. Reichweite (Monatsmittel) [mi]'");
                                s = s.Replace("odometer as 'km Stand [km]'", "odometer / 1.609 as 'mi Stand [mi]'");
                                s = s.Replace("km Stand [km]", "mi Stand [mi]");

                            }
                            else if (f.EndsWith("Laden.json"))
                            {
                                s = s.Replace(" as 'Reichweite [km]',", " / 1.609 as 'Reichweite [mi]',");

                                s = s.Replace("Reichweite [km]", "Reichweite [mi]");
                            }
                            else if (f.EndsWith("Trip.json"))
                            {
                                s = s.Replace(" speed_max,", "speed_max / 1.609 as speed_max,");
                                s = s.Replace(" avg_consumption_kWh_100km,", " avg_consumption_kWh_100km * 1.609 as avg_consumption_kWh_100km,");
                                s = s.Replace(" as avg_kmh,", " / 1.609 as avg_kmh");
                                s = s.Replace(" km_diff,", " km_diff  / 1.609 as km_diff,");

                                s = s.Replace("\"max km/h\"", "\"max mph\"");
                                s = s.Replace("\"Ø km/h\"", "\"Ø mph\"");
                                s = s.Replace("\"km\"", "\"mi\"");
                            }
                            else if (f.EndsWith("Vampir Drain.json"))
                            {
                                s = s.Replace(" TP2.odometer,", " TP2.odometer / 1.609 as odometer,");
                                s = s.Replace("ideal_battery_range_km ", "ideal_battery_range_km / 1.609 ");

                                s = s.Replace("\"km Stand\"", "\"mi Stand\"");
                                s = s.Replace("\"TR km Start\"", "\"TR mi Start\"");
                                s = s.Replace("\"TR km Ende\"", "\"TR mi Ende\"");
                                s = s.Replace("\"TR km Verlust\"", "\"TR mi Verlust\"");
                                s = s.Replace("\"TR km Verlust pro Stunde\"", "\"TR mi Verlust pro Stunde\"");
                            }
                            else if (f.EndsWith("Vampir Drain Monatsstatistik.json"))
                            {
                                s = s.Replace(" as RangeLost", " / 1.609 as RangeLost");

                                s = s.Replace("TR km Verlust", "TR mi Verlust");
                            }
                            else if (f.EndsWith("Verbrauch.json"))
                            {
                                s = s.Replace(" speed as 'Geschwindigkeit [km/h]'", " speed / 1.609 as 'Geschwindigkeit [mph]'");
                                s = s.Replace(" ideal_battery_range_km as 'Reichweite [km]'", " ideal_battery_range_km / 1.609 as 'Reichweite [mi]'");
                            }
                            else if (f.EndsWith("Ladehistorie.json"))
                            {
                                s = s.Replace("ideal_battery_range_km ", "ideal_battery_range_km / 1.609 ");

                                s = s.Replace("\"TR km Start\"", "\"TR mi Start\"");
                                s = s.Replace("\"TR km Ende\"", "\"TR mi Ende\"");
                            }
                        }

                        if (language != "de")
                        {
                            Logfile.Log("Convert to language: " + language);

                            s = ReplaceAliasTags(s, dictLanguage);

                            if (f.EndsWith("Akku Trips.json"))
                            {
                                s = ReplaceTitleTag(s, "Akku Trips", dictLanguage);
                            }
                            else if (f.EndsWith("Degradation.json"))
                            {
                                s = ReplaceTitleTag(s, "Degradation", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Maximalreichweite[km]", "Maximalreichweite [mi]","mi Stand [mi]","km Stand [km]","Max. Reichweite (Monatsmittel) [km]","Max. Reichweite (Monatsmittel) [mi]"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Ladehistorie.json"))
                            {
                                s = ReplaceTitleTag(s, "Ladehistorie", dictLanguage);
                            }
                            else if (f.EndsWith("Laden.json"))
                            {
                                s = ReplaceTitleTag(s, "Laden", dictLanguage);

                                s = ReplaceLanguageTags(s, new string[] {
                                    "SOC [%]", "Leistung [PS]", "Leistung [kW]", "Reichweite [mi]", "Reichweite [km]", "Ladespannung [V]", "Phasen",
                                    "Stromstärke [A]", "Außentemperatur [°C]", "Außentemperatur [°F]",
                                    "Angefordert [A]", "Pilot [A]", "Zelltemperatur [°C]", "Zelltemperatur [°F]"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Trip.json"))
                            {
                                s = ReplaceTitleTag(s, "Trip", dictLanguage);
                            }
                            else if (f.EndsWith("Vampir Drain.json"))
                            {
                                s = ReplaceTitleTag(s, "Vampir Drain", dictLanguage);
                            }
                            else if (f.EndsWith("Vampir Drain Monatsstatistik.json"))
                            {
                                s = ReplaceTitleTag(s, "Vampir Drain Monatsstatistik", dictLanguage);
                            }
                            else if (f.EndsWith("Verbrauch.json"))
                            {
                                s = ReplaceTitleTag(s, "Verbrauch", dictLanguage);

                                s = ReplaceNameTag(s, "Laden", dictLanguage);
                                s = ReplaceNameTag(s, "Laden fertig", dictLanguage);
                                s = ReplaceNameTag(s, "Schlafen", dictLanguage);
                                s = ReplaceNameTag(s, "Online", dictLanguage);

                                s = ReplaceLanguageTags(s, new string[] {
                                    "Geschwindigkeit [km/h]", "Geschwindigkeit [mph]", "Leistung [PS]", "Leistung [kW]", "Reichweite [mi]", "Reichweite [km]", "SOC [%]",
                                    "Außentemperatur [°C]", "Außentemperatur [°F]", "Höhe [m]","Innentemperatur [°C]","Innentemperatur [°F]"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Visited.json"))
                            {
                                s = ReplaceTitleTag(s, "Visited", dictLanguage);
                            }
                            else if (f.EndsWith("km Stand.json"))
                            {
                                s = ReplaceTitleTag(s, "km Stand", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "km Stand [km]"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Ladestatistik.json"))
                            {
                                s = ReplaceTitleTag(s, "Ladestatistik", dictLanguage);
                            }
                            else if (f.EndsWith("SOC Ladestatistik.json"))
                            {
                                s = ReplaceTitleTag(s, "SOC Ladestatistik", dictLanguage);
                            }
                            else if (f.EndsWith("Zellspannungen 01-20 - ScanMyTesla.json"))
                            {
                                s = ReplaceTitleTag(s, "Zellspannungen 01-20 - ScanMyTesla", dictLanguage);
                            }
                            else if (f.EndsWith("Zellspannungen 21-40 - ScanMyTesla.json"))
                            {
                                s = ReplaceTitleTag(s, "Zellspannungen 21-40 - ScanMyTesla", dictLanguage);
                            }
                            else if (f.EndsWith("Zellspannungen 41-60 - ScanMyTesla.json"))
                            {
                                s = ReplaceTitleTag(s, "Zellspannungen 41-60 - ScanMyTesla", dictLanguage);
                            }
                            else if (f.EndsWith("Zellspannungen 61-80 - ScanMyTesla.json"))
                            {
                                s = ReplaceTitleTag(s, "Zellspannungen 61-80 - ScanMyTesla", dictLanguage);
                            }
                            else if (f.EndsWith("Zellspannungen 81-99 - ScanMyTesla.json"))
                            {
                                s = ReplaceTitleTag(s, "Zellspannungen 81-99 - ScanMyTesla", dictLanguage);
                            }
                            else if (f.EndsWith("SOC Ladestatistik.json"))
                            {
                                s = ReplaceTitleTag(s, "SOC Ladestatistik", dictLanguage);
                            }
                            else if (f.EndsWith("Trip Monatsstatistik.json"))
                            {
                                s = ReplaceTitleTag(s, "Trip Monatsstatistik", dictLanguage);
                            }
                            else if (f.EndsWith("Alle Verbräuche -ScanMyTesla.json"))
                            {
                                s = ReplaceTitleTag(s, "Alle Verbräuche - ScanMyTesla", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Außentemperatur [°C]","Zelltemperatur [°C]","Alle Verbräuche - ScanMyTesla"
                                }, dictLanguage, true);
                            }
                            else
                            {
                                Logfile.Log("Title of " + f + " not translated!");
                            }
                        }

                        if (URL_Admin.Length > 0)
                        {
                            string temp_URL = URL_Admin;
                            if (!temp_URL.EndsWith("/"))
                            {
                                temp_URL += "/";
                            }

                            s = s.Replace("http://raspberry/admin/", temp_URL);
                        }

                        if (useNewTrackmapPanel)
                        {
                            s = s.Replace("grafana-trackmap-panel", "pr0ps-trackmap-panel");
                        }

                        File.WriteAllText(f, s);
                    }

                    Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/tmp/Grafana"), new DirectoryInfo("/var/lib/grafana/dashboards"));

                    if (!Tools.IsDocker())
                    {
                        Exec_mono("service", "grafana-server restart");
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            finally
            {
                Logfile.Log("End Grafana update");
            }
        }

        private static string ReplaceAliasTags(string content, Dictionary<string, string> dictLanguage)
        {
            try
            {
                Regex regexAlias = new Regex("\\\"alias\\\":.*?\\\"(.+)\\\"");

                MatchCollection matches = regexAlias.Matches(content);

                foreach (Match match in matches)
                {
                    content = ReplaceAliasTag(content, match.Groups[1].Value, dictLanguage);
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            return content;
        }

        private static string ReplaceAliasTag(string content, string v, Dictionary<string, string> dictLanguage)
        {
            if (!dictLanguage.ContainsKey(v))
            {
                Logfile.Log("Key '" + v + "' not Found in Translationfile!");
                return content;
            }

            Regex regexAlias = new Regex("\\\"alias\\\":.*?\\\""+ v +"\\\"");
            string replace = "\"alias\": \""+dictLanguage[v]+"\"";

            return regexAlias.Replace(content, replace);
        }

        private static string ReplaceNameTag(string content, string v, Dictionary<string, string> dictLanguage)
        {
            if (!dictLanguage.ContainsKey(v))
            {
                Logfile.Log("Key '" + v + "' not Found in Translationfile!");
                return content;
            }

            Regex regexAlias = new Regex("\\\"name\\\":.*?\\\"" + v + "\\\"");
            string replace = "\"name\": \"" + dictLanguage[v] + "\"";

            return regexAlias.Replace(content, replace);
        }

        private static string ReplaceTitleTag(string content, string v, Dictionary<string, string> dictLanguage)
        {
            if (!dictLanguage.ContainsKey(v))
            {
                Logfile.Log("Key '" + v + "' not Found in Translationfile!");
                return content;
            }

            Regex regexAlias = new Regex("\\\"title\\\":.*?\\\"" + v + "\\\"");
            string replace = "\"title\": \"" + dictLanguage[v] + "\"";

            return regexAlias.Replace(content, replace);
        }

        private static string ReplaceLanguageTags(string content, string[] v, Dictionary<string, string> dictLanguage, bool quoted)
        {
            foreach (string l in v)
            {
                content = ReplaceLanguageTag(content, l, dictLanguage, quoted);
            }

            return content;
        }

        private static string ReplaceLanguageTag(string content, string v, Dictionary<string, string> dictLanguage, bool quoted)
        {
            if (!dictLanguage.ContainsKey(v))
            {
                Logfile.Log("Key '" + v + "' not Found in Translationfile!");
                return content;
            }

            if (quoted)
            {
                content = content.Replace("'" + v + "'", "'" + dictLanguage[v] + "'");
                return content.Replace("\"" + v + "\"", "\"" + dictLanguage[v] + "\"");
            }
            else
            {
                return content.Replace(v, dictLanguage[v]);
            }
        }

        public static string Exec_mono(string cmd, string param, bool logging = true, bool stderr2stdout = false)
        {
            try
            {
                if (!Tools.IsMono())
                {
                    return "";
                }

                Logfile.Log("execute: " + cmd + " " + param);

                StringBuilder sb = new StringBuilder();

                System.Diagnostics.Process proc = new System.Diagnostics.Process
                {
                    EnableRaisingEvents = false
                };
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.FileName = cmd;
                proc.StartInfo.Arguments = param;

                proc.Start();

                while (!proc.HasExited)
                {
                    string line = proc.StandardOutput.ReadToEnd().Replace('\r', '\n');

                    if (logging && line.Length > 0)
                    {
                        Logfile.Log(" " + line);
                    }

                    sb.AppendLine(line);

                    line = proc.StandardError.ReadToEnd().Replace('\r', '\n');

                    if (logging && line.Length > 0)
                    {
                        if (stderr2stdout)
                        {
                            Logfile.Log(" " + line);
                        }
                        else
                        {
                            Logfile.Log("Error: " + line);
                        }
                    }

                    sb.AppendLine(line);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logfile.Log("Exception " + cmd + " " + ex.Message);
                return "Exception";
            }
        }

        public static void Chmod(string filename, int chmod, bool logging=true)
        {
            try
            {
                if (!Tools.IsMono())
                {
                    return;
                }

                if (logging)
                {
                    Logfile.Log("chmod " + chmod + " " + filename);
                }

                System.Diagnostics.Process proc = new System.Diagnostics.Process
                {
                    EnableRaisingEvents = false
                };
                proc.StartInfo.FileName = "chmod";
                proc.StartInfo.Arguments = chmod + " " + filename;
                proc.Start();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                Logfile.Log("chmod " + filename + " " + ex.Message);
            }
        }

        public static void CheckForNewVersion()
        {
            try
            {
                TimeSpan ts = DateTime.UtcNow - lastVersionCheck;
                if (ts.TotalMinutes > 120)
                {
                    Logfile.Log(" *** Check new Version ***");

                    string online_version = WebHelper.GetOnlineTeslaloggerVersion();
                    if (string.IsNullOrEmpty(online_version))
                    {
                        // recheck in 10 Minutes
                        Logfile.Log("Empty Version String - recheck in 10 minutes");
                        lastVersionCheck = lastVersionCheck.AddMinutes(10);
                        return;
                    }

                    lastVersionCheck = DateTime.UtcNow;

                    string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    Tools.UpdateType updateType = Tools.UpdateSettings();

                    if (UpdateNeeded(currentVersion, online_version, updateType))
                    {
                        // if update doesn't work, it will retry tomorrow
                        lastVersionCheck = DateTime.UtcNow.AddDays(1);

                        Logfile.Log("---------------------------------------------");
                        Logfile.Log(" *** New Version Detected *** ");
                        Logfile.Log("Current Version: " + currentVersion);
                        Logfile.Log("Online Version: " + online_version);
                        Logfile.Log("Start update!");

                        string cmd_updated = "/etc/teslalogger/cmd_updated.txt";

                        if (File.Exists(cmd_updated))
                        {
                            File.Delete(cmd_updated);
                        }

                        if (Tools.IsDocker())
                        {
                            Logfile.Log("  Docker detected!");
                            File.WriteAllText("/tmp/teslalogger-cmd-restart.txt", "update");
                        }
                        else
                        {
                            Logfile.Log("Rebooting");
                            Exec_mono("reboot", "");
                        }
                    }

                    return;
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static bool UpdateNeeded(string currentVersion, string online_version, Tools.UpdateType updateType)
        {
            if (updateType == Tools.UpdateType.none)
            {
                return false;
            }

            if (updateType == Tools.UpdateType.stable || updateType == Tools.UpdateType.all)
            {
                Version cv = new Version(currentVersion);
                Version ov = new Version(online_version);

                if (cv.CompareTo(ov) < 0)
                {
                    if (updateType == Tools.UpdateType.all)
                    {
                        return true;
                    }

                    if (ov.Build == 0 && ov.Revision == 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }
    }
}
