---
sidebar_position: 10
---
# Wallbox

This function allows the integration of wallboxes to generate better charging statistics.
Depending on the wallbox and available data, the following additional data is calculated and displayed under Charging History:
- Charging efficiency
- PV share
- Charging price

![IMAGE](/img/extras-wallbox-01.png)

For setup, you need at least a hostname or IP address of the wallbox. Host or IP address must be entered with http:// or https:// prefix!
For some, additional port and/or additional parameters are specified.

With the test button, the connection is checked.
If the test was successful, at least the version number is displayed:

![IMAGE](/img/extras-wallbox-02.png)

Currently supported wallboxes:
- OpenWB
- go-eCharger
- Tesla Wall Connector Gen 3
- KEBA KeContact P30 (P20?)
- Shelly 3EM for all unsupported wallboxes or without built-in meter: https://amzn.to/3nUEuO0
- Shelly EM for 1-phase charging
- EVCC



Special notes:

- go-eCharger
Parameters: leave empty
"Local HTTP API v1" must be activated under "Settings -> Advanced Settings"

- OpenWB
Parameters: LP1-LP8 depending on charging point (If nothing is entered, LP1 is assumed)
Works only up to firmware 1.9 (via HTTP)

- ShellyEM
Parameters: C1 (or leave empty) for channel 1 or C2 for channel 2

- EVCC
Host/IP address with port (usually :7070)
Parameters: Wallbox title (not from the car!)
