---
sidebar_position: 5
slug: /lucid-install
---
# Lucid installieren (Beta)

Der LucidLogger ist in den Teslalogger eingebettet und nutzt viele Komponenten und Funktionen mit. Derzeit ist nur eine Docker-Installation möglich; die Raspberry Images unterstützen den LucidLogger noch nicht.

Unterstützte Modelle: Lucid Air und Lucid Gravity (Gravity seit Teslalogger 1.63.14). Modell und Ausstattungsvariante werden aus der VIN gelesen.

## Installation
```
mkdir backup
mkdir mysql
mkdir invoices
chmod 777 backup
chmod 777 mysql
chmod 777 invoices
wget https://raw.githubusercontent.com/bassmaster187/TeslaLogger/refs/heads/NET8/.env -O .env
wget https://raw.githubusercontent.com/bassmaster187/TeslaLogger/refs/heads/NET8/docker-compose.yml -O docker-compose.yml
docker compose pull
```
Einige Einstellungen können in der Datei .env angepasst werden, zum Beispiel die Zeitzone (Standard ist MEZ / Berlin)
```
docker compose up -d
```

Auf einem langsamen Rechner kann die erste Initialisierung sehr lange dauern

## Auto mit dem LucidLogger verbinden
Gehe zu Admin-Panel / Einstellungen / MyTesla / MyLucid oder direkt auf:
http://localhost:8888/admin/password.php

Klicke auf «New Lucid».
![image](https://github.com/user-attachments/assets/d196699c-e0d8-439d-9561-52ab777a9896)

MyLucid-Zugangsdaten eingeben und die Region auswählen.
Klicke auf «Get Cars».

![image](https://github.com/user-attachments/assets/5a8a6c28-4640-46ef-9b91-ae58d4eec40e)

Falls mehr als ein Auto vorhanden ist, das passende auswählen und auf «Save» klicken


Wenn alles funktioniert, ist das Auto im Logfile des teslalogger-Containers zu sehen
```
06.05.2025 22:00:20 : #10[Car_10:22]: Country Code: de
06.05.2025 22:00:20 : #10[Car_10:22]: Voltage at 50% SOC:0V Date:1/1/0001 12:00:00 AM
06.05.2025 22:00:20 : #10[Car_10:22]: Car: Lucid Air PURE - 0.19 Wh/km
06.05.2025 22:00:20 : #10[Car_10:22]: VIN decoder: n/a 2023 AWD:False MIC:False battery:LFP motor:3 single MIG:False
06.05.2025 22:00:20 : #10[Car_10:22]: Vehicle Config: car_type:'LUCID' car_special_type:'' trim_badging:'PURE'
06.05.2025 22:00:20 : #10[Car_10:22]: No meter config
06.05.2025 22:00:20 : #10[StreamAPIThread_10:24]: StartStream
06.05.2025 22:00:20 : #10[.NET Long Running Task:25]: GetChargingHistoryV2Service initializing ...
06.05.2025 22:00:20 : #10[Car_10:22]: CloseDriveState EndDate: 2025-05-06 22:00:09
06.05.2025 22:00:20 : Reverse geocoding by GeocodeCache
06.05.2025 22:00:21 : #10[.NET TP Worker:15]: GetChargingHistoryV2: NotFound CarState: Start (OK: 0 - Fail: 1)
06.05.2025 22:00:21 : #10[.NET Long Running Task:25]: GetChargingHistoryV2Service initialized
06.05.2025 22:00:22 : #10[Car_10:22]: change TeslaLogger state: Start -> Sleep
```

Das Admin-Panel ist hier erreichbar: http://localhost:8888/admin/index.php

![image](https://github.com/user-attachments/assets/26e83be1-0de9-4d4f-a5bb-659a0db402e8)

Alle Grafana-Dashboards hier: http://localhost:3000/?orgId=1
Benutzername: admin
Passwort: teslalogger

Unbedingt vor der ersten Nutzung von Grafana eine Testfahrt machen und das Auto laden, sonst gibt es nur langweilige leere Dashboards.

![image](https://github.com/user-attachments/assets/111ea889-ed96-4d05-aaaa-8170b7edea7c)

Auch die regulären Teslalogger-Docs und -Infos beachten:

https://github.com/bassmaster187/TeslaLogger


## Spenden

[![Paypal Donate](https://img.shields.io/badge/Donate-PayPal-ff69b4.svg)](http://paypal.me/ChristianPogea)
