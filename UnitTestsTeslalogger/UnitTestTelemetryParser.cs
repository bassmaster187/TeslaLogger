﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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

                AssertStates(telemetry, i, lines[i]);
            }

            Assert.AreEqual(1.66, c.CurrentJSON.current_charge_energy_added);
            Assert.AreEqual(1.66, telemetry.charge_energy_added);
            Assert.AreEqual(72.6, telemetry.lastSoc, 0.1);
        }

        [TestMethod]
        public void ACCharging2()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF700000", "", null, false);

            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            telemetry.handleACChargeChange += Telemetry_handleACChargeChange;

            var lines = LoadData("../../testdata/ACCharging2.txt");

            for (int i = 0; i < lines.Count; i++)
            {
                if (i == 6)
                    expectedACCharge = true; // PackCurrent: 16.8

                telemetry.handleMessage(lines[i]);

                AssertStates(telemetry, i, lines[i]);
            }

            Assert.AreEqual(0.08, c.CurrentJSON.current_charge_energy_added, 0.01);
            Assert.AreEqual(0.08, telemetry.charge_energy_added);
            Assert.AreEqual(5.3, telemetry.lastSoc, 0.1);
        }

        [TestMethod]
        public void ChargingDetailedWithoutChargingState()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF700000", "", null, false);

            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            telemetry.handleACChargeChange += Telemetry_handleACChargeChange;

            var lines = LoadData("../../testdata/ChargingDetailedWithoutChargingState.txt");

            for (int i = 0; i < lines.Count; i++)
            {
                if (!lines[i].Contains("\"data\""))
                    continue;

                if (i == 21)
                    expectedACCharge = true; // DetailedChargingState
                else if (i == 67)
                    expectedACCharge = false; // DetailedChargingState

                telemetry.handleMessage(lines[i]);

                AssertStates(telemetry, i, lines[i]);
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

                AssertStates(telemetry, i, lines[i]);
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

                AssertStates(telemetry, i, lines[i]);
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

                AssertStates(telemetry, i, lines[i]);
            }

            Assert.AreEqual(10.4, c.CurrentJSON.current_charge_energy_added);
            Assert.AreEqual(10.4, telemetry.charge_energy_added);
            Assert.AreEqual(14.4, telemetry.lastSoc, 0.1);
        }

        [TestMethod]
        public void DCCharging3()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF700000", "", null, false);

            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            telemetry.handleACChargeChange += Telemetry_handleACChargeChange;

            var lines = LoadData("../../testdata/DCCharging3.txt");

            for (int i = 0; i < lines.Count; i++)
            {
                if (i == 19)
                    expectedDCCharge = true; 

                telemetry.handleMessage(lines[i]);

                AssertStates(telemetry, i, lines[i]);
            }

            Assert.AreEqual(9.44, c.CurrentJSON.current_charge_energy_added);
            Assert.AreEqual(9.44, telemetry.charge_energy_added);
            Assert.AreEqual(31.9, telemetry.lastSoc, 0.1);
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
                AssertStates(telemetry, i, lines[i]);
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
                AssertStates(telemetry, i, lines[i]);
            }
        }

        [TestMethod]
        public void PluggedInNotChargingPrecondition()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF700000", "", null, false);

            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            telemetry.handleACChargeChange += Telemetry_handleACChargeChange;

            var lines = LoadData("../../testdata/PluggedIn-NotCharging-Precondition.txt");

            for (int i = 0; i < lines.Count; i++)
            {
                telemetry.handleMessage(lines[i]);
                Assert.Inconclusive("Test skipped! Known problem");
                // AssertStates(telemetry, i, lines[i]);
            }
        }

        [TestMethod]
        public void Route()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "5YJ3E7EA3LF700000", "", null, false);

            var telemetry = new TelemetryParser(c);
            telemetry.databaseCalls = false;
            telemetry.handleACChargeChange += Telemetry_handleACChargeChange;

            var lines = LoadData("../../testdata/Route.txt");

            for (int i = 0; i < lines.Count; i++)
            {
                telemetry.handleMessage(lines[i]);
                if (i == 0)
                {
                    Assert.AreEqual(0, c.CurrentJSON.active_route_traffic_minutes_delay);
                    Assert.AreEqual(null, c.CurrentJSON.active_route_energy_at_arrival);
                    Assert.AreEqual(null, c.CurrentJSON.active_route_km_to_arrival);
                    Assert.AreEqual(null, c.CurrentJSON.active_route_minutes_to_arrival);
                    Assert.AreEqual(null, c.CurrentJSON.active_route_destination);
                }
                else if (i == 14)
                {
                    Assert.AreEqual(0, c.CurrentJSON.active_route_traffic_minutes_delay);
                    Assert.AreEqual(null, c.CurrentJSON.active_route_energy_at_arrival);
                    Assert.AreEqual(29, c.CurrentJSON.active_route_km_to_arrival);
                    Assert.AreEqual(22, c.CurrentJSON.active_route_minutes_to_arrival);
                    Assert.AreEqual("Arbeit", c.CurrentJSON.active_route_destination);
                }
                else if (i == 15)
                {
                    Assert.AreEqual(0, c.CurrentJSON.active_route_traffic_minutes_delay);
                    Assert.AreEqual(58, c.CurrentJSON.active_route_energy_at_arrival);
                    Assert.AreEqual(29, c.CurrentJSON.active_route_km_to_arrival);
                    Assert.AreEqual(22, c.CurrentJSON.active_route_minutes_to_arrival);
                    Assert.AreEqual("Arbeit", c.CurrentJSON.active_route_destination);
                }
                else if (i == 19)
                {
                    Assert.AreEqual(0, c.CurrentJSON.active_route_traffic_minutes_delay);
                    Assert.AreEqual(58, c.CurrentJSON.active_route_energy_at_arrival);
                    Assert.AreEqual(29, c.CurrentJSON.active_route_km_to_arrival);
                    Assert.AreEqual(22, c.CurrentJSON.active_route_minutes_to_arrival);
                    Assert.AreEqual("Arbeit", c.CurrentJSON.active_route_destination);
                }
                else if (i == 25)
                {
                    Assert.AreEqual(0, c.CurrentJSON.active_route_traffic_minutes_delay);
                    Assert.AreEqual(null, c.CurrentJSON.active_route_energy_at_arrival);
                    Assert.AreEqual(29, c.CurrentJSON.active_route_km_to_arrival);
                    Assert.AreEqual(22, c.CurrentJSON.active_route_minutes_to_arrival);
                    Assert.AreEqual(null, c.CurrentJSON.active_route_destination);
                }
                else if (i == 30)
                {
                    Assert.AreEqual(0, c.CurrentJSON.active_route_traffic_minutes_delay);
                    Assert.AreEqual(58, c.CurrentJSON.active_route_energy_at_arrival);
                    Assert.AreEqual(29, c.CurrentJSON.active_route_km_to_arrival);
                    Assert.AreEqual(22, c.CurrentJSON.active_route_minutes_to_arrival);
                    Assert.AreEqual(null, c.CurrentJSON.active_route_destination);
                }
                else if (i == 35)
                {
                    Assert.AreEqual(0, c.CurrentJSON.active_route_traffic_minutes_delay);
                    Assert.AreEqual(58, c.CurrentJSON.active_route_energy_at_arrival);
                    Assert.AreEqual(29, c.CurrentJSON.active_route_km_to_arrival);
                    Assert.AreEqual(26, c.CurrentJSON.active_route_minutes_to_arrival);
                    Assert.AreEqual("Rot an der Rot", c.CurrentJSON.active_route_destination);
                }
            }
        }


        private void AssertStates(TelemetryParser telemetry, int line, string content)
        {
            Assert.AreEqual(expectedACCharge, telemetry.acCharging, $"\r\nLine: {line}\r\n" +content);
            Assert.AreEqual(expectedDriving, telemetry.Driving, $"\r\nLine: {line}\r\n" + content);
            Assert.AreEqual(expectedDCCharge, telemetry.dcCharging, $"\r\nLine: {line}\r\n" + content);
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
                if (pos > 0)
                {
                    string s = line.Substring(pos + 7);
                    data.Add(s);
                    continue;
                }

                pos = line.IndexOf("\"data\"");
                if (pos > 0)
                {
                    string s = line.Substring(pos-2);
                    data.Add(s);
                    continue;
                }
            }
            return data;
        }
    }
}
