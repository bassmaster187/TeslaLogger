# Wallbox
You can connect your Teslalogger to your Wallbox to calculate the efficiency of charging or the percentage of photovoltaics used to charge your car. 
## Supported Wallboxes
- OpenWB
- go-eCharger
- Tesla Wallbox Gen 3
- Shelly 3EM for all unsupported wallboxes or without build in meter: https://amzn.to/3nUEuO0

# Settings
Go to admin panel and select the car you want to use for the wallbox. Go to Extras / Wallbox.
Every car has it's own wallbox settings. 

Choose your wallbox and set the host name of your wallbox e.g: http://192.168.1.174
Make sure you don't forget http:// or https:// at your host settings. Just the IP address won't work!

## Param
Some types of wallboxes needs some special params to work as expected.

# OpenWB
### Param:
LP1 - LP8 Charging point used for this car. Default: LP1

# go-eCharger
Please make sure you enable HTTP API v1 in "Internet / Enhanced Settings"
<br><img src="https://user-images.githubusercontent.com/6816385/138766186-7c5ff9c7-8225-4094-8444-f5058df24b3c.png" width="220">


# Dashboard
In Charging History you can see the efficiency of charging and percentage of photovoltaics if the wallbox supports this value.
