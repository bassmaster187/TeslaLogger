---
sidebar_position: 1
---
# Ersteinrichtung, die SD Karte mit dem Image bestücken

Wir gehen hier davon aus, dass noch keine fertig bespielte SD-Karte mit dem Teslalogger-Image vorliegt, sondern das Image als Download verfügbar ist und eine (leere) SD-Karte vorhanden ist. Liegt bereits eine fertig bespielte SD-Karte vor, kann dieses Kapitel übersprungen werden.
Die SD-Karte nun in den Computer schieben und dann herausfinden, auf welchem Laufwerk die SD-Karte dem Computer bekannt ist. Hierzu den Windows Datei Explorer öffnen, «Dieser Computer» anwählen und dort den Laufwerksbuchstaben identifizieren. Hier: D: (die SD-Karte hat 128GB, das sind in der Anzeige 119GB)

![BILD](/img/explorer.png)

Der Download kommt als zip-Datei, die darin enthaltene .img Datei muss ausgepackt werden. Ich empfehle, die .img Datei auf dem Desktop abzulegen und sie später dort wieder zu löschen und die zip-Datei irgendwo sicher abzulegen. Das Auspacken kann mehrere Minuten dauern.
Danach Win32 Disk Imager1 starten. Das Tool vorher von der angegebenen Webseite herunterladen,  und die Daten irgendwo in einem leeren Verzeichnis ablegen. Das Tool muss nicht installiert werden, es kann einfach die enthaltene .exe-Datei von dort per Doppelclick gestartet werden, wo es abgelegt wurde. Die SD-Karte wird normalerweise automatisch erkannt, das Laufwerk dazu wird unter «Device» angezeigt und sollte mit dem übereinstimmen, was vorher ermittelt worden ist. Nun noch die .img-Datei auf dem Desktop auswählen und dann auf «Write» drücken.

![BILD](/img/disk-imager.png)

Es kommt eine Warnmeldung, diese muss mit «Ja» bestätigt werden. Der Schreibvorgang wird dann in dem Balken unter «Progress» angezeigt und wird erneut einige Minuten dauern.
Achtung: Diverse Antivirus-Programme können diese Aktion unterbinden, da dies als mögliche Attacke auf die Disk (hier: die SD-Karte) gedeutet wird. Wie das zu verhindern ist, würde an dieser Stelle aber zu weit führen, da dies von der jeweiligen Software abhängt. Wenn die Software temporär abgeschaltet wird bitte unbedingt daran denken, diese am Ende auch wieder einzuschalten.
