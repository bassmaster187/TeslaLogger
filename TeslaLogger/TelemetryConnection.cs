using Exceptionless.Utility;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X509;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace TeslaLogger
{
    internal class TelemetryConnection
    {
        private Car car;
        Thread t;
        CancellationToken ct = new CancellationToken();
        byte[] buffer = new byte[1024];

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
                car.Log("Connect to telemetry server");

                var ws = new ClientWebSocket();
                Task tc = ws.ConnectAsync(new Uri("ws://localhost:5000/ws"), ct);
                tc.Wait();                

                Dictionary<string, object> login = new Dictionary<string, object>{
                    { "msg_type", "login"},
                    { "vin", car.Vin},
                    { "token", car.TaskerHash}
                };

                var jLogin = JsonConvert.SerializeObject(login);
                SendString(ws, jLogin);

                while (ws.State == WebSocketState.Open)
                {
                    try
                    {
                        Array.Clear(buffer, 0, buffer.Length);
                        Task<WebSocketReceiveResult> response = ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        response.Wait();

                        var resultContent = Encoding.UTF8.GetString(buffer);
                        dynamic j = JsonConvert.DeserializeObject(resultContent);

                        if (j.ContainsKey("data"))
                        {
                            dynamic jData = j["data"];
                            string vin = j["vin"];
                            DateTime d = j["createdAt"];

                            if (car.Vin.Equals(vin,StringComparison.OrdinalIgnoreCase))
                            {
                                car.Log(j.ToString());
                            }

                        }
                    } catch (Exception ex)
                    {
                        car.Log(ex.ToString());
                    }
                }
            }
        }

        public Task SendString(ClientWebSocket ws, String data)
        {
            var encoded = Encoding.UTF8.GetBytes(data);
            var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);
            return ws.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
        }
    }
}