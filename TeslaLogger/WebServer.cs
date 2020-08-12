using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TeslaLogger
{
    public class WebServer
    {
        HttpListener listener = null;

        public WebServer()
        {
            if (!HttpListener.IsSupported)
            {
                Logfile.Log("HttpListener is not Supported!!!");
                return;
            }
            
            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add("http://*:5000/");
                listener.Start();

                Logfile.Log("HttpListener bound to http://*:5000/");
            }
            catch (HttpListenerException hlex)
            {
                listener = null;
                if (((UInt32)hlex.HResult) == 0x80004005)
                {
                    Logfile.Log("HTTPListener access denied. Check https://stackoverflow.com/questions/4019466/httplistener-access-denied");
                }
                else
                    Logfile.Log(hlex.ToString());
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
                    listener = new HttpListener();
                    listener.Prefixes.Add("http://localhost:5000/");
                    listener.Start();

                    Logfile.Log("HTTPListener only bound to Localhost!");
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
                    Logfile.Log(hlex.ToString());
            }
            catch (Exception ex)
            {
                listener = null;
                Logfile.Log(ex.ToString());
            }

            while (true)
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

        void OnContext(object o)
        {
            try
            {
                HttpListenerContext context = o as HttpListenerContext;

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                if (request.Url.LocalPath == @"/getchargingstate")
                    getchargingstate(request, response);
                else if (request.Url.LocalPath == @"/setcost")
                    setcost(request, response);
                else
                {
                    string responseString = "URL Not Found!";
                    WriteString(response, responseString);
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }
        }

        private void setcost(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                Logfile.Log("SetCost");

                string json;

                if (request.QueryString["JSON"] != null)
                    json = request.QueryString["JSON"];
                else
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        json = reader.ReadToEnd();
                    }
                }

                Logfile.Log("JSON: " + json);

                dynamic j = new JavaScriptSerializer().DeserializeObject(json);

                using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                {
                    con.Open();
                    MySqlCommand cmd = new MySqlCommand("update chargingstate set cost_total = @cost_total, cost_currency=@cost_currency, cost_per_kwh=@cost_per_kwh, cost_per_session=@cost_per_session, cost_per_minute=@cost_per_minute, cost_idle_fee_total=@cost_idle_fee_total, cost_kwh_meter_invoice=@cost_kwh_meter_invoice  where id= @id", con);

                    if (DBNullIfEmptyOrZero(j["cost_total"]) is DBNull && IsZero(j["cost_per_session"]))
                        cmd.Parameters.AddWithValue("@cost_total", 0);
                    else
                        cmd.Parameters.AddWithValue("@cost_total", DBNullIfEmptyOrZero(j["cost_total"]));

                    cmd.Parameters.AddWithValue("@cost_currency", DBNullIfEmpty(j["cost_currency"]));
                    cmd.Parameters.AddWithValue("@cost_per_kwh", DBNullIfEmpty(j["cost_per_kwh"]));
                    cmd.Parameters.AddWithValue("@cost_per_session", DBNullIfEmpty(j["cost_per_session"]));
                    cmd.Parameters.AddWithValue("@cost_per_minute", DBNullIfEmpty(j["cost_per_minute"]));
                    cmd.Parameters.AddWithValue("@cost_idle_fee_total", DBNullIfEmpty(j["cost_idle_fee_total"]));
                    cmd.Parameters.AddWithValue("@cost_kwh_meter_invoice", DBNullIfEmpty(j["cost_kwh_meter_invoice"]));

                    cmd.Parameters.AddWithValue("@id", j["id"]);
                    int done = cmd.ExecuteNonQuery();

                    Logfile.Log("SetCost OK: " + done);
                    WriteString(response, "OK");
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
                WriteString(response, "ERROR");
            }
        }

        object DBNullIfEmptyOrZero(string val)
        {
            if (val == null || val == "" || val == "0" || val == "0.00")
                return DBNull.Value;

            return val;
        }

        object DBNullIfEmpty(string val)
        {
            if (val == null || val == "")
                return DBNull.Value;

            return val;
        }

        bool IsZero(string val)
        {
            if (val == null || val == "")
                return false;

            double v;
            if (Double.TryParse(val, out v))
            {
                if (v == 0)
                    return true;
            }

            return false;
        }

        private void getchargingstate(HttpListenerRequest request, HttpListenerResponse response)
        {
            string id = request.QueryString["id"];
            string respone = "";

            try
            {
                Logfile.Log("HTTP getchargingstate");                
                DataTable dt = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter("SELECT chargingstate.*, lat, lng, address, charging.charge_energy_added as kWh FROM chargingstate join pos on chargingstate.pos = pos.id join charging on chargingstate.EndChargingID = charging.id where chargingstate.id = @id", DBHelper.DBConnectionstring);
                da.SelectCommand.Parameters.AddWithValue("@id", id);
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    respone = Tools.DataTableToJSONWithJavaScriptSerializer(dt);
                }
                else
                {
                    respone = "not found!";
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
            }

            WriteString(response, respone);
        }

        private static void WriteString(HttpListenerResponse response, string responseString)
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
        }
    }
}
