# TeslaFi Import Tool

With this importer your data from your TeslaFi account can be imported into TeslaLogger.

# Get the raw data from your TeslaFi account
* Log in to your TeslaFi account
* Open Settings - Account
* Scroll down to the Advanced section
* click on Download TeslaFi Data
* Select the year and month for which data should be exported
* Click on submit
* Save the file on your local computer
* Repeat the download for all months whose data you want to import into Teslalogger

# Importing TeslaFi data into the TeslaLogger
## Raspberry edition
* Copy all CSV files into the directory: \\\\RASPBERRY\teslalogger
* Log in to Raspberry via ssh
   User: pi
   Password: teslalogger
* execute the following commands
```
sudo pkill mono
sudo service grafana-server stop
sudo service apache2 stop
cd /etc/teslalogger/
mono TeslaFi-Import.exe
```
please be patient, this may take a long time
After it has done it's job, please reboot the Raspberry:
```
sudo reboot now
```

## Docker edition
Copy all CSV files into the subdirectory of your docker-compose.yml file : TeslaLogger\bin

Get the container name of Teslalogger.
```
Docker PS
```

Open a shell into the docker container:
```
docker exec -it <container name> /bin/bash
```

Start the import:
```
cd /etc/teslalogger/
mono TeslaFi-Import.exe
```
please be patient, this may take a long time
After it has done it's job, please reboot the Raspberry:

Restart your container.

## Note
TeslaFi data are only imported up to the first data set from the Teslalogger. If both systems were running at the same time, the import ends with the first data set from the Teslalogger.

Since TeslaFi does not contain any addresses, everything is empty after the import. If you restart the Teslalogger afterwards, it will geocode all addresses again. Depending on how much data you have, this takes a long time, because I only geocode every 5 seconds to avoid being blocked by OpenStreetMap.
