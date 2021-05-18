using System;
using TeslaLogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.Caching;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethodGeocode()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var geofence = new TeslaLogger.Geofence(false);
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

            GeocodeCache.Instance.ClearCache();
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);

            string temp = WebHelper.ReverseGecocodingAsync(c, 35.677121, 139.751033).Result;
            Assert.AreEqual("jp-100-0013 千代田区, 内堀通り ", temp);
            Assert.AreEqual("jp", c.currentJSON.current_country_code);
            Assert.AreEqual("", c.currentJSON.current_state);

            temp = WebHelper.ReverseGecocodingAsync(c, 48.400892, 9.970095).Result;
            Assert.AreEqual("89077 Ulm, Beringerbrücke ", temp);
            Assert.AreEqual("de", c.currentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", c.currentJSON.current_state);

            temp = WebHelper.ReverseGecocodingAsync(c, 40.773667, -74.039867).Result;
            Assert.AreEqual("us-07047 North Bergen, Jane Street ", temp);
            Assert.AreEqual("us", c.currentJSON.current_country_code);
            Assert.AreEqual("New Jersey", c.currentJSON.current_state);
            
        }
        [TestMethod]
        public void TestJapanese()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);

            Tools.SetThread_enUS();
            long unixTimestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            unixTimestamp *= 1000;
            c.dbHelper.InsertPos(unixTimestamp.ToString(), 48.456691, 10.030241, 0, 0, 1, 0, 0, 0, 0, "0");
            int startid = c.dbHelper.GetMaxPosid(true);
            c.dbHelper.StartDriveState();

            c.dbHelper.InsertPos(unixTimestamp.ToString(), 35.677121, 139.751033, 0, 0, 2, 0, 0, 0, 0, "0");
            int endid = c.dbHelper.GetMaxPosid(true);
            c.dbHelper.CloseDriveState(DateTime.Now);
        }


        [TestMethod]
        public void Car_S85D()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.car_type = "models";
            wh.car.car_special_type = "base";
            wh.car.trim_badging = "85d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 85D", wh.car.ModelName);
            Assert.AreEqual(0.186, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_S_P85()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.car_type = "models";
            wh.car.car_special_type = "signature";
            wh.car.trim_badging = "p85";
            wh.UpdateEfficiency();

            Assert.AreEqual("S P85", wh.car.ModelName);
            Assert.AreEqual(0.201, wh.car.Wh_TR);
        }


        [TestMethod]
        public void Car_M3_LR_RWD()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 515, DateTime.Now.AddMinutes(1));
            wh.car.car_type = "model3";
            wh.car.DB_Wh_TR = 0.145;
            wh.car.trim_badging = "";
            wh.car.vin = "5YJ3E7EA9KFxxxxxx";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR RWD", wh.car.ModelName);
            Assert.AreEqual(0.145, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_M3_LR_RWD2()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 535, DateTime.Now.AddMinutes(1));
            wh.car.vin = "5YJ3E7EA9KFxxxxxx";
            wh.car.car_type = "model3";
            wh.car.car_special_type = "base";
            wh.car.DB_Wh_TR = 0.145;
            wh.car.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR RWD", wh.car.ModelName);
            Assert.AreEqual(0.145, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_M3_LR_P_2021()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            //2021 Model 3 LR Performance
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 505, DateTime.Now.AddMinutes(1));
            wh.car.vin = "5YJ3E7EC2MFXXXXXX";
            wh.car.car_type = "model3";
            wh.car.car_special_type = "base";
            wh.car.DB_Wh_TR = 0.158;
            wh.car.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR P 2021", wh.car.ModelName);
            Assert.AreEqual(0.158, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_M3_LR_P()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 491, DateTime.Now.AddMinutes(1));
            wh.car.vin = "5YJ3F7EC1LFXXXXXX";
            wh.car.car_type = "model3";
            wh.car.car_special_type = "base";
            wh.car.DB_Wh_TR = 0.158;
            wh.car.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 LR P", wh.car.ModelName);
            Assert.AreEqual(0.158, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_M3_SRPlus_LFP()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 407, DateTime.Now.AddMinutes(1));
            wh.car.vin = "LRW3E7FA9LCxxxxxx";
            wh.car.car_type = "model3";
            wh.car.car_special_type = "base";
            wh.car.DB_Wh_TR = 0.133;
            wh.car.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 SR+ LFP", wh.car.ModelName);
            Assert.AreEqual(0.133, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_M3_SRPlus_2021()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            //2021 Model 3 SR+
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 407, DateTime.Now.AddMinutes(1));
            wh.car.vin = "5YJ3E7EA3MFXXXXXX";
            wh.car.car_type = ""; // sometimes empty !!!
            wh.car.car_special_type = "base";
            wh.car.DB_Wh_TR = 0.126;
            wh.car.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("M3 SR+ 2021", wh.car.ModelName);
            Assert.AreEqual(0.126, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_S_75D()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;
            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.car_type = "models2";
            wh.car.car_special_type = "base";
            wh.car.DB_Wh_TR = 0.145;
            wh.car.trim_badging = "75d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 75D", wh.car.ModelName);
            Assert.AreEqual(0.186, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_S_90D()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.car_type = "models2";
            wh.car.car_special_type = "base";
            wh.car.trim_badging = "90d";
            wh.UpdateEfficiency();

            Assert.AreEqual("S 90D", wh.car.ModelName);
            Assert.AreEqual(0.188, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_S_Raven_SR()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 443, DateTime.Now.AddMinutes(1));
            wh.car.car_type = "models2";
            wh.car.car_special_type = "base";
            wh.car.DB_Wh_TR = 0.163;
            wh.car.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("S Raven SR", wh.car.ModelName);
            Assert.AreEqual(0.163, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_S_Raven_LR()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 560, DateTime.Now.AddMinutes(1));
            wh.car.car_type = "models2";
            wh.car.car_special_type = "base";
            wh.car.DB_Wh_TR = 0.169;
            wh.car.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("S Raven LR", wh.car.ModelName);
            Assert.AreEqual(0.173, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_S_Raven_LR_P()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 546, DateTime.Now.AddMinutes(1));
            wh.car.vin = "5YJSA7E43LFXXXXXX";
            wh.car.car_type = "models2";
            wh.car.car_special_type = "base";
            wh.car.DB_Wh_TR = 0.173;
            wh.car.trim_badging = "";
            wh.UpdateEfficiency();

            Assert.AreEqual("S Raven LR P", wh.car.ModelName);
            Assert.AreEqual(0.173, wh.car.Wh_TR);
        }


        [TestMethod]
        public void Car_X_100D()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            wh.car.car_type = "modelx";
            wh.car.car_special_type = "base";
            wh.car.trim_badging = "100d";
            wh.UpdateEfficiency();

            Assert.AreEqual("X 100D", wh.car.ModelName);
            Assert.AreEqual(0.217, wh.car.Wh_TR);
        }

        [TestMethod]
        public void Car_Y_LR_AWD()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);
            WebHelper wh = c.webhelper;

            MemoryCache.Default.Remove("GetAvgMaxRage_0");
            MemoryCache.Default.Add("GetAvgMaxRage_0", 520, DateTime.Now.AddMinutes(1));
            wh.car.car_type = "modely";
            wh.car.car_special_type = "base";
            wh.car.DB_Wh_TR = 0.148;
            wh.car.trim_badging = "74d";
            wh.UpdateEfficiency();

            Assert.AreEqual("Y LR AWD", wh.car.ModelName);
            Assert.AreEqual(0.148, wh.car.Wh_TR);
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
    }
}
