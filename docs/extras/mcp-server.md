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
        "name": "get_trips",
        "description": "Retrieve trips for a vehicle. Returns start/destination, distance, consumption, duration and temperatures. Use 'from'/'to' for a specific date range, or 'days' to look back from now.",
...
```

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
