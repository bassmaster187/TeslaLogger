---
sidebar_position: 4
---
# Teslalogger Einrichtung mit bestehendem Backup

Wichtig: Der bisherige Teslalogger, von dem das Backup benutzt wird muss zu diesem Zeitpunkt abgeschaltet sein!
Das bestehende Backup wird in zwei Schritten durchgeführt. Die Reihenfolge ist nicht wirklich kritisch, aber wir empfehlen, zuerst die persönliche Geofence-Datei zurückzuspielen. Zu diesem Zweck im Windows Datei-Explorer oben in die Adressleiste klicken und dort «\\raspberry\teslalogger» eingeben. Es wird dann eine Aufforderung erscheinen, dass Passwort einzugeben. Dies ist «pi» und «teslalogger». Dort gibt es bereits eine Datei «geofence.csv» (nicht zu verwechseln mit der nicht persönlichen Datei «geofence-private.csv»), die gegen die eigene Datei ausgetauscht werden muss. Liegt mit der Datensicherung eine Datei mit der Erweiterung «.gz» vor, muss diese zuerst, beispielsweise mit 7Zip1, ausgepackt werden – an dieser Stelle darf nur eine Klartextdatei mit der Erweiterung .csv abgelegt werden.
Danach wird im Admin-Panel im Menü «Extras» der Punkt «Wiederherstellung» angewählt. Über die Schaltfläche «Datei auswählen» wird die vorhandene Datensicherung ausgewählt, die üblicherweise «mysqldump….gz» heisst. An dieser Stelle darf die Datei mit dieser Dateierweiterung angegeben werden.
![BILD](/img/installation-09.png)

Nach dem Klick auf «Restore» dauert es einige Minuten, bis die Erfolgmeldung erscheint:
![BILD](/img/installation-10.png)

An dieser Stelle den «Zurück»-Knopf des Webbrowsers bemühen und danach im Menü auf «Neustart» drücken. Danach braucht es ein paar Minuten und der neue Teslalogger arbeiten mit den übertragenen Daten.
Sollte Teslalogger im Heimnetz nicht als «raspberry» erreichbar sein (dies lässt sich mit einigen Routern entsprechend konfigurieren), muss nun noch im Menü «Einstellungen» der entsprechende Name konfiguriert werden für Admin Panel und Grafana. Beim Autor dieser Zeilen sieht das so aus:
![BILD](/img/installation-11.png)