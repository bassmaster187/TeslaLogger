using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using TeslaLogger;
using TeslaLoggerNET8.Lucid;

namespace UnitTestsTeslalogger
{
    /// <summary>
    /// Tests the Lucid raw-data parser (LucidWebHelper.ParseLucidData) against real data recorded
    /// from the Lucid API for the vehicle "Lucy" (Lucid Air Pure). The preconditioning state is
    /// carried by the nested hvac power state: HVAC_PRECONDITION (running) vs. HVAC_OFF (idle).
    /// The fixtures live in testdata/Lucid_Lucy_raw_*.txt (VIN / account id / position redacted).
    /// </summary>
    [TestClass]
    public class UnitTestLucidParser
    {
        // Recorded while preconditioning was active: hvac.power = HVAC_PRECONDITION.
        [TestMethod]
        public void Lucid_HvacPrecondition_IsDetectedAsPreconditioning()
        {
            LucidCar c = NewLucidCar();
            LucidWebHelper wh = (LucidWebHelper)c.webhelper;
            wh.databaseCalls = false;

            wh.ParseLucidData(LoadRaw("Lucid_Lucy_raw_preconditioning.txt"));

            Assert.IsTrue(c.CurrentJSON.current_is_preconditioning,
                "hvac.power = HVAC_PRECONDITION must be detected as preconditioning.");
        }

        // Recorded while the car was idle / going to sleep: hvac.power = HVAC_OFF.
        [TestMethod]
        public void Lucid_HvacOff_IsNotPreconditioning()
        {
            LucidCar c = NewLucidCar();
            LucidWebHelper wh = (LucidWebHelper)c.webhelper;
            wh.databaseCalls = false;

            wh.ParseLucidData(LoadRaw("Lucid_Lucy_raw_not-preconditioning.txt"));

            Assert.IsFalse(c.CurrentJSON.current_is_preconditioning,
                "hvac.power = HVAC_OFF must not be flagged as preconditioning.");
        }

        // Preconditioning must switch off again when a later snapshot reports HVAC_OFF.
        [TestMethod]
        public void Lucid_Preconditioning_TurnsOffOnHvacOff()
        {
            LucidCar c = NewLucidCar();
            LucidWebHelper wh = (LucidWebHelper)c.webhelper;
            wh.databaseCalls = false;

            wh.ParseLucidData(LoadRaw("Lucid_Lucy_raw_preconditioning.txt"));
            Assert.IsTrue(c.CurrentJSON.current_is_preconditioning);

            wh.ParseLucidData(LoadRaw("Lucid_Lucy_raw_not-preconditioning.txt"));
            Assert.IsFalse(c.CurrentJSON.current_is_preconditioning,
                "a following HVAC_OFF snapshot must clear the preconditioning flag.");
        }

        // Battery pack preconditioning alone (BATTERY_PRECON_ON, HVAC off) must set the battery
        // heater flag, not the cabin preconditioning flag. Live fixture recorded while the Lucid
        // app had battery preconditioning active.
        [TestMethod]
        public void Lucid_BatteryPreconOn_SetsBatteryHeaterNotPreconditioning()
        {
            LucidCar c = NewLucidCar();
            LucidWebHelper wh = (LucidWebHelper)c.webhelper;
            wh.databaseCalls = false;

            wh.ParseLucidData(LoadRaw("Lucid_Lucy_raw_battery-preconditioning.txt"));

            Assert.IsTrue(c.CurrentJSON.current_battery_heater,
                "battery preconditioning_status = BATTERY_PRECON_ON must set the battery heater flag.");
            Assert.IsFalse(c.CurrentJSON.current_is_preconditioning,
                "battery preconditioning with hvac.power = HVAC_OFF must not set the cabin preconditioning flag.");
        }

        // HVAC preconditioning must set the cabin flag and leave the battery heater flag alone
        // (battery was BATTERY_PRECON_OFF in that snapshot).
        [TestMethod]
        public void Lucid_HvacPrecondition_SetsPreconditioningNotBatteryHeater()
        {
            LucidCar c = NewLucidCar();
            LucidWebHelper wh = (LucidWebHelper)c.webhelper;
            wh.databaseCalls = false;

            wh.ParseLucidData(LoadRaw("Lucid_Lucy_raw_preconditioning.txt"));

            Assert.IsTrue(c.CurrentJSON.current_is_preconditioning);
            Assert.IsFalse(c.CurrentJSON.current_battery_heater,
                "hvac preconditioning with BATTERY_PRECON_OFF must not set the battery heater flag.");
        }

        // BATTERY_PRECON_UNAVAILABLE / OFF / UNKNOWN must not set the battery heater flag.
        [TestMethod]
        public void Lucid_BatteryPreconOffOrUnavailable_IsNotBatteryHeater()
        {
            foreach (string state in new[] { "BATTERY_PRECON_OFF", "BATTERY_PRECON_UNAVAILABLE", "BATTERY_PRECON_UNKNOWN" })
            {
                LucidCar c = NewLucidCar();
                LucidWebHelper wh = (LucidWebHelper)c.webhelper;
                wh.databaseCalls = false;

                wh.ParseLucidData(Snippet(state));

                Assert.IsFalse(c.CurrentJSON.current_battery_heater,
                    $"{state} must not set the battery heater flag.");
                Assert.IsFalse(c.CurrentJSON.current_is_preconditioning,
                    $"{state} with HVAC_OFF must not set the preconditioning flag.");
            }
        }

        // Minimal raw-text snippet (protobuf text format) with HVAC off and the given battery
        // preconditioning state. The vin line is required: the parser ignores everything
        // before the matching vin.
        private static string Snippet(string batteryPreconState)
        {
            return "vin: \"50EA1PFA4PAXXXXXX\"\n" +
                   "battery {\n" +
                   $"  preconditioning_status: {batteryPreconState}\n" +
                   "}\n" +
                   "hvac {\n" +
                   "  power: HVAC_OFF\n" +
                   "}\n";
        }

        private static LucidCar NewLucidCar()
        {
            // CarInDB 0 keeps the car out of Allcars / the polling loop and avoids DB access.
            // The VIN has to match the recorded fixture so the parser accepts its lines.
            return new LucidCar(0, "", "", 0, "", DateTime.Now, "", "LUCID", "", "", "Lucy",
                "50EA1PFA4PAXXXXXX", "", null, false);
        }

        private static string LoadRaw(string filename)
        {
            // Same relative lookup the other parser tests use (working dir is the test output dir).
            string path = "../../testdata/" + filename;
            if (!File.Exists(path))
                path = "../" + path;

            return File.ReadAllText(path);
        }
    }
}
