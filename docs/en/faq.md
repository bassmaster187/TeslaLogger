# Update doesn't work anymore
If for any reason your automatic / manual update doesn't work anymore, you can force an update this way:
Raspberry: 
- Open a SSH session (credentials: pi/teslalogger). 
```
cd /etc/teslalogger
sudo ./update.sh
```

Docker:
```
docker-compose stop
git fetch
git reset --hard origin/master
git checkout origin/master
docker-compose build
docker-compose up -d
```
