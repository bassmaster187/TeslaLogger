# Custom Points of Interest (POI)

TeslaLogger has capabilities to manage POIs.

You can use it to
* give names to places. Those names will show up in all Grafana Dashboards, eg. Trip
* trigger actions when you arrive / depart, see special flags below

All POIs are stored in a comma separated values file (CSV): geofence-private.csv

# add POIs from Grafana

TODO

# see all POIs on map

TODO

## file format

geofence-private.csv contains data records. One line is one record. Records consist of fields:
1. Name. Mandatory. Must not contain ,
2. Latitude. Mandatory. Floating Point number
3. Longitude. Mandatory. Floating Point number
4. Radius. Optional. Integer. Value in meters. Default: 50
5. special flags. Optional. List of special flags and parameters, each flag starts with + Default: nothing

Example

Supercharger DE-Blankenfelde-Mahlow,52.308921402353334,13.444712162017824,20,+ocp:R->P+hfl:5m
* Name: Supercharger DE-Blankenfelde-Mahlow
* Latitude: 52.308921402353334
* Longitude: 13.444712162017824
* Radius: 20
* special flags: +ocp:R->P+hfl:5m

## special flags for POIs

More than one special flag for each POI is possible. Special flags are not separated by comma, each flag starts with a +. Some flags have configuration parameters after a :

Currently there is no management interface for the browser, you have to edit the geofence-private.csv file.

### Open Charge Port

+ocp

This flag is executed when the shift state of your car changes, eg. when shifting from D to R.

Configuration parameters: from->to

One or more shift states P N R D are valid for from and to.

Example: R->P when shifting from R to P the command Open charge Port is sent to your car

Default: RND->P

### High Frequency Logging

+hfl

This flag is executed during charging.

Configuration parameters: duration

or

Configuration parameters: count

Example: 5m (log as fast as possible for 5 minutes)

Valid configuration parameters: 

count: integer

duration: integer + one character (s: seconds, m: minutes, h: hours, d: days)