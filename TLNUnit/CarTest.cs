using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using NUnit.Framework;

namespace TeslaLogger
{
    [TestFixture()]
    public class CarTest
    {
        [Test()]
        public void CarTest_M3LRRWD()
        {
            WebHelper wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            MemoryCache.Default.Add("GetAvgMaxRage", 515, DateTime.Now.AddMinutes(1));
            wh.carSettings.car_type = "model3";
            wh.carSettings.DB_Wh_TR = "0.145";
            wh.carSettings.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR RWD", wh.carSettings.Name);
            Assert.AreEqual("0.152", wh.carSettings.Wh_TR);
        }

        [Test()]
        public void CarTest_S75D()
        {
            WebHelper wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            wh.carSettings.car_type = "models2";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.DB_Wh_TR = "0.145";
            wh.carSettings.trim_badging = "75d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 75D", wh.carSettings.Name);
            Assert.AreEqual("0.186", wh.carSettings.Wh_TR);
        }

        [Test()]
        public void CarTest_SP85()
        {
            WebHelper wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            wh.carSettings.car_type = "models";
            wh.carSettings.car_special_type = "signature";
            wh.carSettings.trim_badging = "p85";
            wh.UpdateEfficiency();

            Assert.AreEqual("S P85", wh.carSettings.Name);
            Assert.AreEqual("0.201", wh.carSettings.Wh_TR);
        }

        [Test()]
        public void CarTest_S85D()
        {
            WebHelper wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            wh.carSettings.car_type = "models";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.trim_badging = "85d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 85D", wh.carSettings.Name);
            Assert.AreEqual("0.186", wh.carSettings.Wh_TR);
        }

        [Test()]
        public void CarTest_X100D()
        {
            WebHelper wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            wh.carSettings.car_type = "modelx";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.trim_badging = "100d";
            wh.UpdateEfficiency();

            Assert.AreEqual("X 100D", wh.carSettings.Name);
            Assert.AreEqual("0.217", wh.carSettings.Wh_TR);
        }

        [Test()]
        public void CarTest_S90D()
        {
            WebHelper wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            wh.carSettings.car_type = "models2";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.trim_badging = "90d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 90D", wh.carSettings.Name);
            Assert.AreEqual("0.188", wh.carSettings.Wh_TR);
        }

        [Test()]
        public void CarTest_SRavenSR()
        {
            WebHelper wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            MemoryCache.Default.Add("GetAvgMaxRage", 443, DateTime.Now.AddMinutes(1));
            wh.carSettings.car_type = "models2";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.DB_Wh_TR = "0.163";
            wh.carSettings.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("S Raven SR", wh.carSettings.Name);
            Assert.AreEqual("0.163", wh.carSettings.Wh_TR);
        }

        [Test()]
        public void CarTest_SRavenLR()
        {
            WebHelper wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            MemoryCache.Default.Add("GetAvgMaxRage", 560, DateTime.Now.AddMinutes(1));
            wh.carSettings.car_type = "models2";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.DB_Wh_TR = "0.169";
            wh.carSettings.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("S Raven LR", wh.carSettings.Name);
            Assert.AreEqual("0.169", wh.carSettings.Wh_TR);
        }

    }
}