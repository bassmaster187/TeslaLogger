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

namespace TeslaLogger
{
    internal class TelemetryConnection
    {
        private Car car;
        Thread t;
        CancellationToken ct = new CancellationToken();
        ClientWebSocket ws = null;
        byte[] buffer = new byte[1024*10];
        Random r = new Random();

        String lastCruiseState = "";

        public TelemetryConnection(Car car)
        {
            this.car = car;

            t = new Thread(() => { Run(); });
            t.Start();
        }

        private void Run()
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // xx Thread.Sleep(r.Next(1000, 10000)); // if you have many cars, don't connect all at the same time 

                    ConnectToServer();

                    if (ws == null)
                        continue;

                    Login();

                    while (ws.State == WebSocketState.Open)
                    {
                        Thread.Sleep(100);
                        ReadMessages(ws);
                    }
                }
                catch (Exception ex)
                {
                    var s = r.Next(30000, 60000);
                    Thread.Sleep(s);
                }
            }
        }

        private void ReadMessages(ClientWebSocket ws)
        {
            try
            {
                Array.Clear(buffer, 0, buffer.Length);
                Task<WebSocketReceiveResult> response = ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                response.Wait();

                var resultContent = Encoding.UTF8.GetString(buffer);

                if (resultContent.Contains("CruiseState"))
                    System.Diagnostics.Debug.WriteLine("xxx");

                dynamic j = JsonConvert.DeserializeObject(resultContent);

                if (j.ContainsKey("data"))
                {
                    dynamic jData = j["data"];
                    string vin = j["vin"];
                    DateTime d = j["createdAt"];

                    if (car.Vin.Equals(vin, StringComparison.OrdinalIgnoreCase))
                    {
                        car.Log("Telemetry Server Data: " + j.ToString());
                        InsertBatteryTable(jData, d, resultContent);
                        InsertCruiseStateTable(jData, d, resultContent);
                    }
                }
                else
                {
                    car.Log("Unhandled: " + resultContent);

                }

            }
            catch (Exception ex)
            {
                car.Log(ex.ToString());
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
            var s = r.Next(10000, 30000);
            Thread.Sleep(s);
            car.Log("Connect to Telemetry Server");

            if (ws != null)
                ws.Dispose();
            ws = null;
            
            try
            {
                var cws = new ClientWebSocket();
                Task tc = cws.ConnectAsync(new Uri(ApplicationSettings.Default.TelemetryServerURL), ct);
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
            return ws.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
        }
    }
}