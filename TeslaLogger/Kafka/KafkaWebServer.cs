using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TeslaLogger;

namespace TeslaLoggerNET8.Kafka
{
    internal class KafkaWebServer
    {
        internal static void HandleRequest(Uri url, HttpListenerRequest request, HttpListenerResponse response)
        {
            if (url.Segments.Length > 2)
            {
                switch (url.Segments[2])
                {
                    case "insertvin/":
                        InsertVin(url, request, response);
                        break;
                }
            }
        }

        private static void InsertVin(Uri url, HttpListenerRequest request, HttpListenerResponse response)
        {
            string name = "";
            string password = "";
            string region = "";
            string VIN = url.Segments[3].Trim();

            Logfile.Log("Insert VIN");
            if (VinExists(VIN))
            {
                Logfile.Log($"VIN {VIN} already exists. Skipping insert.");
                WebServer.WriteString(response, "Error: VIN already exists");
                return;
            }

            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();

                using (MySqlCommand cmd = new MySqlCommand(@"
SELECT
    MAX(a) + 1
FROM
    (
    SELECT
        MAX(id) AS a
    FROM
        cars
    UNION ALL
	SELECT
    	MAX(carid) AS a
	FROM
  	  pos
) AS t", con)) // 
                {
                    //decimal newid = SQLTracer.TraceSc(cmd) as decimal? ?? 1;
                    int newid = 1;
                    object queryresult = SQLTracer.TraceSc(cmd);
                    if (queryresult != null && !int.TryParse(queryresult.ToString(), out newid))
                    {
                        // assign default id 1 if parsing the queryresult fails
                        newid = 1;
                    }

                    Logfile.Log($"New CarID: {newid} VIN: <{VIN}>");

                    using (var cmd2 = new MySqlCommand("insert cars (id, tesla_name, tesla_password, vin, display_name, car_type, fleetAPIaddress, tesla_token, fleetAPI, virtualkey) " +
                        "values (@id, @tesla_name, @tesla_password, @vin, @display_name, @car_type, @fleetAPIaddress, 'xxx', 1, 2)", con))
                    {
                        cmd2.Parameters.AddWithValue("@id", newid);
                        cmd2.Parameters.AddWithValue("@tesla_name", name);
                        cmd2.Parameters.AddWithValue("@tesla_password", password);
                        cmd2.Parameters.AddWithValue("@vin", VIN);
                        cmd2.Parameters.AddWithValue("@car_type", "");
                        cmd2.Parameters.AddWithValue("@display_name", "Car " + newid);
                        cmd2.Parameters.AddWithValue("@fleetAPIaddress", region);
                        _ = SQLTracer.TraceNQ(cmd2, out _);

                        var dt = DBHelper.GetCarDT(Convert.ToInt32(newid));
                        if (dt?.Rows?.Count > 0)
                            Program.StartCarThread(dt.Rows[0]);

                        WebServer.WriteString(response, "ID:" + newid);
                    }
                }

            }
        }

        private static bool VinExists(string VIN)
        {
            MySqlConnection con = new(DBHelper.DBConnectionstring);
            con.Open();
            MySqlCommand cmd2 = new("select count(*) from cars where vin=@vin", con);
            cmd2.Parameters.AddWithValue("@vin", VIN);
            int cnt = Convert.ToInt32(cmd2.ExecuteScalar());
            
            return cnt > 0;
        }
    }
}
