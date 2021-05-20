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
