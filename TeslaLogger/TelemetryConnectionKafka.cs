using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeslaLogger;

namespace TeslaLoggerNET8
{
    internal class TelemetryConnectionKafka : TelemetryConnection
    {
        static TelemetryConnectionKafka instance = null;
        static System.Collections.Concurrent.BlockingCollection<(string vin, string msg)> queue = new ();
        static System.Collections.Concurrent.ConcurrentDictionary<string, TelemetryParser> parserDict = new ();
        static HashSet<string> vins = new ();
        static CancellationTokenSource cts = new CancellationTokenSource();
        static Task tQueue;

        public TelemetryConnectionKafka(Car car) {
            lock (typeof(TelemetryConnectionKafka))
            {
                try
                {
                    if (instance == null)
                    {
                        instance = this;
                        Task.Run(() => runAsync(cts.Token));
                    }

                    var parser = new TelemetryParser(car);
                    parser.InitFromDB();
                    parserDict.TryAdd(car.Vin, parser);
                    vins.Add(car.Vin);
                }
                catch (Exception ex)
                {
                    Logfile.Log("Error initializing TelemetryConnectionKafka: " + ex.ToString());
                }
            }
        }

        static async Task runAsync(CancellationToken token)    
        {
            var kc = new KafkaConnector.KafkaConnector(ref queue, ref vins);
            
            System.Diagnostics.Debug.WriteLine("TelemetryConnectionKafka Loop");
            foreach (var msg in queue.GetConsumingEnumerable(token))
            {
                try
                {
                    parserDict.TryGetValue(msg.vin, out var parser);
                    if (parser != null)
                    {
                        await parser.handleMessageAsync(msg.msg);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }

            System.Diagnostics.Debug.WriteLine("TelemetryConnectionKafka End");
        }

        public override void CloseConnection()
        {
        }
    }
}
