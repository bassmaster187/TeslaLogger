# Tesla Signal IDs Documentation

This document lists all available Tesla vehicle signals and their corresponding ID numbers used in TeslaLogger. These IDs are used by ScanMyTesla and other external apps. 

## Table of Contents
- [Battery & Cell Information](#battery--cell-information)
- [Thermal Management](#thermal-management)
- [HVAC System](#hvac-system)
- [Range & Energy](#range--energy)
- [Individual Cell Voltages](#individual-cell-voltages)
- [Individual Cell Temperatures](#individual-cell-temperatures)
- [Powertrain & Motors](#powertrain--motors)
- [Calculated Ah Capacity (CAC)](#calculated-ah-capacity-cac)

---

## Battery & Cell Information

| Signal Name | ID | Notes |
|-------------|-----|-------|
| Cell temp min | 1 | Minimum cell temperature |
| Cell temp avg | 2 | Average cell temperature |
| Cell temp mid | 2 | M3 variant |
| Cell temp max | 3 | Maximum cell temperature |
| Cell temp diff | 4 | Temperature difference between cells |
| Cell min | 5 | Minimum cell voltage |
| Cell volt min | 5 | M3 variant |
| Cell avg | 6 | Average cell voltage |
| Cell volt mid | 6 | M3 variant |
| Cell max | 7 | Maximum cell voltage |
| Cell volt max | 7 | M3 variant |
| Cell diff | 8 | Voltage difference between cells |
| Battery voltage | 30 | Total battery pack voltage |
| Battery heater temp | 31 | Battery heater temperature |
| Battery power | 43 | Current battery power (10% diff threshold) |
| Battery current | 44 | Current battery current (10% diff threshold) |
| Battery inlet | 64 | Battery coolant inlet temperature |
| Battery pump 1 | 67 | Battery pump 1 status/speed |
| Battery pump 2 | 68 | Battery pump 2 status/speed |
| Cell imbalance | 27 | Cell imbalance indicator |
| Nominal full pack | 71 | Nominal full pack energy |
| Nominal remaining | 72 | Nominal remaining energy |
| Usable full pack | 73 | Usable full pack energy |
| Expected remaining | 74 | Expected remaining energy |
| Ideal remaining | 75 | Ideal remaining energy |
| Usable remaining | 86 | Usable remaining energy |
| Energy buffer | 87 | Energy buffer |
| Battery flow | 445 | M3 battery coolant flow (10% diff threshold) |

## Charging & Energy

| Signal Name | ID | Notes |
|-------------|-----|-------|
| AC Charge total | 9 | Total AC charging energy |
| DC Charge total | 11 | Total DC charging energy |
| Charge total | 13 | Total charging energy (AC + DC) |
| Regen total | 16 | Total regenerated energy |
| Discharge total | 20 | Total discharged energy |
| BMS max charge | 28 | BMS maximum charge power |
| Max charge power | 28 | M3 variant |
| BMS max discharge | 29 | BMS maximum discharge power |
| Max discharge power | 29 | M3 variant |
| To charge complete | 79 | Energy needed to complete charge |
| Discharge cycles | 76 | Number of discharge cycles |
| Charge cycles | 88 | Number of charge cycles |

## State of Charge & Range

| Signal Name | ID | Notes |
|-------------|-----|-------|
| SOC | 23 | State of Charge (actual) |
| SOC UI | 24 | State of Charge (UI display) |
| SOC Min | 25 | Minimum State of Charge |
| Typical range | 40 | Typical range estimate |
| Rated range | 65 | Rated range estimate |
| Full typical range | 62 | Full typical range at 100% |
| Full rated range | 63 | Full rated range at 100% |
| Odometer | 26 | Current odometer reading |
| Odometer(legacy) | 66 | Legacy odometer reading |
| Odometer (legacy) | 89 | Alternative legacy odometer |

## DC-DC Converter

| Signal Name | ID | Notes |
|-------------|-----|-------|
| DC-DC coolant inlet | 33 | DC-DC converter coolant inlet temp |
| DC-DC current | 34 | DC-DC converter current (10% diff threshold) |
| DC-DC efficiency | 35 | DC-DC converter efficiency (10% diff threshold) |
| DC-DC output voltage | 36 | DC-DC output voltage |
| DC-DC input power | 37 | DC-DC input power (10% diff threshold) |
| DC-DC output power | 38 | DC-DC output power (10% diff threshold) |

## Thermal Management

| Signal Name | ID | Notes |
|-------------|-----|-------|
| Heater L | 41 | Left heater power |
| Heater left | 41 | M3 variant |
| Heater R | 42 | Right heater power |
| Heater right | 42 | M3 variant |
| Radiator bypass | 69 | Radiator bypass valve position |
| Refrigerant temp | 70 | Refrigerant temperature |
| Powertrain pump | 78 | Powertrain pump status/speed |
| Powertrain pump 2 | 90 | Powertrain pump 2 status/speed |
| PT inlet | 80 | Powertrain coolant inlet temp |
| Powertrain inlet | 80 | M3 variant |
| PTC air heater | 81 | PTC air heater power |
| Coolant heater | 82 | Coolant heater power |
| Thermal controller 400V | 83 | MS thermal controller 400V |
| Thermal controller | 84 | MS thermal controller |
| Thermal controller 12V | 85 | MS thermal controller 12V |
| Chiller bypass | 57 | Chiller bypass valve position |
| Powertrain flow | 444 | M3 powertrain coolant flow (10% diff threshold) |

## HVAC System

| Signal Name | ID | Notes |
|-------------|-----|-------|
| HVAC on/off | 45 | HVAC system on/off status |
| HVAC A/C | 46 | A/C compressor status |
| HVAC fan speed | 47 | HVAC fan speed |
| HVAC window | 48 | Window defrost status |
| HVAC temp left | 49 | Left temperature setting |
| HVAC temp right | 50 | Right temperature setting |
| HVAC mid | 51 | Mid vent status |
| HVAC floor | 52 | Floor vent status |
| Mid vent L | 53 | Mid vent left position |
| Floor vent L | 54 | Floor vent left position |
| Mid vent R | 55 | Mid vent right position |
| Floor vent R | 56 | Floor vent right position |
| Inside temp | 58 | Interior temperature |
| Outside temp filtered | 59 | Filtered outside temperature |
| A/C air temp | 60 | A/C air temperature |
| Outside temp | 61 | Outside temperature |

## Louvers

| Signal Name | ID | Notes |
|-------------|-----|-------|
| Louver 1 | 91 | Louver 1 position |
| Louver 2 | 92 | Louver 2 position |
| Louver 3 | 93 | Louver 3 position |
| Louver 4 | 94 | Louver 4 position |
| Louver 5 | 95 | Louver 5 position |
| Louver 6 | 96 | Louver 6 position |
| Louver 7 | 97 | Louver 7 position |
| Louver 8 | 98 | Louver 8 position |

## Individual Cell Voltages

Cell voltages are numbered from 1 to 108, with IDs starting at 101.

| Signal Name | ID Range | Notes |
|-------------|----------|-------|
| Cell 1-108 voltage | 101-208 | Individual cell voltages (ID = 100 + cell number) |

### Detailed Cell Voltage IDs

- Cell 1 voltage: 101
- Cell 2 voltage: 102
- ...
- Cell 108 voltage: 208

## Individual Cell Temperatures

Cell temperatures are numbered from 1 to 31, with IDs starting at 301.

| Signal Name | ID Range | Notes |
|-------------|----------|-------|
| Cell 1-31 temp | 301-331 | Individual cell temperatures (ID = 300 + cell number) |

### Detailed Cell Temperature IDs

- Cell 1 temp: 301
- Cell 2 temp: 302
- ...
- Cell 31 temp: 331

## Powertrain & Motors

### Front Motor

| Signal Name | ID | Notes |
|-------------|-----|-------|
| Fr torque measured | 400 | Front motor measured torque |
| F torque | 400 | M3 variant |
| Fr mech power | 405 | Front motor mechanical power |
| F power | 405 | M3 variant |
| Fr dissipation | 406 | Front motor power dissipation |
| Fr input power | 407 | Front motor input power |
| Fr mech power HP | 408 | Front motor power in HP |
| Fr stator current | 409 | Front motor stator current |
| Fr drive power max | 410 | Front motor max drive power |
| Fr efficiency | 413 | Front motor efficiency |
| Fr torque estimate | 424 | Front motor estimated torque |
| Fr motor RPM | 433 | Front motor RPM |
| F Stator temp | 443 | Front motor stator temperature (10% diff threshold) |

### Rear Motor

| Signal Name | ID | Notes |
|-------------|-----|-------|
| Rr torque measured | 403 | Rear motor measured torque |
| R torque | 403 | M3 variant |
| Rr inverter 12V | 414 | Rear inverter 12V system |
| Rr mech power | 415 | Rear motor mechanical power |
| R power | 415 | M3 variant |
| Rr dissipation | 416 | Rear motor power dissipation |
| Rr input power | 417 | Rear motor input power |
| Rr mech power HP | 419 | Rear motor power in HP |
| Rr stator current | 420 | Rear motor stator current |
| Rr regen power max | 421 | Rear motor max regen power |
| Rr drive power max | 422 | Rear motor max drive power |
| Rr efficiency | 423 | Rear motor efficiency |
| Rr torque estimate | 425 | Rear motor estimated torque |
| Rr coolant inlet | 427 | Rear motor coolant inlet temp |
| Rr inverter PCB | 428 | Rear inverter PCB temperature |
| Rr stator | 429 | Rear motor stator temperature |
| R Stator temp | 429 | M3 variant |
| Rr DC capacitor | 430 | Rear DC capacitor temperature |
| Rr heat sink | 431 | Rear heat sink temperature |
| Rr inverter | 432 | Rear inverter temperature |
| Rr motor RPM | 434 | Rear motor RPM |

### Combined & Drive

| Signal Name | ID | Notes |
|-------------|-----|-------|
| Rr/Fr torque bias | 401 | Torque distribution bias |
| Mech power combined | 411 | Combined mechanical power |
| HP combined | 412 | Combined power in HP |
| Propulsion | 418 | Total propulsion power |
| Consumption | 426 | Power consumption |
| Series/Parallel | 77 | Series/Parallel configuration |

### Driver Inputs & Vehicle Speed

| Signal Name | ID | Notes |
|-------------|-----|-------|
| Watt pedal | 404 | Accelerator pedal position |
| Accelerator Pedal | 404 | M3 variant |
| Brake pedal | 435 | Brake pedal position |
| Speed | 442 | Vehicle speed |

### Wheels & Drive Ratios

| Signal Name | ID | Notes |
|-------------|-----|-------|
| Front left | 436 | Front left wheel speed |
| Front right | 437 | Front right wheel speed |
| Front drive ratio | 438 | Front drive ratio |
| Rear left | 439 | Rear left wheel speed |
| Rear right | 440 | Rear right wheel speed |
| Rear drive ratio | 441 | Rear drive ratio |

## Calculated Ah Capacity (CAC)

| Signal Name | ID | Notes |
|-------------|-----|-------|
| CAC min | 450 | CAC minimum value |
| CAC avg | 451 | CAC average value |
| CAC max | 452 | CAC maximum value |
| CAC imbalance | 453 | CAC imbalance |
| CAC min brick id | 454 | CAC minimum brick ID |
| CAC max brick id | 455 | CAC maximum brick ID |

---

## Notes

### Signal Value Changes
Some signals are marked with a **10% difference threshold** (`diff10percent`). These signals only update when the value changes by more than 10%:
- DC-DC current (34)
- DC-DC efficiency (35)
- DC-DC input power (37)
- DC-DC output power (38)
- Battery power (43)
- Battery current (44)
- F Stator temp (443)
- Powertrain flow (444)
- Battery flow (445)

### Model Variations
- **M3**: Model 3 specific signal names (often more descriptive)
- **MS**: Model S specific signals
- Some signals have multiple names for different Tesla models

---

*Last updated: December 2025*
