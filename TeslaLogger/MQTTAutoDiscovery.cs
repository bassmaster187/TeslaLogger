using System;
using System.Collections.Generic;

namespace TeslaLogger
{

    public class MQTTAutoDiscovery
    {
        public static Dictionary<string, Dictionary<string, string>> autoDiscovery = new Dictionary<string, Dictionary<string, string>>()
        {
//            { autoDiscovery.Add("speed", Dictionary<string, string>) }
        };

        public static string getEntityParamValue(string ent, string param)
        {
            autoDiscovery.TryGetValue(ent, out var value);
            value.TryGetValue(param, out var val);
            return val;
        }

        static MQTTAutoDiscovery()
        {
            // Please refere to Home Assistant documentation:
            // https://www.home-assistant.io/integrations/mqtt/#configuration-via-mqtt-discovery
            // https://www.home-assistant.io/integrations/sensor/#device-class


            autoDiscovery["battery_level"] = new Dictionary<string, string>();
            autoDiscovery["battery_level"]["type"] = "sensor";
            autoDiscovery["battery_level"]["name"] = "Battery Level";
            autoDiscovery["battery_level"]["unit"] = "%";
            autoDiscovery["battery_level"]["class"] = "battery";

            autoDiscovery["power"] = new Dictionary<string, string>();
            autoDiscovery["power"]["type"] = "sensor";
            autoDiscovery["power"]["name"] = "Power";
            autoDiscovery["power"]["unit"] = "kW";
            autoDiscovery["power"]["class"] = "power";

            autoDiscovery["charger_power"] = new Dictionary<string, string>();
            autoDiscovery["charger_power"]["type"] = "sensor";
            autoDiscovery["charger_power"]["name"] = "Charge power";
            autoDiscovery["charger_power"]["unit"] = "kW";
            autoDiscovery["charger_power"]["class"] = "power";

            autoDiscovery["charger_voltage"] = new Dictionary<string, string>();
            autoDiscovery["charger_voltage"]["type"] = "sensor";
            autoDiscovery["charger_voltage"]["name"] = "Charge voltage";
            autoDiscovery["charger_voltage"]["unit"] = "V";
            autoDiscovery["charger_voltage"]["class"] = "voltage";

            autoDiscovery["charger_actual_current"] = new Dictionary<string, string>();
            autoDiscovery["charger_actual_current"]["type"] = "sensor";
            autoDiscovery["charger_actual_current"]["name"] = "Charge current";
            autoDiscovery["charger_actual_current"]["unit"] = "A";
            autoDiscovery["charger_actual_current"]["class"] = "current";

            autoDiscovery["charge_energy_added"] = new Dictionary<string, string>();
            autoDiscovery["charge_energy_added"]["type"] = "sensor";
            autoDiscovery["charge_energy_added"]["name"] = "Energy added";
            autoDiscovery["charge_energy_added"]["unit"] = "kWh";
            autoDiscovery["charge_energy_added"]["class"] = "energy";

            autoDiscovery["charger_phases"] = new Dictionary<string, string>();
            autoDiscovery["charger_phases"]["type"] = "sensor";
            autoDiscovery["charger_phases"]["name"] = "Charge phases";
            autoDiscovery["charger_phases"]["unit"] = "";
            autoDiscovery["charger_phases"]["class"] = "None";

            autoDiscovery["charge_rate_km"] = new Dictionary<string, string>();
            autoDiscovery["charge_rate_km"]["type"] = "sensor";
            autoDiscovery["charge_rate_km"]["name"] = "Charge rate";
            autoDiscovery["charge_rate_km"]["unit"] = "km";
            autoDiscovery["charge_rate_km"]["class"] = "distance";

            autoDiscovery["time_to_full_charge"] = new Dictionary<string, string>();
            autoDiscovery["time_to_full_charge"]["type"] = "sensor";
            autoDiscovery["time_to_full_charge"]["name"] = "Time to full charge";
            autoDiscovery["time_to_full_charge"]["unit"] = "";
            autoDiscovery["time_to_full_charge"]["class"] = "duration";

            autoDiscovery["car_version"] = new Dictionary<string, string>();
            autoDiscovery["car_version"]["type"] = "sensor";
            autoDiscovery["car_version"]["name"] = "Firmware version";
            autoDiscovery["car_version"]["unit"] = "";
            autoDiscovery["car_version"]["class"] = "None";

            autoDiscovery["odometer"] = new Dictionary<string, string>();
            autoDiscovery["odometer"]["type"] = "sensor";
            autoDiscovery["odometer"]["name"] = "Odometer";
            autoDiscovery["odometer"]["unit"] = "km";
            autoDiscovery["odometer"]["class"] = "distance";

            autoDiscovery["battery_range_km"] = new Dictionary<string, string>();
            autoDiscovery["battery_range_km"]["type"] = "sensor";
            autoDiscovery["battery_range_km"]["name"] = "Battery range";
            autoDiscovery["battery_range_km"]["unit"] = "km";
            autoDiscovery["battery_range_km"]["class"] = "distance";

            autoDiscovery["ideal_battery_range_km"] = new Dictionary<string, string>();
            autoDiscovery["ideal_battery_range_km"]["type"] = "sensor";
            autoDiscovery["ideal_battery_range_km"]["name"] = "Ideal battery range";
            autoDiscovery["ideal_battery_range_km"]["unit"] = "km";
            autoDiscovery["ideal_battery_range_km"]["class"] = "distance";

            autoDiscovery["inside_temperature"] = new Dictionary<string, string>();
            autoDiscovery["inside_temperature"]["type"] = "sensor";
            autoDiscovery["inside_temperature"]["name"] = "Inside temperature";
            autoDiscovery["inside_temperature"]["unit"] = "°C";
            autoDiscovery["inside_temperature"]["class"] = "temperature";

            autoDiscovery["outside_temp"] = new Dictionary<string, string>();
            autoDiscovery["outside_temp"]["type"] = "sensor";
            autoDiscovery["outside_temp"]["name"] = "Outside temperature";
            autoDiscovery["outside_temp"]["unit"] = "°C";
            autoDiscovery["outside_temp"]["class"] = "temperature";

            autoDiscovery["ts"] = new Dictionary<string, string>();
            autoDiscovery["ts"]["type"] = "sensor";
            autoDiscovery["ts"]["name"] = "Time stamp";
            autoDiscovery["ts"]["unit"] = "";
            autoDiscovery["ts"]["class"] = "timestamp";

            autoDiscovery["speed"] = new Dictionary<string, string>();
            autoDiscovery["speed"]["type"] = "sensor";
            autoDiscovery["speed"]["name"] = "Speed";
            autoDiscovery["speed"]["unit"] = "km/h";
            autoDiscovery["speed"]["class"] = "speed";

            autoDiscovery["sleeping"] = new Dictionary<string, string>();
            autoDiscovery["sleeping"]["type"] = "bool";
            autoDiscovery["sleeping"]["name"] = "Sleeping";

            autoDiscovery["online"] = new Dictionary<string, string>();
            autoDiscovery["online"]["type"] = "bool";
            autoDiscovery["online"]["name"] = "Online";

            autoDiscovery["driving"] = new Dictionary<string, string>();
            autoDiscovery["driving"]["type"] = "bool";
            autoDiscovery["driving"]["name"] = "Driving";

            autoDiscovery["falling_asleep"] = new Dictionary<string, string>();
            autoDiscovery["falling_asleep"]["type"] = "bool";
            autoDiscovery["falling_asleep"]["name"] = "Falling Asleep";

            autoDiscovery["plugged_in"] = new Dictionary<string, string>();
            autoDiscovery["plugged_in"]["type"] = "bool";
            autoDiscovery["plugged_in"]["name"] = "Plugged in";

            autoDiscovery["locked"] = new Dictionary<string, string>();
            autoDiscovery["locked"]["type"] = "bool";
            autoDiscovery["locked"]["name"] = "Locked";

            autoDiscovery["open_windows"] = new Dictionary<string, string>();
            autoDiscovery["open_windows"]["type"] = "bool";
            autoDiscovery["open_windows"]["name"] = "Windows opened";

            autoDiscovery["open_doors"] = new Dictionary<string, string>();
            autoDiscovery["open_doors"]["type"] = "bool";
            autoDiscovery["open_doors"]["name"] = "Doors opened";

            autoDiscovery["frunk"] = new Dictionary<string, string>();
            autoDiscovery["frunk"]["type"] = "bool";
            autoDiscovery["frunk"]["name"] = "Frunk opened";

            autoDiscovery["trunk"] = new Dictionary<string, string>();
            autoDiscovery["trunk"]["type"] = "bool";
            autoDiscovery["trunk"]["name"] = "Trunk opened";

            autoDiscovery["charge_port_door_open"] = new Dictionary<string, string>();
            autoDiscovery["charge_port_door_open"]["type"] = "bool";
            autoDiscovery["charge_port_door_open"]["name"] = "Charge port opened";

            autoDiscovery["fast_charger_present"] = new Dictionary<string, string>();
            autoDiscovery["fast_charger_present"]["type"] = "bool";
            autoDiscovery["fast_charger_present"]["name"] = "Fast charger";

            autoDiscovery["fast_charger_brand"] = new Dictionary<string, string>();
            autoDiscovery["fast_charger_brand"]["type"] = "sensor";
            autoDiscovery["fast_charger_brand"]["name"] = "Fastcharger brand";
            autoDiscovery["fast_charger_brand"]["unit"] = "";
            autoDiscovery["fast_charger_brand"]["class"] = "None";

            autoDiscovery["sentry_mode"] = new Dictionary<string, string>();
            autoDiscovery["sentry_mode"]["type"] = "onoff";
            autoDiscovery["sentry_mode"]["name"] = "Sentry mode";
            autoDiscovery["sentry_mode"]["pl_on"] = "On";
            autoDiscovery["sentry_mode"]["pl_off"] = "Off";
            autoDiscovery["sentry_mode"]["cmd_topic"] = "sentry_mode_on_off";
            autoDiscovery["sentry_mode"]["class"] = "None";

            autoDiscovery["charge_limit_soc"] = new Dictionary<string, string>();
            autoDiscovery["charge_limit_soc"]["type"] = "number";
            autoDiscovery["charge_limit_soc"]["name"] = "Charge limit state of charge";
            autoDiscovery["charge_limit_soc"]["cmd_topic"] = "set_charge_limit";
            autoDiscovery["charge_limit_soc"]["class"] = "battery";
            autoDiscovery["charge_limit_soc"]["min"] = "50";
            autoDiscovery["charge_limit_soc"]["max"] = "100";
            autoDiscovery["charge_limit_soc"]["step"] = "1";
            /*
            autoDiscovery["charge_limit_soc"] = new Dictionary<string, string>();
            autoDiscovery["charge_limit_soc"]["type"] = "number";
            autoDiscovery["charge_limit_soc"]["name"] = "Charge current";
            autoDiscovery["charge_limit_soc"]["cmd_topic"] = "set_charging_amps";
            autoDiscovery["charge_limit_soc"]["class"] = "current";
            autoDiscovery["charge_limit_soc"]["min"] = "6";
            autoDiscovery["charge_limit_soc"]["max"] = "32";
            autoDiscovery["charge_limit_soc"]["step"] = "1";
            */
        }

    }
}

