# AI Chatbots / MCP Server
![IMAGE](/img/Claude-Desktop-MCP.jpg)

## Advantages of the MCP Server
The TeslaLogger MCP Server connects your locally stored vehicle data with AI assistants like Claude or other MCP-capable applications. This brings several practical advantages:

## Natural Language Evaluation
Instead of SQL queries or Grafana dashboards, you can simply ask in normal language – e.g. "How much did I charge this week?" or "Show me my longest trips last month." The AI assistant automatically translates the question into the appropriate tool call.

## Data Sovereignty – No Cloud
All data remains local on your Raspberry Pi or Docker host. The MCP Server runs in your own network; no vehicle data is transferred to external servers. The AI only gets what you explicitly query.

## Open Protocol – Many Clients
MCP (Model Context Protocol) is an open standard. In addition to Claude Desktop, the TeslaLogger MCP Server works with any compatible client – e.g. VS Code with Copilot, Cursor, or your own scripts. The configuration is minimal (one URL, no API key).

## Complex Analyses Without Programming Knowledge
Relationships that would require multiple dashboards in Grafana can be answered at once via chat: "On which days was my tire pressure low and what was the consumption at the same time?" – the assistant combines get_tpms and get_trips automatically.

## Extensible for Your Own Workflows
Since the server provides a standard JSON-RPC 2.0 API, it can also be integrated into automations (e.g. n8n, Home Assistant automations, your own scripts) – not only in chat clients.


# Function Test
Raspberries use port 5001 for the MCP Server. As a test, you can enter in the browser:
> http://raspberry:5001/

If everything works, you get the following output:
```json
{"status":"ok","server":"TeslaLogger MCP Server","port":5001}
```

In Docker, you must open the port:
````
	ports:
	  - ${TESLALOGGER_PORT:-5010}:5000
	  - 5001:5001
````


## Setting up a Chat Client (Claude Desktop)
Example with Claude Desktop. 

Download Claude Desktop:
> https://claude.com/download

- Go to Developer in Claude Desktop settings
- Edit config
- Add mcpServer
- If Node.js is not installed on the system, you need to install it: https://nodejs.org/en/download
- Very important: after saving the config, Claude Desktop must be closed and restarted!
- Prompt for a function test: "How many vehicles do I have in TeslaLogger"

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


## Example config for VS-Code `mcp.json`
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

# Test if the MCP Server works:
```
curl -X POST http://raspberry:5001/mcp \
  -H "Content-Type: application/json" \
  -d '{
	"jsonrpc":"2.0",
	"id":1,
	"method":"tools/list"
  }'

```
The output shows the commands that the MCP Server can execute:
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

## Available Tools

### `get_vehicles`
Returns all vehicles.

**Parameters:** none

---

### `get_trips`
Returns trips in the period.

**Parameters:**
- `car_id` (required)
- `from` (optional, `yyyy-MM-dd` or `yyyy-MM-dd HH:mm:ss`)
- `to` (optional, `yyyy-MM-dd` or `yyyy-MM-dd HH:mm:ss`)
- `days` (optional, Default `7`; is ignored if `from` is set)

---

### `get_charges`
Returns charging sessions in the period.

**Parameters:**
- `car_id` (required)
- `from` (optional)
- `to` (optional)
- `days` (optional)

---

### `get_errors`
Returns alerts/errors from `alerts`/`alert_names`, filtered on `startedAt`.

**Parameters:**
- `car_id` (required)
- `from` (optional)
- `to` (optional)
- `days` (optional)

---

### `get_degradation`
Returns degradation-relevant charging points (max range, odometer).

**Parameters:**
- `car_id` (required)
- `from` (optional)
- `to` (optional)
- `days` (optional)

---

### `get_firmware`
Returns firmware history from `car_version`.

**Parameters:**
- `car_id` (required)
- `from` (optional)
- `to` (optional)
- `days` (optional)

---

### `get_tpms`
Returns TPMS values from table `TPMS`, aggregated hourly.

**Parameters:**
- `car_id` (required)
