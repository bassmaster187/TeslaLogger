using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TeslaLogger;
using MySql.Data.MySqlClient;
using System.Data;
using System.Runtime.ConstrainedExecution;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestDB
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            // Einmalige Initialisierung für alle Tests in der Klasse
            Program.VERBOSE = true;

            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            c.Log("Update DBSchema!");

            UpdateTeslalogger.Start();

            while (UpdateTeslalogger.Done == false)
            {
                System.Threading.Thread.Sleep(100);
            }
            c.Log("Update done!");

        }

        [TestInitialize]
        public void TestInit()
        {
            DBHelper.ExecuteSQLQuery("DELETE FROM chargingstate where carid = 0");
            DBHelper.ExecuteSQLQuery("DELETE FROM charging where carid = 0");
        }

        [TestMethod]
        public void CheckCombineCharging1()
        {
            Program.VERBOSE = true;
            DBHelper.ExecuteSQLQuery("DELETE FROM chargingstate where carid = 0");
            DBHelper.ExecuteSQLQuery("DELETE FROM charging where carid = 0");

            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);

            var dt = new DateTime(2024, 1, 5);

            c.dbHelper.InsertPos(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt).ToString(), 48.1850756, 9.9016996, 0, 0, 1000, 100, 100, 30, 10, null);

            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddSeconds(10)).ToString(), "30", "0", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.StartChargingState(c.webhelper);

            Console.WriteLine("ChargingStateID: " + c.dbHelper.GetMaxChargingstateId(out _, out _, out _, out _));


            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(10)).ToString(), "32", "2", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(20)).ToString(), "34", "4", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(30)).ToString(), "36", "5", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(40)).ToString(), "37", "6", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.CloseChargingStates();
            
            DataTable dt2 = GetChargingstates();

            Assert.AreEqual(dt2.Rows.Count, 1);
            Assert.AreEqual(6.0, dt2.Rows[0]["charge_energy_added"]);
            Assert.AreEqual(dt.AddSeconds(10), dt2.Rows[0]["StartDate"]);
            Assert.AreEqual(dt.AddMinutes(40), dt2.Rows[0]["EndDate"]);

            c.dbHelper.InsertPos(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(60)).ToString(), 48.1850756, 9.9016996, 0, 0, 1000, 100, 100, 30, 10, null); // same pos and same odometer
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(61)).ToString(), "30", "0", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.StartChargingState(c.webhelper);
            Console.WriteLine("ChargingStateID: " + c.dbHelper.GetMaxChargingstateId(out _, out _, out _, out _));


            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(70)).ToString(), "32", "2", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(80)).ToString(), "34", "4", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(90)).ToString(), "36", "5", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(100)).ToString(), "37", "7", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.CloseChargingStates();

            DataTable dt3 = GetChargingstates();

            Assert.AreEqual(dt3.Rows.Count, 1);
            Assert.AreEqual(13.0, dt3.Rows[0]["charge_energy_added"]);
            Assert.AreEqual(dt.AddSeconds(10), dt3.Rows[0]["StartDate"]);
            Assert.AreEqual(dt.AddMinutes(100), dt3.Rows[0]["EndDate"]);
        }

        [TestMethod]
        public void CheckCombineCharging2_nocombine()
        {
            Program.VERBOSE = true;
            DBHelper.ExecuteSQLQuery("DELETE FROM chargingstate where carid = 0");
            DBHelper.ExecuteSQLQuery("DELETE FROM charging where carid = 0");

            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);

            var dt = new DateTime(2024, 1, 5);

            c.dbHelper.InsertPos(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt).ToString(), 48.1850756, 9.9016996, 0, 0, 1000, 100, 100, 30, 10, null);

            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddSeconds(10)).ToString(), "30", "0", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.StartChargingState(c.webhelper);

            Console.WriteLine("ChargingStateID: " + c.dbHelper.GetMaxChargingstateId(out _, out _, out _, out _));


            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(10)).ToString(), "32", "2", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(20)).ToString(), "34", "4", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(30)).ToString(), "36", "5", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(40)).ToString(), "37", "6", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.CloseChargingStates();

            DataTable dt2 = GetChargingstates();

            Assert.AreEqual(dt2.Rows.Count, 1);
            Assert.AreEqual(6.0, dt2.Rows[0]["charge_energy_added"]);
            Assert.AreEqual(dt.AddSeconds(10), dt2.Rows[0]["StartDate"]);
            Assert.AreEqual(dt.AddMinutes(40), dt2.Rows[0]["EndDate"]);

            c.dbHelper.InsertPos(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(60)).ToString(), 48.1850756, 9.9016996, 0, 0, 1001, 100, 100, 30, 10, null); // different odometer
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(61)).ToString(), "30", "0", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.StartChargingState(c.webhelper);
            Console.WriteLine("ChargingStateID: " + c.dbHelper.GetMaxChargingstateId(out _, out _, out _, out _));


            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(70)).ToString(), "32", "2", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(80)).ToString(), "34", "4", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(90)).ToString(), "36", "5", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(100)).ToString(), "37", "7", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.CloseChargingStates();

            DataTable dt3 = GetChargingstates();

            Assert.AreEqual(2, dt3.Rows.Count);
            Assert.AreEqual(6.0, dt3.Rows[0]["charge_energy_added"]);
            Assert.AreEqual(dt.AddSeconds(10), dt3.Rows[0]["StartDate"]);
            Assert.AreEqual(dt.AddMinutes(40), dt3.Rows[0]["EndDate"]);

            Assert.AreEqual(7.0, dt3.Rows[1]["charge_energy_added"]);
            Assert.AreEqual(dt.AddMinutes(61), dt3.Rows[1]["StartDate"]); // Start Date from charging (not from pos!)
            Assert.AreEqual(dt.AddMinutes(100), dt3.Rows[1]["EndDate"]);
        }

        private static DataTable GetChargingstates()
        {
            var da = new MySqlDataAdapter("SELECT * FROM chargingstate where carid = 0 order by id", DBHelper.DBConnectionstring);
            System.Data.DataTable dt2 = new System.Data.DataTable();
            da.Fill(dt2);
            return dt2;
        }
    }
}
