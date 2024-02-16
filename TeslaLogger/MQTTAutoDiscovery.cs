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
            autoDiscovery["battery_level"] = new Dictionary<string, string>();
            autoDiscovery["battery_level"]["type"] = "sensor";
            autoDiscovery["battery_level"]["name"] = "Battery Level";
            autoDiscovery["battery_level"]["unit"] = "%";
            autoDiscovery["battery_level"]["class"] = "battery";

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
            autoDiscovery["falling_asleep"]["name"] = "Ffalling Asleep";

            autoDiscovery["plugged_in"] = new Dictionary<string, string>();
            autoDiscovery["plugged_in"]["type"] = "bool";
            autoDiscovery["plugged_in"]["name"] = "Plugged in";

            autoDiscovery["locked"] = new Dictionary<string, string>();
            autoDiscovery["locked"]["type"] = "bool";
            autoDiscovery["locked"]["name"] = "Locked";

            autoDiscovery["sentry_mode"] = new Dictionary<string, string>();
            autoDiscovery["sentry_mode"]["type"] = "onoff";
            autoDiscovery["sentry_mode"]["name"] = "Sentry mode";
            autoDiscovery["sentry_mode"]["pl_on"] = "On";
            autoDiscovery["sentry_mode"]["pl_off"] = "Off";
            autoDiscovery["sentry_mode"]["cmd_topic"] = "charge_limit_soc";
            autoDiscovery["sentry_mode"]["class"] = "None";

            autoDiscovery["charge_limit_soc"] = new Dictionary<string, string>();
            autoDiscovery["charge_limit_soc"]["type"] = "number";
            autoDiscovery["charge_limit_soc"]["name"] = "Charge limit state of charge";
            autoDiscovery["charge_limit_soc"]["cmd_topic"] = "charge_limit_soc";
            autoDiscovery["charge_limit_soc"]["class"] = "battery";
        }

    }
}

