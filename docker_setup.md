# Docker Setup
For Synology NAS users, check the hints here: [LINK](docker_setup_synology.md) 

Docker on Raspberry: It won't work if you don't have a 64bit OS as MariaDB requires a 64bit OS!

Please make sure you have the latest docker and docker-compse. Many repositories comes with old docker and / or docker-compose. You can avoid a lot of problems by doblecheck it.

These versions are known to work fine:
```
docker -v
Docker version 19.03.2, build 6a30dfca03

docker-compose -v
docker-compose version 1.24.1, build 4667896b
```

1. Clone the Teslalogger repository into a new folder:
```
git clone https://github.com/bassmaster187/TeslaLogger
```

2. Create a fresh config file:
```
cp TeslaLogger/App.config TeslaLogger/bin/TeslaLogger.exe.config
```

3. edit TeslaLogger\bin\TeslaLogger.exe.config with your favorite editor

4. enter your My-Tesla credentials in TeslaName and TeslaPass

5. enter the DBConnectionstring:
```
Server=database;Database=teslalogger;Uid=root;Password=teslalogger;
```

The config file could look like this:
```xml
....
            <setting name="TeslaName" serializeAs="String">
              <value>elon@tesla.com</value>
            </setting>
            <setting name="TeslaPasswort" serializeAs="String">
              <value>123456</value>
            </setting>
            <setting name="DBConnectionstring" serializeAs="String">
                <value>Server=database;Database=teslalogger;Uid=root;Password=teslalogger;CharSet=utf8;</value>
            </setting>
            <setting name="Car" serializeAs="String">
                <value>0</value>
            </setting>
....
```

6. fire up docker containers. Make sure, you got the latest docker & docker-compose version. Many repositories comes with very old versions!
```
docker-compose build
docker-compose up -d
```

after a minute or two, everything should be ready. On some slow machines or NAS, that could take more than 10 Minutes. Especially the database may take longer, so don't give up, if Teslalogger can't connect to the DB at the first startup. 

Try to connect to Grafana with you favorite browser:
http://localhost:3000 (admin/teslalogger)

Try to connect to Admin-Panel
http://localhost:8888/admin/

# Docker update / upgrade
Usually, you update the Teslalogger in admin-panel by clicking on update button.
If there are updates of the subsystem, you have to get the latest docker-compose.yam file.

```
docker-compose stop
git fetch
git reset --hard origin/master
git checkout origin/master -- docker-compose.yml
docker-compose build
docker-compose up -d
```

# Trouble shooting
1. Init scripts like DB table setup not working

On slower devices like NAS devices, the init scripts of Teslalogger might not run successful during the first run. What you can do is restart the teslalogger_teslalogger_1 container after ~ 5 minutes. This should fix missing tables in the database and can easily be obsorved via the log files. Repeat a couple of times if required. A helpful command is:
```
docker restart teslalogger_teslalogger_1 # or use the beginning of the container ID instead of the name, e.g. r2d2
```

2. Messed up container images or build caches

If you are not running anything else in containers and are stuck with strange behaviour or errors, try cleaning up your existing container images, networks and build caches:
```
docker system prune # make sure all running containers are stopped before
```
