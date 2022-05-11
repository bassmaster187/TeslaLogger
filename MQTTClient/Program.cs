using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using TeslaLogger;
using uPLibrary.Networking.M2Mqtt;

namespace MQTTClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string clientid = "6333abad-51f4-430d-9ba5-0047602612d1";
            int MQTTPort = MqttSettings.MQTT_BROKER_DEFAULT_PORT;
            bool subtopics = false;

            MqttClient client = null;
            try
            {
                Logfile.Log("MQTT: MqttClient Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

                if (Properties.Settings.Default.MQTTHost.Length == 0)
                {
                    Logfile.Log("MQTT: No MQTTHost settings -> MQTT disabled!");
                    return;
                }

                if (Properties.Settings.Default.MQTTPort.Length > 0)
                {
                    try
                    {
                        int.TryParse(Properties.Settings.Default.MQTTPort, out MQTTPort);
                        Logfile.Log("MQTT: Using user specific port: " + Properties.Settings.Default.MQTTPort);
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.Message);
                    }
                }

                if (Properties.Settings.Default.ClientID.Length > 0)
                {
                    clientid = Properties.Settings.Default.ClientID;
                    Logfile.Log("MQTT: Using user specific ClientID: " + clientid);
                }

                if (Properties.Settings.Default.Topic.Length == 0)
                {
                    Logfile.Log("MQTT: No Topic settings -> MQTT disabled!");
                    return;
                }

                if (Properties.Settings.Default.Subtopics.Length > 0)
                {
                    try
                    {
                        Boolean.TryParse(Properties.Settings.Default.Subtopics, out subtopics);
                        
                        if(subtopics)
                        {
                            Logfile.Log("MQTT: Subtopics enabled");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log(ex.Message);
                    }
                }

                client = new MqttClient(Properties.Settings.Default.MQTTHost, MQTTPort, false, null, null, MqttSslProtocols.None);

                if (Properties.Settings.Default.Name.Length > 0 && Properties.Settings.Default.Password.Length > 0)
                {
                    Logfile.Log("MQTT: Connecting with credentials: " + Properties.Settings.Default.MQTTHost + ":" + MQTTPort);
                    client.Connect(clientid, Properties.Settings.Default.Name, Properties.Settings.Default.Password);
                }
                else
                {
                    Logfile.Log("MQTT: Connecting without credentials: " + Properties.Settings.Default.MQTTHost + ":" + MQTTPort);
                    client.Connect(clientid);
                }
                if (client.IsConnected)
                {
                    Logfile.Log("MQTT: Connected!");
                }
                else
                {
                    Logfile.Log("MQTT: Connection failed!");
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.Message);
            }

            System.Collections.Generic.HashSet<int> allCars = GetAllcars();
            System.Collections.Generic.Dictionary<int, string> lastjson = new Dictionary<int, string>();


            while (true)
            {
                try
                {
                    System.Threading.Thread.Sleep(1000);

                    if (!client.IsConnected)
                    {
                        Logfile.Log("MQTT: Reconnect");
                        client.Connect(clientid);
                    }

                    foreach (int car in allCars)
                    {
                        string temp = null;
                        using (WebClient wc = new WebClient())
                        {
                            temp = wc.DownloadString("http://localhost:5000/currentjson/" + car);
                        }

                        if (!lastjson.ContainsKey(car) || temp != lastjson[car])
                        {
                            lastjson[car] = temp;
                            string topic = Properties.Settings.Default.Topic;

                            if (allCars.Count > 1)
                                topic += "-" + car;

                            client.Publish(topic, Encoding.UTF8.GetBytes(lastjson[car]),
                                uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                            
                            if(subtopics)
                            { 
                                var topics = JsonConvert.DeserializeObject<Dictionary<string, string>>(temp);
                                foreach(var keyvalue in topics)
                                {
                                    client.Publish(topic + "/" + keyvalue.Key, Encoding.UTF8.GetBytes(keyvalue.Value),
                                    uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                                }
                            }
                        }
                    }
                }
                catch (WebException wex)
                {
                    Logfile.Log(wex.Message);
                    System.Threading.Thread.Sleep(60000);

                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(30000);
                    Logfile.Log(ex.ToString());
                }
            }
        }

        private static HashSet<int> GetAllcars()
        {
            HashSet<int> h = new HashSet<int>();
            string json = "";

            for (int retry=0; retry < 20; retry++) // wait for teslalogger to start
            {
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        json = wc.DownloadString("http://localhost:5000/getallcars");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log("GetAllCars: " + ex.Message);
                    System.Threading.Thread.Sleep(20000);
                }
            }

            try
            {
                dynamic cars = JsonConvert.DeserializeObject(json);
                foreach (dynamic car in cars)
                {
                    int id = car["id"];
                    string vin = car["vin"];
                    string display_name = car["display_name"];

                    if (!String.IsNullOrEmpty(vin))
                    {
                        Logfile.Log("MQTT: Found Car: " + display_name);
                        h.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.Message);
                System.Threading.Thread.Sleep(20000);
            }

            if (h.Count == 0)
                h.Add(1);

            return h;

        }
    }
}
