using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Policy;
using System.Text;
using Exceptionless;

namespace TeslaLogger
{
    internal enum TLFilename
    {
        CarSettingsFile,
        TeslaTokenFile,
        SettingsFile,
        CurrentJsonFile,
        WakeupFile,
        CmdGoSleepFile,
        GeofenceFile,
        GeofencePrivateFile,
        NewCredentialsFile,
        TeslaLoggerExeConfigFile,
        GeocodeCacheFile,
        GeofenceRacingFile,
        BackupDir,
        ExceptionsDir,
        LogFile,
        LogsDir,
        TLRoot,
        BackupSHFile,
        ShareDataFile
    }

    /// <summary>
    /// This Manager will handle all about Files, specially to have the correct
    /// path for a file.
    /// For a new file add a new Enum and enter the filename in the constructor
    /// and use the GetFilePath(TLFilename) Method
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    internal class FileManager
    {
        private static readonly Dictionary<TLFilename, string> Filenames;
        private static string _Root = "/etc/teslalogger"; // defaults for RasPi
        static FileManager() { 
            if (Tools.IsDocker() || Tools.IsDockerNET8()) {
                // TODO different root for docker env?
            }
            Filenames = new Dictionary<TLFilename, string>()
            {
                { TLFilename.CarSettingsFile,               "/car_settings.xml"},
                { TLFilename.TeslaTokenFile,                "/tesla_token.txt"},
                { TLFilename.SettingsFile,                  "/settings.json"},
                { TLFilename.ShareDataFile,                 "/sharedata.txt"},
                { TLFilename.CurrentJsonFile,               "/current_json.txt"},
                { TLFilename.WakeupFile,                    "/wakeupteslalogger_ID.txt"},
                { TLFilename.CmdGoSleepFile,                "/cmd_gosleep_ID.txt"},
                { TLFilename.GeofenceFile,                  "/geofence.csv"},
                { TLFilename.GeofencePrivateFile,           "/geofence-private.csv"},
                { TLFilename.GeofenceRacingFile,            "/geofence-racing.csv"},
                { TLFilename.NewCredentialsFile,            "/new_credentials.json"},
                { TLFilename.TeslaLoggerExeConfigFile,      "/TeslaLogger.exe.config"},
                { TLFilename.GeocodeCacheFile,              "/GeocodeCache.xml"},
                { TLFilename.BackupDir,                     "/backup"},
                { TLFilename.BackupSHFile,                  "/backup.sh"},
                { TLFilename.ExceptionsDir,                 "/Exception"},
                { TLFilename.LogFile,                       "/nohup.out"},
                { TLFilename.LogsDir,                       "/logs"},
                { TLFilename.TLRoot,                        _Root}
            };
            CheckDirectories();
        }

        private static void CheckDirectories()
        {
            foreach (string tlf in Enum.GetNames(typeof(TLFilename)))
            {
                if (tlf.EndsWith("Dir", StringComparison.Ordinal) && !Directory.Exists(tlf))
                {
                    try
                    {
                        Tools.DebugLog($"FileManager: creating missing directory {tlf}");
                        Directory.CreateDirectory(tlf);
                    }
                    catch (Exception ex)
                    {
                        ex.ToExceptionless().FirstCarUserID().Submit();
                        Logfile.Log(ex.ToString());
                    }
                }
            }
        }

        internal static string GetFilePath(TLFilename filename)
        {
            return Path.Combine(GetExecutingPath(), Filenames[filename]);
        }

        internal static string GetFilePath(string filename)
        {
            return Path.Combine(GetExecutingPath(), filename);
        }

        internal static bool CheckCmdGoSleepFile(int carid)
        {

            if (File.Exists(GetGoSleepPath(carid)))
            {
                File.Delete(GetGoSleepPath(carid));
                return true;
            }
            return false;
        }

        public static string GetSetCostPath
        {
            get
            {
                if (Tools.IsDocker())
                {
                    return Path.Combine("/tmp/", "SetCost.txt");
                }
                else
                {
                    return Path.Combine(GetExecutingPath(), "SetCost.txt");
                }
            }
        }

        private static string GetGoSleepPath(int carid)
        {
            String filename = Filenames[TLFilename.CmdGoSleepFile];
            filename = filename.Replace("ID", carid.ToString(Tools.ciDeDE));

            if (Tools.IsDocker())
            {
                return Path.Combine("/tmp/", filename);
            }
            else
            {
                return GetFilePath(filename);
            }
        }

        internal static string GetWakeupTeslaloggerPath(int carid)
        {
            string filename = Filenames[TLFilename.WakeupFile];
            filename = filename.Replace("ID", carid.ToString(Tools.ciDeDE));

            if (Tools.IsDocker())
                return Path.Combine("/tmp/", filename);
            else
                return GetFilePath(filename);
        }

        internal static string GetTeslaTokenFileContent()
        {
            string filecontent = string.Empty;

            try
            {
                string path = GetFilePath(TLFilename.TeslaTokenFile);
                if (!string.IsNullOrEmpty(path))
                {
                    filecontent = File.ReadAllText(path);
                }
            }
            catch (FileNotFoundException)
            {
                return string.Empty;
            }
            catch (Exception e)
            {
                e.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log($"RestoreToken Exception: {e.Message}");

                return string.Empty;
            }

            return filecontent;
        }

        private static object SyncLock_WriteCurrentJsonFile = new object();

        internal static void WriteCurrentJsonFile(int CarID, string current_json)
        {
            lock (SyncLock_WriteCurrentJsonFile)
            {
                string filepath = Path.Combine(GetExecutingPath(), $"current_json_{CarID}.txt");
                File.WriteAllText(filepath, current_json, Encoding.UTF8);
            }
        }

        internal static string GetSRTMDataPath()
        {
            string path = Path.Combine(GetExecutingPath(), "SRTM-Data");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        internal static string GetMapCachePath()
        {
            string path = Path.Combine(GetExecutingPath(), "MAP-Data");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        /// <summary>
        /// This is needed for mono. On some distributions (e.g. Docker) only the filename as a path
        /// will write the file in / (root)
        /// </summary>
        /// <returns>the path where the application execute is located</returns>
        private static string GetExecutingPath()
        {
            if (System.Reflection.Assembly.GetExecutingAssembly().Location.Contains("UnitTestsTeslalogger"))
            {

                System.Reflection.Assembly executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();

                string executingPath = executingAssembly.Location;

                executingPath = executingPath.Replace(executingAssembly.ManifestModule.Name, string.Empty);
                executingPath = executingPath.Replace("UnitTestsTeslalogger", "TeslaLogger");

                _Root = executingPath;
            } 

            return _Root;
        }
    }
}