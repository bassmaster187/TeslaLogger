using Exceptionless.Utility;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Reflection;
using System.IO;

namespace TeslaLogger
{
    internal class TelemetryConnection
    {
        private Car car;
        Thread t;
        CancellationTokenSource cts = new CancellationTokenSource();
        ClientWebSocket ws = null;
        Random r = new Random();

        String lastCruiseState = "";

        bool connect = false;

        public TelemetryConnection(Car car)
        {
            this.car = car;

            t = new Thread(() => { Run(); });
            t.Start();
        }

        public void CloseConnection()
        {
            try
            {
                car.Log("Telemetry Server close connection!");
                connect = false;
                cts.Cancel();

            } catch (Exception ex)
            {
                car.Log("Telemetry CloseConnection " +  ex.Message);
            }
        }

        public void StartConnection()
        {
            try
            {
                if (connect)
                    return;

                car.Log("Telemetry Server start connection");
                cts = new CancellationTokenSource();
                connect = true;
            }
            catch (Exception ex)
            {
                car.Log("Telemetry StartConnection " + ex.Message);
            }
        }

        private void Run()
        {
            while (true)
            {
                try
                {
                    while (!connect)
                        Thread.Sleep(1000);

                    ConnectToServer();

                    if (ws == null)
                        continue;

                    Login();

                    while (ws.State == WebSocketState.Open)
                    {
                        Thread.Sleep(100);
                        ReceiveAsync(ws).Wait();
                    }
                }
                catch (Exception ex)
                {
                    if (!connect && ex.InnerException is TaskCanceledException)
                        System.Diagnostics.Debug.WriteLine("Telemetry Cancel OK");
                    else
                        car.Log("Telemetry Exception: " + ex.ToString());                    

                    var s = r.Next(30000, 60000);
                    Thread.Sleep(s);
                }
            }
        }

        private void handleMessage(string resultContent)
        {
            try
            {
                dynamic j = JsonConvert.DeserializeObject(resultContent);

                if (j.ContainsKey("data"))
                {
                    dynamic jData = j["data"];
                    string vin = j["vin"];
                    DateTime d = j["createdAt"];

                    if (car.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase))
                    {
                        car.Log("Telemetry Server Data");
                        InsertBatteryTable(jData, d, resultContent);
                        InsertCruiseStateTable(jData, d, resultContent);
                    }
                }
                else if (j.ContainsKey("alerts"))
                {
                    dynamic jData = j["alerts"];
                    string vin = j["vin"];
                    DateTime d = j["createdAt"];

                    if (car.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase))
                    {
                        car.Log("Telemetry Server Alerts");

                        foreach (dynamic ji in jData)
                        {
                            InsertAlert(ji, resultContent);
                        }
                    }
                }
                else
                {
                    car.Log("Unhandled: " + resultContent);
                }

            }
            catch (Exception ex)
            {
                car.Log(ex.ToString()+ "\n" + resultContent);
            }
        }

        private async Task ReceiveAsync(WebSocket socket)
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            WebSocketReceiveResult result;

            String data;

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                do
                {
                    result = await socket.ReceiveAsync(buffer, cts.Token);
                    ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                    throw new Exception("CLOSE");

                ms.Seek(0, System.IO.SeekOrigin.Begin);

                data = Encoding.UTF8.GetString(ms.ToArray());
            }

            handleMessage(data);            
        }

        private void InsertAlert(dynamic ji, string resultContent)
        {
            string name = ji["name"];
            DateTime startedAt = ji["startedAt"];
            DateTime? endedAt = null;
            if (ji.ContainsKey("endedAt"))
                endedAt = ji["endedAt"];

            dynamic audiences = ji["audiences"];

            int nameid = GetAlertNameID(name);

            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand(@"insert into alerts (CarID, startedAt, nameID, endedAt) 
                    values (@CarID, @startedAt, @nameID, @endedAt)
                    ON DUPLICATE KEY UPDATE endedAt=@endedAt, id=LAST_INSERT_ID(id)", con))
                {
                    cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                    cmd.Parameters.AddWithValue("@startedAt", startedAt);
                    cmd.Parameters.AddWithValue("@nameID", nameid);

                    if (endedAt != null)
                        cmd.Parameters.AddWithValue("@endedAt", endedAt);
                    else
                        cmd.Parameters.AddWithValue("@endedAt", DBNull.Value);


                    int o = cmd.ExecuteNonQuery();

                    cmd.CommandText = "SELECT LAST_INSERT_ID()";
                    cmd.Parameters.Clear();
                    object id = cmd.ExecuteScalar();

                    DBHelper.ExecuteSQLQuery("delete from alert_audiences where alertsid = " + id.ToString());

                    foreach (dynamic jau in audiences)
                    {
                        string s = jau.ToString();
                        int aid = -1;
                        switch (s)
                        {
                            case "Customer": aid = 1; break;
                            case "Service": aid = 2; break;
                            default:
                                car.CreateExeptionlessLog("Telemetry audience unknown", s, Exceptionless.Logging.LogLevel.Error).Submit();
                                car.Log("Audience unknown: " + s);
                                break;
                        }

                        DBHelper.ExecuteSQLQuery($"insert into alert_audiences (alertsID,audienceID) values ({id}, {aid})");

                    }
                }
            }
        }



            

        int GetAlertNameID(string name)
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT ID FROM alert_names where Name = @name", con))
                {
                    cmd.Parameters.AddWithValue("@name", name);
                    object o = SQLTracer.TraceSc(cmd);

                    if (o != null)
                        return Convert.ToInt32(o);
                    else
                    {
                        cmd.CommandText = "insert into alert_names (name) values (@name)";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "SELECT LAST_INSERT_ID()";
                        cmd.Parameters.Clear();
                        object id = cmd.ExecuteScalar();
                        return Convert.ToInt32(id);

                    }
                }
            }
        }

        private void InsertCruiseStateTable(dynamic j, DateTime d, string resultContent)
        {
            try
            {
                foreach (dynamic jj in j)
                {
                    string key = jj["key"];
                    if (key == "CruiseState")
                    {
                        dynamic value = jj["value"];
                        if (value.ContainsKey("stringValue"))
                        {
                            string v1 = value["stringValue"];

                            if (v1 != lastCruiseState)
                            {
                                lastCruiseState = v1;

                                int? state = null;
                                switch (v1)
                                {
                                    case "Off":
                                        state = 0;
                                        break;
                                    case "On":
                                        state = 1;
                                        break;
                                    case "Standby":
                                        state = -1;
                                        break;
                                    default:
                                        car.Log("Unhandled Cruise State: " + v1);
                                        break;
                                }

                                if (state != null)
                                {
                                    using (var con = new MySqlConnection(DBHelper.DBConnectionstring))
                                    {
                                        con.Open();

                                        var cmd = new MySqlCommand("insert into cruisestate (CarId, date, state) values (@carid, @date, @state)", con);
                                        cmd.Parameters.AddWithValue("@carid", car.CarInDB);
                                        cmd.Parameters.AddWithValue("@date", d);
                                        cmd.Parameters.AddWithValue("@state", state);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { 
                car.Log(ex.ToString());
                car.CreateExceptionlessClient(ex).AddObject(resultContent, "ResultContent").Submit();
            }
        }

        private void InsertBatteryTable(dynamic j, DateTime date, string resultContent)
        {
            try
            {
                var cols = new string[] {"PackVoltage", "PackCurrent", "IsolationResistance", "NumBrickVoltageMax", "BrickVoltageMax",
                "NumBrickVoltageMin", "BrickVoltageMin", "ModuleTempMax", "ModuleTempMin", "LifetimeEnergyUsed", "LifetimeEnergyUsedDrive"};

                using (var cmd = new MySqlCommand())
                {
                    foreach (dynamic jj in j)
                    {
                        string key = jj["key"];
                        if (cols.Any(key.Contains))
                        {
                            string name = jj["key"];
                            dynamic value = jj["value"];
                            if (value.ContainsKey("stringValue"))
                            {
                                string v1 = value["stringValue"];
                                double d = double.Parse(v1, Tools.ciEnUS);
                                cmd.Parameters.AddWithValue("@" + name, d);

                                if (key == "ModuleTempMin")
                                {
                                    System.Diagnostics.Debug.WriteLine("ModuleTempMin: " + d);
                                    car.CurrentJSON.lastScanMyTeslaReceived = DateTime.Now;
                                    car.CurrentJSON.SMTCellTempAvg = d;
                                    car.CurrentJSON.CreateCurrentJSON();
                                }
                            }
                        }
                    }

                    if (cmd.Parameters.Count > 0)
                    {
                        cmd.Parameters.AddWithValue("@CarID", car.CarInDB);
                        cmd.Parameters.AddWithValue("@date", date);

                        var sb = new StringBuilder("insert into battery (");
                        var sbc = new StringBuilder(") values (");
                        var names = cmd.Parameters.Cast<MySqlParameter>()
                            .Select(p => p.ParameterName.Substring(1))
                            .ToArray();
                        sb.Append(string.Join(", ", names));

                        var values = cmd.Parameters.Cast<MySqlParameter>()
                            .Select(p => p.ParameterName)
                            .ToArray();
                        sbc.Append(string.Join(", ", values));
                        sbc.Append(")");

                        sb.Append(sbc);

                        sb.Append("\n ON DUPLICATE KEY UPDATE ");
                        var update = cmd.Parameters.Cast<MySqlParameter>()
                            .Where(w => w.ParameterName != "@CarID" && w.ParameterName != "@date")
                            .Select(p => p.ParameterName.Substring(1) + "=" + p.ParameterName)
                            .ToArray();

                        sb.Append(string.Join(", ", update));
                        cmd.CommandText = sb.ToString();

                        using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
                        {
                            con.Open();
                            cmd.Connection = con;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            } catch (Exception ex)
            {
                car.Log("Telemetry Error: " + ex.ToString());
                car.CreateExceptionlessClient(ex).AddObject(resultContent, "ResultContent").Submit();
            }
        }

        private void ConnectToServer()
        {
            car.Log("Connect to Telemetry Server");

            if (ws != null)
                ws.Dispose();

            ws = null;
            
            try
            {
                var cws = new ClientWebSocket();
                Task tc = cws.ConnectAsync(new Uri(ApplicationSettings.Default.TelemetryServerURL), cts.Token);
                tc.Wait();

                ws = cws;
            }
            catch (Exception ex)
            {
                if (ex is AggregateException ex2)
                {
                    car.Log("Connect to Telemetry Server Error: " + ex2.InnerException.Message);
                    car.CreateExceptionlessClient(ex2).Submit();
                }
                else
                {
                    car.Log("Connect to Telemetry Server Error: " + ex.Message);
                    car.CreateExceptionlessClient(ex).Submit();
                }
            }
        }

        private void Login()
        {
            car.Log("Login to Telemetry Server");

            Dictionary<string, object> login = new Dictionary<string, object>{
                    { "msg_type", "login"},
                    { "vin", car.Vin},
                    { "token", car.TaskerHash},
                    { "accesstoken", car.Tesla_Token},
                    { "regionurl", car.webhelper.apiaddress},
                    { "version", Assembly.GetExecutingAssembly().GetName().Version.ToString()}
                };

            var jLogin = JsonConvert.SerializeObject(login);
            SendString(ws, jLogin);
        }

        public Task SendString(ClientWebSocket ws, String data)
        {
            var encoded = Encoding.UTF8.GetBytes(data);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);
            return ws.SendAsync(buffer, WebSocketMessageType.Text, true, cts.Token);
        }
    }
}