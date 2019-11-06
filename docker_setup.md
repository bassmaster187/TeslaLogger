# Docker Setup (BETA)

1. Clone the Teslalogger repository into a new folder:
```
git clone https://github.com/bassmaster187/TeslaLogger
```

2. Create a fresh config file:
```
copy TeslaLogger\app.config TeslaLogger\bin\TeslaLogger.exe.config
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
                <value>Server=database;Database=teslalogger;Uid=root;Password=teslalogger;</value>
            </setting>
            <setting name="Car" serializeAs="String">
                <value>0</value>
            </setting>
....
```

6. fire up docker containers. Make sure, you got the latest docker & docker-compose version. Many repositories comes with very old versions!
```
docker-compose build
docker-compose up
```

after a minute or two, everything should be ready.

Try to connect to Grafana with you favorite browser:
http://localhost:3000 (admin/admin)

Try to connect to Admin-Panel
http://localhost:8888/admin/
