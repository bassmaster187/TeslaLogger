using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestMapProvider
    {

        [TestMethod]
        public void TestParking()
        {
            var fi = new FileInfo("temp.png");
            if (fi.Exists) fi.Delete();

            var x = new OSMMapProvider();
            x.CreateParkingMap(51.1262, 13.7845, 200, 150, StaticMapProvider.MapMode.Dark, StaticMapProvider.MapSpecial.None, "temp.png");
        }

        [TestMethod]
        public void CreateAllParkingMaps()
        {
            StaticMapService.CreateAllParkingMaps();

            var inst = StaticMapService.GetSingleton();

            Thread threadStaticMapService = new Thread(() =>
            {
                StaticMapService.GetSingleton().Run();
            })
            {
                Name = "StaticMapServiceThread"
            };
            threadStaticMapService.Start();

            while (inst.GetQueueLength() > 0)
                System.Threading.Thread.Sleep(500);

        }

        [TestMethod]
        public void CreateAllChargingMaps()
        {
            StaticMapService.CreateAllChargingMaps();

            var inst = StaticMapService.GetSingleton();

            Thread threadStaticMapService = new Thread(() =>
            {
                StaticMapService.GetSingleton().Run();
            })
            {
                Name = "StaticMapServiceThread"
            };
            threadStaticMapService.Start();

            while (inst.GetQueueLength() > 0)
                System.Threading.Thread.Sleep(500);

        }


        [TestMethod]
        public void CreateAllTripMaps()
        {
            ApplicationSettings.Default.Reload();
            var k = ApplicationSettings.Default.MapQuestKey;
            ApplicationSettings.Default.PropertyValues["MapQuestKey"].PropertyValue = "";
            k = ApplicationSettings.Default.MapQuestKey;



            StaticMapService.CreateAllTripMaps(StaticMapProvider.MapMode.Dark);

            var inst = StaticMapService.GetSingleton();

            Thread threadStaticMapService = new Thread(() =>
            {
                StaticMapService.GetSingleton().Run();
            })
            {
                Name = "StaticMapServiceThread"
            };
            threadStaticMapService.Start();

            while (inst.GetQueueLength() > 0)
                System.Threading.Thread.Sleep(500);

        }

        [TestMethod]
        public void CreateAllTripMapsMapQuest()
        {
            if (String.IsNullOrEmpty(Settings.Default.MapQuestKey))
                Assert.Inconclusive("No Settings for MapQuestKey");

            ApplicationSettings.Default.Reload();
            var k = ApplicationSettings.Default.MapQuestKey;
            ApplicationSettings.Default.PropertyValues["MapQuestKey"].PropertyValue = Settings.Default.MapQuestKey;

            k = ApplicationSettings.Default.MapQuestKey;

            StaticMapService.CreateAllTripMaps(StaticMapProvider.MapMode.Dark);

            var inst = StaticMapService.GetSingleton();

            Thread threadStaticMapService = new Thread(() =>
            {
                StaticMapService.GetSingleton().Run();
            })
            {
                Name = "StaticMapServiceThread"
            };
            threadStaticMapService.Start();

            while (inst.GetQueueLength() > 0)
                System.Threading.Thread.Sleep(500);

        }
    }
}
