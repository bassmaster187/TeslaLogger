using System;
using System.Collections.Generic;

namespace TeslaLogger
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Keine allgemeinen Ausnahmetypen abfangen", Justification = "<Pending>")]
    public class CurrentJSON
    {
        private static Dictionary<int, string> jsonStringHolder = new Dictionary<int, string>();

        private bool current_charging = false;
        private bool current_driving = false;
        private bool current_online = false;
        private bool current_sleeping = false;
        private bool current_falling_asleep = false;

        private int current_speed = 0;
        private int current_power = 0;
        private double current_odometer = 0;
        private double current_ideal_battery_range_km = 0;
        private double current_battery_range_km = 0;
        private double current_outside_temperature = 0;
        private int current_battery_level = 0;

        private int current_charger_voltage = 0;
        private int current_charger_phases = 0;
        private int current_charger_actual_current = 0;
        private double current_charge_energy_added = 0;
        private int current_charger_power = 0;
        private double current_charge_rate_km = 0;
        private double current_time_to_full_charge = 0;
        private bool current_charge_port_door_open = false;

        private string current_car_version = "";

        private DateTime current_trip_start = DateTime.MinValue;
        private DateTime current_trip_end = DateTime.MinValue;
        private double current_trip_km_start = 0;
        private double current_trip_km_end = 0;
        private double current_trip_max_speed = 0;
        private double current_trip_max_power = 0;
        private double current_trip_start_range = 0;
        private double current_trip_end_range = 0;
        private double wh_TR = 0.19;

        private int current_trip_duration_sec = 0;

        private double latitude = 0;
        private double longitude = 0;
        private int charge_limit_soc = 0;
        private int heading = 0;
        private double current_inside_temperature = 0;
        private bool current_battery_heater = false;
        private bool current_is_sentry_mode = false;
        private bool current_is_preconditioning = false;

        private string current_country_code = "";
        private string current_state = "";

        private DateTime lastScanMyTeslaReceived = DateTime.MinValue;
        private double? sMTCellTempAvg = null;
        private double? sMTCellMinV = null;
        private double? sMTCellAvgV = null;
        private double? sMTCellMaxV = null;
        private double? sMTCellImbalance = null;
        private double? sMTBMSmaxCharge = null;
        private double? sMTBMSmaxDischarge = null;
        private double? sMTACChargeTotal = null;
        private double? sMTDCChargeTotal = null;
        private double? sMTNominalFullPack = null;

        private double? sMTSpeed = null;
        private double? sMTBatteryPower = null;

        private string current_json = "";
        private DateTime lastJSONwrite = DateTime.MinValue;
        readonly Car car;

        public CurrentJSON(Car car)
        {
            this.car = car;
        }

        public static Dictionary<int, string> JsonStringHolder { get => jsonStringHolder; }
        public bool CurrentCharging { get => current_charging; set => current_charging = value; }
        public bool CurrentDriving { get => current_driving; set => current_driving = value; }
        public bool CurrentOnline { get => current_online; set => current_online = value; }
        public bool CurrentSleeping { get => current_sleeping; set => current_sleeping = value; }
        public bool CurrentFallingAsleep { get => current_falling_asleep; set => current_falling_asleep = value; }
        public int CurrentSpeed { get => current_speed; set => current_speed = value; }
        public int CurrentPower { get => current_power; set => current_power = value; }
        public double CurrentOdometer { get => current_odometer; set => current_odometer = value; }
        public double CurrentIdealBatteryRangeKM { get => current_ideal_battery_range_km; set => current_ideal_battery_range_km = value; }
        public double CurrentBatteryRangeKM { get => current_battery_range_km; set => current_battery_range_km = value; }
        public double CurrentOutsideTemperature { get => current_outside_temperature; set => current_outside_temperature = value; }
        public int CurrentBatteryLevel { get => current_battery_level; set => current_battery_level = value; }
        public int CurrentChargerVoltage { get => current_charger_voltage; set => current_charger_voltage = value; }
        public int CurrentChargerPhases { get => current_charger_phases; set => current_charger_phases = value; }
        public int CurrentChargerActualCurrent { get => current_charger_actual_current; set => current_charger_actual_current = value; }
        public double CurrentChargeEnergyAdded { get => current_charge_energy_added; set => current_charge_energy_added = value; }
        public int CurrentChargerPower { get => current_charger_power; set => current_charger_power = value; }
        public double CurrentChargeRateKM { get => current_charge_rate_km; set => current_charge_rate_km = value; }
        public double CurrentTimeToFullCharge { get => current_time_to_full_charge; set => current_time_to_full_charge = value; }
        public bool CurrentChargePortDoorOpen { get => current_charge_port_door_open; set => current_charge_port_door_open = value; }
        public string CurrentCarVersion { get => current_car_version; set => current_car_version = value; }
        public DateTime CurrentTripStart { get => current_trip_start; set => current_trip_start = value; }
        public DateTime CurrentTripEnd { get => current_trip_end; set => current_trip_end = value; }
        public double CurrentTripKMStart { get => current_trip_km_start; set => current_trip_km_start = value; }
        public double CurrentTripKMEnd { get => current_trip_km_end; set => current_trip_km_end = value; }
        public double CurrentTripMaxSpeed { get => current_trip_max_speed; set => current_trip_max_speed = value; }
        public double CurrentTripMaxPower { get => current_trip_max_power; set => current_trip_max_power = value; }
        public double CurrentTripStartRange { get => current_trip_start_range; set => current_trip_start_range = value; }
        public double CurrentTripEndRange { get => current_trip_end_range; set => current_trip_end_range = value; }
        public double WhTR { get => wh_TR; set => wh_TR = value; }
        public int CurrentTripDurationSec { get => current_trip_duration_sec; set => current_trip_duration_sec = value; }
        public double Latitude { get => latitude; set => latitude = value; }
        public double Longitude { get => longitude; set => longitude = value; }
        public int ChargeLimitSoc { get => charge_limit_soc; set => charge_limit_soc = value; }
        public int Heading { get => heading; set => heading = value; }
        public double CurrentInsideTemperature { get => current_inside_temperature; set => current_inside_temperature = value; }
        public bool CurrentBatteryHeater { get => current_battery_heater; set => current_battery_heater = value; }
        public bool CurrentIsSentryMode { get => current_is_sentry_mode; set => current_is_sentry_mode = value; }
        public bool CurrentIsPreconditioning { get => current_is_preconditioning; set => current_is_preconditioning = value; }
        public string CurrentCountryCode { get => current_country_code; set => current_country_code = value; }
        public string CurrentState { get => current_state; set => current_state = value; }
        public DateTime LastScanMyTeslaReceived { get => lastScanMyTeslaReceived; set => lastScanMyTeslaReceived = value; }
        public double? SMTCellTempAvg { get => sMTCellTempAvg; set => sMTCellTempAvg = value; }
        public double? SMTCellMinV { get => sMTCellMinV; set => sMTCellMinV = value; }
        public double? SMTCellAvgV { get => sMTCellAvgV; set => sMTCellAvgV = value; }
        public double? SMTCellMaxV { get => sMTCellMaxV; set => sMTCellMaxV = value; }
        public double? SMTCellImbalance { get => sMTCellImbalance; set => sMTCellImbalance = value; }
        public double? SMTBMSmaxCharge { get => sMTBMSmaxCharge; set => sMTBMSmaxCharge = value; }
        public double? SMTBMSmaxDischarge { get => sMTBMSmaxDischarge; set => sMTBMSmaxDischarge = value; }
        public double? SMTACChargeTotal { get => sMTACChargeTotal; set => sMTACChargeTotal = value; }
        public double? SMTDCChargeTotal { get => sMTDCChargeTotal; set => sMTDCChargeTotal = value; }
        public double? SMTNominalFullPack { get => sMTNominalFullPack; set => sMTNominalFullPack = value; }
        public double? SMTSpeed { get => sMTSpeed; set => sMTSpeed = value; }
        public double? SMTBatteryPower { get => sMTBatteryPower; set => sMTBatteryPower = value; }
        public string CurrentJson { get => current_json; set => current_json = value; }

        public void CheckCreateCurrentJSON()
        {
            TimeSpan ts = DateTime.UtcNow - lastJSONwrite;
            if (ts.TotalMinutes > 5)
            {
                CreateCurrentJSON();
            }
        }

        public void CreateCurrentJSON()
        {
            try
            {
                lastJSONwrite = DateTime.UtcNow;

                int duration = 0;
                double distance = 0;
                double trip_kwh = 0.0;
                double trip_avg_wh = 0.0;

                try
                {
                    if (CurrentTripEnd == DateTime.MinValue)
                    {
                        duration = (int)(DateTime.Now - CurrentTripStart).TotalSeconds;
                        distance = CurrentOdometer - CurrentTripKMStart;
                        trip_kwh = (CurrentTripStartRange - CurrentIdealBatteryRangeKM) * WhTR;

                        if (distance > 0)
                        {
                            trip_avg_wh = trip_kwh / distance * 1000;
                        }
                    }
                    else
                    {
                        duration = (int)(CurrentTripEnd - CurrentTripStart).TotalSeconds;
                        distance = CurrentTripKMEnd - CurrentTripKMStart;
                        trip_kwh = (CurrentTripStartRange - CurrentTripEndRange) * WhTR;

                        if (distance > 0)
                        {
                            trip_avg_wh = trip_kwh / distance * 1000;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logfile.Log(ex.ToString());
                    duration = 0;
                }
                if (duration < 0)
                {
                    duration = 0;
                }

                car.GetTeslaAPIState().GetBool("charge_port_door_open", out current_charge_port_door_open);

                Dictionary<string, object> values = new Dictionary<string, object>
                {
                   { "charging", CurrentCharging},
                   { "driving", CurrentDriving },
                   { "online", CurrentOnline },
                   { "sleeping", CurrentSleeping },
                   { "falling_asleep", CurrentFallingAsleep },
                   { "speed", CurrentSpeed},
                   { "power", CurrentPower },
                   { "odometer", CurrentOdometer },
                   { "ideal_battery_range_km", CurrentIdealBatteryRangeKM},
                   { "battery_range_km", CurrentBatteryRangeKM},
                   { "outside_temp", CurrentOutsideTemperature},
                   { "battery_level", CurrentBatteryLevel},
                   { "charger_voltage", CurrentChargerVoltage},
                   { "charger_phases", CurrentChargerPhases},
                   { "charger_actual_current", CurrentChargerActualCurrent},
                   { "charge_energy_added", CurrentChargeEnergyAdded},
                   { "charger_power", CurrentChargerPower},
                   { "charge_rate_km", CurrentChargeRateKM},
                   { "charge_port_door_open", CurrentChargePortDoorOpen },
                   { "time_to_full_charge", CurrentTimeToFullCharge},
                   { "car_version", CurrentCarVersion },
                   { "trip_start", CurrentTripStart.ToString("t",Tools.ciDeDE) },
                   { "trip_start_dt", CurrentTripStart.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", Tools.ciEnUS) },
                   { "trip_max_speed", CurrentTripMaxSpeed },
                   { "trip_max_power", CurrentTripMaxPower },
                   { "trip_duration_sec", duration },
                   { "trip_kwh", trip_kwh },
                   { "trip_avg_kwh", trip_avg_wh },
                   { "trip_distance", distance },
                   { "ts", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", Tools.ciEnUS)},
                   { "latitude", Latitude },
                   { "longitude", Longitude },
                   { "charge_limit_soc", ChargeLimitSoc},
                   { "inside_temperature", CurrentInsideTemperature },
                   { "battery_heater", CurrentBatteryHeater },
                   { "is_preconditioning", CurrentIsPreconditioning },
                   { "sentry_mode", CurrentIsSentryMode },
                   { "country_code", CurrentCountryCode },
                   { "state", CurrentState },
                   { "display_name", car.DisplayName},
                   { "heading", Heading}
                };

                TimeSpan ts = DateTime.Now - LastScanMyTeslaReceived;
                if (ts.TotalMinutes < 5)
                {
                    values.Add("SMTCellTempAvg", SMTCellTempAvg);
                    values.Add("SMTCellMinV", SMTCellMinV);
                    values.Add("SMTCellAvgV", SMTCellAvgV);
                    values.Add("SMTCellMaxV", SMTCellMaxV);
                    values.Add("SMTCellImbalance", SMTCellImbalance);
                    values.Add("SMTBMSmaxCharge", SMTBMSmaxCharge);
                    values.Add("SMTBMSmaxDischarge", SMTBMSmaxDischarge);
                    values.Add("SMTACChargeTotal", SMTACChargeTotal);
                    values.Add("SMTDCChargeTotal", SMTDCChargeTotal);
                    values.Add("SMTNominalFullPack", SMTNominalFullPack);
                }

                Address addr = Geofence.GetInstance().GetPOI(Latitude, Longitude, false);
                if (addr != null && addr.rawName != null)
                {
                    values.Add("TLGeofence", addr.rawName);
                    values.Add("TLGeofenceIsHome", addr.IsHome);
                    values.Add("TLGeofenceIsCharger", addr.IsCharger);
                    values.Add("TLGeofenceIsWork", addr.IsWork);
                }
                else
                {
                    values.Add("TLGeofence", "-");
                    values.Add("TLGeofenceIsHome", false);
                    values.Add("TLGeofenceIsCharger", false);
                    values.Add("TLGeofenceIsWork", false);
                }

                CurrentJson = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(values);

                JsonStringHolder[car.CarInDB] = CurrentJson;

                // FileManager.WriteCurrentJsonFile(car.CarInDB, current_json);
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
                CurrentJson = "";
            }
        }
    }
}
