using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeslaLogger;

namespace TeslaLoggerNET8.Kafka
{
    internal class KafkaDBHelper :DBHelper
    {
        internal KafkaDBHelper(Car car) : base(car)
        {
        }
        internal override void CleanPasswort()
        {
        }

        public override async Task<string> UpdateCountryCodeAsync()
        {
            return "";
        }

        public override double GetVoltageAt50PercentSOC(out DateTime start, out DateTime ende)
        {
            start = DateTime.Now;
            ende = DateTime.Now;
            return 0.0;
        }
    }
}

