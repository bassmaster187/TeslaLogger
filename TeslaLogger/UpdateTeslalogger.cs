using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Threading;
using System.Net;
using System.IO.Compression;

namespace TeslaLogger
{
    internal class UpdateTeslalogger
    {
        private static readonly string cmd_restart_path = "/tmp/teslalogger-cmd-restart.txt";
        private static bool shareDataOnStartup = false;
        private static System.Threading.Timer timer;

        private static DateTime lastVersionCheck = DateTime.UtcNow;
        internal static DateTime GetLastVersionCheck() { return lastVersionCheck; }

        private static bool _done = false;

        public static bool Done { get => _done; }

        private static Thread ComfortingMessages = null;
        public static bool DownloadUpdateAndInstallStarted = false;

        public static void StopComfortingMessagesThread()
        {
            try
            {
                if (ComfortingMessages != null)
                {
                    ComfortingMessages.Abort();
                }
            }
            catch (Exception ex)
            {
                Tools.DebugLog("StopComfortingMessagesThread() exception", ex);
            }
        }

        public static void Start()
        {
            // update may take quite a while, especially if we ALTER TABLEs
            // start a thread that puts comforting messages into the log
            ComfortingMessages = new Thread(() =>
            {
                Random rnd = new Random();
                while (!Done)
                {
                    Thread.Sleep(15000 + rnd.Next(15000));
                    switch (rnd.Next(3))
                    {
                        case 0:
                            Logfile.Log("TeslaLogger update is still running, please be patient");
                            break;
                        case 1:
                            Logfile.Log("TeslaLogger update is still running, this may take a while");
                            break;
                        case 2:
                            Logfile.Log("TeslaLogger update is still running, this is fine");
                            break;
                        case 3:
                            Logfile.Log("TeslaLogger update is still running, thank you for your patience");
                            break;
                        default:
                            break;
                    }
                }
            })
            {
                Priority = ThreadPriority.BelowNormal
            };
            ComfortingMessages.Start();

            try
            {
                shareDataOnStartup = Tools.IsShareData();
                bool updateAllDrivestateData = false;

                // start schema update

                if (!DBHelper.ColumnExists("pos", "battery_level"))
                {
                    Logfile.Log("ALTER TABLE pos ADD COLUMN battery_level DOUBLE NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN battery_level DOUBLE NULL");
                }

                if (!DBHelper.ColumnExists("drivestate", "outside_temp_avg"))
                {
                    Logfile.Log("ALTER TABLE drivestate ADD COLUMN outside_temp_avg DOUBLE NULL, ADD COLUMN speed_max INT NULL, ADD COLUMN power_max INT NULL, ADD COLUMN power_min INT NULL, ADD COLUMN power_avg DOUBLE NULL");
                    DBHelper.ExecuteSQLQuery("ALTER TABLE drivestate ADD COLUMN outside_temp_avg DOUBLE NULL, ADD COLUMN speed_max INT NULL, ADD COLUMN power_max INT NULL, ADD COLUMN power_min INT NULL, ADD COLUMN power_avg DOUBLE NULL");
                    updateAllDrivestateData = true;
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

                /*
                if (!DBHelper.IndexExists("can_ix", "can"))
                {
                    Logfile.Log("alter table can add index can_ix (id,datum)");
                    DBHelper.ExecuteSQLQuery("alter table can add index can_ix (id,datum)", 600);
                    Logfile.Log("ALTER TABLE OK");
                }*/

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

                if (!DBHelper.ColumnExists("chargingstate", "cost_total"))
                {
                    Logfile.Log("ALTER TABLE chargingstate ADD Column cost_total");
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `chargingstate` 
                        ADD COLUMN `cost_total` DOUBLE NULL DEFAULT NULL,
                        ADD COLUMN `cost_currency` VARCHAR(3) NULL DEFAULT NULL,
                        ADD COLUMN `cost_per_kwh` DOUBLE NULL DEFAULT NULL,
                        ADD COLUMN `cost_per_session` DOUBLE NULL DEFAULT NULL,
                        ADD COLUMN `cost_per_minute` DOUBLE NULL DEFAULT NULL,
                        ADD COLUMN `cost_idle_fee_total` DOUBLE NULL DEFAULT NULL", 600);
                }

                if (!DBHelper.ColumnExists("chargingstate", "cost_kwh_meter_invoice"))
                {
                    Logfile.Log("ALTER TABLE chargingstate ADD Column cost_kwh_meter_invoice");
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `chargingstate` 
                        ADD COLUMN `cost_kwh_meter_invoice` DOUBLE NULL DEFAULT NULL", 600);
                }

                InsertCarID_Column("can");
                InsertCarID_Column("car_version");
                InsertCarID_Column("charging");
                InsertCarID_Column("chargingstate");
                InsertCarID_Column("drivestate");
                InsertCarID_Column("pos");
                InsertCarID_Column("shiftstate");
                InsertCarID_Column("state");

                if (!DBHelper.TableExists("cars"))
                {
                    Logfile.Log("create table cars");
                    DBHelper.ExecuteSQLQuery(@"CREATE TABLE `cars` (
                          `id` int(11) NOT NULL,
                          `tesla_name` varchar(45) DEFAULT NULL,
                          `tesla_password` varchar(45) DEFAULT NULL,
                          `tesla_carid` int(11) DEFAULT NULL,
                          `tesla_token` varchar(100) DEFAULT NULL,
                          `tesla_token_expire` datetime DEFAULT NULL,
                          `tasker_hash` varchar(10) DEFAULT NULL,
                          `model` varchar(45) DEFAULT NULL,
                          `model_name` varchar(45) DEFAULT NULL,
                          `wh_tr` double DEFAULT NULL,
                          `db_wh_tr` double DEFAULT NULL,
                          `db_wh_tr_count` int(11) DEFAULT NULL,
                          `car_type` varchar(45) DEFAULT NULL,
                          `car_special_type` varchar(45) DEFAULT NULL,
                          `car_trim_badging` varchar(45) DEFAULT NULL,
                          `display_name` varchar(45) DEFAULT NULL,
                          `raven` bit(1) DEFAULT NULL,
                          `Battery` varchar(45) DEFAULT NULL,
                          PRIMARY KEY (`id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;", 600);

                    try
                    {
                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();
                            using (MySqlCommand cmd = new MySqlCommand("INSERT INTO cars (id,tesla_name,tesla_password,tesla_carid, display_name) values (1, @tesla_name, @tesla_password, @tesla_carid, 'Tesla')", con))
                            {
                                cmd.Parameters.AddWithValue("@tesla_name", ApplicationSettings.Default.TeslaName);
                                cmd.Parameters.AddWithValue("@tesla_password", ApplicationSettings.Default.TeslaPasswort);
                                cmd.Parameters.AddWithValue("@tesla_carid", ApplicationSettings.Default.Car);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.ToString());
                    }
                }

                if (!DBHelper.ColumnExists("cars", "vin"))
                {
                    Logfile.Log("ALTER TABLE cars ADD Column vin");
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` 
                        ADD COLUMN `vin` VARCHAR(20) NULL DEFAULT NULL", 600);
                }

                if (!DBHelper.ColumnExists("cars", "freesuc"))
                {
                    Logfile.Log("ALTER TABLE cars ADD Column freesuc");
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD `freesuc` TINYINT UNSIGNED NOT NULL DEFAULT '0'", 600);
                }

                if (!DBHelper.IndexExists("can_ix2", "can"))
                {
                    Logfile.Log("alter table can add index can_ix2 (id,carid,datum)");
                    DBHelper.ExecuteSQLQuery("alter table can add index can_ix2 (id,carid,datum)", 6000);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.IndexExists("chargingsate_ix_pos", "chargingstate"))
                {
                    Logfile.Log("alter table chargingstate add index chargingsate_ix_pos (Pos)");
                    DBHelper.ExecuteSQLQuery("alter table chargingstate add index chargingsate_ix_pos (Pos)", 6000);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.ColumnExists("cars", "lastscanmytesla"))
                {
                    Logfile.Log("ALTER TABLE cars ADD Column lastscanmytesla");
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD COLUMN `lastscanmytesla` datetime NULL DEFAULT NULL", 600);
                }

                if (updateAllDrivestateData)
                {
                    DBHelper.UpdateAllDrivestateData();
                }

                if (!DBHelper.IndexExists("idx_pos_CarID_id", "pos"))
                {
                    Logfile.Log("alter table pos add index idx_pos_CarID_id (CarID, id)");      // used for: select max(id) from pos where CarID=?
                    DBHelper.ExecuteSQLQuery("alter table pos add index idx_pos_CarID_id (CarID, id)", 600);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.IndexExists("idx_pos_CarID_datum", "pos"))
                {
                    Logfile.Log("alter table pos add index idx_pos_CarID_datum (CarID, Datum)");
                    DBHelper.ExecuteSQLQuery("alter table pos add index idx_pos_CarID_datum (CarID, Datum)", 600);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (DBHelper.IndexExists("idx_pos_datum", "pos"))
                {
                    Logfile.Log("alter table pos drop index if exists idx_pos_datum");
                    DBHelper.ExecuteSQLQuery("alter table pos drop index if exists idx_pos_datum", 600);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (DBHelper.IndexExists("can_ix", "can"))
                {
                    Logfile.Log("alter table can drop index if exists can_ix");
                    DBHelper.ExecuteSQLQuery("alter table can drop index if exists can_ix", 600);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.IndexExists("IX_charging_carid_datum", "charging"))
                {
                    Logfile.Log("alter table charging add index IX_charging_carid_datum (CarId, Datum)");
                    DBHelper.ExecuteSQLQuery("alter table charging add index IX_charging_carid_datum (CarId, Datum)", 600);
                    Logfile.Log("ALTER TABLE OK");
                }

                if (!DBHelper.TableExists("superchargers"))
                {
                    string sql = @"
CREATE TABLE superchargers(
    id INT NOT NULL AUTO_INCREMENT,
    name VARCHAR(250) NOT NULL,
    lat DOUBLE NOT NULL,
    lng DOUBLE NOT NULL,
    PRIMARY KEY(id)
)";
                    Logfile.Log(sql);
                    DBHelper.ExecuteSQLQuery(sql);
                    Logfile.Log("CREATE TABLE OK");
                }

                if (!DBHelper.TableExists("superchargerstate"))
                {
                    string sql = @"
CREATE TABLE superchargerstate(
    id INT NOT NULL AUTO_INCREMENT,
    nameid INT NOT NULL,
    ts datetime NOT NULL,
    available_stalls TINYINT NOT NULL,
    total_stalls TINYINT NOT NULL,
    PRIMARY KEY(id)
)";
                    Logfile.Log(sql);
                    DBHelper.ExecuteSQLQuery(sql);
                    Logfile.Log("CREATE TABLE OK");
                }

                if (!DBHelper.ColumnExists("cars", "refresh_token"))
                {
                    Logfile.Log("ALTER TABLE cars ADD Column refresh_token");
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD COLUMN `refresh_token` TEXT NULL DEFAULT NULL", 600);
                }

                if (!DBHelper.ColumnExists("cars", "ABRP_token"))
                {
                    Logfile.Log("ALTER TABLE cars ADD Column ABRP_token");
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD COLUMN `ABRP_token` VARCHAR(40) NULL DEFAULT NULL", 600);
                }

                if (!DBHelper.ColumnExists("cars", "ABRP_mode"))
                {
                    Logfile.Log("ALTER TABLE cars ADD Column ABRP_mode");
                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD COLUMN `ABRP_mode` TINYINT(1) NULL DEFAULT 0", 600);
                }

                // check datetime precision in pos
                try
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
                        using (MySqlCommand cmd = new MySqlCommand("SELECT datetime_precision FROM INFORMATION_SCHEMA.COLUMNS WHERE table_name = 'pos' AND COLUMN_NAME = 'datum' and TABLE_SCHEMA = 'teslalogger'", con))
                        {
                            Tools.DebugLog(cmd);
                            MySqlDataReader dr = cmd.ExecuteReader();
                            if (dr.Read() && dr[0] != DBNull.Value)
                            {
                                if (int.TryParse(dr[0].ToString(), out int datetime_precision))
                                {
                                    if (datetime_precision != 3)
                                    {
                                        // update table
                                        Logfile.Log("ALTER TABLE `pos` CHANGE `Datum` `Datum` DATETIME(3) NOT NULL;");
                                        DBHelper.ExecuteSQLQuery(@"ALTER TABLE `pos` CHANGE `Datum` `Datum` DATETIME(3) NOT NULL;", 3000);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }

                // end of schema update

                if (!DBHelper.TableExists("trip") || !DBHelper.ColumnExists("trip", "outside_temp_avg"))
                {
                    UpdateDBView();
                }

                DBHelper.Enable_utf8mb4();

                DBHelper.EnableMothership();

                CheckDBCharset();

                DBHelper.UpdateHTTPStatusCodes();

                Logfile.Log("DBUpdate finished.");

                timer = new System.Threading.Timer(FileChecker, null, 10000, 5000);

                Chmod("/var/www/html/admin/wallpapers", 777);

                UpdatePHPini();
                CreateEmptyWeatherIniFile();
                CheckBackupCrontab();

                DownloadUpdateAndInstall();

            }
            catch (Exception ex)
            {
                Logfile.Log("Error in update: " + ex.ToString());
            }
            finally
            {
                try
                {
                    _done = true;
                    ComfortingMessages.Abort();
                }
                catch (Exception) { }
            }
        }

        public static void DownloadUpdateAndInstall()
        {
            DownloadUpdateAndInstallStarted = true;

            if (File.Exists("cmd_updated.txt"))
            {
                Logfile.Log("Update skipped!");
                try
                {
                    ComfortingMessages.Abort();
                }
                catch (Exception) { }
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

                if (!Tools.Exec_mono("git", "--version", false).Contains("git version"))
                {
                    Tools.Exec_mono("apt-get", "-y install git");
                    Tools.Exec_mono("git", "--version");
                }

                if (!Tools.Exec_mono("optipng", "-version", false).Contains("OptiPNG version"))
                {
                    if (Tools.Exec_mono("apt-get", "-y install optipng", false).Contains("apt --fix-broken"))
                    {
                        Logfile.Log("Info: apt-get cannot install optipng");
                    }
                    else
                    {
                        Tools.Exec_mono("optipng", "-version");
                    }
                }

                Tools.Exec_mono("rm", "-rf /etc/teslalogger/git/*");

                Tools.Exec_mono("rm", "-rf /etc/teslalogger/git");
                Tools.Exec_mono("mkdir", "/etc/teslalogger/git");
                Tools.Exec_mono("cert-sync", "/etc/ssl/certs/ca-certificates.crt");

                // download update package from github
                bool httpDownloadSuccessful = false;
                bool zipExtractSuccessful = false;
                string GitHubURL = "https://github.com/bassmaster187/TeslaLogger/archive/master.zip";
                string updatepackage = "/etc/teslalogger/tmp/master.zip";
                try
                {
                    if (!Directory.Exists("/etc/teslalogger/tmp"))
                    {
                        _ = Directory.CreateDirectory("/etc/teslalogger/tmp");
                    }
                    if (File.Exists(updatepackage))
                    {
                        File.Delete(updatepackage);
                    }
                    using (WebClient wc = new WebClient())
                    {
                        Logfile.Log($"downloading update package from {GitHubURL}");
                        wc.DownloadFile(GitHubURL, updatepackage);
                        Logfile.Log($"update package downloaded to {updatepackage}");
                        httpDownloadSuccessful = true;
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log("Exception during download from github: " + ex.ToString());
                    Logfile.ExceptionWriter(ex, "Exception during download from github");
                }

                // unzip downloaded update package
                if (httpDownloadSuccessful)
                {
                    try
                    {
                        if (File.Exists(updatepackage))
                        {
                            if (Directory.Exists("/etc/teslalogger/git"))
                            {
                                Directory.Delete("/etc/teslalogger/git", true);
                            }
                            if (Directory.Exists("/etc/teslalogger/tmp/zip"))
                            {
                                Directory.Delete("/etc/teslalogger/tmp/zip", true);
                            }
                            Logfile.Log($"unzip update package {updatepackage} to /etc/teslalogger/tmp/zip");
                            ZipFile.ExtractToDirectory(updatepackage, "/etc/teslalogger/tmp/zip");
                            // GitHub zip contains folder "TeslaLogger-master" so we have to move files around
                            if (Directory.Exists("/etc/teslalogger/tmp/zip/TeslaLogger-master"))
                            {
                                Logfile.Log($"move update files from /etc/teslalogger/tmp/zip/TeslaLogger-master to /etc/teslalogger/git");
                                Tools.Exec_mono("mv", "/etc/teslalogger/tmp/zip/TeslaLogger-master /etc/teslalogger/git");
                                if (Directory.Exists("/etc/teslalogger/git/TeslaLogger/GrafanaPlugins"))
                                {
                                    Logfile.Log("update package: download and unzip successful");
                                    zipExtractSuccessful = true;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log("Exception during unzip of downloaded update package: " + ex.ToString());
                        Logfile.ExceptionWriter(ex, "Exception during unzip of downloaded update package");
                    }
                }

                // git clone fallback
                if (httpDownloadSuccessful == false || zipExtractSuccessful == false)
                {
                    for (int x = 1; x < 10; x++)
                    {
                        Logfile.Log("git clone: try " + x);
                        Tools.Exec_mono("git", "clone --progress https://github.com/bassmaster187/TeslaLogger /etc/teslalogger/git/", true, true);

                        if (Directory.Exists("/etc/teslalogger/git/TeslaLogger/GrafanaPlugins"))
                        {
                            Logfile.Log("git clone success!");
                            break;
                        }
                        Logfile.Log("Git failed. Retry in 30 sec!");
                        System.Threading.Thread.Sleep(30000);
                    }
                }

                Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/GrafanaPlugins"), new DirectoryInfo("/var/lib/grafana/plugins"));
                Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/www"), new DirectoryInfo("/var/www/html"));
                Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/bin/geofence.csv", "/etc/teslalogger/geofence.csv");
                Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/GrafanaConfig/sample.yaml", "/etc/grafana/provisioning/dashboards/sample.yaml");

                if (!Directory.Exists("/var/lib/grafana/dashboards"))
                {
                    Directory.CreateDirectory("/var/lib/grafana/dashboards");
                }

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

                Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/bin"), new DirectoryInfo("/etc/teslalogger"), "TeslaLogger.exe");

                try
                {
                    Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/bin/TeslaLogger.exe", "/etc/teslalogger/TeslaLogger.exe");
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }

                Logfile.Log("End update");

                Logfile.Log("Rebooting");

                Tools.Exec_mono("reboot", "");
            }
        }


        private static void CheckBackupCrontab()
        {
            try
            {
                // Logfile.Log("check crontab!");

                if (Tools.GetOsVersion().Contains("RPI4"))
                {
                    string crontab = "/etc/crontab";

                    if (File.ReadAllText(crontab).Contains("/etc/teslalogger/backup.sh"))
                        return;

                    Logfile.Log("append backup.sh to crontab!");
                    File.AppendAllText(crontab, "0 1 * * * root /bin/bash /etc/teslalogger/backup.sh\n");
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void CreateEmptyWeatherIniFile()
        {
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
        }

        private static void InsertCarID_Column(string table)
        {
            if (!DBHelper.ColumnExists(table, "CarID"))
            {
                Logfile.Log($"ALTER TABLE {table} ADD Column CarID");
                DBHelper.ExecuteSQLQuery($"ALTER TABLE `{table}` ADD COLUMN `CarID` TINYINT NULL DEFAULT NULL", 6000);
                DBHelper.ExecuteSQLQuery($"update {table} set CarID=1", 6000);
            }
            if (DBHelper.GetColumnType(table, "CarID").Equals("int"))
            {
                Logfile.Log($"ALTER TABLE `{table}` MODIFY `CarID` TINYINT UNSIGNED");
                DBHelper.ExecuteSQLQuery($"ALTER TABLE `{table}` MODIFY `CarID` TINYINT UNSIGNED", 6000);
            }
        }

        public static void CheckDBCharset()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT default_character_set_name FROM information_schema.SCHEMATA WHERE schema_name = 'teslalogger'; ", con))
                    {
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            string charset = dr[0].ToString();

                            if (charset != "utf8mb4")
                            {
                                dr.Close();

                                Logfile.Log("Chage database charset to utf8mb4");
                                using (var cmd2 = new MySqlCommand("ALTER DATABASE teslalogger CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci", con))
                                {
                                    cmd2.ExecuteNonQuery();
                                }
                            }
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
                    string newphpini = Regex.Replace(phpini, "(post_max_size\\s*=)(.*)", "$1 150M");
                    newphpini = Regex.Replace(newphpini, "(upload_max_filesize\\s*=)(.*)", "$1 150M");

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
                    foreach (Car c in Car.allcars)
                    {
                        shareDataOnStartup = true;
                        Logfile.Log("ShareData turned on!");

                        ShareData sd = new ShareData(c);
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

        private static void UpdateDBView()
        {
            try
            {
                Logfile.Log("update view: trip");
                DBHelper.ExecuteSQLQuery("DROP VIEW IF EXISTS `trip`");
                string s = DBViews.Trip;

                Tools.GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin, out string Range, out _, out _, out _);
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



        internal static Dictionary<string, string> GetLanguageDictionary(string language)
        {
            Dictionary<string, string> ht = new Dictionary<string, string>();
            string filename = GetLanguageFilepath(language);
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
                            ht.Add(key, key + " xxx");
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

        private static string GetLanguageFilepath(string language)
        {
            string filename = Path.Combine(FileManager.GetExecutingPath(), "language-" + language + ".txt");
            filename = filename.Replace("\\bin\\Debug", "\\bin");
            return filename;
        }

        public static void UpdateGrafana()
        {
            try
            {
                if (Tools.IsMono())
                {
                    Tools.GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin, out string Range, out string URL_Grafana, out string defaultcar, out string defaultcarid);

                    Dictionary<string, string> dictLanguage = GetLanguageDictionary(language);

                    Logfile.Log("Start Grafana update");

                    string GrafanaVersion = Tools.GetGrafanaVersion();
                    if (GrafanaVersion == "5.5.0-d3b39f39pre1" || GrafanaVersion == "6.3.5" || GrafanaVersion == "6.7.3")
                    {
                        Thread threadGrafanaUpdate = new Thread(() =>
                        {
                            string GrafanaFilename = "grafana_7.2.0_armhf.deb";

                            Logfile.Log("upgrade Grafana to 7.2.0!");

                            if (File.Exists(GrafanaFilename))
                                File.Delete(GrafanaFilename);

                            // use internal downloader
                            const string grafanaUrl = "https://dl.grafana.com/oss/release/grafana_7.2.0_armhf.deb";
                            const string grafanaFile = "grafana_7.2.0_armhf.deb";
                            if (!Tools.DownloadToFile(grafanaUrl, grafanaFile, 300, true).Result)
                            {
                                // fallback to wget
                                Logfile.Log($"fallback o wget to download {grafanaUrl}");
                                Tools.Exec_mono("wget", $"{grafanaUrl}  --show-progress");
                            }

                            if (File.Exists(GrafanaFilename))
                            {
                                Logfile.Log(GrafanaFilename + " Sucessfully Downloaded -  Size:" + new FileInfo(GrafanaFilename).Length);

                                if (GrafanaVersion == "6.7.3") // first Raspberry PI4 install
                                    Tools.Exec_mono("dpkg", "-r grafana-rpi");

                                Tools.Exec_mono("dpkg", "-i --force-overwrite grafana_7.2.0_armhf.deb");
                            }

                            Logfile.Log("upgrade Grafana DONE!");

                            Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/GrafanaPlugins"), new DirectoryInfo("/var/lib/grafana/plugins"));
                        })
                        {
                            Name = "GrafanaUpdate"
                        };
                        threadGrafanaUpdate.Start();
                    }

                    // TODO Logfile.Log(" Wh/TR km: " + wh.car.Wh_TR);

                    Tools.Exec_mono("rm", "-rf /etc/teslalogger/tmp/*");
                    Tools.Exec_mono("rm", "-rf /etc/teslalogger/tmp");

                    Tools.Exec_mono("mkdir", "/etc/teslalogger/tmp");
                    Tools.Exec_mono("mkdir", "/etc/teslalogger/tmp/Grafana");

                    bool useNewTrackmapPanel = Directory.Exists("/var/lib/grafana/plugins/pR0Ps-grafana-trackmap-panel");

                    UpdateDBView();

                    List<String> dashboardlinks = new List<String>();

                    Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/Grafana"), new DirectoryInfo("/etc/teslalogger/tmp/Grafana"));
                    // changes to dashboards
                    foreach (string f in Directory.GetFiles("/etc/teslalogger/tmp/Grafana"))
                    {
                        Logfile.Log("Update: " + f);
                        string s = File.ReadAllText(f);
                        // TODO s = s.Replace("TASKERTOKEN", wh.car.TaskerHash);

                        if (Range == "RR")
                        {
                            if (!(f.EndsWith("Akku Trips.json") || f.EndsWith("Speed Consumption.json")))
                            {
                                s = s.Replace("ideal_battery_range_km", "battery_range_km");
                            }
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
                                s = s.Replace(" as avg_kmh", " / 1.609 as avg_kmh");
                                s = s.Replace(" km_diff,", " km_diff  / 1.609 as km_diff,");
                                s = s.Replace("StartRange - EndRange as RangeDiff", "(StartRange - EndRange) / 1.609 as RangeDiff");

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
                                s = ReplaceLanguageTags(s, new string[] {
                                    "AVG Max Range","AVG Consumption","AVG Trip Days","AVG SOC Diff"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Degradation.json"))
                            {
                                s = ReplaceTitleTag(s, "Degradation", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Maximalreichweite [km]", "Maximalreichweite [mi]","mi Stand [mi]","km Stand [km]","Max. Reichweite (Monatsmittel) [km]","Max. Reichweite (Monatsmittel) [mi]"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Firmware.json"))
                            {
                                s = ReplaceTitleTag(s, "Degradation", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Firmware","Date Installed","Days since previous update","Min Days Between Updates","AVG Days Between Updates","Max Days Between Updates"
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
                            else if (f.EndsWith("Speed Consumption.json"))
                            {
                                s = ReplaceTitleTag(s, "Speed Consumption", dictLanguage);
                            }
                            else if (f.EndsWith("Status.json"))
                            {
                                s = ReplaceTitleTag(s, "Status", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Current Status","SOC","Reichweite","Außentemperatur","Zelltemperatur","km Stand","Firmware","Nur verfügbar mit ScanMyTesla","N/A","Asleep","Online","Offline","Waking","Driving","Charging"
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
                            else if (f.EndsWith("Verbrauchsstatstik.json"))
                            {
                                s = ReplaceTitleTag(s, "Verbrauchsstatistik", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "km Stand[km]","mi Stand [mi]","Verbrauch Monatsmittel [kWh]","Außentemperatur Monatsmittel [°C]","Außentemperatur Monatsmittel [°F]","Verbrauch Tagesmittel [kWh]","Außentemperatur Tagesmittel [°C]", "Außentemperatur Tagesmittel [°F]"
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

                        string title, uid, link;
                        GrafanaGetTitleAndLink(s, URL_Grafana, out title, out uid, out link);

                        string carLabel = "Car";
                        dictLanguage.TryGetValue("Car", out carLabel);

                        s = UpdateDefaultCar(s, defaultcar, defaultcarid, carLabel);

                        if (!title.Contains("ScanMyTesla") && !title.Contains("Zelltemperaturen") && !title.Contains("SOC ") && !title.Contains("Chargertype") && !title.Contains("Mothership"))
                            dashboardlinks.Add(title + "|" + link);

                        File.WriteAllText(f, s);
                    }

                    try
                    {
                        dashboardlinks.Sort();

                        StringBuilder sb = new StringBuilder();
                        dashboardlinks.ForEach((s) => sb.Append(s).Append("\r\n"));

                        System.IO.File.WriteAllText("/etc/teslalogger/dashboardlinks.txt", sb.ToString(), Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.ToString());
                    }

                    Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/tmp/Grafana"), new DirectoryInfo("/var/lib/grafana/dashboards"));

                    if (!Tools.IsDocker())
                    {
                        Tools.Exec_mono("service", "grafana-server restart");
                    }

                    CopyLanguageFileToTimelinePanel(language);

                    CopySettingsToTimelinePanel();
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

        private static void CopyLanguageFileToTimelinePanel(string language)
        {
            try
            {
                string languageFilepath = GetLanguageFilepath(language);
                if (File.Exists(languageFilepath))
                {
                    string dst = "/var/lib/grafana/plugins/teslalogger-timeline-panel/dist/language.txt";
                    Logfile.Log("Copy " + languageFilepath + " to " + dst);
                    File.Copy(languageFilepath, dst, true);
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void CopySettingsToTimelinePanel()
        {
            try
            {
                string settingsFilepath = "/etc/teslalogger/settings.json";
                if (File.Exists(settingsFilepath))
                {
                    string dst = "/var/lib/grafana/plugins/teslalogger-timeline-panel/dist/settings.json";
                    Logfile.Log("Copy " + settingsFilepath + " to " + dst);
                    File.Copy(settingsFilepath, dst, true);
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        internal static string UpdateDefaultCar(string s, string name, string id, string carLabel)
        {
            try
            {
                if (name == null || name.Length == 0)
                    return s;

                Regex regexAlias = new Regex("(templating.*\\\"text\\\":\\s\\\")(\\\".*?value\\\":\\s\\\")(.*?)(\\\")(.*?display_name)(.*?label\\\":\\s\\\")(.*?)(\\\")", RegexOptions.Singleline | RegexOptions.Multiline);
                var m = regexAlias.Match(s);
                string ret = regexAlias.Replace(s, "${1}" + name + "${2}" + id + "${4}${5}${6}" + carLabel + "${8}");
                return ret;
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            return s;
        }

        internal static void GrafanaGetTitleAndLink(string json, string URL_Grafana, out string title, out string uid, out string link)
        {
            title = "";
            uid = "";
            link = "";
            try
            {
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);
                title = j["title"];
                uid = j["uid"];

                if (!URL_Grafana.EndsWith("/"))
                    URL_Grafana += "/";

                link = URL_Grafana + "d/" + uid + "/" + title;
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "");
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

            Regex regexAlias = new Regex("\\\"alias\\\":.*?\\\"" + v + "\\\"");
            string replace = "\"alias\": \"" + dictLanguage[v] + "\"";

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

        internal static string ReplaceTitleTag(string content, string v, Dictionary<string, string> dictLanguage)
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

        internal static string ReplaceLanguageTags(string content, string[] v, Dictionary<string, string> dictLanguage, bool quoted)
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


        public static void Chmod(string filename, int chmod, bool logging = true)
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

                using (System.Diagnostics.Process proc = new System.Diagnostics.Process
                {
                    EnableRaisingEvents = false
                })
                {
                    proc.StartInfo.FileName = "chmod";
                    proc.StartInfo.Arguments = chmod + " " + filename;
                    proc.Start();
                    proc.WaitForExit();
                }
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
                for (int x = 0; x < Car.allcars.Count; x++)
                {
                    Car c = Car.allcars[x];
                    if (c.GetCurrentState() == Car.TeslaState.Charge || c.GetCurrentState() == Car.TeslaState.Drive)
                        return;
                }

                TimeSpan ts = DateTime.UtcNow - lastVersionCheck;
                if (ts.TotalMinutes > 240)
                {
                    string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    Logfile.Log($"Checking TeslaLogger online update (current version: {currentVersion}) ...");

                    string online_version = WebHelper.GetOnlineTeslaloggerVersion();
                    if (string.IsNullOrEmpty(online_version))
                    {
                        // recheck in 10 Minutes
                        Logfile.Log("Empty Version String - recheck in 10 minutes");
                        lastVersionCheck = lastVersionCheck.AddMinutes(10);
                        return;
                    }

                    lastVersionCheck = DateTime.UtcNow;

                    Tools.UpdateType updateType = Tools.GetOnlineUpdateSettings();

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
                            Tools.Exec_mono("reboot", "");
                        }
                    }
                    else
                    {
                        Logfile.Log($"TeslaLogger is up to date (current version: {currentVersion}, latest version online: {online_version}, update policy: {updateType})");
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
