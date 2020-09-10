using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    public class Tools
    {

        public static System.Globalization.CultureInfo ciEnUS = new System.Globalization.CultureInfo("en-US");
        public static System.Globalization.CultureInfo ciDeDE = new System.Globalization.CultureInfo("de-DE");
        private static int _startSleepingHour = -1;
        private static int _startSleepingMinutes = -1;
        private static string _power = "hp";
        private static string _temperature = "celsius";
        private static string _length = "km";
        private static string _language = "de";
        private static string _URL_Admin = "";
        private static string _URL_Grafana = "http://raspberry:3000/";
        private static string _Range = "IR";
        private static DateTime lastGrafanaSettings = DateTime.UtcNow.AddDays(-1);
        private static DateTime lastSleepingHourMinutsUpdated = DateTime.UtcNow.AddDays(-1);

        private static string _OSVersion = string.Empty;

        public enum UpdateType { all, stable, none};

        public static void SetThread_enUS()
        {
            Thread.CurrentThread.CurrentCulture = ciEnUS;
        }

        public static long ToUnixTime(DateTime dateTime)
        {
            return (long)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static void DebugLog(string text, [CallerFilePath] string _cfp = null, [CallerLineNumber] int _cln = 0)
        {
            if (Program.VERBOSE)
            {
                string temp = "DEBUG : " + text + " (" + Path.GetFileName(_cfp) + ":" + _cln + ")";
                Logfile.Log(temp);
            }
        }

        public static string GetMonoRuntimeVersion()
        {
            Type type = Type.GetType("Mono.Runtime");
            if (type != null)
            {
                MethodInfo displayName = type.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (displayName != null)
                {
                    return displayName.Invoke(null, null).ToString();
                }
            }

            return "NULL";
        }

        public static bool IsMono()
        {
            return GetMonoRuntimeVersion() != "NULL";
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            try
            {
                foreach (DirectoryInfo dir in source.GetDirectories())
                {
                    CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
                }

                foreach (FileInfo file in source.GetFiles())
                {
                    string p = Path.Combine(target.FullName, file.Name);

                    Logfile.Log("Copy '" + file.FullName + "' to '" + p + "'");

                    File.Copy(file.FullName, p, true);
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("CopyFilesRecursively Exception: " + ex.ToString());
            }
        }

        public static void CopyFile(string srcFile, string directory)
        {
            try
            {
                Logfile.Log("Copy '" + srcFile + "' to '" + directory + "'");
                File.Copy(srcFile, directory, true);
            }
            catch (Exception ex)
            {
                Logfile.Log("CopyFile Exception: " + ex.ToString());
            }
        }

        public static string DataTableToJSONWithJavaScriptSerializer(DataTable table)
        {
            JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> childRow;
            foreach (DataRow row in table.Rows)
            {
                childRow = new Dictionary<string, object>();
                foreach (DataColumn col in table.Columns)
                {
                    childRow.Add(col.ColumnName, row[col]);
                }
                parentRow.Add(childRow);
            }
            return jsSerializer.Serialize(parentRow);
        }

        internal static void EndSleeping(out int stopSleepingHour, out int stopSleepingMinute)
        {
            stopSleepingHour = -1;
            stopSleepingMinute = -1;

            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                {
                    return;
                }

                string json = File.ReadAllText(filePath);

                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                if (bool.Parse(j["SleepTimeSpanEnable"]))
                {
                    string start = j["SleepTimeSpanEnd"];
                    string[] s = start.Split(':');

                    int.TryParse(s[0], out stopSleepingHour);
                    int.TryParse(s[1], out stopSleepingMinute);
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        internal static int GetHttpPort()
        {
            int httpport = 5000; // default
            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);
                if (!File.Exists(filePath))
                {
                    Logfile.Log("settings file not found at " + filePath);
                    return httpport;
                }
                string json = File.ReadAllText(filePath);
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);
                if (IsPropertyExist(j, "HTTPPort"))
                {
                    int.TryParse(j["HTTPPort"], out httpport);
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            return httpport;
        }

        internal static void StartSleeping(out int startSleepingHour, out int startSleepingMinutes)
        {
            TimeSpan ts = DateTime.UtcNow - lastSleepingHourMinutsUpdated;
            if (ts.TotalMinutes < 5)
            {
                startSleepingHour = _startSleepingHour;
                startSleepingMinutes = _startSleepingMinutes;
                return;
            }

            startSleepingHour = -1;
            startSleepingMinutes = -1;

            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                {
                    lastSleepingHourMinutsUpdated = DateTime.UtcNow;
                    return;
                }

                string json = File.ReadAllText(filePath);
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                if (IsPropertyExist(j, "SleepTimeSpanEnable") && IsPropertyExist(j, "SleepTimeSpanStart"))
                {
                    if (bool.Parse(j["SleepTimeSpanEnable"]))
                    {
                        string start = j["SleepTimeSpanStart"];
                        string[] s = start.Split(':');

                        if (s.Length >= 2)
                        {
                            int.TryParse(s[0], out startSleepingHour);
                            int.TryParse(s[1], out startSleepingMinutes);
                        }
                    }
                }
                
                _startSleepingHour = startSleepingHour;
                _startSleepingMinutes = startSleepingMinutes;

                lastSleepingHourMinutsUpdated = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
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

        internal static bool UseScanMyTesla()
        {
            try
            {
                if (ApplicationSettings.Default.UseScanMyTesla)
                {
                    return true;
                }

                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                {
                    return false;
                }

                string json = File.ReadAllText(filePath);
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                if (IsPropertyExist(j, "ScanMyTesla"))
                {
                    return bool.Parse(j["ScanMyTesla"]);
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            return false;
        }

        internal static UpdateType GetOnlineUpdateSettings()
        {
            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                {
                    return UpdateType.all;
                }

                string json = File.ReadAllText(filePath);
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                if (IsPropertyExist(j, "update"))
                {
                    if (j["update"] == "stable")
                    {
                        return UpdateType.stable;
                    }
                    else if (j["update"] == "none")
                    {
                        return UpdateType.none;
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            return UpdateType.all;
        }


        internal static void GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin, out string Range, out string URL_Grafana)
        {
            TimeSpan ts = DateTime.UtcNow - lastGrafanaSettings;
            if (ts.TotalMinutes < 10)
            {
                power = _power;
                temperature =_temperature;
                length =_length;
                language =_language;
                URL_Admin =_URL_Admin;
                Range = _Range;
                URL_Grafana = _URL_Grafana;
                return;
            }

            power = "hp";
            temperature = "celsius";
            length = "km";
            language = "de";
            URL_Admin = "";
            Range = "IR";
            URL_Grafana = "http://raspberry:3000/";

            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                {
                    lastGrafanaSettings = DateTime.UtcNow;
                    return;
                }

                string json = File.ReadAllText(filePath);
                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                if (IsPropertyExist(j, "Power"))
                {
                    power = j["Power"];
                }

                if (IsPropertyExist(j, "Temperature"))
                {
                    temperature = j["Temperature"];
                }

                if (IsPropertyExist(j, "Length"))
                {
                    length = j["Length"];
                }

                if (IsPropertyExist(j, "Language"))
                {
                    language = j["Language"];
                }

                if (IsPropertyExist(j, "URL_Admin"))
                {
                    if (j["URL_Admin"].ToString().Length > 0)
                    {
                        URL_Admin = j["URL_Admin"];
                    }
                }

                if (IsPropertyExist(j, "Range"))
                {
                    if (j["Range"].ToString().Length > 0)
                    {
                        Range = j["Range"];
                    }
                }

                if (IsPropertyExist(j, "URL_Grafana"))
                {
                    if (j["URL_Grafana"].ToString().Length > 0)
                    {
                        URL_Grafana = j["URL_Grafana"];
                    }
                }

                _power = power;
                _temperature = temperature;
                _length = length;
                _language = language;
                _URL_Admin = URL_Admin;
                _Range = Range;
                _URL_Grafana = URL_Grafana;

                lastGrafanaSettings = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        public static bool IsPropertyExist(dynamic settings, string name)
        {
            return settings is IDictionary<string, object> dictionary && dictionary.ContainsKey(name);
        }

        public static string GetGrafanaVersion()
        {
            try
            {
                string filename = "/usr/share/grafana/VERSION";

                if (File.Exists(filename))
                {
                    return File.ReadAllText(filename);
                }

            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "GetGrafanaVersion");
            }

            return "?";
        }

        public static bool IsDocker()
        {
            try
            {
                string filename = "/tmp/teslalogger-DOCKER";

                if (File.Exists(filename))
                {
                    return true;
                }

            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "IsDocker");
            }

            return false;
        }

        public static bool IsShareData()
        {
            try
            {
                if (IsDocker())
                {
                    return File.Exists("/tmp/sharedata.txt");
                }

                string filepath = Path.Combine(FileManager.GetExecutingPath(), "sharedata.txt");
                if (File.Exists(filepath))
                {
                    return true;
                }

                filepath = Path.Combine(FileManager.GetExecutingPath(), "sharedata.txt.txt");
                if (File.Exists(filepath))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logfile.ExceptionWriter(ex, "IsShareData");
            }

            return false;

        }

        internal static string GetOsVersion()
        {
            if (!_OSVersion.Equals(string.Empty))
            {
                return _OSVersion;
            }
          
            string ret = "";
            try
            {
                ret = Environment.OSVersion.ToString();

                string modelPath = "/proc/device-tree/model";

                if (File.Exists(modelPath))
                {
                    string model = File.ReadAllText(modelPath);

                    if (model.Contains("Raspberry Pi "))
                    {
                        model += " /";
                        model = model.Replace("Raspberry Pi ", "RPI");
                        model = model.Replace("Model", "");
                        model = model.Replace("Rev", "");
                        model = model.Replace("Plus", "+");
                        model = model.Replace("  ", " ");

                        model = model.Replace("RPI3 B +", "RPI3B+");
                        model = model.Replace("RPI4 B", "RPI4B");

                        ret = ret.Replace("Unix", model);
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            ret = ret.Replace("\0", ""); // remove null bytes

            _OSVersion = ret;
            return ret;
        }

        // source: https://www.limilabs.com/blog/json-net-formatter
        // StringWalker, IndentWriter, JsonFormatter
        // license: You can do whatever you want with it. (lesnikowski@limilabs.com)

        private class StringWalker
        {
            private readonly string _s;

            public int Index { get; private set; }
            public bool IsEscaped { get; private set; }
            public char CurrentChar { get; private set; }

            public StringWalker(string s)
            {
                _s = s;
                Index = -1;
            }

            public bool MoveNext()
            {
                if (Index == _s.Length - 1)
                {
                    return false;
                }

                IsEscaped = IsEscaped == false && CurrentChar == '\\';
                Index++;
                CurrentChar = _s[Index];
                return true;
            }
        };

        private class IndentWriter
        {
            private readonly StringBuilder _result = new StringBuilder();
            private int _indentLevel;

            public void Indent()
            {
                _indentLevel++;
            }

            public void UnIndent()
            {
                if (_indentLevel > 0)
                {
                    _indentLevel--;
                }
            }

            public void WriteLine(string line)
            {
                _result.AppendLine(CreateIndent() + line);
            }

            private string CreateIndent()
            {
                StringBuilder indent = new StringBuilder();
                for (int i = 0; i < _indentLevel; i++)
                {
                    indent.Append("    ");
                }

                return indent.ToString();
            }

            public override string ToString()
            {
                return _result.ToString();
            }
        };

        internal class JsonFormatter
        {
            private readonly StringWalker _walker;
            private readonly IndentWriter _writer = new IndentWriter();
            private readonly StringBuilder _currentLine = new StringBuilder();
            private bool _quoted;

            public JsonFormatter(string json)
            {
                _walker = new StringWalker(json);
                ResetLine();
            }

            public void ResetLine()
            {
                _currentLine.Length = 0;
            }

            public string Format()
            {
                while (MoveNextChar())
                {
                    if (_quoted == false && IsOpenBracket())
                    {
                        WriteCurrentLine();
                        AddCharToLine();
                        WriteCurrentLine();
                        _writer.Indent();
                    }
                    else if (_quoted == false && IsCloseBracket())
                    {
                        WriteCurrentLine();
                        _writer.UnIndent();
                        AddCharToLine();
                    }
                    else if (_quoted == false && IsColon())
                    {
                        AddCharToLine();
                        WriteCurrentLine();
                    }
                    else
                    {
                        AddCharToLine();
                    }
                }
                WriteCurrentLine();
                return _writer.ToString();
            }

            private bool MoveNextChar()
            {
                bool success = _walker.MoveNext();
                if (IsApostrophe())
                {
                    _quoted = !_quoted;
                }
                return success;
            }

            public bool IsApostrophe()
            {
                return _walker.CurrentChar == '"' && _walker.IsEscaped == false;
            }

            public bool IsOpenBracket()
            {
                return _walker.CurrentChar == '{'
                    || _walker.CurrentChar == '[';
            }

            public bool IsCloseBracket()
            {
                return _walker.CurrentChar == '}'
                    || _walker.CurrentChar == ']';
            }

            public bool IsColon()
            {
                return _walker.CurrentChar == ',';
            }

            private void AddCharToLine()
            {
                _currentLine.Append(_walker.CurrentChar);
            }

            private void WriteCurrentLine()
            {
                string line = _currentLine.ToString().Trim();
                if (line.Length > 0)
                {
                    _writer.WriteLine(line);
                }
                ResetLine();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Housekeeping()
        {
            // df and du before cleanup
            LogDiskUsage();
            // log DB usage
            LogDBUsage();
            // cleanup Exceptions
            CleanupExceptionsDir();
            // cleanup database
            CleanupDatabaseTableMothership();
            // run housekeeping regularly:
            // - after 24h
            // - but only if car is asleep, otherwise wait another hour
            CreateMemoryCacheItem(24);
        }

        private static void LogDBUsage()
        {
            /*
             * https://chartio.com/resources/tutorials/how-to-get-the-size-of-a-table-in-mysql/
             */
            Logfile.Log("Housekeeping: database usage");
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT TABLE_NAME, ROUND(DATA_LENGTH / 1024 / 1024), ROUND(INDEX_LENGTH / 1024 / 1024), TABLE_ROWS FROM information_schema.TABLES WHERE TABLE_SCHEMA = \"teslalogger\" AND TABLE_TYPE = \"BASE TABLE\"", con);
                cmd.Parameters.AddWithValue("@tsdate", DateTime.Now.AddDays(-90));
                try
                {
                    MySqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        Logfile.Log($"Table: {dr[0],20} data:{dr[1],5} MB index:{dr[2],5} MB rows:{dr[3],10}");
                    }
                }
                catch (Exception) { }
            }
        }

        private static void CreateMemoryCacheItem(double hours = 24.0)
        {
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.AbsoluteExpiration = DateTime.Now.AddHours(hours);
            CacheEntryRemovedCallback removeCallback = new CacheEntryRemovedCallback(HousekeepingCallback);
            policy.RemovedCallback = removeCallback;
            _ = MemoryCache.Default.Add(Program.TLMemCacheKey.Housekeeping.ToString(), policy, policy);
        }

        private static void HousekeepingCallback(CacheEntryRemovedArguments arguments)
        {
            bool allCarsAsleep = true;
            foreach (Car car in Car.allcars)
            {
                // first car to return false will set allCarsAsleep to false and it'll stay false
                allCarsAsleep &= car.TLUpdatePossible();
            }
            if (allCarsAsleep)
            {
                // CacheItem was removed and all cars are asleep, so run housekeeping
                Program.RunHousekeepingInBackground();
            }
            else
            {
                // wait another hour to try again
                CreateMemoryCacheItem(1);
            }
        }

        private static void CleanupDatabaseTableMothership()
        {
            long mothershipCount = 0;
            long mothershipMaxId = 0;
            long mothershipMinId = 0;
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT COUNT(id), MAX(id), MIN(id) FROM mothership WHERE ts < @tsdate", con);
                cmd.Parameters.AddWithValue("@tsdate", DateTime.Now.AddDays(-90));
                try
                {
                    MySqlDataReader dr = cmd.ExecuteReader();
                    if (dr.Read())
                    {
                        _ = long.TryParse(dr[0].ToString(), out mothershipCount);
                        _ = long.TryParse(dr[1].ToString(), out mothershipMaxId);
                        _ = long.TryParse(dr[2].ToString(), out mothershipMinId);
                        Logfile.Log($"Housekeeping: database.mothership older than 90 days count: {mothershipCount} minID:{mothershipMinId} maxID:{mothershipMaxId}");
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }
                con.Close();
            }
            if (mothershipCount >= 1000)
            {
                // split into chunks to keep database load low
                for (long dbupdate = mothershipMinId + 1000; dbupdate <= mothershipMaxId; dbupdate += 1000)
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        Logfile.Log($"Housekeeping: delete database.mothership chunk {dbupdate}");
                        con.Open();
                        string SQLcmd = "DELETE FROM mothership where id < @maxid";
                        MySqlCommand cmd = new MySqlCommand(SQLcmd, con);
                        cmd.Parameters.AddWithValue("@maxid", dbupdate);
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            Logfile.Log(ex.ToString());
                        }
                        con.Close();
                    }
                    Thread.Sleep(5000);
                }
                // report again
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT COUNT(id) FROM mothership WHERE ts < @tsdate", con);
                    cmd.Parameters.AddWithValue("@tsdate", DateTime.Now.AddDays(-90));
                    try
                    {
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read())
                        {
                            _ = long.TryParse(dr[0].ToString(), out mothershipCount);
                            Logfile.Log("Housekeeping: database.mothership older than 90 days count: " + mothershipCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.ToString());
                    }
                    con.Close();
                }
            }
        }

        private static void CleanupExceptionsDir()
        {
            bool filesFoundForDeletion = false;
            int countDeletedFiles = 0;
            if (Directory.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Exception"))
            {
                foreach (string fs in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Exception"))
                {
                    if ((DateTime.Now - File.GetLastWriteTime(fs)).TotalDays > 30)
                    {
                        try
                        {
                            //Logfile.Log("Housekeeping: delete file " + fs);
                            File.Delete(fs);
                            filesFoundForDeletion = true;
                            countDeletedFiles++;
                        }
                        catch (Exception ex)
                        {
                            Logfile.Log(ex.ToString());
                        }
                    }
                }
            }
            if (filesFoundForDeletion)
            {
                Logfile.Log($"Housekeeping: {countDeletedFiles} file(s) deleted in Exception direcotry");
                if (Directory.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Exception"))
                {
                    Exec_mono("/usr/bin/du", "-sk " + System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Exception", true, true);
                }
            }
        }

        private static void LogDiskUsage()
        {
            _ = Exec_mono("/bin/df", "-k", true, true);
            if (Directory.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/backup"))
            {
                _ = Exec_mono("/usr/bin/du", "-sk " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/backup", true, true);
            }
            if (Directory.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Exception"))
            {
                _ = Exec_mono("/usr/bin/du", "-sk " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Exception", true, true);
            }
            if (File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/nohup.out"))
            {
                _ = Exec_mono("/usr/bin/du", "-sk " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/nohup.out", true, true);
            }
        }
    }
}
