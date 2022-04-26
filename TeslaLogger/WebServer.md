# TeslaLogger internal web server

Ports

Docker: 5010

Raspberry: 5000

## endpoints for admin UI

### /getchargingstate

TODO

### /setcost

TODO

### /getallcars

request: GET /getallcars

response: JSON

Gives main car data, like id, display name, model name, tastker token, VIN...

### /setpassword

TODO

### /export/trip

request: GET /export/trip?from=...&to=...&carID=

response: GPX file

from=... and to=... are pos.id's

### /admin/UpdateElevation

TODO

request: GET /admin/UpdateElevation

response: plain text

Updates elevation data for all empty positions in the database

### /admin/ReloadGeofence

request: GET /admin/ReloadGeofence[?html]

response: JSON or HTML

Reloads geofence.csv and geofence-private.csv

If optional query parameter html is present, the response will be a table containing all POIs.

### /admin/GetPOI?lat=_latitude_&lng=_longitude_

request: GET /admin/GetPOI?lat=_latitude_&lng=_longitude_

response: JSON

Gives geofence.csv and geofence-private.csv info as JSON, eg to find out if a position is tagged with +home.

## get car values

### /get/_CarID_/_name_

request: GET /get/_CarID_/_name_[?raw]

response: JSON or plain text

Get the latest value for property _name_ from car _CarID_.

Example: /get/1/car_version?raw --> 2020.32.3 b9bd4364fd17

## send commands to car

### /command/_CarID_/_name_

request: GET /command/_CarID_/_name_

response: JSON (forwarded from Tesla API)

Allowed commands:
* auto_conditioning_start
* auto_conditioning_stop
* auto_conditioning_toggle
* sentry_mode_on
* sentry_mode_off
* sentry_mode_toggle
* wake_up
* set_charge_limit
* charge_start
* charge_stop
* set_charging_amps

### /command/_CarID_/_name_?_parameter_

request: GET /command/_CarID_/_name_?_parameter_

response: JSON (forwarded from Tesla API)

Allowed commands:
* set_charge_limit
* set_charging_amps

Example: /command/1/set_charging_amps?16

## debugging TeslaLogger

### /debug/TeslaAPI/_CarID_/_name_

request: GET /debug/TeslaAPI/_CarID_/_name_

response: JSON

Gets the latest Tesla API repsonse for endpoint name for car with CarID

Example: /debug/TeslaAPI/1/drive_state

### /debug/TeslaLogger/states

request: GET /debug/TeslaLogger/states

response: HTML

Outputs lots of TeslaLogger internal state as HTML table.

### /debug/TeslaLogger/messages

request: GET /debug/TeslaLogger/messages

response: HTML

Outputs the last 500 lines of DEBUG log

### /get/map

request: GET /get/map?[urlparams]

response: image/png

mandatory urlparams
* start=*int* pos.id
* end=*int* pos.id

optional urlparams
* width=*int* in pixels; if omitted defaults to defaults to 240
* height=*int* in pixels; if omitted defaults to defaults to width / 1.618033 (golden ratio)
* mode=*string*, possible values: dark; if omitted defaults to regular
* type=*string*, possible values: park, charge; if omitted defaults to trip
