using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeslaLogger;

namespace UnitTestsTeslalogger
{
    [TestClass]
    public class UnitTestWallbox
    {

        [TestMethod]
        public void OpenWBMeterLP1Param()
        {
            var v = new ElectricityMeterOpenWB("http://openwb", "LP1");
            Assert.AreEqual(1, v.LP);
            string ret = v.ToString();
            Console.WriteLine(ret);
        }

        [TestMethod]
        public void OpenWBMeterLP2Param()
        {
            var v = new ElectricityMeterOpenWB("http://openwb", "LP2");
            Assert.AreEqual(2, v.LP);
            string ret = v.ToString();
            Console.WriteLine(ret);
        }

        [TestMethod]
        public void OpenWBMeterNoParam()
        {
            var v = new ElectricityMeterOpenWB("http://openwb", "");
            Assert.AreEqual(1, v.LP);
            string ret = v.ToString();
            Console.WriteLine(ret);
        }

        [TestMethod]
        public void OpenWBMeterConstructor()
        {
            Car c = new Car(0, "", "", 0, "", DateTime.Now, "", "", "", "", "", "", "ffffffff", null, false);
            c.CarInDB = 1;

            var v = ElectricityMeterBase.Instance(c);
            var ret = v.ToString();
            Console.WriteLine(ret);
        }

        [TestMethod]
        public void OpenWB2()
        {
            var v = new ElectricityMeterOpenWB2("", "");

            v.mockup_version = System.IO.File.ReadAllText(@"..\..\testdata\openwb2_version.txt");
            v.mockup_charge_state = System.IO.File.ReadAllText(@"..\..\testdata\openwb2_charge_state.txt");
            v.mockup_charge_point = System.IO.File.ReadAllText(@"..\..\testdata\openwb2_cp.txt");
            v.mockup_grid = System.IO.File.ReadAllText(@"..\..\testdata\openwb2_grid.txt");
            v.mockup_hierarchy = System.IO.File.ReadAllText(@"..\..\testdata\openwb2_hierarchy.txt");

            double? kwh = v.GetVehicleMeterReading_kWh();
            var charging = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);
            Assert.AreEqual(441.759, kwh);
            Assert.AreEqual(false, charging);
            Assert.AreEqual(123456.789, utility_meter_kwh);
            Assert.AreEqual("2.1.7-Beta.2", version);
        }


        [TestMethod]
        public void GoEMeter()
        {
            string url = Settings.Default.ElectricityMeterGoEURL;

            if (string.IsNullOrEmpty(url))
                Assert.Inconclusive("No Settings for Go-E Charger");

            var v = new ElectricityMeterGoE(url, "");
            string ret = v.ToString();
            Console.WriteLine(ret);
        }


        [TestMethod]
        public void CFos()
        {
            var v = new ElectricityMeterCFos("", "");
            v.get_dev_info = System.IO.File.ReadAllText(@"..\..\testdata\cfos.txt");

            double? kwh = v.GetVehicleMeterReading_kWh();
            var charging = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(628.277, kwh);
            Assert.AreEqual(false, charging);
            Assert.AreEqual(9766.409, utility_meter_kwh);
            Assert.AreEqual("2.0.1", version);
        }

        [TestMethod]
        public void SmartEVSE3()
        {
            var v = new ElectricityMeterSmartEVSE3("", "");
            v.mockup_status = System.IO.File.ReadAllText(@"..\..\testdata\smartevse3.txt");

            double? kwh = v.GetVehicleMeterReading_kWh();
            var charging = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(2874.199951, kwh);
            Assert.AreEqual(true, charging);
            Assert.AreEqual(3456.98765, utility_meter_kwh);
            Assert.AreEqual("v3.6.10", version);
        }

        [TestMethod]
        public void EVCC_Wallbox()
        {
            var v = new ElectricityMeterEVCC("", "Wallbox1");
            v.api_state = System.IO.File.ReadAllText(@"..\..\testdata\evcc.txt");

            double? kwh = v.GetVehicleMeterReading_kWh();
            var charging = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(544.41, kwh);
            Assert.AreEqual(false, charging);
            Assert.AreEqual(5755.34, utility_meter_kwh);
            Assert.AreEqual("0.133.0", version);
        }

        [TestMethod]
        public void EVCC_Vehicle()
        {
            var v = new ElectricityMeterEVCC("", "TestCar1");
            v.api_state = System.IO.File.ReadAllText(@"..\..\testdata\evcc.txt");

            double? kwh = v.GetVehicleMeterReading_kWh();
            var charging = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(544.41, kwh);
            Assert.AreEqual(false, charging);
            Assert.AreEqual(5755.34, utility_meter_kwh);
            Assert.AreEqual("0.133.0", version);
        }

        [TestMethod]
        public void EVCC_multiple()
        {
            var v = new ElectricityMeterEVCC("", "TestCar2");
            v.api_state = System.IO.File.ReadAllText(@"..\..\testdata\evcc_multiple.txt");

            double? kwh = v.GetVehicleMeterReading_kWh();
            var charging = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(6716.148, kwh);
            Assert.AreEqual(false, charging);
            Assert.AreEqual(null, utility_meter_kwh);
            Assert.AreEqual("0.133.0", version);
        }

        [TestMethod]
        public void WARP()
        {
            var v = new ElectricityMeterWARP("", "");

            v.mockup_info_version = System.IO.File.ReadAllText(@"..\..\testdata\warp_infos_version.txt");
            v.mockup_evse_state = System.IO.File.ReadAllText(@"..\..\testdata\warp_evse_state.txt");
            v.mockup_wallbox_value_ids = System.IO.File.ReadAllText(@"..\..\testdata\warp_wallbox_value_ids.txt");
            v.mockup_wallbox_values = System.IO.File.ReadAllText(@"..\..\testdata\warp_wallbox_values.txt");
            v.mockup_grid_value_ids = System.IO.File.ReadAllText(@"..\..\testdata\warp_grid_value_ids.txt");
            v.mockup_grid_values = System.IO.File.ReadAllText(@"..\..\testdata\warp_grid_values.txt");

            double? kwh = v.GetVehicleMeterReading_kWh();
            var charging = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);
            Assert.AreEqual(544.4099731, kwh);
            Assert.AreEqual(false, charging);
            Assert.AreEqual(5762.71875, utility_meter_kwh);
            Assert.AreEqual("2.6.6+675aeb99", version);
        }

        [TestMethod]
        public void Shelly3EM()
        {
            var v = new ElectricityMeterShelly3EM("", "");
            v.mockup_status = "{\"wifi_sta\":{\"connected\":true,\"ssid\":\"badhome\",\"ip\":\"192.168.70.176\",\"rssi\":-78},\"cloud\":{\"enabled\":false,\"connected\":false},\"mqtt\":{\"connected\":false},\"time\":\"16:19\",\"unixtime\":1636125569,\"serial\":7665,\"has_update\":false,\"mac\":\"C45BBE5F71E5\",\"cfg_changed_cnt\":1,\"actions_stats\":{\"skipped\":0},\"relays\":[{\"ison\":false,\"has_timer\":false,\"timer_started\":0,\"timer_duration\":0,\"timer_remaining\":0,\"overpower\":false,\"is_valid\":true,\"source\":\"input\"}],\"emeters\":[{\"power\":0.00,\"pf\":0.14,\"current\":0.01,\"voltage\":236.00,\"is_valid\":true,\"total\":23870.9,\"total_returned\":28.7},{\"power\":0.00,\"pf\":0.00,\"current\":0.01,\"voltage\":236.07,\"is_valid\":true,\"total\":22102.0,\"total_returned\":59.4},{\"power\":7.49,\"pf\":0.49,\"current\":0.07,\"voltage\":235.88,\"is_valid\":true,\"total\":55527.2,\"total_returned\":0.0}],\"total_power\":7.49,\"fs_mounted\":true,\"update\":{\"status\":\"idle\",\"has_update\":false,\"new_version\":\"20210909-150410/v1.11.4-DNSfix-ge6b2f6d\",\"old_version\":\"20210909-150410/v1.11.4-DNSfix-ge6b2f6d\"},\"ram_total\":49440,\"ram_free\":30260,\"fs_size\":233681,\"fs_free\":156624,\"uptime\":1141576}";
            v.mockup_shelly = "{\"type\":\"SHEM-3\",\"mac\":\"C45BBE5F71E5\",\"auth\":false,\"fw\":\"20210909-150410/v1.11.4-DNSfix-ge6b2f6d\",\"longid\":1,\"num_outputs\":1,\"num_meters\":0,\"num_emeters\":3,\"report_period\":1}";

            double? kwh = v.GetVehicleMeterReading_kWh();
            var chargign = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(101.5001, kwh);
            Assert.AreEqual(false, chargign);
            Assert.AreEqual(null, utility_meter_kwh);
            Assert.AreEqual("20210909-150410/v1.11.4-DNSfix-ge6b2f6d", version);
        }

        [TestMethod]
        public void ShellyEM_CEmpty()
        {
            var v = new ElectricityMeterShellyEM("", "");
            v.mockup_status = System.IO.File.ReadAllText(@"..\..\testdata\shelly-em1-status.txt");
            v.mockup_shelly = System.IO.File.ReadAllText(@"..\..\testdata\shelly-em1-shelly.txt");

            double? kwh = v.GetVehicleMeterReading_kWh();
            var chargign = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(56.256099999999996, kwh);
            Assert.AreEqual(false, chargign);
            Assert.AreEqual(null, utility_meter_kwh);
            Assert.AreEqual("20221027-105518/v1.12.1-ga9117d3", version);
        }

        [TestMethod]
        public void ShellyEM_C1()
        {
            var v = new ElectricityMeterShellyEM("", "C1");
            v.mockup_status = System.IO.File.ReadAllText(@"..\..\testdata\shelly-em1-status.txt");
            v.mockup_shelly = System.IO.File.ReadAllText(@"..\..\testdata\shelly-em1-shelly.txt");

            double? kwh = v.GetVehicleMeterReading_kWh();
            var chargign = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(56.256099999999996, kwh);
            Assert.AreEqual(false, chargign);
            Assert.AreEqual(null, utility_meter_kwh);
            Assert.AreEqual("20221027-105518/v1.12.1-ga9117d3", version);
        }

        [TestMethod]
        public void ShellyEM_C2()
        {
            var v = new ElectricityMeterShellyEM("", "C2");
            v.mockup_status = System.IO.File.ReadAllText(@"..\..\testdata\shelly-em1-status.txt");
            v.mockup_shelly = System.IO.File.ReadAllText(@"..\..\testdata\shelly-em1-shelly.txt");

            double? kwh = v.GetVehicleMeterReading_kWh();
            var chargign = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(1.231, kwh);
            Assert.AreEqual(false, chargign);
            Assert.AreEqual(null, utility_meter_kwh);
            Assert.AreEqual("20221027-105518/v1.12.1-ga9117d3", version);
        }

        [TestMethod]
        public void TeslaGen3WCMeterNotCharging()
        {
            var v = new ElectricityMeterTeslaGen3WallConnector("", "");
            v.mockup_lifetime = "{\"contactor_cycles\":106,\"contactor_cycles_loaded\":0,\"alert_count\":3,\"thermal_foldbacks\":0,\"avg_startup_temp\":nan,\"charge_starts\":106,\"energy_wh\":750685,\"connector_cycles\":58,\"uptime_s\":11117950,\"charging_time_s\":355626}";
            v.mockup_vitals = "{\"contactor_closed\":false,\"vehicle_connected\":false,\"session_s\":0,\"grid_v\":231.9,\"grid_hz\":50.071,\"vehicle_current_a\":0.2,\"currentA_a\":0.2,\"currentB_a\":0.1,\"currentC_a\":0.0,\"currentN_a\":0.1,\"voltageA_v\":0.0,\"voltageB_v\":0.0,\"voltageC_v\":0.0,\"relay_coil_v\":11.8,\"pcba_temp_c\":17.9,\"handle_temp_c\":14.8,\"mcu_temp_c\":26.3,\"uptime_s\":784583,\"input_thermopile_uv\":-195,\"prox_v\":0.0,\"pilot_high_v\":11.9,\"pilot_low_v\":11.9,\"session_energy_wh\":2314.100,\"config_status\":5,\"evse_state\":1,\"current_alerts\":[]}";
            v.mockup_version = "{\"firmware_version\":\"21.8.5+g51eba2369815d7\",\"part_number\":\"1529455-02-D\",\"serial_number\":\"PGT12345678912\"}";
            double? kwh = v.GetVehicleMeterReading_kWh();
            var chargign = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();
            Console.WriteLine(ret);

            Assert.AreEqual(750.685, kwh);
            Assert.AreEqual(false, chargign);
            Assert.AreEqual(null, utility_meter_kwh);
            Assert.AreEqual("21.8.5+g51eba2369815d7", version);
        }

        [TestMethod]
        public void TeslaGen3WCMeterCharging()
        {
            var v = new ElectricityMeterTeslaGen3WallConnector("", "");
            v.mockup_lifetime = "{\"contactor_cycles\":107,\"contactor_cycles_loaded\":0,\"alert_count\":3,\"thermal_foldbacks\":0,\"avg_startup_temp\":nan,\"charge_starts\":107,\"energy_wh\":751369,\"connector_cycles\":59,\"uptime_s\":11130209,\"charging_time_s\":356356}";
            v.mockup_vitals = "{\"contactor_closed\":true,\"vehicle_connected\":true,\"session_s\":545,\"grid_v\":228.3,\"grid_hz\":50.130,\"vehicle_current_a\":5.1,\"currentA_a\":5.1,\"currentB_a\":5.1,\"currentC_a\":5.1,\"currentN_a\":0.0,\"voltageA_v\":230.3,\"voltageB_v\":230.3,\"voltageC_v\":228.7,\"relay_coil_v\":6.1,\"pcba_temp_c\":22.7,\"handle_temp_c\":16.6,\"mcu_temp_c\":28.7,\"uptime_s\":733178,\"input_thermopile_uv\":-516,\"prox_v\":1.9,\"pilot_high_v\":4.6,\"pilot_low_v\":4.6,\"session_energy_wh\":506.800,\"config_status\":5,\"evse_state\":11,\"current_alerts\":[]}";
            v.mockup_version = "{\"firmware_version\":\"21.8.5+g51eba2369815d7\",\"part_number\":\"1529455-02-D\",\"serial_number\":\"PGT12345678912\"}";
            double? kwh = v.GetVehicleMeterReading_kWh();
            var chargign = v.IsCharging();
            var utility_meter_kwh = v.GetUtilityMeterReading_kWh();
            var version = v.GetVersion();
            string ret = v.ToString();

            Console.WriteLine(ret);

            Assert.AreEqual(751.369, kwh);
            Assert.AreEqual(true, chargign);
            Assert.AreEqual(null, utility_meter_kwh);
            Assert.AreEqual("21.8.5+g51eba2369815d7", version);
        }
    }
}
