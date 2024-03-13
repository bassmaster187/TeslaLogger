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
        GeofenceRacingFilename
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
        private static string _ExecutingPath; // defaults to null
#pragma warning disable CA1810 // Statische Felder für Referenztyp inline initialisieren
        static FileManager() => Filenames = new Dictionary<TLFilename, string>()
#pragma warning restore CA1810 // Statische Felder für Referenztyp inline initialisieren
            {
                { TLFilename.CarSettings,               "car_settings.xml"},
                { TLFilename.TeslaTokenFilename,        "tesla_token.txt"},
                { TLFilename.SettingsFilename,          "settings.json"},
                { TLFilename.CurrentJsonFilename,       "current_json.txt"},
                { TLFilename.WakeupFilename,            "wakeupteslalogger_ID.txt"},
                { TLFilename.CmdGoSleepFilename,        "cmd_gosleep_ID.txt"},
                { TLFilename.GeofenceFilename,          "geofence.csv"},
                { TLFilename.GeofencePrivateFilename,   "geofence-private.csv"},
                { TLFilename.GeofenceRacingFilename,    "geofence-racing.csv"},
                { TLFilename.NewCredentialsFilename,    "new_credentials.json"},
                { TLFilename.TeslaLoggerExeConfigFilename,"TeslaLogger.exe.config"},
                { TLFilename.GeocodeCache,              "GeocodeCache.xml"}
            };

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