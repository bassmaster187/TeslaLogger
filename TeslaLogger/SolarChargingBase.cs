using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace TeslaLogger
{
    internal class SolarChargingBase
    {
        protected Car car;
        int lastAmpere = -1;

        public SolarChargingBase(Car c) 
        { 
            car = c;
        }

        public virtual void Plugged(bool plugged)
        {

        }

        public virtual void Charging(bool charging)
        {

        }

        public virtual void SetAmpere(int ampere) {

            if (lastAmpere != ampere && car.webhelper != null) 
            {
                car.Log("SetAmps: " + ampere);
                string data = "{\"charging_amps\":" + ampere + "}";
                string res =  car?.webhelper?.PostCommand("command/set_charging_amps", data, true).Result;
                car.Log("SetAmps Result: " + res);
                lastAmpere = ampere;
            }
        }

        internal virtual void setPower(string charger_power, string charge_energy_added, string battery_level)
        {
            
        }
    }
}
