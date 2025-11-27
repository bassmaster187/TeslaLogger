---
sidebar_position: 6
---
# TeslaLogger-Backups automatisch zu Google Drive hochladen

- Per SSH mit Teslalogger verbinden
- Falls nicht vorhanden *rclone* installieren: `sudo apt-get install -y rclone`
- `sudo rclone config` ausführen (sudo nötig, da Teslalogger auch als sudo läuft; rclone-Konfiguration ist benutzerabhängig)
- Verbindung erstellen (z. B.: "tl_backup") und **ENTER** drücken
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive1.png)
- Listennummer für Google Drive finden, eingeben und **ENTER**
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive2.png)
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive3.png)
- Dieses HowTo für *client_id* und *client_secret* verwenden:
  https://rclone.org/drive/#making-your-own-client-id
- **1** und **ENTER** für *scope*, **ENTER** für *root_folder_id*
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive5.png)
- **ENTER** bei *service_account_file*, **n** für *Use auto config*
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive6.png)
- Angezeigten Link im Browser öffnen. Mit Google-Konto anmelden und Zugriff erlauben:
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive7.png)
- Schlüssel wird im Browser angezeigt, mit Copy-Button kopieren, in Konsole einfügen, **ENTER**, nochmals **ENTER**
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive8.png)
- Einstellungsdatei mit Token erscheint, **ENTER**
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive9.png)
- Dann **q** zum Beenden
- nano öffnen: `nano /etc/teslalogger/my-backup.sh` und einfügen:
```
#!/bin/bash

/usr/bin/rclone copy --update --verbose --transfers 3 --contimeout 60s --timeout 300s --retries 3 --low-level-retries 10 --stats 1s "/etc/teslalogger/backup" "tl_backup:TeslaLoggerBackup"
```
Mit diesem Befehl werden alle Backup-Dateien über die zuvor erstellte *tl_backup*-Verbindung in den Ordner "TeslaLoggerBackup" kopiert. Bei Bedarf anpassen.
- Datei speichern: **CTRL+X**, **y**, **ENTER**
- Ausführbar machen: `chmod +x /etc/teslalogger/my-backup.sh`
- Testen: `sudo /etc/teslalogger/my-backup.sh`
- Bei vielen Backups dauert der erste Lauf länger. Erfolg sieht so aus:
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive10.png)
- Erneut nano öffnen: `nano /etc/teslalogger/my-backup.sh` und **--verbose** entfernen, speichern (**CTRL+X**, **y**, **ENTER**)
- Fertig. Nach jedem neuen Backup führt Teslalogger automatisch "my-backup.sh" aus.