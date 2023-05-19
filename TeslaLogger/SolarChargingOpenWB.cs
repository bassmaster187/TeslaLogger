using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt;
using System.Threading;
using System.Net;

namespace TeslaLogger
{
    internal class SolarChargingOpenWB : SolarChargingBase
    {
        string host = "192.168.1.178";
        int port = 1883;
        int LP = 3;
        string ClientId = "Teslalogger-OpenWB";
        static byte[] msg1 = Encoding.ASCII.GetBytes(("1"));
        static byte[] msg0 = Encoding.ASCII.GetBytes(("0"));
        MqttClient client;

        public SolarChargingOpenWB(Car c) : base(c)
        {
            try
            {
                LogPrefix = "SolarCharging-OpenWB";

                client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);

                client.Connect(ClientId);
                client.Subscribe(new[] {
                    $"openWB/lp/{LP}/AConfigured"
                },
                    new[] {
                        MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE
                    });

                if (client.IsConnected)
                {
                    Log("MQTT: Connected!");
                }
                else
                {
                    Log("MQTT: Connection failed!");
                }

                client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
                new Thread(() => { MQTTConnectionHandler(client); }).Start();

            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                var msg = Encoding.ASCII.GetString(e.Message);

                if (e.Topic == $"openWB/lp/{LP}/AConfigured")
                {
                    int amp = int.Parse(msg);
                    if (amp == 0)
                        StopCharging();
                    else if (lastAmpere == 0 && amp > 0)
                    {
                        StartCharging();
                        SetAmpere(amp);
                    }
                    else
                        SetAmpere(amp);
                }
                
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        public override void Charging(bool charging)
        {
            try
            {
                base.Charging(charging);

                string t = $"openWB/set/lp/{LP}/plugStat";

                client.Publish(t, charging ? msg1 : msg0);
                client.Publish($"openWB/set/lp/{LP}/chargeStat", charging ? msg1 : msg0);

                if (!charging)
                {
                    SendWatt(0);
                }
            }
            catch (Exception ex) { Log(ex.ToString()); }
        }

        public override void Plugged(bool plugged)
        {
            try
            {
                base.Plugged(plugged);

                // xxx if (!plugged)
                    client.Publish($"openWB/set/lp/{LP}/chargeStat", plugged ? msg1 : msg0);

                client.Publish($"openWB/set/lp/{LP}/plugStat", plugged ? msg1 : msg0);
            }
            catch (Exception ex) { Log(ex.ToString()); }
        }

        internal override void setPower(int charger_power, string charge_energy_added, string battery_level)
        {
            try
            {
                base.setPower(charger_power, charge_energy_added, battery_level);

                SendWatt(charger_power);

                byte[] kWh = Encoding.ASCII.GetBytes(charge_energy_added);
                client.Publish($"openWB/set/lp/{LP}/kWhCounter", kWh);
            }
            catch (Exception ex) { Log(ex.ToString()); }
        }

        void SendWatt(int Watt)
        {
            try
            {
                byte[] W = Encoding.ASCII.GetBytes(Watt.ToString());
                client.Publish($"openWB/set/lp/{LP}/W", W);
            }
            catch (Exception ex) { Log(ex.ToString()); }
        }

        private void MQTTConnectionHandler(MqttClient client)
        {
            while (true)
            {
                try
                {
                    System.Threading.Thread.Sleep(1000);

                    if (!client.IsConnected)
                    {
                        Log("MQTT: Reconnect");
                        client.Connect(ClientId);
                    }
                }
                catch (WebException wex)
                {
                    Log(wex.Message);
                    System.Threading.Thread.Sleep(60000);

                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(30000);
                    Log(ex.ToString());
                }
            }
        }
    }
}
