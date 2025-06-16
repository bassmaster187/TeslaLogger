# Lucid install (Beta)

LucidLogger is enbedded into TeslaLogger and reuse many components and features. Right now only a docker installation is supported. Raspberry Images doesn't support the LucidLogger right now.

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
You can adjust some settings in your .env file like Timezone (default is MEZ / Berlin)
```
docker compose up -d
```

If you have a slow machine, the first init can take very long

## Connect your car to the LucidLogger
Go to admin panel / settings / MyLucid credentials or directly to:
http://localhost:8888/admin/password.php

Click on "New Lucid".
![image](https://github.com/user-attachments/assets/d196699c-e0d8-439d-9561-52ab777a9896)

Enter your MyLucid credentials and select your region. 
Click on Get Cars.

![image](https://github.com/user-attachments/assets/5a8a6c28-4640-46ef-9b91-ae58d4eec40e)

Select your car if you have more than one and click "Save"


If everything works, you should see the car in the teslalogger container logfile
```
06.05.2025 22:00:20 : #10[Car_10:22]: Country Code: de
06.05.2025 22:00:20 : #10[Car_10:22]: Voltage at 50% SOC:0V Date:1/1/0001 12:00:00 AM
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

You can access the Admin Panel at: http://localhost:8888/admin/index.php

![image](https://github.com/user-attachments/assets/26e83be1-0de9-4d4f-a5bb-659a0db402e8)

All the Grafana Dashboards at: http://localhost:3000/?orgId=1
Username: admin
Password: teslalogger

Make sure to make a test drive / charging before using Grafana otherwise you will see just boring empty dashboards.

![image](https://github.com/user-attachments/assets/111ea889-ed96-4d05-aaaa-8170b7edea7c)

Also check the regular Teslalogger docs / infos:

https://github.com/bassmaster187/TeslaLogger


## Donations

[![Paypal Donate](https://img.shields.io/badge/Donate-PayPal-ff69b4.svg)](http://paypal.me/ChristianPogea)

