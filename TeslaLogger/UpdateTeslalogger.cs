using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Threading;
using System.Net;
using System.IO.Compression;
using Exceptionless;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    internal class UpdateTeslalogger
    {
        private const string cmd_restart_path = "/tmp/teslalogger-cmd-restart.txt";
        private const string TPMSSchemaVersion = "TPMSSchemaVersion";
        private static bool shareDataOnStartup; // defaults to false;
        private static Timer timer;

        private static DateTime lastTeslaLoggerVersionCheck = DateTime.UtcNow;
        private static Object lastTeslaLoggerVersionCheckObj = new object();
        internal static DateTime GetLastVersionCheck() { return lastTeslaLoggerVersionCheck; }

        private static bool _done; // defaults to false;

        public static bool Done { get => _done; }

        private static Thread ComfortingMessages; // defaults to null;
        public static bool DownloadUpdateAndInstallStarted; // defaults to false;

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
                ex.ToExceptionless().FirstCarUserID().Submit();
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

                // start schema update

                KVS.CheckSchema();
                DBHelper.EnableUTF8mb4();

                CheckDBCharset();

                CheckDBSchema_areaa();

                CheckDBSchema_can();

                CheckDBSchema_candata();

                CheckDBSchema_cars();

                CheckDBSchema_car_version();

                CheckDBSchema_charging();

                CheckDBSchema_chargingstate();

                CheckDBSchema_drivestate();

                CheckDBSchema_httpcodes();

                Journeys.CheckSchema();

                GeocodeCache.CheckSchema();

                CheckDBSchema_mothership();

                CheckDBSchema_mothershipcommands();

                CheckDBSchema_pos();

                CheckDBSchema_shiftstate();

                CheckDBSchema_state();

                CheckDBSchema_superchargers();

                CheckDBSchema_superchargerstate();

                CheckDBSchema_TPMS();

                CheckDBSchema_Battery();

                CheckDBSchema_Cruisestate();

                CheckDBSchema_Alerts();

                GetChargingHistoryV2Service.CheckSchema();

                Komoot.CheckSchema();

                Logfile.Log("DBSchema Update finished.");

                // end of schema update

                // start view update

                _ = Task.Factory.StartNew(() =>
                {
                    Logfile.Log("DBView Update (Task) started.");
                    CheckDBViews();
                    if (!DBHelper.TableExists("trip") || !DBHelper.ColumnExists("trip", "AP_sec_sum"))
                    {
                        UpdateDBViews();
                    }
                    Logfile.Log("DBView Update (Task) finished.");
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

                // end of view update

                // start index update

                _ = Task.Factory.StartNew(() =>
                {
                    Logfile.Log("DBIndex Update (Task) started.");
                    if (!DBHelper.IndexExists("can_ix2", "can"))
                    {
                        Logfile.Log("alter table can add index can_ix2 (id,carid,datum)");
                        AssertAlterDB();
                        DBHelper.ExecuteSQLQuery("alter table can add index can_ix2 (id,carid,datum)", 6000);
                    }

                    if (!DBHelper.IndexExists("chargingsate_ix_pos", "chargingstate"))
                    {
                        Logfile.Log("alter table chargingstate add index chargingsate_ix_pos (Pos)");
                        AssertAlterDB();
                        DBHelper.ExecuteSQLQuery("alter table chargingstate add index chargingsate_ix_pos (Pos)", 6000);
                    }

                    if (!DBHelper.IndexExists("ixAnalyzeChargingStates1", "chargingstate"))
                    {
                        Logfile.Log("ALTER TABLE chargingstate ADD INDEX ixAnalyzeChargingStates1 ...");
                        AssertAlterDB();
                        DBHelper.ExecuteSQLQuery("ALTER TABLE chargingstate ADD INDEX ixAnalyzeChargingStates1 (id, CarID, StartChargingID, EndChargingID)", 6000);
                    }

                    if (!DBHelper.IndexExists("idx_pos_CarID_id", "pos"))
                    {
                        Logfile.Log("alter table pos add index idx_pos_CarID_id (CarID, id)");      // used for: select max(id) from pos where CarID=?
                        AssertAlterDB();
                        DBHelper.ExecuteSQLQuery("alter table pos add index idx_pos_CarID_id (CarID, id)", 6000);
                    }

                    if (!DBHelper.IndexExists("idx_pos_CarID_datum", "pos"))
                    {
                        Logfile.Log("alter table pos add index idx_pos_CarID_datum (CarID, Datum)");
                        AssertAlterDB();
                        DBHelper.ExecuteSQLQuery("alter table pos add index idx_pos_CarID_datum (CarID, Datum)", 6000);
                    }

                    if (DBHelper.IndexExists("idx_pos_datum", "pos"))
                    {
                        Logfile.Log("alter table pos drop index if exists idx_pos_datum");
                        AssertAlterDB();
                        DBHelper.ExecuteSQLQuery("alter table pos drop index if exists idx_pos_datum", 600);
                    }

                    if (DBHelper.IndexExists("can_ix", "can"))
                    {
                        Logfile.Log("alter table can drop index if exists can_ix");
                        AssertAlterDB();
                        DBHelper.ExecuteSQLQuery("alter table can drop index if exists can_ix", 600);
                    }

                    if (!DBHelper.IndexExists("IX_charging_carid_datum", "charging"))
                    {
                        Logfile.Log("alter table charging add index IX_charging_carid_datum (CarId, Datum)");
                        AssertAlterDB();
                        DBHelper.ExecuteSQLQuery("alter table charging add index IX_charging_carid_datum (CarId, Datum)", 600);
                    }

                    try
                    {

                        if (!DBHelper.IndexExists("ix_startpos", "drivestate"))
                        {
                            Logfile.Log("ALTER TABLE drivestate ADD UNIQUE ix_startpos (StartPos)");
                            AssertAlterDB();
                            DBHelper.ExecuteSQLQuery("ALTER TABLE drivestate ADD UNIQUE ix_startpos (StartPos)", 600);
                        }

                        if (DBHelper.IndexExists("ix_endpos", "drivestate"))
                        {
                            Logfile.Log("DROP INDEX ix_endpos");
                            AssertAlterDB();
                            DBHelper.ExecuteSQLQuery("ALTER TABLE drivestate DROP INDEX ix_endpos", 600);
                        }

                        if (!DBHelper.IndexExists("ix_id_ts", "mothership"))
                        {
                            Logfile.Log("ALTER TABLE mothership ADD UNIQUE ix_id_ts (id, ts)");
                            AssertAlterDB();
                            DBHelper.ExecuteSQLQuery("ALTER TABLE mothership ADD UNIQUE ix_id_ts (id, ts)", 1200);
                        }

                        if (!DBHelper.IndexExists("ix_endpos2", "drivestate"))
                        {
                            Logfile.Log("ALTER TABLE drivestate ADD ix_endpos2(EndPos)");
                            AssertAlterDB();
                            DBHelper.ExecuteSQLQuery("ALTER TABLE drivestate ADD INDEX ix_endpos2(EndPos)", 600);
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.ToExceptionless().FirstCarUserID().Submit();
                    }

                    Logfile.Log("DBIndex Update (Task) finished.");
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                // end index update

                DBHelper.EnableMothership();

                if (KVS.Get("UpdateAllDrivestateData", out int UpdateAllDrivestateDataInt) == KVS.NOT_FOUND || UpdateAllDrivestateDataInt < 2)
                {
                    UpdateAllDrivestateDateThread();
                }


                timer = new System.Threading.Timer(FileChecker, null, 10000, 5000);

                Chmod("/var/www/html/admin/wallpapers", 777);

                _ = Task.Factory.StartNew(() =>
                {
                    UpdatePHPini();
                    UpdateApacheConfig();
                    CreateEmptyWeatherIniFile();
                    CheckBackupCrontab();
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
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

            try
            {
                DownloadUpdateAndInstall();
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log("Error in update: " + ex.ToString());
            }
        }

        private static void CheckDBSchema_Alerts()
        {
            if (!DBHelper.TableExists("alerts"))
            {
                string sql = @"CREATE TABLE `alerts` (
                      `CarID` int(11) NOT NULL,
                      `startedAt` datetime NOT NULL,
                      `nameID` int(11) NOT NULL,
                      `endedAt` datetime DEFAULT NULL,
                      `ID` int(11) NOT NULL AUTO_INCREMENT,
                      PRIMARY KEY (`CarID`,`startedAt`,`nameID`),
                      KEY `ID` (`ID`)
                    ) ENGINE=InnoDB AUTO_INCREMENT=1778 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
                    ";

                Logfile.Log(sql);
                UpdateTeslalogger.AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql);
                Logfile.Log("CREATE TABLE OK");
            }

            if (!DBHelper.TableExists("alert_names"))
            {
                string sql = @"CREATE TABLE `alert_names` (
                      `ID` int(11) NOT NULL AUTO_INCREMENT,
                      `Name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
                      PRIMARY KEY (`ID`)
                    ) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

                Logfile.Log(sql);
                UpdateTeslalogger.AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql);
                Logfile.Log("CREATE TABLE OK");
            }

            if (!DBHelper.TableExists("alert_audiences"))
            {
                string sql = @"CREATE TABLE `alert_audiences` (
                      `alertsID` int(11) NOT NULL,
                      `audienceID` tinyint(4) NOT NULL,
                      PRIMARY KEY (`alertsID`,`audienceID`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

                Logfile.Log(sql);
                UpdateTeslalogger.AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql);
                Logfile.Log("CREATE TABLE OK");
            }
        }

        private static void CheckDBSchema_Battery()
        {
            if (!DBHelper.TableExists("battery"))
            {
                string sql = @"CREATE TABLE `battery` (
                      `CarID` int(11) NOT NULL,
                      `date` datetime NOT NULL,
                      `PackVoltage` double DEFAULT NULL,
                      `PackCurrent` double DEFAULT NULL,
                      `IsolationResistance` double DEFAULT NULL,
                      `NumBrickVoltageMax` smallint(6) DEFAULT NULL,
                      `BrickVoltageMax` double DEFAULT NULL,
                      `NumBrickVoltageMin` smallint(6) DEFAULT NULL,
                      `BrickVoltageMin` double DEFAULT NULL,
                      `ModuleTempMax` double DEFAULT NULL,
                      `ModuleTempMin` double DEFAULT NULL,
                      `LifetimeEnergyUsed` double DEFAULT NULL,
                      `LifetimeEnergyUsedDrive` double DEFAULT NULL,
                      PRIMARY KEY (`CarID`,`date`)
                    )";

                Logfile.Log(sql);
                UpdateTeslalogger.AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql);
                Logfile.Log("CREATE TABLE OK");
            }

            if (!DBHelper.TableExists("celltemperature"))
            {
                string sql = @"CREATE 
                    VIEW `celltemperature` AS
                        SELECT 
                            `can`.`CarID` AS `carid`,
                            `can`.`datum` AS `date`,
                            `can`.`val` AS `CellTemperature`,
                            1 AS `source`
                        FROM
                            `can`
                        WHERE
                            `can`.`id` = 3 
                        UNION SELECT 
                            `battery`.`CarID` AS `carid`,
                            `battery`.`date` AS `datum`,
                            `battery`.`ModuleTempMin` AS `CellTemperature`,
                            2 AS `source`
                        FROM
                            `battery`
                        WHERE
                            `battery`.`ModuleTempMin` IS NOT NULL";

                Logfile.Log(sql);
                UpdateTeslalogger.AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql);
                Logfile.Log("CREATE VIEW OK");
            }
        }

        private static void CheckDBSchema_Cruisestate()
        {
            if (!DBHelper.TableExists("cruisestate"))
            {
                string sql = @"CREATE TABLE `cruisestate` (
                  `CarID` int(11) NOT NULL,
                  `date` datetime NOT NULL,
                  `state` tinyint(4) DEFAULT NULL,
                  PRIMARY KEY (`CarID`,`date`)
                )";

                Logfile.Log(sql);
                UpdateTeslalogger.AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql);
                Logfile.Log("CREATE TABLE OK");
            }
        }

        private static void CheckDBSchema_areaa()
        {
            if (!DBHelper.TableExists("active_route_energy_at_arrival"))
            {
                Logfile.Log("CREATE TABLE active_route_energy_at_arrival (posID INT NOT NULL, val TINYINT NOT NULL);");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("CREATE TABLE active_route_energy_at_arrival (posID INT NOT NULL, val TINYINT NOT NULL);");
            }
        }

        internal static void AssertAlterDB()
        {
            // make sure there is enough disk space available for temp tables

            long largestTableMB = getLargestTableMB();
            Tools.DebugLog($"UpdateTeslalogger largestTableMB:{largestTableMB}");
            Tools.CleanupBackupFolder((long)(largestTableMB * 1.5), 3);
        }

        private static long getLargestTableMB()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
  ROUND((DATA_LENGTH + INDEX_LENGTH) / 1024 / 1024) AS `Size (MB)`
FROM
  information_schema.TABLES
WHERE
  TABLE_SCHEMA = @dbschema
ORDER BY
  (DATA_LENGTH + INDEX_LENGTH)
DESC
LIMIT 1", con))
                    {
                        cmd.Parameters.AddWithValue("@dbschema", DBHelper.Database);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read())
                        {
                            if (long.TryParse(dr[0].ToString(), out long largestTableMB))
                            {
                                return largestTableMB;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log("getLargestTableOrDBMB: " + ex.ToString());
            }
            return -1;
        }

        private static void CheckDBSchema_TPMS()
        {
            
            if (!DBHelper.TableExists("TPMS"))
            {
                string sql = @"CREATE TABLE `TPMS` (
                `CarId` INT NOT NULL,
                `Datum` DATETIME NOT NULL,
                `TireId` INT NOT NULL,
                `Pressure` DOUBLE NOT NULL,
                PRIMARY KEY(`CarId`, `Datum`, `TireId`)); ";

                Logfile.Log(sql);
                UpdateTeslalogger.AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql);
                Logfile.Log("CREATE TABLE OK");
            }

            if (!DBHelper.IndexExists("IX_TPMS_CarId_Datum", "TPMS"))
            {
                var sql = "create index IX_TPMS_CarId_Datum on TPMS(CarId, Tireid, Datum, pressure)";
                Logfile.Log(sql);
                UpdateTeslalogger.AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 600);
                KVS.InsertOrUpdate(TPMSSchemaVersion, (int)2);
            }
        }

        private static void CheckDBViews()
        {
            string viewtrip = string.Empty;
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SHOW FULL TABLES", con))
                    {
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read())
                        {
                            if (dr[0] != null && dr[0].ToString().Equals("trip") && dr[1] != null)
                            {
                                viewtrip = dr[1].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log("CheckDBViews exception: " + ex.ToString());
            }
            Logfile.Log("CheckDBViews: trip " + (viewtrip.Equals("VIEW") ? "OK" : $"NOT OK: type {viewtrip}"));
        }

        private static void CheckDBSchema_superchargerstate()
        {
            
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
               
        }

        private static void CheckDBSchema_superchargers()
        {
            
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
                
        }

        private static void CheckDBSchema_candata()
        {
            // empty so far
        }

        private static void CheckDBSchema_state()
        {
            InsertCarID_Column("state");   
        }

        private static void CheckDBSchema_shiftstate()
        {
            // this table is currently unused
            // InsertCarID_Column("shiftstate");
        }

        private static void CheckDBSchema_pos()
        {
            if (!DBHelper.ColumnExists("pos", "battery_level"))
            {
                Logfile.Log("ALTER TABLE pos ADD COLUMN battery_level DOUBLE NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN battery_level DOUBLE NULL");
            }

            if (!DBHelper.ColumnExists("pos", "inside_temp"))
            {
                Logfile.Log("ALTER TABLE pos ADD COLUMN inside_temp DOUBLE NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN inside_temp DOUBLE NULL", 300);
            }

            if (!DBHelper.ColumnExists("pos", "battery_heater"))
            {
                Logfile.Log("ALTER TABLE pos ADD COLUMN battery_heater TINYINT(1) NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN battery_heater TINYINT(1) NULL", 300);
            }

            if (!DBHelper.ColumnExists("pos", "is_preconditioning"))
            {
                Logfile.Log("ALTER TABLE pos ADD COLUMN is_preconditioning TINYINT(1) NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN is_preconditioning TINYINT(1) NULL", 300);
            }

            if (!DBHelper.ColumnExists("pos", "sentry_mode"))
            {
                Logfile.Log("ALTER TABLE pos ADD COLUMN sentry_mode TINYINT(1) NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN sentry_mode TINYINT(1) NULL", 300);
            }

            if (!DBHelper.ColumnExists("pos", "battery_range_km"))
            {
                Logfile.Log("ALTER TABLE pos ADD COLUMN battery_range_km DOUBLE NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN battery_range_km DOUBLE NULL", 600);
            }

            InsertCarID_Column("pos");

            // check datetime precision in pos
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT datetime_precision FROM INFORMATION_SCHEMA.COLUMNS WHERE table_name = 'pos' AND COLUMN_NAME = 'datum' and TABLE_SCHEMA = 'teslalogger'", con))
                    {
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (int.TryParse(dr[0].ToString(), out int datetime_precision))
                            {
                                if (datetime_precision != 3)
                                {
                                    // update table
                                    Logfile.Log("ALTER TABLE `pos` CHANGE `Datum` `Datum` DATETIME(3) NOT NULL;");
                                    AssertAlterDB();
                                    DBHelper.ExecuteSQLQuery(@"ALTER TABLE `pos` CHANGE `Datum` `Datum` DATETIME(3) NOT NULL;", 3000);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }

            if (!DBHelper.ColumnExists("pos", "AP"))
            {
                Logfile.Log("ALTER TABLE pos ADD COLUMN AP TINYINT(1) NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE pos ADD COLUMN AP TINYINT(1) NULL", 300);

                new Thread(() =>
                {
                    while (Car.Allcars.Count == 0)
                    {
                        Thread.Sleep(1000);
                    }
                    Thread.Sleep(5000);
                    for (int x=0; x<Car.Allcars.Count; x++)
                    {
                        Car c = Car.Allcars[x];
                        DBHelper.UpdateAllPOS_AP_Column(c.CarInDB, new DateTime(2024, 2, 1), DateTime.Now);
                    }
                    
                }).Start();
            }
        }

        private static void CheckDBSchema_mothershipcommands()
        {
            if (!DBHelper.TableExists("mothershipcommands"))
            {
                Logfile.Log("CREATE TABLE mothershipcommands (id int NOT NULL AUTO_INCREMENT, command varchar(50) NOT NULL, PRIMARY KEY(id))");
                DBHelper.ExecuteSQLQuery("CREATE TABLE mothershipcommands (id int NOT NULL AUTO_INCREMENT, command varchar(50) NOT NULL, PRIMARY KEY(id))");
                Logfile.Log("CREATE TABLE OK");
            }
            DBHelper.ExecuteSQLQuery("ALTER TABLE mothershipcommands MODIFY COLUMN command varchar(1024) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL;");
        }

        private static void CheckDBSchema_mothership()
        {
            if (!DBHelper.TableExists("mothership"))
            {
                Logfile.Log("CREATE TABLE mothership (id int NOT NULL AUTO_INCREMENT, ts datetime NOT NULL, commandid int NOT NULL, duration DOUBLE NULL, PRIMARY KEY(id))");
                DBHelper.ExecuteSQLQuery("CREATE TABLE mothership (id int NOT NULL AUTO_INCREMENT, ts datetime NOT NULL, commandid int NOT NULL, duration DOUBLE NULL, PRIMARY KEY(id))");
                Logfile.Log("CREATE TABLE OK");
            }
            if (!DBHelper.ColumnExists("mothership", "httpcode"))
            {
                Logfile.Log("ALTER TABLE mothership ADD COLUMN httpcode int NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE mothership ADD COLUMN httpcode int NULL", 600);
            }
            if (!DBHelper.ColumnExists("mothership", "carid"))
            {
                Logfile.Log("ALTER TABLE mothership ADD COLUMN carid INT UNSIGNED NULL DEFAULT NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE mothership ADD COLUMN carid INT UNSIGNED NULL DEFAULT NULL", 6000);
            }
        }

        private static void CheckDBSchema_httpcodes()
        {
            if (!DBHelper.TableExists("httpcodes"))
            {
                Logfile.Log("CREATE TABLE httpcodes (id int NOT NULL, text varchar(50) NOT NULL, PRIMARY KEY(id))");
                DBHelper.ExecuteSQLQuery("CREATE TABLE httpcodes (id int NOT NULL, text varchar(50) NOT NULL, PRIMARY KEY(id))");
                Logfile.Log("CREATE TABLE OK");
                _ = Task.Factory.StartNew(() =>
                {
                    DBHelper.UpdateHTTPStatusCodes();
                    Logfile.Log("CheckDBSchema_httpcodes (Task) finished.");
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private static void CheckDBSchema_drivestate()
        {
            
            if (!DBHelper.ColumnExists("drivestate", "outside_temp_avg"))
            {
                Logfile.Log("ALTER TABLE drivestate ADD COLUMN outside_temp_avg DOUBLE NULL, ADD COLUMN speed_max INT NULL, ADD COLUMN power_max INT NULL, ADD COLUMN power_min INT NULL, ADD COLUMN power_avg DOUBLE NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE drivestate ADD COLUMN outside_temp_avg DOUBLE NULL, ADD COLUMN speed_max INT NULL, ADD COLUMN power_max INT NULL, ADD COLUMN power_min INT NULL, ADD COLUMN power_avg DOUBLE NULL");
                _ = Task.Factory.StartNew(() =>
                {
                    DBHelper.UpdateAllDrivestateData();
                    Logfile.Log("CheckDBSchema_drivestate (Task) finished.");
                }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }

            if (!DBHelper.ColumnExists("drivestate", "meters_up"))
            {
                string sql = "ALTER TABLE drivestate ADD meters_up DOUBLE NULL DEFAULT NULL";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);
            }
            if (!DBHelper.ColumnExists("drivestate", "meters_down"))
            {
                string sql = "ALTER TABLE drivestate ADD meters_down DOUBLE NULL DEFAULT NULL";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);
            }
            if (!DBHelper.ColumnExists("drivestate", "distance_up_km"))
            {
                string sql = "ALTER TABLE drivestate ADD distance_up_km DOUBLE NULL DEFAULT NULL";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);
            }
            if (!DBHelper.ColumnExists("drivestate", "distance_down_km"))
            {
                string sql = "ALTER TABLE drivestate ADD distance_down_km DOUBLE NULL DEFAULT NULL";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);
            }
            if (!DBHelper.ColumnExists("drivestate", "distance_flat_km"))
            {
                string sql = "ALTER TABLE drivestate ADD distance_flat_km DOUBLE NULL DEFAULT NULL";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);
            }
            if (!DBHelper.ColumnExists("drivestate", "height_max"))
            {
                string sql = "ALTER TABLE drivestate ADD height_max DOUBLE NULL DEFAULT NULL";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);
            }
            if (!DBHelper.ColumnExists("drivestate", "height_min"))
            {
                string sql = "ALTER TABLE drivestate ADD height_min DOUBLE NULL DEFAULT NULL";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);
            }

            InsertCarID_Column("drivestate");

            if (!DBHelper.ColumnExists("drivestate", "wheel_type"))
            {
                Logfile.Log("ALTER TABLE drivestate ADD Column wheel_type");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `drivestate` ADD COLUMN `wheel_type` VARCHAR(40) NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("drivestate", "AP_sec_sum"))
            {
                Logfile.Log("ALTER TABLE drivestate ADD Column AP_sec_sum");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `drivestate` ADD COLUMN `AP_sec_sum` int NULL DEFAULT NULL", 600);
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `drivestate` ADD COLUMN `AP_sec_max` int NULL DEFAULT NULL", 600);
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `drivestate` ADD COLUMN `TPMS_FL` double NULL DEFAULT NULL", 600);
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `drivestate` ADD COLUMN `TPMS_FR` double NULL DEFAULT NULL", 600);
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `drivestate` ADD COLUMN `TPMS_RL` double NULL DEFAULT NULL", 600);
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `drivestate` ADD COLUMN `TPMS_RR` double NULL DEFAULT NULL", 600);
            }
        }

        private static void UpdateAllDrivestateDateThread()
        {
            new Thread(() =>
            {
                while (Car.Allcars.Count == 0)
                {
                    Thread.Sleep(1000);
                }
                Thread.Sleep(5000);
                DBHelper.UpdateAllDrivestateData();
            }).Start();
        }

        private static void CheckDBSchema_chargingstate()
        {
            if (!DBHelper.ColumnExists("chargingstate", "conn_charge_cable"))
            {
                Logfile.Log("ALTER TABLE chargingstate ADD COLUMN conn_charge_cable varchar(50)");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE chargingstate ADD COLUMN conn_charge_cable varchar(50)", 300);
            }

            if (!DBHelper.ColumnExists("chargingstate", "fast_charger_brand"))
            {
                Logfile.Log("ALTER TABLE chargingstate ADD COLUMN fast_charger_brand varchar(50)");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE chargingstate ADD COLUMN fast_charger_brand varchar(50)", 300);
            }

            if (!DBHelper.ColumnExists("chargingstate", "fast_charger_type"))
            {
                Logfile.Log("ALTER TABLE chargingstate ADD COLUMN fast_charger_type varchar(50)");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE chargingstate ADD COLUMN fast_charger_type varchar(50)", 300);
            }

            if (!DBHelper.ColumnExists("chargingstate", "fast_charger_present"))
            {
                Logfile.Log("ALTER TABLE chargingstate ADD COLUMN fast_charger_present TINYINT(1)");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE chargingstate ADD COLUMN fast_charger_present TINYINT(1)", 300);
            }

            if (!DBHelper.ColumnExists("chargingstate", "max_charger_power"))
            {
                Logfile.Log("ALTER TABLE chargingstate ADD COLUMN max_charger_power int NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE chargingstate ADD COLUMN max_charger_power int NULL", 600);
            }

            if (!DBHelper.ColumnExists("chargingstate", "cost_total"))
            {
                Logfile.Log("ALTER TABLE chargingstate ADD Column cost_total");
                AssertAlterDB();
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
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `chargingstate` 
                    ADD COLUMN `cost_kwh_meter_invoice` DOUBLE NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("chargingstate", "meter_vehicle_kwh_start"))
            {
                string sql = "ALTER TABLE chargingstate ADD COLUMN meter_vehicle_kwh_start double NULL,  ADD COLUMN meter_vehicle_kwh_end double NULL, ADD COLUMN meter_utility_kwh_start double NULL, ADD COLUMN meter_utility_kwh_end double NULL, ADD COLUMN meter_utility_kwh_sum double NULL";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);
            }

            if (!DBHelper.ColumnExists("chargingstate", "hidden"))
            {
                string sql = "ALTER TABLE chargingstate ADD hidden BOOLEAN NOT NULL DEFAULT FALSE ";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);
            }
            if (!DBHelper.ColumnExists("chargingstate", "combined_into"))
            {
                string sql = "ALTER TABLE chargingstate ADD combined_into INT NULL DEFAULT NULL";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);
            }

            if (!DBHelper.ColumnExists("chargingstate", "meter_vehicle_kwh_sum"))
            {
                string sql = "ALTER TABLE chargingstate ADD meter_vehicle_kwh_sum DOUBLE NULL DEFAULT NULL";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);

                DBHelper.ExecuteSQLQuery("update chargingstate set meter_vehicle_kwh_sum = meter_vehicle_kwh_end - meter_vehicle_kwh_start where meter_vehicle_kwh_sum is null and meter_vehicle_kwh_start is not null and meter_vehicle_kwh_end is not null", 300);
                DBHelper.ExecuteSQLQuery("update chargingstate set cost_kwh_meter_invoice = meter_vehicle_kwh_end - meter_vehicle_kwh_start where cost_kwh_meter_invoice is null and meter_vehicle_kwh_start is not null and meter_vehicle_kwh_end is not null and charge_energy_added < (meter_vehicle_kwh_end - meter_vehicle_kwh_start)", 300);

                DBHelper.ExecuteSQLQuery("update chargingstate set meter_utility_kwh_sum = meter_utility_kwh_end - meter_utility_kwh_start where meter_utility_kwh_sum is null and meter_utility_kwh_start is not null and meter_utility_kwh_end is not null", 300);
            }

            InsertCarID_Column("chargingstate");

            if (!DBHelper.ColumnExists("chargingstate", "wheel_type"))
            {
                Logfile.Log("ALTER TABLE chargingstate ADD Column wheel_type");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `chargingstate` ADD COLUMN `wheel_type` VARCHAR(40) NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("chargingstate", "co2_g_kWh"))
            {
                Logfile.Log("ALTER TABLE chargingstate ADD Column co2_g_kWh");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `chargingstate` ADD COLUMN `co2_g_kWh` int NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("chargingstate", "country"))
            {
                Logfile.Log("ALTER TABLE chargingstate ADD Column country");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `chargingstate` ADD COLUMN `country` varchar(80) NULL DEFAULT NULL", 600);
            }
        }

        private static void CheckDBSchema_charging()
        {
            if (!DBHelper.ColumnExists("charging", "charger_pilot_current"))
            {
                Logfile.Log("ALTER TABLE charging ADD COLUMN charger_pilot_current INT NULL, ADD COLUMN charge_current_request INT NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE charging ADD COLUMN charger_pilot_current INT NULL, ADD COLUMN charge_current_request INT NULL");
            }

            if (!DBHelper.ColumnExists("charging", "battery_heater"))
            {
                Logfile.Log("ALTER TABLE charging ADD COLUMN battery_heater TINYINT(1) NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE charging ADD COLUMN battery_heater TINYINT(1) NULL", 600);
            }

            if (!DBHelper.ColumnExists("charging", "battery_range_km"))
            {
                Logfile.Log("ALTER TABLE charging ADD COLUMN battery_range_km DOUBLE NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE charging ADD COLUMN battery_range_km DOUBLE NULL", 600);
            }

            if (!DBHelper.ColumnExists("charging", "charger_actual_current_calc"))
            {
                Logfile.Log("ALTER TABLE charging ADD COLUMN charger_actual_current_calc INT NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE charging ADD COLUMN charger_actual_current_calc INT NULL");
            }

            if (!DBHelper.ColumnExists("charging", "charger_phases_calc"))
            {
                Logfile.Log("ALTER TABLE charging ADD COLUMN charger_phases_calc TINYINT(1) NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE charging ADD COLUMN charger_phases_calc TINYINT(1) NULL", 600);
            }

            if (!DBHelper.ColumnExists("charging", "charger_power_calc_w"))
            {
                Logfile.Log("ALTER TABLE charging ADD COLUMN charger_power_calc_w INT NULL");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("ALTER TABLE charging ADD COLUMN charger_power_calc_w INT NULL", 600);
            }

            InsertCarID_Column("charging"); 
        }

        private static void CheckDBSchema_car_version()
        {
            if (!DBHelper.TableExists("car_version"))
            {
                Logfile.Log("CREATE TABLE car_version (id int NOT NULL AUTO_INCREMENT, StartDate datetime NOT NULL, version varchar(50), PRIMARY KEY(id))");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("CREATE TABLE car_version (id int NOT NULL AUTO_INCREMENT, StartDate datetime NOT NULL, version varchar(50), PRIMARY KEY(id))");
            }

            InsertCarID_Column("car_version");
        }

        private static void CheckDBSchema_cars()
        {
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
                            _ = SQLTracer.TraceNQ(cmd, out _);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    Logfile.Log(ex.ToString());
                }
            }

            if (!DBHelper.ColumnExists("cars", "vin"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column vin");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` 
                    ADD COLUMN `vin` VARCHAR(20) NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("cars", "freesuc"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column freesuc");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD `freesuc` TINYINT UNSIGNED NOT NULL DEFAULT '0'", 600);
            }

            if (!DBHelper.ColumnExists("cars", "lastscanmytesla"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column lastscanmytesla");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD COLUMN `lastscanmytesla` datetime NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("cars", "refresh_token"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column refresh_token");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD COLUMN `refresh_token` TEXT NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("cars", "ABRP_token"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column ABRP_token");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD COLUMN `ABRP_token` VARCHAR(40) NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("cars", "ABRP_mode"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column ABRP_mode");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD COLUMN `ABRP_mode` TINYINT(1) NULL DEFAULT 0", 600);
            }

            if (!DBHelper.ColumnExists("cars", "SuCBingo_user"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column SuCBingo_user");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD COLUMN `SuCBingo_user` VARCHAR(40) NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("cars", "SuCBingo_apiKey"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column SuCBingo_apiKey");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD COLUMN `SuCBingo_apiKey` VARCHAR(100) NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("cars", "meter_type"))
            {
                string sql = "ALTER TABLE cars ADD COLUMN meter_type varchar(20) NULL, ADD COLUMN meter_host varchar(50) NULL, ADD COLUMN meter_parameter varchar(200) NULL";
                Logfile.Log(sql);
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(sql, 300);
            }

            if (DBHelper.GetColumnType("cars", "tesla_token").Contains("varchar"))
            {
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("alter table cars modify tesla_token TEXT NULL", 120);
            }

            if (!DBHelper.ColumnExists("cars", "wheel_type"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column wheel_type");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD COLUMN `wheel_type` VARCHAR(40) NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("cars", "fleetAPI"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column fleetAPI");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD `fleetAPI` TINYINT UNSIGNED NOT NULL DEFAULT '0'", 600);
            }

            if (!DBHelper.ColumnExists("cars", "fleetAPIaddress"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column fleetAPIaddress");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD `fleetAPIaddress` VARCHAR(200) NULL DEFAULT NULL", 600);
            }

            if (!DBHelper.ColumnExists("cars", "oldAPIchinaCar"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column oldAPIchinaCar");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD `oldAPIchinaCar` TINYINT UNSIGNED NOT NULL DEFAULT '0'", 600);
            }

            if (!DBHelper.ColumnExists("cars", "needVirtualKey"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column needVirtualKey");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD `needVirtualKey` TINYINT UNSIGNED NOT NULL DEFAULT '0'", 600);
            }

            if (!DBHelper.ColumnExists("cars", "needCommandPermission"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column needCommandPermission");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD `needCommandPermission` TINYINT UNSIGNED NOT NULL DEFAULT '0'", 600);
            }

            if (!DBHelper.ColumnExists("cars", "needFleetAPI"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column needFleetAPI");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD `needFleetAPI` TINYINT UNSIGNED NOT NULL DEFAULT '0'", 600);
            }

            if (!DBHelper.ColumnExists("cars", "access_type"))
            {
                Logfile.Log("ALTER TABLE cars ADD Column access_type");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD `access_type` varchar(20) NULL", 600);
            }

            if (!DBHelper.ColumnExists("cars", "virtualkey")) 
            {
                Logfile.Log("ALTER TABLE cars ADD Column virtualkey");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery(@"ALTER TABLE `cars` ADD `virtualkey` TINYINT UNSIGNED  NULL DEFAULT '0'", 600);
            }
        }

        private static void CheckDBSchema_can()
        {
            if (!DBHelper.TableExists("can"))
            {
                Logfile.Log("CREATE TABLE `can` (`datum` datetime NOT NULL, `id` mediumint NOT NULL, `val` double DEFAULT NULL, PRIMARY KEY(`datum`,`id`) ) ENGINE = InnoDB DEFAULT CHARSET = latin1;");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery("CREATE TABLE `can` (`datum` datetime NOT NULL, `id` mediumint NOT NULL, `val` double DEFAULT NULL, PRIMARY KEY(`datum`,`id`) ) ENGINE = InnoDB DEFAULT CHARSET = latin1;");
            }

            InsertCarID_Column("can");
        }

        public static string UpdateApacheConfig(string path = "/etc/apache2/apache2.conf", bool write = true)
        {
            if (!File.Exists(path))
                return "";

            string temp = File.ReadAllText(path);
            if (temp.Contains("<Directory /var/www/>"))
            {
                string pattern = "(<Directory \\/var\\/www\\/>)(.+?)(AllowOverride [A-Za-z]+)(.+?)(<\\/Directory>)";
                Regex r = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Singleline);
                var m = r.Match(temp);
                if (!m.Success)
                {
                    Logfile.Log("Apache Config AllowOverride not found!!!");
                    return temp;
                }
                else if (m.Groups.Count != 6 || m.Groups[3].Value != "AllowOverride All")
                {
                    string oldValue = "";
                    if (m.Groups.Count > 3)
                        oldValue = m.Groups[3].Value;

                    Logfile.Log("Apache Config changed! Old: " + oldValue);
                }

                temp = r.Replace(temp, "$1$2AllowOverride All$4$5");
            }

            if (write)
                File.WriteAllText(path, temp);

            return temp;
        }

        public static async void DownloadUpdateAndInstall()
        {
            DownloadUpdateAndInstallStarted = true;
            CheckNET8Installed();

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
            ExceptionlessClient.Default.CreateLog("Install", "Start update from " + Assembly.GetExecutingAssembly().GetName().Version).Submit();

            if (Tools.IsMono())
            {
                Chmod("VERSION", 666);
                Chmod("settings.json", 666);
                Chmod("cmd_updated.txt", 666);
                Chmod("MQTTClient.exe.config", 666);

                if (!File.Exists("NOBACKUPONUPDATE"))
                {
                    Logfile.Log("Create backup");
                    Tools.ExecMono("/bin/bash", "/etc/teslalogger/backup.sh");
                }

                if (!Tools.ExecMono("git", "--version", false).Contains("git version"))
                {
                    Tools.ExecMono("apt-get", "-y install git");
                    Tools.ExecMono("git", "--version");
                }

                if (!File.Exists("/usr/bin/optipng") || !Tools.ExecMono("optipng", "-version", false).Contains("OptiPNG version"))
                {
                    Logfile.Log("Try to install optipng");
                    ExceptionlessClient.Default.CreateLog("Install", "Try to install optipng", Exceptionless.Logging.LogLevel.Warn).Submit();

                    if (Tools.ExecMono("apt-get", "-y install optipng", false).Contains("apt --fix-broken"))
                    {
                        Logfile.Log("Info: apt-get cannot install optipng");
                    }
                    else
                    {
                        string ret = Tools.ExecMono("optipng", "-version");
                        if (ret == null)
                            ret = "NULL";

                        ExceptionlessClient.Default.CreateLog("Install", "optipng: " + ret, Exceptionless.Logging.LogLevel.Warn).Submit();
                    }
                }

                Tools.ExecMono("rm", "-rf /etc/teslalogger/git/*");

                Tools.ExecMono("rm", "-rf /etc/teslalogger/git");
                Tools.ExecMono("mkdir", "/etc/teslalogger/git");
                CertUpdate();

                // run housekeeping to make sure there is enough free disk space

                Tools.Housekeeping();

                // download update package from github
                // download update package from github
                bool httpDownloadSuccessful = false;
                bool zipExtractSuccessful = false;
                string GitHubURL = "https://github.com/bassmaster187/TeslaLogger/archive/master.zip";
                string master = "master";

                if (File.Exists("BRANCH"))
                {
                    var branch = File.ReadAllText("BRANCH").Trim();

                    if (WebHelper.BranchExists(branch, out HttpStatusCode statusCode))
                    {
                        Logfile.Log($"YOU ARE USING BRANCH: " + branch);

                        GitHubURL = "https://github.com/bassmaster187/TeslaLogger/archive/refs/heads/" + branch + ".zip";
                        master = branch;
                    }
                    else
                    {
                        Logfile.Log($"BRANCH NOT EXIST: " + branch);

                        if (statusCode == HttpStatusCode.NotFound)
                        {
                            File.Delete("BRANCH");
                            Logfile.Log("BRANCH file deleted!");
                        }
                    }
                }


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

                        ExceptionlessClient.Default.CreateLog("Install", "Update Download successful").Submit();
                    }
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless().FirstCarUserID().Submit();
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
                            if (Directory.Exists("/etc/teslalogger/tmp/zip/TeslaLogger-" + master))
                            {
                                Logfile.Log($"move update files from /etc/teslalogger/tmp/zip/TeslaLogger-" + master + " to /etc/teslalogger/git");
                                Tools.ExecMono("mv", "/etc/teslalogger/tmp/zip/TeslaLogger-" + master + " /etc/teslalogger/git");
                                if (Directory.Exists("/etc/teslalogger/git/TeslaLogger/GrafanaPlugins"))
                                {
                                    Logfile.Log("update package: download and unzip successful");
                                    zipExtractSuccessful = true;

                                    ExceptionlessClient.Default.CreateLog("Install", "Update Zip Extract Successful").FirstCarUserID().Submit();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.ToExceptionless().FirstCarUserID().Submit();
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
                        Tools.ExecMono("git", "clone --depth=1 --progress https://github.com/bassmaster187/TeslaLogger /etc/teslalogger/git/", true, true);

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
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    Logfile.Log(ex.ToString());
                }

                // running in TeslaLogger.exe, prepare update in separate process
                if (System.Diagnostics.Process.GetCurrentProcess().ProcessName.Equals("TeslaLogger"))
                {
                    try
                    {
                        Tools.CopyFile("/etc/teslalogger/git/TeslaLogger/bin/TLUpdate.exe", "/etc/teslalogger/TLUpdate.exe");
                        foreach (Car car in Car.Allcars)
                        {
                            car.CurrentJSON.ToKVS();
                        }
                        ExceptionlessClient.Default.CreateLog("Install", "Update finished!").FirstCarUserID().Submit();
                        await ExceptionlessClient.Default.ProcessQueueAsync();
                        using (Process process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "/etc/teslalogger/TLUpdate.exe",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                CreateNoWindow = true
                            }
                        })
                        {
                            Logfile.Log(" *** starting TLUpdate.exe now ***");
                            process.Start();
                            while (!process.StandardOutput.EndOfStream)
                            {
                                Logfile.Log(process.StandardOutput.ReadLine());
                            }
                            process.WaitForExit();
                        }
                    }
                    catch (Exception ex)
                    {
                        ex.ToExceptionless().FirstCarUserID().Submit();
                        Logfile.Log(ex.ToString());
                    }
                }

            }
        }

        private static void CheckNET8Installed()
        {
            try
            {
                if (Tools.IsMono() && !Tools.IsDocker())
                {
                    var net8version = Tools.GetNET8Version();
                    if (net8version?.Contains("8.") == true)
                    {
                        ExceptionlessClient.Default.CreateFeatureUsage("DOTNET8").FirstCarUserID().AddObject(net8version, "DOTNET8").Submit();
                    }
                    else
                    {
                        var t = new Thread(() =>
                        {
                            Logfile.Log("Install .NET 8");

                            Tools.ExecMono("wget", "https://dot.net/v1/dotnet-install.sh -O /home/dotnet-install.sh");
                            UpdateTeslalogger.Chmod("/home/dotnet-install.sh", 777, true);
                            Tools.ExecMono("/bin/bash", "/home/dotnet-install.sh --runtime aspnetcore --channel 8.0 --install-dir /home/cli");
                            //Tools.ExecMono("export", "export DOTNET_ROOT=/home/cli");
                            //Tools.ExecMono("export", "export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools");

                            var temp = Tools.GetNET8Version();
                            if (temp?.Contains("8.") == true)
                            {
                                Logfile.Log(".NET 8 installed: " + temp);
                                ExceptionlessClient.Default.CreateFeatureUsage("DOTNET8").FirstCarUserID().AddObject(temp, "DOTNET8").Submit();
                            }
                        });

                        t.Name = "DOTNET8InstallThread";
                        t.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
                ex.ToExceptionless().FirstCarUserID().Submit();
            }
        }

        public static void CertUpdate()
        {
            try
            {
                // https://github.com/KSP-CKAN/CKAN/wiki/SSL-certificate-errors#removing-expired-lets-encrypt-certificates
                Tools.ExecMono("sed", "-i 's/^mozilla\\/DST_Root_CA_X3.crt$/!mozilla\\/DST_Root_CA_X3.crt/' /etc/ca-certificates.conf");
                Tools.ExecMono("update-ca-certificates", "");
                Tools.ExecMono("cert-sync", "/etc/ssl/certs/ca-certificates.crt");
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
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
                ex.ToExceptionless().FirstCarUserID().Submit();
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
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery($"ALTER TABLE `{table}` ADD COLUMN `CarID` TINYINT NULL DEFAULT NULL", 6000);
                DBHelper.ExecuteSQLQuery($"update {table} set CarID=1", 6000);
            }
            if (DBHelper.GetColumnType(table, "CarID") == "tinyint")
            {
                Logfile.Log($"ALTER TABLE `{table}` MODIFY `CarID` INT UNSIGNED");
                AssertAlterDB();
                DBHelper.ExecuteSQLQuery($"ALTER TABLE `{table}` MODIFY `CarID` INT UNSIGNED", 6000);
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
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read())
                        {
                            string charset = dr[0].ToString();

                            if (charset != "utf8mb4")
                            {
                                dr.Close();

                                Logfile.Log("Change database charset to utf8mb4");
                                AssertAlterDB();
                                using (var cmd2 = new MySqlCommand("ALTER DATABASE teslalogger CHARACTER SET = utf8mb4 COLLATE = utf8mb4_unicode_ci", con))
                                {
                                    _ = SQLTracer.TraceNQ(cmd2, out _);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        private static void UpdatePHPini()
        {
            try
            {
                string phpinipath = "/etc/php/7.0/apache2/php.ini";

                if (!File.Exists(phpinipath))
                    phpinipath = "/etc/php/7.3/apache2/php.ini";

                if (File.Exists(phpinipath))
                {
                    string phpini = File.ReadAllText(phpinipath);
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
                ex.ToExceptionless().FirstCarUserID().Submit();
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
                    foreach (Car c in Car.Allcars)
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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        private static void UpdateDBViews()
        {
            try
            {
                Logfile.Log("UpdateDBViews: trip");
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

                ExceptionlessClient.Default.CreateFeatureUsage("Language_" + language).FirstCarUserID().Submit();
                ExceptionlessClient.Default.CreateFeatureUsage("Power_" + power).FirstCarUserID().Submit();
                ExceptionlessClient.Default.CreateFeatureUsage("Temperature_" + temperature).FirstCarUserID().Submit();
                ExceptionlessClient.Default.CreateFeatureUsage("Length_" + length).FirstCarUserID().Submit();
                ExceptionlessClient.Default.CreateFeatureUsage("Range_" + Range).FirstCarUserID().Submit();
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
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

                        if (line.StartsWith("#", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (!line.Contains("="))
                        {
                            continue;
                        }

                        int pos = line.IndexOf("=", StringComparison.Ordinal);
                        string key = line.Substring(0, pos).Trim();
                        string value = line.Substring(pos + 1);

                        // Logfile.Log("Key insert: " + key);

                        if (value.StartsWith("\"") && value.EndsWith("\""))
                        {
                            value = value.Substring(1, value.Length - 2);
                        }

                        value = value.Replace("\"_QQ_\"", "\"");

                        if (ht.ContainsKey(key))
                        {
                            Logfile.Log($"INFO: Key '{key}' already in Language Dictionary!");
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
                    ex.ToExceptionless().FirstCarUserID().Submit();
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

                    if (!Tools.IsDocker())
                    {
                        AllowUnsignedPlugins("/etc/grafana/grafana.ini", true);
                    }

                    UpdateGrafanaVersion();

                    // TODO Logfile.Log(" Wh/TR km: " + wh.car.Wh_TR);

                    Tools.ExecMono("rm", "-rf /etc/teslalogger/tmp/*");
                    Tools.ExecMono("rm", "-rf /etc/teslalogger/tmp");

                    Tools.ExecMono("mkdir", "/etc/teslalogger/tmp");
                    Tools.ExecMono("mkdir", "/etc/teslalogger/tmp/Grafana");

                    bool useNewTrackmapPanel = Directory.Exists("/var/lib/grafana/plugins/pR0Ps-grafana-trackmap-panel");

                    var DatasourceUID = "000000001";
                    if (Tools.IsDocker())
                        DatasourceUID = "PC0C98BF192F75B00";

                    UpdateDBViews();

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
                            if (!(f.EndsWith("Akku Trips.json", StringComparison.Ordinal) || f.EndsWith("Speed Consumption.json", StringComparison.Ordinal)))
                            {
                                s = s.Replace("ideal_battery_range_km", "battery_range_km");
                            }
                        }

                        if (power == "kw")
                        {
                            //Logfile.Log("Convert to kw");

                            if (f.EndsWith("Verbrauch.json", StringComparison.Ordinal))
                            {
                                s = s.Replace("power as 'Leistung [PS]'", "power/1.35962 as 'Leistung [kW]'");
                            }
                            else if (f.EndsWith("Trip.json", StringComparison.Ordinal))
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

                            if (f.EndsWith("Laden.json", StringComparison.Ordinal))
                            {
                                s = s.Replace("outside_temp as 'Außentemperatur [°C]'", "outside_temp * 9/5 + 32 as 'Außentemperatur [°F]'");
                            }
                            else if (f.EndsWith("Trip.json", StringComparison.Ordinal))
                            {
                                s = s.Replace("Ø °C", "Ø °F");

                                s = s.Replace(" outside_temp_avg", "outside_temp_avg * 9/5 + 32 as outside_temp_avg");
                            }
                            else if (f.EndsWith("Verbrauch.json", StringComparison.Ordinal))
                            {
                                s = s.Replace("outside_temp as 'Außentemperatur [°C]'", "outside_temp * 9/5 + 32 as 'Außentemperatur [°F]'");
                            }
                        }

                        if (length == "mile")
                        {
                            //Logfile.Log("Convert to mile");

                            if (f.EndsWith("Akku Trips.json", StringComparison.Ordinal))
                            {
                                s = s.Replace("Start km", "Start mi");
                                s = s.Replace("End km", "End mi");

                                s = s.Replace("EndOdometer - StartOdometer AS kmDiff", "(EndOdometer - StartOdometer) / 1.609344 AS kmDiff");
                                s = s.Replace("StartOdometer,", " StartOdometer / 1.609344 as StartOdometer,");
                                s = s.Replace("EndOdometer,", " EndOdometer / 1.609344 as EndOdometer,");
                                s = s.Replace("100 AS MaxRange", "100 / 1.609344 AS MaxRange");
                                s = s.Replace("(EndOdometer - StartOdometer) * 100 AS AVGConsumption", "(EndOdometer/1.609344 - StartOdometer/1.609344) * 100 AS AVGConsumption");

                                s = s.Replace("\"unit\": \"lengthkm\"", "\"unit\": \"lengthmi\"");
                            }
                            else if (f.EndsWith("Degradation.json", StringComparison.Ordinal))
                            {
                                s = s.Replace(" as 'Maximalreichweite [km]'", " / 1.609344 as 'Maximalreichweite [mi]'");
                                s = s.Replace(" AS 'Max. Reichweite (Monatsmittel) [km]'", " / 1.609344 AS 'Max. Reichweite (Monatsmittel) [mi]'");
                                s = s.Replace("odometer as 'km Stand [km]'", "odometer / 1.609344 as 'mi Stand [mi]'");
                                s = s.Replace("km Stand [km]", "mi Stand [mi]");

                            }
                            else if (f.EndsWith("Laden.json", StringComparison.Ordinal))
                            {
                                s = s.Replace(" as 'Reichweite [km]',", " / 1.609344 as 'Reichweite [mi]',");

                                s = s.Replace("Reichweite [km]", "Reichweite [mi]");
                            }
                            else if (f.EndsWith("Trip.json", StringComparison.Ordinal))
                            {
                                s = s.Replace(" speed_max,", "speed_max / 1.609344 as speed_max,");
                                s = s.Replace(" avg_consumption_kWh_100km,", " avg_consumption_kWh_100km * 1.609344 as avg_consumption_kWh_100km,");
                                s = s.Replace(" as avg_kmh", " / 1.609344 as avg_kmh");
                                s = s.Replace(" km_diff,", " km_diff  / 1.609344 as km_diff,");
                                s = s.Replace("StartRange - EndRange as RangeDiff", "(StartRange - EndRange) / 1.609344 as RangeDiff");

                                s = s.Replace("\"max km/h\"", "\"max mph\"");
                                s = s.Replace("\"Ø km/h\"", "\"Ø mph\"");
                                s = s.Replace("\"km\"", "\"mi\"");
                            }
                            else if (f.EndsWith("Vampir Drain.json", StringComparison.Ordinal))
                            {
                                s = s.Replace(" TP2.odometer,", " TP2.odometer / 1.609344 as odometer,");
                                s = s.Replace("ideal_battery_range_km ", "ideal_battery_range_km / 1.609344 ");

                                s = s.Replace("\"km Stand\"", "\"mi Stand\"");
                                s = s.Replace("\"TR km Start\"", "\"TR mi Start\"");
                                s = s.Replace("\"TR km Ende\"", "\"TR mi Ende\"");
                                s = s.Replace("\"TR km Verlust\"", "\"TR mi Verlust\"");
                                s = s.Replace("\"TR km Verlust pro Stunde\"", "\"TR mi Verlust pro Stunde\"");
                            }
                            else if (f.EndsWith("Vampir Drain Monatsstatistik.json", StringComparison.Ordinal))
                            {
                                s = s.Replace(" as RangeLost", " / 1.609344 as RangeLost");

                                s = s.Replace("TR km Verlust", "TR mi Verlust");
                            }
                            else if (f.EndsWith("Verbrauch.json", StringComparison.Ordinal))
                            {
                                s = s.Replace(" speed as 'Geschwindigkeit [km/h]'", " speed / 1.609344 as 'Geschwindigkeit [mph]'");
                                s = s.Replace(" ideal_battery_range_km as 'Reichweite [km]'", " ideal_battery_range_km / 1.609344 as 'Reichweite [mi]'");
                            }
                            else if (f.EndsWith("Ladehistorie.json", StringComparison.Ordinal))
                            {
                                s = s.Replace("ideal_battery_range_km ", "ideal_battery_range_km / 1.609344 ");

                                s = s.Replace("\"TR km Start\"", "\"TR mi Start\"");
                                s = s.Replace("\"TR km Ende\"", "\"TR mi Ende\"");
                            }
                        }

                        if (language != "de")
                        {
                            Logfile.Log("Convert to language: " + language);

                            s = ReplaceAliasTags(s, dictLanguage);
                            s = ReplaceValuesTags(s, dictLanguage);

                            if (f.EndsWith("Akku Trips.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Akku Trips", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "AVG Max Range","AVG Consumption","AVG Trip Days","AVG SOC Diff"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Alle Verbräuche - ScanMyTesla.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Alle Verbräuche - ScanMyTesla", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {"Außentemperatur [°C]", "Außentemperatur [°F]",
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Degradation.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Degradation", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Maximalreichweite [km]", "Maximalreichweite [mi]","mi Stand [mi]","km Stand [km]","Max. Reichweite (Monatsmittel) [km]","Max. Reichweite (Monatsmittel) [mi]"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Firmware.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Degradation", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Firmware","Date Installed","Days since previous update","Min Days Between Updates","AVG Days Between Updates","Max Days Between Updates"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Ladehistorie.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Ladehistorie", dictLanguage);
                            }
                            else if (f.EndsWith("Laden.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Laden", dictLanguage);

                                s = ReplaceLanguageTags(s, new string[] {
                                    "SOC [%]", "Leistung [PS]", "Leistung [kW]", "Reichweite [mi]", "Reichweite [km]", "Ladespannung [V]", "Phasen",
                                    "Stromstärke [A]", "Außentemperatur [°C]", "Außentemperatur [°F]",
                                    "Angefordert [A]", "Pilot [A]", "Zelltemperatur [°C]", "Zelltemperatur [°F]"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Speed Consumption.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Speed Consumption", dictLanguage);
                            }
                            else if (f.EndsWith("Status.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Status", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Current Status","SOC","Reichweite","Außentemperatur","Zelltemperatur","km Stand","Firmware","Nur verfügbar mit ScanMyTesla","N/A","Asleep","Online","Offline","Waking","Driving","Charging"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Trip.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Trip", dictLanguage);
                            }
                            else if (f.EndsWith("Vampir Drain.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Vampir Drain", dictLanguage);
                            }
                            else if (f.EndsWith("Vampir Drain Monatsstatistik.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Vampir Drain Monatsstatistik", dictLanguage);
                            }
                            else if (f.EndsWith("Verbrauch.json", StringComparison.Ordinal))
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
                            else if (f.EndsWith("Verbrauchsstatstik.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Verbrauchsstatistik", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "km Stand[km]","mi Stand [mi]","Verbrauch Monatsmittel [kWh]","Außentemperatur Monatsmittel [°C]","Außentemperatur Monatsmittel [°F]","Verbrauch Tagesmittel [kWh]","Außentemperatur Tagesmittel [°C]", "Außentemperatur Tagesmittel [°F]"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Visited.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Visited", dictLanguage);
                            }
                            else if (f.EndsWith("km Stand.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "km Stand", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "km Stand [km]"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Ladestatistik.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Ladestatistik", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Anzahl", "SOC Ladestatistik", "SOC Entladestatistik", "Geladen", "Ladezeit", "Anz. Ladungen", "AC", "DC", "PV", "Ladehub"
                                }, dictLanguage, true);

                                s = ReplaceLanguageTags(s, new string[] {
                                    "Anzahl", "SOC Entladestatistik"
                                }, dictLanguage, false);
                            }
                            else if (f.EndsWith("SOC Ladestatistik.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "SOC Ladestatistik", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Anzahl", "SOC Ladestatistik"
                                }, dictLanguage, true);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Anzahl"
                                }, dictLanguage, false);

                            }
                            else if (f.EndsWith("SOC Entladestatistik.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "SOC Entladestatistik", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Anzahl", "SOC Entladestatistik"
                                }, dictLanguage, true);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Anzahl"
                                }, dictLanguage, false);
                            }
                            else if (f.EndsWith("Zellspannungen 01-20 - ScanMyTesla.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Zellspannungen 01-20 - ScanMyTesla", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Zellspannungen"
                                }, dictLanguage, true);

                                if (dictLanguage.ContainsKey("Zellspannung"))
                                {
                                    for (int x = 1; x < 99; x++)
                                        s = ReplaceLanguageTag(ref s, $"Zellspannung {x} [v]", dictLanguage["Zellspannung"] + " " + x + " [v]");
                                }
                            }
                            else if (f.EndsWith("Zellspannungen 21-40 - ScanMyTesla.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Zellspannungen 21-40 - ScanMyTesla", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Zellspannungen"
                                }, dictLanguage, true);

                                if (dictLanguage.ContainsKey("Zellspannung"))
                                {
                                    for (int x = 1; x < 99; x++)
                                        s = ReplaceLanguageTag(ref s, $"Zellspannung {x} [v]", dictLanguage["Zellspannung"] + " " + x + " [v]");
                                }
                            }
                            else if (f.EndsWith("Zellspannungen 41-60 - ScanMyTesla.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Zellspannungen 41-60 - ScanMyTesla", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Zellspannungen"
                                }, dictLanguage, true);

                                if (dictLanguage.ContainsKey("Zellspannung"))
                                {
                                    for (int x = 1; x < 99; x++)
                                        s = ReplaceLanguageTag(ref s, $"Zellspannung {x} [v]", dictLanguage["Zellspannung"] + " " + x + " [v]");
                                }
                            }
                            else if (f.EndsWith("Zellspannungen 61-80 - ScanMyTesla.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Zellspannungen 61-80 - ScanMyTesla", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Zellspannungen"
                                }, dictLanguage, true);

                                if (dictLanguage.ContainsKey("Zellspannung"))
                                {
                                    for (int x = 1; x < 99; x++)
                                        s = ReplaceLanguageTag(ref s, $"Zellspannung {x} [v]", dictLanguage["Zellspannung"] + " " + x + " [v]");
                                }
                            }
                            else if (f.EndsWith("Zellspannungen 81-99 - ScanMyTesla.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Zellspannungen 81-99 - ScanMyTesla", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Zellspannungen"
                                }, dictLanguage, true);

                                if (dictLanguage.ContainsKey("Zellspannung"))
                                {
                                    for (int x = 1; x < 99; x++)
                                        s = ReplaceLanguageTag(ref s, $"Zellspannung {x} [v]", dictLanguage["Zellspannung"] + " " + x + " [v]");
                                }
                            }
                            else if (f.EndsWith("Trip Monatsstatistik.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Trip Monatsstatistik", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Jahr/Monat", "Fahrzeit [h]", "Strecke [km]", "Strecke [mi]", "Verbrauch [kWh]", "Ø Verbrauch [kWh]"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Alle Verbräuche -ScanMyTesla.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Alle Verbräuche - ScanMyTesla", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Außentemperatur [°C]","Zelltemperatur [°C]","Alle Verbräuche - ScanMyTesla"
                                }, dictLanguage, true);
                            }
                            else if (f.EndsWith("Vehicle Alerts.json", StringComparison.Ordinal))
                            {
                                s = ReplaceTitleTag(s, "Fahrzeug Fehler", dictLanguage);
                                s = ReplaceLanguageTags(s, new string[] {
                                    "Fehler", "Häufigkeit"
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
                            if (!temp_URL.EndsWith("/", StringComparison.Ordinal))
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

                        s = UpdateDatasourceUID(s, DatasourceUID);

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
                        ex.ToExceptionless().FirstCarUserID().Submit();
                        Logfile.Log(ex.ToString());
                    }

                    Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/tmp/Grafana"), new DirectoryInfo("/var/lib/grafana/dashboards"));

                    if (!Tools.IsDocker())
                    {
                        Tools.ExecMono("grafana-cli", "admin data-migration encrypt-datasource-passwords");
                        Tools.ExecMono("service", "grafana-server restart");
                    }

                    CopyLanguageFileToTimelinePanel(language);

                    CopySettingsToTimelinePanel();
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            finally
            {
                Logfile.Log("End Grafana update");
            }
        }

        internal static string UpdateDatasourceUID(string s, string v)
        {
            string pattern = "(\\\"datasource\\\":\\s+{\\s+\\\"type\\\":\\s+\\\"mysql\\\",\\s+\\\"uid\\\":\\s+\\\")(.*?)(\\\")";
            Regex r = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Singleline);
            var m = r.Match(s);
            if (!m.Success)
            {
                // Logfile.Log("datasource not found!!!");
                return s;
            }

            s = r.Replace(s, "${1}" + v + "${3}");

            return s;
        }

        private static void UpdateGrafanaVersion()
        {
            string newversion = "10.0.1";

            string GrafanaVersion = Tools.GetGrafanaVersion();

            if (GrafanaVersion == "5.5.0-d3b39f39pre1" 
                || GrafanaVersion == "6.3.5" 
                || GrafanaVersion == "6.7.3" 
                || GrafanaVersion == "7.2.0" 
                || GrafanaVersion == "8.3.1" 
                || GrafanaVersion == "8.3.2"
                || GrafanaVersion == "8.5.22"
                )
            {
                if (!Tools.GetOsRelease().Contains("buster"))
                {
                    Logfile.Log("Grafana update suspended because of old OS:" + Tools.GetOsRelease());
                    ExceptionlessClient.Default.CreateFeatureUsage("Grafana update suspended").FirstCarUserID().Submit();
                    return;
                }

                Thread threadGrafanaUpdate = new Thread(() =>
                {
                    string GrafanaFilename = $"grafana_{newversion}_armhf.deb";

                    Logfile.Log($"upgrade Grafana to {newversion}!");

                    if (File.Exists(GrafanaFilename))
                        File.Delete(GrafanaFilename);

                    // use internal downloader
                    string grafanaUrl = "https://dl.grafana.com/oss/release/grafana_" + newversion + "_armhf.deb";
                    string grafanaFile = $"grafana_{newversion}_armhf.deb";
                    if (!Tools.DownloadToFile(grafanaUrl, grafanaFile, 300, true).Result)
                    {
                        // fallback to wget
                        Logfile.Log($"fallback o wget to download {grafanaUrl}");
                        Tools.ExecMono("wget", $"{grafanaUrl}  --show-progress");
                    }

                    if (File.Exists(GrafanaFilename))
                    {
                        Logfile.Log(GrafanaFilename + " Sucessfully Downloaded -  Size:" + new FileInfo(GrafanaFilename).Length);

                        if (GrafanaVersion == "6.7.3") // first Raspberry PI4 install
                            Tools.ExecMono("dpkg", "-r grafana-rpi");

                        Tools.ExecMono("dpkg", $"-i --force-overwrite grafana_{newversion}_armhf.deb");
                    }

                    Logfile.Log("upgrade Grafana DONE!");

                    Tools.CopyFilesRecursively(new DirectoryInfo("/etc/teslalogger/git/TeslaLogger/GrafanaPlugins"), new DirectoryInfo("/var/lib/grafana/plugins"));
                })
                {
                    Name = "GrafanaUpdate"
                };
                threadGrafanaUpdate.Start();
            }
        }

        internal static string AllowUnsignedPlugins(string path, bool overwrite)
        {
            try
            {
                Logfile.Log("Start Grafana.ini -> AllowUnsignedPlugins");

                var content = File.ReadAllText(path);
                if (content.Contains("[plugins]"))
                {
                    if (!content.Contains("allow_loading_unsigned_plugins"))
                    {
                        Logfile.Log("Grafana.ini -> AllowUnsignedPlugins with [plugin] section");
                        content = content.Replace("[plugins]", "[plugins]\r\nallow_loading_unsigned_plugins=natel-discrete-panel,pr0ps-trackmap-panel,teslalogger-timeline-panel\r\n");
                        if (overwrite)
                        {
                            File.WriteAllText(path, content);
                            Logfile.Log("Grafana.ini -> AllowUnsignedPlugins - Write File");
                        }
                        return content;
                    }

                    Logfile.Log("Grafana.ini -> AllowUnsignedPlugins - Plugins Section available");
                    return content;
                }

                if (content.Contains("allow_loading_unsigned_plugins"))
                {
                    Logfile.Log("Grafana.ini -> AllowUnsignedPlugins - allow_loading_unsigned_plugins available");
                    return content;
                }

                Logfile.Log("Grafana.ini -> AllowUnsignedPlugins");

                content += "[plugins]\r\nallow_loading_unsigned_plugins=natel-discrete-panel,pr0ps-trackmap-panel,teslalogger-timeline-panel\r\n";

                if (overwrite)
                {
                    File.WriteAllText(path, content);
                    Logfile.Log("Grafana.ini -> AllowUnsignedPlugins - Write File");
                }
                return content;
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }

            return "";
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
                ex.ToExceptionless().FirstCarUserID().Submit();
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
                ex.ToExceptionless().FirstCarUserID().Submit();
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
                ex.ToExceptionless().FirstCarUserID().Submit();
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
                dynamic j = JsonConvert.DeserializeObject(json);
                title = j["title"];
                uid = j["uid"];

                if (!URL_Grafana.EndsWith("/", StringComparison.Ordinal))
                {
                    URL_Grafana += "/";
                }

                link = URL_Grafana + "d/" + uid + "/" + title;
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, "");
            }
        }

        internal static string ReplaceValuesTags(string content, Dictionary<string, string> dictLanguage)
        {
            try
            {
                Regex regexAlias = new Regex("\\\"displayName\\\",\\s*\\\"value\\\":.*?\\\"(.+)\\\"");

                MatchCollection matches = regexAlias.Matches(content);

                foreach (Match match in matches)
                {
                    content = ReplaceValueTag(content, match.Groups[1].Value, dictLanguage);
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }

            return content;
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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }

            return content;
        }

        private static string ReplaceAliasTag(string content, string v, Dictionary<string, string> dictLanguage)
        {
            if (!dictLanguage.ContainsKey(v))
            {
                Logfile.Log("Key '" + v + "' not Found in Translationfile! (Alias)");
                return content;
            }

            Regex regexAlias = new Regex("\\\"alias\\\":.*?\\\"" + v + "\\\"");
            string replace = "\"alias\": \"" + dictLanguage[v] + "\"";

            return regexAlias.Replace(content, replace);
        }

        private static string ReplaceValueTag(string content, string v, Dictionary<string, string> dictLanguage)
        {
            if (!dictLanguage.ContainsKey(v))
            {
                Logfile.Log("Key '" + v + "' not Found in Translationfile! (value)");
                return content;
            }

            Regex regexAlias = new Regex("\\\"value\\\":.*?\\\"" + v + "\\\"");
            string replace = "\"value\": \"" + dictLanguage[v] + "\"";

            return regexAlias.Replace(content, replace);
        }

        private static string ReplaceNameTag(string content, string v, Dictionary<string, string> dictLanguage)
        {
            if (!dictLanguage.ContainsKey(v))
            {
                Logfile.Log("Key '" + v + "' not Found in Translationfile! (name)");
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
                Logfile.Log("Key '" + v + "' not Found in Translationfile! (title)");
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
                return ReplaceLanguageTag(ref content, v, dictLanguage[v]);
            }
            else
            {
                return content.Replace(v, dictLanguage[v]);
            }
        }

        private static string ReplaceLanguageTag(ref string content, string oldtext, string newtext)
        {
            content = content.Replace("'" + oldtext + "'", "'" + newtext + "'");
            return content.Replace("\"" + oldtext + "\"", "\"" + newtext + "\"");
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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log("chmod " + filename + " " + ex.Message);
            }
        }

        public static void CheckForNewVersion()
        {
            lock (lastTeslaLoggerVersionCheckObj)
            {
                try
                {
                    for (int x = 0; x < Car.Allcars.Count; x++)
                    {
                        Car c = Car.Allcars[x];
                        if (c.GetCurrentState() == Car.TeslaState.Charge || c.GetCurrentState() == Car.TeslaState.Drive)
                            return;
                    }

                    TimeSpan ts = DateTime.UtcNow - lastTeslaLoggerVersionCheck;
                    if (ts.TotalMinutes > 240)
                    {
                        lastTeslaLoggerVersionCheck = DateTime.UtcNow;

                        string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                        Logfile.Log($"Checking TeslaLogger online update (current version: {currentVersion}) ...");

                        string online_version = WebHelper.GetOnlineTeslaloggerVersion();
                        if (string.IsNullOrEmpty(online_version))
                        {
                            // recheck in 10 Minutes
                            Logfile.Log("Empty Version String - recheck in 10 minutes");
                            lastTeslaLoggerVersionCheck = lastTeslaLoggerVersionCheck.AddMinutes(10);
                            return;
                        }

                        Tools.UpdateType updateType = Tools.GetOnlineUpdateSettings();

                        if (UpdateNeeded(currentVersion, online_version, updateType))
                        {
                            // if update doesn't work, it will retry tomorrow
                            lastTeslaLoggerVersionCheck = DateTime.UtcNow.AddDays(1);

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

                                foreach (Car car in Car.Allcars)
                                {
                                    car.CurrentJSON.ToKVS();
                                }

                                Tools.ExecMono("reboot", "");
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
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    Logfile.Log(ex.ToString());
                }
            }
        }

        public static bool UpdateNeeded(string currentVersion, string online_version, Tools.UpdateType updateType)
        {
            /*
            if (updateType == Tools.UpdateType.none) // None isn't supported anymore, because Tesla may force me to do an update
            {
                return false;
            }*/

            if (updateType == Tools.UpdateType.stable || updateType == Tools.UpdateType.all || updateType == Tools.UpdateType.none)
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