# Tesla Fleet API
Tesla has officially shut down the "owner-api" Teslalogger used to get data from. It is still possible to get data from the old owner-api with some dirty tricks, but some cars are now returning an error if you want to send commands to the car (e.g turn on sentry mode).

```
{"response":null,"error":"Tesla Vehicle Command Protocol required, please refer to the documentation here: https://developer.tesla.com/docs/fleet-api#2023-10-09-rest-api-vehicle-commands-endpoint-deprecation-warning","error_description":""}
```

## Components of Tesla Fleet API
### Access of 3rd pary software and getting basic data.

Supported by all Tesla Motors cars.

![fleet-api-profile](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/img/fleet-api-profile.png)

![fleet-api-access](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/img/fleet-api-access.png)

### Vehicle Command Proxy
The Vehicle Command Proxy is not supported by Model S/X made before 2021. These old cars are using the old Owners API to send commands like "turn on sentry mode". 
All other cars need a [Virtual Key](#virtual-keys) you can send to your car during setup proccess. 
![fleet-api-access-in-car](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/img/fleet-api-access-in-car.jpeg)

https://github.com/teslamotors/vehicle-command

### Fleet Telemetry Server
The Fleet Telemetry Server is not supported by Model S/X made before 2021. This additional features are only available on newer cars and you need a [Virtual Key](#virtual-keys) you can send to your car during setup proccess. The Access Token must be created by a owner profile - a driver profile won't work. I was told leased cars are currently not supported.

https://github.com/teslamotors/fleet-telemetry

With the Fleet Telemetry Server we can get more data from our cars e.g. Autopilot / TACC state, battery states etc.

![autopilot](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/img/autopilot.jpeg)

![autopilot-stat](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/img/autopilot-stat.jpeg)

# Migrate from old API to Fleet API
- Admin Panel
- Settings
- My Tesla Credentials
- Edit
- Tesla Fleet API (recommended)

# Revoke permission for Teslalogger
- Go to your Tesla Account
- Profile Settings
- Manage Third Party Apps
- Teslalogger / Manage
- Remove Access

https://accounts.tesla.com/de_DE/account-settings/security?tab=tpty-apps

# Virtual Keys
If you forgot to send the virtual keys to your car during setup proccess or you revoked these keys, you can resend them here: [LINK](https://www.tesla.com/_ak/teslalogger.de)
