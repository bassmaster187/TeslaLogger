using System;
using TeslaLogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.Caching;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethodGeocode()
        {
            var geofence = new TeslaLogger.Geofence(false);
            var a = geofence.GetPOI(48.456708, 10.029897);
            Assert.AreEqual(a.name, "Supercharger DE-Ulm");

            a = geofence.GetPOI(48.456616, 10.030200);
            Assert.AreEqual(a.name, "Supercharger DE-Ulm");

            a = geofence.GetPOI(48.456790, 10.030014);
            Assert.AreEqual(a.name, "Supercharger DE-Ulm");

            a = geofence.GetPOI(48.456691, 10.030241);
            Assert.AreEqual(a.name, "Supercharger DE-Ulm");

            a = geofence.GetPOI(48.456888, 10.029635);
            Assert.AreEqual(a.name, "EnBW DE-Ulm");

            GeocodeCache.Instance.ClearCache();
            string temp = WebHelper.ReverseGecocodingAsync(35.677121, 139.751033).Result;
            Assert.AreEqual("jp-100-0013 千代田区, 内堀通り ", temp);
            Assert.AreEqual("jp", DBHelper.currentJSON.current_country_code);
            Assert.AreEqual("", DBHelper.currentJSON.current_state);

            temp = WebHelper.ReverseGecocodingAsync(48.400892, 9.970095).Result;
            Assert.AreEqual("89077 Ulm, Beringerbrücke ", temp);
            Assert.AreEqual("de", DBHelper.currentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", DBHelper.currentJSON.current_state);

            temp = WebHelper.ReverseGecocodingAsync(40.773667, -74.039867).Result;
            Assert.AreEqual("us-07047 , Jane Street ", temp);
            Assert.AreEqual("us", DBHelper.currentJSON.current_country_code);
            Assert.AreEqual("New Jersey", DBHelper.currentJSON.current_state);
        }
        [TestMethod]
        public void TestJapanese()
        {
            Tools.SetThread_enUS();
            long unixTimestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            unixTimestamp *= 1000;
            DBHelper.InsertPos(unixTimestamp.ToString(), 48.456691, 10.030241, 0, 0, 1, 0, 0, 0, "0");
            int startid = DBHelper.GetMaxPosid(true);
            DBHelper.StartDriveState();

            DBHelper.InsertPos(unixTimestamp.ToString(), 35.677121, 139.751033, 0, 0, 2, 0, 0, 0, "0");
            int endid = DBHelper.GetMaxPosid(true);
            DBHelper.CloseDriveState(DateTime.Now);
        }

        [TestMethod]
        public void TestCars()
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


            wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            wh.carSettings.car_type = "models2";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.DB_Wh_TR = "0.145";
            wh.carSettings.trim_badging = "75d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 75D", wh.carSettings.Name);
            Assert.AreEqual("0.186", wh.carSettings.Wh_TR);

            wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            wh.carSettings.car_type = "models";
            wh.carSettings.car_special_type = "signature";
            wh.carSettings.trim_badging = "p85";
            wh.UpdateEfficiency();

            Assert.AreEqual("S P85", wh.carSettings.Name);
            Assert.AreEqual("0.201", wh.carSettings.Wh_TR);

            wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            wh.carSettings.car_type = "models";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.trim_badging = "85d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 85D", wh.carSettings.Name);
            Assert.AreEqual("0.186", wh.carSettings.Wh_TR);

            wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            wh.carSettings.car_type = "modelx";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.trim_badging = "100d";
            wh.UpdateEfficiency();

            Assert.AreEqual("X 100D", wh.carSettings.Name);
            Assert.AreEqual("0.217", wh.carSettings.Wh_TR);


            wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            wh.carSettings.car_type = "models2";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.trim_badging = "90d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 90D", wh.carSettings.Name);
            Assert.AreEqual("0.188", wh.carSettings.Wh_TR);


            wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            MemoryCache.Default.Add("GetAvgMaxRage", 443, DateTime.Now.AddMinutes(1));
            wh.carSettings.car_type = "models2";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.DB_Wh_TR = "0.163";
            wh.carSettings.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("S Raven SR", wh.carSettings.Name);
            Assert.AreEqual("0.163", wh.carSettings.Wh_TR);


            wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            MemoryCache.Default.Add("GetAvgMaxRage", 560, DateTime.Now.AddMinutes(1));
            wh.carSettings.car_type = "models2";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.DB_Wh_TR = "0.169";
            wh.carSettings.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("S Raven LR", wh.carSettings.Name);
            Assert.AreEqual("0.169", wh.carSettings.Wh_TR);

            wh = new WebHelper();
            MemoryCache.Default.Remove("GetAvgMaxRage");
            MemoryCache.Default.Add("GetAvgMaxRage", 546, DateTime.Now.AddMinutes(1));
            wh.carSettings.car_type = "models2";
            wh.carSettings.car_special_type = "base";
            wh.carSettings.DB_Wh_TR = "0.178";
            wh.carSettings.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("S Raven LR P", wh.carSettings.Name);
            Assert.AreEqual("0.178", wh.carSettings.Wh_TR);
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

    }
}
