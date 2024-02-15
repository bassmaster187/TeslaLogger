# Which permissions are required and why?
## Vehicle Information
This permission is mandatory for Teslalogger. It is used for get vehicle's location, SOC, charging speed etc...
Teslalogger will not work at all without this authorization.

## Vehicle Commands
This permission is optional. If you want to send commands with [geofence](https://github.com/bassmaster187/TeslaLogger/blob/master/TeslaLogger/Geofence.md) feature like turn on sentry mode on a specific address. You need this feature. 
If you want to send commands to your car by Teslalogger's built in [REST API](https://github.com/bassmaster187/TeslaLogger/blob/master/TeslaLogger/WebServer.md) or Smart Home / MQTT.

## Profile Information
This permission is currently not used at all. But there are feature request that may be using this feature. For instance sending an e-mail if we detect your Teslalogger stops working for any reason. 

## Vehicle Charging Management
This permission is optional. Teslalogger is able to get the charging bills from your account and store the amount of money you paid at a supercharger and also calculate the savings if you have free supercharging.

## Energy Product Information
Not currently used, but we could calculate the amount of solar you used for charing at home. 

## Energy Product Commands
Not currently used

![Permissions-en](https://github.com/bassmaster187/TeslaLogger/assets/6816385/cb650056-fc76-4433-af17-cae896070bfb)
