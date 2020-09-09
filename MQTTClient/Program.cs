using System;
using System.Collections.Generic;
using System.Linq;
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
            string clientid = Guid.NewGuid().ToString();

            MqttClient client = null;
            try
            {
                Logfile.Log("MqttClient Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

                if (Properties.Settings.Default.MQTTHost.Length == 0)
                {
                    Logfile.Log("No MQTTHost settings -> MQTT disabled!");
                    return;
                }

                if (Properties.Settings.Default.Topic.Length == 0)
                {
                    Logfile.Log("No Topic settings -> MQTT disabled!");
                    return;
                }

                client = new MqttClient(Properties.Settings.Default.MQTTHost);

                if (Properties.Settings.Default.Name.Length > 0 && Properties.Settings.Default.Password.Length > 0)
                {
                    Logfile.Log("Connecting with credentials: " + Properties.Settings.Default.MQTTHost);
                    client.Connect(clientid, Properties.Settings.Default.Name, Properties.Settings.Default.Password);
                }
                else
                {
                    Logfile.Log("Connecting without credentials: " + Properties.Settings.Default.MQTTHost);
                    client.Connect(clientid);
                }
                Logfile.Log("Connected!");
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.Message);
            }

            string lastjson = "-";

            while (true)
            {
                try
                {
                    System.Threading.Thread.Sleep(5000);

                    if (!client.IsConnected)
                    {
                        Logfile.Log("Reconnect");
                        client.Connect(clientid);
                    }

                    string temp = System.IO.File.ReadAllText("/etc/teslalogger/current_json_1.txt");
                    if (temp != lastjson)
                    {
                        lastjson = temp;
                        client.Publish(Properties.Settings.Default.Topic, Encoding.UTF8.GetBytes(lastjson), 
                            uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, true);
                    }
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(30000);
                    Logfile.Log(ex.ToString());
                }
            }
        }
    }
}
