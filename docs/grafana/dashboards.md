---
sidebar_position: 3
---

# Dashboards

### Verbrauch 

Daten zu einem Trip, dazu gehören Reichweite, Position, abgerufene Leistung, Geschwindigkeit und einige andere Daten

![BILD](/img/grafana-08.png)

### Trips 

Trips mit Uhrzeit, Start und Ziel sowie einigen weiteren Angaben wie Durchschnittsverbrauch und Strombedarf. Mit Klick auf die Startzeit landet man in der oberen Auswertung «Verbrauch»:

![BILD](/img/grafana-09.png)

### Laden

Zeigt den Verlauf eines Ladevorgangs an. Hiermit lässt sich sehr gut die Änderung an der Ladeleistung im Vergleich zum Batteriestand ermitteln:

![BILD](/img/grafana-10.png)

### Ladehistorie

Zeigt alle Ladevorgänge an mit Ort, Uhrzeit, der geladenen Leistung sowie weiteren Angaben. Ein Klick auf das Startdatum führt zur Auswertung «Laden».

![BILD](/img/grafana-11.png)

Bei Klick auf einen Eintrag in der Spalte «Cost» können die Kosten für den Ladevorgang erfasst werden. Der angezeigte Wert «set» bedeutet dabei, dass noch keine Kosten definiert sind. Es wird dann der folgende Dialog angezeigt:

![BILD](/img/grafana-12.png)

Sind keine Kosten entstanden, muss im Feld «Kosten pro Ladung» der Eintrag «0» erfolgen. Ansonsten sind die anderen Felder, sofern relevant auszufüllen. Unrelevante Felder bleiben einfach leer (kein Wert).
Ladevorgänge an Tesla Supercharger können über die Schaltfläche «Share» ganz rechts publiziert und geteilt werden. Es wird die jeweilige Ladekurve des Vorgangs angezeigt.

### Ladestatistik 

Hier werden die verschiedenen Orte und deren Häufigkeit zusammen mit ein paar weiteren Angaben gezeigt, an denen geladen wurde:

![BILD](/img/grafana-13.png)

### Akku Trips

Hier werden die Strecken aufgezeigt, die zwischen zwei Ladevorgängen absolviert wurden. Ein Klick auf das Startdatum führt wieder zur ersten Auswertung «Verbrauch» mit entsprechend eingestellten Zeitraum.

![BILD](/img/grafana-14.png)

### Degradation

Hier wird im Zusammenhang mit den bisher gefahrenden Kilometern die Reichweite angezeigt und damit der Verlust an möglicher Reichweite über die Zeit:

![BILD](/img/grafana-15.png)

### Vampir Drain

Unter «Vampir Drain» wird der Ladeverlust im Stillstand, also zwischen zwei Trips bezeichnet.

![BILD](/img/grafana-16.png)

### Visited

Besuchte Orte in einem definierten Zeitraum

![BILD](/img/grafana-17.png)

### Km Stand

Der zeitliche Ablauf der gefahrenen Kilometer: 

![BILD](/img/grafana-18.png)

### Trip Monatsstatistik

![BILD](/img/grafana-19.png)