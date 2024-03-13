---
sidebar_position: 1
---
# Auswertungen

Das grafische Auswertungssystem des Teslaloggers basiert auf «Grafana». Unabhängig von dem Namen oder der IP-Adresse des Teslaloggers ist der Port immer «3000», die Adresse lautet also [http://raspberry:3000](http://raspberry:3000). Die Auswertungen können auch über das Admin-Panel über das Menü «Dashboards» aufgerufen werden.
Der Browser wird danach in etwa das folgende Bild zeigen:

![BILD](/img/grafana-01.png)

Der Hinweis «Nicht sicher» kann ignoriert werden, da wir kein Verschlüsselungszertifikat für einen nur in dem eigenen Netzwerk laufenden Raspberry haben. Wen dies stört kann dies nachrüsten.
Es kann sein, dass beim Erstaufruf eine Warnung kommt, dass diese Webseite nicht sicher sei. In diesem Fall muss eine Ausnahme definiert werden. Wie dies geschieht und wie diese Warnung aussieht hängt vom benutzten Browser ab.
Nach der Installation und wenn dies nicht später geändert wird lautet der Benutzername «admin» und das Passwort «teslalogger», und es erscheint nach erfolgreicher Anmeldung die Übersichtseite mit dem «Verbrauch der letzten 3 Stunden»:

![BILD](/img/grafana-02.png)