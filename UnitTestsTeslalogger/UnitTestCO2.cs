using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
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
            Assert.AreEqual(486, c);

            c = co2.GetData("ro", dateTime);
            Assert.AreEqual(320, c);

            c = co2.GetData("pt", dateTime);
            Assert.AreEqual(128, c);

            c = co2.GetData("es", dateTime);
            Assert.AreEqual(140, c);

            c = co2.GetData("be", dateTime);
            Assert.AreEqual(147, c);

            c = co2.GetData("dk", dateTime);
            Assert.AreEqual(209, c);

            c = co2.GetData("hr", dateTime);
            Assert.AreEqual(340, c);

        }

        [TestMethod]
        public void TestGetDataDB()
        {
            ServicePointManager.ServerCertificateValidationCallback += (p1, p2, p3, p4) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            var dt = DBHelper.GetAllChargingstates();

            dt.Columns.Add("co2_kwh", typeof(int));
            dt.Columns.Add("country");

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
    }
}
