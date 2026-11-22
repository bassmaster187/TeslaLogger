# Tesla Signal IDs Documentation

This document lists all available Tesla vehicle signals and their corresponding ID numbers used in TeslaLogger. These IDs are used by ScanMyTesla and other external apps.

Every ID is stored unmodified in the `can` table (`datum`, `id`, `val`, `CarID`) and can be used in Grafana. How the packets are transferred to Teslalogger (endpoint, JSON format, rate limits) is described in [App data interface](/docs/extras/scanmytesla-api).

## Table of Contents
- [Units and conventions](#units-and-conventions)
- [Battery & Cell Information](#battery--cell-information)
- [12 V / 400 V Consumers](#12-v--400-v-consumers)
- [Charging & Energy](#charging--energy)
- [State of Charge & Range](#state-of-charge--range)
- [DC-DC Converter](#dc-dc-converter)
- [Thermal Management](#thermal-management)
- [HVAC System](#hvac-system)
- [Louvers](#louvers)
- [Individual Cell Voltages](#individual-cell-voltages)
- [Individual Cell Temperatures](#individual-cell-temperatures)
- [Powertrain & Motors](#powertrain--motors)
- [Calculated Ah Capacity (CAC)](#calculated-ah-capacity-cac)
- [Reserved ID ranges](#reserved-id-ranges)
- [Signals evaluated by Teslalogger](#signals-evaluated-by-teslalogger)
- [Notes](#notes)

---

## Units and conventions

The **Unit** column is the unit the app has to send. Teslalogger does not convert or scale anything, the value is written to the database exactly as received.

- **Battery current (44) and battery power (43): positive = discharge (driving), negative = charging / recuperation.**
- **Cell voltages are sent in V, not in mV** (IDs 5-7 and 101-208). The difference of cell voltages exists twice: ID 8 `Cell diff` in **V** and ID 27 `Cell imbalance` in **mV**. Teslalogger uses ID 27 for the cell imbalance shown in the web UI and in the dashboards. ScanMyTesla reads the cells with a resolution of 0.1 mV on Model 3/Y (`value * 0.0001` V) and 0.305 mV on Model S/X, so three decimal places in the packet are enough.
- **Signals that are not available should be omitted from the packet**, not sent as `0`. Values containing `NaN` or `Infinity` are dropped by Teslalogger. For Speed (442) the value `287.6` is treated as "signal not available".
- Some signals differ between model lines (see the **Notes** column), for example ID 41/42: temperature in °C on Model S/X, heater power in W on Model 3/Y.
- IDs that Teslalogger does not know are still stored and can be used in Grafana, but they do not appear in `/currentjson/{id}`, the web UI or the MCP server. See [Signals evaluated by Teslalogger](#signals-evaluated-by-teslalogger).

## Battery & Cell Information

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| Cell temp min | 1 | °C | Minimum cell temperature |
| Cell temp avg | 2 | °C | Average cell temperature |
| Cell temp mid | 2 | °C | M3 variant |
| Cell temp max | 3 | °C | Maximum cell temperature |
| Cell temp diff | 4 | °C | Temperature difference between cells |
| Cell min | 5 | V | Minimum cell voltage |
| Cell volt min | 5 | V | M3 variant |
| Cell avg | 6 | V | Average cell voltage |
| Cell volt mid | 6 | V | M3 variant |
| Cell max | 7 | V | Maximum cell voltage |
| Cell volt max | 7 | V | M3 variant |
| Cell diff | 8 | V | Voltage difference between cells (max − min). The same value in mV is ID 27 |
| Battery voltage | 30 | V | Total battery pack voltage |
| Battery heater temp | 31 | °C \* | Battery heater temperature |
| Battery power | 43 | kW | Current battery power (10% diff threshold). Positive = discharge, negative = charge/regen |
| Battery current | 44 | A | Current battery current (10% diff threshold). Positive = discharge, negative = charge/regen |
| Battery inlet | 64 | °C | Battery coolant inlet temperature |
| Battery pump 1 | 67 | % | Battery pump 1 status/speed |
| Battery pump 2 | 68 | % | Battery pump 2 status/speed |
| Cell imbalance | 27 | mV | Cell imbalance in mV (max − min), used by the Teslalogger web UI |
| Nominal full pack | 71 | kWh | Nominal full pack energy |
| Nominal remaining | 72 | kWh | Nominal remaining energy |
| Usable full pack | 73 | kWh \* | Usable full pack energy |
| Expected remaining | 74 | kWh | Expected remaining energy |
| Ideal remaining | 75 | kWh | Ideal remaining energy |
| Usable remaining | 86 | kWh | Usable remaining energy |
| Energy buffer | 87 | kWh | Energy buffer |
| Battery flow | 445 | l/min | M3 battery coolant flow (10% diff threshold) |

## 12 V / 400 V Consumers

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| 12v systems | 32 | W | Total 12 V consumer power, Model S/X (10% diff threshold) |
| 400V systems | 39 | kW | Total 400 V consumer power, Model S/X |

## Charging & Energy

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| AC Charge total | 9 | kWh | Total AC charging energy |
| DC Charge total | 11 | kWh | Total DC charging energy |
| Charge total | 13 | kWh | Total charging energy (AC + DC) |
| Regen total | 16 | kWh | Total regenerated energy |
| Discharge total | 20 | kWh | Total discharged energy |
| BMS max charge | 28 | kW | BMS maximum charge power |
| Max charge power | 28 | kW | M3 variant |
| BMS max discharge | 29 | kW | BMS maximum discharge power |
| Max discharge power | 29 | kW | M3 variant |
| To charge complete | 79 | kWh | Energy needed to complete charge |
| Discharge cycles | 76 | count | Number of discharge cycles |
| Charge cycles | 88 | count | Number of charge cycles |

## State of Charge & Range

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| SOC | 23 | % | State of Charge of the BMS, calculated as (nominal remaining − buffer) / (nominal full pack − buffer) × 100, with decimals |
| SOC UI | 24 | % | State of Charge as displayed in the car, 0.1 % steps |
| SOC Min | 25 | % | Minimum State of Charge |
| Typical range | 40 | km | Typical range estimate |
| Rated range | 65 | km | Rated range estimate |
| Full typical range | 62 | km | Full typical range at 100% |
| Full rated range | 63 | km | Full rated range at 100% |
| Odometer | 26 | km | Current odometer reading |
| Odometer(legacy) | 66 | km | Legacy odometer reading |
| Odometer (legacy) | 89 | km | Alternative legacy odometer |

## DC-DC Converter

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| DC-DC coolant inlet | 33 | °C | DC-DC converter coolant inlet temp |
| DC-DC current | 34 | A | DC-DC converter current (10% diff threshold) |
| DC-DC efficiency | 35 | % | DC-DC converter efficiency (10% diff threshold) |
| DC-DC output voltage | 36 | V | DC-DC output voltage (called "DC-DC voltage" in the app) |
| DC-DC input power | 37 | W | DC-DC input power (10% diff threshold) |
| DC-DC output power | 38 | W | DC-DC output power (10% diff threshold) |

## Thermal Management

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| Heater L | 41 | °C (S/X) / W (3/Y) | Left heater: outlet temperature on Model S/X, heater power on Model 3/Y |
| Heater left | 41 | W | M3 variant |
| Heater R | 42 | °C (S/X) / W (3/Y) | Right heater: outlet temperature on Model S/X, heater power on Model 3/Y |
| Heater right | 42 | W | M3 variant |
| Radiator bypass | 69 | % | Radiator bypass valve position |
| Refrigerant temp | 70 | °C | Refrigerant temperature |
| Powertrain pump | 78 | % | Powertrain pump status/speed |
| Powertrain pump 2 | 90 | % | Powertrain pump 2 status/speed |
| PT inlet | 80 | °C | Powertrain coolant inlet temp |
| Powertrain inlet | 80 | °C | M3 variant |
| PTC air heater | 81 | % | PTC air heater power |
| Coolant heater | 82 | % | Coolant heater power |
| Thermal controller 400V | 83 | kW | MS thermal controller 400V |
| Thermal controller | 84 | kW | MS thermal controller |
| Thermal controller 12V | 85 | kW | MS thermal controller 12V |
| Chiller bypass | 57 | % | Chiller bypass valve position |
| Powertrain flow | 444 | l/min | M3 powertrain coolant flow (10% diff threshold) |

## HVAC System

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| HVAC on/off | 45 | 0 / 1 | HVAC system on/off status |
| HVAC A/C | 46 | 0 / 1 | A/C compressor status |
| HVAC fan speed | 47 | level | HVAC fan speed (blower level, not a physical unit) |
| HVAC window | 48 | 0 / 1 | Window defrost status |
| HVAC temp left | 49 | °C | Left temperature setting |
| HVAC temp right | 50 | °C | Right temperature setting |
| HVAC mid | 51 | 0 / 1 | Mid vent status |
| HVAC floor | 52 | 0 / 1 | Floor vent status |
| Mid vent L | 53 | °C | Mid vent left position |
| Floor vent L | 54 | °C | Floor vent left position |
| Mid vent R | 55 | °C | Mid vent right position |
| Floor vent R | 56 | °C | Floor vent right position |
| Inside temp | 58 | °C | Interior temperature |
| Outside temp filtered | 59 | °C | Filtered outside temperature |
| A/C air temp | 60 | °C | A/C air temperature |
| Outside temp | 61 | °C | Outside temperature |

## Louvers

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| Louver 1 | 91 | % | Louver 1 position |
| Louver 2 | 92 | % | Louver 2 position |
| Louver 3 | 93 | % | Louver 3 position |
| Louver 4 | 94 | % | Louver 4 position |
| Louver 5 | 95 | % | Louver 5 position |
| Louver 6 | 96 | % | Louver 6 position |
| Louver 7 | 97 | % | Louver 7 position |
| Louver 8 | 98 | % | Louver 8 position |

## Individual Cell Voltages

Cell voltages are numbered from 1 to 108, with IDs starting at 101. The unit is **V** (not mV).

| Signal Name | ID Range | Unit | Notes |
|-------------|----------|------|-------|
| Cell 1-108 voltage | 101-208 | V | Individual cell voltages (ID = 100 + cell number) |

### Detailed Cell Voltage IDs

- Cell 1 voltage: 101
- Cell 2 voltage: 102
- ...
- Cell 108 voltage: 208

The current ScanMyTesla (build 206) maps cells 1-108 to IDs 101-208. Only the cells that actually report a value are sent; how many that are depends on the pack, the app parses up to 108 cells on Model 3/Y and up to 96 on Model S/X. The cell voltage dashboards shipped with Teslalogger cover cells 1-99 (IDs 101-199); older app versions only send cells 1-99.

## Individual Cell Temperatures

Cell temperatures are numbered from 1 to 31, with IDs starting at 301. The unit is **°C**.

| Signal Name | ID Range | Unit | Notes |
|-------------|----------|------|-------|
| Cell 1-31 temp | 301-331 | °C | Individual cell temperatures (ID = 300 + sensor number) |

### Detailed Cell Temperature IDs

- Cell 1 temp: 301
- Cell 2 temp: 302
- ...
- Cell 31 temp: 331

The current ScanMyTesla reads these probes on Model S/X; on Model 3/Y build 206 only the aggregate values (IDs 1-3) are available, so a second app should not assume that 301-331 exist for every model. IDs 332-399 were addressed by older app versions and are no longer in use.

## Powertrain & Motors

### Front Motor

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| Fr torque measured | 400 | Nm | Front motor measured torque |
| F torque | 400 | Nm | M3 variant |
| Fr mech power | 405 | kW | Front motor mechanical power |
| F power | 405 | kW | M3 variant |
| Fr dissipation | 406 | kW | Front motor power dissipation |
| Fr input power | 407 | kW | Front motor input power |
| Fr mech power HP | 408 | HP | Front motor power in HP |
| Fr stator current | 409 | A | Front motor stator current |
| Fr drive power max | 410 | kW | Front motor max drive power |
| Fr efficiency | 413 | % | Front motor efficiency |
| Fr torque estimate | 424 | Nm | Front motor estimated torque |
| Fr motor RPM | 433 | RPM | Front motor RPM |
| F Stator temp | 443 | °C | Front motor stator temperature (10% diff threshold) |

### Rear Motor

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| Rr torque measured | 403 | Nm | Rear motor measured torque |
| R torque | 403 | Nm | M3 variant |
| Rr inverter 12V | 414 | V | Rear inverter 12V system |
| Rr mech power | 415 | kW | Rear motor mechanical power |
| R power | 415 | kW | M3 variant |
| Rr dissipation | 416 | kW | Rear motor power dissipation |
| Rr input power | 417 | kW | Rear motor input power |
| Rr mech power HP | 419 | HP | Rear motor power in HP |
| Rr stator current | 420 | A | Rear motor stator current |
| Rr regen power max | 421 | kW | Rear motor max regen power |
| Rr drive power max | 422 | kW | Rear motor max drive power |
| Rr efficiency | 423 | % | Rear motor efficiency |
| Rr torque estimate | 425 | Nm | Rear motor estimated torque |
| Rr coolant inlet | 427 | °C | Rear motor coolant inlet temp |
| Rr inverter PCB | 428 | °C | Rear inverter PCB temperature |
| Rr stator | 429 | °C | Rear motor stator temperature |
| R Stator temp | 429 | °C | M3 variant |
| Rr DC capacitor | 430 | °C | Rear DC capacitor temperature |
| Rr heat sink | 431 | °C | Rear heat sink temperature |
| Rr inverter | 432 | °C | Rear inverter temperature |
| Rr motor RPM | 434 | RPM | Rear motor RPM |

### Combined & Drive

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| Rr/Fr torque bias | 401 | % | Torque distribution bias |
| Mech power combined | 411 | kW | Combined mechanical power |
| HP combined | 412 | HP | Combined power in HP |
| Propulsion | 418 | kW | Total propulsion power |
| Consumption | 426 | Wh/km | Power consumption |
| Series/Parallel | 77 | % | Series/Parallel configuration (Model S/X) |

### Driver Inputs & Vehicle Speed

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| Watt pedal | 404 | % | Accelerator pedal position |
| Accelerator Pedal | 404 | % | M3 variant |
| Brake pedal | 435 | % (S/X) / 0-1 (3/Y) | Brake pedal position |
| Speed | 442 | km/h | Vehicle speed. 287.6 means "signal not available" and is stored as 0 by Teslalogger |

### Wheels & Drive Ratios

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| Front left | 436 | RPM | Front left wheel speed |
| Front right | 437 | RPM | Front right wheel speed |
| Front drive ratio | 438 | :1 | Front drive ratio |
| Rear left | 439 | RPM | Rear left wheel speed |
| Rear right | 440 | RPM | Rear right wheel speed |
| Rear drive ratio | 441 | :1 | Rear drive ratio |

## Calculated Ah Capacity (CAC)

| Signal Name | ID | Unit | Notes |
|-------------|-----|------|-------|
| CAC min | 450 | Ah | CAC minimum value, 0.1 Ah resolution |
| CAC avg | 451 | Ah | CAC average value, 0.1 Ah resolution |
| CAC max | 452 | Ah | CAC maximum value, 0.1 Ah resolution |
| CAC imbalance | 453 | Ah | CAC max − CAC min |
| CAC min brick id | 454 | number | Brick number (1-127) with the smallest CAC |
| CAC max brick id | 455 | number | Brick number (1-127) with the largest CAC |

Teslalogger does not evaluate IDs 450-455 yet, they are only stored in `can` and can be used in Grafana.

---

## Reserved ID ranges

`can.id` is a signed `mediumint`, so the technical limit is 8 388 607. The following ranges are in use and must not be used for new signals:

| Range | Meaning |
|-------|---------|
| 0 | "do not send" marker in the apps, never stored |
| 1-99 | main signals (see tables above) |
| 101-208 | cell voltages |
| 301-331 | cell temperatures |
| 400-455 | powertrain, wheels, CAC |
| 1000 | reserved by Teslalogger, used internally for Lucid "Battery Health Level" (SOH) |

Free for new signals: 100, 209-300, 332-399, 456-999 and 1001 and above. New IDs must be coordinated with the Teslalogger project before they are used, so that they do not collide with signals of other apps or of the fleet telemetry integration.

## Signals evaluated by Teslalogger

Only the signals below are handled specially by Teslalogger. All other IDs are only written to the `can` table (`datum`, `id`, `val`, `CarID`) and can be used in own Grafana dashboards, for example with a query like the one the shipped dashboards use:

```sql
SELECT $__time(datum), val as 'Cell 1 [V]'
FROM can
WHERE $__timeFilter(datum) and carid=$Car and id = 101
ORDER BY datum ASC
```

The user side of the integration is described in [ScanMyTesla](/docs/faq/scanmytesla).

| ID | Teslalogger field | Used for |
|----|-------------------|----------|
| 2 | `SMTCellTempAvg` | /currentjson, web UI |
| 5 | `SMTCellMinV` | /currentjson |
| 6 | `SMTCellAvgV` | /currentjson |
| 7 | `SMTCellMaxV` | /currentjson |
| 9 | `SMTACChargeTotal` | /currentjson |
| 11 | `SMTDCChargeTotal` | /currentjson |
| 27 | `SMTCellImbalance` | /currentjson, web UI ("Cell Imbalance" in mV) |
| 28 | `SMTBMSmaxCharge` | /currentjson |
| 29 | `SMTBMSmaxDischarge` | /currentjson |
| 43 | `SMTBatteryPower` | internal: prevents the car from falling asleep while it is still driving (> 2 kW) |
| 71 | `SMTNominalFullPack` | /currentjson |
| 442 | `SMTSpeed` | internal: prevents the car from falling asleep, wakes the car up |

The fields with a `/currentjson` entry are only published while the last packet was received less than 5 minutes ago.

---

## Notes

### Signal Value Changes

The change filter is implemented in the sending app, **not** on the Teslalogger server. The server stores every value it receives exactly once, without any threshold.

ScanMyTesla (build 206) sends a signal only when it differs from the last sent value of that signal:

- by more than **1 %** by default,
- by more than **10 %** for the signals below (`diff10percent`):
  - 12v systems (32)
  - DC-DC current (34)
  - DC-DC efficiency (35)
  - DC-DC input power (37)
  - DC-DC output power (38)
  - Battery power (43)
  - Battery current (44)
  - F Stator temp (443)
  - Powertrain flow (444)
  - Battery flow (445)
- by a fixed **0.1 kWh** for Nominal full pack (71) and Energy buffer (87),
- by a fixed **0.2 kWh** for Nominal remaining (72), Expected remaining (74) and Ideal remaining (75),
- by a fixed **2 mV** (0.002 V) for the cell voltage IDs 100-208.

The percentage thresholds are calculated as `last value * percentage`, so a negative last value inverts the range and the signal is sent on every change — worth knowing for signals such as battery current (44) and battery power (43).

A signal that does not change simply does not appear in the next packet, the previous value stays in the database, and the Grafana panels continue the curve from there.

Because the primary key of the `can` table is `(datum, id)` with one second resolution, the same ID must not be sent twice in the same second — in that case the whole packet is rejected. See [App data interface](/docs/extras/scanmytesla-api).

### Model Variations
- **M3**: Model 3 specific signal names (often more descriptive)
- **MS**: Model S specific signals
- Some signals have multiple names for different Tesla models

### Units marked with \*

The unit of ID 31 (Battery heater temp) and 73 (Usable full pack) could not be taken from the app sources: both signals are listed in the signal dictionary of ScanMyTesla build 206, but no CAN packet in that build writes a value for them, so no unit is defined there. The unit shown above follows from the signal name and the neighbouring signals.

### Source of this documentation

The ID list and the units match the current ScanMyTesla sources (MAUI, build 206 / 3.2.6, `SignalDictionary.cs`, `Model3Packets.cs`, `ModelSPackets.cs`) signal for signal — all 280 entries of the app dictionary are covered here. The Teslalogger side is described as implemented in `TeslaLogger/ScanMyTesla.cs`.

---

*Last updated: November 2026 — units, missing signals (32, 39), reserved ID ranges and the app-side change filter added.*
