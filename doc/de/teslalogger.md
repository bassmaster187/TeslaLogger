**Teslalogger**
Dieses Handbuch beschreibt die Version 1.47
Handbuchversion vom März 2021
Autor: [Rolf](https://www.3lands.ch/rolf)[ ](https://www.3lands.ch/rolf)[Wilhelm](https://www.3lands.ch/rolf)

**Inhaltsverzeichnis**
[1.](#br2)[ ](#br2)[Worum](#br2)[ ](#br2)[geht](#br2)[ ](#br2)[es](#br2)[ ](#br2)[und](#br2)[ ](#br2)[wie](#br2)[ ](#br2)[wird](#br2)[ ](#br2)[es](#br2)[ ](#br2)[gelöst?](#br2)
[1.1](#br2)[ ](#br2)[Was](#br2)[ ](#br2)[braucht](#br2)[ ](#br2)[es](#br2)[ ](#br2)[dazu?](#br2)
[1.2](#br2)[ ](#br2)[Dieses](#br2)[ ](#br2)[Handbuch](#br2)
[2.](#br3)[ ](#br3)[Die](#br3)[ ](#br3)[Installation](#br3)
[2.1](#br3)[ ](#br3)[Ersteinrichtung,](#br3)[ ](#br3)[die](#br3)[ ](#br3)[SD](#br3)[ ](#br3)[Karte](#br3)[ ](#br3)[mit](#br3)[ ](#br3)[dem](#br3)[ ](#br3)[Image](#br3)[ ](#br3)[bestücken](#br3)
[2.2](#br4)[ ](#br4)[Ersteinrichtung](#br4)
[2.3](#br6)[ ](#br6)[Teslalogger](#br6)[ ](#br6)[Ersteinrichtung](#br6)[ ](#br6)[(ohne](#br6)[ ](#br6)[bestehendes](#br6)[ ](#br6)[Backup](#br6)[ ](#br6)[einer](#br6)[ ](#br6)[anderen](#br6)[ ](#br6)[Installation)6(#br6)
[2.4](#br9)[ ](#br9)[Teslalogger](#br9)[ ](#br9)[Einrichtung](#br9)[ ](#br9)[mit](#br9)[ ](#br9)[bestehendem](#br9)[ ](#br9)[Backup](#br9)
[2.5](#br10)[ ](#br10)[Smart-Home](#br10)[ ](#br10)[mit](#br10)[ ](#br10)[MQTT](#br10)
[2.6](#br11)[ ](#br11)[Mehrere](#br11)[ ](#br11)[Fahrzeuge](#br11)[ ](#br11)[im](#br11)[ ](#br11)[gleichen](#br11)[ ](#br11)[Tesla-Account](#br11)
[3.](#br12)[ ](#br12)[Das](#br12)[ ](#br12)[Admin-Interface](#br12)
[3.1](#br13)[ ](#br13)[Die](#br13)[ ](#br13)[Auswahl](#br13)[ ](#br13)[des](#br13)[ ](#br13)[Fahrzeugs](#br13)
[3.2](#br13)[ ](#br13)[Logfile](#br13)
[3.3](#br13)[ ](#br13)[Restart](#br13)
[3.4](#br13)[ ](#br13)[Update](#br13)
[3.5](#br13)[ ](#br13)[Extras](#br13)[ ](#br13)[-](#br13)[ ](#br13)[Backup](#br13)
[3.6](#br14)[ ](#br14)[Extras](#br14)[ ](#br14)[–](#br14)[ ](#br14)[Restore](#br14)
[3.7](#br14)[ ](#br14)[Extras](#br14)[ ](#br14)[–](#br14)[ ](#br14)[Geofence](#br14)
[3.8](#br16)[ ](#br16)[Extras](#br16)[ ](#br16)[–](#br16)[ ](#br16)[Dashboard](#br16)
[3.9](#br17)[ ](#br17)[Extras](#br17)[ ](#br17)[-](#br17)[ ](#br17)[Suspend](#br17)
[3.10](#br17)[ ](#br17)[Extras](#br17)[ ](#br17)[-](#br17)[ ](#br17)[Wakeup](#br17)
[3.11](#br18)[ ](#br18)[Settings](#br18)
[3.12](#br19)[ ](#br19)[Fleet](#br19)[ ](#br19)[Statistic](#br19)
[4.](#br21)[ ](#br21)[Die](#br21)[ ](#br21)[Auswertungen](#br21)
[4.1](#br22)[ ](#br22)[Standardauswertungen](#br22)[ ](#br22)[abrufen](#br22)
[4.2](#br23)[ ](#br23)[Favoriten](#br23)[ ](#br23)[definieren](#br23)
[4.3](#br23)[ ](#br23)[Mit](#br23)[ ](#br23)[den](#br23)[ ](#br23)[Auswertungen](#br23)[ ](#br23)[arbeiten](#br23)
[4.4](#br24)[ ](#br24)[Verfügbare](#br24)[ ](#br24)[Auswertungen](#br24)
[5.](#br30)[ ](#br30)[Optimierung](#br30)[ ](#br30)[mit](#br30)[ ](#br30)[einer](#br30)[ ](#br30)[Fritz!Box](#br30)
[5.1](#br30)[ ](#br30)[Fixe](#br30)[ ](#br30)[IP](#br30)
[5.2](#br30)[ ](#br30)[Mit](#br30)[ ](#br30)[Namen](#br30)[ ](#br30)[statt](#br30)[ ](#br30)[IP](#br30)[ ](#br30)[Adresse](#br30)[ ](#br30)[ansprechen](#br30)

**1. Worum geht es und wie wird es gelöst?**
Ziel ist es, die Daten eines Fahrzeugs der Marke Tesla aus dem Auto auszulesen, ohne die Tesla-
Zugangsdaten (Passwort des persönlichen Tesla-Kontos) aus den Händen geben zu müssen wie dies
bei Cloud-Services notwendig ist oder die doch sehr persönlichen Daten (Bewegungsprofil) auf
fremden Servern zu speichern.
Neben dem Wunsch, die eigenen Daten auch in den eigenen Händen halten zu wollen ist ein
weiteres Ziel, im Falle von Ansprüchen gegenüber Tesla mit diesen Daten auch argumentieren zu
können. So lässt sich unter Vorweisung der Detaildaten ein Akkutausch im Fall, dass dieser
notwendig erscheint, sehr viel besser argumentieren. Ohne diese Daten hat nur Tesla alle
Informationen über das Fahrzeug. Hier geht es auch um die sogenannte «Prospekthaftung» im
Rahmen der zugesicherten Leistungsversprechen von Seiten des Herstellers.
Der Teslalogger ist eine Software, die auf einem Raspberry Pi oder in einer Docker-Umgebung
zuhause läuft, die Daten aus dem Fahrzeug ausliest und lokal speichert. Für die Auswertung gibt es
ein Webinterface, welche verschiedene Übersichten bietet wie Ladestatistik, Trips, Verbrauch, Akku
Degradation, Vampir Drain und vieles mehr. Das Gerät muss für die Funktion nicht von aussen
erreichbar sein. Teslalogger ist keine Smartphone-App, wer von unterwegs auf die Datenzugreifen
will, braucht eine sichere Verbindung nach Hause (VPN).

**1.1 Was braucht es dazu?**
Der Teslalogger ist entwickelt, um mit einem Raspberry Pi 3+/4 zu funktionieren. Dafür gibt es
auch ein «Image», um die Installation zu vereinfachen. Zusätzlich gibt es die Möglichkeit, das
Image in einem Docker-Kontext zu betrieben (typischerweise auf einem zuhause vorhandenem
NAS, dann braucht es keinen Raspberry mehr). Da das Projekt als OpenSource bei GitHub
verwaltet wird, ist es technisch möglich, dies auch auf anderen Plattformen zu betreiben. Im
Anhang gibt es dazu einige Hinweise.
Wir beschreiben hier die Standardinstallation auf einem Raspberry Pi 4. Der Raspberry verbraucht
sehr wenig Strom, ist klein und kann fast unsichtbar überall zuhause untergebracht werden, wo ein
Internetanschluss zur Verfügung steht. Zusätzlich zur reinen Raspberry Platine braucht es ein
passendes Netzteil, eine MicroSD-Karte für das Image und die Daten, ein Netzwerkkabel für den
Anschluss an das Heimnetzwerk (Router) und eigentlich auch ein Gehäuse.
Bei Amazon.de gibt es ein entsprechendes Kit , aber grundsätzlich tut es jeder Raspberry Pi 4, die
[1](#br2)
passende Stromversorgung und eine mindestens 16GB grosse MicroSD-Karte. Stand 2021 wird
natürlich der sehr viel performantere Raspberry 4 empfohlen, aber wirklich technisch braucht es das
nicht.
Auf der Webseite der e-mobility driving solutions GmbH, unter deren Schirmherrschaft der
Teslalogger entwickelt wird, gibt es natürlich ein fix fertiges Kit inklusive fertig bespielter
MicroSD-Karte .
[2](#br2)
**1.2 Dieses Handbuch**
Dieses Handbuch ist genauso wie das ganze Projekt OpenSource. Das Original liegt als LibreOffice
OpenDocument .odt Datei vor und wird im Teslalogger als PDF eingebunden.
Die Beispiele in diesem Handbuch wurden auf Basis eines Raspberry Pi 3, Model B, einem
Windows-Desktop und Google Chrome als Browser angefertigt.
1
2
https://www.amazon.de/Raspberry-Pi-ARM-Cortex-A72-Bluetooth-Micro-HDMI/dp/B07TC2BK1X
https://e-mobility-driving-solutions.com/produkt-kategorie/teslalogger/


Alle Beispiele gehen davon aus, dass der Teslalogger unter dem Namen «raspberry» im Heimnetz
erreichbar ist. Dies kann geändert werden, siehe dazu unter anderem auch Seite [30](#br30), «[Mit](#br30)[ ](#br30)[Namen](#br30)
[statt](#br30)[ ](#br30)[IP](#br30)[ ](#br30)[Adresse](#br30)[ ](#br30)[ansprechen](#br30)», die Beispiele sind dann entsprechend bei der Benutzung anzupassen.

**2. Die Installation**

**2.1 Ersteinrichtung, die SD Karte mit dem Image bestücken**

Wir gehen hier davon aus, dass noch keine fertig bespielte SD-Karte mit dem Teslalogger-Image
vorliegt, sondern das Image als Download verfügbar ist und eine (leere) SD-Karte vorhanden ist.
Liegt bereits eine fertig bespielte SD-Karte vor, kann dieses Kapitel übersprungen werden.
Die SD-Karte nun in den Computer schieben und dann herausfinden, auf welchem Laufwerk die
SD-Karte dem Computer bekannt ist. Hierzu den Windows Datei Explorer öffnen, «Dieser
Computer» anwählen und dort den Laufwerksbuchstaben identifizieren. Hier: D: (die SD-Karte hat
128GB, das sind in der Anzeige 119GB)
Der Download kommt als zip-Datei, die darin enthaltene .img muss ausgepackt werden. Ich
empfehle, die .img Datei auf dem Desktop abzulegen und sie später dort wieder zu löschen und die
zip-Datei irgendwo sicher abzulegen. Das Auspacken kann mehrere Minuten dauern.


Danach Win32 Disk Imager starten. Das Tool vorher von der angegebenen Webseite herunterladen,
[3](#br4)
und die Daten irgendwo in einem leeren Verzeichnis ablegen. Das Tool muss nicht installiert
werden, es kann einfach die enthaltene .exe-Datei von dort per Doppelclick starten, wo es abgelegt
wurde. Die SD-Karte wird normalerweise automatisch erkannt, das Laufwerk dazu wird unter
«Device» angezeigt und sollte mit dem übereinstimmen, was vorher ermittel worden ist. Nun noch
die .img-Datei auf dem Desktop auswählen und dann auf «Write» drücken.
Es kommt eine Warnmeldung, die muss mit «Ja» bestätigt werden. Der Schreibvorgang wird dann
in dem Balken unter «Progress» angezeigt und wird erneut einige Minuten dauern.
Achtung: Diverse Antivirus-Programme können diese Aktion unterbinden, da dies als mögliche
Attacke auf die Disk (hier: die SD-Karte) gedeutet wird. Wie das zu verhindern ist, würde an dieser
Stelle aber zu weit führen, da dies von der jeweiligen Software abhängt. Wenn die Software
temporär abgeschaltet wird bitte unbedingt daran denken, diese am Ende auch wieder einzuschalten.

**2.2 Ersteinrichtung**
Nachdem die SD-Karte bespielt wurde (bzw. die bereits bespielte Karte geliefert wurde), wird sie in
den Raspberry eingesetzt. Dabei kommen die Metallkontakte der Karte nach oben (Richtung
Platine) zuerst in den SD-Slot. Danach muss das Netzwerkkabel eingesteckt werden und das Gerät
an den Strom angeschlossen werden.
Die Erstinbetriebnahme kann ein paar Minuten dauern, später geht ein Neustart etwas schneller.
Nach ein paar Minuten sollte der Raspberry mit Eingabe von «[http://raspberry](http://raspberry/)» ansprechbar sein.
«https://» gibt es hier nicht. Da dies aber mittlerweile und aus guten Gründen für öffentliche
Webseiten Standard ist, bitte unbedingt «[http://raspberry](http://raspberry/)» ausschreiben. Eventuell produziert der
Browser eine Warnung, das ist ok und kann bestätigt werden. Es sollte dann das folgende Bild zu
sehen sein, das bestätigt, dass der Raspberry erreichbar ist und funktioniert:
3
<https://sourceforge.net/projects/win32diskimager/>


Nun kann das Admin-Panel aufgerufen werden und die Erstkonfiguration des Teslalogger kann
beginnen.
Bitte «<http://raspberry/admin>[ ](http://raspberry/admin)aufrufen, es erscheint das Admin-Panel:
Hier sind zwei wichtige Sachen zu sehen:
\1. Die Frage zum Anonymen Teilen von Daten im oberen Drittel


\2. Der Hinweis, dass ein Update verfügbar ist (hier: 1.47.2.0, die Bilder wurden mit 1.46.0.0
gemacht). Der Hinweis erscheint natürlich nur, wenn das Image nicht 100% aktuell ist – dies dürfte
aber der Regelfall sein.
Wir empfehlen das Anonyme Teilen von Daten zu aktivieren, da hier die ganze Teslalogger-
Community von diesen statistischen Daten profitiert. Die entsprechenden Daten sind im Menü unter
«Fleet Statistic» einzusehen. Die Einstellung kann jederzeit im Menü «Settings» geändert werden
und wird auf Seite [19](#br19), «[Fleet](#br19)[ ](#br19)[Statistic](#br19)» beschrieben
Updates sind wichtig, bringen neue Funktionen und beheben in vielen Fällen auch bekannte Fehler.
Wir empfehlen, die Updates wie hier im Bild zu sehen immer zu aktivieren. Das kann ebenfalls
über das Menü «Settings» automatisiert werden und wir empfehlen, vor jeder anderen
Einrichtungsaktivität zuerst den Update durchzuführen. Dies kann über das Menü oben mit Druck
auf «Update» eingeleitet werden. Das Raspberry wird einen Neustart bestätigen und steht nach 2-3
Minuten danach wieder zur Verfügung (einfach nach 2-3 Minuten über Taste F5 das Bild erneuern
und schauen, dass die Teslalogger-Version jetzt auf dem angekündigten Stand ist.
Wenn dieses Bild erscheint, ist der Raspberry noch nicht vollständig neu gestartet, es braucht dann
noch einen Augenblick länger:
Wenn das Image auf dem aktuellen Stand ist, gibt es zwei Möglichkeiten. Die Ersteinrichtung für
diejenigen, die Teslalogger bisher noch nicht benutzt haben und die Einrichtung dieser Instanz für
Leute, die von einem alten Image/einer alten Installation beispielsweise von einem Raspberry 3 auf
einen Raspberry 4 umsteigen.

**2.3 Teslalogger Ersteinrichtung (ohne bestehendes Backup einer anderen Installation)**
Dieses Kapitel beschreibt die Ersteinrichtung. Anwender, die ein vorhandenes Backup einer alten
Installation benutzen, können dieses Kapitel überspringen.

Unabhängig davon, ob die Zustimmung zum Teilen anonymer Daten bestätigt wurde oder nicht
wird nun das Menü «Settings» aufgerufen. Hier können Sprache und einige andere Dinge gewählt
werden, die typischen Einstellungen sind schon vorausgewählt. Vorsichtige Benutzer können hier
«Automatische Updates» auf «Stable» stellen. Wer anstelle von PS lieber kW sehen möchte,
Fahrenheit statt Grad Celsius oder Meilen anstelle Kilometer: das ist die Stelle, wo das passiert.
Diese Möglichkeiten werden im Details auf Seite [18](#br18), «[Settings](#br18)» beschrieben
Für die nächsten Schritte braucht es die Email-Adresse des persönlichen Tesla-Kontos, das Passwort
dazu und für diejenigen, die die Multifaktorauthentifizierung (MFA) benutzen, den entsprechenden
Code-Generator. Bitte auf «Zugangsdaten» drücken:
Im nun erscheinendem Bild auf «New Car» drücken und es erscheint der folgende Dialog:
Bitte wie angefordert die Email-Adresse eingeben und das Passwort (unsichtbar) zweimal eingeben.
Wer mehr als nur einen Tesla in seinem Tesla-Konto verwaltet, muss noch die Nummer, beginnend
bei Null(!) angeben, für den zweiten Tesla hier also eine «1» angeben. Am Ende noch für die
Berechnung der Kosten anwählen, ob aktuell Free Supercharging gewährt ist. Dies kann jederzeit
geändert werden (beispielsweise, wenn die 1500km kostenloses Laden abgelaufen sind). Dann auf
«Speichern» drücken.

Wer die Multifaktor Authentifizierung aktiv hat, wird nun dazu aufgefordert, den zweiten Faktor
einzugeben:
Sobald die 6 Ziffern eingegeben wurden wird angezeigt, von welchem Gerät diese Daten stammen
und die Korrektheit wird bestätigt (natürlich nur, wenn der Code richtig war):

Mit dem Druck auf «ok» erscheint wieder das Admin-Panel, dieses mal sind aber weitere Daten
ausgefüllt. Damit ist die Ersteinrichtung abgeschlossen.

**2.4 Teslalogger Einrichtung mit bestehendem Backup**
Wichtig: Der bisherige Teslalogger, von dem das Backup benutzt wird muss zu diesem Zeitpunkt
abgeschaltet sein!
Das bestehende Backup wird in zwei Schritten durchgeführt. Die Reihenfolge ist nicht wirklich
kritisch, aber wir empfehlen, zuerst die persönliche Geofence-Datei zurückzuspielen. Zu diesem
Zweck im Windows Datei-Explorer oben in die Adressleiste klicken und dort «[\\teslalogger\](file://teslalogger/teslalogger)
[teslalogger](file://teslalogger/teslalogger)» eingeben. Es wird dann eine Aufforderung erscheinen, dass Passwort einzugeben. Dies
ist «pi» und «teslalogger». Dort gibt es bereits eine Datei «geofence.csv» und eine alte, nicht
persönliche Datei «geofence-private.csv», die gegen die eigene Datei ausgetauscht werden muss.
Liegt mit der Datensicherung eine Datei mit der Erweiterung «.gz» vor, muss diese zuerst,
beispielsweise mit 7Zip, ausgepackt werden – an dieser Stelle darf nur eine Klartextdatei mit der
Erweiterung .csv abgelegt werden.
Danach wird im Admin-Panel im Menü «Extras» der Punkt «Restore» angewählt. Über die
Schaltfläche «Datei auswählen» wird die vorhandene Datensicherung ausgewählt, die üblicherweise
«mysqldump….gz» heisst. An dieser Stelle darf die Datei mit dieser Dateierweiterung angegeben
werden.

Nach dem Klick auf «Restore» dauert es einige Minuten, bis die Erfolgmeldung erscheint:
An dieser Stelle den «Zurück»-Knopf des Webbrowsers bemühen und danach im Menü auf
«Restart» drücken. Danach braucht es ein paar Minuten und der neue Teslalogger arbeiten mit den
übertragenen Daten.
Sollte Teslalogger im Heimnetz nicht als «raspberry» erreichbar sein (dies lässt sich mit einigen
Routern entsprechend konfigurieren), muss nun noch im Menü «settings» der entsprechende Name
konfiguriert werden für Admin Panel und Grafana. Beim Author dieser Zeilen sieht das so aus:

**2.5 Smart-Home mit MQTT**
Sollen die Daten vom Teslalogger an einem MQTT-Broker geschickt werden, dann geht dies wie
folgt:
\1. Im Windows Datei-Explorer [\\raspberry\teslalogger](file://raspberry/teslalogger)[ ](file://raspberry/teslalogger)anwählen. Das Benutzerkonto ist «pi»,
dass Passwort ist «teslalogger»
\2. Die Datei «MQTTClient.exe.config» mit einem Texteditor, beispielsweise Notepad öffnen
\3. Den folgenden Abschnitt anpassen:
<setting name="MQTTHost" serializeAs="String">
`    `<value></value>
</setting>
<setting name="Topic" serializeAs="String">
`    `<value></value>
</setting>
<setting name="Name" serializeAs="String">
`    `<value></value>
</setting>
<setting name="Password" serializeAs="String">
`    `<value></value>
</setting>
Name und Password werden nur benötigt, wenn der MQTT-Broker das auch wirklich
braucht.
\4. Die Datei speichern
\5. Mit dem Webbrowser auf der Admin-Seite den Teslalogger einmal neu starten:
<http://raspberry/admin>[ ](http://raspberry/admin)→ «Restart»
\6. In der Logdatei sollte dann etwas in der Art wie hier zu sehen sein:
17.02.2019 23:49:28 : MQTT : MqttClient Version: 1.2.0.0
17.02.2019 23:49:29 : MQTT : Connecting without credentials: 192.168.1.23
17.02.2019 23:49:29 : MQTT : Connected! 

**2.6 Mehrere Fahrzeuge im gleichen Tesla-Account**
Über den Dialog «Zugangsdaten» im Menü «Settings» lassen sich mehrere Fahrzeuge eintragen.
Gibt es für jedes Fahrzeug eine eigene Email-Adresse, ist nichts weiter zu beachten. Werden über
eine Email-Adresse mehrere Fahrzeuge verwaltet, ist die Nummer, beginnend bei Null (!) für jedes
Fahrzeug über eine eigene Zeile einzurichten.
Kommt die Zweifaktor-Authentifizierung zum Einsatz, wird für jede Zeile jeweils der Code des
definierten Authenticators ebenfalls abgefragt.

**3. Das Admin-Interface**
Das Admin-Interface wird zur Verwaltung von Teslalogger benutzt. Hier können verschiedene
Dinge aktiviert oder abgerufen werden, beispielsweise kann die Logdatei abgerufen werden, ein
Update ausgelöst oder ein Neustart eingeleitet werden. Ausserdem sind einige statistische Daten zu
sehen.
Das Admin-Interface ist unter der Adresse «http://raspberry/admin» zu erreichen.
Es braucht dafür kein Passwort und es ist (abhängig natürlich vom eigenen Fahrzeug) folgendes
Bild zu sehen:
Die sichtbaren Angaben sollten selbsterklärend sein. Besteht derzeit keine Verbindung zum
Fahrzeug, so wird im oberen Block als Status «Schlafen» angezeigt. Des weiteren gibt es noch
«Online» (dann besteht eine Verbindung zum Fahrzeug) und «Laden» (zusammen mit einigen
Parametern zum aktuellen Ladevorgang). Ausserdem wird der aktuelle Software-Stand des
Fahrzeuges sowie die aktuelle Version des Teslalogger angezeigt.
Im unteren Block gibt es einige statistische Informationen zur letzten Fahrt aus.
Im rechten Block wird eine zoombare Karte angezeigt mit der letzten bekannten Position des
Fahrzeugs. Der Zoomlevel kann unter «Settings» eingestellt werden. Die Zoomstufe im Bild
entspricht «13»

Im Folgenden werden die einzelnen Menüpunkte vom oberen Bildschirmrand erklärt.

**3.1 Die Auswahl des Fahrzeugs**
Im Bild ist links oben «NCC-1031» zu sehen. Das ist der Name des Tesla Model 3 des Authors
dieses Handbuches. Werden in Teslalogger mehrere Fahrzeuge verwaltet, kann hier das Fahrzeug
ausgewählt werden, für dass die Daten angezeigt werden.

**3.2 Logfile**
Das Logfile gibt die internen Meldungen des Teslaloggers an. Diese können zur Fehlersuche
benutzt werden. Im Falle von Problemen kann es sein, dass die helfenden Menschen darum bitten,
Einträge aus dem Logfile zur Analyse zuzuschicken oder nach bestimmten Einträgen zu suchen.
Um zum Admin-Interface zurück zu kommen bitte einfach wieder auf den Fahrzeugnamen drücken
oder die «Zurück»-Funktion im Browser benutzen, dies ist meist auch mit «Alt-Taste»+»Pfeil
links» möglich.

**3.3 Restart**
Hiermit kann manuell ein Neustart des Teslaloggers eingeleitet werden. Dies ist manuell nötig,
wenn Änderungen an den Konfigurationsdateien gemacht wurden und wird dort erwähnt.

**3.4 Update**
Wie Updates für Teslalogger automatisch installiert werden sollen kann in den «Settings» unter
«Automatische Updates» eingestellt werden. Üblicherweise wird einmal am Tag nach einem Update
gesucht, wenn die Einstellung nicht «None» ist. Um manuell die Suche auszulösen, beispielsweise
nach der Änderung von «Stable» auf «All», kann der Menüeintrag «Update» benutzt werden. Der
Teslalogger wird dann neu starten, es wird eine entsprechende Meldung angezeigt. Der Update
braucht im Regelfall weniger als 5min.

**3.5 Extras - Backup**
Hiermit kann manuell die Erstellung einer Sicherungsdatei der Datenbank erstellt werden.
Grundsätzlich passiert dies auch automatisch einmal pro Tag und ist daher nur sinnvoll vor
Änderungen. Wurde die Sicherungsdatei erstellt, wird «ok» angezeigt.
Die Sicherungsdatei kann über den Windows-Explorer abgeholt werden, in dem in der Adresszeile
oben «[\\raspberry\teslalogger\backup](file://raspberry/teslalogger/backup)» eingegeben wird.
Wir empfehlen, die aktuelle Datei dort von Zeit zu Zeit an einem anderen Ort, beispielsweise auf
dem persönlichen PC abzulegen und alte Dateien, um Platz zu sparen dort zu löschen. Da mit der
Zeit immer mehr Daten anfallen wird auch die Datensicherung immer grösser!
Zusätzlich wird empfohlen, eine Kopie der folgenden Datei anzufertigen, die eine Verzeichnisebene
weiter oben zu finden sind:
•
geofence-private.csv
Da diese nur bewusst geändert werden, beispielsweise durch die Benutzung von «Settings», ist eine
neue Kopie nur dann notwendig, wenn es eine Änderung gegeben hat.
Wir empfehlen nicht, Änderungen an der Datei «geofence.csv» vorzunehmen, da diese vom Projekt
verwaltet und bei Aktualisierungen überschrieben werden kann. Deshalb ist es auch nicht
notwendig, diese Datei zu sichern oder zurück zu spielen. Die Datei «geofence-private.csv» muss
nicht manuell bearbeitet werden,hier für gibt es einen Dialog (siehe weiter unten)

**3.6 Extras – Restore**
Der Restore muss nur benutzt werden, wenn existierende Daten von einem früheren Backup auf
eine neue oder reparierte Installation eingespielt werden müssen. Alle eventuell vorhanden Daten
werden gelöscht. Es wird eine alte Backup Datei angegeben, danach werden die Daten
zurückgespielt. Dies kann einige Minuten dauern und darf nicht unterbrochen werden!
Die Details hierzu sind in der Ersteinrichtung auf Seite [9](#br9), «[Teslalogger](#br9)[ ](#br9)[Einrichtung](#br9)[ ](#br9)[mit](#br9)
[bestehendem](#br9)[ ](#br9)[Backup](#br9)» beschrieben.

**3.7 Extras – Geofence**
In den späteren Auswertungen wäre es schön, wenn häufiger angefahrene Ziele wie das eigene
Heim oder die Arbeitsstelle nicht mit dem Standardeintrag Ort/Strasse/Hausnummer erscheinen
sondern mit einem individuellen Namen. Hierzu können im Menü unter «Extras»-«Geofence»
persönliche Einträge verwaltet werden.
Wichtig: Neue Einträge werden über das später beschriebene Grafana-Dashboard «Trips» erzeugt.
In diesem Dialog werden vorhandene Einträge verwaltet, weil sich neben dem Namen zu einem Ort
auch noch einige andere Dinge einstellen lassen, wie im Folgenden beschrieben wird. Wird die
Funktion aufgerufen, kommt eine Liste der aktuell bekannten Einträge und eine Karte, die mit
grünen Nadeln die Position der persönlichen Einträge enthält und mit blauen Nadeln die vom
Projekt verwalteten Einträge wie Supercharger, Service Centers etc.:

Ein Klick auf das Lupensymbol zoomt die Karte zu der jeweiligen Adresse, ein Klick auf das
Bleistift-Symbol startet den Bearbeiten-Dialog für den Eintrag:
\1. Bezeichnung. Dies ist der Name des Eintrags
\2. Radius. Dies ist der Radius, innerhalb dem der Ort des Fahrzeuges als zu diesem Eintrag
zugehörig erkannt wird. Ist der Wert zu klein, können Ungenauigkeiten des GPS dazu
führen, dass der Standort nicht dem Eintrag zugeordnet werden kann. Die Grösse wird durch
den blauen Kreis angedeutet
\3. Die «Special Flags» erzeugen in den Listen ein Symbol und haben aktuell keine weitere
Funktion
\4. «Copy Charging Costs» wird angewählt, wenn eine einmal eingetragene Kostenstruktur für
eine Ladung an diesem Ort für alle Folgeladungen am gleichen Ort übernommen werden
sollen
\5. «Set Charge Limit» setzt beim Start der Ladung an diesem Ort den entsprechenden Wert.
Dies wird hier eine vorher manuell eingegebene Angabe via App oder im Fahrzeug ersetzen.
Ist hier ein Wert eingetragen, muss er via App oder im Fahrzeug **nach dem Start der**
**Ladung** geändert werden
\6. «Open Charge Port» öffnet den Ladeanschluss an diesem Ort, wenn der Ganghebel in die
ausgewählte Position bewegt wird. Wenn immer an diesem Ort geladen werden soll, ist dies
eine Erleichterung
\7. High Frequency Logging. Dies dient dazu, während des Ladens an diesem Ort eine höhere
Frequenz für den Protokollvorgang zu aktivieren, um präzise Ladekurven generieren zu
können
\8. Sentry Mode. Damit kann aktiviert werden, dass der Sentry-Mode (Wächter) an diesem Ort
aktiviert ist, wenn der Ganghebel in eine ausgewählte Position bewegt wird
\9. Turn HVAC off. Schaltet an diesem Ort die Klimaanlage aus
\10. No Sleep. Verhindert, dass an diesem Ort das Auto schlafen geht
Am Ende noch die Schaltfläche «Save» drücken, um die Änderungen abzuspeichern.
Neue Einträge können u.A. über das Grafana Dashboard «Trip» durchgeführt werden. Dabei muss
nur auf eine Adresse geklickt werden (es erscheint, wenn die Maus über dem Eintrag schwebt, die
Meldung «Add Geofence»
Ein Klick auf die Adresse öffnet den oben gezeigten Dialog zur Bearbeitung eines Ortes

**3.8 Extras – Dashboard**
Das Dashboard ist die ideale Funktion, um auf einem Tablet oder sonstigem zentralen Bildschirm
daheim anzuzeigen, wie der aktuelle Ladevorgang oder der sonstige Status des Fahrzeuges gerade
ist. Es kann ein eigenes Bild hinterlegt werden sowie die Wetterprognose eingeblendet werden.
Hinweis: bei den meisten Browsern kann unter Windows der Vollbildmodus mit F11 aktiviert oder
beendet werden. Hier im Beispiel ist es nicht aktiv, der Rahmen und sonstige Bedienelemente sind
noch sichtbar.

Um das Bild zu hinterlegen, muss beispielsweise mit dem Windows Datei-Explorer im Verzeichnis
«\\raspberry\teslalogger-web\admin\wallpapers\1» eine JPG-Datei abgelegt werden.:
Wie schon an anderer Stelle erwähnt, ist, falls gefragt wird, der Benutzername «pi» und das
Passwort «teslalogger».
Weitere Möglichkeiten, das Dashboard anzupassen, sind hier aktuell zur jeweils gültigen Version
von Teslalogger dokumentiert:
«<https://github.com/bassmaster187/TeslaLogger/blob/master/dashboard.md>»

**3.9 Extras - Suspend**
Dass für eine manuelle zu definierende Zeit keine Kommunikation von Teslalogger zum Fahrzeug
stattfinden, kann diese Funktion genutzt werden. Es wird dann die folgende Information angezeigt:

**3.10 Extras - Wakeup**
Die Funktion ist nur zu sehen, wenn «Suspend» aktiv ist.
Wurde Teslalogger über die Funktion «Sleep» zum Schlafen gelegt, kann er mit dieser Funktion
oder mit der Schaltfläche «Teslalogger starten» wieder aufgeweckt werden.

**3.11 Settings**
Dies ist der zentrale Ort, um Teslalogger zu konfigurieren. In diesem Dialog wurden schon bei der
initialen Einrichtungen die Zugangsdaten erfasst. Es gibt aber noch eine Reihe von weiteren
Möglichkeiten:
Neben den offensichtlichen Dingen wie «Zugangsdaten», «Sprache», «Leistung», «Temperatur»
und «Längenmaß» gibt es Folgendes:
Reichweite. Hier wird … tbd
Daten anonym teilen. Wird der Haken hier gesetzt, können auf «[https://teslalogger.de](https://teslalogger.de/)»
diverse Informationen aller Teslaloggerbenutzer gesammelt werden. Diese Daten sind im
Menü unter «Fleet Statistic» abrufbar und werden weiter unten beschrieben. Es steht jedem
frei, seine Daten nicht zu teilen, aber wir rufen dazu auf, das Teilen zu aktivieren, da hiervon
viele Teslafahrer profitieren

Automatische Updates: Hier kann eingestellt werden, ob alle (potentiell auch instabile
Betaversionen) Updates, gar keine Updates oder nur stabile Hauptversionen installiert
werden
Schlafen. Dies ist eine automatisierte Funktion, die sich zur definierten Uhrzeit genauso
verhält wie die Menüfunktionen «Suspend» und «Wakeup»
Wenn die persönliche Installation nicht unter «raspberry» zu erreichen ist (siehe dazu auch
Seite [30](#br30), «[Mit](#br30)[ ](#br30)[Namen](#br30)[ ](#br30)[statt](#br30)[ ](#br30)[IP](#br30)[ ](#br30)[Adresse](#br30)[ ](#br30)[ansprechen](#br30)»). können die beiden URLs für das
Admin-Panel und Grafana hier definiert werden
Über den Zoom Level wird die Grösse der Karte für den aktuellen Standort des Fahrzeuges
im Admin-Panel eingestellt. Je grösser dieser Wert ist, desto mehr Details gibt es. «1»
entspricht der gesamten Weltkarte
Main Car definiert bei mehreren definierten Fahrzeugen, welches davon immer zuerst
angezeigt werden soll, wenn das Admin-Panel oder Grafana gestarten werden

**3.12 Fleet Statistic**
Die Flottenstatistik basiert auf den anonym geteilten Daten (siehe: «Settings») und kann teilweise
auch die eigenen Daten in Relation zur Flotte aller Teslaloggerbenutzer (die, die die Daten anonym
teilen) setzen.
Die Statistiken sind meist selbsterklärend und rufen die Daten über die Webseite
«[https://teslalogger.de](https://teslalogger.de/)» ab, die Daten liegen also nicht in der eigenen Installation lokal vor und es
wird daher dafür eine Internetverbindung benötigt.
Die vermutlich wichtigste Funktion ist «My Degradation», die die aktuelle Akkualterung in den
Vergleich zur Flotte setzt.
Dabei ist die blaue Linie die geglättete Degradation aller Nutzer, während die rote Linie die
persönlichen Daten darstellt.
Die zweite wichtige Funktion ist der aktuelle Rollout-Stand der Fahrzeugsoftware «Firmware
Tracker»:
Weitere Charts zeigen die üblichen Reichweiten, Ladekurven in Relation zum Fahrzeugmodell und
Firmware an und vieles mehr.

**4. Die Auswertungen**
Das grafische Auswertungssystem des Teslaloggers basiert auf «Grafana». Unabhängig von dem
Namen oder der IP-Adresse des Teslaloggers ist der Port immer «3000», die Adresse lautet also
«[http://raspberry:3000](http://raspberry:3000/)». Die Auswertungen können auch über das Admin-Panel über das Menü
«Dashboards» aufgerufen werden.
Der Browser wird danach in etwa das folgende Bild zeigen:
Der Hinweis «Nicht sicher» kann ignoriert werden, da wir kein Verschlüsselungszertifikat für einen
nur in dem eigenen Netzwerk laufenden Raspberry haben. Wen dies stört kann dies nachrüsten, dies
wird aber nicht in diesem Handbuch beschrieben.
Es kann sein, dass beim Erstaufruf eine Warnung kommt, dass diese Webseite nicht sicher sei. In
diesem Fall muss eine Ausnahme definiert werden. Wie dies geschieht und wie diese Warnung
aussieht hängt vom benutzten Browser ab.

Nach der Installation und wenn dies nicht später geändert wird lautet der Benutzername «admin»
und das Passwort «teslalogger», und es erscheint nach erfolgreicher Anmeldung die Übersichtseite
mit dem «Verbrauch der letzten 3 Stunden»:

**4.1 Standardauswertungen abrufen**
Im vorherigen Abschnitt haben wir uns an Grafana angemeldet und sind auf einer vermutlich leeren
Seite mit dem Titel «Verbrauch» gelandet.
Neben dem Namen ist ein kleiner Pfeil zu sehen, wenn hier draufgedrückt wird, erscheint eine Liste
mit allen verfügbaren Auswertungen, die der Teslalogger zur Verfügung stellt

**4.2 Favoriten definieren**
Um die für den eigenen Bedarf wichtigen Auswertungen gleich oben in der Liste zu haben muss nur
die Auswertung angewählt werden und danach das Stern-Symbol angewählt werden. Nach der
Definition einiger Auswertungen kann die Liste dann wie folgt aussehen (die Favoriten sind oben
als «Starred» zu sehen):

**4.3 Mit den Auswertungen arbeiten**
Die wesentliche Funktion zum Einstellungen der Auswertungen ist es, den Zeitpunkt zu definieren.
Hierfür gibt es zwei Varianten.
Die erste Variante ist es, den Zeitraum manuell zu definieren. Hier auf den Zeitbereich klicken, der
hier im Beispiel recht oben mit «Last 3 hours» zu sehen ist:
Es gibt nun die Möglichkeit, eine der verschiedenen vorbereiteten Auswahlmöglichkeiten wie
«Today» oder «Yesterday» zu wählen oder aber im Bereich «Custom range» dies beliebig selber zu
definieren:

Sind nun Daten angezeigt, kann diese Auswahl mit der Maus verfeinert werden. Im Beispiel unten
sind zwei kurze Trips aus der Auswertung «Verbrauch» zu sehen. Durch Markieren des Bereiches
vom 2. Trip kann dieser im Detail angezeigt und so quasi gezoomt werden, indem einfach der
Anfang irgendwo im grafischen Bereich angeklickt wird und dann mit gedrückter Maustaste zum
Ende gefahren wird:
Auf einem Tablet oder Smartphone ist mir dieser «Zoom» noch nicht geglückt. Wer dafür eine
Lösung kennt darf sich gerne melden.

**4.4 Verfügbare Auswertungen**
«Verbrauch»: Daten zu einem Trip, dazu gehören Reichweite, Position, abgerufene Leistung,
Geschwindigkeit und einige andere Daten

«Trips»: mit Uhrzeit, Start und Ziel sowie einigen weiteren Angaben wie Durchschnittsverbrauch
und Strombedarf. Mit Klick auf die Startzeit landet man in der oberen Auswertung «Verbrauch»:
«Laden»: zeigt den Verlauf eines Ladevorgangs an. Hiermit lässt sich sehr gut die Änderung an der
Ladeleistung im Vergleich zum Batteriestand ermitteln:
«Ladehistorie»: zeigt alle Ladevorgänge an mit Ort, Uhrzeit, der geladenen Leistung sowie weiteren
Angaben. Ein Klick auf das Startdatum führt zur Auswertung «Laden».
Bei Klick auf einen Eintrag in der Spalte «Cost» können die Kosten für den Ladevorgang erfasst
werden. Der angezeigte Wert «set» bedeutet dabei, dass noch keine Kosten definiert sind. Es wird
dann der folgende Dialog angezeigt:

Sind keine Kosten entstanden, muss im Feld «Kosten pro Ladung» der Eintrag «0» erfolgen.
Ansonsten sind die anderen Felder, sofern relevant auszufüllen. Unrelevante Felder bleiben einfach
leer (kein Wert).
Ladevorgänge an Tesla Supercharger können über die Schaltfläche «Share» ganz rechts publiziert
und geteilt werden. Es wird die jeweilige Ladekurve des Vorgangs angezeigt.
«Ladestatistik»: Hier werden die verschiedenen Orte und deren Häufigkeit zusammen mit ein paar
weiteren Angaben gezeigt, an denen geladen wurde:

«Akku Trips»: Hier werden die Strecken aufgezeigt, die zwischen zwei Ladevorgängen absolviert
wurden. Ein Klick auf das Startdatum führt wieder zur ersten Auswertung «Verbrauch» mit
entsprechend eingestellten Zeitraum.
«Degradation»: Hier wird im Zusammenhang mit den bisher gefahrenden Kilometern die
Reichweite angezeigt und damit der Verlust an möglicher Reichweite über die Zeit:

«Vampir Drain»: Unter «Vampir Drain» wird der Ladeverlust im Stillstand, also zwischen zwei
Trips bezeichnet.
«Visited»: Besuchte Orte in einem definierten Zeitraum

«Km Stand»: Der zeitliche Ablauf der gefahrenen Kilometer:
«Trip Monatsstatistik»

**5. Optimierung mit einer Fritz!Box**
Die in vielen deutschsprachigen Ländern weit verbreitete Fritz!Box bietet ein paar schöne
Funktionen, um die Benutzung des Teslaloggers zu vereinfachen. Ähnliches geht auch mit anderen
Heimroutern und mit professioneller Infrastruktur sowieso.

**5.1 Fixe IP**
Der Teslalogger ist wie ein Server im Internet, aber zuhause. Server im Internet haben im
Normalfall eine fixe IP-Adresse, so dass sie besser und einfacher erreichbar sind und deshalb ist es
sinnvoll, dem Gerät auf dem der Teslalogger läuft ebenfalls eine feste IP-Adresse zuhause zu
vergeben. Dies ist im nächsten Abschnitt in Punkt 6 beschrieben.

**5.2 Mit Namen statt IP Adresse ansprechen**
Kein System im Internet wird direkt mit seiner IP Adresse angesprochen, sondern mit seinem
Namen, beispielsweise [https://teslalogger.de](https://teslalogger.de/). Genau das geht auch mit dem Teslalogger zuhause. Im
Beispiel vergeben wir dem Raspberry einen Namen, so dass er danach via
https://teslalogger.fritz.box zuhause angesprochen werden kann.
Das Folgende bezieht sich auf eine Fritz!Box mit Firmware Version 7.x, bei älteren Versionen ist es
aber prinzipiell ähnlich.
\1. Mit dem Webbrowser an der Fritz!Box anmelden. Das geht normalerweise mit
[https://fritz.box](https://fritz.box/)
\2. Sicherstellen, dass die «Erweiterte Ansicht» aktiv ist, dazu rechts oben auf den Fritz!Box
Benutzernamen drücken, im Normalfall ist das «Admin». Dort den Schalter für «Erweiterte
Ansicht» aktivieren, falls dies noch nicht passiert ist.
\3. «Heimnetz», dann «Netzwerk» anwählen
\4. In der Liste den Raspberry suchen und in der gleichen Zeile rechts die Bearbeiten-
Schaltfläche anwählen
\5. Den Namen anpassen, im Beispiel haben wir «teslalogger» benutzt
\6. Ausserdem wird hier die IP-Adresse definiert. Diese kann prinzipiell so bleiben wie sie ist,
aber der Haken bei «Diesem Netzwerkgerät immer die gleiche IPv4-Adresse zuweisen»
wird angewählt
\7. Unten recht auf «ok» drücken. Der Raspberry ist ab sofort unter dem neuen Namen
ansprechbar
