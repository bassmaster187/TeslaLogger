using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeslaLogger;

namespace TeslaLoggerNET8.Kafka
{
    internal class KafkaWebHelper : WebHelper
    {
        internal KafkaWebHelper(Car car) : base(car)
        {
        }

        public override string GetVehicles()
        {
            return "";
        }

        public override string GetRegion()
        {
            return "";
        }

        public override bool TaskerWakeupfile(bool force = false)
        {
            return true;
        }
    }
}

