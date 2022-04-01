# Version 1.52.3
- Support for Support for KEBA KeContact P30 (P20?) wallbox

# Version 1.52.2
- [OVMSLogger](https://github.com/bassmaster187/OVMSLogger) support by Teslalogger. If you have a car supported by https://www.openvehicles.com/ (e.g. Tesla Roadster), you can start logging it. 

# Version 1.52.1
- Bugfixes

# Version 1.52.0
- Tesla changed the authentification proccess. 

# Version 1.51.10
- Link to release notes of firmware in admin panel and firmware dashboard
- Show available firmware updates on admin panel

# Version 1.51.9
- Using Newtonsoft JSON instead of buggy build in Mono JSON Parser.

# Version 1.51.6
- Bugfixes 
- New MySql Client [Link](https://github.com/advisories/GHSA-77rm-9x9h-xj3g)
- Restart car thread after 5 x 404 errors within 10 Minutes 

# Version 1.51.5
- Using [Excptionless](https://exceptionless.com/) to track exceptions.

# Version 1.51.4
- Easier auth proccess.

# Version 1.51.3
- You can use journeys to combine a long trip / time and summarize all numbers. This is very useful to track the complete charge time of a long trip or to compare the consumption of summer or winter tires. This feature can be find in: Admin Panel / Extras / Journeys 

# Version 1.51.2
- Bugfix: Restart whole thread after access_token has been updated

# Version 1.49.3
- Grafana 8.3.2 @Docker Users: Make sure you updated your docker-compose.yml and datasource.yaml [LINK](https://github.com/bassmaster187/TeslaLogger/blob/master/docker_setup.md)
- Trip Dashboard using the new table

# Version 1.49.1
- French Language
- Support for Shelly 3EM

# Version 1.49.0
- Bugfixes and Release

# Version 1.48.17
- Support for wallboxes [LINK](https://github.com/bassmaster187/TeslaLogger/blob/master/wallbox.md)

# Version 1.48.15
- turn off name & passwort authentification to Tesla Server because it won't work anymore
- calculate ampere if tesla server returns 0
- improve calculation of charge energy added for combined/interrupted charging session
- remove database password from logs
- detect more car models

# Version 1.48.14
- BF: prevent endless loop in reCaptcha service

# Version 1.48.13
- Tesla Wallbox Gen 3 first support

# Version 1.48.12
- Using 2Captcha for resolving ReCaptchas

# Version 1.48.11
- authentification with access token & refresh token as a fallback if email & password doesn't work

# Version 1.48.10
- BF: new authentification
- combine charging is disabled per default, can be enabled for Geofences, but will fail if the car stays plugged in all the time
- VIN decoder now decodes more vehicles

# Version 1.48.9
- Skip inactive cars from authentification to server
- Don't wait (forever) for user input. Give other Cars a chance to authentificate meanwile
- List cars in account if car id not found
- Wrong captcha has been showed for car id > 1

# Version 1.48.8
- Delete password from database after successful login.
- Show if your account is locked during authentification process
- Use Captach if asked by Tesla mothership

# Version 1.48.7
- New source of SRTM files as dds.cr.usgs.gov doesn't provide them anymore [Bug #596](https://github.com/bassmaster187/TeslaLogger/issues/596)
- Offline is now also considered as sleeping in Dashboards: Vampir Drain, Vampir Dran Monthly Statistics and Timeline Plugin

# Version 1.48.6
- Detecting Model Y SR+

# Version 1.48.5
- Tesla turned off captcha, so we did it as well

# Version 1.48.4
- Supporting the new Tesla authentification proccess with captcha

# Version 1.48.3
- UI for geofence / "on charge complete" [LINK](https://github.com/bassmaster187/TeslaLogger/blob/master/TeslaLogger/Geofence.md#set-new-charge-limit-when-charging-is-complete)
- UI for geofence / Don't combine charging sessions [LINK](https://github.com/bassmaster187/TeslaLogger/blob/master/TeslaLogger/Geofence.md#do-not-combine-chargin-sessions)
- UI for StreamingPos in settings. Enables position DB entries from the streaming API - depending on car's reception this means up to 5 positions per second (compared to one position every five seconds on normal mode), this will increase your database's size a lot, will increase wear on the SD card and result in huge backups

# Version 1.48.2
- BF: Authentification to Tesla's server not possible

# Version 1.48.1
- Admin Panel can now be proteced by password. Check menu/settings/Teslalogger Adminpanel credentials

# Version 1.48.0
- Stable, well tested major release. Contains all features and bug fixes since 1.48.0

# Version 1.47.8
- BF: Abetterrouteplanner wrong calibrated consumption

# Version 1.47.7
- Abetterrouteplanner integration. Check menu/extras/abetterrouteplanner in admin panel.
- Textfilter and action filter for Timeline Plugin Dashboard
- Dashboard Timeline Plugin using OpenStreetMap for map drawing
-  Timeline Plugin Dashboard:
   - Vampir drain
   - max / avg speed
   - max / avg cahrging kw
   - avg consumption
   - distance
   - Multilingual / using units from settings page
   - links to geofence and set carging costs
   - Text and action filter  
- Model 3 LR P 2021 detection
- dectect wating for MFA code and automatic redirecting to MFA entering page
- redirect to password.php if cars table is empty
- backup of geofence-private.csv
- New Language: chinese
- merge interrupted chargings

 BF: 
- Authentification in USA & China

# Version 1.47.0
- Danish translation
- Bugfixes
- Timeline Plugin Dashboard supports light theme

# Version 1.46.5
- New Tesla authentification 
- chinese authentification server support

# Version 1.46.2
- Using the streaming API to wakeup Teslalogger, so no tricks with tasker are necessary anymore
- Timeline Dashboard & Timeline Panel Dashboard [Screenshot](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/timeline.png)

If you want to display the small maps in Timeline Panel Dashboard, you have to optain a free MapQuest Key:
Go to: https://developer.mapquest.com/
Click on: "Get your free API Key".

Enter the key in your TeslaLogger.exe.config:
```xml
<setting name="MapQuestKey" serializeAs="String">
		<value>xxxxxxxxxxxxxxxxxxxxxxxxxx</value>
</setting>
```
Restart Teslalogger

# Version 1.46.0
- New Tesla Authentification
- Detection of: Model Y LR AWD, Model Y, Model 3 SR+ LFP (MIC)
- Teslamate Import
- UseOpenTopoData as fallback
- New Languages: Portugues, Italian, Russian
- Nearby Supercharger Service & Dashboards
- Housekeeping of old backup files

# Version 1.44.0
- Charging costs in dashboard charging history / statistics
- Settings for ideal & rated range 
- Filter for wrong data from ScanMyTesla
- Speed Consumption Dashboard
- Share button for DC chargings in charging history if you share your data anonymously 
- Firmware update dashboard
- Retry to update 15 times if github fail
- Model S Raven LR Performance supported
- Energy buffer in degradation dashboard if you use ScanMyTesla
- Special Flags doc
- Bugfixes & Improvements

# Version 1.42.1
- New Admin Panel Design
- Date & time in Trip Start Admin Panel
- Automatic Updates (check settings panel. Default: all updates)
- Special Flags in geofence (Open Charge Port, High Frequency Logging)

# Version 1.40.6
- ShareData anonymously if you want
- Optimize Performance -> lower CPU on idle
- Mothership Communication Dashboard
- Consumption Statistics Dashboard
- Supporting Japanese charset in database
- Duch language
- Model 3 LR RWD detection

# Version 1.39.0
- Better Tesla Model detection
- Many supercharger & fast charger updates.
- Better geofence detection, if two or more POIs are close to each other.
- ShareData Beta Programm
- Synology Docker support
- Untranslated English sentences fixed
- Admin-Panel shows miles and °F if chosen in settings
- ScanMyTesla wakeup teslalogger if detects speed > 5kph
- ScanMyTEsla signals in admin-page
- TeslaFi import : [Readme](https://github.com/bassmaster187/TeslaLogger/blob/master/TeslaFi-Import/README.md)
- Battery Heater in Grafana dashboard charging 
- Webapp for full screen dashboard

# Version V1.37.5.0
- A nice dashboard for the livingroom [Readme](https://github.com/bassmaster187/TeslaLogger/blob/master/dashboard.md)
- Restore database in Settings
- inside temperature during preconditioning in admin panel and dashboard
- Grafana Status Dashboard
- Chargertype (AC / DC / Chademo / Tesla)  in Charging Statistics
- Battery inside temperature, heater, preconditioning and sentry mode in Grafana Dashboard Consumption
- Radius in geofence
- New Panels in chargingstatistics
- ScanMyTesla integration with a bunch of new Grafana Dashboards
https://www.impala64.de/blog/tesla/2019/12/27/teslalogger-scanmytesla-integration-english/
https://github.com/bassmaster187/TeslaLogger/#screenshots-with-scanmytesla-integration
- Grafana Dashboard monthly trip statistics

# Version 1.34.10
- Map in Admin Panel
- Responsive design
- Docker is now able to update the admin panel
- MQTT additional signals

# Version 1.34.2
- Elevation trough SRTM database
- update to Grafana 6.3.5
- update Trackmap Panel

# Version 1.31.1

- english and german is now supported.
