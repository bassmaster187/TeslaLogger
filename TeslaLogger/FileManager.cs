using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Exceptionless;

namespace TeslaLogger
{
    internal enum TLFilename
    {
        CarSettings,
        TeslaTokenFilename,
        SettingsFilename,
        CurrentJsonFilename,
        WakeupFilename,
        CmdGoSleepFilename,
        GeofenceFilename,
        GeofencePrivateFilename,
        NewCredentialsFilename,
        TeslaLoggerExeConfigFilename,
        GeocodeCache,
        GeofenceRacingFilename,
        BackupDir,
        ExceptionDir,
        LogFile,
        LogsDir,
        TLRoot,
        BackupSH
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
                { TLFilename.CarSettings,                   _Root + "/car_settings.xml"},
                { TLFilename.TeslaTokenFilename,            _Root + "/tesla_token.txt"},
                { TLFilename.SettingsFilename,              _Root + "/settings.json"},
                { TLFilename.CurrentJsonFilename,           _Root + "/current_json.txt"},
                { TLFilename.WakeupFilename,                _Root + "/wakeupteslalogger_ID.txt"},
                { TLFilename.CmdGoSleepFilename,            _Root + "/cmd_gosleep_ID.txt"},
                { TLFilename.GeofenceFilename,              _Root + "/geofence.csv"},
                { TLFilename.GeofencePrivateFilename,       _Root + "/geofence-private.csv"},
                { TLFilename.GeofenceRacingFilename,        _Root + "/geofence-racing.csv"},
                { TLFilename.NewCredentialsFilename,        _Root + "/new_credentials.json"},
                { TLFilename.TeslaLoggerExeConfigFilename,  _Root + "/TeslaLogger.exe.config"},
                { TLFilename.GeocodeCache,                  _Root + "/GeocodeCache.xml"},
                { TLFilename.BackupDir,                     _Root + "/backup"},
                { TLFilename.BackupSH,                      _Root + "/backup.sh"},
                { TLFilename.ExceptionDir,                  _Root + "/Exception"},
                { TLFilename.LogFile,                       _Root + "/nohup.out"},
                { TLFilename.LogsDir,                       _Root + "/logs"},
                { TLFilename.TLRoot,                        _Root}
            };
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
            String filename = Filenames[TLFilename.CmdGoSleepFilename];
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
            string filename = Filenames[TLFilename.WakeupFilename];
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
                string path = GetFilePath(TLFilename.TeslaTokenFilename);
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
            /* if (_Root == null)
            {

                System.Reflection.Assembly executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();

                string executingPath = executingAssembly.Location;

                executingPath = executingPath.Replace(executingAssembly.ManifestModule.Name, string.Empty);
                executingPath = executingPath.Replace("UnitTestsTeslalogger", "TeslaLogger");

                _Root = executingPath;
            } */

            return _Root;
        }
    }
}