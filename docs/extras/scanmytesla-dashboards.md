---
sidebar_position: 3
---
# ScanMyTesla-Daten in eigenen Dashboards nutzen

Jedes Signal, das die App liefert, liegt unverändert in der Tabelle `can` der Datenbank. Welche ID welche Bedeutung und Einheit hat, steht in der [Tesla Signal IDs Documentation](/docs/extras/signalIds).

| Spalte | Typ | Inhalt |
|---|---|---|
| `datum` | datetime | Zeitstempel des Datenpunkts (Sekundenauflösung) |
| `id` | mediumint | Signal-ID |
| `val` | double | Wert des Signals |
| `CarID` | int | Fahrzeug, von dem der Wert stammt |

Primärschlüssel ist `(datum, id)` – pro Signal und Sekunde existiert genau ein Wert.

## Abfrage in Grafana

Alle mitgelieferten Dashboards lesen die Tabelle über die MySQL-Datenquelle `teslalogger`. Eine Panel-Abfrage sieht so aus, Beispiel aus dem mitgelieferten Dashboard «Zellspannungen 01-20 - ScanMyTesla»:

```sql
SELECT $__time(datum), val as 'Zellspannung 1 [V]'
FROM can
WHERE $__timeFilter(datum) and carid=$Car and id = 101
ORDER BY datum ASC
```

Beachten:

- `Format` der Abfrage auf **Time series** stellen.
- `$Car` ist eine Dashboard-Variable. In den mitgelieferten Dashboards ist sie als Query definiert:
  `select display_name as __text, id as __value from cars where length(vin) > 5 order by display_name`
  In einem eigenen Dashboard ohne diese Variable die CarID direkt eintragen (zum Beispiel `carid=1`).
- Ohne die Grafana-MySQL-Makros `$__time(...)` und `$__timeFilter(...)` erkennt Grafana die Zeitachse nicht, dann erscheint keine Kurve.

## Welche IDs kommen tatsächlich an?

```sql
SELECT id, COUNT(*) AS anzahl, MAX(datum) AS letzter_wert
FROM can
WHERE datum > NOW() - INTERVAL 1 DAY
GROUP BY id
ORDER BY id
```

Damit lässt sich prüfen, ob die App ein Signal überhaupt liefert, und die gefüllten IDs mit der ID-Übersicht abgleichen.

## Mitgelieferte Dashboards

| Dashboard | Signal-IDs |
|---|---|
| Zellspannungen 01-20 / 21-40 / 41-60 / 61-80 / 81-99 - ScanMyTesla | 101-199 (Einzelzellspannungen), zusätzlich 5, 6, 7 und 43 |
| Performance - ScanMyTesla | 2, 5, 27, 29, 43, 78, 90, 400, 401, 403, 404, 405, 415, 429, 435, 443, 444, 445 |
| HVAC - ScanMyTesla | 41, 42, 47-56, 58, 81, 84, 91-96 |
| Alle Verbräuche - ScanMyTesla | 2, 31, 32, 39, 43, 58, 83, 84, 85, 418, 442 |

## Was sonst noch gilt

- Nur ein kleiner Teil der IDs wird von Teslalogger zusätzlich in die aktuellen Fahrzeugdaten übernommen und erscheint damit auch im Web-UI, über `/currentjson/{id}` oder in der Wachhalte-Logik. Alles andere ist ausschließlich über Grafana-Abfragen auf `can` sichtbar, siehe [Signals evaluated by Teslalogger](/docs/extras/signalIds#signals-evaluated-by-teslalogger).
- Lücken und lange waagerechte Linien in den Kurven sind normal: Die App sendet nur Werte, die sich seit dem letzten Paket geändert haben, siehe [Änderungsfilter](/docs/extras/scanmytesla-api#change-filter).
