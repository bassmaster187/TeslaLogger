# Local Tesla HTTP Proxy and Telemetry Server

## ATTENTION: ##
This implemention is without any support. Ask community and not the developer for help!

You are responsible for the required and correct car configuration for TeslaLogger. 
Theoreticaly each TeslaLogger update, that uses more signals than in your config defined, can couse missed data. Be aware of it.

## Purpose: ##
You can use your own local server, with own car configs, other signal frequency and also utilize 10€/10$ account discount that Tesla gives you in your developer account.

## Requierments: ##
Only local intances are supported (without authentification)!

For more information: https://github.com/teslamotors/fleet-telemetry?tab=readme-ov-file#configuring-and-running-the-service

- All needed keys, certificates, car profiles etc. are available and functioning
- Tesla Http Proxy is running
- Tesla Telemetry Server is running
- Telemetry Server outputs via ZeroMQ protocol
<details>

<summary>Recomended/tested telemetry config:</summary>

```json
{
    "host": "",
    "port": 12345,
    "log_level": "debug",
    "json_log_enable": true,
    "namespace": "TeslaLogger",
    "reliable_ack": true,
    "transmit_decoded_records": true,
    "logger": {
      "verbose": true
    },
    "zmq": {
        "addr": "tcp://*:5284",
        "verbose": true
    },
    "records": {
      "alerts": [
          "zmq"
      ],
      "errors": [
          "zmq"
      ],
      "V": [
          "zmq"
      ]
    },
    "tls": {
      "server_cert": "/certs/fullchain.pem",
      "server_key": "/certs/private.pem"
    }
}
```

</details>

<details>

<summary>Recomended car profile:</summary>

```json
{
  "vins": ["XP7XXXXXXXX0000000"],
  "config": {
    "hostname": "telemetry.yourdomain.com",
    "port": 12345,
    "ca": "${ca_data}",
    "prefer_typed": true,
    "fields": {
      "PackVoltage": { "interval_seconds": 10, "resend_interval_seconds": 120, "minimum_delta": 0.5 },
      "PackCurrent": { "interval_seconds": 10, "resend_interval_seconds": 120, "minimum_delta": 0.2 },
      "ACChargingPower": { "interval_seconds": 10, "resend_interval_seconds": 120, "minimum_delta": 0.1 },
      "ACChargingEnergyIn" : { "interval_seconds": 60, "resend_interval_seconds": 600, "minimum_delta": 0.1 },
      "DCChargingPower": { "interval_seconds": 1, "resend_interval_seconds": 120, "minimum_delta": 0.1 },
      "DCChargingEnergyIn" : { "interval_seconds": 30, "resend_interval_seconds": 120, "minimum_delta": 0.1},
      "ChargeLimitSoc": { "interval_seconds": 30, "resend_interval_seconds": 86400, "minimum_delta": 1 },
      "FastChargerPresent": { "interval_seconds": 5 },
      "Location": { "interval_seconds": 1, "minimum_delta": 3},
      "VehicleSpeed": { "interval_seconds": 1 },
      "Gear": { "interval_seconds": 5, "resend_interval_seconds": 120 },
      "EstBatteryRange": { "interval_seconds": 30 },
      "RatedRange": { "interval_seconds": 30 },
      "IdealBatteryRange": { "interval_seconds": 30 },
      "Soc": { "interval_seconds": 15, "resend_interval_seconds": 300, "minimum_delta": 0.1 },
      "ModuleTempMax": { "interval_seconds": 60 , "minimum_delta": 0.1 },
      "NumModuleTempMax": { "interval_seconds": 60 },
      "ModuleTempMin": { "interval_seconds": 60 , "minimum_delta": 0.1 },
      "NumModuleTempMin": { "interval_seconds": 60 },
      "NumBrickVoltageMax": { "interval_seconds": 60 },
      "BrickVoltageMax": { "interval_seconds": 60, "minimum_delta": 0.001 },
      "NumBrickVoltageMin": { "interval_seconds": 60 },
      "BrickVoltageMin": { "interval_seconds": 60, "minimum_delta": 0.001 },
      "Odometer": { "interval_seconds": 30 },
      "EnergyRemaining": { "interval_seconds": 60, "minimum_delta": 0.1 },
      "TimeToFullCharge": { "interval_seconds": 60 },
      "EstBatteryRange": { "interval_seconds": 60, "minimum_delta": 0.1 },
      "SentryMode": { "interval_seconds": 10 },
      "ChargeState": { "interval_seconds": 10 },
      "DetailedChargeState": { "interval_seconds": 10, "resend_interval_seconds": 120 },
      "BatteryHeaterOn": { "interval_seconds": 10 },
      "DoorState": { "interval_seconds": 10, "resend_interval_seconds": 300 },
      "FdWindow": { "interval_seconds": 10, "resend_interval_seconds": 300 },
      "FpWindow": { "interval_seconds": 10, "resend_interval_seconds": 300 },
      "RdWindow": { "interval_seconds": 10, "resend_interval_seconds": 300 },
      "RpWindow": { "interval_seconds": 10, "resend_interval_seconds": 300 },
      "TpmsPressureFl": { "interval_seconds": 10, "resend_interval_seconds": 300, "minimum_delta": 0.01 },
      "TpmsPressureFr": { "interval_seconds": 10, "resend_interval_seconds": 300, "minimum_delta": 0.01 },
      "TpmsPressureRl": { "interval_seconds": 10, "resend_interval_seconds": 300, "minimum_delta": 0.01 },
      "TpmsPressureRr": { "interval_seconds": 10, "resend_interval_seconds": 300, "minimum_delta": 0.01 },
      "SpeedLimitMode": { "interval_seconds": 30 },
      "VehicleName": { "interval_seconds": 600 },
      "CarType": { "interval_seconds": 600 },
      "Trim": { "interval_seconds": 600 },
      "Version": { "interval_seconds": 600 },
      "InsideTemp": { "interval_seconds": 60, "minimum_delta": 0.1 },
      "OutsideTemp": { "interval_seconds": 60, "minimum_delta": 0.1 },
      "Locked": { "interval_seconds": 5 },
      "ChargePortDoorOpen": { "interval_seconds": 10 },
      "PreconditioningEnabled": { "interval_seconds": 30 },
      "DefrostForPreconditioning": { "interval_seconds": 30 },
      "DefrostMode": { "interval_seconds": 60 },
      "FastChargerType": { "interval_seconds": 10 },
      "HvacACEnabled": { "interval_seconds": 60 },
      "HvacAutoMode": { "interval_seconds": 60 },
      "HvacLeftTemperatureRequest": { "interval_seconds": 60 },
      "HvacSteeringWheelHeatAuto": { "interval_seconds": 60 },
      "HvacSteeringWheelHeatLevel": { "interval_seconds": 60 },
      "SoftwareUpdateVersion": { "interval_seconds": 600 },
      "SoftwareUpdateDownloadPercentComplete": { "interval_seconds": 600 },
      "SoftwareUpdateExpectedDurationMinutes": { "interval_seconds": 600 },
      "SoftwareUpdateInstallationPercentComplete": { "interval_seconds": 600 },
      "SoftwareUpdateScheduledStartTime": { "interval_seconds": 600 },
      "WiperHeatEnabled": { "interval_seconds": 10 },
      "RouteTrafficMinutesDelay": { "interval_seconds": 10 },
      "MilesToArrival": { "interval_seconds": 10 },
      "MinutesToArrival": { "interval_seconds": 10 },
      "OriginLocation": { "interval_seconds": 10 },
      "DestinationLocation": { "interval_seconds": 10 },
      "DestinationName": { "interval_seconds": 10 },
      "ExpectedEnergyPercentAtTripArrival": { "interval_seconds": 10 }
    }
  }
}
```

</details>

## TeslaLogger settings: ##
You need to know the local IPs and ports of the HTTP Proxy and Telemetry server

<details>

<summary>To switch to local telemetry add this lines (with your IPs and ports) to TeslaLogger.exe.config file:</summary>

```xml
            <setting name="TeslaHttpProxyURL" serializeAs="String">
                <value>https://192.x.x.x:4443</value>
            </setting>
            <setting name="TelemetryServerURL" serializeAs="String">
                <value>tcp://192.x.x.x:5284</value>
            </setting>
            <setting name="TelemetryServerType" serializeAs="String">
                <value>ZMQ</value>
            </setting>
            <setting name="TelemetryClientID" serializeAs="String">
                <value>xxxxxxxxxx-xxxxxx-xxxxx-xxxxxxxxx</value>
            </setting>
```
</details>
