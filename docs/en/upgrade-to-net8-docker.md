# Upgrade from Raspberry Image or legacy Docker to new .net8 Docker

- Backup your database and geofence file. Your backup is located on your host system at
> TeslaLogger\TeslaLogger\bin\backup
- Move the files to a safe place!!! (e.g. your desktop)
- Stop old Docker
> docker compose stop
- Install new .net8 Docker image: https://github.com/bassmaster187/TeslaLogger/blob/master/docker_setup.md
- go to Admin Panlel / Extras / Restore
- Restore your Database. Make sure to use the latest backup. The name has a pattern: year-month-day-hour-minute ...
- Resotre your geofence file
- Because your tesla auth token are encrypted, the new Teslalogger can't connect to Tesla API.
- Go to Admin Panel / Settings / My Tesla Credentials / Edit (every car) / reconnect to your cars (don't delete them!)
- Restart Teslalogger and make sure all cars can connect to Tesla API / Fleet Telemetry Server
- If everything works fine, make sure you remove your old Docker. Running both docker at the same time, tesla may block you and the Fleet Telemetry Server will disconnect you all the time!
- Remove old Docker:
> docker compose down
