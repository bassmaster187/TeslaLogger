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
            Online,
            GoSleep
        }

        static TeslaState currentState = TeslaState.Start;
        WebHelper wh = new WebHelper();
        static DateTime lastCarUsed = DateTime.Now;
        static DateTime lastTryTokenRefresh = DateTime.Now;
        static bool goSleepWithWakeup = false;

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

                if (!wh.RestoreToken())
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


                if (DBHelper.GetMaxPosid(false) == 0)
                {
                    Tools.Log("Insert first Pos");
                    wh.IsDriving(true);
                }

                wh.DeleteWakeupFile();

                Tools.Log("Car: " + wh.carSettings.Name + " - " + wh.carSettings.Wh_TR + " Wh/km");
                double.TryParse(wh.carSettings.Wh_TR, out DBHelper.currentJSON.Wh_TR);
                DBHelper.GetLastTrip();
                UpdateTeslalogger.Start(wh);
                UpdateTeslalogger.UpdateGrafana(wh);

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
                Task.Factory.StartNew(() => wh.UpdateAllPOIAddresses());
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
                                    RefreshToken(wh);

                                    // Alle States werden geschlossen
                                    DBHelper.CloseChargingState();
                                    DBHelper.CloseDriveState();

                                    string res = wh.IsOnline().Result;
                                    lastCarUsed = DateTime.Now;

                                    if (res == "online")
                                    {
                                        Tools.Log(res);
                                        currentState = TeslaState.Online;
                                        wh.IsDriving(true);
                                        DBHelper.StartState(res);
                                        continue;
                                    }
                                    else if (res == "asleep")
                                    {
                                        Tools.Log(res);
                                        currentState = TeslaState.Sleep;
                                        DBHelper.StartState(res);
                                        DBHelper.currentJSON.CreateCurrentJSON();
                                    }
                                    else if (res == "offline")
                                    {
                                        Tools.Log(res);
                                        DBHelper.StartState(res);
                                        DBHelper.currentJSON.CreateCurrentJSON();
                                    }
                                    else
                                    {
                                        DBHelper.currentJSON.current_sleeping = false;
                                        DBHelper.currentJSON.current_online = false;
                                        DBHelper.currentJSON.CreateCurrentJSON();

                                        Tools.Log("Unhandled State: " + res);
                                    }
                                }
                                break;

                            case TeslaState.Online:
                                {
                                    if (wh.IsDriving())
                                    {
                                        lastCarUsed = DateTime.Now;
                                        Tools.Log("Driving");
                                        // TODO: StartDriving
                                        currentState = TeslaState.Drive;
                                        wh.StartStreamThread(); // für altitude
                                        DBHelper.StartDriveState();

                                        Task.Run(() => wh.DeleteWakeupFile());
                                        continue;
                                    }
                                    else if (wh.isCharging())
                                    {
                                        lastCarUsed = DateTime.Now;
                                        Tools.Log("Charging");
                                        wh.IsDriving(true);
                                        DBHelper.StartChargingState();
                                        currentState = TeslaState.Charge;

                                        wh.DeleteWakeupFile();
                                    }
                                    else
                                    {
                                        RefreshToken(wh);

                                        int startSleepHour, startSleepMinute;
                                        Tools.StartSleeping(out startSleepHour, out startSleepMinute);
                                        bool sleep = false;

                                        if (System.IO.File.Exists("cmd_gosleep.txt"))
                                        {
                                            System.IO.File.Delete("cmd_gosleep.txt");

                                            Tools.Log("STOP communication with Tesla Server to enter sleep Mode! (Sleep Button)  https://teslalogger.de/faq-1.php");
                                            currentState = TeslaState.GoSleep;
                                            goSleepWithWakeup = false;
                                        }
                                        else if (DateTime.Now.Hour == startSleepHour && DateTime.Now.Minute == startSleepMinute)
                                        {
                                            Tools.Log("STOP communication with Tesla Server to enter sleep Mode! (Timespan Sleep Mode)  https://teslalogger.de/faq-1.php");
                                            currentState = TeslaState.GoSleep;
                                            goSleepWithWakeup = true;
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
                                                    try
                                                    {
                                                        Tools.Log("STOP communication with Tesla Server to enter sleep Mode! https://teslalogger.de/faq-1.php");

                                                        for (int x = 0; x < ApplicationSettings.Default.SuspendAPIMinutes * 10; x++)
                                                        {
                                                            if (wh.existsWakeupFile)
                                                            {
                                                                Tools.Log("Wakeupfile prevents car to get sleep");
                                                                wh.DeleteWakeupFile();
                                                                string wakeup = wh.Wakeup().Result;
                                                                sleep = false;
                                                                break;
                                                            }

                                                            if (x%10 == 0)
                                                                Tools.Log("Waiting for car to go to sleep " + (x/10).ToString());

                                                            System.Threading.Thread.Sleep(1000 * 6);
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        Tools.Log("Restart communication with Tesla Server!");
                                                    }
                                                }
                                            }
                                        }

                                        if (sleep)
                                            System.Threading.Thread.Sleep(5000);
                                        else
                                            continue;
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
                                        DBHelper.currentJSON.current_trip_end = DateTime.Now;
                                        DBHelper.currentJSON.current_trip_km_end = DBHelper.currentJSON.current_odometer;
                                        DBHelper.currentJSON.current_trip_end_range = DBHelper.currentJSON.current_ideal_battery_range_km;
                                        wh.StopStreaming();
                                    }
                                }
                                break;

                            case TeslaState.GoSleep:
                                {
                                    bool KeepSleeping = true;
                                    int round = 0;

                                    try
                                    {
                                        while (KeepSleeping)
                                        {
                                            round++;
                                            System.Threading.Thread.Sleep(1000);
                                            if (System.IO.File.Exists("wakeupteslalogger.txt"))
                                            {
                                                if (wh.DeleteWakeupFile())
                                                {
                                                    string wakeup = wh.Wakeup().Result;
                                                }

                                                KeepSleeping = false;
                                                currentState = TeslaState.Start;
                                                break;
                                            }
                                            else if (round > 10)
                                            {
                                                round = 0;

                                                if (wh.TaskerWakeupfile())
                                                {
                                                    if (wh.DeleteWakeupFile())
                                                    {
                                                        string wakeup = wh.Wakeup().Result;
                                                    }

                                                    KeepSleeping = false;
                                                    currentState = TeslaState.Start;
                                                    break;
                                                }
                                            }

                                            if (goSleepWithWakeup)
                                            {
                                                int stopSleepingHour, stopSleepingMinute;
                                                Tools.EndSleeping(out stopSleepingHour, out stopSleepingMinute);

                                                if (DateTime.Now.Hour == stopSleepingHour && DateTime.Now.Minute == stopSleepingMinute)
                                                {
                                                    Tools.Log("Stop Sleeping Timespan reached!");

                                                    KeepSleeping = false;
                                                    currentState = TeslaState.Start;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        Tools.Log("Restart communication with Tesla Server!");
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
                Tools.Log(ex.Message);
                Tools.ExceptionWriter(ex, "While Schleife");
            }
            finally
            {
                Tools.Log("Teslalogger Stopped!");
            }
        }

        private static void RefreshToken(WebHelper wh)
        {
            TimeSpan ts = DateTime.Now - wh.lastTokenRefresh;
            if (ts.TotalDays > 9)
            {
                TimeSpan ts2 = DateTime.Now - lastTryTokenRefresh;
                if (ts2.TotalMinutes > 30)
                {
                    lastTryTokenRefresh = DateTime.Now;
                    Tools.Log("try to get new Token");

                    var temp = wh.GetTokenAsync().Result;
                    if (temp != "NULL")
                    {
                        Tools.Log("new Token received!");

                        wh.Tesla_token = temp;
                        wh.lastTokenRefresh = DateTime.Now;
                    }
                    else
                    {
                        Tools.Log("Error getting new Token!");
                    }
                }
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
