using System;
using Exceptionless;
using Newtonsoft.Json;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    internal static class MQTTClient
    {
        internal static void StartMQTTClient()
        {
            if(KVS.Get("MQTTSettings", out string mqttSettingsJson) == KVS.SUCCESS)
            {
                try 
                { 
                    dynamic r = JsonConvert.DeserializeObject(mqttSettingsJson);
                    if ((r["mqtt_host"] > 0))
                    {
                        Logfile.Log("MQTT: Using new MQTT client!");
                        ExceptionlessClient.Default.CreateFeatureUsage("MQTTClient").FirstCarUserID().Submit();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ex.ToExceptionless().FirstCarUserID().Submit();

                    Logfile.Log("MQTT: StartMQTTClient Exeption" + ex.ToString());
                }
            }
            
            if (!ApplicationSettings.Default.UseMQTT)
            {
                Logfile.Log("MQTT: No settings found, disabled!");
                return;
            }

            try
            {
                System.Threading.Thread MQTTthread = new System.Threading.Thread(StartMqttClient);
                MQTTthread.Start();
                Logfile.Log("MQTT: Using old MQTT client, not recomended!");
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();

                Logfile.Log(ex.ToString());
            }
        }

        private static void StartMqttClient()
        {
            if (Tools.IsDockerNET8())
                return;

            string MQTTClientPath = "/etc/teslalogger/MQTTClient.exe";

            try
            {
                if (!System.IO.File.Exists(MQTTClientPath))
                {
                    Logfile.Log("MQTTClient.exe not found!");
                    return;
                }

                ExceptionlessClient.Default.CreateFeatureUsage("MQTTClientOld").FirstCarUserID().Submit();

                using (System.Diagnostics.Process proc = new System.Diagnostics.Process
                {
                    EnableRaisingEvents = false
                })
                {
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.FileName = "mono";
                    proc.StartInfo.Arguments = MQTTClientPath;

                    proc.Start();

                    while (!proc.StandardOutput.EndOfStream)
                    {
                        string line = proc.StandardOutput.ReadLine();
                        Logfile.Log(line);
                    }

                    proc.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();
                Logfile.Log(ex.ToString());
            }
            finally
            {
                Logfile.Log("MQTT terminated");
            }
        }
    }
}
