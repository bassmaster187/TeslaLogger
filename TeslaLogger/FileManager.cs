using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        GeofenceRacingFilename
    }

    /// <summary>
    /// This Manager will handle all about Files, specially to have the correct
    /// path for a file.
    /// For a new file add a new Enum and enter the filename in the constructor
    /// and use the GetFilePath(TLFilename) Method
    /// </summary>
    internal class FileManager
    {
        private static readonly Dictionary<TLFilename, string> Filenames;
        private static string _ExecutingPath = null;
        static FileManager()
        {
            Filenames = new Dictionary<TLFilename, string>()
            {
                { TLFilename.CarSettings,               "car_settings.xml"},
                { TLFilename.TeslaTokenFilename,        "tesla_token.txt"},
                { TLFilename.SettingsFilename,          "settings.json"},
                { TLFilename.CurrentJsonFilename,       "current_json.txt"},
                { TLFilename.WakeupFilename,            "wakeupteslalogger.txt"},
                { TLFilename.CmdGoSleepFilename,        "cmd_gosleep.txt"},
                { TLFilename.GeofenceFilename,          "geofence.csv"},
                { TLFilename.GeofencePrivateFilename,   "geofence-private.csv"},
                { TLFilename.GeofenceRacingFilename,    "geofence-racing.csv"},
                { TLFilename.NewCredentialsFilename,    "new_credentials.json"},
                { TLFilename.TeslaLoggerExeConfigFilename,"TeslaLogger.exe.config"},
                { TLFilename.GeocodeCache,              "GeocodeCache.xml"}
            };
        }

        internal static string GetFilePath(TLFilename filename)
        {
            return Path.Combine(GetExecutingPath(), Filenames[filename]);
        }

        internal static bool CheckCmdGoSleepFile()
        {

            if (File.Exists(GetGoSleepPath))
            {
                File.Delete(GetGoSleepPath);
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

        private static string GetGoSleepPath
        {
            get
            {
                if (Tools.IsDocker())
                {
                    return Path.Combine("/tmp/", Filenames[TLFilename.CmdGoSleepFilename]);
                }
                else
                {
                    return GetFilePath(TLFilename.CmdGoSleepFilename);
                }
            }
        }

        internal static string GetWakeupTeslaloggerPath
        {
            get
            {
                if (Tools.IsDocker())
                    return Path.Combine("/tmp/", Filenames[TLFilename.WakeupFilename]);
                else
                    return GetFilePath(TLFilename.WakeupFilename);
            }
        }

        internal static string GetTeslaTokenFileContent()
        {
            string filecontent = string.Empty;

            try
            {
                string path = GetFilePath(TLFilename.TeslaTokenFilename);
                if (path != string.Empty)
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

        /// <summary>
        /// This is needed for mono. On some distributions (e.g. Docker) only the filename as a path
        /// will write the file in / (root)
        /// </summary>
        /// <returns>the path where the application execute is located</returns>
        public static string GetExecutingPath()
        {
            //System.IO.Directory.GetCurrentDirectory() is not returning the current path of the assembly
            if (_ExecutingPath == null)
            {

                System.Reflection.Assembly executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();

                string executingPath = executingAssembly.Location;

                executingPath = executingPath.Replace(executingAssembly.ManifestModule.Name, string.Empty);
                executingPath = executingPath.Replace("UnitTestsTeslalogger", "TeslaLogger");

                _ExecutingPath = executingPath;
            }

            return _ExecutingPath;
        }
    }
}