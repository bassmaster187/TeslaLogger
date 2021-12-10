using System;
using System.Data;
using System.IO;
using System.Net;
using System.Text;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    internal static class Journeys
    {
        private const string baseURL = "/journeys/";
        //i18n
        internal static string TEXT_SELECT_CAR = "Select Car";

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
    duration_minutes INT NULL DEFAULT NULL
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

        }

        internal static void JourneysCreateStart(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: CarID
            // out: CarID, StartPosID
            // action: render Start selection HTML
        }

        internal static void JourneysCreateEnd(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: CarID, StartPosID
            // out: CarID, StartPosID, EndPosId
            // action: render End selection HTML
        }

        internal static void JourneysCreateCreate(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: CarID, StartPosID, EndPosId
            // out: nothing
            // action: create journey table entry, render result selection HTML
        }

        internal static void JourneysList(HttpListenerRequest request, HttpListenerResponse response)
        {
            // in: nothing
            // out: nothing
            // action: render list HTML
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
<a href=""{baseURL}"">Index</a>&nbsp;|&nbsp;
<a href=""{baseURL}create/selectCar"">Create new Journey</a>&nbsp;|&nbsp;
<a href=""{baseURL}list"">List and manage Journeys</a>&nbsp;|&nbsp;
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
    }
}
