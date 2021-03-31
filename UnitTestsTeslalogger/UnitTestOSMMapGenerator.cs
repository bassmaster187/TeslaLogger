using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestOSMMapGenerator
    {
        [TestMethod]
        public void ParkingUlm()
        {
            var f = new FileInfo("../../Testfile-P1.txt");
            string tempfile = Path.GetTempFileName();
            f.CopyTo(tempfile,true);

            string p = f.FullName;

            string[] args = { "-jobfile", tempfile, "-debug" };
            OSMMapGenerator.Main(args);
        }
    }
}
