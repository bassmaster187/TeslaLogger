using Confluent.Kafka;
using System.Text.RegularExpressions;
using Telemetry.VehicleAlerts;
using Telemetry.VehicleData;
using Telemetry.VehicleError;
using Telemetry.VehicleMetrics;

namespace KafkaConnector
{
    public class KafkaConnector
    {
        readonly CancellationTokenSource ct = new();
        IConsumer<string, byte[]> consumer;
        readonly string bootstrapServers = "";
        string groupID = "";
        static System.Collections.Concurrent.BlockingCollection<(string vin, string msg)> queue;
        static HashSet<string> vins;

        public KafkaConnector(ref System.Collections.Concurrent.BlockingCollection<(string vin, string msg)> queue,
            ref HashSet<string> vins) {
            KafkaConnector.queue = queue;
            KafkaConnector.vins = vins;

            bootstrapServers = "kafka:9092";
            groupID = "teslaloggeronline";
            GetTopics();

            var cc = new ConsumerConfig
            {
                GroupId = groupID,
                //SecurityProtocol = SecurityProtocol.Plaintext,
                //LogConnectionClose = true,
                //LogQueue = true,
                //LogThreadName = true,
                BootstrapServers = this.bootstrapServers
            };

            if (false) // xxx earlierst
            {
                Console.WriteLine("*** AutoOffsetReset.Earliest ***");
                cc.AutoOffsetReset = AutoOffsetReset.Earliest;
            }
            else
                cc.AutoOffsetReset = AutoOffsetReset.Latest;

            Console.WriteLine("Kafka Server: " + this.bootstrapServers);
            consumer = new ConsumerBuilder<string, byte[]>(cc).Build();
            string KafkaSubscribe = "tesla_telemetry_V"; // "tesla_telemetry_V,tesla_telemetry_alerts,tesla_telemetry_errors";
            var subscribe = KafkaSubscribe.Split(",").Select(s => s.Trim()).ToArray();
            Console.WriteLine("Kafka subscribe: " + String.Join(", ", subscribe));
            consumer.Subscribe(subscribe);

            Thread thread = new Thread(() => { Run(); } );
            thread.Start();
        }

        public void Run()
        {
            Thread.Sleep(30000);
            Console.WriteLine("*** Kafka Start consume ***");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var r = consumer.Consume();
                    // Interlocked.Increment(ref Metrics.consume_counter);

                    // Log(r.Message.Key, "Kafka Consume");

                    Headers headers = r.Message.Headers;
                    string Type = "";
                    string Receiver = "";
                    string vin = "";
                    string txtype = "";

                    foreach (var h in headers)
                    {
                        switch (h.Key)
                        {
                            case "Type":
                                Type = System.Text.Encoding.ASCII.GetString(h.GetValueBytes());
                                break;
                            case "Receiver":
                                Receiver = System.Text.Encoding.ASCII.GetString(h.GetValueBytes());
                                break;
                            case "vin":
                                vin = System.Text.Encoding.ASCII.GetString(h.GetValueBytes());

                                break;
                            case "txtype":
                                txtype = System.Text.Encoding.ASCII.GetString(h.GetValueBytes());
                                break;
                            default:
                                // System.Diagnostics.Debug.WriteLine("Message Header unknown: " + h.Key);
                                break;
                        }
                    }
                    if (!vins.Contains(vin))
                        continue;

                    if (txtype == "V")
                    {
                        Payload pl = null;
                        pl = Payload.Parser.ParseFrom(r.Message.Value);
                        string str = pl.ToString();
                        
                        queue.Add((vin, str));
                    }
                }
                catch (OperationCanceledException ex2)
                {
                    Console.WriteLine("EventServerKafka Stop! " + ex2.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        private void GetTopics()
        {
            while (true)
            {
                try
                {
                    Console.WriteLine("Get Topics");
                    var adminConfig = new AdminClientConfig()
                    {
                        BootstrapServers = this.bootstrapServers,
                        SecurityProtocol = SecurityProtocol.Plaintext,
                        LogConnectionClose = true,
                        LogQueue = true,
                        LogThreadName = true
                    };
                    using (var adminClient = new AdminClientBuilder(adminConfig).Build())
                    {
                        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                        var topicsMetadata = metadata.Topics;
                        var topicNames = metadata.Topics.Select(a => a.Topic).ToList();

                        Console.WriteLine("Topics received: " + String.Join(", ", topicNames));

                        SendTestmessage();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error getting topics: " + ex.ToString());
                }
                Thread.Sleep(5000);
            }
        }

        public void SendTestmessage()
        {
            try
            {
                Console.WriteLine("SendTestmessage to Kafka");

                var config = new ProducerConfig
                {
                    BootstrapServers = this.bootstrapServers
                };
                using var producer = new ProducerBuilder<string, string>(config).Build();

                var topic = "test";
                var message = new Message<string, string> { Key = "PING", Value = "Ping from " + groupID + " Time: " + DateTime.Now.ToString() };
                producer.Produce(topic, message, deliveryReport =>
                {
                    Console.WriteLine(" Delivery Report: " + deliveryReport.Error.ToString() + " / " + deliveryReport.Message.Value);
                });
                Console.WriteLine("Produce");
                Thread.Sleep(1000);
                producer.Flush();
                Console.WriteLine("Flush");
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

    }
}
