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
using NetMQ;
using NetMQ.Sockets;

namespace TeslaLogger
{
    internal class TelemetryConnectionZMQ : TelemetryConnection
    {
        private Car car;
        Thread t;
        CancellationTokenSource cts = new CancellationTokenSource();
        SubscriberSocket zmq = new SubscriberSocket();
        Random r = new Random();

        bool connect;

        void Log(string message)
        {
            car.Log("*** FT-ZMQ: " + message);
        }


        public TelemetryConnectionZMQ(Car car)
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

            }
            catch (Exception ex)
            {
                car.Log("Telemetry CloseConnection " + ex.Message);
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

                    if (zmq == null)
                        continue;


                    while (true)
                    {
                        Thread.Sleep(100);
                        NetMQMessage message = zmq.ReceiveMultipartMessage();
                        parser.handleMessage(message[1].ConvertToString());
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


        private void ConnectToServer()
        {
            Log("Connect to Telemetry Server (ZQM)");

            if (zmq != null)
                zmq.Dispose();

            zmq = null;

            try
            {
                var zmqsubscriber = new SubscriberSocket();
                zmqsubscriber.Connect(ApplicationSettings.Default.TelemetryServerURL);
                zmqsubscriber.Subscribe("");

                zmq = zmqsubscriber;
            }
            catch (Exception ex)
            {
                if (ex is AggregateException ex2)
                {
                    Log("Connect to Telemetry Server (ZQM) Error: " + ex2.InnerException.Message);
                    if (ex.InnerException != null)
                        car.CreateExceptionlessClient(ex2.InnerException).Submit();
                    else
                        car.CreateExceptionlessClient(ex2).Submit();

                    Thread.Sleep(60000);
                }
                else
                {
                    Log("Connect to Telemetry Server (ZQM) Error: " + ex.Message);
                    car.CreateExceptionlessClient(ex).Submit();
                    Thread.Sleep(60000);
                }
            }
        }
    }
}