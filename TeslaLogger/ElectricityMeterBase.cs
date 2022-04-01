
using System;
using Exceptionless;

namespace TeslaLogger
{
    abstract class ElectricityMeterBase
    {
        public static ElectricityMeterBase Instance(Car car)
        {
            var dr = DBHelper.GetCar(car.CarInDB);
            if (dr != null)
            {
                string type = dr["meter_type"] as string ?? "";
                string host = dr["meter_host"] as string ?? "";
                string parameter = dr["meter_parameter"] as string ?? "";

                var ret = ElectricityMeterBase.Instance(type, host, parameter);
                string version = "";
                try
                {
                    version = ret?.GetVersion();
                }
                catch (Exception) { }

                if (!String.IsNullOrEmpty(version))
                {
                    car.CreateExeptionlessFeature("Wallbox_" + type).AddObject(version, "Version").Submit();
                }

                return ret;
            }

            return null;
        }


        public static ElectricityMeterBase Instance(string type, string host, string paramater)
        {
            if (type == "openwb")
                return new ElectricityMeterOpenWB(host, paramater);
            else if (type == "go-e")
                return new ElectricityMeterGoE(host, paramater);
            else if (type == "tesla-gen3")
                return new ElectricityMeterTeslaGen3WallConnector(host, paramater);
            else if (type == "shelly3em")
                return new ElectricityMeterShelly3EM(host, paramater);
            else if (type == "keba")
                return new ElectricityMeterKeba(host, paramater);

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
