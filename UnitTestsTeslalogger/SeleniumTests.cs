using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Collections.ObjectModel;
using System.Threading;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class SeleniumTests
    {
        [TestMethod]
        public void TestGeofencing()
        {
            String dockerPath = @"C:\Docker\TeslaLogger\TeslaLogger";

            // Backup current geofence-private.csv
            String geofencePrivatePath = Path.Combine(dockerPath, @"TeslaLogger\bin\geofence-private.csv");
            String geofencePrivateBackupPath = Path.Combine(dockerPath, @"TeslaLogger\bin\geofence-private.csv.backup");
            File.Copy(geofencePrivatePath, geofencePrivateBackupPath, true);

            // Copy predefined geofence file
            File.Copy("geofence-private.csv", geofencePrivatePath, true);

            // Items to check
            string[] items = { "Beavers Stadion", "Burger King BC", "Flughafen Stuttgart P0", "Ikea Ulm", "Ladestation Bauhaus Ulm", "Ladestation Norma Ulm" };

            IWebDriver d = new ChromeDriver();
            d.Navigate().GoToUrl("http://localhost:8888/admin/geofencing.php");
            ReadOnlyCollection<IWebElement> rows = CheckGeofenceItems(items, d);

            // Ikea Ulm
            var ikea = rows[3].FindElements(By.TagName("td"));
            Assert.AreEqual(ikea[0].Text, "Ikea Ulm");
            var ikeaLinks = ikea[1].FindElements(By.TagName("a"));

            // click on show
            ikeaLinks[1].Click();

            ikeaLinks[0].Click();

            // compare entries 
            Assert.AreEqual(d.FindElement(By.Id("text")).GetAttribute("value"), "Ikea Ulm");
            Assert.AreEqual(d.FindElement(By.Id("radius")).GetAttribute("value"), "20");
            Assert.AreEqual(d.FindElement(By.Id("home")).GetAttribute("checked"), null);
            Assert.AreEqual(d.FindElement(By.Id("work")).GetAttribute("checked"), null);
            Assert.AreEqual(d.FindElement(By.Id("charger")).GetAttribute("checked"), "true");
            Assert.AreEqual(d.FindElement(By.Id("ccp")).GetAttribute("checked"), "true");

            // modify 
            d.FindElement(By.Id("text")).Clear();
            d.FindElement(By.Id("text")).SendKeys("Ikea Ulm Ladestation");
            d.FindElement(By.Id("radius")).Clear();
            d.FindElement(By.Id("radius")).SendKeys("25");

            // save
            d.FindElement(By.Id("btn_save")).Click();
            waitForAlert(d);
            d.SwitchTo().Alert().Accept();

            // check if table has changed sucessfully 
            items[3] = "Ikea Ulm Ladestation";
            var r = CheckGeofenceItems(items, d);

            // check if items has been changed
            r[3].FindElements(By.TagName("td"))[1].FindElements(By.TagName("a"))[0].Click();
            Assert.AreEqual(d.FindElement(By.Id("text")).GetAttribute("value"), "Ikea Ulm Ladestation");
            Assert.AreEqual(d.FindElement(By.Id("radius")).GetAttribute("value"), "25");
            Assert.AreEqual(d.FindElement(By.Id("home")).GetAttribute("checked"), null);
            Assert.AreEqual(d.FindElement(By.Id("work")).GetAttribute("checked"), null);
            Assert.AreEqual(d.FindElement(By.Id("charger")).GetAttribute("checked"), "true");
            Assert.AreEqual(d.FindElement(By.Id("ccp")).GetAttribute("checked"), "true");

            // Insert new geofence
            d.Navigate().GoToUrl("http://localhost:8888/admin/geoadd.php?lat=48.82705&lng=9.0998");
            d.FindElement(By.Id("text")).Clear();
            d.FindElement(By.Id("text")).SendKeys("Tesla Service Center Stuttgart");
            d.FindElement(By.Id("radius")).Clear();
            d.FindElement(By.Id("radius")).SendKeys("55");

            // save
            d.FindElement(By.Id("btn_save")).Click();
            waitForAlert(d);
            d.SwitchTo().Alert().Accept();

            d.Navigate().GoToUrl("http://localhost:8888/admin/geofencing.php");

            String[] itemsNew = { "Beavers Stadion", "Burger King BC", "Flughafen Stuttgart P0", "Ikea Ulm Ladestation", "Ladestation Bauhaus Ulm", "Ladestation Norma Ulm", "Tesla Service Center Stuttgart" };
            CheckGeofenceItems(itemsNew, d);


        }

        // compare table by items
        private static ReadOnlyCollection<IWebElement> CheckGeofenceItems(string[] items, IWebDriver d)
        {
            var e = d.FindElement(By.Id("locations"));
            var rows = e.FindElements(By.TagName("tr"));
            for (int i = 0; i < rows.Count; i++)
            {
                var cols = rows[i].FindElements(By.TagName("td"));

                // Check Sorting
                Assert.AreEqual(cols[0].Text, items[i]);
            }

            return rows;
        }

        static void waitForAlert(IWebDriver driver)
        {
            int i = 0;
            while (i++ < 5)
            {
                try
                {
                    IAlert alert = driver.SwitchTo().Alert();
                    break;
                }
                catch (NoAlertPresentException)
                {
                    Thread.Sleep(1000);
                    continue;
                }
            }
        }
    }
}
