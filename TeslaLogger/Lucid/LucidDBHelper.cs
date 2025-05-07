using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeslaLogger;

namespace TeslaLoggerNET8.Lucid
{
    internal class LucidDBHelper : DBHelper
    {
        internal LucidDBHelper(Car car) : base(car)
        {
        }

        internal override void CleanPasswort()
        {
        }
    }
}
