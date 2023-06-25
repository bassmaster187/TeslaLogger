using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Exceptionless;
using Newtonsoft.Json;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    internal class MQTT
    {
        private static MQTT _Mqtt;

        private string clientid = "6333abad-51f4-430d-9ba5-0047602612d1";
        private string host = "localhost";
        private int port = 1883;
        private string topic = "Tesla";
        private bool subtopics = false;
        private string user = null;
        private string password = null;

        MqttClient client = null;

        System.Collections.Generic.HashSet<int> allCars;
        System.Collections.Generic.Dictionary<int, string> lastjson = new Dictionary<int, string>();

        private MQTT()
        {
            Logfile.Log("MQTT: initialized");
        }

        public static MQTT GetSingleton()
        {
            if (_Mqtt == null)
            {
                _Mqtt = new MQTT();
            }
            return _Mqtt;
        }
        internal void RunMqtt()
        {
            // initially sleep 30 seconds to let the cars get from Start to Online
            Thread.Sleep(30000);

            try
            {
                allCars = GetAllcars();
                if (KVS.Get("MQTTSettings", out string mqttSettingsJson) == KVS.SUCCESS)
                {
                    dynamic r = JsonConvert.DeserializeObject(mqttSettingsJson);
                    if (r["mqtt_host"] > 0)
                    {
                        host = r["mqtt_host"];
                    }
                    if (r["mqtt_port"] > 0)
                    {
                        port = (int)r["mqtt_port"];
                    }
                    if (r["mqtt_user"] > 0 && r["mqtt_passwd"] > 0)
                    {
                        user = r["mqtt_user"];
                        password = r["mqtt_passwd"];
                    }
                    if (r["mqtt_topic"] > 0)
                    {
                        topic = r["mqtt_topic"];
                    }
                    if (r["mqtt_clientid"] > 0)
                    {
                        clientid = r["mqtt_clientid"];
                    }
                    if (r["mqtt_subtopics"] > 0)
                    {
                        subtopics = (bool)r["mqtt_subtopics"];
                    }
                }

                client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);

                if (user != null && password != null)
                {
                    Logfile.Log("MQTT: Connecting with credentials: " + host + ":" + port);
                    client.Connect(clientid, user, password);
                }
                else
                {
                    Logfile.Log("MQTT: Connecting without credentials: " + host + ":" + port);
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

                foreach (int car in allCars)
                {
                    client.Subscribe(new[] {
                        $"{topic}/car/{car}/command/+"
                    },
                        new[] {
                            MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE
                        });
                }

                while (true)
                {
                    Work();                    
                    // sleep 1 second
                    Thread.Sleep(1000);
                }

            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Tools.DebugLog("MQTT: Exception", ex);
            }
        }

        internal void Work()
        {
           try
            {
                if (!client.IsConnected)
                {
                    if (user != null && password != null)
                    {
                        Logfile.Log("MQTT: Connecting with credentials: " + host + ":" + port);
                        client.Connect(clientid, user, password);
                    }
                    else
                    {
                        Logfile.Log("MQTT: Connecting without credentials: " + host + ":" + port);
                        client.Connect(clientid);
                    }
                }

                foreach (int car in allCars)
                {
                    string temp = null;
                    string carTopic = $"{topic}/car/{car}";
                    using (WebClient wc = new WebClient())
                    {
                        temp = wc.DownloadString("http://localhost:5000/currentjson/" + car);
                    }

                    if (!lastjson.ContainsKey(car) || temp != lastjson[car])
                    {
                        lastjson[car] = temp;      

                        client.Publish(carTopic + "/currentjson", Encoding.UTF8.GetBytes(lastjson[car]),
                            uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                            
                        if(subtopics)
                        { 
                            var topics = JsonConvert.DeserializeObject<Dictionary<string, string>>(temp);
                            foreach(var keyvalue in topics)
                            {
                                client.Publish(carTopic + "/" + keyvalue.Key, Encoding.UTF8.GetBytes(keyvalue.Value ?? "NULL"),
                                uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logfile.Log("MQTT: Exeption: " + ex.Message);
                ex.ToExceptionless().FirstCarUserID().Submit();
                System.Threading.Thread.Sleep(60000);

            }
        }

        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                var msg = Encoding.ASCII.GetString(e.Message);

                //Example: "teslalogger/car/1/command/set_charge_limit", raw value "13"

                Match m = Regex.Match(e.Topic, $@"{topic}/car/([0-9]+)/(.+)/(.+)");
                if (m.Success && m.Groups.Count == 5 && m.Groups[2].Captures.Count == 1 && m.Groups[4].Captures.Count == 1)
                {
                    if(m.Groups[3].Captures[0].ToString() == "command")
                    {
                        _ = int.TryParse(m.Groups[2].Captures[0].ToString(), out int CarID);
                        string command = m.Groups[4].Captures[0].ToString();
                        try
                        {
                            using (WebClient wc = new WebClient())
                            {
                                string json = wc.DownloadString($"http://localhost:5000/command/{CarID}/{command}?{msg}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logfile.Log("MQTT: Subcribe exeption: " + ex.Message);
                            ex.ToExceptionless().FirstCarUserID().Submit();
                            System.Threading.Thread.Sleep(20000);
                        }
                    }
                    
                }
                
            }
            catch (Exception ex)
            {
                Logfile.Log("MQTT: Exeption: " + ex.ToString());
                ex.ToExceptionless().FirstCarUserID().Submit();
            }
        }

        private static HashSet<int> GetAllcars()
        {
            HashSet<int> h = new HashSet<int>();
            string json = "";

            try
            {
                using (WebClient wc = new WebClient())
                {
                    json = wc.DownloadString("http://localhost:5000/getallcars");
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("GetAllCars: " + ex.Message);
                ex.ToExceptionless().FirstCarUserID().Submit();
                System.Threading.Thread.Sleep(20000);
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
                        Logfile.Log("MQTT: car found: " + display_name);
                        h.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("MQTT: Exception: " + ex.Message);
                ex.ToExceptionless().FirstCarUserID().Submit();
                System.Threading.Thread.Sleep(20000);
            }

            if (h.Count == 0)
                h.Add(1);

            return h;

        }
    }
}
