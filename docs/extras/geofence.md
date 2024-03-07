---
sidebar_position: 4
---
# Geofence

In den späteren Auswertungen wäre es schön, wenn häufiger angefahrene Ziele wie das eigene Heim oder die Arbeitsstelle nicht mit dem Standardeintrag Ort/Strasse/Hausnummer erscheinen sondern mit einem individuellen Namen. Hierzu können im Menü unter «Extras»-«Geofence» persönliche Einträge verwaltet werden.
Wichtig: Neue Einträge werden über das später beschriebene Grafana-Dashboard «Trips» erzeugt. In diesem Dialog werden vorhandene Einträge verwaltet, weil sich neben dem Namen zu einem Ort auch noch einige andere Dinge einstellen lassen, wie im Folgenden beschrieben wird. Wird die Funktion aufgerufen, kommt eine Liste der aktuell bekannten Einträge und eine Karte, die mit grünen Nadeln die Position der persönlichen Einträge enthält und mit blauen Nadeln die vom Projekt verwalteten Einträge wie Supercharger, Service Centers etc.:

![BILD](/img/extras-geofence-01.png)

Ein Klick auf das Lupensymbol zoomt die Karte zu der jeweiligen Adresse, ein Klick auf das Bleistift-Symbol startet den Bearbeiten-Dialog für den Eintrag:

![BILD](/img/extras-geofence-02.png)

1.	Bezeichnung. Dies ist der Name des Eintrags
2.	Radius. Dies ist der Radius, innerhalb dem der Ort des Fahrzeuges als zu diesem Eintrag zugehörig erkannt wird. Ist der Wert zu klein, können Ungenauigkeiten des GPS dazu führen, dass der Standort nicht dem Eintrag zugeordnet werden kann. Die Grösse wird durch den blauen Kreis angedeutet. Im Beispiel ist der Radius sehr gross, weil der gesamte Campingplatz abgedeckt werden soll.
3.	Die Auswahl eines «Typs» erzeugt in den Listen ein Symbol und haben aktuell keine weitere Funktion
4.	«Ladekosten kopieren» wird angewählt, wenn eine einmal eingetragene Kostenstruktur für eine Ladung an diesem Ort für alle Folgeladungen am gleichen Ort übernommen werden sollen
5.	Sollen mehrere Ladungen, die ohne das Fahrzeug zu bewegen an diesem Punkt als eine Ladung behandelt werden sollen, ist der Haken hier zu setzen. Dies empfiehlt sich primär zuhause, wenn die Photovoltaik-Anlage Überschussladen ermöglicht
6.	«Ladelimit setzen» setzt beim Start der Ladung an diesem Ort den entsprechenden Wert. Dies wird hier eine vorher manuell eingegebene Angabe via App oder im Fahrzeug ersetzen. Ist hier ein Wert eingetragen, muss er via App oder im Fahrzeug nach dem Start der Ladung geändert werden. Das setzen des Hakens bei «Setzen bei Ankunft» setzt diesen Wert nicht erst beim Start des Ladevorgangs, sondern bei der Ankunft.
7.	«Setze Ladelimit nach dem Laden» kann aktiviert werden, wenn nur an diesem Ort ein spezielles Limit gelten soll und der übliche Standard danach wieder aktiviert werden soll.
8.	«Ladeklappe öffnen» öffnet den Ladeanschluss an diesem Ort, wenn der Ganghebel in die ausgewählte Position bewegt wird. Wenn immer an diesem Ort geladen werden soll, ist dies eine Erleichterung
9.	«Hochfrequentes loggen» dient dazu, während des Ladens an diesem Ort eine höhere Frequenz für den Protokollvorgang zu aktivieren, um präzise Ladekurven generieren zu können
10.	«Wächter-Modus». Hier an eingestellt werden, ob der Wächter-Modus explizit aktiviert oder deaktiviert werden soll, wenn der Ganghebel in eine ausgewählte Position bewegt wird
11.	«Klimaanlage ausschalten. Schaltet an diesem Ort die Klimaanlage aus
12.	«Kein Ruhezustand. Verhindert, dass an diesem Ort das Auto schlafen geht
Das Textfeld unten enthält die (kryptischen) Zeichenfolgen, die in der persönlichen Geofence-Datei die eingestellten Parameter darstellen.
Am Ende noch die Schaltfläche «Speichern» drücken, um die Änderungen abzuspeichern.
Neue Einträge können u.A. über das Grafana Dashboard «Trip» durchgeführt werden. Dabei muss nur auf eine Adresse geklickt werden (es erscheint, wenn die Maus über dem Eintrag schwebt, die Meldung «Add Geofence»

![BILD](/img/extras-geofence-03.png)