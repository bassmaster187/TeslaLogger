---
sidebar_position: 7
---
# Upgrade from Raspberry Image or Legacy Docker to the new .NET 8 Docker

- Backup your database and geofence file. Your backup is located on your host system at
> TeslaLogger\TeslaLogger\bin\backup
- Move the files to a safe place!!! (e.g. your desktop)
- Stop old Docker
> docker compose stop
- Install new .NET 8 Docker image: https://github.com/bassmaster187/TeslaLogger/blob/master/docker_setup.md
- Go to Admin Panel / Extras / Restore
- Restore your database. Make sure to use the latest backup. The name has a pattern: year-month-day-hour-minute ...
- Restore geofence file
- Since the Tesla auth tokens are encrypted, the new TeslaLogger cannot connect to the Tesla API.
- Admin Panel / Settings / My Tesla Credentials / Edit (each vehicle) / reconnect (do not delete)
- Restart TeslaLogger and ensure that all vehicles connect to the Tesla API / Fleet Telemetry Server
- If everything works, remove the old Docker. If both run in parallel, Tesla can block you and the Fleet Telemetry Server disconnects constantly.
- Remove old Docker:
> docker compose down
