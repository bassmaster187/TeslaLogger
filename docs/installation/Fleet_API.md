# Tesla Fleet API
Tesla hat die frühere "owner-api", welche Teslalogger genutzt hat, offiziell abgeschaltet. Es ist weiterhin möglich, über unsaubere Tricks Daten von der alten owner-api zu erhalten, aber einige Fahrzeuge liefern jetzt einen Fehler, wenn man Befehle senden möchte (z. B. Wächtermodus einschalten).

```
{"response":null,"error":"Tesla Vehicle Command Protocol required, please refer to the documentation here: https://developer.tesla.com/docs/fleet-api#2023-10-09-rest-api-vehicle-commands-endpoint-deprecation-warning","error_description":""}
```

## Komponenten der Tesla Fleet API
### Zugriff durch Drittanbieter-Software und Abruf grundlegender Daten

Unterstützt von allen Tesla-Fahrzeugen.

![fleet-api-profile](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/img/fleet-api-profile.png)

![fleet-api-access](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/img/fleet-api-access.png)

### Vehicle Command Proxy
Der Vehicle Command Proxy wird von Model S/X vor Baujahr 2021 nicht unterstützt. Diese älteren Fahrzeuge verwenden weiterhin die alte Owners API für Befehle wie „Wächtermodus einschalten“. Alle anderen Fahrzeuge benötigen einen [Virtuellen Schlüssel](#virtuelle-schlüssel), den du während des Einrichtungsprozesses an dein Auto sendest.  
![fleet-api-access-in-car](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/img/fleet-api-access-in-car.jpeg)

https://github.com/teslamotors/vehicle-command

### Fleet Telemetry Server
Der Fleet Telemetry Server wird von Model S/X vor Baujahr 2021 nicht unterstützt. Diese zusätzlichen Funktionen sind nur bei neueren Fahrzeugen verfügbar und du brauchst einen [Virtuellen Schlüssel](#virtuelle-schlüssel), den du während des Einrichtungsprozesses an dein Auto sendest. Das Zugriffstoken muss mit einem Besitzer-Profil erstellt werden – ein Fahrer-Profil funktioniert nicht. Mir wurde gesagt, dass Leasingfahrzeuge derzeit nicht unterstützt werden.

https://github.com/teslamotors/fleet-telemetry

Mit dem Fleet Telemetry Server können wir mehr Daten vom Fahrzeug abrufen, z. B. Autopilot- / TACC-Status, Batteriezustände usw.

![autopilot](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/img/autopilot.jpeg)

![autopilot-stat](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/img/autopilot-stat.jpeg)

# Migration von alter API zur Fleet API
- Admin Panel
- Settings
- My Tesla Credentials
- Edit
- Tesla Fleet API (empfohlen)

# Berechtigungen für Teslalogger widerrufen
- Tesla Account öffnen
- Profile Settings
- Manage Third Party Apps
- Teslalogger / Manage
- Remove Access

https://accounts.tesla.com/de_DE/account-settings/security?tab=tpty-apps

# Virtuelle Schlüssel
Falls du vergessen hast, die virtuellen Schlüssel während der Einrichtung an dein Auto zu senden oder sie widerrufen hast, kannst du sie hier erneut senden: [LINK](https://www.tesla.com/_ak/teslalogger.de)