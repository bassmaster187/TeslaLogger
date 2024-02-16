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

        private string clientid;
        private string host = "localhost";
        private int port = 1883;
        private string topic = "teslalogger";
        private bool subtopics;
        private bool publishJson;
        private bool discoveryEnable;
        private string discoverytopic = "homeassistant";
        private string user;
        private string password;
        private static int httpport = 5000;
        private static int heartbeatCounter;
        private static bool connecting;

        MqttClient client;

        System.Collections.Generic.HashSet<string> allCars;
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
                httpport = Tools.GetHttpPort();
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
                    if (r["mqtt_publishjson"] > 0)
                    {
                        publishJson = (bool)r["mqtt_publishjson"];
                    }
                    if (r["mqtt_subtopics"] > 0)
                    {
                        subtopics = (bool)r["mqtt_subtopics"];
                    }
                    if (r["mqtt_discoveryenable"] > 0)
                    {
                        discoveryEnable = (bool)r["mqtt_discoveryenable"];
                    }
                    if (r["mqtt_topic"] > 0)
                    {
                        discoverytopic = r["mqtt_discoverytopic"];
                    }
                    if (r["mqtt_clientid"] > 0)
                    {
                        clientid = r["mqtt_clientid"];
                    }
                    Logfile.Log("MQTT: Settings found");
                }
                else
                {
                    Logfile.Log("MQTT: Settings not found!");
                }

                client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);

                ConnectionCheck();

                if (client.IsConnected)
                {
                    Logfile.Log("MQTT: Connected!");
                    foreach (string vin in allCars)
                    {

                        client.Subscribe(new[] {
                        $"{topic}/command/{vin}/+"
                        },
                            new[] {
                            MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE
                            });
                    }

                    client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
                    new Thread(() => { MQTTConnectionHandler(client); }).Start();

                }
                else
                {
                    Logfile.Log("MQTT: Connection failed!");
                }

                



                if (discoveryEnable)
                {
                    foreach(string vin in allCars)
                    {
                        PublishDiscovery(vin);
                    }
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
                if (ConnectionCheck())
                {
                    //heartbeat
                    if (heartbeatCounter % 10 == 0)
                    {
                        client.Publish($@"{topic}/system/status", Encoding.UTF8.GetBytes("online"),
                                    uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                        Tools.DebugLog("MQTT: hearbeat!");
                        heartbeatCounter = 0;
                    }
                    heartbeatCounter++;


                    foreach (string vin in allCars)
                    {
                        string temp = null;
                        string carTopic = $"{topic}/car/{vin}";
                        string jsonTopic = $"{topic}/json/{vin}/currentjson";

                        int carId = Car.GetCarIDFromVIN(vin);

                        using (WebClient wc = new WebClient())
                        {
                            try
                            {
                                temp = wc.DownloadString($"http://localhost:{httpport}/currentjson/" + carId);
                            }
                            catch (Exception ex)
                            {
                                Logfile.Log("MQTT: CurrentJson Exeption: " + ex.Message);
                                Tools.DebugLog("MQTT: CurrentJson Exception", ex);
                                ex.ToExceptionless().FirstCarUserID().Submit();
                            }
                            
                        }

                        if (!lastjson.ContainsKey(carId) || temp != lastjson[carId])
                        {
                            lastjson[carId] = temp;

                            client.Publish(jsonTopic, Encoding.UTF8.GetBytes(lastjson[carId]),
                                uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);

                            if (subtopics)
                            {
                                var topics = JsonConvert.DeserializeObject<Dictionary<string, string>>(temp);
                                foreach (var keyvalue in topics)
                                {
                                    client.Publish(carTopic + "/" + keyvalue.Key, Encoding.UTF8.GetBytes(keyvalue.Value ?? "NULL"),
                                    uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);

                                }
                                Double.TryParse(topics["latitude"], out double lat);
                                Double.TryParse(topics["longitude"], out double lon);
                                PublichGPSTracker(vin, lat, lon);
                            }

                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logfile.Log("MQTT: Work Exeption: " + ex.Message);
                Tools.DebugLog("MQTT: Work Exception", ex);
                ex.ToExceptionless().FirstCarUserID().Submit();
                System.Threading.Thread.Sleep(60000);

            }
        }


        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                var msg = Encoding.ASCII.GetString(e.Message);

                //Example: "teslalogger/command/LRW123456/set_charging_amps", raw value "13"
                string commandRegex = topic + @"/command/(.{17})/(.+)";
                Logfile.Log("MQTT: Client_MqttMsgPublishReceived");

                Match m = Regex.Match(e.Topic, commandRegex);
                if (m.Success && m.Groups.Count == 3 && m.Groups[1].Captures.Count == 1 && m.Groups[2].Captures.Count == 1)
                {
                    string vin = m.Groups[1].Captures[0].ToString();
                    string command = m.Groups[2].Captures[0].ToString();
                    try
                    {
                        using (WebClient wc = new WebClient())
                        {
                            string json = wc.DownloadString($"http://localhost:{httpport}/command/{vin}/{command}?{msg}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log("MQTT: Subcribe exeption: " + ex.Message);
                        Tools.DebugLog("MQTT: PublishReceived Exception", ex);
                        System.Threading.Thread.Sleep(20000);
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("MQTT: PublishReceived Exeption: " + ex.ToString());
                Tools.DebugLog("MQTT: PublishReceived Exception", ex);
                ex.ToExceptionless().FirstCarUserID().Submit();
            }
        }

        private bool ConnectionCheck()
        {
            try
            {
                if (client != null)
                {
                    if (client.IsConnected)
                    {
                        if (connecting) connecting = false;
                        return true;
                    }
                    else
                    {
                        if (!connecting)
                        {
                            string newClientId;
                            if(clientid != null)
                            {
                                newClientId = clientid;
                            }
                            else
                            {
                                newClientId = Guid.NewGuid().ToString();
                            }
                            if (user != null && password != null)
                            {
                                Logfile.Log("MQTT: Connecting with credentials: " + host + ":" + port);
                                client.Connect(newClientId, user, password, false, 0, true, $@"{topic}/system/status", "offline", true, 30);
                            }
                            else
                            {
                                Logfile.Log("MQTT: Connecting without credentials: " + host + ":" + port);
                                client.Connect(newClientId, null, null, false, 0, true, $@"{topic}/system/status", "offline", true, 30);
                            }
                            connecting = true;
                            Tools.DebugLog("MQTT: not connected, connecting = true");
                            return false;
                            
                        }
                        return false;
                    }
                }
                else 
                {
                    Logfile.Log("MQTT: ConnectionCheck client is null!");
                    return false;
                }
            }
            catch (WebException wex)
            {
                Logfile.Log("MQTT: ConnectionCheck WebExeption: " + wex.Message);
                System.Threading.Thread.Sleep(60000);

            }
            catch (Exception ex)
            {
                System.Threading.Thread.Sleep(30000);
                Logfile.Log("MQTT: ConnectionCheck Exeption: " + ex.ToString());
            }
            return false;
        }

        private void MQTTConnectionHandler(MqttClient client)
        {
            while (true)
            {
                try
                {
                    System.Threading.Thread.Sleep(1000);

                    ConnectionCheck();
                }
                catch (WebException wex)
                {
                    Logfile.Log("MQTT: MQTTConnectionHandler WebExeption: " + wex.Message);
                    System.Threading.Thread.Sleep(60000);

                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(30000);
                    Logfile.Log("MQTT: MQTTConnectionHandler Exeption: " + ex.ToString());
                }
            }
        }

        private static HashSet<string> GetAllcars()
        {
            HashSet<string> h = new HashSet<string>();
            string json = "";

            try
            {
                using (WebClient wc = new WebClient())
                {
                    json = wc.DownloadString($"http://localhost:{httpport}/getallcars");
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("MQTT: GetAllCars: " + ex.Message);
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
                        h.Add(vin);
                    }
                }
            }
            catch (Exception ex)
            {
                Logfile.Log("MQTT: HashSet Exception: " + ex.Message);
                ex.ToExceptionless().FirstCarUserID().Submit();
                System.Threading.Thread.Sleep(20000);
            }

            return h;

        }

        internal void PublishDiscovery(string vin)
        {

            int carId = Car.GetCarIDFromVIN(vin);
            string model = "Model " + vin[3]; //Car.GetCarByID(carId).CarType;
            string name = Car.GetCarByID(carId).DisplayName;
            string sw = Car.GetCarByID(carId).CurrentJSON.current_car_version;
            string type = "sensor";

            foreach (string entity in MQTTAutoDiscovery.autoDiscovery.Keys)
            {
                string entityConfig;
                Dictionary<string, string> entitycontainer = MQTTAutoDiscovery.autoDiscovery[entity];
                entitycontainer.TryGetValue("name", out string entityName);
                entitycontainer.TryGetValue("type", out string entityType);
                if (entityType == "onoff")
                {

                    entitycontainer.TryGetValue("pl_on", out string entityTextOn);
                    entitycontainer.TryGetValue("pl_off", out string entityTextOff);
                    entitycontainer.TryGetValue("cmd_topic", out string entityControlTopic);
                    entitycontainer.TryGetValue("unit", out string entityUnit);
                    entitycontainer.TryGetValue("class", out string entityClass);

                    if (entityControlTopic != null)
                    {
                        entityConfig = JsonConvert.SerializeObject(new
                        {
                            name = entityName,
                            uniq_id = vin + "_" + entity,
                            stat_t = $"{topic}/car/{vin}/{entity}",
                    //        unit_of_meas = entityUnit,
                    //        dev_cla = entityClass,
                            pl_on = entityTextOn,
                            pl_off = entityTextOff,
                            cmd_t = $"{topic}/command/{vin}/{entityControlTopic}",
                            dev = new
                            {
                                ids = vin,
                                mf = "Tesla",
                                mdl = model,
                                name = name,
                                sw = sw
                            }
                        });
                    }
                    else
                    {
                        entityConfig = JsonConvert.SerializeObject(new
                        {
                            name = entityName,
                            uniq_id = vin + "_" + entity,
                            stat_t = $"{topic}/car/{vin}/{entity}",
                    //        unit_of_meas = entityUnit,
                    //        dev_cla = entityClass,
                            pl_on = entityTextOn,
                            pl_off = entityTextOff,
                            dev = new
                            {
                                ids = vin,
                                mf = "Tesla",
                                mdl = model,
                                name = name,
                                sw = sw
                            }
                        });
                    }
                    type = "switch";

                }
                else if (entityType == "number")
                {
                    entitycontainer.TryGetValue("cmd_topic", out string entityControlTopic);
                    entitycontainer.TryGetValue("unit", out string entityUnit);
                    entitycontainer.TryGetValue("class", out string entityClass);
                    entitycontainer.TryGetValue("min", out string entityMin);
                    entitycontainer.TryGetValue("max", out string entityMax);
                    entitycontainer.TryGetValue("step", out string entityStep);
                    if (entityControlTopic != null)
                    {
                        entityConfig = JsonConvert.SerializeObject(new
                        {
                            name = entityName,
                            uniq_id = vin + "_" + entity,
                            stat_t = $"{topic}/car/{vin}/{entity}",
                            unit_of_meas = entityUnit,
                            dev_cla = entityClass,
                            min = entityMin,
                            max = entityMax,
                            step = entityStep,
                            cmd_t = $"{topic}/command/{vin}/{entityControlTopic}",
                            dev = new
                            {
                                ids = vin,
                                mf = "Tesla",
                                mdl = model,
                                name = name,
                                sw = sw
                            }
                        }) ;
                    }
                    else
                    {
                        entityConfig = JsonConvert.SerializeObject(new
                        {
                            name = entityName,
                            uniq_id = vin + "_" + entity,
                            stat_t = $"{topic}/car/{vin}/{entity}",
                            unit_of_meas = entityUnit,
                            dev_cla = entityClass,
                            dev = new
                            {
                                ids = vin,
                                mf = "Tesla",
                                mdl = model,
                                name = name,
                                sw = sw
                            }
                        });
                    }
                    type = "number";
                }
                else if(entityType == "bool")
                {
                    entityConfig = JsonConvert.SerializeObject(new
                    {
                        name = entityName,
                        uniq_id = vin + "_" + entity,
                        stat_t = $"{topic}/car/{vin}/{entity}",
                        dev = new
                        {
                            ids = vin,
                            mf = "Tesla",
                            mdl = model,
                            name = name,
                            sw = sw
                        }
                    });
                    type = "sensor";
                        
                }
                else
                {

                    entitycontainer.TryGetValue("unit", out string entityUnit);
                    entitycontainer.TryGetValue("class", out string entityClass);
                    entityConfig = JsonConvert.SerializeObject(new
                    {
                        name = entityName,
                        uniq_id = vin + "_" + entity,
                        stat_t = $"{topic}/car/{vin}/{entity}",
                        unit_of_meas = entityUnit,
                        dev_cla = entityClass,
                        dev = new
                        {
                            ids = vin,
                            mf = "Tesla",
                            mdl = model,
                            name = name,
                            sw = sw
                        }
                    }); 
                    type = "sensor";
                }

                client.Publish($"{discoverytopic}/{type}/{vin}/{entity}/config", Encoding.UTF8.GetBytes(entityConfig ?? "NULL"),
                                    uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);

            Tools.DebugLog($"MQTT: AutoDiscovery for {vin}: " + entity);
            }


            //speical case: GPS Tracker
            string dicoveryGPSTracker = JsonConvert.SerializeObject(new
            {
                name = name,
                json_attributes_topic = $"{topic}/car/{vin}/gps_tracker",
                state_topic = $"{topic}/car/{vin}/TLGeofenceIsHome",
                payload_home = "true",
                payload_not_home = "fasle"
            }) ;

            client.Publish($"{discoverytopic}/device_tracker/{vin}/config", Encoding.UTF8.GetBytes(dicoveryGPSTracker ?? "NULL"),
                    uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
            
            Tools.DebugLog($"MQTT: AutoDiscovery for {vin}: device_tracker");

        }

        internal void PublichGPSTracker(string vin, double lat, double lon)
        {
            try
            {

                string gpsTrackerTopic = $"{topic}/car/{vin}/gps_tracker";

                string json = JsonConvert.SerializeObject(new { latitude = lat, longitude = lon, gps_accuracy = 1.0 });

                client.Publish(gpsTrackerTopic, Encoding.UTF8.GetBytes(json ?? "NULL"),
                                    uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
            }
            catch (Exception ex)
            {
                Logfile.Log("MQTT: PublichGPSTracker Exeption: " + ex.Message);
                ex.ToExceptionless().FirstCarUserID().Submit();
                System.Threading.Thread.Sleep(60000);

            }

        }

        internal void PublishMqttValue(string vin, String name, object newvalue)
        {
            string carTopic = $"{topic}/car/{vin}";
            string jsonTopic = $"{topic}/json/{vin}";
            try
            {
                if(ConnectionCheck())
                {
                    client.Publish(carTopic + "/" + name, Encoding.UTF8.GetBytes(newvalue.ToString() ?? "NULL"),
                                    uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                }

            }
            catch (Exception ex)
            {
                Logfile.Log("MQTT: PublishMqttValue Exeption: " + ex.Message);
                ex.ToExceptionless().FirstCarUserID().Submit();
                System.Threading.Thread.Sleep(60000);

            }
        }
    }
}
