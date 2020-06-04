using System;
using System.Collections.Generic;
using System.Text;

namespace TeslaLogger
{
    public class CurrentJSON
    {      
        public bool current_charging = false;
        public bool current_driving = false;
        public bool current_online = false;
        public bool current_sleeping = false;
        public bool current_falling_asleep = false;

        public int current_speed = 0;
        public int current_power = 0;
        public double current_odometer = 0;
        public double current_ideal_battery_range_km = 0;
        public double current_outside_temp = 0;
        public double current_inside_temp = 0;
        public int current_battery_level = 0;

        public int current_charger_voltage = 0;
        public int current_charger_phases = 0;
        public int current_charger_actual_current = 0;
        public double current_charge_energy_added = 0;
        public int current_charger_power = 0;

        public string current_car_version = "";

        public DateTime current_trip_start = DateTime.MinValue;
        public DateTime current_trip_end = DateTime.MinValue;
        public double current_trip_km_start = 0;
        public double current_trip_km_end = 0;
        public double current_trip_max_speed = 0;
        public double current_trip_max_power = 0;
        public double current_trip_start_range = 0;
        public double current_trip_end_range = 0;
        public double Wh_TR = 0.19;

        public int current_trip_duration_sec = 0;

        public double latitude = 0;
        public double longitude = 0;
        public int charge_limit_soc = 0;
        public double current_inside_temperature = 0;
        public bool current_battery_heater = false;
        public bool current_is_sentry_mode = false;
        public bool current_is_preconditioning = false;

        public string current_country_code = "";
        public string current_state = "";

        public DateTime lastScanMyTeslaReceived = DateTime.MinValue;
        public double? SMTCellTempAvg = null;
        public double? SMTCellMinV = null;
        public double? SMTCellAvgV = null;
        public double? SMTCellMaxV = null;
        public double? SMTBMSmaxCharge = null;
        public double? SMTBMSmaxDischarge = null;

        public double? SMTSpeed = null;
        public double? SMTBatteryPower = null;

        public string current_json = "";
        private DateTime lastJSONwrite = DateTime.MinValue;

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
                    if (current_trip_end == DateTime.MinValue)
                    {
                        duration = (int)(DateTime.Now - current_trip_start).TotalSeconds;
                        distance = current_odometer - current_trip_km_start;
                        trip_kwh = (current_trip_start_range - current_ideal_battery_range_km) * Wh_TR;

                        if (distance > 0)
                        {
                            trip_avg_wh = trip_kwh / distance * 1000;
                        }
                    }
                    else
                    {
                        duration = (int)(current_trip_end - current_trip_start).TotalSeconds;
                        distance = current_trip_km_end - current_trip_km_start;
                        trip_kwh = (current_trip_start_range - current_trip_end_range) * Wh_TR;

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

                Dictionary<string, object> values = new Dictionary<string, object>
                {
                   { "charging", current_charging},
                   { "driving", current_driving },
                   { "online", current_online },
                   { "sleeping", current_sleeping },
                   { "falling_asleep", current_falling_asleep },
                   { "speed", current_speed},
                   { "power", current_power },
                   { "odometer", current_odometer },
                   { "ideal_battery_range_km", current_ideal_battery_range_km},
                   { "outside_temp", current_outside_temp},
                   { "battery_level", current_battery_level},
                   { "charger_voltage", current_charger_voltage},
                   { "charger_phases", current_charger_phases},
                   { "charger_actual_current", current_charger_actual_current},
                   { "charge_energy_added", current_charge_energy_added},
                   { "charger_power", current_charger_power},
                   { "car_version", current_car_version },
                   { "trip_start", current_trip_start.ToString("t",Tools.ciDeDE) },
                   { "trip_start_dt", current_trip_start.ToString("s") },
                   { "trip_max_speed", current_trip_max_speed },
                   { "trip_max_power", current_trip_max_power },
                   { "trip_duration_sec", duration },
                   { "trip_kwh", trip_kwh },
                   { "trip_avg_kwh", trip_avg_wh },
                   { "trip_distance", distance },
                   { "ts", DateTime.Now.ToString("s")},
                   { "latitude", latitude },
                   { "longitude", longitude },
                   { "charge_limit_soc", charge_limit_soc},
                   { "inside_temperature", current_inside_temperature },
                   { "battery_heater", current_battery_heater },
                   { "is_preconditioning", current_is_preconditioning },
                   { "sentry_mode", current_is_sentry_mode },
                   { "country_code", current_country_code },
                   { "state", current_state }
                };

                TimeSpan ts = DateTime.Now - lastScanMyTeslaReceived;
                if (ts.TotalMinutes < 5)
                {
                    values.Add("SMTCellTempAvg", SMTCellTempAvg);
                    values.Add("SMTCellMinV", SMTCellMinV);
                    values.Add("SMTCellAvgV", SMTCellAvgV);
                    values.Add("SMTCellMaxV", SMTCellMaxV);
                    values.Add("SMTBMSmaxCharge", SMTBMSmaxCharge);
                    values.Add("SMTBMSmaxDischarge", SMTBMSmaxDischarge);
                }

                current_json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(values);

                FileManager.WriteCurrentJsonFile(new JsonFormatter(current_json).Format());
            }
            catch (Exception ex)
            {
                Logfile.Log(ex.ToString());
                current_json = "";
            }
        }

        // source: https://www.limilabs.com/blog/json-net-formatter
        // license: You can do whatever you want with it. (lesnikowski@limilabs.com)

        private class StringWalker
        {
            private readonly string _s;

            public int Index { get; private set; }
            public bool IsEscaped { get; private set; }
            public char CurrentChar { get; private set; }

            public StringWalker(string s)
            {
                _s = s;
                Index = -1;
            }

            public bool MoveNext()
            {
                if (Index == _s.Length - 1)
                {
                    return false;
                }

                IsEscaped = IsEscaped == false && CurrentChar == '\\';
                Index++;
                CurrentChar = _s[Index];
                return true;
            }
        };

        private class IndentWriter
        {
            private readonly StringBuilder _result = new StringBuilder();
            private int _indentLevel;

            public void Indent()
            {
                _indentLevel++;
            }

            public void UnIndent()
            {
                if (_indentLevel > 0)
                {
                    _indentLevel--;
                }
            }

            public void WriteLine(string line)
            {
                _result.AppendLine(CreateIndent() + line);
            }

            private string CreateIndent()
            {
                StringBuilder indent = new StringBuilder();
                for (int i = 0; i < _indentLevel; i++)
                {
                    indent.Append("    ");
                }

                return indent.ToString();
            }

            public override string ToString()
            {
                return _result.ToString();
            }
        };

        private class JsonFormatter
        {
            private readonly StringWalker _walker;
            private readonly IndentWriter _writer = new IndentWriter();
            private readonly StringBuilder _currentLine = new StringBuilder();
            private bool _quoted;

            public JsonFormatter(string json)
            {
                _walker = new StringWalker(json);
                ResetLine();
            }

            public void ResetLine()
            {
                _currentLine.Length = 0;
            }

            public string Format()
            {
                while (MoveNextChar())
                {
                    if (_quoted == false && IsOpenBracket())
                    {
                        WriteCurrentLine();
                        AddCharToLine();
                        WriteCurrentLine();
                        _writer.Indent();
                    }
                    else if (_quoted == false && IsCloseBracket())
                    {
                        WriteCurrentLine();
                        _writer.UnIndent();
                        AddCharToLine();
                    }
                    else if (_quoted == false && IsColon())
                    {
                        AddCharToLine();
                        WriteCurrentLine();
                    }
                    else
                    {
                        AddCharToLine();
                    }
                }
                WriteCurrentLine();
                return _writer.ToString();
            }

            private bool MoveNextChar()
            {
                bool success = _walker.MoveNext();
                if (IsApostrophe())
                {
                    _quoted = !_quoted;
                }
                return success;
            }

            public bool IsApostrophe()
            {
                return _walker.CurrentChar == '"' && _walker.IsEscaped == false;
            }

            public bool IsOpenBracket()
            {
                return _walker.CurrentChar == '{'
                    || _walker.CurrentChar == '[';
            }

            public bool IsCloseBracket()
            {
                return _walker.CurrentChar == '}'
                    || _walker.CurrentChar == ']';
            }

            public bool IsColon()
            {
                return _walker.CurrentChar == ',';
            }

            private void AddCharToLine()
            {
                _currentLine.Append(_walker.CurrentChar);
            }

            private void WriteCurrentLine()
            {
                string line = _currentLine.ToString().Trim();
                if (line.Length > 0)
                {
                    _writer.WriteLine(line);
                }
                ResetLine();
            }
        };
    }
}
