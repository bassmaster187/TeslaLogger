namespace TeslaLogger
{
    using System;

    internal static class MQTTClient
    {
        internal static void StartMQTTClient()
        {
            if (!ApplicationSettings.Default.UseMQTT)
                return;

            try
            {
                System.Threading.Thread MQTTthread = new System.Threading.Thread(StartMqttClient);
                MQTTthread.Start();
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
            }
        }

        private static void StartMqttClient()
        {
            string MQTTClientPath = "/etc/teslalogger/MQTTClient.exe";

            try
            {
                if (!System.IO.File.Exists(MQTTClientPath))
                {
                    Tools.Log("MQTTClient.exe not found!");
                    return;
                }

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.FileName = "mono";
                proc.StartInfo.Arguments = MQTTClientPath;

                proc.Start();

                while (!proc.StandardOutput.EndOfStream)
                {
                    string line = proc.StandardOutput.ReadLine();
                    Tools.Log(line);
                }

                proc.WaitForExit();
            }
            catch (Exception ex)
            {
                Tools.Log(ex.ToString());
            }
            finally
            {
                Tools.Log("MQTT terminated");
            }
        }
    }
}
