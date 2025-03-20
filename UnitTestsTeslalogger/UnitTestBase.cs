using System;
using TeslaLogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.Caching;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using SRTM;
using System.Web;

using Newtonsoft.Json;
using System.Data;
using System.Security.Cryptography;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestBase
    {
        // [TestMethod]
        public void TestJapanese()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);

            Tools.SetThreadEnUS();
            long unixTimestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            unixTimestamp *= 1000;
            c.DbHelper.InsertPos(unixTimestamp.ToString(), 48.456691, 10.030241, 0, 0, 1, 0, 0, 0, 0, "0");
            int startid = c.DbHelper.GetMaxPosid(true);
            c.DbHelper.StartDriveState(DateTime.Now);

            c.DbHelper.InsertPos(unixTimestamp.ToString(), 35.677121, 139.751033, 0, 0, 2, 0, 0, 0, 0, "0");
            int endid = c.DbHelper.GetMaxPosid(true);
            c.DbHelper.CloseDriveState(DateTime.Now);
        }


        [TestMethod]
        public void Car_S85D()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "models";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "85d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 85D", wh.car.ModelName);
            Assert.AreEqual(0.186, wh.car.WhTR);
        }

        [TestMethod]
        public void Car_S85D_350V()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "models";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "85d";
            wh.car.CarVoltageAt50SOC = 336;
            wh.UpdateEfficiency();

            Assert.AreEqual("S 85D 350V", wh.car.ModelName);
            Assert.AreEqual(0.186, wh.car.WhTR);
        }

        [TestMethod]
        public void Car_S_P85()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "models";
            wh.car.CarSpecialType = "signature";
            wh.car.TrimBadging = "p85";
            wh.UpdateEfficiency();

            Assert.AreEqual("S P85", wh.car.ModelName);
            Assert.AreEqual(0.201, wh.car.WhTR);
        }


        [TestMethod]
        public void Car_M3_LR_RWD()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 515, DateTime.Now.AddMinutes(1));
            wh.car.CarType = "model3";
            wh.car.DBWhTR = 0.145;
            wh.car.TrimBadging = "";
            wh.car.Vin = "5YJ3E7EA9KFxxxxxx";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR RWD 2019", wh.car.ModelName);
            Assert.AreEqual(0.145, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_RWD2()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 535, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "5YJ3E7EA9KFxxxxxx";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.145;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR RWD 2019", wh.car.ModelName);
            Assert.AreEqual(0.145, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_RWD_2023()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 535, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRW3E7ES7PCXXXXXX";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.142;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR RWD 2023", wh.car.ModelName);
            Assert.AreEqual(0.142, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_P_2019()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            //2021 Model 3 LR Performance
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 422, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "5YJ3E7EB1KFXXXXXX";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "p74d";
            wh.car.DBWhTR = 0.152;
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR P", wh.car.ModelName);
            Assert.AreEqual(0.152, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_RWD_2019()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;


            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 488, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "5YJ3E7EA5KFXXXXXX";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "74";
            wh.car.DBWhTR = 0.145;
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR RWD 2019", wh.car.ModelName);
            Assert.AreEqual(0.145, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_P_2021()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            //2021 Model 3 LR Performance
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 505, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "5YJ3E7EC2MFXXXXXX";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.158;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR P 2021", wh.car.ModelName);
            Assert.AreEqual(0.158, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_P_2024_MIC()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            //2021 Model 3 LR Performance
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 505, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRW3E7ET7RCXXXXXX";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.147;
            wh.car.TrimBadging = "p74d";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR P 2024", wh.car.ModelName);
            Assert.AreEqual(0.147, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_2024_MIC_Without_vehicle_config()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            //2021 Model 3 LR Highland
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 550, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRW3E7EK2RCXXXXXX";
            wh.car.CarType = "";
            wh.car.CarSpecialType = "";
            wh.car.DBWhTR = 0.141;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR 2024", wh.car.ModelName);
            Assert.AreEqual(0.141, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_2024_MIC()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            //2021 Model 3 LR Highland
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 550, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRW3E7EK2RCXXXXXX";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.141;
            wh.car.TrimBadging = "74d";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR 2024", wh.car.ModelName);
            Assert.AreEqual(0.141, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_P_2024_MIC_Without_vehicle_config()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            //2021 Model 3 LR Performance
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 505, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRW3E7ET7RCXXXXXX";
            wh.car.CarType = "";
            wh.car.CarSpecialType = "";
            wh.car.DBWhTR = 0;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR P 2024", wh.car.ModelName);
            Assert.AreEqual(0.147, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_P_2021_MIC()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            //2021 Model 3 LR Performance
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 505, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRW3E7EL1MCXXXXXX";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.152;
            wh.car.TrimBadging = "p74d";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR P 2021", wh.car.ModelName);
            Assert.AreEqual(0.158, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 460, DateTime.Now.AddMinutes(1));
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "74d";
            wh.car.Vin = "5YJ3E7EB7KFXXXXXX";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR", wh.car.ModelName);
            Assert.AreEqual(0.152, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_DegradedBattery()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 430, DateTime.Now.AddMinutes(1));
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "74d";
            wh.car.Vin = "5YJ3E7EB7KFXXXXXX";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR", wh.car.ModelName);
            Assert.AreEqual(0.152, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_LR_P()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 491, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "5YJ3F7EC1LFXXXXXX";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.152;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR P", wh.car.ModelName);
            Assert.AreEqual(0.158, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_SRPlus_LFP()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 407, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRW3E7FA9LCxxxxxx";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.133;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 SR+ LFP", wh.car.ModelName);
            Assert.AreEqual(0.133, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_SRPlus_LFP_2021()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 420, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRW3E7FA1MCxxxxxx";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 127;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 SR+ LFP 2021", wh.car.ModelName);
            Assert.AreEqual(0.127, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_SRPlus_LFP_2021_60kWh()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 430, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRW3E7FA3MCCxxxxxx";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 138;
            wh.car.TrimBadging = "50";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 SR+ LFP 2021", wh.car.ModelName);
            Assert.AreEqual(0.127, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_SRPlus_LFP_2021_without_charging()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 0, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRW3E7FA1MCxxxxxx";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 127;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 SR+ LFP 2021", wh.car.ModelName);
            Assert.AreEqual(0.127, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_M3_SRPlus_2021()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            //2021 Model 3 SR+
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 407, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "5YJ3E7EA3MFXXXXXX";
            wh.car.CarType = ""; // sometimes empty !!!
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.126;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 SR+ 2021", wh.car.ModelName);
            Assert.AreEqual(0.126, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_S_75D()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "models2";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.145;
            wh.car.TrimBadging = "75d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 75D", wh.car.ModelName);
            Assert.AreEqual(0.186, wh.car.WhTR);
        }

        [TestMethod]
        public void Car_S_75D_400V()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "models2";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.145;
            wh.car.TrimBadging = "75d";
            wh.car.CarVoltageAt50SOC = 380;
            wh.UpdateEfficiency();

            Assert.AreEqual("S 75D 400V", wh.car.ModelName);
            Assert.AreEqual(0.186, wh.car.WhTR);
        }


        [TestMethod]
        public void Car_S_90D()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "models2";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "90d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 90D", wh.car.ModelName);
            Assert.AreEqual(0.188, wh.car.WhTR);
        }

        [TestMethod]
        public void Car_S_Raven_SR()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 443, DateTime.Now.AddMinutes(1));
            wh.car.CarType = "models2";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.163;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("S Raven SR", wh.car.ModelName);
            Assert.AreEqual(0.163, wh.car.WhTR);
        }

        [TestMethod]
        public void Car_S_Raven_LR()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 560, DateTime.Now.AddMinutes(1));
            wh.car.CarType = "models2";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.169;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("S Raven LR", wh.car.ModelName);
            Assert.AreEqual(0.173, wh.car.WhTR);
        }

        [TestMethod]
        public void Car_S_100D_Raven()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 594, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "5YJSA7E28LFXXXXXX";
            wh.car.CarType = "models2";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.162;
            wh.car.TrimBadging = "100d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 100D Raven", wh.car.ModelName);
            Assert.AreEqual(0.162, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsFalse(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_S_Raven_LR_P()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 546, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "5YJSA7E43LFXXXXXX";
            wh.car.CarType = "models2";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.173;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("S Raven LR P", wh.car.ModelName);
            Assert.AreEqual(0.173, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsFalse(supportedByFleetTelemetry);
        }


        [TestMethod]
        public void Car_X_100D()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "modelx";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "100d";
            wh.UpdateEfficiency();

            Assert.AreEqual("X 100D", wh.car.ModelName);
            Assert.AreEqual(0.217, wh.car.WhTR);
        }

        [TestMethod]
        public void Car_X_P100D_founder()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "modelx";
            wh.car.CarSpecialType = "founder";
            wh.car.TrimBadging = "p100d";
            wh.UpdateEfficiency();

            Assert.AreEqual("X P100D", wh.car.ModelName);
            Assert.AreEqual(0.226, wh.car.WhTR);
        }
        
        [TestMethod]
        public void Car_X_100DRaven()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "modelx";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "100d";
            wh.car.Raven = true;
            wh.car.Vin = "5YJXCCE2XLFXXXXXX";
            wh.UpdateEfficiency();

            Assert.AreEqual("X 100D", wh.car.ModelName); // Raven suffix will be appended later
            Assert.AreEqual(0.184, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsFalse(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_X_2021_Plaid()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "tamarind";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "p100d";
            wh.car.Vin = "7SAXCBE62NFxxxxxx";
            wh.UpdateEfficiency();

            Assert.AreEqual("X 2021 Plaid", wh.car.ModelName);
            Assert.AreEqual(0.193, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_X_2021_Plaid_vin()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            // wh.car.CarType = "tamarind";
            // wh.car.CarSpecialType = "base";
            // wh.car.TrimBadging = "p100d";
            wh.car.Vin = "7SAXCBE62NFxxxxxx";
            wh.UpdateEfficiency();

            Assert.AreEqual("X 2021 Plaid", wh.car.ModelName);
            Assert.AreEqual(0.193, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_S_2021_Plaid()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "lychee";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "p100d";
            wh.car.Vin = "5YJSA7E66PFxxxxxx";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 2021 Plaid", wh.car.ModelName);
            Assert.AreEqual(0.172, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_S_2021_Plaidvin()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            // wh.car.CarType = "lychee";
            // wh.car.CarSpecialType = "base";
            // wh.car.TrimBadging = "p100d";
            wh.car.Vin = "5YJSA7E66PFxxxxxx";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 2021 Plaid", wh.car.ModelName);
            Assert.AreEqual(0.172, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_S_2021_LRvinTelemetry()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "models";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "100d";
            wh.car.Vin = "SYJSA7E53PFXXXXXX";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 2021 LR", wh.car.ModelName);
            Assert.AreEqual(0.151, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_S_2021_PlaidvinTelemetry()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "models";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "p100d";
            wh.car.Vin = "5YJSA7E66PFXXXXXX";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 2021 Plaid", wh.car.ModelName);
            Assert.AreEqual(0.172, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_X_2021_LRvinTelemetry()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "modelx";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "100d";
            wh.car.Vin = "7SAXCCE50PFXXXXXX";
            wh.UpdateEfficiency();

            Assert.AreEqual("X 2021 LR", wh.car.ModelName);
            Assert.AreEqual(0.181, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_Y_LR_AWD_US()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 520, DateTime.Now.AddMinutes(1));
            wh.car.CarType = "modely";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.148;
            wh.car.TrimBadging = "74d";
            wh.UpdateEfficiency();

            Assert.AreEqual("Y LR AWD (US)", wh.car.ModelName);
            Assert.AreEqual(0.148, wh.car.WhTR);
        }

        [TestMethod]
        public void Car_Y_LR_AWD_MIC_2021()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 500, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRWYGCEK3MCXXXXXX";
            wh.car.CarType = "modely";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.148;
            wh.car.TrimBadging = "74d";
            wh.UpdateEfficiency();

            Assert.AreEqual("Y LR AWD (MIC 2021)", wh.car.ModelName);
            Assert.AreEqual(0.148, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_Y_LR_AWD_MIC_2022()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 520, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "LRWYGCEKXNCXXXXXX";
            wh.car.CarType = "modely";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.148;
            wh.car.TrimBadging = "74d";
            wh.UpdateEfficiency();

            Assert.AreEqual("Y LR AWD (MIC 2022)", wh.car.ModelName);
            Assert.AreEqual(0.148, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }
        [TestMethod]
        public void Car_Y_SR_MIG_BYD()
        {
            string[] VINSs = {
                "XP7YGCEJ1PB",
                "XP7YGCEJ2PB",
                "XP7YGCEJ3PB",
                "XP7YGCEJ6PB",
                "XP7YGCEJ7PB",
                "XP7YGCEJ8PB",
                "XP7YGCEJ9PB",
                "XP7YGCES0RB",
                "XP7YGCES1RB",
                "XP7YGCES4RB",
                "XP7YGCES8PB",
                "XP7YGCES9RB" };

            foreach (string vin in VINSs)
            {
                Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
                WebHelper wh = c.webhelper;

                MemoryCache.Default.Remove("GetAvgMaxRage_0");
                MemoryCache.Default.Add("GetAvgMaxRage_0", 520, DateTime.Now.AddMinutes(1));
                wh.car.Vin = vin + "XXXXXX";
                wh.car.CarType = "modely";
                wh.car.CarSpecialType = "base";
                wh.car.DBWhTR = 0.142;
                wh.car.TrimBadging = "50";
                wh.UpdateEfficiency();

                Assert.AreEqual("Y SR (MIG BYD)", wh.car.ModelName);
                Assert.AreEqual(0.142, wh.car.WhTR);

                bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
                Assert.IsTrue(supportedByFleetTelemetry);
            }
        }

        [TestMethod]
        public void Car_Y_SR_MIG_CATL()
        {
            string[] VINSs = { "XP7YGCFS2RB", "XP7YGCFSXRB" };

            foreach (string vin in VINSs)
            {
                Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
                WebHelper wh = c.webhelper;

                MemoryCache.Default.Remove("GetAvgMaxRage_0");
                MemoryCache.Default.Add("GetAvgMaxRage_0", 520, DateTime.Now.AddMinutes(1));
                wh.car.Vin = vin + "XXXXXX";
                wh.car.CarType = "modely";
                wh.car.CarSpecialType = "base";
                wh.car.DBWhTR = 0.142;
                wh.car.TrimBadging = "50";
                wh.UpdateEfficiency();

                Assert.AreEqual("Y SR (MIG CATL)", wh.car.ModelName);
                Assert.AreEqual(0.142, wh.car.WhTR);

                bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
                Assert.IsTrue(supportedByFleetTelemetry);
            }
        }

        [TestMethod]
        public void Car_Y_SR_MIC()
        {
            string[] VINSs = { "LRWYGCFSXPC" };

            foreach (string vin in VINSs)
            {
                Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
                WebHelper wh = c.webhelper;

                MemoryCache.Default.Remove("GetAvgMaxRage_0");
                MemoryCache.Default.Add("GetAvgMaxRage_0", 520, DateTime.Now.AddMinutes(1));
                wh.car.Vin = vin + "XXXXXX";
                wh.car.CarType = "modely";
                wh.car.CarSpecialType = "base";
                wh.car.DBWhTR = 0.142;
                wh.car.TrimBadging = "50";
                wh.UpdateEfficiency();

                Assert.AreEqual("Y SR (MIC)", wh.car.ModelName);
                Assert.AreEqual(0.142, wh.car.WhTR);

                bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
                Assert.IsTrue(supportedByFleetTelemetry);
            }
        }

        [TestMethod]
        public void Car_YP_MIG_First30()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 520, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "XP7YGCELONBXXXXXX";
            wh.car.CarType = "modely"; // ???
            wh.car.CarSpecialType = "base"; // ???
            wh.car.DBWhTR = 0.165; // ???
            wh.car.TrimBadging = "p74d"; // ???
            wh.UpdateEfficiency();

            Assert.AreEqual("Y P (MIG)", wh.car.ModelName);
            Assert.AreEqual(0.165, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void Car_Y_LR_RWD()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 520, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "XP7YGCER0RBXXXXXX";
            wh.car.CarType = "modely"; 
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 149; 
            wh.car.TrimBadging = "74"; 
            wh.UpdateEfficiency();

            Assert.AreEqual("Y LR RWD", wh.car.ModelName);
            Assert.AreEqual(0.149, wh.car.WhTR);

            bool supportedByFleetTelemetry = c.SupportedByFleetTelemetry();
            Assert.IsTrue(supportedByFleetTelemetry);
        }

        [TestMethod]
        public void VersionCheck()
        {
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.0", Tools.UpdateType.all));
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.1", Tools.UpdateType.all));
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.1", "1.0.0.0", Tools.UpdateType.all));

            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.0", Tools.UpdateType.stable));

            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.1", Tools.UpdateType.stable));
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.1.0", Tools.UpdateType.stable));
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.1.0.0", Tools.UpdateType.stable));
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.2.3.4", Tools.UpdateType.stable));

            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "2.0.0.1", Tools.UpdateType.stable));
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "2.0.0.0", Tools.UpdateType.stable));
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "2.1.0.0", Tools.UpdateType.stable));

            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.1", "1.0.0.0", Tools.UpdateType.stable));

            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.0", Tools.UpdateType.none));
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.1", Tools.UpdateType.none));
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.1", "1.0.0.0", Tools.UpdateType.none));
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.1.0.0", Tools.UpdateType.none));

            // None is now the same as stable
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.0.1", Tools.UpdateType.none));
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.0.1.0", Tools.UpdateType.none));
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.1.0.0", Tools.UpdateType.none));
            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "1.2.3.4", Tools.UpdateType.none));

            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "2.0.0.1", Tools.UpdateType.none));
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "2.0.0.0", Tools.UpdateType.none));
            Assert.IsTrue(UpdateTeslalogger.UpdateNeeded("1.0.0.0", "2.1.0.0", Tools.UpdateType.none));

            Assert.IsFalse(UpdateTeslalogger.UpdateNeeded("1.0.0.1", "1.0.0.0", Tools.UpdateType.none));
        }

        [TestMethod]
        public void ParseDashboard()
        {
            string dashboard = System.IO.File.ReadAllText("../../../TeslaLogger/Grafana/Verbrauch.json");
            string title, uid, link;
            UpdateTeslalogger.GrafanaGetTitleAndLink(dashboard, "http://raspberry:3000/", out title, out uid, out link);
            Assert.AreEqual("Verbrauch", title);
            Assert.AreEqual("zm7wN6Zgz", uid);
            Assert.AreEqual("http://raspberry:3000/d/zm7wN6Zgz/Verbrauch", link);
        }

        [TestMethod]
        public void UpdateDefaultCar()
        {
            string dashboard = System.IO.File.ReadAllText("../../../TeslaLogger/Grafana/Verbrauch.json");
            dashboard = UpdateTeslalogger.UpdateDefaultCar(dashboard, "BATmobil", "2", "Fahrzeug");

            Assert.IsTrue(dashboard.Contains("\"text\": \"BATmobil\","));
            Assert.IsTrue(dashboard.Contains("\"label\": \"Fahrzeug\","));


            dashboard = System.IO.File.ReadAllText("../../../TeslaLogger/Grafana/Trip.json");
            dashboard = UpdateTeslalogger.UpdateDefaultCar(dashboard, "BATmobil", "2", "Fahrzeug");

            Assert.IsTrue(dashboard.Contains("\"text\": \"BATmobil\","));
            Assert.IsTrue(dashboard.Contains("\"label\": \"Fahrzeug\","));
        }

        [TestMethod]
        public void UpdateDatasource()
        {
            var dashboard = System.IO.File.ReadAllText("../../../TeslaLogger/Grafana/Trip.json");
            dashboard = UpdateTeslalogger.UpdateDatasourceUID(dashboard, "000000001");

            Assert.IsTrue(dashboard.Contains("\"uid\": \"000000001\""));
        }

        [TestMethod]
        public void POI()
        {
            var geofence = Geofence.GetInstance();

            var a = geofence.GetPOI(48.456708, 10.029897);
            Assert.AreEqual(a.name, "⚡⚡ Supercharger DE-Ulm");

            a = geofence.GetPOI(48.456616, 10.030200);
            Assert.AreEqual(a.name, "⚡⚡ Supercharger DE-Ulm");

            a = geofence.GetPOI(48.456790, 10.030014);
            Assert.AreEqual(a.name, "⚡⚡ Supercharger DE-Ulm");

            a = geofence.GetPOI(48.456691, 10.030241);
            Assert.AreEqual(a.name, "⚡⚡ Supercharger DE-Ulm");

            a = geofence.GetPOI(48.456888, 10.029635);
            Assert.AreEqual(a.name, "EnBW DE-Ulm Seligweiler");
        }

        [TestMethod]
        public void UpdateLanguage()
        {
            Dictionary<string, string> dictLanguage = UpdateTeslalogger.GetLanguageDictionary("ru");

            string s = System.IO.File.ReadAllText("../../../TeslaLogger/Grafana/Verbrauchsstatstik.json");
            s = UpdateTeslalogger.ReplaceTitleTag(s, "Verbrauchsstatistik", dictLanguage);
            s = UpdateTeslalogger.ReplaceLanguageTags(s, new string[] {
                                    "km Stand [km]","mi Stand [mi]","Verbrauch Monatsmittel [kWh]","Außentemperatur Monatsmittel [°C]","Außentemperatur Monatsmittel [°F]","Verbrauch Tagesmittel [kWh]","Außentemperatur Tagesmittel [°C]", "Außentemperatur Tagesmittel [°F]"
                                }, dictLanguage, true);

            Assert.IsFalse(s.Contains("km Stand [km]"));

        }

        [TestMethod]
        public void UpdateLanguageNewTable()
        {
            Dictionary<string, string> dictLanguage = UpdateTeslalogger.GetLanguageDictionary("ru");

            string s = System.IO.File.ReadAllText("../../../TeslaLogger/Grafana/Trip.json");
            s = UpdateTeslalogger.ReplaceValuesTags(s, dictLanguage);
            Assert.IsFalse(s.Contains("Start Adresse"));
        }

        [TestMethod]
        public void AllowUnsignedPlugins()
        {
            UpdateTeslalogger.AllowUnsignedPlugins("../../grafana.ini", false);
        }


        [TestMethod]
        public void CreateAuthTokenFromRefreshToken()
        {
            /*
            DBHelper.ExecuteSQLQuery("update cars set tesla_token = '', tesla_token_expire='2020-01-01' where id = 1");
            
            Thread t = new Thread(() => Program.GetAllCars());
            t.Start();

            for (int x = 0; x < 300; x++)
            {
                string tt = DBHelper.ExecuteSQLScalar("Select tesla_token from cars where id=1").ToString();
                if (tt.Length > 10)
                {
                    t.Abort();
                    return;
                }

                System.Threading.Thread.Sleep(1000);
            }

            t.Abort();

            Assert.Fail("could not get Auth Token from Refresh Token!");
            */
        }

        [TestMethod]
        public void CreateAuthTokenWithoutRefreshToken()
        {
            /*
            DBHelper.ExecuteSQLQuery("update cars set tesla_token = '', refresh_token = '', tesla_token_expire='2020-01-01' where id = 1");

            Thread t = new Thread(() => Program.GetAllCars());
            t.Start();

            for (int x = 0; x < 300; x++)
            {
                string tt = DBHelper.ExecuteSQLScalar("Select tesla_token from cars where id=1").ToString();
                if (tt.Length > 10)
                {
                    t.Abort();
                    return;
                }

                System.Threading.Thread.Sleep(1000);
            }

            t.Abort();

            Assert.Fail("could not get Auth Token from Refresh Token!");
            */
        }

        [TestMethod]
        public void SendDataToAbetterrouteplanner()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            long ts = Tools.ToUnixTime(DateTime.Now) / 1000;

            wh.SendDataToAbetterrouteplannerAsync(ts, 55, 0, false, 0, 0, 0).Wait();
        }

        [TestMethod]
        public void UpdateApacheConfig()
        {
            var temp = UpdateTeslalogger.UpdateApacheConfig("../../apache2.conf", false);
            var expected = System.IO.File.ReadAllText("../../apache2-ready.conf");
            Assert.AreEqual(expected, temp);
        }

        [TestMethod]
        public void UpdateApacheConfigUnchanged()
        {
            var temp = UpdateTeslalogger.UpdateApacheConfig("../../apache2-ready.conf", false);
            var expected = System.IO.File.ReadAllText("../../apache2-ready.conf");
            Assert.AreEqual(expected, temp);
        }

        [TestMethod]
        public void Srtm()
        {
            string path = "srtmcache";
            if (System.IO.Directory.Exists(path))
                System.IO.Directory.Delete(path, true);

            System.IO.Directory.CreateDirectory(path);

            var srtmData = new SRTMData(path);

            // get elevations for some locations
            int? elevation = srtmData.GetElevation(47.267222, 11.392778);
            Console.WriteLine("Elevation of Innsbruck: {0}m", elevation);
            Assert.AreEqual(584, elevation);

            elevation = srtmData.GetElevation(-16.5, -68.15);
            Console.WriteLine("Elevation of La Paz: {0}m", elevation);
            Assert.AreEqual(3782, elevation);

            elevation = srtmData.GetElevation(27.702983735525862f, 85.2978515625f);
            Console.WriteLine("Elevation of Kathmandu {0}m", elevation);
            Assert.AreEqual(1312, elevation);

            elevation = srtmData.GetElevation(21.030673628606102f, 105.853271484375f);
            Console.WriteLine("Elevation of Ha Noi {0}m", elevation);
            Assert.AreEqual(14, elevation);
        }

        // [TestMethod]
        public void CheckJsonString()
        {
            string resultContent = System.IO.File.ReadAllText("../../CheckJsonString.txt");
            object jsonResult = JsonConvert.DeserializeObject(resultContent);
            Assert.IsNotNull(jsonResult);
        }

        // [TestMethod]
        public void CheckExportColumn()
        {
            ShareData.UpdateDataTable("chargingstate");
        }

        // [TestMethod]
        public void GeocacheBasic()
        {
            GeocodeCache.useGeocodeCache = false;

            // xx GeocodeCache.ClearCache();
            GeocodeCache.Insert(10, 20, "Test");
            string temp = GeocodeCache.Search(10, 20);
            Assert.AreEqual("Test", temp);
            temp = GeocodeCache.Search(11, 20);
            Assert.IsNull(temp);

            GeocodeCache.Insert(10, 20, "Test");
            temp = GeocodeCache.Search(10, 20);
            Assert.AreEqual("Test", temp);

            // xx GeocodeCache.ClearCache();
        }

        [TestMethod]
        public void TeslaApiVehicles1()
        {
            var json = System.IO.File.ReadAllText("../../TeslaApiVehicles.txt");
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJSA7E21JF230000", "", null, false);
            var t = new TeslaAPIState(c);
            t.ParseAPI(json, "vehicles");

            t.GetString("vin", out string vin);
            Assert.AreEqual("5YJSA7E21JF230000", vin);

            t.GetString("state", out string state);
            Assert.AreEqual("online", state);

            t.GetBool("in_service", out bool in_service);
            Assert.AreEqual(false, in_service);

            t.GetString("id", out string id);
            Assert.AreEqual("1492932463490000", id);

            t.GetString("vehicle_id", out string vehicle_id);
            Assert.AreEqual("163110000", vehicle_id);

            t.GetString("display_name", out string display_name);
            Assert.AreEqual("Teslarossa", display_name);

        }

        [TestMethod]
        public void TeslaApiVehicles2()
        {
            var json = System.IO.File.ReadAllText("../../TeslaApiVehicles.txt");
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF760000", "", null, false);
            var t = new TeslaAPIState(c);
            t.ParseAPI(json, "vehicles");

            t.GetString("vin", out string vin);
            Assert.AreEqual("5YJ3E7EA3LF760000", vin);

            t.GetString("state", out string state);
            Assert.AreEqual("asleep", state);

            t.GetBool("in_service", out bool in_service);
            Assert.AreEqual(false, in_service);

            t.GetString("id", out string id);
            Assert.AreEqual("1492932263280000", id);

            t.GetString("vehicle_id", out string vehicle_id);
            Assert.AreEqual("95020000", vehicle_id);

            t.GetString("display_name", out string display_name);
            Assert.AreEqual("Tessi", display_name);

        }

        [TestMethod]
        public void TeslaApiVehiclesEmpty()
        {
            var json = "{\"response\":[],\"Count\":0}";
            Car c = new Car(1, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            var t = new TeslaAPIState(c);
            var ret = t.ParseAPI(json, "vehicles");

            Assert.IsFalse(ret);
        }

        // [TestMethod] file is outdated
        public void TeslaApiUpdateAvailable()
        {
            var json = System.IO.File.ReadAllText("../../vehicle_state_with_update_available.txt");
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            var t = new TeslaAPIState(c);
            t.ParseAPI(json, "vehicle_state");

            t.GetString("software_update.status", out string status);
            Assert.AreEqual("available", status);

            t.GetInt("software_update.download_perc", out int download_perc);
            Assert.AreEqual(100, download_perc);

            t.GetInt("software_update.expected_duration_sec", out int expected_duration_sec);
            Assert.AreEqual(1500, expected_duration_sec);

            t.GetInt("software_update.install_perc", out int install_perc);
            Assert.AreEqual(10, install_perc);

            t.GetString("software_update.version", out string version);
            Assert.AreEqual("2022.4.5.3", version);
        }

        [TestMethod]
        public void TestEncryption()
        {
            var body = "hfsdohfoiHZIOUzhhr9083urhf983u4z098ZH)(/z0ß98zhgo8/ZT9ßp8uzh)PO(/Z noifjn398ru039euoiH)(Pzru9p83u4rjoifhnejj09f8u3094rj3f9843ur09ujoi";
            var pass = "27dbeab3-1574-4bad-a7b5-d7dadb0de2d1";
            var wrongPass = "hjiodgfodfgj";

            string encrypted = StringCipher.Encrypt(body, pass);
            string decrypted = StringCipher.Decrypt(encrypted, pass);

            Assert.AreEqual(body, decrypted);
            Assert.AreNotEqual(body, encrypted);

            var expectedExceptionCatched = false;

            try
            {
                string decryptedWrong = StringCipher.Decrypt(encrypted, wrongPass);
                Assert.Fail("Should throw an exception");
            }
            catch (CryptographicException ex)
            {
                expectedExceptionCatched = true;
                System.Diagnostics.Debug.WriteLine("Expected CryptographicException catched");
            }

            if (!expectedExceptionCatched)
                Assert.Fail("Expected CryptographicException not thrown");
        }

        /*
        [TestMethod]
        public void RefreshAuthTokenFromRefrehToken()
        {
            // ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "ffffffff", null);

            c.CarInDB = 1;
            c.webhelper.UpdateTeslaTokenFromRefreshToken();
        }
        */

        // [TestMethod]
        public void TestAuthTesla()
        {
            TeslaAuth t = new TeslaAuth();
            var url = t.GetLoginUrlForBrowser();
            string newurl = "https://auth.tesla.com/void/callback?code=1c0939d1421cd504cca7405b76c92b25fb6b2419e88e2231b568c70e7671&state=xqXt8LsGQle0UximPatd&issuer=https%3A%2F%2Fauth.tesla.com%2Foauth2%2Fv3";
            var tokens = t.GetTokenAfterLoginAsync(newurl).Result;
        }

        [TestMethod]
        public void SleepRateLimit()
        {
            DateTime startOfDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0 ,0, DateTimeKind.Utc);
            int s = Tools.CalculateSleepSeconds(300, 0, startOfDay);
            Assert.AreEqual(s, 288);

            DateTime oneHourRemaining = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 0, 0, DateTimeKind.Utc);
            int s2 = Tools.CalculateSleepSeconds(300, 100, oneHourRemaining);
            Assert.AreEqual(s2, 18);

            DateTime fiveMinuteRemaining = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 55, 0, DateTimeKind.Utc);

            int s3 = Tools.CalculateSleepSeconds(300, 0, fiveMinuteRemaining);
            Assert.AreEqual(s2, 18);
        }

        [TestMethod]
        public void ParsePackCurrent()
        {
            string data =
                "{\"data\":[{\"key\":\"PackCurrent\",\"value\":{\"stringValue\":\"121.900\"}},{\"key\":\"ChargeState\",\"value\":{\"stringValue\":\"Idle\"}},{\"key\":\"FastChargerPresent\",\"value\":{\"stringValue\":\"true\"}}],\"createdAt\":\"2024-07-07T15:11:05.839516023Z\",\"vin\":\"5YJ3E7EA3LF123456\"}";
            dynamic j = JsonConvert.DeserializeObject(data);
            dynamic jData = j["data"];

            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            WebHelper wh = c.webhelper;

            TelemetryParser f = new TelemetryParser(c);
            double? current = f.PackCurrent(jData, DateTime.Now);
            Assert.AreEqual(current, 121.9d);

        }

        [TestMethod]
        public void IsInUnitTest()
        {
            var inTest = Tools.IsUnitTest();
            Assert.IsTrue(inTest, "Should detect that we are in unit test");
        }

        [TestMethod]
        public void TestJWT1()
        {
            string token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InFEc3NoM2FTV0cyT05YTTdLMzFWV0VVRW5BNCJ9.eyJpc3MiOiJodHRwczovL2F1dGgudGVzbGEuY29tL29hdXRoMi92My9udHMiLCJhenAiOiI5MWQ0ZGEyYTM0NjgtNGI4MS1hZGJhLWQyYjI3MWJlZGZhMiIsInN1YiI6ImVhZTU4YzE4LWRiODEtNGZhZC05NDU2LTJiZDJhOWYyZjg0MCIsImF1ZCI6WyJodHRwczovL2ZsZWV0LWFwaS5wcmQubmEudm4uY2xvdWQudGVzbGEuY29tIiwiaHR0cHM6Ly9mbGVldC1hcGkucHJkLmV1LnZuLmNsb3VkLnRlc2xhLmNvbSIsImh0dHBzOi8vYXV0aC50ZXNsYS5jb20vb2F1dGgyL3YzL3VzZXJpbmZvIl0sInNjcCI6WyJvZmZsaW5lX2FjY2VzcyIsIm9wZW5pZCIsInVzZXJfZGF0YSIsInZlaGljbGVfZGV2aWNlX2RhdGEiLCJ2ZWhpY2xlX2NtZHMiLCJ2ZWhpY2xlX2NoYXJnaW5nX2NtZHMiXSwiYW1yIjpbInB3ZCIsIm1mYSIsIm90cCJdLCJleHAiOjE3MzgwNzA4ODQsImlhdCI6MTczODA0MjA4NCwib3VfY29kZSI6IkVVIiwibG9jYWxlIjoiZGEtREsiLCJhY2NvdW50X3R5cGUiOiJidXNpbmVzcyIsIm9wZW5fc291cmNlIjpudWxsLCJhY2NvdW50X2lkIjoiNGZjZTM3OWMtMmU0NS00Y2Y0LTkwYmMtNDZhMzE5MTZkZDE0IiwiYXV0aF90aW1lIjoxNzM4MDQyMDg0LCJub25jZSI6bnVsbH0.XXXXXXXXXXXXXX";
            WebHelper.CheckJWT(token, out bool vehicle_location, out bool offline_access);
            Assert.IsFalse(vehicle_location);
            Assert.IsTrue(offline_access);
        }
        [TestMethod]
        public void TestJWT2()
        {
            string token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InFEc3NoM2FTV0cyT05YTTdLMzFWV0VVRW5BNCJ9.eyJpc3MiOiJodHRwczovL2F1dGgudGVzbGEuY29tL29hdXRoMi92My9udHMiLCJhenAiOiI5MWQ0ZGEyYTM0NjgtNGI4MS1hZGJhLWQyYjI3MWJlZGZhMiIsInN1YiI6IjQ0ZjBjYTA5LTk0OGQtNDgxNy1hMWQ4LWMzNjM0YWY3MmNhYiIsImF1ZCI6WyJodHRwczovL2ZsZWV0LWFwaS5wcmQubmEudm4uY2xvdWQudGVzbGEuY29tIiwiaHR0cHM6Ly9mbGVldC1hcGkucHJkLmV1LnZuLmNsb3VkLnRlc2xhLmNvbSIsImh0dHBzOi8vYXV0aC50ZXNsYS5jb20vb2F1dGgyL3YzL3VzZXJpbmZvIl0sInNjcCI6WyJ1c2VyX2RhdGEiLCJ2ZWhpY2xlX2RldmljZV9kYXRhIiwidmVoaWNsZV9sb2NhdGlvbiIsInZlaGljbGVfY21kcyIsInZlaGljbGVfY2hhcmdpbmdfY21kcyIsIm9mZmxpbmVfYWNjZXNzIiwib3BlbmlkIl0sImFtciI6WyJwd2QiXSwiZXhwIjoxNzM4MDgxMzIzLCJpYXQiOjE3MzgwNTI1MjMsIm91X2NvZGUiOiJFVSIsImxvY2FsZSI6ImRlLURFIiwiYWNjb3VudF90eXBlIjoiYnVzaW5lc3MiLCJvcGVuX3NvdXJjZSI6bnVsbCwiYWNjb3VudF9pZCI6IjRmY2UzNzljLTJlNDUtNGNmNC05MGJjLTQ2YTMxOTE2ZGQxNCIsImF1dGhfdGltZSI6MTczODA1MjUyMywibm9uY2UiOm51bGx9.XXXXXXXXXX";
            WebHelper.CheckJWT(token, out bool vehicle_location, out bool offline_access);
            Assert.IsTrue(vehicle_location);
            Assert.IsTrue(offline_access);
        }
    }
}
