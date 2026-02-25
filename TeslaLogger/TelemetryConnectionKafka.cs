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

        public TelemetryConnectionKafka(Car car) {
            lock (typeof(TelemetryConnectionKafka))
            {
                if (instance == null)
                {
                    instance = this;
                    Thread t = new Thread(() => run());
                    t.Start();
                }

                var parser = new TelemetryParser(car);
                parser.InitFromDB();
                parserDict.TryAdd(car.Vin, parser);
                vins.Add(car.Vin);
            }
        }

        static void run()
        {
            var kc = new KafkaConnector.KafkaConnector(ref queue, ref vins);
            
            System.Diagnostics.Debug.WriteLine("TelemetryConnectionKafka Loop");
            foreach (var msg in queue.GetConsumingEnumerable())
            {
                try
                {
                    parserDict.TryGetValue(msg.vin, out var parser);
                    parser?.handleMessage(msg.msg);
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
