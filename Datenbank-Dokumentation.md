# TeslaLoggerNET8 — Datenbank-Dokumentation

## Übersicht

Die Datenbank `teslalogger` (MariaDB/MySQL) speichert alle Fahrzeugdaten, Lade- und Fahr_sessions, sowie Konfigurations- und Hilfsdaten. Es gibt **26 Tabellen** und **2 Views**.

> **Hinweis:** Es werden keine expliziten Foreign Keys auf DB-Ebene definiert (`FOREIGN_KEY_CHECKS=0`). Die Beziehungen sind logisch über gemeinsame Spalten (`CarID`, `id`-Verweise) hergestellt.
>
> Viele Spalten wurden nachträglich via `ALTER TABLE`-Migrationen hinzugefügt (siehe `UpdateTeslalogger.cs`, `KVS.cs`, `Journeys.cs`, `GetChargingHistoryV2Service.cs`, `Komoot.cs`, `GeocodeCache.cs`).

---

## Tabellen-Katalog

### 1. `cars` — Fahrzeugkonfiguration

Zentrale Tabelle für alle registrierten Fahrzeuge.

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK) | CREATE | Fahrzeug-ID, Referenz in fast allen anderen Tabellen |
| `tesla_name` | varchar(45) | CREATE | Tesla-Anmeldename |
| `tesla_password` | varchar(45) | CREATE | Tesla-Passwort |
| `tesla_carid` | int | CREATE | Tesla API Fahrzeug-ID |
| `tesla_token` | text | CREATE→ALTER | Authentifizierungs-Token (von varchar(100) auf TEXT erweitert) |
| `tesla_token_expire` | datetime | CREATE | Token-Ablauf |
| `tasker_hash` | varchar(10) | CREATE | Tasker-Hash |
| `model` | varchar(45) | CREATE | Fahrzeugmodell |
| `model_name` | varchar(45) | CREATE | Modellname |
| `wh_tr` | double | CREATE | Wh pro km (Reichweite) |
| `db_wh_tr` | double | CREATE | Aus DB berechnetes Wh/km |
| `db_wh_tr_count` | int | CREATE | Anzahl der Wh/km-Messungen |
| `car_type` | varchar(45) | CREATE | Fahrzeugtyp |
| `car_special_type` | varchar(45) | CREATE | Spezieller Fahrzeugtyp |
| `car_trim_badging` | varchar(45) | CREATE | Trim-Aufkleber |
| `display_name` | varchar(45) | CREATE | Anzeigename |
| `raven` | bit(1) | CREATE | Raven-Flag |
| `Battery` | varchar(45) | CREATE | Batterietyp |
| `vin` | varchar(20) | ALTER | Fahrzeuginummer |
| `freesuc` | tinyint unsigned | ALTER | Kostenloses Supercharging (default 0) |
| `lastscanmytesla` | datetime | ALTER | Letzter MyTesla-Scan |
| `refresh_token` | text | ALTER | OAuth Refresh Token |
| `ABRP_token` | varchar(40) | ALTER | A Better Routeplanner Token |
| `ABRP_mode` | tinyint(1) | ALTER | ABRP-Modus |
| `SuCBingo_user` | varchar(40) | ALTER | Supercharger Bingo Benutzer |
| `SuCBingo_apiKey` | varchar(100) | ALTER | SuCBingo API-Schlüssel |
| `meter_type` | varchar(20) | ALTER | Stromzähler-Typ |
| `meter_host` | varchar(50) | ALTER | Stromzähler-Host |
| `meter_parameter` | varchar(200) | ALTER | Stromzähler-Parameter |
| `wheel_type` | varchar(40) | ALTER | Radtyp |
| `fleetAPI` | tinyint unsigned | ALTER | Fleet API aktiv (default 0) |
| `fleetAPIaddress` | varchar(200) | ALTER | Fleet API Adresse |
| `oldAPIchinaCar` | tinyint unsigned | ALTER | Alte China-API (default 0) |
| `needVirtualKey` | tinyint unsigned | ALTER | Virtueller Schlüssel nötig (default 0) |
| `needCommandPermission` | tinyint unsigned | ALTER | Befehlsberechtigung nötig (default 0) |
| `needFleetAPI` | tinyint unsigned | ALTER | Fleet API nötig (default 0) |
| `access_type` | varchar(20) | ALTER | Zugangstyp |
| `virtualkey` | tinyint unsigned | ALTER | Virtueller Schlüssel (default 0) |

**Verknüpfungen:**
- `cars.id` → `pos.CarID`, `charging.CarID`, `chargingstate.CarID`, `drivestate.CarID`, `state.CarID`, `shiftstate.CarID`, `TPMS.CarId`, `can.CarID`, `car_version.CarID`, `battery.CarID`, `cruisestate.CarID`, `alerts.CarID`, `komoot.carID` (alle 1:n)
- `cars.id` → `journeys.CarID` (1:n)
- `cars.vin` → `teslacharging.VIN` (1:n)

---

### 2. `pos` — Positionsdatenpunkte

Zeitstempel mit GPS-Position und Fahrdaten pro Messpunkt.

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | Eindeutige Positions-ID |
| `Datum` | datetime(3) | CREATE→ALTER | Zeitstempel (auf Millisekunden-Präzision erweitert) |
| `lat` | double | CREATE | Breitengrad |
| `lng` | double | CREATE | Längengrad |
| `speed` | int | CREATE | Geschwindigkeit |
| `power` | int | CREATE | Leistung |
| `odometer` | double | CREATE | Kilometerstand |
| `ideal_battery_range_km` | double | CREATE | Idealereichweite |
| `address` | varchar(250) | CREATE | Geocodierte Adresse |
| `outside_temp` | double | CREATE | Außentemperatur |
| `altitude` | double | CREATE | Höhe |
| `battery_level` | double | ALTER | Batteriestand (%) |
| `inside_temp` | double | ALTER | Innentemperatur |
| `battery_heater` | tinyint(1) | ALTER | Batteriewärmer aktiv |
| `is_preconditioning` | tinyint(1) | ALTER | Vorconditioning aktiv |
| `sentry_mode` | tinyint(1) | ALTER | Sentry-Modus |
| `battery_range_km` | double | ALTER | Tatsächliche Reichweite |
| `CarID` | int unsigned | ALTER | → `cars.id` |
| `AP` | tinyint(1) | ALTER | Autopilot aktiv |

**Indizes:** `idx_pos_CarID_id` (CarID, id), `idx_pos_CarID_datum` (CarID, Datum)

**Verknüpfungen:**
- `pos.id` → `drivestate.StartPos` (1:1, unique)
- `pos.id` → `drivestate.EndPos` (1:1)
- `pos.id` → `state.StartPos`, `state.EndPos` (1:n)
- `pos.id` → `chargingstate.Pos` (1:n)
- `pos.id` → `journeys.StartPosID`, `journeys.EndPosID` (1:n)
- `pos.id` → `active_route_energy_at_arrival.posID` (1:n)
- `pos.lat/lng` → `geocodecache.lat/lng` (Cache-Lookup)

---

### 3. `charging` — Ladedatenpunkte

Messwerte während eines Ladevorgangs (zeitliche Auflösung).

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | Ladedaten-ID |
| `Datum` | datetime | CREATE | Zeitstempel |
| `battery_level` | double | CREATE | Batteriestand (%) |
| `charge_energy_added` | double | CREATE | Zugeladene Energie (kWh) |
| `charger_power` | double | CREATE | Ladeleistung (kW) |
| `ideal_battery_range_km` | double | CREATE | Idealereichweite |
| `charger_voltage` | int | CREATE | Ladespannung |
| `charger_phases` | int | CREATE | Ladephasen |
| `charger_actual_current` | int | CREATE | Tatsächlicher Ladestrom |
| `outside_temp` | double | CREATE | Außentemperatur |
| `charger_pilot_current` | int | ALTER | Pilot-Strom |
| `charge_current_request` | int | ALTER | Angeforderter Strom |
| `battery_heater` | tinyint(1) | ALTER | Batteriewärmer aktiv |
| `battery_range_km` | double | ALTER | Tatsächliche Reichweite |
| `charger_actual_current_calc` | int | ALTER | Berechneter Ladestrom |
| `charger_phases_calc` | tinyint(1) | ALTER | Berechnete Phasen |
| `charger_power_calc_w` | int | ALTER | Berechnete Leistung (W) |
| `CarID` | int unsigned | ALTER | → `cars.id` |

**Indizes:** `IX_charging_carid_datum` (CarID, Datum)

**Verknüpfungen:**
- `charging.id` → `chargingstate.StartChargingID` (1:1)
- `charging.id` → `chargingstate.EndChargingID` (1:1)

---

### 4. `chargingstate` — Ladesessions

Zusammengefasste Ladeereignisse mit Start/Ende.

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | Ladesession-ID |
| `StartDate` | datetime | CREATE | Session-Start |
| `EndDate` | datetime | CREATE | Session-Ende |
| `UnplugDate` | datetime | CREATE | Absteck-Zeitpunkt |
| `Pos` | int | CREATE | → `pos.id` |
| `charge_energy_added` | double | CREATE | Zugeladene Energie |
| `StartChargingID` | int | CREATE | → `charging.id` (Start) |
| `EndChargingID` | int | CREATE | → `charging.id` (Ende) |
| `conn_charge_cable` | varchar(50) | ALTER | Kabeltyp |
| `fast_charger_brand` | varchar(50) | ALTER | Schnelllader-Marke |
| `fast_charger_type` | varchar(50) | ALTER | Schnelllader-Typ |
| `fast_charger_present` | tinyint(1) | ALTER | Schnellladen vorhanden |
| `max_charger_power` | int | ALTER | Max. Ladeleistung |
| `cost_total` | double | ALTER | Gesamtkosten |
| `cost_currency` | varchar(3) | ALTER | Währung |
| `cost_per_kwh` | double | ALTER | Kosten pro kWh |
| `cost_per_session` | double | ALTER | Kosten pro Session |
| `cost_per_minute` | double | ALTER | Kosten pro Minute |
| `cost_idle_fee_total` | double | ALTER | Gesamte Wartegebühr |
| `cost_kwh_meter_invoice` | double | ALTER | Zähler-Rechnung kWh |
| `cost_freesuc_savings_total` | double | ALTER | Gespartes durch gratis SuC |
| `meter_vehicle_kwh_start` | double | ALTER | Fahrzeugzähler Start |
| `meter_vehicle_kwh_end` | double | ALTER | Fahrzeugzähler Ende |
| `meter_vehicle_kwh_sum` | double | ALTER | Fahrzeugzähler Summe |
| `meter_utility_kwh_start` | double | ALTER | Netz-Zähler Start |
| `meter_utility_kwh_end` | double | ALTER | Netz-Zähler Ende |
| `meter_utility_kwh_sum` | double | ALTER | Netz-Zähler Summe |
| `hidden` | tinyint(1) | ALTER | Ausgeblendet (default 0) |
| `combined_into` | int | ALTER | → `chargingstate.id` (Zusammenführung) |
| `CarID` | int unsigned | ALTER | → `cars.id` |
| `wheel_type` | varchar(40) | ALTER | Radtyp zum Zeitpunkt |
| `co2_g_kWh` | int | ALTER | CO₂ in g/kWh |
| `country` | varchar(80) | ALTER | Land |
| `export` | tinyint(1) | CREATE | Export-Flag |
| `sessionId` | varchar(40) | ALTER | Tesla Charging Session ID |

**Indizes:** `chargingsate_ix_pos` (Pos), `ixAnalyzeChargingStates1` (id, CarID, StartChargingID, EndChargingID)

**Verknüpfungen:**
- `chargingstate.Pos` → `pos.id`
- `chargingstate.StartChargingID` → `charging.id`
- `chargingstate.EndChargingID` → `charging.id`
- `chargingstate.combined_into` → `chargingstate.id` (Selbstreferenz)

---

### 5. `drivestate` — Fahrsessions

Zusammengefasste Fahrereignisse.

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | Fahrsession-ID |
| `StartDate` | datetime | CREATE | Session-Start |
| `StartPos` | int (unique) | CREATE | → `pos.id` (Start) |
| `EndDate` | datetime | CREATE | Session-Ende |
| `EndPos` | int | CREATE | → `pos.id` (Ende) |
| `outside_temp_avg` | double | ALTER | Durchschnittstemperatur |
| `speed_max` | int | ALTER | Max. Geschwindigkeit |
| `power_max` | int | ALTER | Max. Leistung |
| `power_min` | int | ALTER | Min. Leistung |
| `power_avg` | double | ALTER | Durchschnittsleistung |
| `meters_up` | double | ALTER | Höhenmeter bergauf |
| `meters_down` | double | ALTER | Höhenmeter bergab |
| `distance_up_km` | double | ALTER | Strecke bergauf |
| `distance_down_km` | double | ALTER | Strecke bergab |
| `distance_flat_km` | double | ALTER | Strecke eben |
| `height_max` | double | ALTER | Max. Höhe |
| `height_min` | double | ALTER | Min. Höhe |
| `CarID` | int unsigned | ALTER | → `cars.id` |
| `wheel_type` | varchar(40) | ALTER | Radtyp zum Zeitpunkt |
| `AP_sec_sum` | int | ALTER | Autopilot Sekunden Summe |
| `AP_sec_max` | int | ALTER | Autopilot Sekunden Max |
| `TPMS_FL` | double | ALTER | Reifendruck vorne links |
| `TPMS_FR` | double | ALTER | Reifendruck vorne rechts |
| `TPMS_RL` | double | ALTER | Reifendruck hinten links |
| `TPMS_RR` | double | ALTER | Reifendruck hinten rechts |
| `export` | tinyint(1) | CREATE | Export-Flag |

**Indizes:** `ix_startpos` (unique, StartPos), `ix_endpos2` (EndPos)

**Verknüpfungen:**
- `drivestate.StartPos` → `pos.id` (1:1, unique)
- `drivestate.EndPos` → `pos.id` (1:1)
- `drivestate.id` → `komoot.drivestateID` (1:n)

---

### 6. `state` — Zustandsübergänge

Allgemeine Fahrzeugzustandsänderungen (z. B. sleeping, online, driving, charging).

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | State-ID |
| `StartDate` | datetime | CREATE | Zustands-Start |
| `state` | varchar(50) | CREATE | Zustandsname |
| `EndDate` | datetime | CREATE | Zustands-Ende |
| `StartPos` | int | CREATE | → `pos.id` |
| `EndPos` | int | CREATE | → `pos.id` |
| `CarID` | int unsigned | ALTER | → `cars.id` |

**Verknüpfungen:**
- `state.StartPos` → `pos.id`
- `state.EndPos` → `pos.id`

---

### 7. `shiftstate` — Gangwechsel

Aufzeichnung der Gangstellung (D, R, P, N). Derzeit nicht aktiv verwendet.

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | Shiftstate-ID |
| `StartDate` | datetime | CREATE | Zeitfenster-Start |
| `state` | varchar(5) | CREATE | Gang (D/R/P/N) |
| `EndDate` | datetime | CREATE | Zeitfenster-Ende |
| `CarID` | int unsigned | ALTER | → `cars.id` |

---

### 8. `TPMS` — Reifendrucküberwachung

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `CarId` | int | CREATE | → `cars.id` |
| `Datum` | datetime | CREATE | Zeitstempel |
| `TireId` | int | CREATE | Reifen-Position (0-3) |
| `Pressure` | double | CREATE | Druck |

**PK:** (`CarId`, `Datum`, `TireId`)
**Indizes:** `IX_TPMS_CarId_Datum` (CarId, TireId, Datum, Pressure)

---

### 9. `can` — CAN-Bus-Daten

Rohdaten vom CAN-Bus des Fahrzeugs.

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `datum` | datetime | CREATE | Zeitstempel |
| `id` | mediumint | CREATE | CAN-ID |
| `val` | double | CREATE | Wert |
| `CarID` | int unsigned | ALTER | → `cars.id` |

**PK:** (`datum`, `id`)
**Indizes:** `can_ix2` (id, CarID, datum)

---

### 10. `car_version` — Softwareversionshistorie

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | Eintrag-ID |
| `StartDate` | datetime | CREATE | Zeitpunkt der Version |
| `version` | varchar(50) | CREATE | Versionsnummer |
| `CarID` | int unsigned | ALTER | → `cars.id` |

---

### 11. `battery` — Batteriegesundheit

Detaillierte Batteriediagnosedaten.

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `CarID` | int | CREATE | → `cars.id` |
| `date` | datetime | CREATE | Zeitstempel |
| `PackVoltage` | double | CREATE | Pack-Spannung |
| `PackCurrent` | double | CREATE | Pack-Strom |
| `IsolationResistance` | double | CREATE | Isolationswiderstand |
| `NumBrickVoltageMax` | smallint | CREATE | Index der Max-Zelle |
| `BrickVoltageMax` | double | CREATE | Max. Zellenspannung |
| `NumBrickVoltageMin` | smallint | CREATE | Index der Min-Zelle |
| `BrickVoltageMin` | double | CREATE | Min. Zellenspannung |
| `ModuleTempMax` | double | CREATE | Max. Modultemperatur |
| `ModuleTempMin` | double | CREATE | Min. Modultemperatur |
| `LifetimeEnergyUsed` | double | CREATE | Gesamte Lebensdauer-Energie |
| `LifetimeEnergyUsedDrive` | double | CREATE | Lebensdauer-Energie (Fahren) |

**PK:** (`CarID`, `date`)

---

### 12. `cruisestate` — Tempomat-Zustand

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `CarID` | int | CREATE | → `cars.id` |
| `date` | datetime | CREATE | Zeitstempel |
| `state` | tinyint | CREATE | Tempomat-Status |

**PK:** (`CarID`, `date`)

---

### 13. `alerts` — Warnungen/Alarme

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `ID` | int (AI) | CREATE | Alert-ID |
| `CarID` | int | CREATE | → `cars.id` |
| `startedAt` | datetime | CREATE | Alarm-Start |
| `nameID` | int | CREATE | → `alert_names.ID` |
| `endedAt` | datetime | CREATE | Alarm-Ende |

**PK:** (`CarID`, `startedAt`, `nameID`)

**Verknüpfungen:**
- `alerts.nameID` → `alert_names.ID`
- `alerts.ID` → `alert_audiences.alertsID`

---

### 14. `alert_names` — Warnungstypen

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `ID` | int (PK, AI) | CREATE | Typ-ID |
| `Name` | varchar(255) | CREATE | Typ-Name |

**Verknüpfungen:**
- `alert_names.ID` → `alerts.nameID` (1:n)

---

### 15. `alert_audiences` — Alert-Empfänger

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `alertsID` | int | CREATE | → `alerts.ID` |
| `audienceID` | tinyint | CREATE | Empfänger-ID |

**PK:** (`alertsID`, `audienceID`)

---

### 16. `kvs` — Key-Value-Speicher

Allgemeiner Datenspeicher für Konfiguration und Status.

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | varchar(64) (unique) | CREATE | Schlüssel |
| `ivalue` | int | CREATE | Integer-Wert |
| `dvalue` | double | CREATE | Double-Wert |
| `bvalue` | boolean | CREATE | Boolean-Wert |
| `ts` | date | CREATE | Zeitstempel |
| `JSON` | longtext | CREATE | JSON-Daten |
| `longvalue` | bigint | ALTER | Long-Wert |

---

### 17. `geocodecache` — Geocoding-Cache

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `lat` | double | CREATE | Breitengrad |
| `lng` | double | CREATE | Längengrad |
| `lastUpdate` | date | CREATE | Letztes Update |
| `address` | longtext | CREATE | Gespeicherte Adresse |

**Unique:** (`lat`, `lng`)

**Verknüpfungen:**
- `geocodecache.lat/lng` ← `pos.lat/lng` (Cache-Lookup)

---

### 18. `teslacharging` — Tesla-Ladehistorie

Importierte Ladedaten aus der Tesla API (Fleet API).

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `sessionId` | varchar(40) (unique) | CREATE | Session-ID |
| `chargeStartDateTime` | datetime | CREATE | Ladestart |
| `siteLocationName` | varchar(128) | CREATE | Ladestandort |
| `VIN` | varchar(20) | CREATE | Fahrzeuginummer → `cars.vin` |
| `json` | longtext | CREATE | Roh-JSON-Daten |

---

### 19. `komoot` — Komoot-Touren

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `tourID` | bigint (unique) | CREATE | Komoot-Tour-ID |
| `carID` | int | CREATE | → `cars.id` |
| `drivestateID` | int | CREATE | → `drivestate.id` |
| `json` | longtext | CREATE | Tour-Daten |

---

### 20. `journeys` — Reisen (Fahr- + Ladeabschnitte)

Aggregierte Reisen über mehrere Fahr- und Ladeevents.

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | Reise-ID |
| `CarID` | tinyint | CREATE | → `cars.id` |
| `StartPosID` | int | CREATE | → `pos.id` |
| `EndPosID` | int | CREATE | → `pos.id` |
| `consumption_kwh` | double | CREATE | Verbrauch |
| `charged_kwh` | double | CREATE | Geladene Energie |
| `drive_duration_minutes` | int | CREATE | Fahrzeit |
| `charge_duration_minutes` | int | CREATE | Ladezeit |
| `name` | varchar(250) | CREATE | Reisetitel |
| `freesuc` | double | ALTER | Kostenloses Supercharging |

---

### 21. `superchargers` — Supercharger-Standorte

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | Standort-ID |
| `name` | varchar(250) | CREATE | Name |
| `lat` | double | CREATE | Breitengrad |
| `lng` | double | CREATE | Längengrad |

**Verknüpfungen:**
- `superchargers.id` → `superchargerstate.nameid` (1:n)

---

### 22. `superchargerstate` — Supercharger-Verfügbarkeit

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | Status-ID |
| `nameid` | int | CREATE | → `superchargers.id` |
| `ts` | datetime | CREATE | Zeitstempel |
| `available_stalls` | tinyint | CREATE | Verfügbare Stellschalen |
| `total_stalls` | tinyint | CREATE | Gesamt-Stellschalen |

---

### 23. `mothership` — Mothership-Befehlsprotokoll

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | Eintrag-ID |
| `ts` | datetime | CREATE | Zeitstempel |
| `commandid` | int | CREATE | → `mothershipcommands.id` |
| `duration` | double | CREATE | Dauer (Sekunden) |
| `httpcode` | int | ALTER | → `httpcodes.id` |
| `carid` | int unsigned | ALTER | → `cars.id` |

**Unique:** (`id`, `ts`)

---

### 24. `mothershipcommands` — Mothership-Befehle

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK, AI) | CREATE | Befehls-ID |
| `command` | varchar(1024) | CREATE→ALTER | Befehls-URL (von varchar(50) erweitert) |

**Verknüpfungen:**
- `mothershipcommands.id` → `mothership.commandid` (1:n)

---

### 25. `httpcodes` — HTTP-Statuscodes

Lookup-Tabelle für HTTP-Antwortcodes.

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `id` | int (PK) | CREATE | HTTP-Code (z. B. 200, 404) |
| `text` | varchar(50) | CREATE | Beschreibung |

**Verknüpfungen:**
- `httpcodes.id` → `mothership.httpcode` (1:n)

---

### 26. `active_route_energy_at_arrival` — Routenenergie

Energieverfügbarkeit bei Ankunft auf der aktiven Route.

| Spalte | Typ | Herkunft | Beschreibung |
|---|---|---|---|
| `posID` | int | CREATE | → `pos.id` |
| `val` | tinyint | CREATE | Energie-Wert |

---

## Views

### `trip` — Fahrtrip-View

Join aus `drivestate`, `pos` (Start/Ende) und `cars`. Berechnet Verbrauch und Durchschnittsverbrauch pro 100 km.

```
drivestate ──StartPos──→ pos (pos_start)
drivestate ──EndPos──→   pos (pos_end)
drivestate ──CarID──→   cars
```

### `celltemperature` — Zelltemperatur-View

Union aus CAN-Daten (id=3) und Batterie-Modultemperaturen.

```
can (WHERE id=3)  →  celltemperature (source=1)
battery            →  celltemperature (source=2)
```

---

## Beziehungsdiagramm (textuell)

```
                        ┌─────────┐
                        │  cars   │
                        └────┬────┘
                             │ CarID (1:n)
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
    ┌───────┐           ┌────────┐          ┌──────────────┐
    │  pos  │           │charging│          │chargingstate │
    └───┬───┘           └───┬────┘          └──────┬───────┘
        │                   │                      │
        │ StartPos/EndPos   │ StartChargingID      │ Pos → pos.id
        │                   │ EndChargingID        │ StartChargingID → charging.id
        ▼                   │                      │ EndChargingID → charging.id
 ┌──────────────┐           │                      │ combined_into → self
 │ drivestate   │           │                      │
 └──────┬───────┘           │                      │
        │                   │                      │
        │ drivestateID      ▼                      ▼
        ▼              (zeitliche Messpunkte)  (Sessions)
    ┌───────┐
    │komoot │
    └───────┘

    ┌─────────┐     nameID     ┌──────────────┐
    │ alerts  │ ──────────────→│ alert_names   │
    └────┬────┘                └──────────────┘
         │ ID
         ▼
    ┌──────────────┐
    │alert_audiences│
    └──────────────┘

    ┌──────────────────┐  commandid  ┌────────────────────┐
    │   mothership     │ ──────────→ │mothershipcommands  │
    └──────────────────┘             └────────────────────┘
         │
         │ httpcode
         ▼
    ┌───────────┐
    │ httpcodes │
    └───────────┘

    ┌───────────────┐  nameid   ┌───────────────────┐
    │superchargerstate│ ──────→ │  superchargers     │
    └───────────────┘          └───────────────────┘

    ┌───────────┐  StartPosID/EndPosID  ┌───────┐
    │ journeys  │ ─────────────────────→│  pos  │
    └───────────┘                       └───────┘

    ┌───────────────┐  VIN  ┌───────┐
    │ teslacharging │ ────→ │ cars  │
    └───────────────┘       └───────┘
```

---

## Zusammenfassung der Verknüpfungen

| Quell-Tabelle | Quellspalte | → Ziel-Tabelle | Zielspalte |
|---|---|---|---|
| `pos` | `id` | `drivestate` | `StartPos`, `EndPos` |
| `pos` | `id` | `state` | `StartPos`, `EndPos` |
| `pos` | `id` | `chargingstate` | `Pos` |
| `pos` | `id` | `journeys` | `StartPosID`, `EndPosID` |
| `pos` | `id` | `active_route_energy_at_arrival` | `posID` |
| `charging` | `id` | `chargingstate` | `StartChargingID`, `EndChargingID` |
| `cars` | `id` | `pos`, `charging`, `chargingstate`, `drivestate`, `state`, `shiftstate`, `TPMS`, `can`, `car_version`, `battery`, `cruisestate`, `alerts`, `komoot` | `CarID` |
| `cars` | `id` | `journeys` | `CarID` |
| `cars` | `id` | `trip` (View) | `CarID` |
| `cars` | `vin` | `teslacharging` | `VIN` |
| `alert_names` | `ID` | `alerts` | `nameID` |
| `alerts` | `ID` | `alert_audiences` | `alertsID` |
| `mothershipcommands` | `id` | `mothership` | `commandid` |
| `httpcodes` | `id` | `mothership` | `httpcode` |
| `superchargers` | `id` | `superchargerstate` | `nameid` |
| `drivestate` | `id` | `komoot` | `drivestateID` |
| `chargingstate` | `id` | `chargingstate` | `combined_into` |
| `can` | `*` | `celltemperature` (View) | — |
| `battery` | `*` | `celltemperature` (View) | — |

---

## Migration-Quellen

Die Spalten-Herkunft (CREATE vs ALTER) stammt aus folgenden Dateien:

| Datei | Verantwortliche Tabellen |
|---|---|
| `sqlschema.sql` | Original-CREATE für alle Basistabellen |
| `UpdateTeslalogger.cs` | Alle ALTER TABLE-Migrationen (pos, drivestate, chargingstate, charging, cars, can, car_version, mothership, mothershipcommands) |
| `KVS.cs` | `kvs.longvalue` |
| `Journeys.cs` | `journeys.freesuc` |
| `GetChargingHistoryV2Service.cs` | `chargingstate.cost_freesuc_savings_total`, `chargingstate.sessionId`, `teslacharging` (CREATE) |
| `Komoot.cs` | `komoot` (CREATE) |
| `GeocodeCache.cs` | `geocodecache` (CREATE) |
