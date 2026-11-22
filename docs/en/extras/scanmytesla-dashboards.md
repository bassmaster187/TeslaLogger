---
sidebar_position: 3
---
# Use ScanMyTesla data in your own dashboards

Every signal the app delivers is stored unmodified in the `can` table of the database. What each ID means and which unit it uses is listed in the [Tesla Signal IDs Documentation](/docs/extras/signalIds).

| Column | Type | Content |
|---|---|---|
| `datum` | datetime | timestamp of the data point (one second resolution) |
| `id` | mediumint | signal ID |
| `val` | double | signal value |
| `CarID` | int | vehicle the value comes from |

The primary key is `(datum, id)` – there is exactly one value per signal and second.

## Querying in Grafana

All shipped dashboards read this table through the MySQL data source `teslalogger`. A panel query looks like this, taken from the shipped dashboard "Zellspannungen 01-20 - ScanMyTesla":

```sql
SELECT $__time(datum), val as 'Zellspannung 1 [V]'
FROM can
WHERE $__timeFilter(datum) and carid=$Car and id = 101
ORDER BY datum ASC
```

Things to keep in mind:

- Set the query `Format` to **Time series**.
- `$Car` is a dashboard variable. In the shipped dashboards it is defined as a query:
  `select display_name as __text, id as __value from cars where length(vin) > 5 order by display_name`
  In your own dashboard without that variable, put the CarID in directly (for example `carid=1`).
- Without the Grafana MySQL macros `$__time(...)` and `$__timeFilter(...)`, Grafana does not recognise the time axis and no graph is drawn.

## Which IDs actually arrive?

```sql
SELECT id, COUNT(*) AS count, MAX(datum) AS last_value
FROM can
WHERE datum > NOW() - INTERVAL 1 DAY
GROUP BY id
ORDER BY id
```

Use this to check whether the app delivers a signal at all, and to compare the IDs that have data against the ID list.

## Shipped dashboards

| Dashboard | signal IDs |
|---|---|
| Zellspannungen 01-20 / 21-40 / 41-60 / 61-80 / 81-99 - ScanMyTesla | 101-199 (individual cell voltages), plus 5, 6, 7 and 43 |
| Performance - ScanMyTesla | 2, 5, 27, 29, 43, 78, 90, 400, 401, 403, 404, 405, 415, 429, 435, 443, 444, 445 |
| HVAC - ScanMyTesla | 41, 42, 47-56, 58, 81, 84, 91-96 |
| Alle Verbräuche - ScanMyTesla | 2, 31, 32, 39, 43, 58, 83, 84, 85, 418, 442 |

## Other things to know

- Only a small subset of the IDs is additionally mapped into the current car data by Teslalogger, which is what makes them show up in the web UI, in `/currentjson/{id}` and in the keep-awake logic. Everything else is visible only through Grafana queries on `can`, see [Signals evaluated by Teslalogger](/docs/extras/signalIds#signals-evaluated-by-teslalogger).
- Gaps and long flat lines in the graphs are normal: the app only sends values that changed since the last packet, see [Change filter](/docs/extras/scanmytesla-api#change-filter).
