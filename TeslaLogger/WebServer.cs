using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TeslaLogger
{
    public class WebServer : IDisposable
    {
        private HttpListener listener = null;
        private bool isDisposed;

        private readonly List<string> AllowedTeslaAPICommands = new List<string>()
        {
            "auto_conditioning_start",
            "auto_conditioning_stop",
            "auto_conditioning_toggle",
            "sentry_mode_on",
            "sentry_mode_off",
            "sentry_mode_toggle",
            "wake_up",
            "set_charge_limit"
        };

        public WebServer()
        {
            if (!HttpListener.IsSupported)
            {
                Logfile.Log("HttpListener is not Supported!!!");
                return;
            }

            try
            {
                int httpport = Tools.GetHttpPort();
                listener = new HttpListener();
                listener.Prefixes.Add($"http://*:{httpport}/");
                listener.Start();

                Logfile.Log($"HttpListener bound to http://*:{httpport}/");
            }
            catch (HttpListenerException hlex)
            {
                listener = null;
                if (((UInt32)hlex.HResult) == 0x80004005)
                {
                    Logfile.Log("HTTPListener access denied. Check https://stackoverflow.com/questions/4019466/httplistener-access-denied");
                }
                else
                {
                    Logfile.Log(hlex.ToString());
                }
            }
            catch (Exception ex)
            {
                listener = null;
                Logfile.Log(ex.ToString());
            }

            try
            {
                if (listener == null)
                {
                    int httpport = Tools.GetHttpPort();
                    listener = new HttpListener();
                    listener.Prefixes.Add($"http://localhost:{httpport}/");
                    listener.Start();

                    Logfile.Log($"HTTPListener only bound to Localhost:{httpport}!");
                }
            }
            catch (HttpListenerException hlex)
            {
                listener = null;
                if (((UInt32)hlex.HResult) == 0x80004005)
                {
                    Logfile.Log("HTTPListener access denied. Check https://stackoverflow.com/questions/4019466/httplistener-access-denied");
                }
                else
                {
                    Logfile.Log(hlex.ToString());
                }
            }
            catch (Exception ex)
            {
                listener = null;
                Logfile.Log(ex.ToString());
            }

            while (listener != null)
            {
                try
                {
                    ThreadPool.QueueUserWorkItem(OnContext, listener.GetContext());
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }
            }
        }

        void WriteFile(HttpListenerResponse response, string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                string filename = Path.GetFileName(path);
                //response is HttpListenerContext.Response...
                response.ContentLength64 = fs.Length;
                response.SendChunked = false;
                response.ContentType = System.Net.Mime.MediaTypeNames.Application.Octet;
                response.AddHeader("Content-disposition", "attachment; filename=" + filename);

                byte[] buffer = new byte[64 * 1024];
                int read;
                using (BinaryWriter bw = new BinaryWriter(response.OutputStream))
                {
                    while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bw.Write(buffer, 0, read);
                        bw.Flush(); //seems to have no effect
                    }

                    bw.Close();
                }

                response.StatusCode = (int)HttpStatusCode.OK;
                response.StatusDescription = "OK";
                response.OutputStream.Close();
            }
        }

        private void OnContext(object o)
        {
            string localpath = "";

            try
            {
                HttpListenerContext context = o as HttpListenerContext;

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                if (request.Url.LocalPath != null)
                {
                    localpath = request.Url.LocalPath;
                }
                else
                {
                    localpath = "NULL";
                }

                switch (true)
                {
                    // commands for admin UI
                    case bool _ when request.Url.LocalPath.Equals("/getchargingstate"):
                        Getchargingstate(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/setcost"):
                        Setcost(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/getallcars"):
                        GetAllCars(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/setpassword"):
                        SetPassword(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/admin/UpdateElevation"):
                        Admin_UpdateElevation(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/admin/OpenTopoDataQueue"):
                        Admin_OpenTopoDataQueue(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/admin/ReloadGeofence"):
                        Admin_ReloadGeofence(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/admin/GetPOI"):
                        Admin_GetPOI(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/admin/update"):
                        Admin_Update(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/admin/updategrafana"):
                        updategrafana(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/admin/downloadlogs"):
                        Admin_DownloadLogs(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/export/trip"):
                        ExportTrip(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/passwortinfo"):
                        passwortinfo(request, response);
                        break;
                    // get car values
                    case bool _ when Regex.IsMatch(request.Url.LocalPath, @"/get/[0-9]+/.+"):
                        Get_CarValue(request, response);
                        break;
                    // static map service
                    case bool _ when request.Url.LocalPath.Equals("/get/map"):
                        GetStaticMap(request, response);
                        break;
                    // send car commands
                    case bool _ when Regex.IsMatch(request.Url.LocalPath, @"/command/[0-9]+/.+"):
                        SendCarCommand(request, response);
                        break;
                    case bool _ when Regex.IsMatch(request.Url.LocalPath, @"/currentjson/[0-9]+"):
                        GetCurrentJson(request, response);
                        break;
                    case bool _ when Regex.IsMatch(request.Url.LocalPath, @"/decodecar/[0-9]+"):
                        DecodeCar(request, response);
                        break;
                    // Tesla API debug
                    case bool _ when Regex.IsMatch(request.Url.LocalPath, @"/debug/TeslaAPI/[0-9]+/.+"):
                        Debug_TeslaAPI(request, response);
                        break;
                    case bool _ when Regex.IsMatch(request.Url.LocalPath, @"/mfa/[0-9]+/.+"):
                        Set_MFA(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/debug/TeslaLogger/states"):
                        Debug_TeslaLoggerStates(request, response);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/debug/TeslaLogger/messages"):
                        Debug_TeslaLoggerMessages(request, response);
                        break;
                    // developer features
                    case bool _ when request.Url.LocalPath.Equals("/dev/dumpJSON/on"):
                        Dev_DumpJSON(response, true);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/dev/dumpJSON/off"):
                        Dev_DumpJSON(response, false);
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/dev/verbose/on"):
                        Program.VERBOSE = true;
                        Logfile.Log("VERBOSE on");
                        WriteString(response, "VERBOSE on");
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/dev/verbose/off"):
                        Program.VERBOSE = false;
                        Logfile.Log("VERBOSE off");
                        WriteString(response, "VERBOSE off");
                        break;
                    case bool _ when request.Url.LocalPath.Equals("/logfile"):
                        GetLogfile(response);
                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        WriteString(response, @"URL Not Found!");
                        break;
                }

            }
            catch (Exception ex)
            {
                Logfile.Log($"Localpath: {localpath}\r\n" + ex.ToString());
            }
        }

        private void GetStaticMap(HttpListenerRequest request, HttpListenerResponse response)
        {
            int startPosID = 0;
            int endPosID = 0;
            int width = 240;
            int height = 0;
            StaticMapProvider.MapType type = StaticMapProvider.MapType.Trip;
            StaticMapProvider.MapMode mode = StaticMapProvider.MapMode.Regular;
            if (request.QueryString.HasKeys())
            {
                foreach (string key in request.QueryString.AllKeys)
                {
                    switch (key)
                    {
                        case "start":
                            _ = int.TryParse(request.QueryString.GetValues(key)[0], out startPosID);
                            break;
                        case "end":
                            _ = int.TryParse(request.QueryString.GetValues(key)[0], out endPosID);
                            break;
                        case "width":
                            _ = int.TryParse(request.QueryString.GetValues(key)[0], out width);
                            break;
                        case "height":
                            _ = int.TryParse(request.QueryString.GetValues(key)[0], out height);
                            break;
                        case "mode":
                            if ("dark".Equals(request.QueryString.GetValues(key)[0]))
                            {
                                mode = StaticMapProvider.MapMode.Dark;
                            }
                            break;
                        case "type":
                            if ("park".Equals(request.QueryString.GetValues(key)[0]))
                            {
                                type = StaticMapProvider.MapType.Park;
                            }
                            else if ("charge".Equals(request.QueryString.GetValues(key)[0]))
                            {
                                type = StaticMapProvider.MapType.Charge;
                            }
                            break;
                    }
                }
            }
            if (startPosID != 0 && endPosID != 0)
            {
                try
                {
                    string path = FileManager.GetMapCachePath() + Path.DirectorySeparatorChar + $"map_{startPosID}_{endPosID}.png";
                    // check file age
                    if (File.Exists(path))
                    {
                        if ((DateTime.UtcNow - File.GetCreationTimeUtc(path)).TotalDays > 90)
                        {
                            File.Delete(path);
                        }
                    }
                    if (File.Exists(path))
                    {
                        using (FileStream fs = File.OpenRead(path))
                        {
                            WritePNGStream(response, fs);
                            return;
                        }
                    }
                    else
                    {
                        // order static map generation
                        StaticMapService.GetSingleton().Enqueue(1, startPosID, endPosID, width, height, mode, StaticMapProvider.MapSpecial.None);
                        // wait
                        for (int i = 0; i < 30; i++)
                        {
                            Thread.Sleep(1000);
                            if (File.Exists(path))
                            {
                                using (FileStream fs = File.OpenRead(path))
                                {
                                    WritePNGStream(response, fs);
                                    return;
                                }
                            }
                        }
                        WriteString(response, "Error generating map took too long");
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }
            }
            else
            {
                try
                {
                    WriteString(response, "Error in map request");
                }
                catch (Exception ex)
                {
                    // ignore
                }
            }
        }

        private static void WritePNGStream(HttpListenerResponse response, Stream fs)
        {
            response.ContentLength64 = fs.Length;
            response.SendChunked = false;
            response.ContentType = "image/png";
            response.StatusCode = (int)HttpStatusCode.OK;
            response.StatusDescription = "OK";
            fs.CopyTo(response.OutputStream);
            response.OutputStream.Close();
        }

        private void Set_MFA(HttpListenerRequest request, HttpListenerResponse response)
        {
            Match m = Regex.Match(request.Url.LocalPath, @"/mfa/([0-9]+)/(.+)");
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1)
            {
                int.TryParse(m.Groups[1].Captures[0].ToString(), out int CarID);
                string mfa = m.Groups[2].Captures[0].ToString();
                if (mfa.Length > 0 && CarID > 0)
                {
                    Car car = Car.GetCarByID(CarID);
                    if (car != null)
                    {
                        car.passwortinfo.Append("Send MFA to Tesla server<br>");
                        car.MFA_Code = mfa;
                        car.waitForMFACode = false;
                    }
                }
            }
            WriteString(response, "");
        }

        private void Admin_DownloadLogs(HttpListenerRequest request, HttpListenerResponse response)
        {
            Queue<string> result = new Queue<string>();
            // set defaults
            DateTime startdt = DateTime.Now.AddHours(-48);
            DateTime enddt = DateTime.Now.AddSeconds(1);
            // parse query string
            if (request.QueryString.Count > 0 && request.QueryString.HasKeys())
            {
                foreach (string key in request.QueryString.AllKeys)
                {
                    if (request.QueryString.GetValues(key).Length == 1)
                    {
                        switch (key)
                        {
                            case "from":
                                Tools.DebugLog($"from {request.QueryString.GetValues(key)[0]}");
                                if (!DateTime.TryParse(request.QueryString.GetValues(key)[0], out startdt))
                                {
                                    startdt = DateTime.Now.AddHours(-48);
                                }
                                break;
                            case "to":
                                Tools.DebugLog($"to {request.QueryString.GetValues(key)[0]}");
                                if (!DateTime.TryParse(request.QueryString.GetValues(key)[0], out enddt))
                                {
                                    enddt = DateTime.Now.AddSeconds(1);
                                }
                                break;
                        }
                    }
                }
            }
            if (File.Exists(Path.Combine(Logfile.GetExecutingPath(), "nohup.out")))
            {
                System.Globalization.CultureInfo ciDeDE = new System.Globalization.CultureInfo("de-DE");
                int linenumber = 0;
                int startlinenumber = 0;
                int endlinennumber = 0;
                int TLstartlinenumber = 0;
                string startdate = startdt.ToString(ciDeDE);
                string enddate = enddt.ToString(ciDeDE);
                Tools.DebugLog($"startdate {startdate}");
                Tools.DebugLog($"enddate {enddate}");
                // parse nohup.out
                foreach (string line in File.ReadAllLines(Path.Combine(Logfile.GetExecutingPath(), "nohup.out")))
                {
                    if (startlinenumber == 0)
                    {
                        if (line.Contains(" : TeslaLogger Version: "))
                        {
                            TLstartlinenumber = linenumber;
                        }
                    }
                    if (startlinenumber == 0 && line.Length > startdate.Length && DateTime.TryParse(line.Substring(0, startdate.Length), ciDeDE, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime linedt) && linedt >= startdt)
                    {
                        startlinenumber = linenumber;
                    }
                    if (endlinennumber == 0 && line.Length > startdate.Length && DateTime.TryParse(line.Substring(0, enddate.Length), ciDeDE, System.Globalization.DateTimeStyles.AssumeLocal, out linedt) && linedt >= enddt)
                    {
                        endlinennumber = linenumber;
                    }
                    linenumber++;
                }
                Tools.DebugLog($"linenumber {linenumber}");
                if (endlinennumber == 0)
                {
                    endlinennumber = linenumber - 1;
                }
                Tools.DebugLog($"TLstartlinenumber {TLstartlinenumber}");
                Tools.DebugLog($"startlinenumber {startlinenumber}");
                Tools.DebugLog($"endlinennumber {endlinennumber}");
                // grab line from nohup.out
                linenumber = 0;
                // do TLstartlinenumber + 17 and startlinenumber overlap?
                if (startlinenumber - TLstartlinenumber < 17)
                {
                    startlinenumber += 17 - (TLstartlinenumber - startlinenumber);
                }
                foreach (string line in File.ReadAllLines(Path.Combine(Logfile.GetExecutingPath(), "nohup.out")))
                {
                    // TL start was before startlinenumber
                    if (TLstartlinenumber < startlinenumber)
                    {
                        if (linenumber >= TLstartlinenumber && linenumber <= TLstartlinenumber + 17)
                        {
                            result.Enqueue(line);
                        }
                    }
                    if (linenumber >= startlinenumber && linenumber <= endlinennumber)
                    {
                        result.Enqueue(line);
                    }
                    linenumber++;
                }
            }
            WriteString(response, string.Join(Environment.NewLine, result));
        }

        private void Admin_OpenTopoDataQueue(HttpListenerRequest request, HttpListenerResponse response)
        {
            Logfile.Log("Admin: OpenTopoDataQueue ...");
            if (Tools.UseOpenTopoData())
            {
                double queue = OpenTopoDataService.GetSingleton().GetQueueLength();
                // 100 pos every 2 Minutes
                WriteString(response, $"OpenTopoData Queue contains {queue} positions. It will take approx {Math.Ceiling(queue / 100) * 2} minutes to process.");
            }
            else
            {
                WriteString(response, "OpenTopoData is disabled");
            }
        }

        private void ExportTrip(HttpListenerRequest request, HttpListenerResponse response)
        {
            // source: https://github.com/rowich/Teslalogger2gpx/blob/master/Teslalogger2GPX.ps1
            // parse request
            if (request.QueryString.Count == 3 && request.QueryString.HasKeys())
            {
                long from = long.MinValue;
                long to = long.MinValue;
                int carID = int.MinValue;
                foreach (string key in request.QueryString.AllKeys)
                {
                    if (request.QueryString.GetValues(key).Length == 1)
                    {
                        switch (key)
                        {
                            case "from":
                                long.TryParse(request.QueryString.GetValues(key)[0], out from);
                                break;
                            case "to":
                                long.TryParse(request.QueryString.GetValues(key)[0], out to);
                                break;
                            case "carID":
                                int.TryParse(request.QueryString.GetValues(key)[0], out carID);
                                break;
                        }
                    }
                }
                if (from != long.MinValue && to != long.MinValue && carID != int.MinValue)
                {
                    // request parsed successfully
                    // create GPX header
                    StringBuilder GPX = new StringBuilder();
                    GPX.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>
<gpx version=""1.1"" creator=""Teslalogger GPX Export"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:gpxtpx=""http://www.garmin.com/xmlschemas/TrackPointExtension/v1"" xmlns:gpxx=""http://www.garmin.com/xmlschemas/GpxExtensions/v3"" elementFormDefault=""qualified"">
<metadata>
    <name>teslalogger.gpx</name>
</metadata>
");
                    string DateLast = "n/a";
                    string PosLast = "n/a";
                    // now get pos data
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
                        using (MySqlCommand cmd = new MySqlCommand("SELECT lat,lng,Datum,altitude,address FROM pos WHERE id >= @from AND id <= @to and CarID = @CarID ORDER BY Datum ASC", con))
                        {
                            cmd.Parameters.AddWithValue("@from", from);
                            cmd.Parameters.AddWithValue("@to", to);
                            cmd.Parameters.AddWithValue("@CarID", carID);
                            Tools.DebugLog(cmd);
                            MySqlDataReader dr = cmd.ExecuteReader();
                            while (dr.Read())
                            {
                                if (double.TryParse(dr[0].ToString(), out double lat)
                                     && double.TryParse(dr[1].ToString(), out double lng)
                                     && DateTime.TryParse(dr[2].ToString(), out DateTime Datum))
                                {
                                    string Pos = ($"lat=\"{lat}\" lon=\"{lng}\"");
                                    if (!Pos.Equals(PosLast))
                                    {
                                        // convert date/time into GPX format (insert a "T")
                                        // 2020-01-30 09:19:55 --> 2020-01-30T09:19:55
                                        string Date = Datum.ToString("yyyy-MM-dd") + "T" + Datum.ToString("HH:mm:ss");
                                        string alt = "";
                                        if (double.TryParse(dr[3].ToString(), out double altitude))
                                        {
                                            alt = $"<ele>{altitude}</ele>";
                                        }
                                        string name = "";
                                        if (dr[4] != null && dr[4] != DBNull.Value)
                                        {
                                            name = $"<name>{SecurityElement.Escape(dr[4].ToString())}</name>";
                                        }
                                        // create new Track element if day has changed since last element. New track node gets the name of the day (allows filtering for days later on)
                                        if (!DateLast.Equals(Date.Substring(0, 10)))
                                        {
                                            if (!DateLast.Equals("n/a"))
                                            {
                                                GPX.Append("</trkseg></trk>" + Environment.NewLine);
                                            }
                                            DateLast = Date.Substring(0, 10);
                                            GPX.Append($"<trk><name>{DateLast}</name><trkseg>" + Environment.NewLine);
                                        }
                                        GPX.Append($"    <trkpt {Pos}>{alt}<time>{Date}</time>{name}</trkpt>" + Environment.NewLine);
                                        PosLast = Pos;
                                    }
                                }
                            }
                        }
                    }
                    // create GPX footer
                    GPX.Append(@"</trkseg>
</trk>
</gpx>
");
                    response.AddHeader("Content-Type", "application/gpx+xml; charset=utf-8");
                    response.AddHeader("Content-Disposition", "inline; filename=\"trip.gpx\"");
                    Tools.DebugLog("GPX:" + Environment.NewLine + GPX.ToString());
                    WriteString(response, GPX.ToString());
                }
                else
                {
                    WriteString(response, "error parsing request");
                }
            }
            else
            {
                WriteString(response, "malformed request");
            }
        }

        private void GetLogfile(HttpListenerResponse response)
        {
            try
            {
                string logfilePath = Path.Combine(FileManager.GetExecutingPath(), "nohup.out");

                if (Directory.Exists("zip"))
                    Directory.Delete("zip", true);

                Directory.CreateDirectory("zip");
                File.Copy(logfilePath, "zip/logfile.txt");

                if (File.Exists("logfile.zip"))
                    File.Delete("logfile.zip");

                ZipFile.CreateFromDirectory("zip", "logfile.zip");

                WriteFile(response, "logfile.zip");
            }
            catch (Exception ex)
            {
                WriteString(response, ex.ToString());
                Logfile.Log(ex.ToString());
            }
        }

        private void Debug_TeslaLoggerMessages(HttpListenerRequest request, HttpListenerResponse response)
        {
            response.AddHeader("Content-Type", "text/html; charset=utf-8");
            WriteString(response, "<html><head></head><body><table border=\"1\">" + string.Concat(Tools.debugBuffer.Select(a => string.Format("<tr><td>{0}&nbsp;{1}</td></tr>", a.Key, a.Value))) + "</table></body></html>");
        }
        
        private void passwortinfo(HttpListenerRequest request, HttpListenerResponse response)
        {
            System.Diagnostics.Debug.WriteLine("passwortinfo");
            string data = GetDataFromRequestInputStream(request);
            int id = 0;

            if (String.IsNullOrEmpty(data))
            {
                id = Convert.ToInt32(request.QueryString["id"]);
            }
            else
            {
                dynamic r = new JavaScriptSerializer().DeserializeObject(data);
                id = Convert.ToInt32(r["id"]);
            }

            var c = Car.GetCarByID(id);
            if (c != null)
                WriteString(response, c.passwortinfo.ToString());
            else
                WriteString(response, "CarId not found: " + id);
        }

        private static string GetDataFromRequestInputStream(HttpListenerRequest request)
        {
            string data;
            using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                data = reader.ReadToEnd();
            }

            return data;
        }

        private void GetCurrentJson(HttpListenerRequest request, HttpListenerResponse response)
        {
            System.Diagnostics.Debug.WriteLine(request.Url.LocalPath);

            Match m = Regex.Match(request.Url.LocalPath, @"/currentjson/([0-9]+)");
            if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
            {
                int.TryParse(m.Groups[1].Captures[0].ToString(), out int CarID);
                try
                {
                    if (CurrentJSON.jsonStringHolder.TryGetValue(CarID, out string json))
                    {
                        WriteString(response, json);
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        WriteString(response, @"URL Not Found!");
                    }
                }
                catch (Exception ex)
                {
                    WriteString(response, ex.ToString());
                    Logfile.ExceptionWriter(ex, request.Url.LocalPath);
                }
            }
        }

        private void DecodeCar(HttpListenerRequest request, HttpListenerResponse response)
        {
            System.Diagnostics.Debug.WriteLine(request.Url.LocalPath);

            Match m = Regex.Match(request.Url.LocalPath, @"/decodecar/([0-9]+)");
            if (m.Success && m.Groups.Count == 2 && m.Groups[1].Captures.Count == 1)
            {
                int.TryParse(m.Groups[1].Captures[0].ToString(), out int CarID);
                try
                {
                    StringBuilder sb = new StringBuilder();
                    Car c = Car.GetCarByID(CarID);

                    c.webhelper.lastUpdateEfficiency = DateTime.Now.AddDays(-1);
                    string s = c.webhelper.Wakeup().Result;
                    string io = c.webhelper.IsOnline().Result;

                    c.webhelper.UpdateEfficiency();

                    sb.Append("ModelName:").Append(c.ModelName).Append("\r\n");
                    sb.Append("Wh_TR:").Append(c.Wh_TR).Append("\r\n").Append("\r\n");

                    int maxRange = c.dbHelper.GetAvgMaxRage();
                    sb.Append("AvgMaxRage:").Append(maxRange).Append("\r\n").Append("\r\n");

                    sb.Append("display_name:").Append(c.display_name).Append("\r\n");
                    sb.Append("vin:").Append(c.vin.Substring(0,11)).Append("XXXXXX").Append("\r\n");
                    sb.Append("car_type:").Append(c.car_type).Append("\r\n");
                    sb.Append("car_special_type:").Append(c.car_special_type).Append("\r\n");
                    sb.Append("trim_badging:").Append(c.trim_badging).Append("\r\n");
                    
                    c.GetTeslaAPIState().GetBool("has_ludicrous_mode", out bool has_ludicrous_mode);
                    sb.Append("has_ludicrous_mode:").Append(has_ludicrous_mode).Append("\r\n");
                    sb.Append("DB_Wh_TR:").Append(c.DB_Wh_TR).Append("\r\n").Append("\r\n");

                    Tools.VINDecoder(c.vin, out int year, out string carType, out bool AWD, out bool MIC, out string batery, out string motor);
                    sb.Append("VIN Year:").Append(year).Append("\r\n");
                    sb.Append("VIN carType:").Append(carType).Append("\r\n");
                    sb.Append("VIN AWD:").Append(AWD).Append("\r\n");
                    sb.Append("VIN MIC:").Append(MIC).Append("\r\n");
                    sb.Append("VIN batery:").Append(batery).Append("\r\n");
                    sb.Append("VIN motor:").Append(motor).Append("\r\n");

                    sb.Append("Voltage at 50% SOC:").Append(c.dbHelper.GetVoltageAt50PercentSOC(out DateTime startdate, out DateTime ende)).Append("V Date:").Append(startdate).Append("\r\n");

                    string vehicle_config = "";

                    for (int retry = 0; retry < 10; retry++)
                    {
                        vehicle_config = c.webhelper.GetCommand("vehicle_config").Result;
                        if (vehicle_config?.Trim()?.StartsWith("{") == true)
                            break;

                        System.Threading.Thread.Sleep(2000);
                    }

                    sb.Append("Vehicle Config:").Append("\r\n").Append(vehicle_config).Append("\r\n");

                    WriteString(response, sb.ToString());
                }
                catch (Exception ex)
                {
                    WriteString(response, ex.ToString());
                    Logfile.ExceptionWriter(ex, request.Url.LocalPath);
                }
            }
        }

        private void updategrafana(HttpListenerRequest request, HttpListenerResponse response)
        {
            Tools.lastGrafanaSettings = DateTime.UtcNow.AddDays(-1);
            Task.Run(() => { UpdateTeslalogger.UpdateGrafana(); });
            WriteString(response, @"OK");
        }

        private void Admin_Update(HttpListenerRequest request, HttpListenerResponse response)
        {
            // TODO copy what update.php does
            WriteString(response, "");
        }

        private void Dev_DumpJSON(HttpListenerResponse response, bool dumpJSON)
        {
            foreach (Car car in Car.allcars)
            {
                if (car.GetTeslaAPIState().DumpJSON != dumpJSON)
                {
                    car.GetTeslaAPIState().DumpJSON = dumpJSON;
                    if (dumpJSON)
                    {
                        // get /vehicles at session start
                        _ = car.webhelper.IsOnline().Result;
                    }
                }
            }
            WriteString(response, $"DumpJSON {dumpJSON}");
        }

        private void SendCarCommand(HttpListenerRequest request, HttpListenerResponse response)
        {
            Match m = Regex.Match(request.Url.LocalPath, @"/command/([0-9]+)/(.+)");
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1)
            {
                int.TryParse(m.Groups[1].Captures[0].ToString(), out int CarID);
                string command = m.Groups[2].Captures[0].ToString();
                if (command.Length > 0 && CarID > 0)
                {
                    Car car = Car.GetCarByID(CarID);
                    if (car != null)
                    {
                        // check if command is in list of allowed commands
                        if (AllowedTeslaAPICommands.Contains(command))
                        {
                            switch (command)
                            {
                                case "auto_conditioning_start":
                                    WriteString(response, car.webhelper.PostCommand("command/auto_conditioning_start", null).Result);
                                    break;
                                case "auto_conditioning_stop":
                                    WriteString(response, car.webhelper.PostCommand("command/auto_conditioning_stop", null).Result);
                                    break;
                                case "auto_conditioning_toggle":
                                    if (car.currentJSON.current_is_preconditioning)
                                    {
                                        WriteString(response, car.webhelper.PostCommand("command/auto_conditioning_stop", null).Result);
                                    }
                                    else
                                    {
                                        WriteString(response, car.webhelper.PostCommand("command/auto_conditioning_start", null).Result);
                                    }
                                    break;
                                case "sentry_mode_on":
                                    WriteString(response, car.webhelper.PostCommand("command/set_sentry_mode", "{\"on\":true}", true).Result);
                                    break;
                                case "sentry_mode_off":
                                    WriteString(response, car.webhelper.PostCommand("command/set_sentry_mode", "{\"on\":false}", true).Result);
                                    break;
                                case "sentry_mode_toggle":
                                    if (car.webhelper.is_sentry_mode)
                                    {
                                        WriteString(response, car.webhelper.PostCommand("command/set_sentry_mode", "{\"on\":false}", true).Result);
                                    }
                                    else
                                    {
                                        WriteString(response, car.webhelper.PostCommand("command/set_sentry_mode", "{\"on\":true}", true).Result);
                                    }
                                    break;
                                case "wake_up":
                                    WriteString(response, car.webhelper.Wakeup().Result);
                                    break;
                                case "set_charge_limit":
                                    if (request.QueryString.Count == 1 && int.TryParse(string.Concat(request.QueryString.GetValues(0)), out int newChargeLimit))
                                    {
                                        Address addr = Geofence.GetInstance().GetPOI(car.currentJSON.latitude, car.currentJSON.longitude, false);
                                        if (addr != null)
                                        {
                                            car.Log($"SetChargeLimit to {newChargeLimit} at '{addr.name}' ...");
                                            car.LastSetChargeLimitAddressName = addr.name;
                                        }
                                        WriteString(response, car.webhelper.PostCommand("command/set_charge_limit", "{\"percent\":" + newChargeLimit + "}", true).Result);
                                    }
                                    break;
                                default:
                                    WriteString(response, "");
                                    break;
                            }
                            return;
                        }
                    }
                }
            }
            WriteString(response, "");
        }

        private void SetPassword(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                Logfile.Log("SetPassword");

                string data = GetDataFromRequestInputStream(request);

                dynamic r = new JavaScriptSerializer().DeserializeObject(data);

                int id = Convert.ToInt32(r["id"]);

                if (Tools.IsPropertyExist(r, "deletecar"))
                {
                    Logfile.Log("Delete Car #" + id);

                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();


                        using (var cmd2 = new MySqlCommand("delete from cars where id = @id", con))
                        {
                            cmd2.Parameters.AddWithValue("@id", id);
                            cmd2.ExecuteNonQuery();

                            Car c = Car.GetCarByID(id);
                            if (c != null)
                            {
                                c.ExitTeslaLogger("Car deleted!");
                            }

                            WriteString(response, "OK");
                        }
                    }
                }
                else if (Tools.IsPropertyExist(r, "reconnect"))
                {
                    Logfile.Log("reconnect Car #" + id);

                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();


                        using (var cmd2 = new MySqlCommand("update cars set tesla_token='', refresh_token='' where id = @id", con))
                        {
                            cmd2.Parameters.AddWithValue("@id", id);
                            cmd2.ExecuteNonQuery();

                            Car c = Car.GetCarByID(id);
                            if (c != null)
                            {
                                c.ExitTeslaLogger("Reconnect!");
                                c.passwortinfo = new StringBuilder();

                                c.ThreadJoin();

                                Logfile.Log("Start Reconnect!");

                                Car nc = new Car(c.CarInDB, c.TeslaName, c.TeslaPasswort, c.CarInAccount, "", DateTime.MinValue, c.ModelName, c.car_type, c.car_special_type, c.trim_badging, c.display_name, c.vin, c.TaskerHash, c.Wh_TR);
                            }

                            WriteString(response, "OK");
                        }
                    }
                }
                else
                {
                    int teslacarid = Convert.ToInt32(r["carid"]);
                    string email = r["email"];
                    string password = r["password"];
                    bool freesuc = r["freesuc"];

                    if (id == -1)
                    {
                        Logfile.Log("Insert Password");

                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();

                            using (MySqlCommand cmd = new MySqlCommand("select max(id)+1 from cars", con))
                            {
                                long newid = cmd.ExecuteScalar() as long? ?? 1;

                                using (var cmd2 = new MySqlCommand("insert cars (id, tesla_name, tesla_password, tesla_carid, display_name, freesuc) values (@id, @tesla_name, @tesla_password, @tesla_carid, @display_name, @freesuc)", con))
                                {
                                    cmd2.Parameters.AddWithValue("@id", newid);
                                    cmd2.Parameters.AddWithValue("@tesla_name", email);
                                    cmd2.Parameters.AddWithValue("@tesla_password", password);
                                    cmd2.Parameters.AddWithValue("@tesla_carid", teslacarid);
                                    cmd2.Parameters.AddWithValue("@display_name", "Car " + newid);
                                    cmd2.Parameters.AddWithValue("@freesuc", freesuc ? 1 : 0);
                                    cmd2.ExecuteNonQuery();

                                    Car nc = new Car(Convert.ToInt32(newid), email, password, teslacarid, "", DateTime.MinValue, "", "", "", "", "", "", "", null);

                                    WriteString(response, "ID:"+newid);
                                }
                            }
                        }
                    }
                    else
                    {
                        Logfile.Log("Update Password ID:" + id);
                        int dbID = Convert.ToInt32(id);

                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();

                            using (MySqlCommand cmd = new MySqlCommand("update cars set tesla_name=@tesla_name, tesla_password=@tesla_password, tesla_carid=@tesla_carid, freesuc=@freesuc,  tesla_token='', refresh_token='' where id=@id", con))
                            {
                                cmd.Parameters.AddWithValue("@id", dbID);
                                cmd.Parameters.AddWithValue("@tesla_name", email);
                                cmd.Parameters.AddWithValue("@tesla_password", password);
                                cmd.Parameters.AddWithValue("@tesla_carid", teslacarid);
                                cmd.Parameters.AddWithValue("@freesuc", freesuc ? 1 : 0);
                                cmd.ExecuteNonQuery();

                                Car c = Car.GetCarByID(dbID);
                                if (c != null)
                                {
                                    c.ExitTeslaLogger("Credentials changed!");
                                }

                                Car nc = new Car(dbID, email, password, teslacarid, "", DateTime.MinValue, "", "", "", "", "", "", "", null);
                                WriteString(response, "OK");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteString(response, "ERROR");
                Logfile.Log(ex.ToString());
            }
        }

        private void Get_CarValue(HttpListenerRequest request, HttpListenerResponse response)
        {
            Match m = Regex.Match(request.Url.LocalPath, @"/get/([0-9]+)/(.+)");
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1)
            {
                int.TryParse(m.Groups[1].Captures[0].ToString(), out int CarID);
                string name = m.Groups[2].Captures[0].ToString();
                if (name.Length > 0 && CarID > 0)
                {
                    Car car = Car.GetCarByID(CarID);
                    if (car != null)
                    {
                        if (car.GetTeslaAPIState().GetState(name, out Dictionary<TeslaAPIState.Key, object> state))
                        {
                            if (request.QueryString.Count == 1 && string.Concat(request.QueryString.GetValues(0)).Equals("raw"))
                            {
                                WriteString(response, state[TeslaAPIState.Key.Value].ToString());
                                return;
                            }
                            else
                            {
                                response.AddHeader("Content-Type", "application/json; charset=utf-8");
                                WriteString(response, "{\"response\":{ \"value\":\"" + state[TeslaAPIState.Key.Value].ToString() + "\", \"timestamp\":" + state[TeslaAPIState.Key.Timestamp] + "} }");
                                return;
                            }
                        }
                    }
                }
            }
            WriteString(response, "");
        }

        private static void Admin_ReloadGeofence(HttpListenerRequest request, HttpListenerResponse response)
        {
            Logfile.Log("Admin: ReloadGeofence ...");
            Geofence.GetInstance().Init();

            if (request.QueryString.Count == 1 && string.Concat(request.QueryString.GetValues(0)).Equals("html"))
            {
                IEnumerable<string> geofence = Geofence.GetInstance().geofenceList.Select(
                    a => string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>geofence</td></tr>",
                        a.name,
                        a.lat,
                        a.lng,
                        a.radius,
                        string.Concat(a.specialFlags.Select(
                            sp => string.Format("{0}<br/>",
                            sp.ToString()))
                        )
                    )
                );
                IEnumerable<string> geofenceprivate = Geofence.GetInstance().geofencePrivateList.Select(
                    a => string.Format("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>geofence-private</td></tr>",
                        a.name,
                        a.lat,
                        a.lng,
                        a.radius,
                        string.Concat(a.specialFlags.Select(
                            sp => string.Format("{0}<br/>",
                            sp.ToString()))
                        )
                    )
                );
                response.AddHeader("Content-Type", "text/html; charset=utf-8");
                WriteString(response, "<html><head></head><body><table border=\"1\">" + string.Concat(geofence) + string.Concat(geofenceprivate) + "</table></body></html>");
            }
            else
            {
                WriteString(response, "{\"response\":{\"reason\":\"\", \"result\":true}}");
            }
            WebHelper.UpdateAllPOIAddresses();
            Logfile.Log("Admin: ReloadGeofence done");
        }

        private void Debug_TeslaLoggerStates(HttpListenerRequest request, HttpListenerResponse response)
        {
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                { "System.DateTime.Now", DateTime.Now.ToString() },
                { "System.DateTime.UtcNow", DateTime.UtcNow.ToString() },
                { "System.DateTime.UnixTime", Tools.ToUnixTime(DateTime.Now).ToString() },
                { "UpdateTeslalogger.lastVersionCheck", UpdateTeslalogger.GetLastVersionCheck().ToString() },
                {
                "TLMemCacheKey.Housekeeping",
                MemoryCache.Default.Get(Program.TLMemCacheKey.Housekeeping.ToString()) != null
                    ? "AbsoluteExpiration: " + ((CacheItemPolicy)MemoryCache.Default.Get(Program.TLMemCacheKey.Housekeeping.ToString())).AbsoluteExpiration.ToString()
                    : "null"
                },
            };

            foreach (Car car in Car.allcars)
            {
                Dictionary<string, string> carvalues = new Dictionary<string, string>
                {
                    { $"Car #{car.CarInDB} GetCurrentState()", car.GetCurrentState().ToString() },
                    { $"Car #{car.CarInDB} GetWebHelper().GetLastShiftState()", car.GetWebHelper().GetLastShiftState().ToString() },
                    { $"Car #{car.CarInDB} GetHighFrequencyLogging()", car.GetHighFrequencyLogging().ToString() },
                    { $"Car #{car.CarInDB} GetHighFrequencyLoggingTicks()", car.GetHighFrequencyLoggingTicks().ToString() },
                    { $"Car #{car.CarInDB} GetHighFrequencyLoggingTicksLimit()", car.GetHighFrequencyLoggingTicksLimit().ToString() },
                    { $"Car #{car.CarInDB} GetHighFrequencyLoggingUntil()", car.GetHighFrequencyLoggingUntil().ToString() },
                    { $"Car #{car.CarInDB} GetHighFrequencyLoggingMode()", car.GetHighFrequencyLoggingMode().ToString() },
                    { $"Car #{car.CarInDB} GetLastCarUsed()", car.GetLastCarUsed().ToString() },
                    { $"Car #{car.CarInDB} GetLastOdometerChanged()", car.GetLastOdometerChanged().ToString() },
                    { $"Car #{car.CarInDB} GetLastTryTokenRefresh()", car.GetLastTryTokenRefresh().ToString() },
                    { $"Car #{car.CarInDB} lastSetChargeLimitAddressName",
                        car.LastSetChargeLimitAddressName.Equals(string.Empty)
                        ? "&lt;&gt;"
                        : car.LastSetChargeLimitAddressName
                    },
                    { $"Car #{car.CarInDB} GetGoSleepWithWakeup()", car.GetGoSleepWithWakeup().ToString() },
                    { $"Car #{car.CarInDB} GetOdometerLastTrip()", car.GetOdometerLastTrip().ToString() },
                    { $"Car #{car.CarInDB} WebHelper.lastIsDriveTimestamp", car.GetWebHelper().lastIsDriveTimestamp.ToString() },
                    { $"Car #{car.CarInDB} WebHelper.lastUpdateEfficiency", car.GetWebHelper().lastUpdateEfficiency.ToString() },
                    { $"Car #{car.CarInDB} TeslaAPIState", car.GetTeslaAPIState().ToString(true).Replace(Environment.NewLine, "<br />") },
                };
                string carHTMLtable = "<table>" + string.Concat(carvalues.Select(a => string.Format("<tr><td>{0}</td><td>{1}</td></tr>", a.Key, a.Value))) + "</table>";
                values.Add($"Car #{car.CarInDB}", carHTMLtable);
            }

            /*{
                "TLMemCacheKey.GetOutsideTempAsync",
                MemoryCache.Default.Get(Program.TLMemCacheKey.GetOutsideTempAsync.ToString()) != null
                    ? ((double)MemoryCache.Default.Get(Program.TLMemCacheKey.GetOutsideTempAsync.ToString())).ToString()
                    : "null"
            },*/

            IEnumerable<string> trs = values.Select(a => string.Format("<tr><td>{0}</td><td>{1}</td></tr>", a.Key, a.Value));
            WriteString(response, "<html><head></head><body><table>" + string.Concat(trs) + "</table></body></html>");
        }

        private void Debug_TeslaAPI(HttpListenerRequest request, HttpListenerResponse response)
        {
            Match m = Regex.Match(request.Url.LocalPath, @"/debug/TeslaAPI/([0-9]+)/(.+)");
            if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1)
            {
                string value = m.Groups[2].Captures[0].ToString();
                int.TryParse(m.Groups[1].Captures[0].ToString(), out int CarID);
                if (value.Length > 0 && CarID > 0)
                {
                    Car car = Car.GetCarByID(CarID);
                    if (car != null && car.GetWebHelper().TeslaAPI_Commands.TryGetValue(value, out string TeslaAPIJSON))
                    {
                        response.AddHeader("Content-Type", "application/json; charset=utf-8");
                        WriteString(response, TeslaAPIJSON);
                    }
                    else
                    {
                        WriteString(response, "");
                    }
                }
                else
                {
                    WriteString(response, "");
                }
            }
            else
            {
                WriteString(response, "");
            }
        }

        private void Setcost(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                Logfile.Log("SetCost");

                string json;

                if (request.QueryString["JSON"] != null)
                {
                    json = request.QueryString["JSON"];
                }
                else
                {
                    json = GetDataFromRequestInputStream(request);
                }

                Logfile.Log("JSON: " + json);

                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("update chargingstate set cost_total = @cost_total, cost_currency=@cost_currency, cost_per_kwh=@cost_per_kwh, cost_per_session=@cost_per_session, cost_per_minute=@cost_per_minute, cost_idle_fee_total=@cost_idle_fee_total, cost_kwh_meter_invoice=@cost_kwh_meter_invoice  where id= @id", con))
                    {

                        if (DBHelper.DBNullIfEmptyOrZero(j["cost_total"]) is DBNull && DBHelper.IsZero(j["cost_per_session"]))
                        {
                            cmd.Parameters.AddWithValue("@cost_total", 0);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@cost_total", DBHelper.DBNullIfEmptyOrZero(j["cost_total"]));
                        }

                        cmd.Parameters.AddWithValue("@cost_currency", DBHelper.DBNullIfEmpty(j["cost_currency"]));
                        cmd.Parameters.AddWithValue("@cost_per_kwh", DBHelper.DBNullIfEmpty(j["cost_per_kwh"]));
                        cmd.Parameters.AddWithValue("@cost_per_session", DBHelper.DBNullIfEmpty(j["cost_per_session"]));
                        cmd.Parameters.AddWithValue("@cost_per_minute", DBHelper.DBNullIfEmpty(j["cost_per_minute"]));
                        cmd.Parameters.AddWithValue("@cost_idle_fee_total", DBHelper.DBNullIfEmpty(j["cost_idle_fee_total"]));
                        cmd.Parameters.AddWithValue("@cost_kwh_meter_invoice", DBHelper.DBNullIfEmpty(j["cost_kwh_meter_invoice"]));

                        cmd.Parameters.AddWithValue("@id", j["id"]);
                        int done = cmd.ExecuteNonQuery();

                        Logfile.Log("SetCost OK: " + done);
                        WriteString(response, "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
                WriteString(response, "ERROR");
            }
        }

        private void Getchargingstate(HttpListenerRequest request, HttpListenerResponse response)
        {
            string id = request.QueryString["id"];
            string responseString = "";

            try
            {
                Logfile.Log("HTTP getchargingstate");
                using (DataTable dt = new DataTable())
                {
                    using (MySqlDataAdapter da = new MySqlDataAdapter("SELECT chargingstate.*, lat, lng, address, charging.charge_energy_added as kWh FROM chargingstate join pos on chargingstate.pos = pos.id join charging on chargingstate.EndChargingID = charging.id where chargingstate.id = @id", DBHelper.DBConnectionstring))
                    {
                        da.SelectCommand.Parameters.AddWithValue("@id", id);
                        da.Fill(dt);

                        responseString = dt.Rows.Count > 0 ? Tools.DataTableToJSONWithJavaScriptSerializer(dt) : "not found!";
                    }
                    dt.Clear();
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            WriteString(response, responseString);
        }

        private void GetAllCars(HttpListenerRequest request, HttpListenerResponse response)
        {
            string responseString = "";

            try
            {
                Car c = Car.allcars.FirstOrDefault(r => r.waitForMFACode);
                if (c != null)
                {
                    responseString = "WAITFORMFA:" + c.CarInDB;
                }
                else
                {
                    using (DataTable dt = new DataTable())
                    {
                        using (MySqlDataAdapter da = new MySqlDataAdapter("SELECT id, display_name, tasker_hash, model_name, vin, tesla_name, tesla_carid, lastscanmytesla, freesuc FROM cars order by display_name", DBHelper.DBConnectionstring))
                        {
                            da.Fill(dt);

                            responseString = dt.Rows.Count > 0 ? Tools.DataTableToJSONWithJavaScriptSerializer(dt) : "not found!";
                        }
                        dt.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            WriteString(response, responseString);
        }

        private static void WriteString(HttpListenerResponse response, string responseString)
        {
            response.ContentEncoding = System.Text.Encoding.UTF8;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
        }

        private void Admin_GetPOI(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.QueryString.Count == 2 && request.QueryString.HasKeys())
            {
                double lat = double.NaN;
                double lng = double.NaN;
                foreach (string key in request.QueryString.AllKeys)
                {
                    if (request.QueryString.GetValues(key).Length == 1)
                    {
                        switch (key)
                        {
                            case "lat":
                                double.TryParse(request.QueryString.GetValues(key)[0], out lat);
                                break;
                            case "lng":
                                double.TryParse(request.QueryString.GetValues(key)[0], out lng);
                                break;
                            default:
                                break;
                        }
                    }
                }
                if (lat != double.NaN && lng != double.NaN)
                {
                    Address addr = Geofence.GetInstance().GetPOI(lat, lng, false);
                    if (addr != null)
                    {
                        Dictionary<string, object> data = new Dictionary<string, object>()
                        {
                            { "name", addr.name },
                            { "rawName", addr.rawName },
                            { "lat", addr.lat },
                            { "lng", addr.lng },
                            { "radius", addr.radius },
                            { "IsHome", addr.IsHome },
                            { "IsWork", addr.IsWork },
                            { "IsCharger", addr.IsCharger },
                            { "NoSleep", addr.NoSleep },
                        };
                        Dictionary<string, object> specialflags = new Dictionary<string, object>();
                        foreach (KeyValuePair<Address.SpecialFlags, string> flag in addr.specialFlags)
                        {
                            specialflags.Add(flag.Key.ToString(), flag.Value);
                        }
                        data.Add("SpecialFlags", specialflags);
                        response.AddHeader("Content-Type", "application/json; charset=utf-8");
                        WriteString(response, new JavaScriptSerializer().Serialize(data));
                        return;
                    }
                }
            }
            // finally close response
            WriteString(response, "");
        }

        private void Admin_UpdateElevation(HttpListenerRequest request, HttpListenerResponse response)
        {
            int from = 1;
            int to = 1;
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand("Select max(id) from pos", con))
                    {
                        MySqlDataReader dr = cmd.ExecuteReader();
                        if (dr.Read() && dr[0] != DBNull.Value)
                        {
                            int.TryParse(dr[0].ToString(), out to);
                        }
                        con.Close();
                    }
                }
            }
            catch (Exception) { }
            Logfile.Log($"Admin: UpdateElevation ({from} -> {to}) ...");
            WriteString(response, $"Admin: UpdateElevation ({from} -> {to}) ...");
            DBHelper.UpdateTripElevation(from, to, "/admin/UpdateElevation");
            Logfile.Log("Admin: UpdateElevation done");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                listener.Close();
            }
            isDisposed = true;
        }
    }
}
