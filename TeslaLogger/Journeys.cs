using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;
using Exceptionless;
using Newtonsoft.Json;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    internal static class Journeys
    {
        private static Dictionary<string, string> EndPoints = new Dictionary<string, string>()
        {
            { "JourneysCreateSelectCar", "/journeys/create/selectCar" },
            { "JourneysCreateStart", "/journeys/create/start" },
            { "JourneysCreateEnd", "/journeys/create/end" },
            { "JourneysCreateCreate", "/journeys/create/create" },
            { "JourneysDelete", "/journeys/delete" },
            { "JourneysDeleteDelete", "/journeys/delete/delete" },
            { "JourneysIndex", "/journeys" },
            { "JourneysList", "/journeys/list" }
        };
        //i18n
        internal static string TEXT_LABEL_SELECT_CAR = "Select Car";
        internal static string TEXT_LABEL_SELECT_START = "Select Start";
        internal static string TEXT_LABEL_SELECT_END = "Select Destination";
        internal static string TEXT_LABEL_JOURNEY_NAME = "Name";
        internal static string TEXT_LABEL_REALLY_DELETE = "Really delete";

        internal static string TEXT_BUTTON_NEXT = "Next -->";
        internal static string TEXT_BUTTON_DELETE = "Delete -->";
        internal static string TEXT_BUTTON_DELETE_DELETE = "Delete!";
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
        internal static string TEXT_TH_DRIVE_CHARGE = "Driving vs. Charging";
        internal static string TEXT_TH_DURATION_CHARGED = "Charging Duration";
        internal static string TEXT_TH_DISTANCE = "Distance";
        internal static string TEXT_TH_EXPORT = "Export";
        internal static string TEXT_TH_WH_KM = "Wh/km";
        internal static string TEXT_TH_CHARGE_EFF = "Charge efficiency";
        internal static string TEXT_TH_ACTIONS = "Actions";

        private static string html1 = @"<html>
  <head>
    <link href=""https://cdn.jsdelivr.net/npm/select2@4.0.13/dist/css/select2.min.css"" type=""text/css"" rel=""stylesheet"" />
    <script src=""https://ajax.googleapis.com/ajax/libs/jquery/3.5.1/jquery.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/select2@4.0.13/dist/js/select2.min.js""></script>
  </head>
  <body>" + PageHeader() + "<table border=\"1\">";
        private static string html2 = @"
    </table>
    <script>
$(document).ready(function() {
    $('.js-select').select2();
});
    </script>
  </body>
</html>";

        internal static void CheckSchema()
        {
            try
            {
                if (!DBHelper.TableExists("journeys"))
                {
                    Logfile.Log("CREATE TABLE journeys ...");
                    UpdateTeslalogger.AssertAlterDB();
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
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
        }

        internal static void JourneysCreateSelectCar(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: nothing
            // out: carID
            // action: render car selection HTML
            response.AddHeader("Content-Type", "text/html; charset=utf-8");
            StringBuilder sb = new StringBuilder();
            sb.Append($@"<tr><td>{WebUtility.HtmlEncode(TEXT_LABEL_SELECT_CAR)}</td><td><form action=""{EndPoints["JourneysCreateStart"]}""><select class=""js-select"" name=""CarID"" style=""width: 300px"">");
            using (DataTable dt = DBHelper.GetCars())
            {
                foreach (DataRow r in dt.Rows)
                {
                    int id = id = Convert.ToInt32(r["id"], Tools.ciDeDE);
                    string display_name = r["display_name"] as String ?? "";
                    sb.Append($@"<option value=""{id}"" label=""{WebUtility.HtmlEncode(display_name)}"">{WebUtility.HtmlEncode(display_name)}</option>");
                }
                dt.Clear();
            }
            sb.Append($"</select></td><td>");
            sb.Append($@"<button type=""submit"">{WebUtility.HtmlEncode(TEXT_BUTTON_NEXT)}</button></form></td></tr>");
            WriteString(response, html1 + sb.ToString() + html2);
        }

        internal static void JourneysCreateStart(HttpListenerRequest request, HttpListenerResponse response)
        {
            string json = "";
            string data = WebServer.GetDataFromRequestInputStream(request);
            dynamic r = JsonConvert.DeserializeObject(data);

            int CarID = r["carid"];
            Tools.DebugLog($"JourneysCreateStart CarID:{CarID}");

            var o = new List<object>();
            o.Add(new KeyValuePair<string, string>("", "Please Select"));

            try
            {
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
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read() && dr[0] != DBNull.Value)
                        {
                            o.Add(new KeyValuePair<string, string>(dr[0].ToString(), dr[1].ToString() + " - " + dr[2].ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }

            json = JsonConvert.SerializeObject(o);

            WriteString(response, json);
        }

        internal static void JourneysCreateEnd(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: CarID, StartPosID
            // out: CarID, StartPosID, EndPosId
            // action: render End selection HTML

            string json = "";
            string data = WebServer.GetDataFromRequestInputStream(request);
            dynamic r = JsonConvert.DeserializeObject(data);

            int CarID = r["carid"];
            Tools.DebugLog($"JourneysCreateStart CarID:{CarID}");

            var o = new List<object>();
            o.Add(new KeyValuePair<string, string>("", "Please Select"));


            int StartPosID = Convert.ToInt32(r["StartPosID"]);
            Tools.DebugLog($"JourneysCreateEnd CarID:{CarID} StartPosID:{StartPosID}");

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
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read() && dr[0] != DBNull.Value)
                        {
                            if (int.TryParse(dr[0].ToString(), out int id))
                            {
                                o.Add(new KeyValuePair<string, string>(dr[0].ToString(), dr[1].ToString() + " - " + dr[2].ToString()));
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

            json = JsonConvert.SerializeObject(o);
            WriteString(response, json);
        }

        internal static void JourneysCreateCreate(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: CarID, StartPosID, EndPosId
            // out: nothing
            // action: create journey table entry, render result selection HTML
            string data = WebServer.GetDataFromRequestInputStream(request);
            dynamic r = JsonConvert.DeserializeObject(data);

            int CarID = r["CarID"];
            int StartPosID = Convert.ToInt32(r["StartPosID"]);
            int EndPosID = Convert.ToInt32(r["EndPosID"]);
            string name = r["name"];

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
                            _ = SQLTracer.TraceNQ(cmd, out _);
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
ORDER BY
    Id DESC
LIMIT 1", con))
                        {
                            cmd.Parameters.AddWithValue("@CarID", CarID);
                            cmd.Parameters.AddWithValue("@StartPosID", StartPosID);
                            cmd.Parameters.AddWithValue("@EndPosID", EndPosID);
                            cmd.Parameters.AddWithValue("@name", name);
                            int journeyId = (int)SQLTracer.TraceSc(cmd);
                            UpdateJourney(journeyId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless().FirstCarUserID().Submit();
                    Logfile.Log(ex.ToString());
                }
            }
            WriteString(response, "OK");
        }

        private static void UpdateJourney(int journeyId)
        {
            _ = Task.Factory.StartNew(() =>
            {
                CalculateConsumption(journeyId);
                CalculateDriveDuration(journeyId);
                CalculateCharged(journeyId);
                CalculateChargeDuration(journeyId);
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        internal static void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            switch (true)
            {
                case bool _ when request.Url.LocalPath.Equals(EndPoints["JourneysCreateSelectCar"], StringComparison.Ordinal):
                    JourneysCreateSelectCar(request, response);
                    break;
                case bool _ when request.Url.LocalPath.Equals(EndPoints["JourneysCreateStart"], StringComparison.Ordinal):
                    JourneysCreateStart(request, response);
                    break;
                case bool _ when request.Url.LocalPath.Equals(EndPoints["JourneysCreateEnd"], StringComparison.Ordinal):
                    JourneysCreateEnd(request, response);
                    break;
                case bool _ when request.Url.LocalPath.Equals(EndPoints["JourneysCreateCreate"], StringComparison.Ordinal):
                    JourneysCreateCreate(request, response);
                    break;
                case bool _ when request.Url.LocalPath.Equals(EndPoints["JourneysDelete"], StringComparison.Ordinal):
                    JourneysDelete(request, response);
                    break;
                case bool _ when request.Url.LocalPath.Equals(EndPoints["JourneysDeleteDelete"], StringComparison.Ordinal):
                    JourneysDeleteDelete(request, response);
                    break;
                case bool _ when request.Url.LocalPath.Equals(EndPoints["JourneysIndex"], StringComparison.Ordinal):
                    JourneysIndex(request, response);
                    break;
                case bool _ when request.Url.LocalPath.Equals(EndPoints["JourneysList"], StringComparison.Ordinal):
                    JourneysList(request, response);
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    WriteString(response, @"URL Not Found!");
                    break;
            }
        }

        private static void CalculateChargeDuration(int journeyId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    double charge_duration_minutes = 0;
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    chargingstate.EndDate,
    chargingstate.StartDate
FROM
    chargingstate
WHERE
    chargingstate.Pos >= (SELECT StartPosID FROM journeys WHERE ID = @journeyID)
    AND chargingstate.Pos < (SELECT EndPosID FROM journeys WHERE ID = @journeyID)
    AND chargingstate.carID = (SELECT CarID FROM journeys WHERE ID = @journeyID)
", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyId);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read())
                        {
                            DateTime d1 = (DateTime)dr[0];
                            DateTime d2 = (DateTime)dr[1];
                            TimeSpan ts = d1 - d2;
                            charge_duration_minutes += ts.TotalMinutes;
                        }
                        dr.Close();
                    }
                    using (MySqlCommand cmd = new MySqlCommand(@"
UPDATE
    journeys
SET
    charge_duration_minutes = @charge_duration_minutes
WHERE
    Id = @journeyID", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyId);
                        cmd.Parameters.AddWithValue("@charge_duration_minutes", (int)charge_duration_minutes);
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

        private static void CalculateCharged(int journeyId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    double charged_kwh = 0;
                    bool update = false;
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    SUM(chargingstate.charge_energy_added)
FROM
    chargingstate
WHERE
    chargingstate.Pos > (SELECT StartPosID FROM journeys WHERE ID = @journeyID)
    AND chargingstate.Pos < (SELECT EndPosID FROM journeys WHERE ID = @journeyID)
    AND chargingstate.carID = (SELECT CarID FROM journeys WHERE ID = @journeyID)
", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyId);
                        update = double.TryParse(SQLTracer.TraceSc(cmd).ToString(), out charged_kwh);
                    }
                    if (update)
                    {
                        using (MySqlCommand cmd = new MySqlCommand(@"
UPDATE
    journeys
SET
    charged_kwh = @charged_kwh
WHERE
    Id = @journeyID", con))
                        {
                            cmd.Parameters.AddWithValue("@journeyID", journeyId);
                            cmd.Parameters.AddWithValue("@charged_kwh", charged_kwh);
                            _ = SQLTracer.TraceNQ(cmd, out _);
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

        private static void CalculateDriveDuration(int journeyId)
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    int drive_duration_minutes = 0;
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    SUM(trip.DurationMinutes)
FROM
    trip
WHERE
    trip.StartPosID >= (SELECT StartPosID FROM journeys WHERE ID = @journeyID)
    AND trip.EndPosID <= (SELECT EndPosID FROM journeys WHERE ID = @journeyID)
    AND trip.carID = (SELECT CarID FROM journeys WHERE ID = @journeyID)
", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyId);
                        drive_duration_minutes = int.Parse(SQLTracer.TraceSc(cmd).ToString(), Tools.ciEnUS);
                    }
                    using (MySqlCommand cmd = new MySqlCommand(@"
UPDATE
    journeys
SET
    drive_duration_minutes = @drive_duration_minutes
WHERE
    Id = @journeyID", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyId);
                        cmd.Parameters.AddWithValue("@drive_duration_minutes", drive_duration_minutes);
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
    SUM(trip.consumption_kWh)
FROM
    trip
WHERE
    trip.StartPosID >= (SELECT StartPosID FROM journeys WHERE ID = @journeyID)
    AND trip.EndPosID <= (SELECT EndPosID FROM journeys WHERE ID = @journeyID)
    AND trip.carID = (SELECT CarID FROM journeys WHERE ID = @journeyID)
", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyId);
                        consumption_kWh = (double)SQLTracer.TraceSc(cmd);
                    }
                    using (MySqlCommand cmd = new MySqlCommand(@"
UPDATE
    journeys
SET
    consumption_kWh = @consumption_kWh
WHERE
    Id = @journeyID", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyId);
                        cmd.Parameters.AddWithValue("@consumption_kWh", consumption_kWh);
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

        internal static void JourneysList(HttpListenerRequest request, HttpListenerResponse response)
        {
            string data = WebServer.GetDataFromRequestInputStream(request);
            dynamic r = JsonConvert.DeserializeObject(data);

            int carid = r["carid"];

            string sql = $@"
SELECT
    journeys.Id, 
    journeys.name,
    tripStart.Start_address,
    tripStart.StartDate, 
    tripEnd.End_address, 
    tripEnd.EndDate, 
    Round(journeys.consumption_kwh) as consumption_kwh, 
    Round(journeys.charged_kwh,1) as charged_kwh,
    journeys.drive_duration_minutes, 
    journeys.charge_duration_minutes,
    Round(tripEnd.EndKm - tripStart.StartKm,1) as distance, 
    (
	select round(sum(charge_energy_added * co2_g_kWh) / 1000,1) from chargingstate as T1 where T1.CarID = cars.Id and T1.StartDate between tripStart.StartDate and tripEnd.EndDate
    ) as CO2kg,
    (
	select round(sum(cost_total),2) from chargingstate as T1 where T1.CarID = cars.Id and T1.StartDate between tripStart.StartDate and tripEnd.EndDate
    ) as cost_total
    
FROM
    journeys join cars on journeys.CarID = cars.Id
    join trip tripStart on journeys.StartPosID = tripStart.StartPosID
    join trip tripEnd on journeys.EndPosID = tripEnd.EndPosID
WHERE cars.Id = {carid}
ORDER BY
    journeys.Id ASC";
            
            WriteString(response, DBHelper.GetJQueryDataTableJSON(sql));
        }

        internal static void JourneysDelete(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: CarID, StartPosID, EndPosId
            // out: CarID, StartPosID, EndPosId
            // action: render really delete HTML
            response.AddHeader("Content-Type", "text/html; charset=utf-8");
            StringBuilder sb = new StringBuilder();
            int journeyID = Convert.ToInt32(GetUrlParameterValue(request, "id"), Tools.ciEnUS);
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    journeys.name,
    cars.display_name,
    tripStart.Start_address,
    tripEnd.End_address
FROM
    journeys,
    cars,
    trip tripStart,
    trip tripEnd
WHERE
    journeys.CarID = cars.Id
    AND journeys.StartPosID = tripStart.StartPosID
    AND journeys.EndPosID = tripEnd.EndPosID
    AND journeys.ID = @journeyID", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyID);
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        if (dr.Read())
                        {
                            sb.Append($@"
<tr><td>{WebUtility.HtmlEncode(TEXT_LABEL_REALLY_DELETE)}&nbsp;{WebUtility.HtmlEncode(dr[0].ToString())}&nbsp;({WebUtility.HtmlEncode(dr[1].ToString())})&nbsp;-&nbsp;{WebUtility.HtmlEncode(dr[2].ToString())}{WebUtility.HtmlEncode("-->")}{WebUtility.HtmlEncode(dr[3].ToString())}?</td>
<td><form action=""{EndPoints["JourneysDeleteDelete"]}""><input type=""hidden"" name=""id"" value=""{journeyID}""><button type=""submit"">{WebUtility.HtmlEncode(TEXT_BUTTON_DELETE_DELETE)}</button></form></td>
");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
                sb.Append(ex.ToString());
            }
            WriteString(response, html1 + sb.ToString() + html2);
        }

        internal static void JourneysDeleteDelete(HttpListenerRequest request, HttpListenerResponse response)
        {
            string data = WebServer.GetDataFromRequestInputStream(request);
            dynamic r = JsonConvert.DeserializeObject(data);

            int journeyID = r["id"];
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
DELETE
FROM
    journeys
WHERE
    ID = @journeyID
", con))
                    {
                        cmd.Parameters.AddWithValue("@journeyID", journeyID);
                        _ = SQLTracer.TraceNQ(cmd, out _);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            WriteString(response, "OK");
        }

        internal static void JourneysIndex(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: nothing
            // out: render index HTML
            response.AddHeader("Content-Type", "text/html; charset=utf-8");
            StringBuilder sb = new StringBuilder();
            WriteString(response, html1 + sb.ToString() + html2);
        }

        private static string PageHeader()
        {
            return $@"
<a href=""{EndPoints["JourneysIndex"]}"">Index</a>&nbsp;|&nbsp;
<a href=""{EndPoints["JourneysCreateSelectCar"]}"">Create a new Journey</a>&nbsp;|&nbsp;
<a href=""{EndPoints["JourneysList"]}"">List and manage Journeys</a>&nbsp;|&nbsp;
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
                        if (key.Equals(paramName, StringComparison.Ordinal))
                        {
                            return request.QueryString.GetValues(key)[0];
                        }
                    }
                }
            }
            return "";
        }

        internal static bool CanHandleRequest(HttpListenerRequest request)
        {
            return EndPoints.ContainsValue(request.Url.LocalPath);
        }

        internal static void UpdateAllJourneys()
        {
            try
            {
                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    id
FROM
    journeys", con))
                    {
                        MySqlDataReader dr = SQLTracer.TraceDR(cmd);
                        while (dr.Read() && dr[0] != DBNull.Value && int.TryParse(dr[0].ToString(), out int journeyID))
                        {
                            UpdateJourney(journeyID);
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
    }
}
