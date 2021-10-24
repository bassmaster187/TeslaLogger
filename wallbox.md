# Wallbox
You can connect your Teslalogger to your Wallbox to calculate the efficiency of charging or the percentage of photovoltaics used to charge your car. 
## Supported Wallboxes
- OpenWB
- go-eCharger
- Tesla Wallbox Gen 3
# Settings
Go to admin panel / Extras / Wallbox

Choose your wallbox and set the host name of your wallbox e.g: http://192.168.1.174
Make sure you don't forget http:// or https:// at your host settings. Just the IP address won't work!

## Param
Some types of wallboxes needs some special params to work as expected.
#### OpenWB
LP1 - LP8 Charging point. Default: LP1

# Dashboard
In Charging History you can see the efficiency of charging and percentage of photovoltaics if the wallbox supports this value.
