---
sidebar_position: 7
---
# Upgrade vom Raspberry-Image oder Legacy-Docker zum neuen .NET 8 Docker

- Datenbank und Geofence-Datei sichern. Dein Backup liegt auf dem Host-System unter
> TeslaLogger\TeslaLogger\bin\backup
- Dateien an einen sicheren Ort verschieben (z. B. Desktop)
- Alten Docker stoppen
> docker compose stop
- Neues .NET 8 Docker-Image installieren: https://github.com/bassmaster187/TeslaLogger/blob/master/docker_setup.md
- Admin Panel / Extras / Restore öffnen
- Datenbank wiederherstellen. Sicherstellen, dass das neueste Backup genutzt wird. Namensmuster: Jahr-Monat-Tag-Stunde-Minute ...
- Geofence-Datei wiederherstellen
- Da die Tesla Auth Tokens verschlüsselt sind, kann der neue Teslalogger nicht zur Tesla API verbinden.
- Admin Panel / Settings / My Tesla Credentials / Edit (jedes Fahrzeug) / erneut verbinden (nicht löschen)
- Teslalogger neu starten und sicherstellen, dass alle Fahrzeuge zur Tesla API / Fleet Telemetry Server verbinden
- Wenn alles funktioniert, alten Docker entfernen. Läuft beides parallel, kann Tesla dich blockieren und der Fleet Telemetry Server trennt ständig.
- Alten Docker entfernen:
> docker compose down
