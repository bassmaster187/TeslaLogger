using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeslaLogger;

namespace TeslaLoggerNET8.Lucid
{
    internal class LucidCar : Car
    {
        public LucidCar(int CarInDB, string TeslaName, string TeslaPasswort, int CarInAccount, string TeslaToken, DateTime TeslaTokenExpire, string ModelName, string cartype, string carspecialtype, string cartrimbadging, string displayname, string vin, string TaskerHash, double? WhTR, bool fleetAPI, TeslaState currentState = TeslaState.Start, string wheel_type = "") 
            : base(CarInDB, TeslaName, TeslaPasswort, CarInAccount, TeslaToken, TeslaTokenExpire, ModelName, cartype, carspecialtype, cartrimbadging, displayname, vin, TaskerHash, WhTR, fleetAPI, currentState, wheel_type)
        {
            Program.SuspendAPIMinutes = 0;
            SleepInStateSleep = 30000;   
        }

        internal override bool SupportedByFleetTelemetry()
        {
            return false;
        }
    }
}
