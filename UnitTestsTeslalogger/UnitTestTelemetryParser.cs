using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestTelemetryParser
    {
        bool _expectedACCharge = false;
        bool _expectedDriving = false;
        bool _expectedDCCharge = false;

        public bool expectedACCharge
        {
            get => _expectedACCharge; 
            set
            {
                Console.WriteLine("### ExpectedACCharge: " + value);
                _expectedACCharge = value;
            }
        }

        public bool expectedDCCharge
        {
            get => _expectedDCCharge;
            set
            {
                Console.WriteLine("### ExpectedDCCharge: " + value);
                _expectedDCCharge = value;
            }
        }

        public bool expectedDriving
        {
            get => _expectedDriving;
            set
            {
                Console.WriteLine("### ExpectedDriving: " + value);
                _expectedDriving = value;
            }
        }

        [TestMethod]
        public void ACCharging1()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF700000", "", null, false);

            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            telemetry.handleACChargeChange += Telemetry_handleACChargeChange;

            var lines = LoadData("../../testdata/ACCharging1.txt");

            for (int i = 0; i < lines.Count; i++)
            {
                if (i == 7)
                    expectedACCharge = true; // PackCurrent: 16.8

                telemetry.handleMessage(lines[i]);

                AssertStates(telemetry);
            }
        }

        [TestMethod]
        public void ACChargingJustPreheating()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF700000", "", null, false);

            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            telemetry.handleACChargeChange += Telemetry_handleACChargeChange;

            var lines = LoadData("../../testdata/ACChargingJustPreheating.txt");

            for (int i = 0; i < lines.Count; i++)
            {
                
                if (i == 25)
                    expectedACCharge = true; // ACChargingPower: 3.2


                telemetry.handleMessage(lines[i]);

                AssertStates(telemetry);
            }
        }

        [TestMethod]
        public void DCCharging1()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF700000", "", null, false);

            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            telemetry.handleACChargeChange += Telemetry_handleACChargeChange;

            var lines = LoadData("../../testdata/DCCharging1.txt");

            for (int i = 0; i < lines.Count; i++)
            {
                if (i == 14)
                    expectedDCCharge = true; // PackCurrent: 473.0
                else if (i == 190)
                    expectedDCCharge = false; // FastChargerPresent = false / ChargeState: ClearFaults
                else if (i == 202)
                    expectedDriving = true; // VehicleSpeed = 6.8

                telemetry.handleMessage(lines[i]);

                if (i == 190)
                {
                    Assert.AreEqual(35.76, c.CurrentJSON.current_charge_energy_added);
                    Assert.AreEqual(35.76, telemetry.charge_energy_added);
                }

                AssertStates(telemetry);
            }
        }

        [TestMethod]
        public void DCCharging2()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF700000", "", null, false);

            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            telemetry.handleACChargeChange += Telemetry_handleACChargeChange;

            var lines = LoadData("../../testdata/DCCharging2.txt");

            for (int i = 0; i < lines.Count; i++)
            {
                if (i == 6)
                    expectedDCCharge = true; // DCChargingPower 7.3

                telemetry.handleMessage(lines[i]);

                AssertStates(telemetry);
            }

            Assert.AreEqual(10.4, c.CurrentJSON.current_charge_energy_added);
            Assert.AreEqual(10.4, telemetry.charge_energy_added);
        }

        [TestMethod]
        public void DrivingByGear()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF700000", "", null, false);

            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            telemetry.handleACChargeChange += Telemetry_handleACChargeChange;

            var lines = LoadData("../../testdata/DrivingByGear.txt");

            for (int i = 0; i < lines.Count; i++)
            {
                if (i == 13)
                    expectedDriving = true; // Gear: D
                else if (i == 38)
                    expectedDriving = false; // Gear: P

                telemetry.handleMessage(lines[i]);
                AssertStates(telemetry);
            }
        }

        [TestMethod]
        public void DrivingBySpeed()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF700000", "", null, false);

            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            telemetry.handleACChargeChange += Telemetry_handleACChargeChange;

            var lines = LoadData("../../testdata/DrivingBySpeed.txt");

            for (int i = 0; i < lines.Count; i++)
            {
                if (i == 15)
                    expectedDriving = true; // VehicleSpeed: 16.15
                else if (i == 37)
                    expectedDriving = false; // Gear: P

                telemetry.handleMessage(lines[i]);
                AssertStates(telemetry);
            }
        }

        private void AssertStates(TelemetryParser telemetry)
        {
            Assert.AreEqual(expectedACCharge, telemetry.acCharging);
            Assert.AreEqual(expectedDriving, telemetry.Driving);
            Assert.AreEqual(expectedDCCharge, telemetry.dcCharging);
        }

        private void Telemetry_handleACChargeChange(object sender, EventArgs e)
        {
            TelemetryParser telemetryParser = (TelemetryParser)sender;
            Assert.AreEqual(expectedACCharge, telemetryParser.acCharging);
        }

        List<string> LoadData(string path)
        {
            List<string> data = new List<string>();
            string[] lines = System.IO.File.ReadAllLines(path);
            foreach (string line in lines)
            {
                var pos = line.IndexOf("*** FT:");
                if (pos == -1)
                    continue;

                string s = line.Substring(pos + 7);
                data.Add(s);
            }
            return data;
        }
    }
}
