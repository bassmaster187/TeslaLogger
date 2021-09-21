# Teslamate Import Tool (Beta)

With this  tool you can import your data from Teslamate into Teslalogger.

# Import your data
Expose Postgresql port 5432 to your host. Change your docker-compose.yml file of Teslamate:

```
database:
    image: postgres:13
    restart: always
    environment:
      - POSTGRES_USER=teslamate
      - POSTGRES_PASSWORD=secret
      - POSTGRES_DB=teslamate
    ports:
        - 5432:5432
    volumes:
      - teslamate-db:/var/lib/postgresql/data
```

Make sure your database connection string to teslamate and teslalogger is valid in config file: bin/Teslamate-Import.exe.config

```
<applicationSettings>
    <Teslamate_Import.Settings1>
      <setting name="TeslaloggerDB" serializeAs="String">
        <value>Server=127.0.0.1;Database=teslalogger;Uid=root;Password=teslalogger;</value>
      </setting>
      <setting name="TeslamateDB" serializeAs="String">
        <value>Host=localhost;Username=teslamate;Password=secret;Database=teslamate</value>
      </setting>
    </Teslamate_Import.Settings1>
  </applicationSettings>
  ```
  Make a backup of Teslamate and Teslalogger database and move it to a safe place!!!
  
  Start the tool bin/Teslamate-Import.exe
  On Windows 10 it should run without installing .net Framework. On macOS and Linux you may need to download and use Mono: https://www.mono-project.com/download/stable/ 
  
  This process may take a long time depending on the power of your machine, network and storage....
  
  If everything is finished, inspect the Logfile for errors (Teslamate-Logfile.txt). If there are Exceptions, feel free to open a bug in github.
  The tool created a geofence file  (geofence-private.csv). Copy it to your Teslalogger.exe folder.
  
  Restart Teslalogger. Make sure your credentials for your car are already entered. Make sure the IDs are matching Teslalogger and Teslamate car id. If you have just one car, ID should be 1.
  
  Now it starts to geocode all destinations. This may take a very long time as Nominatim / OpenStreetMap is limited to one geocode all 5 seconds. 
  Meanwile you can take a look at Visited, Trips and Chargings Dashboard.
  
  
