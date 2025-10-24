---
sidebar_position: 2
---
# Ersteinrichtung
Nachdem die SD-Karte bespielt wurde (bzw. die bereits bespielte Karte geliefert wurde), wird sie in den Raspberry eingesetzt. Dabei kommen die Metallkontakte der Karte nach oben (Richtung Platine) zuerst in den SD-Slot. Danach muss das Netzwerkkabel eingesteckt werden und das Gerät an den Strom angeschlossen werden.
Die Erstinbetriebnahme kann ein paar Minuten dauern, später geht ein Neustart etwas schneller.
Nach ein paar Minuten sollte der Raspberry mit Eingabe von «http://raspberry» ansprechbar sein. «https://» gibt es hier nicht. Da dies aber mittlerweile und aus guten Gründen für öffentliche Webseiten Standard ist, bitte unbedingt «http://raspberry» ausschreiben. Eventuell produziert der Browser eine Warnung, das ist ok und kann bestätigt werden. Es sollte dann das folgende Bild zu sehen sein, das bestätigt, dass der Raspberry erreichbar ist und funktioniert:

![BILD](/img/installation-01.png)

un kann das Admin-Panel aufgerufen werden und die Erstkonfiguration des Teslalogger kann beginnen.
Bitte «http://raspberry/admin» aufrufen, es erscheint das Admin-Panel:

![BILD](/img/installation-02.png)

Hier sind zwei wichtige Sachen  zu sehen:
1. Die Frage zum Anonymen Teilen von Daten im oberen Drittel
2. Der Hinweis, dass ein Update verfügbar ist (hier: 1.47.2.0, die Bilder wurden mit 1.46.0.0 gemacht). Der Hinweis erscheint natürlich nur, wenn das Image nicht 100% aktuell ist – dies dürfte aber der Regelfall sein.
Wir empfehlen das Anonyme Teilen von Daten zu aktivieren, da hier die ganze Teslalogger-Community von diesen statistischen Daten profitiert. Die entsprechenden Daten sind im Menü unter «Flottenstatistik» einzusehen. Die Einstellung kann jederzeit im Menü «Einstellungen» geändert werden und wird auf Seite 20, «Flottenstatistik»  beschrieben
Updates sind wichtig, bringen neue Funktionen und beheben in vielen Fällen auch bekannte Fehler. Wir empfehlen, die Updates wie hier im Bild zu sehen immer zu aktivieren. Das kann ebenfalls über das Menü «Einstellungen» automatisiert werden und wir empfehlen, vor jeder anderen Einrichtungsaktivität zuerst den Update durchzuführen. Dies kann über das Menü oben mit Druck auf «Aktualisierung» eingeleitet werden. Der Raspberry wird einen Neustart bestätigen und steht nach 2-3 Minuten danach wieder zur Verfügung (einfach nach 2-3 Minuten über Taste F5 das Bild erneuern und schauen, dass die Teslalogger-Version jetzt auf dem angekündigten Stand ist.
Wenn dieses Bild erscheint, ist der Raspberry noch nicht vollständig neu gestartet, es braucht dann noch einen Augenblick länger:

![BILD](/img/installation-03.png)

Wenn das Image auf dem aktuellen Stand ist, gibt es zwei Möglichkeiten. Die Ersteinrichtung für diejenigen, die Teslalogger bisher noch nicht benutzt haben und die Einrichtung dieser Instanz für Leute, die von einem alten Image/einer alten Installation beispielsweise von einem Raspberry 3 auf einen Raspberry 4 umsteigen.