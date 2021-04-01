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
        public void ParkingP1()
        {
            var f = new FileInfo("../../Testfile-P1.txt");
            string tempfile = Path.GetTempFileName();
            f.CopyTo(tempfile,true);

            var map = new FileInfo("maps/P-51,1624-13,5748.png");
            if (map.Exists) 
                map.Delete();

            string p = f.FullName;

            string[] args = { "-jobfile", tempfile, "-debug" };
            OSMMapGenerator.Main(args);
        }

        [TestMethod]
        public void ParkingP2()
        {
            var f = new FileInfo("../../Testfile-P2.txt");
            string tempfile = Path.GetTempFileName();
            f.CopyTo(tempfile, true);

            var map = new FileInfo("maps/P-51,1576-13,6364.png");
            if (map.Exists)
                map.Delete();

            string p = f.FullName;

            string[] args = { "-jobfile", tempfile, "-debug" };
            OSMMapGenerator.Main(args);
        }

        [TestMethod]
        public void ParkingP3()
        {
            var f = new FileInfo("../../Testfile-P3.txt");
            string tempfile = Path.GetTempFileName();
            f.CopyTo(tempfile, true);

            var map = new FileInfo("maps/P-51,1576-13,6364.png");
            if (map.Exists)
                map.Delete();

            string p = f.FullName;

            string[] args = { "-jobfile", tempfile, "-debug" };
            OSMMapGenerator.Main(args);
        }

        [TestMethod]
        public void ParkingP4()
        {
            var f = new FileInfo("../../Testfile-P4.txt");
            string tempfile = Path.GetTempFileName();
            f.CopyTo(tempfile, true);

            var map = new FileInfo("maps/P-51,1576-13,6364.png");
            if (map.Exists)
                map.Delete();

            string p = f.FullName;

            string[] args = { "-jobfile", tempfile, "-debug" };
            OSMMapGenerator.Main(args);
        }

        [TestMethod]
        public void TripT1()
        {
            var f = new FileInfo("../../Testfile-T1.txt");
            string tempfile = Path.GetTempFileName();
            f.CopyTo(tempfile, true);

            var map = new FileInfo("maps/P-51,1576-13,6364.png");
            if (map.Exists)
                map.Delete();

            string p = f.FullName;

            string[] args = { "-jobfile", tempfile, "-debug" };
            OSMMapGenerator.Main(args);
        }
    }
}
