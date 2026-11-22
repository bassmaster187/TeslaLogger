---
sidebar_position: 3
---

# App data interface (ScanMyTesla and other apps)

This page describes how an app delivers vehicle signals that Teslalogger cannot read itself (cell voltages, cell temperatures, motor temperatures, …). It is the technical reference for app authors; the user side is described in [ScanMyTesla](/docs/faq/scanmytesla) and the signal list in [Tesla Signal IDs Documentation](/docs/extras/signalIds).

```
app  --HTTPS POST-->  teslalogger.de/insert_scanmytesla.php  -->  queue table tesla_scanmytesla
Teslalogger  --HTTP POST (t = tasker token)-->  teslalogger.de/get_scanmytesla.php  (one packet per call, then deleted)
Teslalogger  -->  INSERT INTO can (datum, id, val, CarID)  -->  Grafana dashboards, web UI
```

The intermediate server only buffers the data: every packet is deleted as soon as the Teslalogger installation has downloaded it, and anything older than one day is deleted by a cleanup job. No personal or position data is required.

## Prerequisites on the Teslalogger side

- The checkbox **ScanMyTesla integration** has to be enabled in `http://raspberry/admin/settings.php`. Without it the installation never downloads the packets, they stay on the server until the cleanup job removes them.
- The download thread is created when Teslalogger has fetched the vehicle list from the Tesla backend successfully, so at least one successful login is required before packets are picked up.
- The **Tasker Token** shown in the same settings page is used as the address of the queue. It is the CRC32 of VIN plus Tesla account name (8 hexadecimal characters), with an optional `prefix_` in front and `_2`, `_3`, … for the second and further cars in one account. It is `varchar(10)` in Teslalogger and `varchar(20)` in the queue table.

## Sending data

`POST https://teslalogger.de/insert_scanmytesla.php`, content type `application/x-www-form-urlencoded`:

| Field | Content |
|-------|---------|
| `t` | Tasker token of the Teslalogger installation |
| `v` | protocol version marker, ScanMyTesla sends `1`. The server currently does not evaluate it |
| `m` | JSON packet, see below |

The answer is `ok` on success, otherwise an SQL error message. A packet is accepted without checking the token, so a wrong token means that the data is never picked up by any installation. To validate a token before sending, use `POST https://teslalogger.de/check_token.php` with `t` — the answer is `OK` or `not found`.

### Packet format

```json
{
  "d": "2025-11-22T12:34:56.7890123+01:00",
  "dict": {
    "2": 25.5,
    "27": 12,
    "43": -24.7,
    "101": 3.412
  }
}
```

- `d` — timestamp of the measurement. Recommended: ISO 8601 including UTC offset (or `Z`), which is what ScanMyTesla sends (the default JSON serialisation of a local `DateTime`). Teslalogger parses the value and converts it to the local time of the installation before writing it to the database, so the time zone of the phone does not have to match the car. Without an offset the value is read as if it were already the local time of the Teslalogger installation. Parsing is done with the culture of the installation, so localised formats such as `22.11.2025 12:34` must not be used — they are read wrongly or rejected.
- `d` is also the value shown as "ScanMyTesla last received" in the settings page, and it is the timestamp written to the database (`can.datum`, resolution one second). The timestamp the queue stores on the server is ignored by Teslalogger.
- `dict` — signal ID (as a string) → value (double). The IDs and units are listed in [Tesla Signal IDs Documentation](/docs/extras/signalIds).

Notes on the values:

- Send the units listed in the signal documentation, Teslalogger does not convert anything.
- Omit signals that are currently unavailable instead of sending `0`. Values containing `NaN` or `Infinity` are discarded by Teslalogger. For Speed (442) the value `287.6` is recognised as "signal not available".
- Only signals that actually changed have to be sent — see [Change filter](#change-filter) below.

## Receiving side: queue and rate

The server keeps a real FIFO queue, it does not keep only the last packet:

```sql
select id, datum, message from tesla_scanmytesla where token = ? order by id limit 1   -- then DELETE that row
```

- One Teslalogger installation downloads **one packet every 5 seconds**. If the queue was empty and the car is neither driving nor charging, it waits an additional 30 seconds before the next attempt.
- The practical limit is therefore **1 packet per 5 seconds per token** (about 720 packets per hour). Sending faster builds up a backlog; rows older than one day are deleted by the cleanup job, so packets are lost and the database on the server grows.
- If the packet is in the queue, it is deleted after the download. Retransmission has to be done by the app if it did not receive `ok`.
- Under load the server answers with HTTP 503 or with the text `Resource Limit Is Reached`. Teslalogger backs off for 25 seconds in that case, the app simply retries after 5 seconds.
- The `message` column is `text`, so a packet should stay below 64 KB. A complete packet with all cell voltages and cell temperatures is about 2 KB.

### Change filter

Teslalogger stores every value exactly once, it does not filter anything, so the app has to reduce the amount of data itself. ScanMyTesla sends a signal only when it differs from the last sent value of the same signal:

| Signals | Difference that triggers a new value |
|---------|--------------------------------------|
| all signals (default) | more than 1 % of the last value |
| 32, 34, 35, 37, 38, 43, 44, 443, 444, 445 | more than 10 % of the last value |
| 71 (Nominal full pack), 87 (Energy buffer) | fixed 0.1 kWh |
| 72 (Nominal remaining), 74 (Expected remaining), 75 (Ideal remaining) | fixed 0.2 kWh |
| 100-208 (cell voltages) | fixed 0.002 V (2 mV) |

A signal that did not change is simply left out of the next packet, the old value stays in the database and the graphs continue from it. Signals that are currently not available are left out as well (`null` in the app), never sent as `0`.

Within one packet each signal ID appears at most once, because the packet is a JSON object keyed by the signal ID.

ScanMyTesla sends a packet about every five seconds while driving, and slows down on its own: up to 25 s longer while the car is charging or recuperating, up to 50 s longer while it is idling (`Battery power` < 0.2 kW and HVAC fan stopped).

### One value per second per signal

The primary key of the local `can` table is `(datum, id)`, and `datum` has a resolution of one second. All values of one packet are written with a single multi-row `INSERT`; if one of them already exists, **the whole packet is discarded** and only a "Duplicate entry" message is logged. Therefore:

- do not send the same signal ID twice within the same second,
- do not send two packets with the identical timestamp,
- run only one app per car: ScanMyTesla and a second app writing the same IDs into the same installation overwrite and block each other.

## Storage inside Teslalogger

Every value is written unmodified into the `can` table (`datum`, `id`, `val`, `CarID`). All Grafana dashboards that show cell voltages, cell imbalance, degradation and so on read from that table — no code change is needed for a new signal ID.

A small subset of the IDs is additionally mapped to fields of the current car data and is used for the web UI, `/currentjson/{id}`, the MCP server and for the "keep the car awake" logic. That mapping has to be extended by the Teslalogger project for every new signal that should appear outside Grafana, see [Signals evaluated by Teslalogger](/docs/extras/signalIds#signals-evaluated-by-teslalogger).

The date of the last received packet is stored in `cars.lastscanmytesla` and is shown in the settings page as "ScanMyTesla last received".

## Requesting new signal IDs

`can.id` is a signed `mediumint`. The ranges in use and the ranges that are still free are listed in [Reserved ID ranges](/docs/extras/signalIds#reserved-id-ranges). Before an app uses new IDs, they have to be coordinated with the Teslalogger project so that no other app or the fleet telemetry integration uses them later. Please send for every signal:

- signal name, preferred ID,
- unit and resolution,
- sign convention (for currents and powers),
- value range and the value that means "not available",
- whether the signal should also appear in the web UI / `/currentjson`.

## Debugging

- On the server every packet is additionally appended to `admin/smt-<yyyy-mm-dd>.txt` as `token~<json>`, and `admin/smt_data.php` lists the number of packets per token.
- On Teslalogger the log file contains the download result (`ScanMyTesla: insert ok [n] <timestamp>`) as well as `SMT: …` messages for server problems.
- The settings page shows when the last packet arrived; if it stays empty, check the checkbox, the token and whether the car is online.

## Data protection

The intermediate server is located in Germany, transport is encrypted with TLS, no personal or position data is sent, and the data is deleted immediately after it has been downloaded by the Teslalogger installation. It is not shared with third parties.
