using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using MySql.Data.MySqlClient;
using Exceptionless;
using Newtonsoft.Json;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    public static class Tools
    {

        public static readonly System.Globalization.CultureInfo ciEnUS = new System.Globalization.CultureInfo("en-US");
        public static readonly System.Globalization.CultureInfo ciDeDE = new System.Globalization.CultureInfo("de-DE");
        private static int _startSleepingHour = -1;
        private static int _startSleepingMinutes = -1;
        private static string _power = "hp";
        private static string _temperature = "celsius";
        private static string _length = "km";
        private static string _language = "de";
        private static string _URL_Admin = "";
        private static string _URL_Grafana = "http://raspberry:3000/";
        private static string _Range = "IR";
        private static string _defaultcar = "";
        private static string _defaultcarid = "";
        internal static DateTime lastGrafanaSettings = DateTime.UtcNow.AddDays(-1);
        internal static DateTime lastSleepingHourMinutsUpdated = DateTime.UtcNow.AddDays(-1);
        internal static bool? _StreamingPos = null;

        internal static bool UseNearbySuCService()
        {
            // TODO
            return true;
        }

        private static string _OSVersion = string.Empty;

        public enum UpdateType { all, stable, none};

        internal static Queue<Tuple<DateTime, string>> debugBuffer = new Queue<Tuple<DateTime, string>>();

        public static void SetThreadEnUS()
        {
            Thread.CurrentThread.CurrentCulture = ciEnUS;
        }

        public static long ToUnixTime(DateTime dateTime)
        {
            return (long)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static void DebugLog(MySqlCommand cmd, string prefix = "", [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = null)
        {
            if (Program.SQLTRACE)
            {
                try
                {
                    string msg = "SQL" + Environment.NewLine + ExpandSQLCommand(cmd).Trim();
                    DebugLog($"{callerMemberName}: " + msg, null, prefix, callerFilePath, callerLineNumber);
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    DebugLog("Exception in SQL DEBUG", ex, prefix);
                }
            }
        }

        public static void DebugLog(MySqlDataReader dr, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = null)
        {
            if (dr != null)
            {
                string msg = "RAWSQL:";
                for (int column = 0; column < dr.FieldCount; column++)
                {
                    msg += (column == 0 ? "" : "|") + dr.GetName(column) + "<" + dr.GetValue(column) + ">";
                }
                DebugLog($"{callerMemberName}: " + msg, null, "", callerFilePath, callerLineNumber);
            }
        }

        public static void DebugLog(DataTable dt, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0, [CallerMemberName] string callerMemberName = null)
        {
            string msg = callerMemberName + Environment.NewLine;
            if (dt != null)
            {
                // https://stackoverflow.com/questions/15547959/print-contents-of-a-datatable
                Dictionary<string, int> colWidths = new Dictionary<string, int>();

                foreach (DataColumn col in dt.Columns)
                {
                    msg += col.ColumnName;
                    var maxLabelSize = dt.Rows.OfType<DataRow>()
                            .Select(m => (m.Field<object>(col.ColumnName)?.ToString() ?? "").Length)
                            .OrderByDescending(m => m).FirstOrDefault();

                    colWidths.Add(col.ColumnName, maxLabelSize);
                    for (int i = 0; i < maxLabelSize - col.ColumnName.Length + 10; i++)
                    {
                        msg += " ";
                    }
                }

                msg += Environment.NewLine;

                foreach (DataRow dataRow in dt.Rows)
                {
                    for (int j = 0; j < dataRow.ItemArray.Length; j++)
                    {
                        string value = string.IsNullOrEmpty(dataRow.ItemArray[j].ToString()) ? "<NULL>" : dataRow.ItemArray[j].ToString();
                        msg += value;
                        for (int i = 0; i < colWidths[dt.Columns[j].ColumnName] - value.Length + 10; i++)
                        {
                            msg += " ";
                        }
                    }
                    msg += Environment.NewLine;
                }
            }
            DebugLog(msg, null, "", callerFilePath, callerLineNumber);
        }

        internal static string ExpandSQLCommand(MySqlCommand cmd)
        {
            string msg = string.Empty;
            if (cmd != null && cmd.Parameters != null)
            {
                msg = cmd.CommandText;
                foreach (MySqlParameter p in cmd.Parameters)
                {
                    string pValue = "";
                    switch (p.DbType)
                    {
                        case DbType.AnsiString:
                        case DbType.AnsiStringFixedLength:
                        case DbType.Date:
                        case DbType.DateTime:
                        case DbType.DateTime2:
                        case DbType.Guid:
                        case DbType.String:
                        case DbType.StringFixedLength:
                        case DbType.Time:
                            if (p.Value != null)
                            {
                                pValue = $"'{p.Value.ToString().Replace("'", "\\'")}'";
                            }
                            else
                            {
                                pValue = "'NULL'";
                            }
                            break;
                        case DbType.Decimal:
                        case DbType.Double:
                        case DbType.Int16:
                        case DbType.Int32:
                        case DbType.Int64:
                        case DbType.UInt16:
                        case DbType.UInt32:
                        case DbType.UInt64:
                        case DbType.VarNumeric:
                        case DbType.Object:
                        case DbType.SByte:
                        case DbType.Single:
                        case DbType.Binary:
                        case DbType.Boolean:
                        case DbType.Byte:
                        case DbType.Currency:
                        case DbType.DateTimeOffset:
                        case DbType.Xml:
                        default:
                            if (p.Value != null)
                            {
                                pValue = p.Value.ToString();
                            }
                            else
                            {
                                pValue = "NULL";
                            }
                            break;
                    }
                    msg = msg.Replace(p.ParameterName, pValue);
                }
            }
            return msg;
        }

        public static void StartOVMS()
        {
            string OVMSClientPath = "/etc/teslalogger/OVMS/OVMS.exe";

            try
            {
                if (!System.IO.File.Exists(OVMSClientPath))
                {
                    Logfile.Log("OVMS.exe not found!");
                    return;
                }

                using (System.Diagnostics.Process proc = new System.Diagnostics.Process
                {
                    EnableRaisingEvents = false
                })
                {
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.FileName = "mono";
                    proc.StartInfo.Arguments = OVMSClientPath + " nodate";

                    proc.Start();

                    while (!proc.StandardOutput.EndOfStream)
                    {
                        string line = proc.StandardOutput.ReadLine();
                        Logfile.Log(line);
                    }

                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            finally
            {
                Logfile.Log("OVMS terminated");
            }
        }

        public static void DebugLog(string text, Exception ex = null, string prefix = "", [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0)
        {
            string temp = "DEBUG : " + prefix + text + " (" + Path.GetFileName(callerFilePath) + ":" + callerLineNumber + ")";
            AddToBuffer(temp);
            if (Program.VERBOSE)
            {
                Logfile.Log(temp);
            }
            if (ex != null)
            {
                string exmsg = $"DEBUG : Exception {ex.GetType()} {ex}";
                AddToBuffer(exmsg);
                if (Program.VERBOSE)
                {
                    Logfile.Log(exmsg);
                }
            }
        }

        private static void AddToBuffer(string msg)
        {
            DateTime dt = DateTime.Now;
            try
            {
                debugBuffer.Enqueue(new Tuple<DateTime, string>(DateTime.Now, msg));
                while (debugBuffer.Count > 500)
                {
                    _ = debugBuffer.Dequeue();
                }
            }
            // ignore failed inserts
            catch (Exception) {  }
        }

        // source: https://stackoverflow.com/questions/6994852
        private static void TraceException(Exception e)
        {
            try
            {
                MethodBase site = e.TargetSite;//Get the methodname from the exception.
                string methodName = site == null ? "" : site.Name;//avoid null ref if it's null.
                methodName = ExtractBracketed(methodName);

                StackTrace stkTrace = new System.Diagnostics.StackTrace(e, true);
                for (int i = 0; i < 3; i++)
                {
                    //In most cases GetFrame(0) will contain valid information, but not always. That's why a small loop is needed. 
                    var frame = stkTrace.GetFrame(i);
                    int lineNum = frame.GetFileLineNumber();//get the line and column numbers
                    int colNum = frame.GetFileColumnNumber();
                    string className = ExtractBracketed(frame.GetMethod().ReflectedType.FullName);
                    Logfile.Log(ThreadAndDateInfo + "Exception: " + className + "." + methodName + ", Ln " + lineNum + " Col " + colNum + ": " + e.Message);
                    if (lineNum + colNum > 0)
                        break; //exit the for loop if you have valid info. If not, try going up one frame...
                }

            }
            catch (Exception ee)
            {
                ee.ToExceptionless().FirstCarUserID().Submit();
                //Avoid any situation that the Trace is what crashes you application. While trace can log to a file. Console normally not output to the same place.
                Logfile.Log("Tracing exception in TraceException(Exception e)" + ee.Message);
            }
        }

        private static string ExtractBracketed(string str)
        {
            string s;
            if (str.IndexOf('<') > -1) //using the Regex when the string does not contain <brackets> returns an empty string.
                s = Regex.Match(str, @"\<([^>]*)\>").Groups[1].Value;
            else
                s = str;
            if (s.Length == 0)
                return "'Emtpy'"; //for log visibility we want to know if something it's empty.
            else
                return s;

        }

        private static string ThreadAndDateInfo
        {
            //returns thread number and precise date and time.
            get { return "[" + Thread.CurrentThread.ManagedThreadId + " - " + DateTime.Now.ToString("dd/MM HH:mm:ss.ffffff", Tools.ciEnUS) + "] "; }
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

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target, string excludeFile = null)
        {
            if (source != null && target != null)
            {
                try
                {
                    foreach (DirectoryInfo dir in source.GetDirectories())
                    {
                        CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
                    }

                    foreach (FileInfo file in source.GetFiles())
                    {
                        if (excludeFile != null && file.Name == excludeFile)
                        {
                            Logfile.Log($"CopyFilesRecursively: skip {excludeFile}");
                        }
                        else
                        {
                            string p = Path.Combine(target.FullName, file.Name);
                            Logfile.Log("Copy '" + file.FullName + "' to '" + p + "'");
                            File.Copy(file.FullName, p, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    Logfile.Log("CopyFilesRecursively Exception: " + ex.ToString());
                }
            }
            else
            {
                Logfile.Log($"CopyFilesRecursively: source or target is null - source:{source} target:{target}");
            }
        }

        internal static object VINDecoder(string vin, out int year, out string carType, out bool AWD, out bool MIC, out string battery, out string motor, out bool MIG)
        {
            year = 0;
            carType = "";
            AWD = false;
            MIC = false;
            MIG = false;
            battery = "";
            motor = "";

            try
            {
                // source https://github.com/mseminatore/TeslaJS/blob/master/teslajs.js
                int dateCode = (int)vin[9];
                year = 2010 + dateCode - (int)'A';
                carType = "n/a";
                AWD = false;
                MIC = false;
                battery = "n/a";
                motor = "n/a";
                // handle the skipped 'I' code. We may also need to skip 'O'
                if (dateCode > 73)
                {
                    year--;
                }
                switch (vin[3])
                {
                    case 'S':
                        carType = "Model S";
                        break;
                    case '3':
                        carType = "Model 3";
                        break;
                    case 'X':
                        carType = "Model X";
                        break;
                    case 'Y':
                        carType = "Model Y";
                        break;
                    case 'R':
                        carType = "Roadster";
                        break;
                }
                // Check for AWD config 2, 4 or B
                if (
                        vin[7] == '2' || // Dual Motor (standard) (Designated for Model S & Model X)
                        vin[7] == '4' || // Dual Motor (performance) (Designated for Model S & Model X)
                        vin[7] == '5' || // Dual Motor Models S und Model X 2021 Refresh
                        vin[7] == '6' || // Triple Motor Models S und Model X 2021 Plaid
                        vin[7] == 'B' || // Dual motor - standard Model 3
                        vin[7] == 'C' || // Dual motor - performance Model 3
                        vin[7] == 'E' ||  // Dual motor - Model Y
                        vin[7] == 'F' ||  // Dual motor Performance - Model Y
                        vin[7] == 'K' ||  // Dual motor Standard "Hairpin Windings"
                        vin[7] == 'L'   // Dual motor Performance "Hairpin Windings"
                    )
                {
                    AWD = true;
                }
                // check made in China
                if (vin.StartsWith("LRW", StringComparison.Ordinal))
                {
                    MIC = true;
                }
                else if (vin.StartsWith("XP7"))
                {
                    MIG = true;
                }

                // battery type, source https://teslawissen.ch/tesla-vin-nummer-des-fahrzeugs-dekodieren/
                switch (vin[6])
                {
                    case 'E':
                        battery = "NMC";
                        break;
                    case 'F':
                        battery = "LFP";
                        break;
                    case 'H':
                        battery = "hcNMC";
                        break;
                    case 'S':
                        battery = "stdNMC";
                        break;
                    case 'V':
                        battery = "uhcNMC";
                        break;
                }
                // motor, source https://teslawissen.ch/tesla-vin-nummer-des-fahrzeugs-dekodieren/
                switch (vin[7])
                {
                    case '1':
                        motor = "single";
                        break;
                    case '2':
                        motor = "dual";
                        break;
                    case '3':
                        motor = "single performance";
                        break;
                    case '4':
                        motor = "dual performance";
                        break;
                    case '5':
                        motor = "dual 2021 refresh";
                        break;
                    case '6':
                        motor = "triple 2021 plaid";
                        break;
                    case 'A':
                        motor = "3 single";
                        break;
                    case 'B':
                        motor = "3 dual";
                        break;
                    case 'C':
                        motor = "3 dual performance";
                        break;
                    case 'D':
                    case 'J':
                        motor = "Y single";
                        break;
                    case 'E':
                    case 'K':
                    case 'L':
                        motor = "Y dual";
                        break;
                    case 'F':
                        motor = "Y dual performance";
                        break;
                }

                return $"{carType} {year} AWD:{AWD} MIC:{MIC} battery:{battery} motor:{motor}";
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }

            return "?";
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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log("CopyFile Exception: " + ex.ToString());
            }
        }

        public static string DataTableToJSONWithJavaScriptSerializer(DataTable table)
        {
            List<Dictionary<string, object>> parentRow = new List<Dictionary<string, object>>();
            Dictionary<string, object> childRow;
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    childRow = new Dictionary<string, object>();
                    foreach (DataColumn col in table.Columns)
                    {
                        childRow.Add(col.ColumnName, row[col]);
                    }
                    parentRow.Add(childRow);
                }
            }
            return JsonConvert.SerializeObject(parentRow);
        }

        internal static void EndSleeping(out int stopSleepingHour, out int stopSleepingMinute)
        {
            stopSleepingHour = -1;
            stopSleepingMinute = -1;
            string json = "";

            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                {
                    return;
                }

                json = File.ReadAllText(filePath);

                dynamic j = JsonConvert.DeserializeObject(json);

                if (j.ContainsKey("SleepTimeSpanEnable"))
                {
                    bool SleepTimeSpanEnable = j["SleepTimeSpanEnable"];

                    if (SleepTimeSpanEnable)
                    {
                        string start = j["SleepTimeSpanEnd"];
                        string[] s = start.Split(':');

                        _ = int.TryParse(s[0], out stopSleepingHour);
                        _ = int.TryParse(s[1], out stopSleepingMinute);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().AddObject(json,"JSON").Submit();
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
                dynamic j = JsonConvert.DeserializeObject(json);
                if (IsPropertyExist(j, "HTTPPort"))
                {
                    int.TryParse(j["HTTPPort"].ToString(), out httpport);

                    if (httpport == 0)
                    {
                        httpport = 5000;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            return httpport;
        }

        internal static bool CombineChargingStates()
        {
            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);
                if (!File.Exists(filePath))
                {
                    Logfile.Log("settings file not found at " + filePath);
                    return false;
                }
                string json = File.ReadAllText(filePath);
                dynamic j = JsonConvert.DeserializeObject(json);
                if (IsPropertyExist(j, "CombineChargingStates"))
                {
                    if (bool.TryParse(j["CombineChargingStates"].ToString(), out bool combineChargingStates))
                    {
                        return combineChargingStates;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            return true;
        }

        internal static bool UseOpenTopoData()
        {
            return true; // Use by default

            /*
            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);
                if (!File.Exists(filePath))
                {
                    Logfile.Log("settings file not found at " + filePath);
                    return false;
                }
                string json = File.ReadAllText(filePath);
                dynamic j = JsonConvert.DeserializeObject(json);
                if (IsPropertyExist(j, "UseOpenTopoData"))
                {
                    if(bool.TryParse(j["UseOpenTopoData"], out bool useOpenTopoData)) {
                        return useOpenTopoData;
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            return false;
            */
        }

        internal static bool StreamingPos()
        {
            try
            {
                if (_StreamingPos != null)
                    return (bool)_StreamingPos;

                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);
                if (!File.Exists(filePath))
                {
                    Logfile.Log("settings file not found at " + filePath);
                    return false;
                }
                string json = File.ReadAllText(filePath);
                dynamic j = JsonConvert.DeserializeObject(json);
                if (IsPropertyExist(j, "StreamingPos"))
                {
                    if(bool.TryParse(j["StreamingPos"].ToString(), out bool streamingPos)) {
                        Logfile.Log("StreamingPos: " + streamingPos);
                        _StreamingPos = streamingPos;
                        return streamingPos;
                    }
                }

                Logfile.Log("StreamingPos not found in settings.json");
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            _StreamingPos = false;
            return false;
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

            string json = "";
            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                {
                    lastSleepingHourMinutsUpdated = DateTime.UtcNow;
                    return;
                }

                json = File.ReadAllText(filePath);
                dynamic j = JsonConvert.DeserializeObject(json);

                if (IsPropertyExist(j, "SleepTimeSpanEnable") && IsPropertyExist(j, "SleepTimeSpanStart"))
                {
                    if (bool.Parse(j["SleepTimeSpanEnable"].ToString()))
                    {
                        string start = j["SleepTimeSpanStart"];
                        string[] s = start.Split(':');

                        if (s.Length >= 2)
                        {
                            _ = int.TryParse(s[0], out startSleepingHour);
                            _ = int.TryParse(s[1], out startSleepingMinutes);
                        }
                    }
                }
                
                _startSleepingHour = startSleepingHour;
                _startSleepingMinutes = startSleepingMinutes;

                lastSleepingHourMinutsUpdated = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().AddObject(json, "JSON").Submit();
                Logfile.Log(ex.ToString());
            }
        }

        // timeout in seconds
        // https://docs.microsoft.com/de-de/dotnet/api/system.diagnostics.process.exitcode?view=netcore-3.1
        public static string ExecMono(string cmd, string param, bool logging = true, bool stderr2stdout = false, int timeout = 0)
        {
            Logfile.Log("Exec_mono: " + cmd + " " + param);

            StringBuilder sb = new StringBuilder();

            bool bTimeout = false;

            try
            {
                if (!Tools.IsMono())
                {
                    return "";
                }

                using (Process proc = new Process())
                {
                    proc.EnableRaisingEvents = false;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.FileName = cmd;
                    proc.StartInfo.Arguments = param;

                    proc.Start();

                    do
                    {
                        if (!proc.HasExited)
                        {
                            proc.Refresh();

                            if (timeout > 0 && (DateTime.Now - proc.StartTime).TotalSeconds > timeout)
                            {
                                proc.Kill();
                                bTimeout = true;
                            }
                        }
                    }
                    while (!proc.WaitForExit(100));

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
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log("Exception " + cmd + " " + ex.Message);
                return "Exception";
            }
            return bTimeout ? "Timeout! " + sb.ToString() : sb.ToString();
        }

        internal static string ObfuscateVIN(string input)
        {
            if (input == null)
                return null;

            string obfuscated = string.Empty;
            for (int i = 0; i < input.Length; i++)
            {
                if (i >= 11) // Obfuscate Serial Number of VIN
                {
                    obfuscated += "X";
                }
                else
                {
                    obfuscated += input[i];
                }
            }
            return obfuscated;
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
                dynamic j = JsonConvert.DeserializeObject(json);

                if (IsPropertyExist(j, "ScanMyTesla"))
                {
                    return bool.Parse(j["ScanMyTesla"].ToString());
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
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
                dynamic j = JsonConvert.DeserializeObject(json);

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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }

            return UpdateType.all;
        }

        internal static void GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin, out string Range, out string URL_Grafana, out string defaultcar, out string defaultcarid)
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
                defaultcar = _defaultcar;
                defaultcarid = _defaultcarid;
                return;
            }

            power = "hp";
            temperature = "celsius";
            length = "km";
            language = "de";
            URL_Admin = "";
            Range = "IR";
            URL_Grafana = "http://raspberry:3000/";
            defaultcar = "";
            defaultcarid = "";

            string json = null;

            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                {
                    lastGrafanaSettings = DateTime.UtcNow;
                    return;
                }

                json = File.ReadAllText(filePath);

                dynamic j = JsonConvert.DeserializeObject(json);

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
                    if (j["Range"]?.ToString()?.Length > 0)
                    {
                        Range = j["Range"];
                    }
                }

                if (IsPropertyExist(j, "URL_Grafana"))
                {
                    if (j["URL_Grafana"]?.ToString()?.Length > 0)
                    {
                        URL_Grafana = j["URL_Grafana"];
                    }
                }

                if (IsPropertyExist(j, "defaultcar"))
                {
                    if (j["defaultcar"]?.ToString()?.Length > 0)
                    {
                        defaultcar = j["defaultcar"];
                    }
                }

                if (IsPropertyExist(j, "defaultcarid"))
                {
                    if (j["defaultcarid"]?.ToString()?.Length > 0)
                    {
                        defaultcarid = j["defaultcarid"];
                    }
                }

                _power = power;
                _temperature = temperature;
                _length = length;
                _language = language;
                _URL_Admin = URL_Admin;
                _Range = Range;
                _URL_Grafana = URL_Grafana;
                _defaultcar = defaultcar;
                _defaultcarid = defaultcarid;

                lastGrafanaSettings = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().AddObject(json, "JSON").AddObject(Tools.ConvertString2Base64(json),"JSON-Base64").Submit();
                Logfile.Log(ex.ToString());
            }
        }

        public static string ConvertString2Base64(string content)
        {
            string base64 = "";
            try
            {
                if (content != null)
                {
                    var t = System.Text.UTF8Encoding.UTF8.GetBytes(content);
                    base64 = Convert.ToBase64String(t);
                }
            }
            catch (Exception)
            { }

            return base64;
        }

        public static bool IsPropertyExist(object settings, string name)
        {
            if (settings == null)
                return false;

            if (settings is Newtonsoft.Json.Linq.JObject)
            {
                return ((Newtonsoft.Json.Linq.JObject)settings).ContainsKey(name);
            }

            if (!(settings is IDictionary<string, object>))
                return false;

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
                else
                {
                    if (Tools.IsDocker())
                    {
                        string temp = null;
                        using (WebClient wc = new WebClient())
                        {
                            temp = wc.DownloadString("http://grafana:3000/api/health");
                            dynamic j = JsonConvert.DeserializeObject(temp);
                            return j["version"];
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                if (ex is WebException we)
                {
                    if (we.Status == WebExceptionStatus.NameResolutionFailure)
                    {
                        return "NRF";
                    }
                    else if (we.Status == WebExceptionStatus.ConnectFailure)
                    {
                        return "CF";
                    }
                }

                ex.ToExceptionless().FirstCarUserID().Submit();
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
                ex.ToExceptionless().FirstCarUserID().Submit();
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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.ExceptionWriter(ex, "IsShareData");
            }

            return false;

        }

        internal static string GetOsVersion()
        {
            if (!string.IsNullOrEmpty(_OSVersion))
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
                ex.ToExceptionless().FirstCarUserID().Submit();
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
            try
            {
                // df and du before cleanup
                LogDiskUsage();
                // log DB usage
                LogDBUsage();
                // cleanup Exceptions
                CleanupExceptionsDir();
                // cleanup database
                CleanupDatabaseTableMothership();
                // cleanup backup folder
                CleanupBackupFolder();

                // run housekeeping regularly:
                // - after 24h
                // - but only if car is asleep, otherwise wait another hour
                CreateMemoryCacheItem(24);
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        public static void CleanupBackupFolder()
        {
            if (Tools.IsDocker())
                return;

            bool filesFoundForDeletion = false;
            int countDeletedFiles = 0;
            long freeDiskSpaceNeeded = 2048;

            if (FreeDiskSpaceMB() > freeDiskSpaceNeeded) // Keep 2GB of free disk space
                return;


            DirectoryInfo di = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "backup"));

            if (di.Exists)
            {
                var ds = di.GetFiles().OrderBy(p => p.LastWriteTime);

                foreach (var fi in ds)
                {
                    if (FreeDiskSpaceMB() > freeDiskSpaceNeeded) // already deleted enough?
                        return;

                    if ((DateTime.Now - fi.LastWriteTime).TotalDays > 30)
                    {
                        try
                        {
                            Logfile.Log("Housekeeping: delete file " + fi.Name);
                            fi.Delete();
                            filesFoundForDeletion = true;
                            countDeletedFiles++;
                        }
                        catch (Exception ex)
                        {
                            ex.ToExceptionless().FirstCarUserID().Submit();
                            Logfile.Log(ex.ToString());
                        }
                    }
                }
            }
            if (filesFoundForDeletion)
            {
                Logfile.Log($"Housekeeping: {countDeletedFiles} file(s) deleted in Backup direcotry Free Disk Space: {FreeDiskSpaceMB()} MB");
            }
        }

        internal static long FreeDiskSpaceMB()
        {
            DirectoryInfo di = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            DriveInfo driveinfo = new DriveInfo(di.Root.FullName);
            long freeMB = driveinfo.AvailableFreeSpace / 1024 / 1024;
            return freeMB;
        }

        private static void LogDBUsage()
        {
            /*
             * https://chartio.com/resources/tutorials/how-to-get-the-size-of-a-table-in-mysql/
             */
            Logfile.Log($"Housekeeping: database usage ({DBHelper.Database})");
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(
@"SELECT
  TABLE_NAME,
  ROUND(DATA_LENGTH / 1024 / 1024),
  ROUND(INDEX_LENGTH / 1024 / 1024),
  TABLE_ROWS
FROM
  information_schema.TABLES
WHERE
  TABLE_SCHEMA = @dbname
  AND TABLE_TYPE = 'BASE TABLE'", con))
                {
                    cmd.Parameters.AddWithValue("@tsdate", DateTime.Now.AddDays(-90));
                    cmd.Parameters.AddWithValue("@dbname", DBHelper.Database);
                    try
                    {
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read())
                        {
                            Logfile.Log($"Table: {dr[0],20} data:{dr[1],5} MB index:{dr[2],5} MB rows:{dr[3],10}");
                        }
                    }
                    catch (Exception) { }

                }
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
            foreach (Car car in Car.Allcars)
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
                using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(id), MAX(id), MIN(id) FROM mothership WHERE ts < @tsdate", con))
                {
                    cmd.Parameters.AddWithValue("@tsdate", DateTime.Now.AddDays(-GetMothershipKeepDays()));
                    try
                    {
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read())
                        {
                            _ = long.TryParse(dr[0].ToString(), out mothershipCount);
                            _ = long.TryParse(dr[1].ToString(), out mothershipMaxId);
                            _ = long.TryParse(dr[2].ToString(), out mothershipMinId);
                            Logfile.Log($"Housekeeping: database.mothership older than {GetMothershipKeepDays()} days count: {mothershipCount} minID:{mothershipMinId} maxID:{mothershipMaxId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.ToString());
                    }
                    con.Close();
                }
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
                        using (MySqlCommand cmd = new MySqlCommand(SQLcmd, con))
                        {
                            cmd.Parameters.AddWithValue("@maxid", dbupdate);
                            try
                            {
                                SQLTracer.TraceNQ(cmd);
                            }
                            catch (Exception ex)
                            {
                                Logfile.Log(ex.ToString());
                            }
                            con.Close();
                        }
                    }
                    Thread.Sleep(1000);
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
                    ExecMono("/usr/bin/du", "-sk " + System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Exception", true, true);
                }
            }
        }

        private static void LogDiskUsage()
        {
            _ = ExecMono("/bin/df", "-k", true, true);
            if (Directory.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/backup"))
            {
                _ = ExecMono("/usr/bin/du", "-sk " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/backup", true, true);
            }
            if (Directory.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Exception"))
            {
                _ = ExecMono("/usr/bin/du", "-sk " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Exception", true, true);
            }
            if (File.Exists(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/nohup.out"))
            {
                _ = ExecMono("/usr/bin/du", "-sk " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/nohup.out", true, true);
            }
        }

        public static string ObfuscateString(string input)
        {
            if (input == null)
                return null;

            string obfuscated = string.Empty;
            for (int i = 0; i < input.Length; i++)
            {
                if (i % 3 == 0 || i % 5 == 0)
                {
                    obfuscated += "X";
                }
                else
                {
                    obfuscated += input[i];
                }
            }
            return obfuscated;
        }

        internal async static Task<bool> DownloadToFile(string url, string path, int timeout = 60, bool overwrite = false)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                return await DownloadToFile(uri, path, timeout, overwrite).ConfigureAwait(true);
            }
            return false;
        }

        internal async static Task<bool> DownloadToFile(Uri uri, string path, int timeout = 60, bool overwrite = false)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            if (File.Exists(path) && !overwrite)
            {
                return false;
            }
            using (HttpClient httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            })
            {
                try
                {
                    if (File.Exists(path) && overwrite)
                    {
                        File.Decrypt(path);
                    }
                    FileInfo fileInfo = new FileInfo(path);
                    HttpResponseMessage response = await httpClient.GetAsync(uri).ConfigureAwait(true);
                    _ = response.EnsureSuccessStatusCode();
                    using (Stream responseContentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false)) {
                        using (FileStream outputFileStream = File.Create(fileInfo.FullName)) {
                            responseContentStream.Seek(0, SeekOrigin.Begin);
                            responseContentStream.CopyTo(outputFileStream);
                            outputFileStream.Close();
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    DebugLog("DownloadToFile exception:", ex);
                    try
                    {
                        // clean up in case of error
                        File.Delete(path);
                    }
                    catch (Exception ex2)
                    {
                        DebugLog("DownloadToFile exception:", ex2);
                    }
                }
            }
            return false;
        }

        internal static int GetMothershipKeepDays()
        {
            int days = 14; // default
            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);
                if (!File.Exists(filePath))
                {
                    Logfile.Log("settings file not found at " + filePath);
                    return days;
                }
                string json = File.ReadAllText(filePath);
                dynamic j = JsonConvert.DeserializeObject(json);
                if (IsPropertyExist(j, "MothershipKeepDays"))
                {
                    int.TryParse(j["MothershipKeepDays"].ToString(), out days);

                    if (days == 0)
                    {
                        days = 14; // default
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            return days;
        }

        internal static int GetSettingsInt(string name, int Default)
        {
            int value = 0;
            string json = "";
            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);
                if (!File.Exists(filePath))
                {
                    Logfile.Log("settings file not found at " + filePath);
                    return Default;
                }
                json = File.ReadAllText(filePath);
                dynamic j = JsonConvert.DeserializeObject(json);
                if (IsPropertyExist(j, name))
                {
                    if (int.TryParse(j[name].ToString(), out value))
                        return value;
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("GetSettingsInt:" + name +"\r\n"+  ex.ToString());

                ex.ToExceptionless().FirstCarUserID().AddObject(json, "JSON").Submit();
            }
            return Default;
        }

        internal static string GetMapProvider()
        {
            string mapProvider = "OSMMapProvider"; // default
            try
            {
                string filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);

                if (!File.Exists(filePath))
                {
                    return mapProvider;
                }

                string json = File.ReadAllText(filePath);
                dynamic j = JsonConvert.DeserializeObject(json);

                if (IsPropertyExist(j, "MapProvider"))
                {
                    if (j["MapProvider"] == "MapQuest")
                    {
                        return "MapQuest";
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            return mapProvider;
        }

        internal static void ExternalLog(string text)
        {
            /*

            try
            {
                text = "V:" + Assembly.GetExecutingAssembly().GetName().Version + " - " + text;
                var c = WebHelper.httpclient_teslalogger_de;

                UriBuilder b = new UriBuilder("https://teslalogger.de/log.php");
                b.Port = -1;
                var q = HttpUtility.ParseQueryString(b.Query);
                q["t"] = text;
                b.Query = q.ToString();
                string url = b.ToString();

                HttpResponseMessage result = c.GetAsync(new Uri(url)).Result;
                string resultContent = result.Content.ReadAsStringAsync().Result;

            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            */
        }

        public static string ConvertBase64toString(string base64)
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
    }

    public static class EventBuilderExtension
    {
        static String lastFirstCar;
        public static EventBuilder FirstCarUserID(this EventBuilder v)
        {
            try
            {
                if (lastFirstCar != null)
                    return v.SetUserIdentity(lastFirstCar);

                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT tasker_hash FROM cars where length(tasker_hash) >= 8 limit 1", con))
                    {
                        object o = cmd.ExecuteScalar()?.ToString();
                        if (o is String && o.ToString().Length >= 8)
                        {
                            lastFirstCar = o.ToString();
                            return v.SetUserIdentity(o.ToString());
                        }
                    }
                }
                
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            return v;
        }
    }
}
