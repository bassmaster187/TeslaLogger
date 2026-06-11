# Data Backup

Once a day, TeslaLogger performs various data backup operations.
The following files are created daily:
- mysqldump\<date\>.gz
- geofence-private\<date\>.gz

The following file is created once a month:
- logfile-\<date\>.gz

The following files are created once a year:
- yeardump-\<year\>.gz
- yeargeofence-\<year\>.gz

From February 2026, the daily files will be prefixed with "DAY-" and will be deleted after 31 days to prevent the storage from filling up. So that older data is still available, the prefix "MON-" is used instead of "DAY-" on the 1st of each month. The MON files will be deleted after one year when the "Year" data has been created, which is never deleted. This way, annual backups without an expiration date are available and monthly backups for one year, while daily backups are available for a month. Old backup files without a prefix will be deleted after 180 days.

It is recommended to copy the backup files from TeslaLogger from time to time and store them at another location.
The files can be found with Windows File Explorer at the following location:
\\raspberry\teslalogger\backup

If, as described elsewhere, TeslaLogger is operated under a different name instead of "raspberry", use the changed name accordingly.

For experts:
After the regular backup process has been carried out, the script file "/etc/teslalogger/my-backup.sh" is executed, if present. This file could contain operations to automatically copy the backup files, for example, to a local NAS or a personal cloud storage.
