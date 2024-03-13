---
slug: /
sidebar_position: 1
---
# Einführung

Ziel ist es, die Daten eines Fahrzeugs der Marke Tesla aus dem Auto auszulesen, ohne die Tesla-Zugangsdaten (Passwort des persönlichen Tesla-Kontos) aus den Händen geben zu müssen wie dies bei Cloud-Services notwendig ist und damit auch die Kontrolle über das Fahrzeug abzugeben oder die doch sehr persönlichen Daten wie das Bewegungsprofil auf fremden Servern zu speichern.
Neben dem Wunsch, die eigenen Daten auch in den eigenen Händen halten zu wollen ist ein weiteres Ziel, im Falle von Ansprüchen gegenüber Tesla mit diesen Daten auch argumentieren zu können. So lässt sich unter Vorweisung der Detaildaten ein Akkutausch im Fall, dass dieser notwendig erscheint, sehr viel besser argumentieren. Ohne diese Daten hat nur Tesla alle Informationen über das Fahrzeug. Hier geht es auch um die sogenannte «Prospekthaftung» im Rahmen der zugesicherten Leistungsversprechen von Seiten des Herstellers.
Der Teslalogger ist eine Software, die auf einem Raspberry Pi oder in einer Docker-Umgebung zuhause läuft, die Daten aus dem Fahrzeug ausliest und lokal speichert. Für die Auswertung gibt es ein Webinterface, welche verschiedene Übersichten bietet wie Ladestatistik, Trips, Verbrauch, Akku Degradation, Vampir Drain und vieles mehr. Das Gerät muss für die Funktion nicht von aussen erreichbar sein. Teslalogger ist keine Smartphone-App, wer von unterwegs auf die Datenzugreifen will, braucht eine sichere Verbindung nach Hause (VPN).