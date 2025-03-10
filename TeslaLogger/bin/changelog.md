# Version 1.63.3
- Bugfixes

# Version 1.63.2
- faster logging (positions every 10 seconds)
- Komoot integration (admin panel / extras / Komoot Settings)
- New Wallbox: Openwb 2
- Local Fleet Telemetry support with ZMQ

# Version 1.63.0
- Stable Version

# Version 1.62.14
- New Wallboxes: SmartEVSE 3 and WARP
- EVCC supports multiple loadpoints and cars now. You can find a loadpoint by car name (EVCC should be configured correctly)

# Version 1.62.13
- Tesla has introduced more granular control over data access: [LINK](https://developer.tesla.com/docs/fleet-api/announcements#2024-11-26-introducing-a-new-oauth-scope-vehicle-location) 
  Third-party apps can request permission to access location information. Starting in March 2025, no location information will be shared unless you grant the necessary permission.

# Version 1.62.12
- Fleet API: new signals: ExpectedEnergyPercentAtTripArrival and ExpectedEnergyPercentAtTripEnd

# Version 1.62.5
- Detect Model Y LR RWD
- BF: Split drives
- BF: inactive cars
- BF: faster startup (inactive cars will be checked after active cars)

# Version 1.62.4
- Better support for new Tesla Model S/X 2021 

# Version 1.62.3
- Fleet API: Doors, Windows, Trunk, Frunk and Locked status will be send to Admin Panel and MQTT

# Version 1.62.1
- BF: Support for new Tesla Model S/X 2021 - Tesla identified the new Model S/X as pre face lift Model S/X.

# Version 1.62.0
- Update = none is no longer supported. Tesla may force me to update a version so "none" is now "stable"

# Version 1.61.0
- Simplified the switch to Fleet API. 
- Infos if you need to switch to Fleet API in the admin panel. 
- Info if you need a subscription in the admin panel.
- Info screen about Fleet API and subscription won't be shown anymore if you have a subscription and Fleet API is enabled.
- Direct link to subscription in the My Tesla Credentials settings if you need to switch to Fleet API.
- Don't show the subscription info if you are using the old Tesla API. (old Model S/X)

# Version 1.60.6
- Fleet API: TPMS, VehicleName, Trim, CarType, Version now available

# Version 1.60.3
- don't use data commands at all for fleet api cars

# Version 1.60.2
- don't use nearby_charging_sites anymore in fleet api because it is a paid feature

# Version 1.60.1
- You can rename your cars in the admin panel / settings / my Tesla Credentials. Useful if Tesla API has overwritten an empty name or for old cars without access anymore. The name will be used in the Grafana dashboards and in the MQTT topics.

# Version 1.60.0
- Stable version

# Version 1.59.12
- Daily info on admin panel about the need to migrate to Fleet API and the subscription model
- Bugfixes for charging state detection with Fleet API / more signals

# Version 1.59.11
- From the beginning of 2025, Tesla will charge money for the use of FleetAPI and will probably switch off the unofficial “Owners API”. Therefore I am forced to offer a monthly subscription for Teslalogger. Please switch your Teslas to FleetAPI and take out the subscription model. We do not yet know what will happen with Model S/X before 2021. For this reason, please do not switch the old Model S/X to Fleet API. 
- Subscription link in Settings/MyTesla
- Show destination and eta in Admin Panel with FleetAPI

# Version 1.59.9
- Use as less commands as possible with FleetAPI

# Version 1.59.8
- Show signal counter for Fleet API cars in Settings / My Tesla Credentials

# Version 1.59.7
- Dashboard Trips Monthly Statistics now also shows anual statistics
- Support for Model 3 Highland

# Version 1.59.6
- BF: missing trip because of Duplicate entry 'xxx' for key 'ix_startpos'
- XSS vulnerability in some php files. Thanks to Mohammed Shine 

# Version 1.59.5
- BF: Fleet API detecting DC charging on newer Tesla firmware
 
# Version 1.59.4
- BF: Fleet API detecting AC charging on newer Tesla firmware

# Version 1.59.3
- Support for new FW 2024.26 with Fleet API

# Version 1.59.0
- Tesla API has been changed!

# Version 1.58.2
- Many improvements for Fleet API

# Version 1.58.2
- Many changes to Fleet API and Owners API due to command limit by tesla (Owners API: 600/day, Fleet API: 300/day) [LINK](https://github.com/bassmaster187/TeslaLogger/issues/1304)
- Dashboard Mothership filter by car

# Version 1.58.1
- Using Telemetry Server even with old Tesla API. Require Virtual Key
- New update client
- MQTT: active cars only
- Calculated Power/Current/Phases in Admin-Panel and MQTT

# Version 1.58.0
- New stable version
- Encryption file is protected against grafana, apache and mariadb 

# Version 1.57.14
- Tesla access token and refresh token are stored encryted

# Version 1.57.13
- Use Guest NearbySuCService to complete Supercharger usage map

# Version 1.57.11
- Rollback code to 1.57.9

# Version 1.57.10
- Show virtual key and access type in settings / cars

# Version 1.57.9
- New Wallbox supported: cFos
- New Wallbox supported: EVCC

# Version 1.57.8
- Dashboard consumption shows the usage of Autopilot / TACC in different colors (Fleet Telemetry). [Screenshot](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/Autopilot.PNG)

# Version 1.57.6
- Dashboard Trip: percent of drive with Autopilot / TACC and the longes section with Autopilot / TACC (Fleet Telemetry) [Screenshot](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/AP-TACC-Percent.JPG)
- We are using now Hosted Weblate for translations. Feel free to contribute your translation: [Weblate](https://hosted.weblate.org/engage/teslalogger/)

# Version 1.57.5
- Using [official](https://github.com/teslamotors/fleet-telemetry) Tesla Fleet Telemetry Server if your Car is connectet with Tesla-Fleet API (not used by pre 2021 Model S/X)
- Grafana Dashboard Vehicle Alerts. (depends on Fleet Telemetry - not supported by pre 2021 Model S/X)
- Cell temperature in charging dashboard (depends on Fleet Telemetry)

# Version 1.57.3
- Completely new MQTT client with MQTT AutoDiscovery and control possibilities. Old MQTT client will still work until new MQTT client is activated, but is not supported any more. Setup: go to Admin Panel->Extras->MQTT Settings

# Version 1.57.1
- Detect Y SR MIC / MIG / BYD / CATL / SR+

# Version 1.57.0
- Teslalogger is now supporting the [official](https://developer.tesla.com/docs/fleet-api#overview) Tesla-Fleet API. Especially cars bought after December 2023 should use it! You can migrate your car to use the new API if you go to Settings / MyTesla / Edit / Tesla Fleet API -> go through login process 
- New Grafana dashboard: Trip Top Destinations
- BF: Cars located in China are now able to use Teslalogger again
- Geofence: Grouping of all public chargers to impove performance

# Version 1.56.1
- Update certificates for Mono

# Version 1.56.0
- Support new Tesla API
- Attention: MapQuest isn't free anymore! Either you remove the key in your settings, if you used it or you have to provide a credit card to MapQuest. 
- Dayli backup in docker
- Support for V4 Supercharger in statistics

# Version 1.55.0
- Supporting new API change after Tesla firmware 2023.38.4

# Version 1.54.26
- Vehicle state icons in admin panel. e.g. open window, unlocked car, open doors, open frunk / trunk
- Destination route will be displayed in admin panel with ETA and SOC at destination.

# Version 1.54.25
- Bugfix in upgrade zu Debian Buster

# Version 1.54.24
- Update Grafana to 10.0.1 Note: Grafana 10.0.1 is not compatible with old Raspberry PI3 OS. You have to update it [manually](https://github.com/bassmaster187/TeslaLogger/blob/master/docs/en/os_upgrade.md) 

# Version 1.54.22
- Restore chargingstate from backup [Docs](https://github.com/bassmaster187/TeslaLogger/blob/master/docs/en/faq.md#grafana-dashboard-charging-history--ladehistorie-has-wrong-entries-for-total-costs)

# Version 1.54.20
- reduce Tesla API calls to minimum

# Version 1.54.19
- New Tesla API -> older Teslalogger versions won't work anymore!

# Version 1.54.16
- Update to Grafana 8.5.22 [security fix](https://grafana.com/blog/2023/03/22/grafana-security-release-new-versions-with-security-fixes-for-cve-2023-1410/)
- New CO2 Dashboard
- Dashboard Visited multiple vehicles can be shown at the same time

# Version 1.54.15
- 3rd party apps for Tesla tokens are not longer needed. 

# Version 1.54.14
- Simplify docker install

# Version 1.54.12
- CO2 and cost column in journeys.
- Links to Grafana dashboards in journeys
- Support for Shelly EM [Docs](https://github.com/bassmaster187/TeslaLogger/blob/master/wallbox.md)
- Support more countries for CO2 calculations
- Dashboard Charging Statistics contains CO2

# Version 1.54.11
- Calculation of CO2 for each charging. [Docs](https://github.com/bassmaster187/TeslaLogger/blob/master/docs/en/co2.md)
- Dashboard Charging History with CO2 

# Version 1.54.8
- Wheeltype will be stored in trips and chargings. 

# Version 1.54.6
- Detecting Model S Plaid / LR

# Version 1.54.5
- TMPS in Trip Dashboard. Signals are colleced after update!

# Version 1.54.4
- Using new GraphQL nearby SUC service. https://teslalogger.de/suc-map.php shows now max kW per site
- BF: MapQuest reverse geocoding and static map creation
- new vehicles in account are detected automatically and used in teslalogger
- reduce calls to Tesla API to prevent ban with many cars in account

# Version 1.54.3
- Detecting Model X 2021 Plaid / LR

# Version 1.54.2
- New statistics: https://teslalogger.de/consumption.php
- [SuperChargeBingo](https://supercharge.bingo/?pk_campaign=integration&pk_kwd=teslalogger) integration

# Version 1.54.1
- Share consumption data
- detection of Model Y MIG / US / MIC

# Version MQTT 2.2.0.0
- Non-standard MQTT brocker port can be defined
- User specific ClientID can be defined
- Subtopics can be activated

More information: https://github.com/bassmaster187/TeslaLogger/blob/master/MQTTClient/readme.md

# Version 1.53.0
- Get rid of duplicate trips

# Version 1.52.4
- Slower getting data from ScanMyTesla 

# Version 1.52.3
- Support for Support for KEBA KeContact P30 (P20?) wallbox

# Version 1.52.2
- [OVMSLogger](https://github.com/bassmaster187/OVMSLogger) support by Teslalogger. If you have a car supported by https://www.openvehicles.com/ (e.g. Tesla Roadster), you can start logging it. 

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
