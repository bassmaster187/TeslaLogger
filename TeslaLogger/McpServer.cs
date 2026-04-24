using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace TeslaLogger
{
    /// <summary>
    /// MCP (Model Context Protocol) Server – exposes vehicles, trips and charging sessions as tools.
    /// Runs on a separate HTTP port (default: HttpPort + 1).
    /// Transport: Streamable HTTP (JSON-RPC 2.0 via POST).
    /// </summary>
    public class McpServer : IDisposable
    {
        private HttpListener _listener;
        private Thread _thread;
        private volatile bool _running;
        private readonly int _port;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        };

        public McpServer()
        {
            _port = Tools.GetHttpPort() + 1;
        }

        public void Start()
        {
            _thread = new Thread(Run) { IsBackground = true, Name = "McpServer" };
            _thread.Start();
        }

        private void Run()
        {
            _running = true;

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://*:{_port}/");
                _listener.Start();
                Logfile.Log($"MCP Server listening on http://*:{_port}/");
            }
            catch (Exception ex)
            {
                Logfile.Log($"MCP Server failed to bind to *:{_port}, trying localhost...");
                try
                {
                    _listener = new HttpListener();
                    _listener.Prefixes.Add($"http://localhost:{_port}/");
                    _listener.Start();
                    Logfile.Log($"MCP Server listening on http://localhost:{_port}/");
                }
                catch (Exception ex2)
                {
                    Logfile.Log("MCP Server failed to start: " + ex2.Message);
                    return;
                }
            }

            while (_running)
            {
                try
                {
                    var context = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(HandleRequest, context);
                }
                catch (HttpListenerException) when (!_running)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logfile.Log("MCP Server error: " + ex.Message);
                }
            }
        }

        private void HandleRequest(object state)
        {
            var context = (HttpListenerContext)state;
            var request = context.Request;
            var response = context.Response;

            // CORS headers for MCP clients
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept");

            try
            {
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 204;
                    response.Close();
                    return;
                }

                if (request.HttpMethod == "GET" && request.Url.LocalPath == "/")
                {
                    // Health check / info
                    WriteJsonResponse(response, new { status = "ok", server = "TeslaLogger MCP Server", port = _port });
                    return;
                }

                // MCP Streamable HTTP: POST to /mcp
                if (request.HttpMethod == "POST" && request.Url.LocalPath == "/mcp")
                {
                    string body;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        body = reader.ReadToEnd();
                    }

                    JObject jsonRpcRequest;
                    try
                    {
                        jsonRpcRequest = JObject.Parse(body);
                    }
                    catch (Exception ex)
                    {
                        Logfile.Log($"MCP Request: invalid JSON body: {body}");
                        throw new InvalidOperationException("Invalid JSON-RPC request body", ex);
                    }

                    LogMcpRequest(jsonRpcRequest);
                    var result = ProcessJsonRpc(jsonRpcRequest);
                    WriteJsonResponse(response, result);
                    return;
                }

                response.StatusCode = 404;
                WriteJsonResponse(response, new { error = "Not Found" });
            }
            catch (Exception ex)
            {
                Logfile.Log("MCP request error: " + ex.Message);
                try
                {
                    response.StatusCode = 500;
                    WriteJsonResponse(response, JsonRpcError(null, -32603, "Internal error: " + ex.Message));
                }
                catch { }
            }
        }

        private static void LogMcpRequest(JObject request)
        {
            try
            {
                string method = request["method"]?.ToString() ?? "<null>";
                string id = request["id"]?.ToString() ?? "<null>";
                string parameters = request["params"]?.ToString(Formatting.None) ?? "{}";
                Logfile.Log($"MCP Request: method={method} id={id} params={parameters}");
            }
            catch (Exception ex)
            {
                Logfile.Log("MCP Request logging failed: " + ex.Message);
            }
        }

        private object ProcessJsonRpc(JObject request)
        {
            var id = request["id"];
            var method = request["method"]?.ToString();
            var parameters = request["params"] as JObject;

            switch (method)
            {
                case "initialize":
                    return JsonRpcResult(id, new
                    {
                        protocolVersion = "2025-03-26",
                        capabilities = new
                        {
                            tools = new { }
                        },
                        serverInfo = new
                        {
                            name = "TeslaLogger",
                            version = "1.0.0"
                        }
                    });

                case "notifications/initialized":
                    // Notification – no response needed, but since we're over HTTP we return empty
                    return JsonRpcResult(id, new { });

                case "tools/list":
                    return JsonRpcResult(id, new
                    {
                        tools = GetToolDefinitions()
                    });

                case "tools/call":
                    return HandleToolCall(id, parameters);

                case "ping":
                    return JsonRpcResult(id, new { });

                default:
                    return JsonRpcError(id, -32601, $"Method not found: {method}");
            }
        }

        private object[] GetToolDefinitions()
        {
            return new object[]
            {
                new
                {
                    name = "get_vehicles",
                    description = "Retrieve all vehicles from TeslaLogger. Returns ID, display name, VIN, model and status.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = Array.Empty<string>()
                    }
                },
                new
                {
                    name = "get_trips",
                    description = "Retrieve trips for a vehicle. Returns start/destination, distance, consumption, duration and temperatures. Use 'from'/'to' for a specific date range, or 'days' to look back from now.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            car_id = new { type = "integer", description = "Vehicle ID (from get_vehicles)" },
                            days = new { type = "integer", description = "Number of days to look back from now (default: 7, ignored if 'from' is set)" },
                            from = new { type = "string", description = "Start date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss" },
                            to = new { type = "string", description = "End date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss (default: now)" }
                        },
                        required = new[] { "car_id" }
                    }
                },
                new
                {
                    name = "get_charges",
                    description = "Retrieve charging sessions for a vehicle. Returns date, location, kWh charged, costs and duration. Use 'from'/'to' for a specific date range, or 'days' to look back from now.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            car_id = new { type = "integer", description = "Vehicle ID (from get_vehicles)" },
                            days = new { type = "integer", description = "Number of days to look back from now (default: 7, ignored if 'from' is set)" },
                            from = new { type = "string", description = "Start date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss" },
                            to = new { type = "string", description = "End date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss (default: now)" }
                        },
                        required = new[] { "car_id" }
                    }
                },
                new
                {
                    name = "get_errors",
                    description = "Retrieve vehicle alerts/errors for a vehicle. Use 'from'/'to' for a specific date range, or 'days' to look back from now.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            car_id = new { type = "integer", description = "Vehicle ID (from get_vehicles)" },
                            days = new { type = "integer", description = "Number of days to look back from now (default: 7, ignored if 'from' is set)" },
                            from = new { type = "string", description = "Start date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss" },
                            to = new { type = "string", description = "End date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss (default: now)" }
                        },
                        required = new[] { "car_id" }
                    }
                },
                new
                {
                    name = "get_degradation",
                    description = "Retrieve degradation-related charging data for a vehicle. Returns timestamp, calculated max range (km), and odometer (km). Use 'from'/'to' for a specific date range, or 'days' to look back from now.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            car_id = new { type = "integer", description = "Vehicle ID (from get_vehicles)" },
                            days = new { type = "integer", description = "Number of days to look back from now (default: 7, ignored if 'from' is set)" },
                            from = new { type = "string", description = "Start date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss" },
                            to = new { type = "string", description = "End date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss (default: now)" }
                        },
                        required = new[] { "car_id" }
                    }
                },
                new
                {
                    name = "get_firmware",
                    description = "Retrieve firmware history for a vehicle. Returns start date and firmware version. Use 'from'/'to' for a specific date range, or 'days' to look back from now.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            car_id = new { type = "integer", description = "Vehicle ID (from get_vehicles)" },
                            days = new { type = "integer", description = "Number of days to look back from now (default: 7, ignored if 'from' is set)" },
                            from = new { type = "string", description = "Start date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss" },
                            to = new { type = "string", description = "End date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss (default: now)" }
                        },
                        required = new[] { "car_id" }
                    }
                },
                new
                {
                    name = "get_tpms",
                    description = "Retrieve TPMS history for a vehicle. Returns start/end date and TPMS values for FL/FR/RL/RR. Use 'from'/'to' for a specific date range, or 'days' to look back from now.",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            car_id = new { type = "integer", description = "Vehicle ID (from get_vehicles)" },
                            days = new { type = "integer", description = "Number of days to look back from now (default: 7, ignored if 'from' is set)" },
                            from = new { type = "string", description = "Start date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss" },
                            to = new { type = "string", description = "End date/time in format yyyy-MM-dd or yyyy-MM-dd HH:mm:ss (default: now)" }
                        },
                        required = new[] { "car_id" }
                    }
                }
            };
        }

        private object HandleToolCall(JToken id, JObject parameters)
        {
            var toolName = parameters?["name"]?.ToString();
            var arguments = parameters?["arguments"] as JObject ?? new JObject();

            try
            {
                switch (toolName)
                {
                    case "get_vehicles":
                        return ToolResult(id, GetVehicles());

                    case "get_trips":
                    {
                        int carId = arguments["car_id"]?.Value<int>() ?? 0;
                        if (carId <= 0) return ToolError(id, "car_id is required and must be > 0");
                        var (from, to) = ParseDateRange(arguments);
                        return ToolResult(id, GetTrips(carId, from, to));
                    }

                    case "get_charges":
                    {
                        int carId = arguments["car_id"]?.Value<int>() ?? 0;
                        if (carId <= 0) return ToolError(id, "car_id is required and must be > 0");
                        var (from, to) = ParseDateRange(arguments);
                        return ToolResult(id, GetCharges(carId, from, to));
                    }

                    case "get_errors":
                    {
                        int carId = arguments["car_id"]?.Value<int>() ?? 0;
                        if (carId <= 0) return ToolError(id, "car_id is required and must be > 0");
                        var (from, to) = ParseDateRange(arguments);
                        return ToolResult(id, GetErrors(carId, from, to));
                    }

                    case "get_degradation":
                    {
                        int carId = arguments["car_id"]?.Value<int>() ?? 0;
                        if (carId <= 0) return ToolError(id, "car_id is required and must be > 0");
                        var (from, to) = ParseDateRange(arguments);
                        return ToolResult(id, GetDegradation(carId, from, to));
                    }

                    case "get_firmware":
                    {
                        int carId = arguments["car_id"]?.Value<int>() ?? 0;
                        if (carId <= 0) return ToolError(id, "car_id is required and must be > 0");
                        var (from, to) = ParseDateRange(arguments);
                        return ToolResult(id, GetFirmware(carId, from, to));
                    }

                    case "get_tpms":
                    {
                        int carId = arguments["car_id"]?.Value<int>() ?? 0;
                        if (carId <= 0) return ToolError(id, "car_id is required and must be > 0");
                        var (from, to) = ParseDateRange(arguments);
                        return ToolResult(id, GetTpms(carId, from, to));
                    }

                    default:
                        return JsonRpcError(id, -32602, $"Unknown tool: {toolName}");
                }
            }
            catch (Exception ex)
            {
                Logfile.Log($"MCP tool error ({toolName}): {ex.Message}");
                return ToolError(id, ex.Message);
            }
        }

        #region Tool Implementations

        private string GetVehicles()
        {
            var vehicles = new List<object>();
            using (var con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (var cmd = new MySqlCommand(
                    "SELECT id, display_name, vin, model_name, car_type, freesuc, fleetAPI FROM cars ORDER BY id", con))
                {
                    using (var dr = SQLTracer.TraceDR(cmd))
                    {
                        while (dr.Read())
                        {
                            vehicles.Add(new
                            {
                                id = dr.GetInt32(0),
                                display_name = dr.IsDBNull(1) ? "" : dr.GetString(1),
                                vin = dr.IsDBNull(2) ? "" : dr.GetString(2),
                                model_name = dr.IsDBNull(3) ? "" : dr.GetString(3),
                                car_type = dr.IsDBNull(4) ? "" : dr.GetString(4),
                                free_supercharging = !dr.IsDBNull(5) && dr.GetInt32(5) == 1,
                                fleet_api = !dr.IsDBNull(6) && dr.GetInt32(6) == 1
                            });
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(vehicles, Formatting.Indented);
        }

        private static (DateTime from, DateTime to) ParseDateRange(JObject arguments)
        {
            string fromStr = arguments["from"]?.Value<string>();
            string toStr = arguments["to"]?.Value<string>();
            int days = arguments["days"]?.Value<int>() ?? 7;

            DateTime to = DateTime.Now;
            DateTime from = to.AddDays(-days);

            if (!string.IsNullOrEmpty(fromStr) && DateTime.TryParse(fromStr, out DateTime parsedFrom))
            {
                from = parsedFrom;
            }
            if (!string.IsNullOrEmpty(toStr) && DateTime.TryParse(toStr, out DateTime parsedTo))
            {
                to = parsedTo;
            }

            return (from, to);
        }

        private string GetTrips(int carId, DateTime from, DateTime to)
        {
            var trips = new List<object>();
            using (var con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (var cmd = new MySqlCommand(@"
SELECT 
    ds.StartDate,
    ds.EndDate,
    ps.address AS start_address,
    pe.address AS end_address,
    ps.lat AS start_lat,
    ps.lng AS start_lng,
    pe.lat AS end_lat,
    pe.lng AS end_lng,
    ROUND(pe.odometer - ps.odometer, 2) AS distance_km,
    ROUND((ps.ideal_battery_range_km - pe.ideal_battery_range_km) * c.wh_tr / 1000, 2) AS consumption_kWh,
    TIMESTAMPDIFF(MINUTE, ds.StartDate, ds.EndDate) AS duration_minutes,
    ds.outside_temp_avg,
    ds.speed_max,
    ds.power_max,
    ds.power_min,
    ds.power_avg,
    ps.odometer AS start_km,
    pe.odometer AS end_km
FROM drivestate ds
JOIN pos ps ON ds.StartPos = ps.id
JOIN pos pe ON ds.EndPos = pe.id
JOIN cars c ON c.id = ds.CarID
WHERE ds.CarID = @carId
  AND ds.StartDate >= @from
  AND ds.StartDate <= @to
  AND (pe.odometer - ps.odometer) > 0.1
ORDER BY ds.StartDate DESC", con))
                {
                    cmd.Parameters.AddWithValue("@carId", carId);
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@to", to);
                    using (var dr = SQLTracer.TraceDR(cmd))
                    {
                        while (dr.Read())
                        {
                            trips.Add(new
                            {
                                start_date = dr.IsDBNull(0) ? "" : ((DateTime)dr[0]).ToString("yyyy-MM-dd HH:mm:ss"),
                                end_date = dr.IsDBNull(1) ? "" : ((DateTime)dr[1]).ToString("yyyy-MM-dd HH:mm:ss"),
                                start_address = dr.IsDBNull(2) ? "" : dr.GetString(2),
                                end_address = dr.IsDBNull(3) ? "" : dr.GetString(3),
                                start_lat = dr.IsDBNull(4) ? 0.0 : dr.GetDouble(4),
                                start_lng = dr.IsDBNull(5) ? 0.0 : dr.GetDouble(5),
                                end_lat = dr.IsDBNull(6) ? 0.0 : dr.GetDouble(6),
                                end_lng = dr.IsDBNull(7) ? 0.0 : dr.GetDouble(7),
                                distance_km = dr.IsDBNull(8) ? 0.0 : Convert.ToDouble(dr[8]),
                                consumption_kWh = dr.IsDBNull(9) ? 0.0 : Convert.ToDouble(dr[9]),
                                duration_minutes = dr.IsDBNull(10) ? 0 : Convert.ToInt32(dr[10]),
                                outside_temp_avg = dr.IsDBNull(11) ? (double?)null : Convert.ToDouble(dr[11]),
                                speed_max = dr.IsDBNull(12) ? (int?)null : Convert.ToInt32(dr[12]),
                                power_max = dr.IsDBNull(13) ? (int?)null : Convert.ToInt32(dr[13]),
                                power_min = dr.IsDBNull(14) ? (int?)null : Convert.ToInt32(dr[14]),
                                power_avg = dr.IsDBNull(15) ? (double?)null : Convert.ToDouble(dr[15]),
                                start_km = dr.IsDBNull(16) ? 0.0 : Convert.ToDouble(dr[16]),
                                end_km = dr.IsDBNull(17) ? 0.0 : Convert.ToDouble(dr[17])
                            });
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(trips, Formatting.Indented);
        }

        private string GetCharges(int carId, DateTime from, DateTime to)
        {
            var charges = new List<object>();
            using (var con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (var cmd = new MySqlCommand(@"
SELECT 
    cs.StartDate,
    cs.EndDate,
    p.address,
    p.lat,
    p.lng,
    cs.charge_energy_added,
    cs.cost_total,
    cs.cost_currency,
    cs.cost_per_kwh,
    cs.cost_per_session,
    cs.cost_per_minute,
    cs.cost_idle_fee_total,
    cs.cost_kwh_meter_invoice,
    TIMESTAMPDIFF(MINUTE, cs.StartDate, cs.EndDate) AS duration_minutes,
    cs.max_charger_power,
    cs.StartChargingID,
    cs.EndChargingID
FROM chargingstate cs
JOIN pos p ON cs.pos = p.id
WHERE cs.CarID = @carId
  AND cs.StartDate >= @from
  AND cs.StartDate <= @to
ORDER BY cs.StartDate DESC", con))
                {
                    cmd.Parameters.AddWithValue("@carId", carId);
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@to", to);
                    using (var dr = SQLTracer.TraceDR(cmd))
                    {
                        while (dr.Read())
                        {
                            charges.Add(new
                            {
                                start_date = dr.IsDBNull(0) ? "" : ((DateTime)dr[0]).ToString("yyyy-MM-dd HH:mm:ss"),
                                end_date = dr.IsDBNull(1) ? "" : ((DateTime)dr[1]).ToString("yyyy-MM-dd HH:mm:ss"),
                                address = dr.IsDBNull(2) ? "" : dr.GetString(2),
                                lat = dr.IsDBNull(3) ? 0.0 : dr.GetDouble(3),
                                lng = dr.IsDBNull(4) ? 0.0 : dr.GetDouble(4),
                                charge_energy_added_kWh = dr.IsDBNull(5) ? 0.0 : Convert.ToDouble(dr[5]),
                                cost_total = dr.IsDBNull(6) ? (double?)null : Convert.ToDouble(dr[6]),
                                cost_currency = dr.IsDBNull(7) ? null : dr.GetString(7),
                                cost_per_kwh = dr.IsDBNull(8) ? (double?)null : Convert.ToDouble(dr[8]),
                                cost_per_session = dr.IsDBNull(9) ? (double?)null : Convert.ToDouble(dr[9]),
                                cost_per_minute = dr.IsDBNull(10) ? (double?)null : Convert.ToDouble(dr[10]),
                                cost_idle_fee_total = dr.IsDBNull(11) ? (double?)null : Convert.ToDouble(dr[11]),
                                cost_kwh_meter_invoice = dr.IsDBNull(12) ? (double?)null : Convert.ToDouble(dr[12]),
                                duration_minutes = dr.IsDBNull(13) ? 0 : Convert.ToInt32(dr[13]),
                                max_charger_power_kW = dr.IsDBNull(14) ? (int?)null : Convert.ToInt32(dr[14])
                            });
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(charges, Formatting.Indented);
        }

        private string GetErrors(int carId, DateTime from, DateTime to)
        {
            var errors = new List<object>();
            using (var con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (var cmd = new MySqlCommand(@"
SELECT 
    Name as ErrorText,
    startedAt,
    endedAt
FROM alerts
JOIN alert_names ON alerts.nameID = alert_names.ID
WHERE carid = @carId
  AND startedAt >= @from
  AND startedAt <= @to
ORDER BY startedAt DESC", con))
                {
                    cmd.Parameters.AddWithValue("@carId", carId);
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@to", to);
                    using (var dr = SQLTracer.TraceDR(cmd))
                    {
                        while (dr.Read())
                        {
                            errors.Add(new
                            {
                                error_text = dr.IsDBNull(0) ? "" : dr.GetString(0),
                                started_at = dr.IsDBNull(1) ? "" : ((DateTime)dr[1]).ToString("yyyy-MM-dd HH:mm:ss"),
                                ended_at = dr.IsDBNull(2) ? null : ((DateTime)dr[2]).ToString("yyyy-MM-dd HH:mm:ss")
                            });
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(errors, Formatting.Indented);
        }

        private string GetDegradation(int carId, DateTime from, DateTime to)
        {
            var degradation = new List<object>();
            using (var con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (var cmd = new MySqlCommand(@"
SELECT
    chargingstate.StartDate,
    charging_End.ideal_battery_range_km / charging_End.battery_level * 100 as maxRangeKm,
    pos.odometer as odometerKm
FROM charging
INNER JOIN chargingstate ON charging.id = chargingstate.StartChargingID
INNER JOIN pos ON chargingstate.pos = pos.id
LEFT OUTER JOIN charging AS charging_End ON chargingstate.EndChargingID = charging_End.id
WHERE chargingstate.StartDate BETWEEN @from AND @to
  AND TIMESTAMPDIFF(MINUTE, chargingstate.StartDate, chargingstate.EndDate) > 3
  AND pos.odometer > 1
  AND charging_End.battery_level >= 70
  AND chargingstate.CarID = @carId
ORDER BY chargingstate.StartDate", con))
                {
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@to", to);
                    cmd.Parameters.AddWithValue("@carId", carId);
                    using (var dr = SQLTracer.TraceDR(cmd))
                    {
                        while (dr.Read())
                        {
                            degradation.Add(new
                            {
                                start_date = dr.IsDBNull(0) ? "" : ((DateTime)dr[0]).ToString("yyyy-MM-dd HH:mm:ss"),
                                max_range_km = dr.IsDBNull(1) ? (double?)null : Convert.ToDouble(dr[1]),
                                odometer_km = dr.IsDBNull(2) ? (double?)null : Convert.ToDouble(dr[2])
                            });
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(degradation, Formatting.Indented);
        }

        private string GetFirmware(int carId, DateTime from, DateTime to)
        {
            var firmware = new List<object>();
            using (var con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (var cmd = new MySqlCommand(@"
SELECT
    startDate,
    version
FROM car_version
WHERE CarID = @carId
  AND startDate BETWEEN @from AND @to
ORDER BY startDate", con))
                {
                    cmd.Parameters.AddWithValue("@carId", carId);
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@to", to);
                    using (var dr = SQLTracer.TraceDR(cmd))
                    {
                        while (dr.Read())
                        {
                            firmware.Add(new
                            {
                                start_date = dr.IsDBNull(0) ? "" : ((DateTime)dr[0]).ToString("yyyy-MM-dd HH:mm:ss"),
                                version = dr.IsDBNull(1) ? "" : dr.GetString(1)
                            });
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(firmware, Formatting.Indented);
        }

        private string GetTpms(int carId, DateTime from, DateTime to)
        {
            var tpms = new List<object>();
            using (var con = new MySqlConnection(DBHelper.DBConnectionstring))
            {
                con.Open();
                using (var cmd = new MySqlCommand(@"
SELECT
    DATE_FORMAT(Datum, '%Y-%m-%d %H:00:00') AS HourBucket,
    MAX(CASE WHEN TireID = 1 THEN Pressure END) AS TPMS_FL,
    MAX(CASE WHEN TireID = 2 THEN Pressure END) AS TPMS_FR,
    MAX(CASE WHEN TireID = 3 THEN Pressure END) AS TPMS_RL,
    MAX(CASE WHEN TireID = 4 THEN Pressure END) AS TPMS_RR
FROM TPMS
WHERE carID = @carId
  AND Datum BETWEEN @from AND @to
  AND (
      Pressure IS NOT NULL
  )
GROUP BY DATE_FORMAT(Datum, '%Y-%m-%d %H')
ORDER BY HourBucket", con))
                {
                    cmd.Parameters.AddWithValue("@carId", carId);
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@to", to);
                    using (var dr = SQLTracer.TraceDR(cmd))
                    {
                        while (dr.Read())
                        {
                            tpms.Add(new
                            {
                                hour = dr.IsDBNull(0) ? "" : dr.GetString(0),
                                tpms_fl = dr.IsDBNull(1) ? (double?)null : Convert.ToDouble(dr[1]),
                                tpms_fr = dr.IsDBNull(2) ? (double?)null : Convert.ToDouble(dr[2]),
                                tpms_rl = dr.IsDBNull(3) ? (double?)null : Convert.ToDouble(dr[3]),
                                tpms_rr = dr.IsDBNull(4) ? (double?)null : Convert.ToDouble(dr[4])
                            });
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(tpms, Formatting.Indented);
        }

        #endregion

        #region JSON-RPC Helpers

        private static object JsonRpcResult(JToken id, object result)
        {
            return new
            {
                jsonrpc = "2.0",
                id = id,
                result = result
            };
        }

        private static object JsonRpcError(JToken id, int code, string message)
        {
            return new
            {
                jsonrpc = "2.0",
                id = id,
                error = new { code, message }
            };
        }

        private static object ToolResult(JToken id, string textContent)
        {
            return JsonRpcResult(id, new
            {
                content = new[]
                {
                    new { type = "text", text = textContent }
                }
            });
        }

        private static object ToolError(JToken id, string errorMessage)
        {
            return JsonRpcResult(id, new
            {
                isError = true,
                content = new[]
                {
                    new { type = "text", text = "Error: " + errorMessage }
                }
            });
        }

        private static void WriteJsonResponse(HttpListenerResponse response, object data)
        {
            response.ContentType = "application/json; charset=utf-8";
            var json = JsonConvert.SerializeObject(data, JsonSettings);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }

        #endregion

        public void Dispose()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }
        }
    }
}
