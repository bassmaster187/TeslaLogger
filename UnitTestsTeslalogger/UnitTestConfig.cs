using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestConfig
    {
        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            System.Diagnostics.Debug.WriteLine("ClassInit");

            var filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);
            if (File.Exists(filePath))
            {
                File.Copy(filePath, filePath + "-backup", true);
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            Tools.lastGrafanaSettings = DateTime.UtcNow.AddDays(-1);
            Tools.lastSleepingHourMinutsUpdated = DateTime.UtcNow.AddDays(-1);
            Tools._StreamingPos = null;
        }

        [TestMethod]
        public void CheckDefaultConfig()
        {
            var filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);
            File.WriteAllText(filePath, Program.GetDefaultConfigFileContent());
            CheckSettings(-1, -1, -1, -1, 5000, true, false, false, Tools.UpdateType.all, "hp", "celsius", "en", "", "IR", "http://raspberry:3000/", "", "");
        }

        private static void CheckSettings(int endSleepingHour, int endSleepingMin, int startSleepingHour, int startSleepingMin, int httpPort, bool combineChargingStates,
            bool useScanmyTesla, bool streamingPos, Tools.UpdateType onlineUpdates, string gPower, string gTemperature, string gLanguage, string gURLAdmin, string gRange,
            string gURL_Grafana, string gDefaultcar, string gDefaultCarId)
        {
            Tools.EndSleeping(out int hour, out int min);
            Assert.AreEqual(endSleepingHour, hour);
            Assert.AreEqual(endSleepingMin, min);

            Tools.StartSleeping(out int startHour, out int endHour);
            Assert.AreEqual(startSleepingHour, startHour);
            Assert.AreEqual(startSleepingMin, endHour);

            int port = Tools.GetHttpPort();
            Assert.AreEqual(httpPort, port);

            var ccs = Tools.CombineChargingStates();
            Assert.AreEqual(combineChargingStates, ccs);

            var sp = Tools.StreamingPos();
            Assert.AreEqual(streamingPos, sp);

            var scanMyTesla = Tools.UseScanMyTesla();
            Assert.AreEqual(useScanmyTesla, scanMyTesla);

            var update = Tools.GetOnlineUpdateSettings();
            Assert.AreEqual(onlineUpdates, update);

            Tools.GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin,
                out string Range, out string URL_Grafana, out string defaultcar, out string defaultcarid);
            Assert.AreEqual(gPower, power);
            Assert.AreEqual(gTemperature, temperature);
            Assert.AreEqual(gLanguage, language);
            Assert.AreEqual(gURLAdmin, URL_Admin);
            Assert.AreEqual(gRange, Range);
            Assert.AreEqual(gURL_Grafana, URL_Grafana);
            Assert.AreEqual(gDefaultcar, defaultcar);
            Assert.AreEqual(gDefaultCarId, defaultcarid);

            var msKeepDays = Tools.GetMothershipKeepDays();
            Assert.AreEqual(14, msKeepDays);

            int settingsInt = Tools.GetSettingsInt("SleepTimeSpanStart", -12345);
            Assert.AreEqual(-12345, settingsInt);

            settingsInt = Tools.GetSettingsInt("KeyNotExist", -12345999);
            Assert.AreEqual(-12345999, settingsInt);
        }

        [TestMethod]
        public void CheckConfig1()
        {
            var filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);
            File.Copy("../../settings-test1.json", filePath , true);

            CheckSettings(2, 0, 0, 30, 5000, true, true, true, Tools.UpdateType.stable, "kw", "fahrenheit", "en", "http://chris8:8888/admin/", "RR", "http://chris8:3000/", "Two weeks", "1");
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            System.Diagnostics.Debug.WriteLine("ClassCleanup");

            var filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);
            if (File.Exists(filePath + "-backup"))
            {
                File.Copy(filePath + "-backup", filePath, true);
                File.Delete(filePath + "-backup");
            }
        }
    }
}
