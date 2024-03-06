---
sidebar_position: 7
---
# MQTT-Einstellungen

Seit März 2024 wird MQTT über dieses Menü eingerichtet

![BILD](/img/admin-mqtt-01.png)

Man benötigt einen eingerichteten MQTT Brocker. In diesem Beispiel wird ein MQTT Brocker aus HomeAssistant installation genutz.

In dem Feld "Host" wird Hostname bzw. IP-Adresse des Brockers eingegeben.
Port ist standardmässig auf 1883 eingestellt und muss in den meisten Fällen nicht verändert werden.
Wenn MQTT Bocker mit Login-Daten versehen ist, bitte Benutzername und Passwort eingeben, sonst leer lassen.
Topic ist der Name des Topics unter welchem die Daten von TeslaLogger später auf MQTT publiziert werden.

TeslaLogger kann auf 2 Wege Daten versenden:
- als JSON, dann sind diese unter "TOPIC/json/CAR_VIN/currentjson" publizert
- als einzelne Topic, dann kommt jeder Paramter als einzeles Topic ("Single topic")
Eine der Optionen muss aktiviert werden, sonst werden keine Daten publiziert

Wenn man AutoDiscovery aktiviert, werden SmartHome Systeme, wie HomeAssistant, OpenHAB und weitere die Daten von TeslaLogger automatisch erkennen und einrichten.
Für diese Funktion muss "Single Topics" Option aktiviert werden.
Für Autodiscovery wird stadardmässig "homeassistant" als Topic verwedet und muss normalerweise nicht verändert werden.

TeslalLogger geniert bei Verbindung zu einem MQTT Brocker eine zufällige ClientID. In seltenen Fällen muss eine eindeutige ClientID vergeben werden. Bitte dieses Feld nur bewerten, wenn es wirklich nötig ist, sonst kann es zu Verbindungsproblemmen führen.

:::warning

Die Einstellungen werden erst nach "Speichern" und anschlißendem Neustart übernommen!

:::