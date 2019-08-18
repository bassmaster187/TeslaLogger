# TeslaLogger

TeslaLogger is a self hosted data logger for your Tesla Model S/X/3. Actually it supports RaspberryPi 3B and 3B+.

You can download the Raspberry image here: https://teslalogger.de/teslalogger20190812-public.zip
I strongly recommand you to use an official Raspberry kit as some sd-cards and power supply will run in some serious problems.
Amazon: https://amzn.to/2GhIcPu

If you want to purchase a ready to go Raspberry PI 3B+ with TeslaLogger installed, send me an e-email: mail@pogea.de
120€ shipped to Europa / $150 shipped to USA.

# Configuration
Connect your Raspberry PI with your router with a network cable and turn in on.
Within 2-3 minutes the Raspberry should show up in you network.

## Enter your Tesla crendentials
Use your browser to go to:

http://raspberry/admin/password.php 

Enter the same credentials as you use in your teslaaccount or app.

## Settings & Language
Change the language and units and reboot the Teslalogger.

http://raspberry/admin/settings.php

## Admin Panel
http://raspberry/admin/

## Dashboard
http://raspberry:3000

Username: admin

Password: teslalogger

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



# German manual
http://teslalogger.de/handbuch.php

Translations are welcome :-)
Please contact us beforehand to allow a coordinated approach for translations.

# Donations:
http://paypal.me/ChristianPogea

You can also use my referral code to buy a Tesla: 
http://ts.la/christian7267

# Screenshots
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

