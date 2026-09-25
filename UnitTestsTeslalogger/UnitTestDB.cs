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

            while (UpdateTeslalogger.done.IsCancellationRequested == false)
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
            DBHelper.ExecuteSQLQuery("DELETE FROM pos where carid = 0 AND address LIKE 'UnitTest%'");
        }

        [TestMethod]
        public void CheckCombineCharging1()
        {
            Program.VERBOSE = true;
            DBHelper.ExecuteSQLQuery("DELETE FROM chargingstate where carid = 0");
            DBHelper.ExecuteSQLQuery("DELETE FROM charging where carid = 0");

            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);

            var dt = new DateTime(2024, 1, 5);

            c.dbHelper.InsertPosAsync(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt).ToString(), 48.1850756, 9.9016996, 0, 0, 1000, 100, 100, 30, 20, 10, null);

            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddSeconds(10)).ToString(), "30", "0", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.StartChargingStateAsync(c.webhelper);

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

            c.dbHelper.InsertPosAsync(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(60)).ToString(), 48.1850756, 9.9016996, 0, 0, 1000, 100, 100, 30, 20, 10, null); // same pos and same odometer
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(61)).ToString(), "30", "0", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.StartChargingStateAsync(c.webhelper);
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

            c.dbHelper.InsertPosAsync(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt).ToString(), 48.1850756, 9.9016996, 0, 0, 1000, 100, 100, 30, 20, 10, null);

            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddSeconds(10)).ToString(), "30", "0", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.StartChargingStateAsync(c.webhelper);

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

            c.dbHelper.InsertPosAsync(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(60)).ToString(), 48.1850756, 9.9016996, 0, 0, 1001, 100, 100, 30, 20, 10, null); // different odometer
            c.dbHelper.InsertCharging(TelemetryParser.DateTimeToUTC_UnixTimestamp(dt.AddMinutes(61)).ToString(), "30", "0", "11", 100, 100, "240", "2", "16", null, true, "16", "16");
            c.dbHelper.StartChargingStateAsync(c.webhelper);
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

        [TestMethod]
        public void ChargingStateLocationIsSuC_SuperchargerAddress_NullBrandType_True()
        {
            // Fleet API users: fast_charger_brand/fast_charger_type are NULL -> address fallback (#1752)
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            int posID = InsertTestPos("UnitTest Supercharger-V4 DE-Offenbach");
            int chargingStateID = InsertTestChargingState(posID, null, null);

            Assert.IsTrue(c.dbHelper.ChargingStateLocationIsSuC(chargingStateID));
        }

        [TestMethod]
        public void ChargingStateLocationIsSuC_SuperchargerAddress_EmptyBrandType_True()
        {
            // Fleet API users: fast_charger_brand/fast_charger_type are '' (empty string) -> address fallback (#1752)
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            int posID = InsertTestPos("UnitTest Supercharger-V4 DE-Offenbach");
            int chargingStateID = InsertTestChargingState(posID, "", "");

            Assert.IsTrue(c.dbHelper.ChargingStateLocationIsSuC(chargingStateID));
        }

        [TestMethod]
        public void ChargingStateLocationIsSuC_SuperchargerAddress_Lowercase_True()
        {
            // address match is case-insensitive
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            int posID = InsertTestPos("UnitTest supercharger-v4 de-offenbach");
            int chargingStateID = InsertTestChargingState(posID, null, null);

            Assert.IsTrue(c.dbHelper.ChargingStateLocationIsSuC(chargingStateID));
        }

        [TestMethod]
        public void ChargingStateLocationIsSuC_HomeAddress_NullBrandType_False()
        {
            // no Supercharger address and no brand/type -> false
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            int posID = InsertTestPos("UnitTest Home");
            int chargingStateID = InsertTestChargingState(posID, null, null);

            Assert.IsFalse(c.dbHelper.ChargingStateLocationIsSuC(chargingStateID));
        }

        [TestMethod]
        public void ChargingStateLocationIsSuC_HomeAddress_TeslaTesla_True()
        {
            // old behavior: brand/type check does not depend on the address
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            int posID = InsertTestPos("UnitTest Home");
            int chargingStateID = InsertTestChargingState(posID, "Tesla", "Tesla");

            Assert.IsTrue(c.dbHelper.ChargingStateLocationIsSuC(chargingStateID));
        }

        [TestMethod]
        public void ChargingStateLocationIsSuC_HomeAddress_TeslaCombo_True()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            int posID = InsertTestPos("UnitTest Home");
            int chargingStateID = InsertTestChargingState(posID, "Tesla", "Combo");

            Assert.IsTrue(c.dbHelper.ChargingStateLocationIsSuC(chargingStateID));
        }

        [TestMethod]
        public void ChargingStateLocationIsSuC_HomeAddress_TeslaCCS_False()
        {
            // old behavior: only Tesla/Combo are valid fast_charger_type values
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            int posID = InsertTestPos("UnitTest Home");
            int chargingStateID = InsertTestChargingState(posID, "Tesla", "CCS");

            Assert.IsFalse(c.dbHelper.ChargingStateLocationIsSuC(chargingStateID));
        }

        [TestMethod]
        public void ChargingStateLocationIsSuC_NoPos_TeslaTesla_True()
        {
            // LEFT JOIN: charging state without pos still matches on brand/type
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            int chargingStateID = InsertTestChargingState(null, "Tesla", "Tesla");

            Assert.IsTrue(c.dbHelper.ChargingStateLocationIsSuC(chargingStateID));
        }

        [TestMethod]
        public void ChargingStateLocationIsSuC_NoPos_NullBrandType_False()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "", null, false);
            int chargingStateID = InsertTestChargingState(null, null, null);

            Assert.IsFalse(c.dbHelper.ChargingStateLocationIsSuC(chargingStateID));
        }

        private static int InsertTestPos(string address)
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("INSERT INTO pos (Datum, lat, lng, address, CarID) VALUES (now(3), 0, 0, @address, 0)", con))
                {
                    cmd.Parameters.AddWithValue("@address", address);
                    cmd.ExecuteNonQuery();
                    return (int)cmd.LastInsertedId;
                }
            }
        }

        private static int InsertTestChargingState(object posID, object brand, object type)
        {
            using (MySqlConnection con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (MySqlCommand cmd = new MySqlCommand("INSERT INTO chargingstate (StartDate, Pos, CarID, fast_charger_brand, fast_charger_type) VALUES (now(), @pos, 0, @brand, @type)", con))
                {
                    cmd.Parameters.AddWithValue("@pos", posID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@brand", brand ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@type", type ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                    return (int)cmd.LastInsertedId;
                }
            }
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
