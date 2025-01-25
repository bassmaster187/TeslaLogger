using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Reflection;
using System.IO;
using Exceptionless;
using Newtonsoft.Json.Linq;
using System.Globalization;
using Google.Protobuf.WellKnownTypes;

namespace TeslaLogger
{
    class TelemetryConnectionWS : TelemetryConnection
    {
        private Car car;
        Thread t;
        CancellationTokenSource cts = new CancellationTokenSource();
        ClientWebSocket ws = null;
        Random r = new Random();

        bool connect;

        void Log(string message)
        {
            car.Log("*** FT: " +  message);
        }
            

        public TelemetryConnectionWS(Car car)
        {
            this.car = car;
            if (car == null)
                return;

            parser = new TelemetryParser(car);
            parser.InitFromDB();

            t = new Thread(() => { Run(); });
            t.Start();
         }

        public override void CloseConnection()
        {
            try
            {
                if (car.FleetAPI)
                    return;

                Log("Telemetry Server close connection!");
                connect = false;
                cts.Cancel();

            } catch (Exception ex)
            {
                car.Log("Telemetry CloseConnection " +  ex.Message);
            }
        }

        public override void StartConnection()
        {
            try
            {
                if (connect)
                    return;

                Log("Telemetry Server start connection");
                cts = new CancellationTokenSource();
                connect = true;
            }
            catch (Exception ex)
            {
                Log("Telemetry StartConnection " + ex.Message);
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
                    else if (ex.InnerException?.InnerException is System.Net.Sockets.SocketException se)
                    {
                        Log(se.Message);
                        car.CreateExceptionlessClient(ex.InnerException).Submit();
                    }
                    else if (ex.InnerException?.InnerException != null)
                    {
                        Log(ex.InnerException.Message);
                        car.CreateExceptionlessClient(ex.InnerException).Submit();
                    }
                    else
                    {
                        Log("Telemetry Exception: " + ex.ToString());
                        car.CreateExceptionlessClient(ex).Submit();
                    }

                    var s = r.Next(30000, 60000);
                    Thread.Sleep(s);
                }
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

            parser.handleMessage(data);            
        }

        private void ConnectToServer()
        {
            Log("Connect to Telemetry Server (WS)");

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
                    Log("Connect to Telemetry Server (WS) Error: " + ex2.InnerException.Message);
                    if (ex.InnerException != null)
                        car.CreateExceptionlessClient(ex2.InnerException).Submit();
                    else
                        car.CreateExceptionlessClient(ex2).Submit();

                    Thread.Sleep(60000);
                }
                else
                {
                    Log("Connect to Telemetry Server (WS) Error: " + ex.Message);
                    car.CreateExceptionlessClient(ex).Submit();
                    Thread.Sleep(60000);
                }
            }
        }

        private void Login()
        {
            
            string configname = "";
            if (car.FleetAPI)
                configname = "paid";

            Log("Login to Telemetry Server");
            string vin = car.Vin;
            // vin = "LRW3E7EK6NC483045"; // xxx
            string fw = car.CurrentJSON.current_car_version;
            if (fw?.Contains(" ") == true)
                fw = fw.Substring(0, fw.IndexOf(" ")).Trim();

            Dictionary<string, object> login = new Dictionary<string, object>{
                    { "msg_type", "login"},
                    { "vin", vin},
                    { "token", car.TaskerHash},
                    { "FW", fw},
                    { "accesstoken", car.Tesla_Token},
                    { "regionurl", car.webhelper.apiaddress},
                    { "config", configname},
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