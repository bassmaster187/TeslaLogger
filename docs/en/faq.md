# Update doesn't work anymore
If for any reason your automatic / manual update doesn't work anymore, you can force an update this way:
### Raspberry: 
- Open a SSH session (credentials: pi/teslalogger). 
```
cd /etc/teslalogger
sudo ./update.sh
```

### Docker:
```
docker-compose stop
git fetch
git reset --hard origin/master
git checkout origin/master
docker-compose build
docker-compose up -d
```

# Teslalogger keep loosing connection to Tesla Server
There are a few Teslas often loosing connection to Tesla Server. Sometimes this happen if you use the API with many Services at the same time (maybe because of calling the same API to often?). A common workaround is to create an additional driver in your Tesla Account and generate access token & refresh token with the driver credentials. Please make sure this driver account is just for Teslalogger and you don't share it with other apps. 

# Logfile: Unable to connect to any of the specified MySQL hosts
Because of a power failure during write attemp, the database won't start anymore. Deleting the transaction logfile is a common solution.

Check the current status of the database:
```
sudo service mysql status
```

If you find this, your transaction log is damaged:
```
Mar 12 05:46:17 raspberry mysqld[577]: 2022-03-12 5:46:17 1988878128 [Note] Recovering after a crash using tc.log
Mar 12 05:46:17 raspberry mysqld[577]: 2022-03-12 5:46:17 1988878128 [ERROR] Can’t init tc log
```

Delete the transaction log and restart teslalogger
```
sudo rm -f /var/lib/mysql/tc.log
sudo reboot now
```

# Grafana Dashboard Charging History / "Ladehistorie" has wrong entries for total costs

The reason for very high costs is a programming error which has been corrected.

Please create a manual backup before you proceed!

To restore the correct entries before the faulty version there is a web UI:

Navigate to (http://teslalogger:5000/RestoreChargingCostsFromBackup) (adapt to your settings of hostname and port, for Docker the default port is 5010) and
follow the steps:

- select a local backup found by TeslaLogger or upload your own backup file
- let TeslaLogger analyse the backup file, this might take a while depending on the size of your backup
- if differences between your current database and the backup are found, those differences will be listet and you can select to restore charge sessions values from the backup
- selected session values will be restored

No restart needed.

# Connect to your Raspberry with SSH
You need a SSH client to connect to the shell of your Raspberry. 
Windows users can use https://www.putty.org/ 
Mac User can use the terminal
Host: raspberry or raspberry.local
Name: pi
Password: teslalogger
