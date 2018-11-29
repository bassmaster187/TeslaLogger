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
                Tools.Log("DBConnectionstring: " + ApplicationSettings.Default.DBConnectionstring);

                WebHelper wh = new WebHelper();
                wh.Tesla_token = wh.GetTokenAsync().Result;

                if (wh.Tesla_token == "NULL")
                    return;

                Tools.Log("TOKEN: " + wh.Tesla_token);

                if (ApplicationSettings.Default.DBConnectionstring.Length == 0)
                    return;

                wh.GetVehicles();

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
                                    }
                                    else
                                    {
                                        // wenn er 15 min online war und nicht geladen oder gefahren ist, dann muss man ihn die möglichkeit geben offline zu gehen
                                        TimeSpan ts = DateTime.Now - lastCarUsed;
                                        if (ts.TotalMinutes > 15)
                                        {
                                            currentState = TeslaState.Start;

                                            wh.IsDriving(true); // kurz bevor er schlafen geht, eine Positionsmeldung speichern 

                                            for (int x = 0; x < 21; x++)
                                            {
                                                Tools.Log("Waiting for car to go to sleep " + x.ToString());
                                                System.Threading.Thread.Sleep(1000 * 60);
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
                                        System.Threading.Thread.Sleep(60000);

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
                                        Tools.Log(res);
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

    }
}
