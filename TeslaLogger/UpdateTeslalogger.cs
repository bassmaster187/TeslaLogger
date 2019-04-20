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
        public static void Start(WebHelper wh)
        {
            try
            {
                if (!DBHelper.ColumnExists("pos", "battery_level"))
                {
                    Tools.Log("ALTER TABLE pos ADD COLUMN battery_level DOUBLE NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN battery_level DOUBLE NULL");
                }

                if (!DBHelper.ColumnExists("drivestate", "outside_temp_avg"))
                {
                    Tools.Log("ALTER TABLE drivestate ADD COLUMN outside_temp_avg DOUBLE NULL, ADD COLUMN speed_max INT NULL, ADD COLUMN power_max INT NULL, ADD COLUMN power_min INT NULL, ADD COLUMN power_avg DOUBLE NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE drivestate ADD COLUMN outside_temp_avg DOUBLE NULL, ADD COLUMN speed_max INT NULL, ADD COLUMN power_max INT NULL, ADD COLUMN power_min INT NULL, ADD COLUMN power_avg DOUBLE NULL");

                    DBHelper.UpdateAllDrivestateData();
                }

                if (!DBHelper.ColumnExists("charging", "charger_pilot_current"))
                {
                    Tools.Log("ALTER TABLE charging ADD COLUMN charger_pilot_current INT NULL, ADD COLUMN charge_current_request INT NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE charging ADD COLUMN charger_pilot_current INT NULL, ADD COLUMN charge_current_request INT NULL");
                }

                if (!DBHelper.ColumnExists("trip", "outside_temp_avg"))
                {
                    UpdateDBView(wh);
                }

                if (System.IO.File.Exists("cmd_updated.txt"))
                {
                    Tools.Log("Update skipped!");
                    return;
                }

                System.IO.File.AppendAllText("cmd_updated.txt", DateTime.Now.ToLongTimeString());
                Tools.Log("Start update");

                if (Tools.IsMono())
                {
                    chmod("cmd_updated.txt", 666);
                    chmod("/etc/teslalogger/nohup.out", 666);
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
                            Tools.Log("Copy empty MQTTClient.exe.config file");
                            Tools.CopyFile("/etc/teslalogger/git/MQTTClient/App.config", "/etc/teslalogger/MQTTClient.exe.config");
                        }
                    }
                    catch (Exception ex)
                    {
                        Tools.Log(ex.ToString());
                    }
                }

                Tools.Log("End update");

                Tools.Log("Rebooting");

                exec_mono("reboot", "");
            }
            catch (Exception ex)
            {
                Tools.Log("Error in update: " + ex.ToString());
            }
        }

        private static void UpdateDBView(WebHelper wh)
        {
            try
            {
                Tools.Log("update view: trip");
                DBHelper.ExecuteSQLQuery("DROP VIEW IF EXISTS `trip`");
                String s = DBViews.Trip;
                s = s.Replace("0.190052356", wh.carSettings.Wh_TR);

                System.IO.File.WriteAllText("view_trip.txt", s);

                DBHelper.ExecuteSQLQuery(s);
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
            }
        }

        public static void UpdateGrafana(WebHelper wh)
        {
            try
            {
                if (Tools.IsMono())
                {

                    Tools.Log("Start Grafana update");
                    Tools.Log(" Wh/TR km: " + wh.carSettings.Wh_TR);

                    exec_mono("rm", "-rf /etc/teslalogger/tmp/*");
                    exec_mono("rm", "-rf /etc/teslalogger/tmp");

                    exec_mono("mkdir", "/etc/teslalogger/tmp");
                    exec_mono("mkdir", "/etc/teslalogger/tmp/Grafana");

                    UpdateDBView(wh);

                    Tools.CopyFilesRecursively(new System.IO.DirectoryInfo("/etc/teslalogger/git/TeslaLogger/Grafana"), new System.IO.DirectoryInfo("/etc/teslalogger/tmp/Grafana"));
                    // changes to dashboards
                    foreach (string f in System.IO.Directory.GetFiles("/etc/teslalogger/tmp/Grafana"))
                    {
                        Tools.Log("Update: " + f);
                        String s = System.IO.File.ReadAllText(f);
                        s = s.Replace("0.190052356", wh.carSettings.Wh_TR);
                        System.IO.File.WriteAllText(f, s);
                    }

                    Tools.CopyFilesRecursively(new System.IO.DirectoryInfo("/etc/teslalogger/tmp/Grafana"), new System.IO.DirectoryInfo("/var/lib/grafana/dashboards"));

                    exec_mono("service", "grafana-server restart");
                }
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
            }
            finally
            {
                Tools.Log("End Grafana update");
            }
        }

        public static string exec_mono(string cmd, string param, bool logging = true)
        {
            try
            {
                if (!Tools.IsMono())
                    return "";

                Tools.Log("execute: " + cmd + " " + param);

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
                        Tools.Log(" " + line);

                    sb.AppendLine(line);
                }

                while (!proc.StandardError.EndOfStream)
                {
                    string line = proc.StandardError.ReadLine();

                    if (logging)
                        Tools.Log("Error: " + line);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Tools.Log("Exception " + cmd + " " + ex.Message);
                return "Exception";
            }
        }

        public static void chmod(string filename, int chmod)
        {
            try
            {
                if (!Tools.IsMono())
                    return;

                Tools.Log("chmod " + chmod + " " + filename);

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = "chmod";
                proc.StartInfo.Arguments = chmod + " " + filename;
                proc.Start();
                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                Tools.Log("chmod " + filename + " " + ex.Message);
            }
        }
    }
}
