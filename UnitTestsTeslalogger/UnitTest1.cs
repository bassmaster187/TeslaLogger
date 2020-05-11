using System;
using TeslaLogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethodGeocode()
        {
            var geofence = new TeslaLogger.Geofence();
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

            string temp = WebHelper.ReverseGecocodingAsync(35.677121, 139.751033).Result;
            Assert.AreEqual(temp, "警視庁, jp-100-0013 千代田区, 内堀通り ");
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


        public void TestCars()
        {

        }
    }
}
