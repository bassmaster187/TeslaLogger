
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaLogger
{
    class ElectricityMeterBase
    {
        private string host;
        private string paramater;

        public static ElectricityMeterBase Instance(int carid)
        {
            var dr = DBHelper.GetCar(carid);
            if (dr != null)
            {
                string type = dr["meter_type"] as string ?? "";
                string host = dr["meter_host"] as string ?? "";
                string parameter = dr["meter_parameter"] as string ?? "";

                return ElectricityMeterBase.Instance(type, host, parameter);
            }

            return null;
        }


        public static ElectricityMeterBase Instance(string type, string host, string paramater)
        {
            if (type == "openwb")
                return new ElectricityMeterOpenWB(host, paramater);
            else if (type == "go-e")
                return new ElectricityMeterGoE(host, paramater);
            return null;
        }

        public virtual bool? IsCharging()
        {
            return false;
        }

        public virtual double? GetVehicleMeterReading_kWh()
        {
            return null;
        }

        // GetUtilityMeterReading_kWh will be used to calculate the rate of solar energy used
        public virtual double? GetUtilityMeterReading_kWh()
        {
            return null;
        }

        public virtual string GetVersion()
        {
            return null;
        }

        public override string ToString()
        {
            var isCharging = IsCharging();
            var vm = GetVehicleMeterReading_kWh();
            var evu = GetUtilityMeterReading_kWh();
            var version = GetVersion();

            string ret = $"IsCharging: {isCharging} / Vehicle Meter: {vm} kWh / Utility Meter: {evu ?? Double.NaN} kWh / Class: {this.GetType().Name} / Version: {version}";
            return ret;
        }

    }
}
