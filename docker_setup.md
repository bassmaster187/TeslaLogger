git clone https://github.com/bassmaster187/TeslaLogger

copy TeslaLogger\app.config TeslaLogger\bin\TeslaLogger.exe.config

edit TeslaLogger\bin\TeslaLogger.exe.config with your favorite editor

enter your My-Tesla credentials in TeslaName and TeslaPass

enter the DBConnectionstring:
Server=database;Database=teslalogger;Uid=root;Password=teslalogger;

docker-compose build
docker-compose up

after a minute or two, everything should be ready.

Try to connect to Grafana with you favorite browser:
http://localhost:3000

Try to connect to Admin-Panel
http://localhost:8888/admin/
