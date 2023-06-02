using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ubiety.Dns.Core;

namespace TeslaLogger
{
    internal class SolarChargingBase
    {
        protected Car car;
        protected int lastAmpere = 0;
        protected String LogPrefix = "SolarCharging";
        protected bool lastPlugged = false;
        protected bool lastCharging = false;

        public SolarChargingBase(Car c) 
        { 
            car = c;
        }

        public virtual void Plugged(bool plugged)
        {
            if (plugged != lastPlugged)
            {
                Log("Plugged " + plugged);
                lastPlugged = plugged;
            }
        }

        public virtual void Charging(bool charging)
        {
            if (charging != lastCharging)
            {
                Log("Charging " + charging);
                lastCharging = charging;
            }
        }

        public virtual void SetAmpere(int ampere) {
            try
            {
                if (lastAmpere != ampere && car.webhelper != null)
                {
                    Log("SetAmps: " + ampere);
                    string data = "{\"charging_amps\":" + ampere + "}";
                    string res = car?.webhelper?.PostCommand("command/set_charging_amps", data, true).Result;
                    Log("SetAmps Result: " + res);
                    lastAmpere = ampere;
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        internal virtual void setPower(int charger_power, string charge_energy_added, string battery_level)
        {
            
        }

        internal virtual void setGrid(int chager_voltage, int charger_current, int charger_phases)
        {

        }

        public virtual void StartCharging()
        {
            try
            {
                Log("StartCharging: GetCurrentState " + car.GetCurrentState());
                if (car.GetCurrentState() == Car.TeslaState.Sleep )
                {
                    _ = Task.Factory.StartNew(() =>
                    {
                        Log("StartCharging: wake up!");
                        //string ret = car.webhelper.Wakeup().Result;
                        //Log("StartCharging wake_up result: " + ret);

                        int x = 20;
                        while (car.GetCurrentState() == Car.TeslaState.Sleep)
                        {
                            Thread.Sleep(10000);
                            Log("StartCharging: waiting for car...");
                            x--;
                            if (x <= 0)
                            {
                                Log("StartCharging: time out, car is still sleeping. give up...");
                                return;
                            }
                            
                        }
                        Log("StartCharging: car is online");
                        string retcs = car.webhelper.PostCommand("command/charge_start", null).Result;
                        Log("StartCharging charge_start result: " + retcs);
                        //car.Log(car.webhelper.PostCommand("command/charge_start", null).Result);
                    }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
                    
                }
                else
                {
                    Log("StartCharging: start charging");
                    string retcs = car.webhelper.PostCommand("command/charge_start", null).Result;
                    Log("StartCharging charge_start result: " + retcs);
                }

                
                
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        internal virtual void StopCharging()
        {
            try
            {
                Log("StopCharging");
                string ret = car.webhelper.PostCommand("command/charge_stop", null).Result;
                Log("StopCharging result: " + ret);
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        internal virtual void Log(string message)
        {
            car.Log(LogPrefix + ": " + message);
        }
    }
}
