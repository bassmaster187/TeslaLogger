using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    internal static class Journeys
    {
        public static class EndPoints
        {
            public const string JourneysCreateSelectCar = "/journeys/create/selectCar";
            public const string JourneysCreateStart = "/journeys/create/start";
            public const string JourneysCreateEnd = "/journeys/create/end";
            public const string JourneysCreateCreate = "/journeys/create/create";
            public const string JourneysDelete = "/journeys/delete";
            public const string JourneysDeleteDelete = "/journeys/delete/delete";
            public const string JourneysIndex = "/journeys";
            public const string JourneysList = "/journeys/list";
        }
        //i18n
        internal static string TEXT_LABEL_SELECT_CAR = "Select Car";
        internal static string TEXT_LABEL_SELECT_START = "Select Start";
        internal static string TEXT_LABEL_SELECT_END = "Select Destination";
        internal static string TEXT_LABEL_JOURNEY_NAME = "Name";
        
        internal static string TEXT_BUTTON_NEXT = "Next -->";
        internal static string TEXT_BUTTON_CREATE = "Create Journey";

        internal static string TEXT_TH_DISPLAY_NAME = "Car";
        internal static string TEXT_TH_START_POS = "Start";
        internal static string TEXT_TH_START_DATE = "Start Date";
        internal static string TEXT_TH_END_POS = "Destination";
        internal static string TEXT_TH_END_DATE = "End Date";
        internal static string TEXT_TH_JOURNEY_NAME = "Journey";
        internal static string TEXT_TH_CONSUMPTION = "Consumption";
        internal static string TEXT_TH_CHARGED = "Charged";
        internal static string TEXT_TH_CONSUMPTION_AVG = "Avg. Consumption";
        internal static string TEXT_TH_DURATION_DRIVE = "Driving Duration";
        internal static string TEXT_TH_DURATION_Charge = "Charging Duration";
        internal static string TEXT_TH_DISTANCE = "Distance";
        internal static string TEXT_TH_EXPORT = "Export";

        internal static void CheckSchema()
        {
            try
            {
                if (!DBHelper.TableExists("journeys"))
                {
                    Logfile.Log("CREATE TABLE journeys ...");
                    DBHelper.ExecuteSQLQuery(@"
CREATE TABLE journeys (
    id int NOT NULL AUTO_INCREMENT,
    CarID TINYINT NOT NULL,
    StartPosID INT NOT NULL,
    EndPosID INT NOT NULL,
    consumption_kwh DOUBLE NULL DEFAULT NULL,
    charged_kwh DOUBLE NULL DEFAULT NULL,
    drive_duration_minutes INT NULL DEFAULT NULL,
    charge_duration_minutes INT NULL DEFAULT NULL,
    name VARCHAR(250) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL,
    PRIMARY KEY(id))");
                    Logfile.Log("CREATE TABLE OK");
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        internal static void JourneysCreateSelectCar(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: nothing
            // out: carID
            // action: render car selection HTML
            response.AddHeader("Content-Type", "text/html; charset=utf-8");
            string html1 = "<html><head></head><body>" + PageHeader() + "<table border=\"1\">";
            string html2 = "</table></body></html>";
            StringBuilder sb = new StringBuilder();
            sb.Append($@"<tr><td>{WebUtility.HtmlEncode(TEXT_LABEL_SELECT_CAR)}</td><td><form action=""{EndPoints.JourneysCreateStart}""><select name=""CarID"">");
            using (DataTable dt = DBHelper.GetCars())
            {
                foreach (DataRow r in dt.Rows)
                {
                    int id = id = Convert.ToInt32(r["id"], Tools.ciDeDE);
                    string display_name = r["display_name"] as String ?? "";
                    sb.Append($@"<option value=""{id}"" label=""{WebUtility.HtmlEncode(display_name)}"" />");
                }
                dt.Clear();
            }
            sb.Append($"</select></td><td>");
            sb.Append($@"<button type=""submit"">{WebUtility.HtmlEncode(TEXT_BUTTON_NEXT)}</button></form></td></tr>");
            WriteString(response, html1 + sb.ToString() + html2);
        }

        internal static void JourneysCreateStart(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: CarID
            // out: CarID, StartPosID
            // action: render Start selection HTML
            response.AddHeader("Content-Type", "text/html; charset=utf-8");
            string html1 = "<html><head></head><body>" + PageHeader() + "<table border=\"1\">";
            string html2 = "</table></body></html>";
            StringBuilder sb = new StringBuilder();
            int CarID = Convert.ToInt32(GetUrlParameterValue(request, "CarID"), Tools.ciEnUS);
            Tools.DebugLog($"JourneysCreateStart CarID:{CarID}");
            sb.Append($@"<tr><td>{WebUtility.HtmlEncode(TEXT_LABEL_SELECT_START)}</td><td><form action=""{EndPoints.JourneysCreateEnd}""><input type=""hidden"" name=""CarID"" value=""{CarID}""><select name=""StartPosID"">");
            try {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    StartPosID,
    StartDate, 
    Start_address
FROM
    trip
WHERE
    CarID = @CarID
ORDER BY
    StartDate", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", CarID);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (int.TryParse(dr[0].ToString(), out int id))
                            {
                                sb.Append($@"<option value=""{dr[0]}"" label=""{WebUtility.HtmlEncode(dr[1].ToString())} - {WebUtility.HtmlEncode(dr[2].ToString())}"" />");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            sb.Append($" </select></td><td>");
            sb.Append($@"<button type=""submit"">{WebUtility.HtmlEncode(TEXT_BUTTON_NEXT)}</button></form></td></tr>");
            WriteString(response, html1 + sb.ToString() + html2);
        }

        internal static void JourneysCreateEnd(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: CarID, StartPosID
            // out: CarID, StartPosID, EndPosId
            // action: render End selection HTML
            response.AddHeader("Content-Type", "text/html; charset=utf-8");
            string html1 = "<html><head></head><body>" + PageHeader() + "<table border=\"1\">";
            string html2 = "</table></body></html>";
            StringBuilder sb = new StringBuilder();
            int CarID = Convert.ToInt32(GetUrlParameterValue(request, "CarID"), Tools.ciEnUS);
            int StartPosID = Convert.ToInt32(GetUrlParameterValue(request, "StartPosID"), Tools.ciEnUS);
            Tools.DebugLog($"JourneysCreateEnd CarID:{CarID} StartPosID:{StartPosID}");
            sb.Append($@"<tr><td>{WebUtility.HtmlEncode(TEXT_LABEL_SELECT_END)}</td><td><form action=""{EndPoints.JourneysCreateCreate}""><input type=""hidden"" name=""CarID"" value=""{CarID}""><input type=""hidden"" name=""StartPosID"" value=""{StartPosID}""><select name=""EndPosID"">");
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    EndPosID,
    EndDate, 
    End_address
FROM
    trip
WHERE
    CarID = @CarID
    AND EndPosID > @StartPosID
ORDER BY
    StartDate", con))
                    {
                        cmd.Parameters.AddWithValue("@CarID", CarID);
                        cmd.Parameters.AddWithValue("@StartPosID", StartPosID);
                        Tools.DebugLog(cmd);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (int.TryParse(dr[0].ToString(), out int id))
                            {
                                sb.Append($@"<option value=""{dr[0]}"" label=""{dr[1]} - {dr[2]}"" />");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            sb.Append("</select></td><td>");
            sb.Append($@"{WebUtility.HtmlEncode(TEXT_LABEL_JOURNEY_NAME)}</td><td><input type=""text"" name=""name"" /></td><td>");
            sb.Append($@"<button type=""submit"">{WebUtility.HtmlEncode(TEXT_BUTTON_CREATE)}</button></form></td></tr>");
            WriteString(response, html1 + sb.ToString() + html2);
        }

        internal static void JourneysCreateCreate(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: CarID, StartPosID, EndPosId
            // out: nothing
            // action: create journey table entry, render result selection HTML
            response.AddHeader("Content-Type", "text/html; charset=utf-8");
            string html1 = "<html><head></head><body>" + PageHeader() + "<table border=\"1\">";
            string html2 = "</table></body></html>";
            StringBuilder sb = new StringBuilder();
            int CarID = Convert.ToInt32(GetUrlParameterValue(request, "CarID"), Tools.ciEnUS);
            int StartPosID = Convert.ToInt32(GetUrlParameterValue(request, "StartPosID"), Tools.ciEnUS);
            int EndPosID = Convert.ToInt32(GetUrlParameterValue(request, "EndPosID"), Tools.ciEnUS);
            string name = GetUrlParameterValue(request, "name");
            Tools.DebugLog($"JourneysCreateCreate CarID:{CarID} StartPosID:{StartPosID} EndPosID:{EndPosID} name:{name}");
            DataRow car = DBHelper.GetCar(CarID);
            if (car != null && StartPosID < EndPosID && !string.IsNullOrEmpty(name))
            {
                try
                {
                    using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                    {
                        con.Open();
                        using (MySqlCommand cmd = new MySqlCommand(@"
INSERT journeys (
    CarID,
    StartPosID,
    EndPosID,
    name
)
VALUES (
    @CarID,
    @StartPosID,
    @EndPosID,
    @name
)", con))
                        {
                            cmd.Parameters.AddWithValue("@CarID", CarID);
                            cmd.Parameters.AddWithValue("@StartPosID", StartPosID);
                            cmd.Parameters.AddWithValue("@EndPosID", EndPosID);
                            cmd.Parameters.AddWithValue("@name", name);
                            SQLTracer.TraceNQ(cmd);
                        }
                        using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    Id
FROM
    journeys
WHERE
    CarID = @CarID
    AND StartPosID = @StartPosID
    AND EndPosID = @EndPosID
    AND name
ORDER BY
    Id DESC
LIMIT 1", con))
                        {
                            cmd.Parameters.AddWithValue("@CarID", CarID);
                            cmd.Parameters.AddWithValue("@StartPosID", StartPosID);
                            cmd.Parameters.AddWithValue("@EndPosID", EndPosID);
                            cmd.Parameters.AddWithValue("@name", name);
                            int journeyId = SQLTracer.TraceNQ(cmd);
                            _ = Task.Factory.StartNew(() =>
                            {
                                CalculateConsumption(journeyId);
                                CalculateDriveDuration(journeyId);
                            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                        }
                        sb.Append("OK");
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                }
            }
            WriteString(response, html1 + sb.ToString() + html2);
        }

        private static void CalculateDriveDuration(int journeyId)
        {
            try {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    int drive_duration_minutes = 0;
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    trip.DurationMinutes
FROM
    trip,
    journeys
WHERE
    trip.StartPosID >= (SELECT StartPosID FROM journeys WHERE ID = @journeyID)
    AND trip.EndPosID <= (SELECT EndPosID FROM journeys WHERE ID = @journeyID)
", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyId);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read())
                        {
                            drive_duration_minutes += Convert.ToInt32(dr[0].ToString());
                        }
                    }
                    using (MySqlCommand cmd = new MySqlCommand(@"
UPDATE journeys (
    drive_duration_minutes
)
VALUES (
    @drive_duration_minutes
)
WHERE
    journeys.ID = @journeyID", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyId);
                        cmd.Parameters.AddWithValue("@drive_duration_minutes", drive_duration_minutes);
                        SQLTracer.TraceNQ(cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private static void CalculateConsumption(int journeyId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    double consumption_kWh = 0;
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    trip.consumption_kWh
FROM
    trip,
    journeys
WHERE
    trip.StartPosID >= (SELECT StartPosID FROM journeys WHERE ID = @journeyID)
    AND trip.EndPosID <= (SELECT EndPosID FROM journeys WHERE ID = @journeyID)
", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyId);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read())
                        {
                            consumption_kWh += Convert.ToDouble(dr[0].ToString());
                        }
                    }
                    using (MySqlCommand cmd = new MySqlCommand(@"
UPDATE journeys (
    consumption_kWh
)
VALUES (
    @consumption_kWh
)
WHERE
    journeys.ID = @journeyID", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyId);
                        cmd.Parameters.AddWithValue("@consumption_kWh", consumption_kWh);
                        SQLTracer.TraceNQ(cmd);
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        internal static void JourneysList(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: nothing
            // out: nothing
            // action: render list HTML
            response.AddHeader("Content -Type", "text/html; charset=utf-8");
            string html1 = "<html><head></head><body>" + PageHeader() + "<table border=\"1\">";
            string html2 = "</table></body></html>";
            StringBuilder sb = new StringBuilder();
            sb.Append($@"
<tr>
<th>{WebUtility.HtmlEncode(TEXT_TH_DISPLAY_NAME)}</th>
<th>{WebUtility.HtmlEncode(TEXT_TH_START_POS)}</th>
<th>{WebUtility.HtmlEncode(TEXT_TH_START_DATE)}</th>
<th>{WebUtility.HtmlEncode(TEXT_TH_END_POS)}</th>
<th>{WebUtility.HtmlEncode(TEXT_TH_END_DATE)}</th>
<th>{WebUtility.HtmlEncode(TEXT_TH_JOURNEY_NAME)}</th>
<th>{WebUtility.HtmlEncode(TEXT_TH_CONSUMPTION)}</th>
<th>{WebUtility.HtmlEncode(TEXT_TH_DURATION_DRIVE)}</th>
<th>{WebUtility.HtmlEncode(TEXT_TH_DISTANCE)}</th>
<th>{WebUtility.HtmlEncode(TEXT_TH_EXPORT)}</th>
</tr>"); // TODO conver to miles if miles are configured
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    journeys.Id,
    journeys.CarID,
    cars.display_name,
    journeys.StartPosID,
    tripStart.Start_address,
    tripStart.StartDate,
    journeys.EndPosID,
    tripEnd.End_address,
    tripEnd.EndDate,
    journeys.name,
    journeys.consumption_kwh,
    journeys.duration_minutes,
    tripEnd.EndKm - tripStart.StartKm as distance
FROM
    journeys,
    cars,
    trip tripStart,
    trip tripEnd
WHERE
    journeys.CarID = cars.Id
    AND journeys.StartPosID = tripStart.StartPosID
    AND journeys.EndPosID = tripEnd.EndPosID
ORDER BY
    journeys.Id ASC", con))
                    {
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read())
                        {
                            sb.Append($@"
<tr>
<td>{WebUtility.HtmlEncode(dr[2].ToString())}</td>
<td>{WebUtility.HtmlEncode(dr[4].ToString())}</td>
<td>{WebUtility.HtmlEncode(dr[5].ToString())}</td>
<td>{WebUtility.HtmlEncode(dr[7].ToString())}</td>
<td>{WebUtility.HtmlEncode(dr[8].ToString())}</td>
<td>{WebUtility.HtmlEncode(dr[9].ToString())}</td>
<td>{WebUtility.HtmlEncode(dr[10].ToString())}</td>
<td>{WebUtility.HtmlEncode(dr[11].ToString())}</td>
<td>{WebUtility.HtmlEncode(Convert.ToDouble(dr[12].ToString()).ToString("0.00"))}km</td>
<td><form action=""/export/trip""><input type=""hidden"" name=""carID"" value=""{dr[1]}""><input type=""hidden"" name=""from"" value=""{dr[3]}""><input type=""hidden"" name=""to"" value=""{dr[6]}""><button type=""submit"">GPX</button></form></td>
</tr>"); // TODO converto miles if miles are configured
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
            WriteString(response, html1 + sb.ToString() + html2);
        }

        internal static void JourneysDelete(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: CarID, StartPosID, EndPosId
            // out: CarID, StartPosID, EndPosId
            // action: render really delete HTML
        }

        internal static void JourneysDeleteDelete(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: CarID, StartPosID, EndPosId
            // out: delete journey, render result HTML
        }

        internal static void JourneysIndex(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: nothing
            // out: render index HTML
        }

        private static string PageHeader()
        {
            return $@"
<a href=""{EndPoints.JourneysIndex}"">Index</a>&nbsp;|&nbsp;
<a href=""{EndPoints.JourneysCreateSelectCar}"">Create a new Journey</a>&nbsp;|&nbsp;
<a href=""{EndPoints.JourneysList}"">List and manage Journeys</a>&nbsp;|&nbsp;
<br />";
        }

        private static void WriteString(HttpListenerResponse response, string responseString)
        {
            response.ContentEncoding = Encoding.UTF8;
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
        }

        private static string GetUrlParameterValue(HttpListenerRequest request, string paramName)
        {
            if (request.QueryString.HasKeys())
            {
                foreach (string key in request.QueryString.AllKeys)
                {
                    if (request.QueryString.GetValues(key).Length == 1)
                    {
                        if (key.Equals(paramName))
                        {
                            return request.QueryString.GetValues(key)[0];
                        }
                    }
                }
            }
            return "";
        }
    }
}
