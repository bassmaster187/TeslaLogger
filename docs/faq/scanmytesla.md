---
sidebar_position: 1
---

# ScanMyTesla

ScanMyTesla ist eine Smartphone-App, die es ermöglicht, Daten aus dem Tesla auszulesen, die über die reguläre Tesla-App nicht verfügbar sind und daher auch für Teslalogger normalerweise nicht zugänglich sind.
Hierfür muss ein sogenannter OBD2-Adapter im Fahrzeug eingebaut werden, der dann via Bluetooth mit der «SMT»-App gekoppelt wird. In einem zweiten Schritt kann nun die App so konfiguriert werden, dass sie die ausgewählten Live-Daten an die Teslalogger-Installation zuhause übermittelt und so die sowieso schon vorhandene Datensammlung mit weiteren Informationen wie beispielsweise der tatsächlich verfügbaren Batteriekapazität, der Batterietemperatur angereichert werden kann.
Die Kopplung der App mit Teslalogger geschieht ähnlich wie schon vorher bei der Integration mit «A Better Routeplanner» mit einem eindeutigen Token. Das Token für SMT ist im Einstellungsdialog als «Tasker Token» zu finden. Hier muss dann auch der Haken bei «ScanMy Tesla-Integration» gesetzt werden. Der Wert für das Token muss dann in der App unter «Settings» - «Teslalogger» erfasst werden. Das sieht so aus:

![BILD](/img/smt-01.png)

Sobald der OBD2-Adapter nach Anleitung mit der App gekoppelt ist und während einer Fahrt läuft (das kann auch im Hintergrund sein), dann werden die ausgewählten Datenpunkte live an den Teslalogger übermittelt.
Der Einbau des OBD2-Adapters ist von Fahrzeug zu Fahrzeug leicht unterschiedlich, meist ist der Zugang wie beim Model 3 und Model Y unten in der Mittelkonsole vor der hinteren Sitzreihe zu finden. Hier muss der Adapter mit einem Y-Kabel in die Datenleitung eingeschleift werden. Es empfiehlt sich, das Fahrzeug vorher stromlos zu machen. Dies ist ebenfalls von Fahrzeug zu Fahrzeug unterschiedlich, bei Model 3/Y findet sich unter der hinteren Sitzbank auf der linken Seite ein Schalter dafür. Die Installation kann nach Öffnen der Abdeckung so aussehen:

![BILD](/img/smt-02.png)

Wer sich die Installation nicht selber zutraut, sollte unbedingt einen Tesla Bodyshop aufsuchen. Passende Adapter und Bezugsquellen werden auf der Homepage von «Scan My Tesla»  dokumentiert.
Wie die Daten aus der App dann in die normalen Daten des Teslaloggers einfliessen lässt sich in den Dashboards zur Degradation («Nominal full pack», zu Ladevorgängen (insbesondere an Schnellladern, «Zelltemperatur», «Cell imbalance» etc) sowie in den üblichen «Trips» finden.
