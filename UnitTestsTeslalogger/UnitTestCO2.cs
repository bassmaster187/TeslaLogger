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
        static DateTime dateTime;
        static CO2 co2;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            ServicePointManager.ServerCertificateValidationCallback += (p1, p2, p3, p4) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            dateTime = new DateTime(2022, 12, 21, 22, 00, 00);
            co2 = new CO2();
            co2.useCache = false;
        }
        private const string AddressCountryCacheFilename = "AddressCountryCache.json";

        [TestMethod]
        public void TestDEToday()
        {
            int c = co2.GetData("de", DateTime.Now.AddHours(-4));
            Assert.IsTrue(c > 100);
        }

        [TestMethod]
        public void TestFRToday()
        {
            int c = co2.GetData("fr", DateTime.Now.AddHours(-4));
            Assert.IsTrue(c < 200);
        }


        [TestMethod]
        public void TestDE()
        {
            int c = co2.GetData("de", dateTime);
            Assert.AreEqual(423, c, 10);
        }

        [TestMethod]
        public void TestDE_KW53()
        {
            int c = co2.GetData("de", new DateTime(2024,12,30,12,10,0));
            Assert.AreEqual(91, c, 10);
        }

        [TestMethod]
        public void TestFR()
        {
            int c = co2.GetData("fr", dateTime);
            Assert.AreEqual(67, c, 10);
        }

        [TestMethod]
        public void TestCH()
        {
            int c = co2.GetData("ch", dateTime);
            Assert.AreEqual(153, c, 20);
        }

        [TestMethod]
        public void TestAT()
        {
            int c = co2.GetData("at", dateTime);
            Assert.AreEqual(414, c, 20);
        }

        [TestMethod]
        public void TestIT()
        {
            int c = co2.GetData("it", dateTime);
            Assert.AreEqual(500, c, 10);
        }

        [TestMethod]
        public void TestRO()
        {
            int c = co2.GetData("ro", dateTime);
            Assert.AreEqual(320, c, 10);
        }

        [TestMethod]
        public void TestPT()
        {
            int c = co2.GetData("pt", dateTime);
            Assert.AreEqual(125, c, 20);
        }

        [TestMethod]
        public void TestES()
        {
            int c = co2.GetData("es", dateTime);
            Assert.AreEqual(140, c, 30);
        }

        [TestMethod]
        public void TestBE()
        {
            int c = co2.GetData("be", dateTime);
            Assert.AreEqual(201, c, 20);
        }
        [TestMethod]
        public void TestDK()
        {
            int c = co2.GetData("dk", dateTime);
            Assert.AreEqual(209, c, 10);
        }
        [TestMethod]
        public void TestHR()
        {
            int c = co2.GetData("hr", dateTime);
            Assert.AreEqual(336, c, 35);
        }
        [TestMethod]
        public void TestCZ()
        {
            int c = co2.GetData("cz", dateTime);
            Assert.AreEqual(620, c, 10);
        }
        [TestMethod]
        public void TestHU()
        {
            int c = co2.GetData("hu", dateTime);
            Assert.AreEqual(317, c, 10);
        }
        [TestMethod]
        public void TestNL()
        {
            int c = co2.GetData("nl", dateTime);
            Assert.AreEqual(512, c, 10);
        }
        [TestMethod]
        public void TestNO()
        {
            int c = co2.GetData("no", dateTime);
            Assert.AreEqual(47, c, 20);
        }
        [TestMethod]
        public void TestSI()
        {
            int c = co2.GetData("si", dateTime);
            Assert.AreEqual(291, c, 10);
        }
        [TestMethod]
        public void TestSE()
        {
            int c = co2.GetData("se", dateTime);
            Assert.AreEqual(69, c, 10);
        }
        [TestMethod]
        public void TestGB()
        {
            int c = co2.GetData("gb", dateTime);
            Assert.AreEqual(161, c, 10);
        }
        [TestMethod]
        public void TestUK()
        {
            int c = co2.GetData("uk", dateTime);
            Assert.AreEqual(161, c, 10);
        }
        [TestMethod]
        public void TestPL()
        {
            int c = co2.GetData("pl", dateTime);
            Assert.AreEqual(719, c, 20);
        }

        [TestMethod]
        public void TestFI()
        {
            int c = co2.GetData("fi", dateTime);
            Assert.AreEqual(171, c, 10);
        }

        [TestMethod]
        public void TestSK()
        {
            int c = co2.GetData("sk", dateTime);
            Assert.AreEqual(363, c, 20);
        }

        [TestMethod]
        public void TestBG()
        {
            int c = co2.GetData("bg", dateTime);
            Assert.AreEqual(641, c, 10);
        }

        [TestMethod]
        public void TestEE()
        {
            int c = co2.GetData("ee", dateTime);
            Assert.AreEqual(767, c, 130);
        }

        [TestMethod]
        public void TestLV()
        {
            int c = co2.GetData("lv", dateTime);
            Assert.AreEqual(490, c, 55);
        }

        [TestMethod]
        public void TestGR()
        {
            int c = co2.GetData("gr", dateTime);
            Assert.AreEqual(571, c, 20);
        }


        // [TestMethod]
        public void GetMonthExport()
        {
            var dt = new DateTime(2023, 07, 01);
            var dtEnde = dt.AddMonths(1);
            CO2 co2 = new CO2();

            decimal sumEur = 0;
            decimal sumMWh = 0;


            while (dt < dtEnde)
            {
                int c = co2.GetData("de", dt);
                dt = dt.AddMinutes(15);
                if (co2.CrossBorderElectricityTrading > 0)
                    continue;

                co2.CrossBorderElectricityTrading = co2.CrossBorderElectricityTrading * -1 / 4;

                var eur = co2.CrossBorderElectricityTrading * co2.DayAheadAuction;
                sumEur += (decimal)eur;
                sumMWh += (decimal)co2.CrossBorderElectricityTrading;

                System.Diagnostics.Debug.WriteLine(dt.ToString() + ";" + co2.CrossBorderElectricityTrading.ToString(Tools.ciDeDE) + ";" + co2.DayAheadAuction.ToString(Tools.ciDeDE) + ";" + eur.ToString(Tools.ciDeDE));
            }

            var avgEur = sumEur / sumMWh;

            System.Diagnostics.Debug.WriteLine($"Sum Eur: {sumEur}");
            System.Diagnostics.Debug.WriteLine($"Sum Eur: {sumEur / 1000000} Mio€");
            System.Diagnostics.Debug.WriteLine($"Sum MWh: {sumMWh}");
            System.Diagnostics.Debug.WriteLine($"Sum GWh: {sumMWh / 1000}");
            System.Diagnostics.Debug.WriteLine($"Avg Eur: {avgEur}");
        }
        
        /*
        [TestMethod]
        public void TestGetDataDB()
        {
            ServicePointManager.ServerCertificateValidationCallback += (p1, p2, p3, p4) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            DataTable dt = null;

            try
            {
                dt = DBHelper.GetAllChargingstates();
            }
            catch (Exception ex)
            {
                if (ex.HResult == -2147467259) // unable to connect to any of the specified MySQL hosts
                    Assert.Inconclusive(ex.Message);
            }

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
        */

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
