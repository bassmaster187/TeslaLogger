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
