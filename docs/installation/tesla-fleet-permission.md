---
sidebar_position: 8
slug: /tesla-fleet-permission
---
# Welche Berechtigungen werden benötigt und warum?

## Vehicle Information
Diese Berechtigung ist Pflicht für Teslalogger. Sie wird verwendet, um Standort, SOC, Ladegeschwindigkeit usw. des Fahrzeugs abzurufen.
Ohne diese Freigabe funktioniert Teslalogger überhaupt nicht.

## Vehicle Commands
Diese Berechtigung ist optional. Sie wird benötigt, wenn Befehle an das Fahrzeug gesendet werden sollen: mit der [Geofence](https://github.com/bassmaster187/TeslaLogger/blob/master/TeslaLogger/Geofence.md)-Funktion (zum Beispiel Wächtermodus an einer bestimmten Adresse einschalten), über die eingebaute [REST-API](https://github.com/bassmaster187/TeslaLogger/blob/master/TeslaLogger/WebServer.md) von Teslalogger oder über Smart Home / MQTT.

## Profile Information
Derzeit gar nicht in Verwendung. Es gibt aber Funktionswünsche, die sie nutzen könnten – zum Beispiel eine E-Mail senden, wenn Teslalogger aus irgendeinem Grund nicht mehr arbeitet.

## Vehicle Charging Management
Diese Berechtigung ist optional. Teslalogger kann damit die Rechnungen aus dem Konto abrufen und den Betrag speichern, der an einem Supercharger bezahlt wurde; bei kostenlosem Supercharging wird zusätzlich die Ersparnis berechnet.

## Energy Product Information
Derzeit nicht verwendet; denkbar wäre zum Beispiel, den Anteil des eigenen Solarstroms beim Laden zu Hause zu berechnen.

## Energy Product Commands
Derzeit nicht verwendet.

![Permissions-en](https://github.com/bassmaster187/TeslaLogger/assets/6816385/cb650056-fc76-4433-af17-cae896070bfb)
