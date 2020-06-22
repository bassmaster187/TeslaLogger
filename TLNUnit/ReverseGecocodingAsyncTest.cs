using System.Collections.Generic;
using NUnit.Framework;

namespace TeslaLogger
{
    [TestFixture()]
    public class ReverseGecocodingAsyncTest
    {
        [Test()]
        public void Instantiate()
        {
            GeocodeCache.Instance.ClearCache();
            Assert.IsNotNull(GeocodeCache.Instance);
        }

        [Test()]
        public void ReverseGecocodingAsyncTest1()
        {
            string temp = WebHelper.ReverseGecocodingAsync(35.677121, 139.751033).Result;
            Assert.AreEqual("jp-100-0013 千代田区, 内堀通り ", temp);
            Assert.AreEqual("jp", DBHelper.currentJSON.current_country_code);
            Assert.AreEqual("", DBHelper.currentJSON.current_state);
        }

        [Test()]
        public void ReverseGecocodingAsyncTest2()
        {
            string temp = WebHelper.ReverseGecocodingAsync(48.400892, 9.970095).Result;
            Assert.AreEqual("89077 Ulm, Beringerbrücke ", temp);
            Assert.AreEqual("de", DBHelper.currentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", DBHelper.currentJSON.current_state);
        }

        [Test()]
        public void ReverseGecocodingAsyncTest3()
        {
            string temp = WebHelper.ReverseGecocodingAsync(40.773667, -74.039867).Result;
            Assert.AreEqual("us-07047 , Jane Street ", temp);
            Assert.AreEqual("us", DBHelper.currentJSON.current_country_code);
            Assert.AreEqual("New Jersey", DBHelper.currentJSON.current_state);
        }
    }
}
