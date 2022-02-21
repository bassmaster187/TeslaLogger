using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestsGeocode
    {
        Car c = null;
        Geofence geofence;

        [TestInitialize]
        public void TestInitialize()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var geofence = Geofence.GetInstance();

            if (c == null)
                c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null);

            geofence = Geofence.GetInstance();
            geofence.geofenceList.Clear();
            geofence.geofencePrivateList.Clear();
            GeocodeCache.Instance.ClearCache();
        }

        [TestMethod]
        public void UlmBeimTuermle()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 48.4053267, 9.9547932).Result;
            Assert.AreEqual("89075 Ulm, Beim Türmle 23", temp);
            Assert.AreEqual("de", c.CurrentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void SulzbacherStr()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 48.96092, 9.43113).Result;
            Assert.AreEqual("71522 Backnang, Sulzbacher Straße 176", temp);
            Assert.AreEqual("de", c.CurrentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", c.CurrentJSON.current_state);
        }


        [TestMethod]
        public void NewJerseyNorthBergen()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 40.773667, -74.039867).Result;
            Assert.AreEqual("us-07047 North Bergen, Jane Street ", temp);
            Assert.AreEqual("us", c.CurrentJSON.current_country_code);
            Assert.AreEqual("New Jersey", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void UlmBeringerbruecke()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 48.400892, 9.970095).Result;
            Assert.AreEqual("89077 Ulm, Beringerbrücke ", temp);
            Assert.AreEqual("de", c.CurrentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void Japan()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 35.677121, 139.751033).Result;
            Assert.AreEqual("jp-100-0013 千代田区, 内堀通り ", temp);
            Assert.AreEqual("jp", c.CurrentJSON.current_country_code);
            Assert.AreEqual("", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void MietingenMehrzweckhalle()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 48.1850756, 9.9016996).Result;
            Assert.AreEqual("88487 Walpertshofen, Tulpenweg 20", temp); // should be "88487 Mietingen, Tulpenweg 20" but nominatim doesn't provide Mietingen as village
            Assert.AreEqual("de", c.CurrentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void ApothekeWiblingen()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 48.360601, 9.984227).Result;
            Assert.AreEqual("89079 Ulm, Donautalstraße 46", temp); 
            Assert.AreEqual("de", c.CurrentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void Lat0Lng0()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 0, 0).Result;
            Assert.AreEqual("- ,  ", temp);
        }
    }
}
