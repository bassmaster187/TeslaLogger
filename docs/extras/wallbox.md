---
sidebar_position: 10
---
# Wallbox

Diese Funktion erlaubt einbung von Wallboxen um bessere Ladestatistiken zu erzeugen.
Je nach Wallbox und verfügbare Daten werden zusätzlich folgende Daten unter Ladehistorie berechnet und angezeigt:
- Ladewirkungsgrad
- PV Anteil
- Ladepreis

![BILD](/img/extras-wallbox-01.png)

Für die Einrichtung braucht man mindestens einen Hostnamen bzw. eine IP Adresse der Wallbox. Host bzw. IP-Adresse müssen mit http:// bzw. https:// Prefix eingegeben werden!
Bei einigen wird zusätzlich Port und/oder zusätzliche Parameter angegeben.

Mit dem Test-Button wir die Verbindung geprüft.
Wenn der Test erfolgreich war, wird mindestens die Versionsnummer angezeigt:

![BILD](/img/extras-wallbox-02.png)

Aktuell unterstützte Wallboxen:
- OpenWB
- go-eCharger
- Tesla Wallbox Gen 3
- KEBA KeContact P30 (P20?)
- Shelly 3EM for all unsupported wallboxes or without build in meter: https://amzn.to/3nUEuO0
- Shelly EM for 1 phase charging
- EVCC



Besondere Hinweise:

- go-eCharger
Parameter: leer lassen
Es muss "lokale HTTP API v1" unter "Einstellungen -> Erweiterte Einstellungen" aktiviert werden

- OpenWB
Parameter: LP1-LP8 je nach Ladepunkt (Wenn nichts eingeben wird LP1 angenommen)
Funktioniert nur bis Firmware 1.9 (via HTTP)

- ShellyEM
Parameter: C1 (oder leer lassen) für Kanal 1 bzw. C2 für Kanal 2 

- EVCC
Host/IP Adresse mit Port (meistens :7070)
Parameter: Wallbox Titel (nicht von dem Auto!)

