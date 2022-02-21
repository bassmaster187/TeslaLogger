using System;
using Exceptionless;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Literale nicht als lokalisierte Parameter übergeben", Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    internal static class MQTTClient
    {
        internal static void StartMQTTClient()
        {
            if (!ApplicationSettings.Default.UseMQTT)
            {
                return;
            }

            try
            {
                System.Threading.Thread MQTTthread = new System.Threading.Thread(StartMqttClient);
                MQTTthread.Start();
            }
            catch (Exception ex)
            {
                ex.ToExceptionless().FirstCarUserID().Submit();

                Logfile.Log(ex.ToString());
            }
        }

        private static void StartMqttClient()
        {
            string MQTTClientPath = "/etc/teslalogger/MQTTClient.exe";

            try
            {
                if (!System.IO.File.Exists(MQTTClientPath))
                {
                    Logfile.Log("MQTTClient.exe not found!");
                    return;
                }

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
