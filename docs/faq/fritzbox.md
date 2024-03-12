---
sidebar_position: 2
---

# Optimierung mit einer Fritz!Box

Die in vielen deutschsprachigen Ländern weit verbreitete Fritz!Box bietet ein paar schöne Funktionen, um die Benutzung des Teslaloggers zu vereinfachen. Ähnliches geht auch mit anderen Heimroutern und mit professioneller Infrastruktur sowieso.


### Fixe IP

Der Teslalogger ist wie ein Server im Internet, aber zuhause. Server im Internet haben im Normalfall eine fixe IP-Adresse, so dass sie besser und einfacher erreichbar sind und deshalb ist es sinnvoll, dem Gerät auf dem der Teslalogger läuft ebenfalls eine feste IP-Adresse zuhause zu vergeben.

### Mit Namen statt IP Adresse ansprechen

Kein System im Internet wird direkt mit seiner IP Adresse angesprochen, sondern mit seinem Namen, beispielsweise [https://teslalogger.de](https://teslalogger.de). Genau das geht auch mit dem Teslalogger zuhause. Im Beispiel vergeben wir dem Raspberry einen Namen, so dass er danach via https://teslalogger.fritz.box zuhause angesprochen werden kann.
Das Folgende bezieht sich auf eine Fritz!Box mit Firmware Version 7.x, bei älteren Versionen ist es aber prinzipiell ähnlich.
1.	Mit dem Webbrowser an der Fritz!Box anmelden. Das geht normalerweise mit [https://fritz.box](https://fritz.box)
2.	Sicherstellen, dass die «Erweiterte Ansicht» aktiv ist, dazu recht oben auf den Fritz!Box Benutzernamen drücken, im Normalfall ist das «Admin». Dort den Schalter für «Erweiterte Ansicht» aktivieren, falls dies noch nicht passiert ist.
3.	«Heimnetz», dann «Netzwerk» anwählen
4.	In der Liste den Raspberry suchen und in der gleichen Zeile rechts die Bearbeiten-Schaltfläche anwählen
5.	Den Namen anpassen, im Beispiel haben wir «teslalogger» benutzt

![BILD](/img/fritzbox-01.png)

6.	Ausserdem wird hier die IP-Adresse definiert. Diese kann prinzipiell so bleiben wie sie ist, aber der Haken bei «Diesem Netzwerkgerät immer die gleiche IPv4-Adresse zuweisen» wird angewählt
7.	Unten recht auf «ok» drücken. Der Raspberry ist ab sofort unter dem neuen Namen ansprechbar
