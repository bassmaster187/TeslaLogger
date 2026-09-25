# TeslaLogger MCP Server
![BILD](Claude-Desktop-MCP.jpg)

## Überblick
Der TeslaLogger MCP Server stellt Fahrzeugdaten als MCP-Tools bereit. Sie werden für KI-Chatbots oder andere Anwendungen über eine JSON-RPC 2.0 API zugänglich gemacht.

## Funktionstest
Raspberries verwenden Port 5001 für den MCP Server. Als Test kann man im Browser eingeben:
> http://raspberry:5001/

Wenn alles klappt bekommt man als Ausgabe:
> {"status":"ok","server":"TeslaLogger MCP Server","port":5001}

Im Docker muss man den Port öffnen:
````
	ports:
      - ${TESLALOGGER_PORT:-5010}:5000
      - 5001:5001
````




## Chat-Client einrichten
Als Beispiel mit Claude Desktop. 

Claude Desktop runterladen:
> https://claude.com/download

- In den Einstellungen von Claude Desktop zu Entwickler gehen
- Config bearbeiten
- mcpServer hinzufügen
- Wenn auf dem System Node.js nicht installiert ist, dann muss man es nachinstallieren: https://nodejs.org/en/download
- ganz wichtig: nach dem speichern der Config muss Claude Desktop beendet und neu gestartet werden!
- Prompt für einen Funktionstest: "Wie viele Fahrzeuge habe ich im Teslalogger"

```
{
  "mcpServers": {
    "TeslaLogger": {
      "command": "npx",
      "args": [
        "-y",
        "mcp-remote",
        "http://raspberry:5001/mcp",
        "--allow-http"
      ]
    }
  },
  "preferences": {
    "coworkScheduledTasksEnabled": false,
    "ccdScheduledTasksEnabled": false,
    "coworkWebSearchEnabled": true,
    "epitaxyPrefs": {
      "starred-local-code-sessions": [],
      "starred-cowork-spaces": [],
      "starred-session-groups": [],
      "dframe-local-slice": {
        "pinnedOrder": [],
        "customGroupAssignments": {},
        "customGroupOrder": {}
      }
    },
    "sidebarMode": "chat"
  }
}
```


## Beispiel config für VS-Code `mcp.json`
```json
{
	"servers": {
		"TeslaLogger mcp server": {
			"url": "http://raspberry:5001/mcp",
			"type": "http"
		}
	},
	"inputs": []
}
```

# Test ob der MCP Server funktioniert:
```
curl -X POST http://raspberry:5001/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc":"2.0",
    "id":1,
    "method":"tools/list"
  }'

```
Als Ausgabe kommen die Befehle, die der MCP Server kann:
```
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tools": [
      {
        "name": "get_vehicles",
        "description": "Retrieve all vehicles from TeslaLogger. Returns ID, display name, VIN, model and status.",
        "inputSchema": {
          "type": "object",
          "properties": {},
          "required": []
        }
      },
      {
        "name": "get_current",
        "description": "Retrieve the current live data of a vehicle (same data as the /currentjson/{id} endpoint). Use 'refresh' to force an immediate data update. Returns a JSON object with these fields: Status flags (boolean): charging, driving, online, sleeping, falling_asleep, plugged_in, charge_port_door_open, fast_charger_present, battery_heater, is_preconditioning, sentry_mode, locked. Battery/range: battery_level (percent 0-100), ideal_battery_range_km (km), battery_range_km (km), charge_limit_soc (percent), charge_energy_added (kWh since charge start). Charging: charger_power (kW), charger_power_calc_w (W), charger_voltage (V), charger_phases (count), charger_actual_current (A), charge_current_request (A), charge_rate_km (km/h range gain), time_to_full_charge (hours), fast_charger_brand (string, e.g. 'Supercharger'). Driving: speed (km/h), power (kW), heading (degrees 0-360), odometer (km). Current trip: trip_start (time HH:mm:ss), trip_start_dt (ISO 8601 UTC), trip_max_speed (km/h), trip_max_power (kW), trip_duration_sec (seconds), trip_distance (km), trip_kwh (kWh), trip_avg_kwh (Wh/km). Position: latitude, longitude (WGS84), state, country_code, display_name, car_version. Active route: active_route_destination (string), active_route_energy_at_arrival (kWh), active_route_km_to_arrival (km), active_route_minutes_to_arrival (minutes), active_route_traffic_minutes_delay (minutes), active_route_latitude, active_route_longitude. Temperatures: outside_temp (degrees C), inside_temperature (degrees C). Windows/doors: open_windows (count), open_doors (count), frunk (1 = open), trunk (1 = open). Software: software_update_status (string), software_update_version (string). TPMS (bar, only if the vehicle has TPMS): tpms_pressure_fl, tpms_pressure_fr, tpms_pressure_rl, tpms_pressure_rr. Geofence: TLGeofence (name string, '-' if none), TLGeofenceIsHome, TLGeofenceIsCharger, TLGeofenceIsWork (boolean). ScanMyTesla (only included if data was received recently): SMTCellTempAvg (degrees C), SMTCellMinV/SMTCellAvgV/SMTCellMaxV (V), SMTCellImbalance (mV), SMTBMSmaxCharge/SMTBMSmaxDischarge (A), SMTACChargeTotal/SMTDCChargeTotal (kWh), SMTNominalFullPack (kWh). Metadata: ts (ISO 8601 UTC timestamp of the data), FatalError (string, null if no error). Note: power is positive while driving/consuming, negative while regenerating/charging.",
        "inputSchema": {
          "type": "object",
          "properties": {
            "car_id": {
              "type": "integer",
              "description": "Vehicle ID (from get_vehicles)"
            },
            "refresh": {
              "type": "boolean",
              "description": "Force an immediate update of the current data (default: false)"
            }
          },
          "required": ["car_id"]
        }
      },
      {
        "name": "get_trips",
        "description": "Retrieve trips for a vehicle. Returns start/destination, distance, consumption, duration and temperatures. Use 'from'/'to' for a specific date range, or 'days' to look back from now.",
...
```

## Verfügbare Tools

### `get_vehicles`
Liefert alle Fahrzeuge.

**Parameter:** keine

---

### `get_current`
Liefert die aktuellen Live-Daten eines Fahrzeugs (derselbe Datenstand wie der Endpoint `/currentjson/{id}`). Enthält Status (online/fahrend/ladend/schlafend), Batterieladung und Reichweite, Ladedetails, Position, Geofence, aktuelle Fahrt und TPMS-Werte.

**Parameter:**
- `car_id` (required)
- `refresh` (optional, `boolean`, Default `false`) – erzwingt eine sofortige Aktualisierung der Daten, bevor sie geliefert werden

**Rückgabe-Felder (Auszug):**
- Status (bool): `charging`, `driving`, `online`, `sleeping`, `falling_asleep`, `plugged_in`, `charge_port_door_open`, `fast_charger_present`, `battery_heater`, `is_preconditioning`, `sentry_mode`, `locked`
- Batterie/Reichweite: `battery_level` (%), `ideal_battery_range_km` (km), `battery_range_km` (km), `charge_limit_soc` (%), `charge_energy_added` (kWh seit Ladebeginn)
- Laden: `charger_power` (kW), `charger_power_calc_w` (W), `charger_voltage` (V), `charger_phases` (Anzahl), `charger_actual_current` (A), `charge_current_request` (A), `charge_rate_km` (km/h Reichweitengewinn), `time_to_full_charge` (Stunden), `fast_charger_brand` (z. B. `Supercharger`)
- Fahren: `speed` (km/h), `power` (kW), `heading` (Grad 0–360), `odometer` (km)
- Aktuelle Fahrt: `trip_start` (Uhrzeit `HH:mm:ss`), `trip_start_dt` (ISO 8601 UTC), `trip_max_speed` (km/h), `trip_max_power` (kW), `trip_duration_sec` (Sekunden), `trip_distance` (km), `trip_kwh` (kWh), `trip_avg_kwh` (Wh/km)
- Position: `latitude`, `longitude` (WGS84), `state`, `country_code`, `display_name`, `car_version`
- Route: `active_route_destination`, `active_route_energy_at_arrival` (kWh), `active_route_km_to_arrival` (km), `active_route_minutes_to_arrival` (Minuten), `active_route_traffic_minutes_delay` (Minuten), `active_route_latitude`, `active_route_longitude`
- Temperaturen: `outside_temp` (°C), `inside_temperature` (°C)
- Fenster/Türen: `open_windows` (Anzahl), `open_doors` (Anzahl), `frunk` (1 = offen), `trunk` (1 = offen)
- Software: `software_update_status`, `software_update_version`
- TPMS (bar, nur wenn vorhanden): `tpms_pressure_fl`, `tpms_pressure_fr`, `tpms_pressure_rl`, `tpms_pressure_rr`
- Geofence: `TLGeofence` (Name, `-` wenn keiner), `TLGeofenceIsHome`, `TLGeofenceIsCharger`, `TLGeofenceIsWork` (bool)
- ScanMyTesla (nur bei aktuellen Daten): `SMTCellTempAvg` (°C), `SMTCellMinV`/`SMTCellAvgV`/`SMTCellMaxV` (V), `SMTCellImbalance` (mV), `SMTBMSmaxCharge`/`SMTBMSmaxDischarge` (A), `SMTACChargeTotal`/`SMTDCChargeTotal` (kWh), `SMTNominalFullPack` (kWh)
- Metadaten: `ts` (ISO 8601 UTC Zeitstempel der Daten), `FatalError` (string, `null` wenn kein Fehler)

> Hinweis: `power` ist positiv beim Fahren/Verbrauchen und negativ beim Rekuperieren/Laden.

---

### `get_trips`
Liefert Fahrten im Zeitraum.

**Parameter:**
- `car_id` (required)
- `from` (optional, `yyyy-MM-dd` oder `yyyy-MM-dd HH:mm:ss`)
- `to` (optional, `yyyy-MM-dd` oder `yyyy-MM-dd HH:mm:ss`)
- `days` (optional, Default `7`; wird ignoriert, wenn `from` gesetzt ist)

---

### `get_charges`
Liefert Ladevorgänge im Zeitraum.

**Parameter:**
- `car_id` (required)
- `from` (optional)
- `to` (optional)
- `days` (optional)

---

### `get_errors`
Liefert Alerts/Fehler aus `alerts`/`alert_names`, gefiltert auf `startedAt`.

**Parameter:**
- `car_id` (required)
- `from` (optional)
- `to` (optional)
- `days` (optional)

---

### `get_degradation`
Liefert Degradation-relevante Ladepunkte (max range, odometer).

**Parameter:**
- `car_id` (required)
- `from` (optional)
- `to` (optional)
- `days` (optional)

---

### `get_firmware`
Liefert Firmware-Historie aus `car_version`.

**Parameter:**
- `car_id` (required)
- `from` (optional)
- `to` (optional)
- `days` (optional)

---

### `get_tpms`
Liefert TPMS-Werte aus Tabelle `TPMS`, stündlich aggregiert.

**Parameter:**
- `car_id` (required)
- `from` (optional)
- `to` (optional)
- `days` (optional)

**Rückgabe-Felder:**
- `hour`
- `tpms_fl`
- `tpms_fr`
- `tpms_rl`
- `tpms_rr`

---

### `get_tripsummary`
Liefert aggregierte Fahrten-Zusammenfassungen für ein Fahrzeug, gruppiert nach Monat.

**Parameter:**
- `car_id` (required)
- `from` (optional)
- `to` (optional)
- `days` (optional)

**Rückgabe-Felder:**
- `period` (z. B. `2025-01`)
- `trips` (Anzahl Fahrten)
- `total_distance_km` (Gesamtdistanz)
- `total_consumption_kWh` (Gesamtverbrauch)
- `avg_consumption_per_100km` (Durchschnittsverbrauch)
- `total_minutes` (Gesamtfahrzeit in Minuten)
- `avg_speed_max` (Durchschnitt der Maximalgeschwindigkeiten)
- `max_speed` (Maximale Geschwindigkeit)
- `avg_outside_temp` (Durchschnittliche Außentemperatur)

---

### `get_chargesummary`
Liefert aggregierte Lade-Zusammenfassungen für ein Fahrzeug, gruppiert nach Monat.

**Parameter:**
- `car_id` (required)
- `from` (optional)
- `to` (optional)
- `days` (optional)

**Rückgabe-Felder:**
- `period` (z. B. `2025-01`)
- `sessions` (Anzahl Ladevorgänge)
- `total_kWh` (Gesamt-ladung)
- `total_cost` (Gesamtkosten)
- `avg_cost_per_kWh` (Durchschnittspreis pro kWh)
- `avg_cost_per_session` (Durchschnittspreis pro Ladevorgang)
- `total_idle_fees` (Gesamte Leergebühren)
- `avg_charger_power_kW` (Durchschnittsladeleistung)
- `max_charger_power_kW` (Maximale Ladeleistung)
- `total_minutes` (Gesamte Ladedauer in Minuten)

## Zeitfilter-Logik
Alle Tools mit Zeitfilter nutzen dieselbe Logik:

1. Wenn `from`/`to` gesetzt sind, wird dieser Bereich verwendet.
2. Wenn `from` nicht gesetzt ist, wird `days` genutzt (`now - days` bis `now`).
3. Wenn `to` fehlt, ist `to = now`.

## Logging
Alle MCP Requests werden mit Methode, ID und Parametern geloggt.

Beispiel-Logeintrag:

`MCP Request: method=tools/call id=123 params={...}`

## Beispiel-Request (tools/list)
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list",
  "params": {}
}
```

## Beispiel-Request (tools/call)
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
	"name": "get_charges",
	"arguments": {
	  "car_id": 1,
	  "from": "2025-01-01 00:00:00",
	  "to": "2025-01-31 23:59:59"
	}
  }
}
```

## Beispiel-Request (get_current)
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
	"name": "get_current",
	"arguments": {
	  "car_id": 1,
	  "refresh": true
	}
  }
}
```
