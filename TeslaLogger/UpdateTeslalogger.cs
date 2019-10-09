using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    class UpdateTeslalogger
    {
        static string cmd_restart_path = "/tmp/teslalogger-cmd-restart.txt";
        public static void Start(WebHelper wh)
        {
            try
            {
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
                }

                if (!DBHelper.TableExists("car_version"))
                {
                    Logfile.Log("CREATE TABLE car_version (id int NOT NULL AUTO_INCREMENT, StartDate datetime NOT NULL, version varchar(50), PRIMARY KEY(id))");
                    DBHelper.ExecuteSQLQuery("CREATE TABLE car_version (id int NOT NULL AUTO_INCREMENT, StartDate datetime NOT NULL, version varchar(50), PRIMARY KEY(id))");
                }


                if (!DBHelper.ColumnExists("trip", "outside_temp_avg"))
                {
                    UpdateDBView(wh);
                }

                System.Threading.Timer t = new System.Threading.Timer(FileChecker, null, 10000, 5000);

                if (System.IO.File.Exists("cmd_updated.txt"))
                {
                    Logfile.Log("Update skipped!");
                    return;
                }

                System.IO.File.AppendAllText("cmd_updated.txt", DateTime.Now.ToLongTimeString());
                Logfile.Log("Start update");

                if (Tools.IsMono())
                {
                    chmod("VERSION", 666);
                    chmod("settings.json", 666);
                    chmod("cmd_updated.txt", 666);
                    chmod("MQTTClient.exe.config", 666);

                    if (!exec_mono("git", "--version", false).Contains("git version"))
                    {
                        exec_mono("apt-get", "-y install git");
                        exec_mono("git", "--version");
                    }

                    exec_mono("rm", "-rf /etc/teslalogger/git/*");

                    exec_mono("rm", "-rf /etc/teslalogger/git");
                    exec_mono("mkdir", "/etc/teslalogger/git");
                    exec_mono("git", "clone https://github.com/bassmaster187/TeslaLogger /etc/teslalogger/git/");

                    Tools.CopyFilesRecursively(new System.IO.DirectoryInfo("/etc/teslalogger/git/TeslaLogger/GrafanaPlugins"), new System.IO.DirectoryInfo("/var/lib/grafana/plugins"));
                    Tools.CopyFilesRecursively(new System.IO.DirectoryInfo("/etc/teslalogger/git/TeslaLogger/www"), new System.IO.DirectoryInfo("/var/www/html"));
                    Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/bin/geofence.csv", "/etc/teslalogger/geofence.csv");
                    Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/GrafanaConfig/sample.yaml", "/etc/grafana/provisioning/dashboards/sample.yaml");

                    if (!System.IO.Directory.Exists("/var/lib/grafana/dashboards"))
                        System.IO.Directory.CreateDirectory("/var/lib/grafana/dashboards");

                    Tools.CopyFilesRecursively(new System.IO.DirectoryInfo("/etc/teslalogger/git/TeslaLogger/bin"), new System.IO.DirectoryInfo("/etc/teslalogger"));

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

                exec_mono("reboot", "");
            }
            catch (Exception ex)
            {
                Logfile.Log("Error in update: " + ex.ToString());
            }
        }

        private static void FileChecker(object state)
        {
            try
            {
                if (File.Exists(cmd_restart_path))
                {
                    string content = File.ReadAllText(cmd_restart_path);
                    if (content.Contains("update"))
                    {
                        Logfile.Log("Update Request!");

                        if (System.IO.File.Exists("cmd_updated.txt"))
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
                String s = DBViews.Trip;
                s = s.Replace("0.190052356", wh.carSettings.Wh_TR);

                System.IO.File.WriteAllText("view_trip.txt", s);

                DBHelper.ExecuteSQLQuery(s);
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        static Dictionary<string, string> GetLanguageDictionary(string language)
        {
            System.Collections.Generic.Dictionary<string, string> ht = new Dictionary<string, string>();

            string filename = Path.Combine(FileManager.GetExecutingPath(), "language-" + language + ".txt");
            string content = null;

            if (System.IO.File.Exists(filename))
            {
                try
                {
                    String[] lines = File.ReadAllLines(filename);
                    foreach (string line in lines)
                    {
                        content = line;

                        if (line.Length == 0)
                            continue;
                        if (line.StartsWith("#"))
                            continue;
                        if (!line.Contains("="))
                            continue;

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
                            ht.Add(key, value);
                        else
                            ht.Add(key, key +" xxx");
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
                    string power;
                    string temperature;
                    string length;
                    string language;
                    string URL_Admin;
                    Tools.GrafanaSettings(out power, out temperature, out length, out language, out URL_Admin);

                    Dictionary<string, string> dictLanguage = GetLanguageDictionary(language);

                    Logfile.Log("Start Grafana update");

                    if (Tools.GetGrafanaVersion() == "5.5.0-d3b39f39pre1")
                    {
                        Logfile.Log("upgrade Grafana to 6.3.5!");

                        exec_mono("wget", @"https://dl.grafana.com/oss/release/grafana_6.3.5_armhf.deb");

                        exec_mono("dpkg", "-i grafana_6.3.5_armhf.deb");

                        Tools.CopyFilesRecursively(new System.IO.DirectoryInfo("/etc/teslalogger/git/TeslaLogger/GrafanaPlugins"), new System.IO.DirectoryInfo("/var/lib/grafana/plugins"));
                    }

                    Logfile.Log(" Wh/TR km: " + wh.carSettings.Wh_TR);

                    exec_mono("rm", "-rf /etc/teslalogger/tmp/*");
                    exec_mono("rm", "-rf /etc/teslalogger/tmp");

                    exec_mono("mkdir", "/etc/teslalogger/tmp");
                    exec_mono("mkdir", "/etc/teslalogger/tmp/Grafana");

                    bool useNewTrackmapPanel = System.IO.Directory.Exists("/var/lib/grafana/plugins/pR0Ps-grafana-trackmap-panel");

                    UpdateDBView(wh);

                    Tools.CopyFilesRecursively(new System.IO.DirectoryInfo("/etc/teslalogger/git/TeslaLogger/Grafana"), new System.IO.DirectoryInfo("/etc/teslalogger/tmp/Grafana"));
                    // changes to dashboards
                    foreach (string f in System.IO.Directory.GetFiles("/etc/teslalogger/tmp/Grafana"))
                    {
                        Logfile.Log("Update: " + f);
                        String s = System.IO.File.ReadAllText(f);
                        s = s.Replace("0.190052356", wh.carSettings.Wh_TR);

                        if (power == "kw")
                        {
                            Logfile.Log("Convert to kw");

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
                            Logfile.Log("Convert to fahrenheit");

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
                            Logfile.Log("Convert to mile");

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
                                s = s.Replace("odometer as 'km Stand [km]'", "odometer / 1.609 as 'mi Stand [mi]'");
                                s = s.Replace("\"max\": \"550\"", "\"max\": \"350\"");
                                s = s.Replace("\"min\": \"300\"", "\"min\": \"180\"");

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
                                    "Maximalreichweite[km]", "Maximalreichweite [mi]","mi Stand [mi]","km Stand [km]"
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
                                    "Angefordert [A]", "Pilot [A]"
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
                                    "Außentemperatur [°C]", "Außentemperatur [°F]", "Höhe [m]"
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
                            else
                                Logfile.Log("Title of " + f + " not translated!");
                        }

                        if (URL_Admin.Length > 0)
                        {
                            string temp_URL = URL_Admin;
                            if (!temp_URL.EndsWith("/"))
                                temp_URL += "/";

                            s = s.Replace("http://raspberry/admin/", temp_URL);
                        }

                        if (useNewTrackmapPanel)
                            s = s.Replace("grafana-trackmap-panel", "pr0ps-trackmap-panel");

                        System.IO.File.WriteAllText(f, s);
                    }

                    Tools.CopyFilesRecursively(new System.IO.DirectoryInfo("/etc/teslalogger/tmp/Grafana"), new System.IO.DirectoryInfo("/var/lib/grafana/dashboards"));

                    exec_mono("service", "grafana-server restart");
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
                System.Text.RegularExpressions.Regex regexAlias = new System.Text.RegularExpressions.Regex("\\\"alias\\\":.*?\\\"(.+)\\\"");

                var matches = regexAlias.Matches(content);

                foreach (System.Text.RegularExpressions.Match match in matches)
                    content = ReplaceAliasTag(content, match.Groups[1].Value, dictLanguage);
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

            System.Text.RegularExpressions.Regex regexAlias = new System.Text.RegularExpressions.Regex("\\\"alias\\\":.*?\\\""+ v +"\\\"");
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

            System.Text.RegularExpressions.Regex regexAlias = new System.Text.RegularExpressions.Regex("\\\"name\\\":.*?\\\"" + v + "\\\"");
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

            System.Text.RegularExpressions.Regex regexAlias = new System.Text.RegularExpressions.Regex("\\\"title\\\":.*?\\\"" + v + "\\\"");
            string replace = "\"title\": \"" + dictLanguage[v] + "\"";

            return regexAlias.Replace(content, replace);
        }

        private static string ReplaceLanguageTags(string content, string[] v, Dictionary<string, string> dictLanguage, bool quoted)
        {
            foreach (string l in v)
                content = ReplaceLanguageTag(content, l, dictLanguage, quoted);

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
                return content.Replace(v, dictLanguage[v]);
        }

        public static string exec_mono(string cmd, string param, bool logging = true)
        {
            try
            {
                if (!Tools.IsMono())
                    return "";

                Logfile.Log("execute: " + cmd + " " + param);

                StringBuilder sb = new StringBuilder();

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.FileName = cmd;
                proc.StartInfo.Arguments = param;
                
                proc.Start();

                proc.WaitForExit();

                while (!proc.StandardOutput.EndOfStream)
                {
                    string line = proc.StandardOutput.ReadLine();

                    if (logging)
                        Logfile.Log(" " + line);

                    sb.AppendLine(line);
                }

                while (!proc.StandardError.EndOfStream)
                {
                    string line = proc.StandardError.ReadLine();

                    if (logging)
                        Logfile.Log("Error: " + line);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Logfile.Log("Exception " + cmd + " " + ex.Message);
                return "Exception";
            }
        }

        public static void chmod(string filename, int chmod, bool logging=true)
        {
            try
            {
                if (!Tools.IsMono())
                    return;

                if (logging)
                    Logfile.Log("chmod " + chmod + " " + filename);

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
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
    }
}
