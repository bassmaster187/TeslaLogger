# TeslaLogger MCP Server

## Überblick
Der TeslaLogger MCP Server stellt Fahrzeugdaten als MCP-Tools bereit. Sie werden für KI-Chatbots oder andere Anwendungen über eine JSON-RPC 2.0 API zugänglich gemacht.

## Beispiel config für VS-Code `mcp.json`
```json
{
	"servers": {
		"TeslaLogger mcp server": {
			"url": "http://teslalogger-ip:5001/mcp",
			"type": "http"
		}
	},
	"inputs": []
}
```

- Transport: Streamable HTTP (JSON-RPC 2.0 via `POST`)
- Endpoint: `http://teslalogger-ip:5001/mcp` (Standard: `HTTPPort + 1`)
- Health: `GET http://teslalogger-ip:5001/`

Der Server startet zusammen mit TeslaLogger.

## Verfügbare Tools

### `get_vehicles`
Liefert alle Fahrzeuge.

**Parameter:** keine

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
