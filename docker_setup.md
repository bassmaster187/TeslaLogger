git clone https://github.com/bassmaster187/TeslaLogger

copy TeslaLogger\app.config TeslaLogger\bin\TeslaLogger.exe.config

edit TeslaLogger\bin\TeslaLogger.exe.config with your favorite editor

enter your My-Tesla credentials in TeslaName and TeslaPass

enter the DBConnectionstring:
Server=database;Database=teslalogger;Uid=root;Password=teslalogger;

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

docker-compose build
docker-compose up

after a minute or two, everything should be ready.

Try to connect to Grafana with you favorite browser:
http://localhost:3000

Try to connect to Admin-Panel
http://localhost:8888/admin/
