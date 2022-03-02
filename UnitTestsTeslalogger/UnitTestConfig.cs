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

        [TestMethod]
        public void CheckDefaultConfig()
        {
            var filePath = FileManager.GetFilePath(TLFilename.SettingsFilename);
            File.WriteAllText(filePath, Program.GetDefaultConfigFileContent());

            Tools.EndSleeping(out int hour, out int min);
            Assert.AreEqual(-1, hour);
            Assert.AreEqual(-1, min);

            Tools.StartSleeping(out int startHour, out int endHour);
            Assert.AreEqual(-1, startHour);
            Assert.AreEqual(-1, endHour);

            int port = Tools.GetHttpPort();
            Assert.AreEqual(5000, port);

            var ccs = Tools.CombineChargingStates();
            Assert.AreEqual(true, ccs);

            var sp = Tools.StreamingPos();
            Assert.AreEqual(false, sp);

            var scanMyTesla = Tools.UseScanMyTesla();
            Assert.AreEqual(false, scanMyTesla);

            var update = Tools.GetOnlineUpdateSettings();
            Assert.AreEqual(Tools.UpdateType.all, update);

            Tools.GrafanaSettings(out string power, out string temperature, out string length, out string language, out string URL_Admin, 
                out string Range, out string URL_Grafana, out string defaultcar, out string defaultcarid);
            Assert.AreEqual("hp", power);
            Assert.AreEqual("celsius", temperature);
            Assert.AreEqual("en", language);
            Assert.AreEqual("", URL_Admin);
            Assert.AreEqual("IR", Range);
            Assert.AreEqual("http://raspberry:3000/", URL_Grafana);
            Assert.AreEqual("", defaultcar);
            Assert.AreEqual("", defaultcarid);

            var msKeepDays = Tools.GetMothershipKeepDays();
            Assert.AreEqual(14, msKeepDays);

            int settingsInt = Tools.GetSettingsInt("SleepTimeSpanStart", -12345);
            Assert.AreEqual(-12345, settingsInt);

            settingsInt = Tools.GetSettingsInt("KeyNotExist", -12345999);
            Assert.AreEqual(-12345999, settingsInt);
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
