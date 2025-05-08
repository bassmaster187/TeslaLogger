using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TeslaLogger;
using System.Drawing;
using MySql.Data.MySqlClient;

namespace TeslaLoggerNET8.Lucid
{
    public class LucidWebServer
    {
        internal static void HandleRequest(Uri url, HttpListenerRequest request, HttpListenerResponse response)
        {
            if (url.Segments.Length > 2)
            {
                switch (url.Segments[2])
                {
                    case "getallcars":
                        GetAllCars(url, request, response);
                        break;
                        case "savecar":
                        SaveCar(url, request, response);
                        break;
                }

            }
        }

        private static void SaveCar(Uri url, HttpListenerRequest request, HttpListenerResponse response)
        {
            Logfile.Log("LucidSaveCar");

            string istream = WebServer.GetDataFromRequestInputStream(request);
            dynamic r = JsonConvert.DeserializeObject(istream);

            string name = r["email"];
            string password = r["password"];
            string region = r["region"];
            string VIN = r["vin"];
            string id = r["id"];

            if (id == "-1") // new car
            {
                Logfile.Log("Insert Password");

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

                        Logfile.Log($"New CarID: {newid} SQL Query result: <{queryresult}>");

                        using (var cmd2 = new MySqlCommand("insert cars (id, tesla_name, tesla_password, vin, display_name, car_type, fleetAPIaddress) values (@id, @tesla_name, @tesla_password, @vin, @display_name, 'LUCID', @fleetAPIaddress)", con))
                        {
                            cmd2.Parameters.AddWithValue("@id", newid);
                            cmd2.Parameters.AddWithValue("@tesla_name", name);
                            cmd2.Parameters.AddWithValue("@tesla_password", password);
                            cmd2.Parameters.AddWithValue("@vin", VIN);
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
        }

        private static void GetAllCars(Uri url, HttpListenerRequest request, HttpListenerResponse response)
        {
            Logfile.Log("LucidGetAllCars");

            string istream = WebServer.GetDataFromRequestInputStream(request);
            dynamic r = JsonConvert.DeserializeObject(istream);

            string name = r["email"];
            string password = r["password"];
            string region = r["region"];

            var lastData = LucidWebHelper.PythonLucidAPI(name, password, region, null, out string error);

            if (error?.Contains("StatusCode.UNAUTHENTICATED") == true)
            {
                WebServer.WriteString(response, "Error: StatusCode.UNAUTHENTICATED");
                return;
            }

            string[] lines = lastData.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            ArrayList data = new ArrayList();
            var lc = new LC();
            data.Add(lc);

            foreach (string line in lines)
            {
                try
                {
                    string[] parts = line.Split(new[] { ':' }, 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim();

                        if (key == "vin")
                        {
                            if (lc.VIN != null)
                            {
                                lc = new LC();
                                data.Add(lc);
                            }

                            lc.VIN = value.Replace("\"", "");
                            Logfile.Log("Lucid VIN: " + lc.VIN);
                        }
                        else if (key == "nickname")
                        {
                            lc.Nickname = value.Replace("\"", "");
                        }
                        else if (key == "variant")
                        {
                            string tempVariant = value.Replace("MODEL_VARIANT_", "");
                            lc.Model = tempVariant;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception
                    Logfile.Log(ex.ToString());
                }
            }

            var json = JsonConvert.SerializeObject(data);
            WebServer.WriteString(response, json, "application/json");
        }

        class LC
        {
            public string VIN;
            public string Nickname;
            public string Model;
        }
    }
}
