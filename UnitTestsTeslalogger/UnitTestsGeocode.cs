using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
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
        static Car c = null;
        Geofence geofence;

        [TestInitialize]
        public void TestInitialize()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            GeocodeCache.useGeocodeCache = false;

            var geofence = Geofence.GetInstance();

            if (c == null)
                c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false); 

            geofence = Geofence.GetInstance();
            geofence.geofenceList.Clear();
            geofence.geofencePrivateList.Clear();
            // xx GeocodeCache.ClearCache();

            ApplicationSettings.Default.Reload();
            var k = ApplicationSettings.Default.MapQuestKey;
            ApplicationSettings.Default.PropertyValues["MapQuestKey"].PropertyValue = "";

            k = ApplicationSettings.Default.MapQuestKey;
        }

        [TestMethod]
        public void UlmBeimTuermle()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 48.4053267, 9.9547932, true).Result;
            Assert.AreEqual("89075 Ulm, Beim Türmle 23", temp);
            Assert.AreEqual("de", c.CurrentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void SulzbacherStr()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 48.96092, 9.43113, true).Result;
            Assert.AreEqual("71522 Backnang, Sulzbacher Straße ", temp);
            Assert.AreEqual("de", c.CurrentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", c.CurrentJSON.current_state);
        }


        [TestMethod]
        public void NewJerseyNorthBergen()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 40.773667, -74.039867, true).Result;
            Assert.AreEqual("us-07047 North Bergen, Jane Street ", temp);
            Assert.AreEqual("us", c.CurrentJSON.current_country_code);
            Assert.AreEqual("New Jersey", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void UlmBeringerbruecke()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 48.400892, 9.970095, true).Result;
            Assert.AreEqual("89077 Ulm, Blaubeurer Straße ", temp);
            Assert.AreEqual("de", c.CurrentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void Japan()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 35.677121, 139.751033, true).Result;
            Assert.AreEqual("jp-100-0013 千代田区, 内堀通り ", temp);
            Assert.AreEqual("jp", c.CurrentJSON.current_country_code);
            Assert.AreEqual("", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void MietingenMehrzweckhalle()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 48.1850756, 9.9016996, true).Result;
            Assert.AreEqual("88487 Walpertshofen, Tulpenweg 20", temp); // should be "88487 Mietingen, Tulpenweg 20" but nominatim doesn't provide Mietingen as village
            Assert.AreEqual("de", c.CurrentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void ApothekeWiblingen()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 48.360601, 9.984227, true).Result;
            Assert.AreEqual("89079 Ulm, Donautalstraße 46", temp); 
            Assert.AreEqual("de", c.CurrentJSON.current_country_code);
            Assert.AreEqual("Baden-Württemberg", c.CurrentJSON.current_state);
        }

        [TestMethod]
        public void Lat0Lng0()
        {
            string temp = WebHelper.ReverseGecocodingAsync(c, 0, 0, true).Result;
            Assert.AreEqual("", temp);
        }

        [TestMethod]
        public void ParseGeocodeFile()
        {
            var filename = "../../../TeslaLogger/bin/geofence.csv";
            String line;
            using (StreamReader file = new StreamReader(filename))
            {
                while ((line = file.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var args = line.Split(',');
                    Assert.IsNotNull(args);

                    // System.Diagnostics.Debug.WriteLine(line);

                    if (args.Length < 3) 
                        Assert.Fail("Expected format: name, lat, lng, radius: " + line);
                    else if (args.Length > 4)
                        Assert.Fail("Expected format: name, lat, lng, radius: " + line);

                    try
                    {
                        double.Parse(args[1], Tools.ciEnUS.NumberFormat);
                        double lng = double.Parse(args[2], Tools.ciEnUS.NumberFormat);
                    } catch (Exception){ 
                        Assert.Fail("Can't parse coordinate: " + line); 
                    }


                    if (args.Length == 4)
                    {
                        if (!int.TryParse(args[3], out int radius))
                            Assert.Fail("Can't parse radius: " + line);
                    }

                    string name = args[0];
                    if (name.Contains("\""))
                        Assert.Fail($"'${name}' contains illegal characters: \"");

                    if (name.IndexOf("supercharger", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!name.StartsWith("Supercharger"))
                            Assert.Fail($"'${name}' must start with Supercharger");

                        var s = name.Split(' ');
                        if (s[0] == "Supercharger-V3" || s[0] == "Supercharger-V4" || s[0] == "Supercharger")
                        {
                            CheckCountry(s[1], name);
                        }
                        else
                        {
                            Assert.Fail("Supercharger must start with 'Supercharger', 'Supercherger-V3' or 'Supercharger-V4' : " + name);
                        }
                    }
                    else if (name.StartsWith("Tesla Service Center"))
                    {
                        CheckCountry(name.Substring(21), name);
                    }
                    else if (name.StartsWith("Circle K"))
                    {
                        CheckCountry(name.Substring(9), name);
                    }
                    else if (name.StartsWith("Grønn Kontakt"))
                    {
                        CheckCountry(name.Substring(14), name);
                    }
                    else if (name.Substring(2,1) == " ")
                    {
                        // Unspecific Charger starting with country code
                    }
                    else
                    {
                        CheckCountry(name.Substring(name.IndexOf(" ")+1), name);
                    }
                }
            }
        }

        private static void CheckCountry(string name, string fullname)
        {
            var l = name.Split('-');
            if (l[0].Length != 2)
                Assert.Fail($"Country ({l}) should be 2 chars: " + fullname);
        }
    }
}
