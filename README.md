# TeslaLogger

TeslaLogger is a self hosted data logger for your Tesla Model S/X/3. Actually it supports RaspberryPi 3B and 3B+.

If you want to purchase a ready to go Raspberry PI 3B+ with TeslaLogger installed follow this link:
https://www.e-mobility-driving-solutions.com/produkt/raspi-teslalogger/?lang=en

Or a Teslalogger Image for a Raspberry PI 3B+: 
https://e-mobility-driving-solutions.com/produkt/teslalogger-image/?lang=en

You can also run it in a Docker:
[Docker Setup](docker_setup.md)

# Configuration
Connect your Raspberry PI with your router with a network cable and turn in on.
Within 2-3 minutes the Raspberry should show up in you network.

## Enter your Tesla crendentials
Use your browser to go to:

http://raspberry/admin/password.php

Enter the same credentials as you use in your teslaaccount or app.

## Settings & Language
Available languages: English, German, Norwegian - Translations are welcome

Change the language and units and reboot the Teslalogger.

http://raspberry/admin/settings.php

## Admin Panel
http://raspberry/admin/

## Grafana-Dashboard
http://raspberry:3000

Username: admin

Password: teslalogger

## Dashboard
http://raspberry/admin/dashboard.php

Customizing the Dashboard goes here: [LINK](dashboard.md)

## Fleet Statistics
Fleet Statistics can be used by anyone without Teslalogger. To compare your degradation and charging curves with the fleet, you need a Teslalogger.

### Degradation Statistics
https://teslalogger.de/degradation.php

### Charging Speed Statistics
https://teslalogger.de/charger.php

### Firmware Tracker
https://teslalogger.de/firmware.php

### Map of fast chargings by Teslalogger Users
http://teslalogger.de/map.php

## SSH for advanced users

Username: pi

Password: teslalogger

## More than one car

Go to \\RASPBERRY\teslalogger with your file Explorer. There is a File: TeslaLogger.exe.config
```xml
<setting name="Car" serializeAs="String">

<value>0</value>

</setting>
```
value 0 is 1st car / value 1 is 2nd car and so on...

## Custom Points of Interest (POI)

Details how to add / manage your own Points of Interest (POI) are [described here](TeslaLogger/Geofence.md)

# German manual
http://teslalogger.de/handbuch.php

Translations are welcome :-)
Please contact us beforehand to allow a coordinated approach for translations.

# TeslaFi Import
You can import your TeslaFi data [here](TeslaFi-Import/README.md).

# Donations:
http://paypal.me/ChristianPogea

You can also use my referral code to buy a Tesla:
http://ts.la/christian7267

# Screenshots
 [Dashboard](dashboard.md)
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/Dashboard.PNG)

Grafana Dashboards: http://raspberry:3000
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/verbrauch_en.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/trip_en.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/laden_en.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/ladehistorie_en.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/ladestatistik_en.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/akkutrips_en.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/degradation_en.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/SOCladestatistik_en.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/vampirdrain_en.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/vampirdrain_month_en.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/visited.PNG)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/Trip-Monatsstatistik.PNG)

# Screenshots with ScanMyTesla integration #

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/Zellspannungen_ScanMyTesla.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/HVAC-ScanMyTesla.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/verbrauch-ScanMyTesla.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/performance-ScanMyTesla.png)

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/Zelltemperaturen.PNG)

# Your Car vs Fleet #
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/MyDegradationVsFleet.PNG)
