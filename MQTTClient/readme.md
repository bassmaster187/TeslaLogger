## MQTT Settings
Edit settings in file: **MQTTClient.exe.config**

### MQTTHost (mandatory)
MQTT brocker address or IP

Example:
```
<setting name="MQTTHost" serializeAs="String">
    <value>192.168.1.35</value>
</setting>
```

### MQTTPort (optional)
MQTT brocker port

Example:
```
<setting name="MQTTPort" serializeAs="String">
    <value>3456</value>
</setting>
```

### Topic (mandatory)
Topic name. Default "Tesla". 

If you have more than one car, TeslaLogger CarID will be added: "Tesla-2"

This topic returns json-string with all available parameters

Example:
```
<setting name="Topic" serializeAs="String">
    <value>mytesla</value>
</setting>
```

### Subtopics (optional)
Enable or disable subtopics. Default: "False".

If enabled, json-string from "Topic" (see above) will be splitted into subtopics: e.g. mytesla-2/battery_level

Example:
```
<setting name="Subtopics" serializeAs="String">
    <value>True</value>
</setting>
```

### Name (optional)
Login name for MQTT brocker (if required)

Example:
```
<setting name="Name" serializeAs="String">
    <value>teslalogger</value>
</setting>
```

### Password (optional)
Login password for MQTT brocker (if required)

Example:
```
<setting name="Password" serializeAs="String">
    <value>mypassword123</value>
</setting>
```


### ClientID (optional)
User specific MQTT ClientID. Useful if more then one TeslaLogger are connected to the same MQTT brocker.

Example:
```
<setting name="ClientID" serializeAs="String">
    <value>Teslalogger1</value>
</setting>
```