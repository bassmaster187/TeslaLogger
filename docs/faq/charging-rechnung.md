# Supercharging-Rechnungen herunterladen
Nach einer Supercharger-Ladung schaut Teslalogger in regelmäßigen Abständen nach, ob Tesla die Rechnung für diesen Ladevorgang bereits erstellt hat, und lädt sie herunter, sobald sie verfügbar ist. Zusätzlich lädt Teslalogger einmal am Tag alle Rechnungen, die noch nicht lokal vorhanden sind, herunter.

Mit dem Windows-Dateiexplorer sind diese an folgendem Ort zu finden:

`\\raspberry\teslalogger\tesla_invoices`

Bei der Abfrage sind als Benutzername «pi» und als Passwort «teslalogger» einzugeben.

Wenn, wie an anderer Stelle beschrieben, der Teslalogger unter einem anderen Namen anstelle «raspberry» betrieben wird, ist der geänderte Name entsprechend einzusetzen.

Bei einer Docker-Installation werden die Rechnungen im Container im Unterverzeichnis `tesla_invoices` neben der Applikation abgelegt (im Container: `/TeslaLogger/bin/tesla_invoices`).

Bitte die (bereits kopierten) Dateien hier nicht löschen, da diese sonst in der nächsten Nacht wieder erneut heruntergeladen werden.
