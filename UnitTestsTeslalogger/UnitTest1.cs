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

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestJapanese()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);

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
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 515, DateTime.Now.AddMinutes(1));
            wh.car.CarType = "model3";
            wh.car.DBWhTR = 0.145;
            wh.car.TrimBadging = "";
            wh.car.Vin = "5YJ3E7EA9KFxxxxxx";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR RWD", wh.car.ModelName);
            Assert.AreEqual(0.145, wh.car.WhTR);
        }

        [TestMethod]
        public void Car_M3_LR_RWD2()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 535, DateTime.Now.AddMinutes(1));
            wh.car.Vin = "5YJ3E7EA9KFxxxxxx";
            wh.car.CarType = "model3";
            wh.car.CarSpecialType = "base";
            wh.car.DBWhTR = 0.145;
            wh.car.TrimBadging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR RWD", wh.car.ModelName);
            Assert.AreEqual(0.145, wh.car.WhTR);
        }

        [TestMethod]
        public void Car_M3_LR_P_2019()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_M3_LR_P_2021()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_M3_LR_P_2021_MIC()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_M3_LR_P()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_M3_SRPlus_LFP()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_M3_SRPlus_LFP_2021()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_M3_SRPlus_LFP_2021_60kWh()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_M3_SRPlus_LFP_2021_without_charging()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_M3_SRPlus_2021()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_S_75D()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_S_Raven_LR_P()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }


        [TestMethod]
        public void Car_X_100D()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        public void Car_X_2021_Plaid()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null); 
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.CarType = "tamarind";
            wh.car.CarSpecialType = "base";
            wh.car.TrimBadging = "p100d";
            wh.car.Vin = "7SAXCBE62NFxxxxxx";
            wh.UpdateEfficiency();

            Assert.AreEqual("X 2021 Plaid", wh.car.ModelName);
            Assert.AreEqual(0.149, wh.car.WhTR);
        }

        [TestMethod]
        public void Car_Y_LR_AWD_US()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_Y_LR_AWD_MIC_2022()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
        }

        [TestMethod]
        public void Car_YP_MIG_First30()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
            Assert.AreEqual(a.name, "EnBW DE-Ulm");
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
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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

        [TestMethod]
        public void OpenWBMeterLP1Param()
        {
            var v = new ElectricityMeterOpenWB("http://openwb", "LP1");
            Assert.AreEqual(1, v.LP);
            string ret = v.ToString();
            Console.WriteLine(ret);
        }

        [TestMethod]
        public void OpenWBMeterLP2Param()
        {
            var v = new ElectricityMeterOpenWB("http://openwb", "LP2");
            Assert.AreEqual(2, v.LP);
            string ret = v.ToString();
            Console.WriteLine(ret);
        }

        [TestMethod]
        public void OpenWBMeterNoParam()
        {
            var v = new ElectricityMeterOpenWB("http://openwb", "");
            Assert.AreEqual(1, v.LP);
            string ret = v.ToString();
            Console.WriteLine(ret);
        }

        [TestMethod]
        public void OpenWBMeterConstructor()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "ffffffff", null);
            c.CarInDB = 1;

            var v = ElectricityMeterBase.Instance(c);
            var ret = v.ToString();
            Console.WriteLine(ret);
        }

        [TestMethod]
        public void GoEMeter()
        {
            string url = Settings.Default.ElectricityMeterGoEURL;

            if (string.IsNullOrEmpty(url))
                Assert.Inconclusive("No Settings for Go-E Charger");

            var v = new ElectricityMeterGoE(url, "");
            string ret = v.ToString();
            Console.WriteLine(ret);
        }

        [TestMethod]
        public void Shelly3EM()
        {
            var v = new ElectricityMeterShelly3EM("", "");
            v.mockup_status = "{\"wifi_sta\":{\"connected\":true,\"ssid\":\"badhome\",\"ip\":\"192.168.70.176\",\"rssi\":-78},\"cloud\":{\"enabled\":false,\"connected\":false},\"mqtt\":{\"connected\":false},\"time\":\"16:19\",\"unixtime\":1636125569,\"serial\":7665,\"has_update\":false,\"mac\":\"C45BBE5F71E5\",\"cfg_changed_cnt\":1,\"actions_stats\":{\"skipped\":0},\"relays\":[{\"ison\":false,\"has_timer\":false,\"timer_started\":0,\"timer_duration\":0,\"timer_remaining\":0,\"overpower\":false,\"is_valid\":true,\"source\":\"input\"}],\"emeters\":[{\"power\":0.00,\"pf\":0.14,\"current\":0.01,\"voltage\":236.00,\"is_valid\":true,\"total\":23870.9,\"total_returned\":28.7},{\"power\":0.00,\"pf\":0.00,\"current\":0.01,\"voltage\":236.07,\"is_valid\":true,\"total\":22102.0,\"total_returned\":59.4},{\"power\":7.49,\"pf\":0.49,\"current\":0.07,\"voltage\":235.88,\"is_valid\":true,\"total\":55527.2,\"total_returned\":0.0}],\"total_power\":7.49,\"fs_mounted\":true,\"update\":{\"status\":\"idle\",\"has_update\":false,\"new_version\":\"20210909-150410/v1.11.4-DNSfix-ge6b2f6d\",\"old_version\":\"20210909-150410/v1.11.4-DNSfix-ge6b2f6d\"},\"ram_total\":49440,\"ram_free\":30260,\"fs_size\":233681,\"fs_free\":156624,\"uptime\":1141576}";
            v.mockup_shelly = "{\"type\":\"SHEM-3\",\"mac\":\"C45BBE5F71E5\",\"auth\":false,\"fw\":\"20210909-150410/v1.11.4-DNSfix-ge6b2f6d\",\"longid\":1,\"num_outputs\":1,\"num_meters\":0,\"num_emeters\":3,\"report_period\":1}";

            double? kwh = v.GetVehicleMeterReading_kWh();
            var chargign = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(101.5001, kwh);
            Assert.AreEqual(false, chargign);
            Assert.AreEqual(null, utility_meter_kwh);
            Assert.AreEqual("20210909-150410/v1.11.4-DNSfix-ge6b2f6d", version);
        }

        [TestMethod]
        public void TeslaGen3WCMeterNotCharging()
        {
            var v = new ElectricityMeterTeslaGen3WallConnector("", "");
            v.mockup_lifetime = "{\"contactor_cycles\":106,\"contactor_cycles_loaded\":0,\"alert_count\":3,\"thermal_foldbacks\":0,\"avg_startup_temp\":nan,\"charge_starts\":106,\"energy_wh\":750685,\"connector_cycles\":58,\"uptime_s\":11117950,\"charging_time_s\":355626}";
            v.mockup_vitals = "{\"contactor_closed\":false,\"vehicle_connected\":false,\"session_s\":0,\"grid_v\":231.9,\"grid_hz\":50.071,\"vehicle_current_a\":0.2,\"currentA_a\":0.2,\"currentB_a\":0.1,\"currentC_a\":0.0,\"currentN_a\":0.1,\"voltageA_v\":0.0,\"voltageB_v\":0.0,\"voltageC_v\":0.0,\"relay_coil_v\":11.8,\"pcba_temp_c\":17.9,\"handle_temp_c\":14.8,\"mcu_temp_c\":26.3,\"uptime_s\":784583,\"input_thermopile_uv\":-195,\"prox_v\":0.0,\"pilot_high_v\":11.9,\"pilot_low_v\":11.9,\"session_energy_wh\":2314.100,\"config_status\":5,\"evse_state\":1,\"current_alerts\":[]}";
            v.mockup_version = "{\"firmware_version\":\"21.8.5+g51eba2369815d7\",\"part_number\":\"1529455-02-D\",\"serial_number\":\"PGT12345678912\"}";
            double? kwh = v.GetVehicleMeterReading_kWh();
            var chargign = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(750.685, kwh);
            Assert.AreEqual(false, chargign);
            Assert.AreEqual(null, utility_meter_kwh);
            Assert.AreEqual("21.8.5+g51eba2369815d7", version);
        }

        [TestMethod]
        public void TeslaGen3WCMeterCharging()
        {
            var v = new ElectricityMeterTeslaGen3WallConnector("", "");
            v.mockup_lifetime = "{\"contactor_cycles\":107,\"contactor_cycles_loaded\":0,\"alert_count\":3,\"thermal_foldbacks\":0,\"avg_startup_temp\":nan,\"charge_starts\":107,\"energy_wh\":751369,\"connector_cycles\":59,\"uptime_s\":11130209,\"charging_time_s\":356356}";
            v.mockup_vitals = "{\"contactor_closed\":true,\"vehicle_connected\":true,\"session_s\":545,\"grid_v\":228.3,\"grid_hz\":50.130,\"vehicle_current_a\":5.1,\"currentA_a\":5.1,\"currentB_a\":5.1,\"currentC_a\":5.1,\"currentN_a\":0.0,\"voltageA_v\":230.3,\"voltageB_v\":230.3,\"voltageC_v\":228.7,\"relay_coil_v\":6.1,\"pcba_temp_c\":22.7,\"handle_temp_c\":16.6,\"mcu_temp_c\":28.7,\"uptime_s\":733178,\"input_thermopile_uv\":-516,\"prox_v\":1.9,\"pilot_high_v\":4.6,\"pilot_low_v\":4.6,\"session_energy_wh\":506.800,\"config_status\":5,\"evse_state\":11,\"current_alerts\":[]}";
            v.mockup_version = "{\"firmware_version\":\"21.8.5+g51eba2369815d7\",\"part_number\":\"1529455-02-D\",\"serial_number\":\"PGT12345678912\"}";
            double? kwh = v.GetVehicleMeterReading_kWh();
            var chargign = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();

            Console.WriteLine(ret);

            Assert.AreEqual(751.369, kwh);
            Assert.AreEqual(true, chargign);
            Assert.AreEqual(null, utility_meter_kwh);
            Assert.AreEqual("21.8.5+g51eba2369815d7", version);
        }

        [TestMethod]
        public void CheckJsonString()
        {
            string resultContent = System.IO.File.ReadAllText("../../CheckJsonString.txt");
            object jsonResult = JsonConvert.DeserializeObject(resultContent);
            Assert.IsNotNull(jsonResult);
        }

        [TestMethod]
        public void CheckExportColumn()
        {
            ShareData.UpdateDataTable("chargingstate");
        }

        [TestMethod]
        public void GeocacheBasic()
        {
            GeocodeCache.Instance.ClearCache();
            GeocodeCache.Instance.Insert(10, 20, "Test");
            string temp = GeocodeCache.Instance.Search(10, 20);
            Assert.AreEqual("Test", temp);
            temp = GeocodeCache.Instance.Search(11, 20);
            Assert.IsNull(temp);

            GeocodeCache.Instance.Insert(10, 20, "Test");
            temp = GeocodeCache.Instance.Search(10, 20);
            Assert.AreEqual("Test", temp);

            GeocodeCache.Instance.ClearCache();
        }

        [TestMethod]
        public void TeslaApiVehicles()
        {
            var json = System.IO.File.ReadAllText("../../TeslaApiVehicles.txt");
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            var t = new TeslaAPIState(c);
            t.ParseAPI(json, "vehicles");
            
            t.GetString("vin", out string vin);
            Assert.AreEqual("5YJSA7E21JF123456", vin);

            t.GetString("state", out string state);
            Assert.AreEqual("asleep", state);

            t.GetBool("in_service", out bool in_service);
            Assert.AreEqual(false, in_service);

            t.GetString("id", out string id);
            Assert.AreEqual("1492912313499558", id);

            t.GetString("vehicle_id", out string vehicle_id);
            Assert.AreEqual("162542655", vehicle_id); 

            t.GetString("display_name", out string display_name);
            Assert.AreEqual("Two weeks", display_name);

        }

        [TestMethod]
        public void TeslaApiVehiclesEmpty()
        {
            var json = "{\"response\":[],\"Count\":0}";
            Car c = new Car(1, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            var t = new TeslaAPIState(c);
            var ret = t.ParseAPI(json, "vehicles", 0);

            Assert.IsFalse(ret);
        }

        [TestMethod]
        public void TeslaApiUpdateAvailable()
        {
            var json = System.IO.File.ReadAllText("../../vehicle_state_with_update_available.txt");
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
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
    }
}
