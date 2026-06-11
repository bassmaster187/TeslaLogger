---
sidebar_position: 2
---
# Backup

This allows you to manually create a backup file of the database. Basically, this also happens automatically once a day or before an update and is therefore only useful before changes. Once the backup file has been created, "ok" is displayed.
The backup file can be retrieved via Windows Explorer by entering "\\raspberry\teslalogger\backup" in the address bar at the top.
We recommend occasionally copying the current file there to another location, for example on your personal PC, and deleting old files there to save space. As more and more data accumulates over time, the backup will also get larger!

Not only the database is backed up, but also personal "Geofence" data and the log files of the last months.
We do not recommend making changes to the file "geofence.csv", as this is managed by the project and can be overwritten during updates. Therefore, it is also not necessary to back up or restore this file. The file "geofence-private.csv" does not need to be edited manually; there is a dialog "Geofence" for this.
