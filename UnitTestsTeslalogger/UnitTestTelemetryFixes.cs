using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestTelemetryFixes
    {
        const string VIN = "5YJ3E7EA3LF700000";

        static TelemetryParser CreateParser(out Car c)
        {
            c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", VIN, "", null, false);
            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            return telemetry;
        }

        static string Msg(string key, string value)
        {
            return "{\"data\":[{\"key\":\"" + key + "\",\"value\":" + value + "}],\"createdAt\":\"2026-06-07T10:00:00.000000000Z\",\"vin\":\"" + VIN + "\"}";
        }

        // #1725: PreconditioningEnabled without booleanValue threw a RuntimeBinderException before the value was stored
        [TestMethod]
        public void PreconditioningStringValue()
        {
            var telemetry = CreateParser(out Car c);
            telemetry.handleMessageAsync(Msg("PreconditioningEnabled", "{\"stringValue\":\"True\"}")).Wait();
            Assert.IsTrue(c.CurrentJSON.current_is_preconditioning);
        }

        // #1725: firmware 2026.20 sends {"invalid": true} -> value is skipped, last known state is kept
        [TestMethod]
        public void PreconditioningInvalid()
        {
            var telemetry = CreateParser(out Car c);
            c.CurrentJSON.current_is_preconditioning = true;
            telemetry.handleMessageAsync(Msg("PreconditioningEnabled", "{\"invalid\":true}")).Wait();
            Assert.IsTrue(c.CurrentJSON.current_is_preconditioning);
        }

        // #1553: energy counter reset while not charging becomes the new baseline
        [TestMethod]
        public void ChargingEnergyCounterResetWhileNotCharging()
        {
            var telemetry = CreateParser(out _);
            telemetry.charge_energy_added = 10.5;
            telemetry.handleMessageAsync(Msg("DCChargingEnergyIn", "{\"doubleValue\":2.25}")).Wait();
            Assert.AreEqual(2.25, telemetry.charge_energy_added.Value, 0.001);
        }

        // #1553: a higher value while not charging does not change the baseline
        [TestMethod]
        public void ChargingEnergyCounterHigherWhileNotCharging()
        {
            var telemetry = CreateParser(out _);
            telemetry.charge_energy_added = 10.5;
            telemetry.handleMessageAsync(Msg("DCChargingEnergyIn", "{\"doubleValue\":12.0}")).Wait();
            Assert.AreEqual(10.5, telemetry.charge_energy_added.Value, 0.001);
        }
    }
}
