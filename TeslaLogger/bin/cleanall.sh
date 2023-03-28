#!/bin/bash
pkill mono
mysql -u root -pteslalogger teslalogger -Bse "delete from pos;delete from charging;delete from chargingstate; delete from drivestate;delete from shiftstate;delete from state;delete from can;delete from car_version;delete from cars;delete from journeys;delete from TPMS; delete from superchargers;delete from superchargerstate; delete from mothership; delete from geocodecache;" 
rm -rf /etc/teslalogger/Exception/
rm -rf /etc/teslalogger/backup/*
rm -rf /etc/teslalogger/MAP-Data/*
rm -f /etc/teslalogger/current_json.txt
rm -f /etc/teslalogger/tesla_token.txt
rm -f /etc/teslalogger/TASKERTOKEN
rm -f /etc/teslalogger/LASTSCANMYTESLA
rm -f /etc/teslalogger/LASTTASKERWAKEUPFILE
rm -f /etc/teslalogger/MISSING
rm -f /etc/teslalogger/DISPLAY_NAME
rm -f /etc/teslalogger/weather.ini
rm -f /etc/teslalogger/sharedata.txt
rm -f /etc/teslalogger/nosharedata.txt
rm -f /etc/teslalogger/my-backup.sh
rm -f /etc/teslalogger/mono_crash.*
cp /etc/teslalogger/GeocodeCacheEmpty.xml /etc/teslalogger/GeocodeCache.xml 
> TeslaFi-Logfile.txt
> nohup.out
mkdir /etc/teslalogger/Exception
chmod 777 /etc/teslalogger/Exception
rm -rf /etc/teslalogger/MQTTClient.exe.config
rm -rf /etc/teslalogger/TeslaLogger.exe.config
cp /etc/teslalogger/git/TeslaLogger/App.config /etc/teslalogger/TeslaLogger.exe.config
cp /etc/teslalogger/git/MQTTClient/App.config /etc/teslalogger/MQTTClient.exe.config
echo "Ladestation Bauhaus Ulm, 48.400892, 9.970095" > geofence-private.csv
> /etc/teslalogger/MISSINGKM
chmod 777 /etc/teslalogger/TeslaLogger.exe.config
chmod 777 /etc/teslalogger/MQTTClient.exe.config
