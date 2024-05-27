﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt;
using System.Threading;
using System.Net;
using Newtonsoft.Json;

namespace TeslaLogger
{
    internal class SolarChargingOpenWB : SolarChargingBase
    {

        string host = "";
        int port = 1883;
        int LP = 3;
        string ClientId = "Teslalogger-OpenWB";
        static byte[] msg1 = Encoding.ASCII.GetBytes(("1"));
        static byte[] msg0 = Encoding.ASCII.GetBytes(("0"));
        string user = null;
        string passwd = null;
        MqttClient client;

        public SolarChargingOpenWB(Car c) : base(c)
        {
            try
            {
                LogPrefix = "SolarCharging-OpenWB";

                if (KVS.Get("MQTTSettings", out string mqttSettingsJson) == KVS.SUCCESS)
                {
                    dynamic r = JsonConvert.DeserializeObject(mqttSettingsJson);
                    host = r["mqtt_host"];
                    port = (int)r["mqtt_port"];
                    LP = 3;
                    ClientId = r["mqtt_clientid"];
                    user = r["mqtt_user"];
                    passwd = r["mqtt_passwd"];
                }
                else
                {
                    Log("SolarCharging can't start without settings!");
                    return;
                }
                
                if(host != null && port > 0)
                {
                    client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);

                    if (user != null && passwd != null)
                    {
                        client.Connect(ClientId, user, passwd);
                    }
                    else
                    {
                        client.Connect(ClientId);
                    }

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
                else
                {
                    return;
                }
                



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

        internal override void setGrid(int chager_voltage, int charger_current, int charger_phases)
        {
            base.setGrid(chager_voltage, charger_current, charger_phases);

            SendVoltage(chager_voltage, charger_phases);
            SendCurrent(chager_voltage, charger_phases);
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

        void SendVoltage(int Voltage, int Phases)
        {
            try
            {
                byte[] V = Encoding.ASCII.GetBytes(Voltage.ToString());
                if (Phases > 0)
                {
                    client.Publish($"openWB/set/lp/{LP}/VPhase1", V);
                }
                if (Phases > 1)
                {
                    client.Publish($"openWB/set/lp/{LP}/VPhase2", V);
                }
                if (Phases > 2)
                {
                    client.Publish($"openWB/set/lp/{LP}/VPhase3", V);
                }
            }
            catch (Exception ex) { Log(ex.ToString()); }
        }

        void SendCurrent(int Current, int Phases)
        {
            try
            {
                byte[] A = Encoding.ASCII.GetBytes(Current.ToString());
                if (Phases > 0)
                {
                    client.Publish($"openWB/set/lp/{LP}/APhase1", A);
                }
                if (Phases > 1)
                {
                    client.Publish($"openWB/set/lp/{LP}/APhase2", A);
                }
                if (Phases > 2)
                {
                    client.Publish($"openWB/set/lp/{LP}/APhase3", A);
                }
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
