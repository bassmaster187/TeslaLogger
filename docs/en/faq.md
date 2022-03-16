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
