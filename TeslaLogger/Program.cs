using System; 
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace TeslaLogger
{
    class Program
    {
        enum TeslaState
        {
            Start,
            Drive,
            Park,
            Charge,
            Sleep,
            WaitForSleep,
            Online
        }

        static TeslaState currentState = TeslaState.Start;
        WebHelper wh = new WebHelper();
        static DateTime lastCarUsed = DateTime.Now;
        static DateTime lastTokenRefresh = DateTime.Now;

        static void Main(string[] args)
        {
            try
            {
                Tools.SetThread_enUS();

                Tools.Log("TeslaLogger Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
                Tools.Log("Current Culture: " + System.Threading.Thread.CurrentThread.CurrentCulture.ToString());
                Tools.Log("Mono Runtime: " + Tools.GetMonoRuntimeVersion());
                
                Tools.Log("DBConnectionstring: " + DBHelper.DBConnectionstring);

                Tools.Log("Car#:" + ApplicationSettings.Default.Car);
                Tools.Log("KeepOnlineMinAfterUsage: " + ApplicationSettings.Default.KeepOnlineMinAfterUsage);
                Tools.Log("SuspendAPIMinutes: " + ApplicationSettings.Default.SuspendAPIMinutes);

                for (int x = 0; x < 30; x++) // try 30 times until DB is up and running
                {
                    try
                    {
                        x++;
                        Tools.Log("DB Version: " + DBHelper.GetVersion());
                        Tools.Log("Count Pos: " + DBHelper.CountPos()); // test the DBConnection
                        break;
                    }
                    catch (Exception ex)
                    {
                        Tools.Log("DBCONNECTION " + ex.Message);
                        System.Threading.Thread.Sleep(10000);
                    }
                }

                WebHelper wh = new WebHelper();
                wh.Tesla_token = wh.GetTokenAsync().Result;

                if (wh.Tesla_token == "NULL")
                    return;

                // don't show full Token in Logfile
                string tempToken = wh.Tesla_token;
                if (tempToken.Length > 5)
                {
                    tempToken = tempToken.Substring(0, tempToken.Length - 5);
                    tempToken += "XXXXX";
                }

                Tools.Log("TOKEN: " + tempToken);

                if (DBHelper.DBConnectionstring.Length == 0)
                    return;

                if (wh.GetVehicles() == "NULL")
                    return;

                String online = wh.IsOnline().Result;
                Tools.Log("Streamingtoken: " + wh.Tesla_Streamingtoken);


                if (DBHelper.GetMaxPosid() == 0)
                {
                    Tools.Log("Insert first Pos");
                    wh.IsDriving(true);
                }

                wh.DeleteWakeupFile();

                UpdateTeslalogger.Start();

                try
                {
                    System.Threading.Thread MQTTthread = new System.Threading.Thread(StartMqttClient);
                    MQTTthread.Start();
                }
                catch (Exception ex)
                {
                    Tools.Log(ex.ToString());
                }

                // Task.Factory.StartNew(() => wh.UpdateAllPosAddresses());
                // Task.Factory.StartNew(() => wh.UpdateAllPOIAddresses());
                // wh.IsDriving();
                // wh.GetCachedRollupData();

                //wh.GetEnergyChartData();

                while (true)
                {
                    try
                    {
                        switch (currentState)
                        {
                            case TeslaState.Start:
                                {
                                    TimeSpan ts = DateTime.Now - lastTokenRefresh;
                                    if (ts.TotalDays > 9)
                                    {
                                        wh.Tesla_token = wh.GetTokenAsync().Result;
                                        lastTokenRefresh = DateTime.Now;

                                        if (wh.Tesla_token == "NULL")
                                            return;
                                    }

                                    // Alle States werden geschlossen
                                    DBHelper.CloseChargingState();
                                    DBHelper.CloseDriveState();

                                    string res = wh.IsOnline().Result;

                                    // xxx test

                                    /*
                                    wh.StartStreamThread();

                                    System.Threading.Thread.Sleep(10000);
                                    wh.IsDriving(true);
                                    System.Threading.Thread.Sleep(5000);
                                    wh.IsDriving(true);
                                    System.Threading.Thread.Sleep(5000);
                                    wh.IsDriving(true);
                                    System.Threading.Thread.Sleep(5000);
                                    wh.IsDriving(true);
                                    System.Threading.Thread.Sleep(5000);
                                    wh.IsDriving(true);
                                    System.Threading.Thread.Sleep(5000);
                                    wh.IsDriving(true);
                                    wh.StopStreaming();
                                    */


                                    lastCarUsed = DateTime.Now;

                                    if (res == "online")
                                    {
                                        Tools.Log(res);
                                        currentState = TeslaState.Online;
                                        wh.IsDriving(true);
                                        DBHelper.StartState(res);
                                        
                                    }
                                    else if (res == "asleep")
                                    {
                                        Tools.Log(res);
                                        currentState = TeslaState.Sleep;
                                        DBHelper.StartState(res);
                                        System.Threading.Thread.Sleep(10000);
                                    }
                                    else if (res == "offline")
                                    {
                                        Tools.Log(res);
                                        DBHelper.StartState(res);
                                        System.Threading.Thread.Sleep(10000);
                                    }
                                    else
                                    {
                                        Tools.Log(res);
                                    }
                                }
                                break;

                            case TeslaState.Online:
                                {
                                    if (wh.isCharging())
                                    {
                                        lastCarUsed = DateTime.Now;
                                        Tools.Log("Charging");
                                        wh.IsDriving(true);
                                        DBHelper.StartChargingState();
                                        currentState = TeslaState.Charge;

                                        wh.DeleteWakeupFile();
                                    }
                                    else if (wh.IsDriving())
                                    {
                                        lastCarUsed = DateTime.Now;
                                        Tools.Log("Driving");
                                        // TODO: StartDriving
                                        wh.IsDriving(true);
                                        currentState = TeslaState.Drive;
                                        wh.StartStreamThread(); // für altitude
                                        DBHelper.StartDriveState();

                                        wh.DeleteWakeupFile();
                                    }
                                    else
                                    {
                                        // wenn er 15 min online war und nicht geladen oder gefahren ist, dann muss man ihn die möglichkeit geben offline zu gehen
                                        TimeSpan ts = DateTime.Now - lastCarUsed;
                                        if (ts.TotalMinutes > ApplicationSettings.Default.KeepOnlineMinAfterUsage)
                                        {
                                            currentState = TeslaState.Start;

                                            wh.IsDriving(true); // kurz bevor er schlafen geht, eine Positionsmeldung speichern und schauen ob standheizung / standklima läuft.
                                            if (wh.is_preconditioning)
                                            {
                                                Tools.Log("preconditioning prevents car to get sleep");
                                                lastCarUsed = DateTime.Now;
                                            }
                                            else
                                            {
                                                for (int x = 0; x < ApplicationSettings.Default.SuspendAPIMinutes; x++)
                                                {
                                                    if (wh.existsWakeupFile)
                                                    {
                                                        Tools.Log("Wakeupfile prevents car to get sleep");
                                                        wh.DeleteWakeupFile();
                                                        break;
                                                    }

                                                    Tools.Log("Waiting for car to go to sleep " + x.ToString());
                                                    System.Threading.Thread.Sleep(1000 * 60);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            case TeslaState.Charge:
                                {
                                    if (!wh.isCharging())
                                    {
                                        // TODO: ende des ladens in die datenbank schreiben
                                        currentState = TeslaState.Start;
                                        wh.IsDriving(true);
                                    }
                                    else
                                    {
                                        lastCarUsed = DateTime.Now;
                                        System.Threading.Thread.Sleep(10000);

                                        //wh.GetCachedRollupData();
                                    }

                                }
                                break;

                            case TeslaState.Sleep:
                                {
                                    string res = wh.IsOnline().Result;

                                    if (res == "online")
                                    {
                                        Tools.Log(res);
                                        currentState = TeslaState.Start;

                                        wh.IsDriving(true); // Positionsmeldung in DB für Wechsel
                                    }
                                    else
                                    {
                                        // Tools.Log(res);
                                        System.Threading.Thread.Sleep(10000); 
                                    }
                                }
                                break;

                            case TeslaState.Drive:
                                {
                                    int t = Environment.TickCount;
                                    if (wh.IsDriving())
                                    {
                                        lastCarUsed = DateTime.Now;

                                        t = 4000 - (Environment.TickCount - t);

                                        if (t > 0)
                                            System.Threading.Thread.Sleep(t); // alle 5 sek eine positionsmeldung
                                    }
                                    else
                                    {
                                        // fahren aufgehört
                                        // TODO: Fahrt beenden
                                        currentState = TeslaState.Start;
                                        wh.StopStreaming();
                                    }
                                }
                                break;

                        }

                    }
                    catch (Exception ex)
                    {
                        Tools.ExceptionWriter(ex, "While Schleife");
                    }

                    System.Threading.Thread.Sleep(1000);
                }
                
            }
            catch (Exception ex)
            {
                Tools.ExceptionWriter(ex, "While Schleife");
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
