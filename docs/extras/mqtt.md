---
sidebar_position: 7
---
# MQTT-Einstellungen

Seit März 2024 wird MQTT über dieses Menü eingerichtet

![BILD](/img/admin-mqtt-01.png)

Man benötigt einen eingerichteten MQTT Brocker. In diesem Beispiel wird ein MQTT Brocker aus HomeAssistant installation genutz.

In dem Feld "Host" wird Hostname bzw. IP-Adresse des Brockers eingegeben. Das ist ein Pflichtfeld!
Port ist auf 1883 voreingestellt und muss in den meisten Fällen nicht verändert werden.
Wenn MQTT Bocker Login-Daten voraussetzt, bitte Benutzername und Passwort eingeben, sonst leer lassen.
Topic ist der Name des Topics unter welchem die Daten von TeslaLogger später auf MQTT publiziert werden.

TeslaLogger kann auf 2 Wege Daten versenden:
- als JSON, wird unter "TOPIC/json/CAR_VIN/currentjson" publizert
- als einzelne Topics, werden unter "TOPIC/car/CAR_VIN/PARAMETER_NAME" als einzelene Topics ("Single topic") publiziert

:::note

Eine der Optionen muss aktiviert werden, sonst werden keine Daten publiziert.

:::

Wenn man AutoDiscovery aktiviert, werden SmartHome Systeme, wie z.B. HomeAssistant, OpenHAB und weitere die Daten von TeslaLogger automatisch erkennen und einrichten.
Für diese Funktion muss "Single Topics" Option aktiviert werden.
Für Autodiscovery wird "homeassistant" als Topic voreingestellt und muss normalerweise nicht verändert werden.

TeslaLogger geniert bei Verbindung zu einem MQTT Brocker eine zufällige ClientID. In seltenen Fällen muss eine eindeutige ClientID vergeben werden. Bitte dieses Feld nur bewerten, wenn es wirklich nötig ist, sonst kann es zu Verbindungsproblemmen führen.

:::warning

Die Einstellungen werden erst nach "Speichern" und anschließendem Neustart übernommen!

:::