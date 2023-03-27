using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Exceptionless;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestCO2
    {
        private const string AddressCountryCacheFilename = "AddressCountryCache.json";

        [TestMethod]
        public void TestGetData()
        {
            ServicePointManager.ServerCertificateValidationCallback += (p1, p2, p3, p4) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            DateTime dateTime = new DateTime(2022, 12, 21, 22, 00, 00);

            CO2 co2 = new CO2();
            
            int c = co2.GetData("de", dateTime);
            Assert.AreEqual(422, c);

            c = co2.GetData("fr", dateTime);
            Assert.AreEqual(67, c);

            c = co2.GetData("ch", dateTime);
            Assert.AreEqual(154, c);

            c = co2.GetData("at", dateTime);
            Assert.AreEqual(414, c);

            c = co2.GetData("it", dateTime);
            Assert.AreEqual(500, c);

            c = co2.GetData("ro", dateTime);
            Assert.AreEqual(320, c);

            c = co2.GetData("pt", dateTime);
            Assert.AreEqual(126, c);

            c = co2.GetData("es", dateTime);
            Assert.AreEqual(140, c);

            c = co2.GetData("be", dateTime);
            Assert.AreEqual(147, c);

            c = co2.GetData("dk", dateTime);
            Assert.AreEqual(209, c);

            c = co2.GetData("hr", dateTime);
            Assert.AreEqual(336, c);

            c = co2.GetData("cz", dateTime);
            Assert.AreEqual(620, c);

            c = co2.GetData("hu", dateTime);
            Assert.AreEqual(317, c);

            c = co2.GetData("nl", dateTime);
            Assert.AreEqual(505, c);

            c = co2.GetData("no", dateTime);
            Assert.AreEqual(47, c);

            c = co2.GetData("si", dateTime);
            Assert.AreEqual(291, c);

            c = co2.GetData("se", dateTime);
            Assert.AreEqual(69, c);

            c = co2.GetData("gb", dateTime);
            Assert.AreEqual(161, c);

            c = co2.GetData("uk", dateTime);
            Assert.AreEqual(161, c);

            c = co2.GetData("lu", dateTime);
            Assert.AreEqual(390, c);

            c = co2.GetData("pl", dateTime);
            Assert.AreEqual(719, c);

            c = co2.GetData("fi", dateTime);
            Assert.AreEqual(171, c);

            c = co2.GetData("sk", dateTime);
            Assert.AreEqual(363, c);

            c = co2.GetData("bg", dateTime);
            Assert.AreEqual(641, c);

            c = co2.GetData("ee", dateTime);
            Assert.AreEqual(767, c);

            c = co2.GetData("lv", dateTime);
            Assert.AreEqual(490, c);

            c = co2.GetData("gr", dateTime);
            Assert.AreEqual(583, c);
        }

        [TestMethod]
        public void TestGetDataDB()
        {
            ServicePointManager.ServerCertificateValidationCallback += (p1, p2, p3, p4) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            var dt = DBHelper.GetAllChargingstates();

            dt.Columns.Add("co2_kwh", typeof(int));
            // dt.Columns.Add("country");

            DateTime dateTime = new DateTime(2022, 12, 21, 22, 00, 00);

            Dictionary<string,string> dic = new Dictionary<string,string>();

            if (System.IO.File.Exists(AddressCountryCacheFilename))
            {
                string json = System.IO.File.ReadAllText(AddressCountryCacheFilename);
                dic = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }


            foreach (DataRow dr in dt.Rows)
            {
                try
                {
                    int id = (int)dr["id"];
                    double lat = (double)dr["lat"];
                    double lng = (double)dr["lng"];
                    DateTime date = (DateTime)dr["StartDate"];
                    string address = dr["address"].ToString();

                    string country = "";

                    if (address.Length > 0 && dic.TryGetValue(address, out country))
                    {
                        System.Diagnostics.Debug.WriteLine("address from cache:");
                    }
                    else
                    {
                        country = WebHelper.ReverseGecocodingCountryAsync(lat, lng).Result;

                        if (address.Length > 0 && country.Length > 0)
                            dic.Add(address, country);

                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(dic, Newtonsoft.Json.Formatting.Indented);
                        System.IO.File.WriteAllText(AddressCountryCacheFilename, json);
                    }


                    CO2 co2 = new CO2();
                    int c = co2.GetData(country, date);
                    System.Diagnostics.Debug.WriteLine($"Address: {address} / Country: {country} / Date: {date.ToLongDateString()} /  CO2: " + c);

                    dr["co2_kwh"] = c;
                    dr["country"] = country;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            }

            System.Diagnostics.Debug.WriteLine("Done");
        }

        [TestCleanup()]
        public void Cleanup()
        {
            ExceptionlessClient.Default.ProcessQueueAsync().Wait();
        }

        [AssemblyInitialize()]
        public static void AssemblyInit(TestContext context)
        {
            ExceptionlessClient.Default.Startup(ApplicationSettings.Default.ExceptionlessApiKey);
            ExceptionlessClient.Default.Configuration.UseFileLogger("exceptionless.log");
            ExceptionlessClient.Default.Configuration.ServerUrl = ApplicationSettings.Default.ExceptionlessServerUrl;
            ExceptionlessClient.Default.Configuration.SetVersion(Assembly.GetExecutingAssembly().GetName().Version);
        }
    }
}
