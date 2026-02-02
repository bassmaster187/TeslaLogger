# Datensicherung

Einmal am Tag führt Teslalogger diverse Datensicherungsoperationen aus.
Dabei werden täglich folgende Dateien erstellt:
- mysqldump\<datum\>.gz
- geofence-private\<datum\>.gz

Einmal im Monat wird die folgende Datei erstellt:
- logfile-\<datum\>.gz

Einmal im Jahr wird die folgenden Dateien erstellt:
- yeardump-\<jahr\>.gz
- yeargeofence-\<jahr\>.gz

Ab Februar 2026 wird den täglichen Dateien das Prefix "DAY-" vorangestellt und diese werden nach 31 Tagen wieder gelöscht, um ein Volllaufen des Speicherplatzes zu verhindern. Damit trotzdem ältere Daten verfügbar sind, wird am 1. jeden Monats anstelle dem Prefix "DAY-" das Prefix "MON-" genutzt. Die MON-Dateien werden nach einem Jahr gelöscht, wenn die "Year"-Daten erstellt wurden, die nie gelöscht werden. So stehen Jahressicherungen ohne Verfalldatum zur Verfügung und Monatssicherungen für ein Jahr, während Tagessicherungen für einen Montag verfügbar sind. Alte Backupdateien ohne Prefix werden nach 180 Tagen gelöscht.

Es wird empfohlen, die Datensicherungsdateien von Zeit zu Zeit vom Teslalogger zu kopieren und an einem anderen Ort abzulegen.
Die Dateien sind mit dem Windows Dateiexplorer an folgendem Ort zu finden:
\\raspberry\teslalogger\backup

Wenn, wie an anderer Stelle beschrieben, der Teslalogger unter einem anderen Namen anstelle "raspberry" betrieben wird, den geänderten Namen entsprechend einsetzen.

Für Spezialisten:
Nachdem der reguläre Datensicherungsprozess durchgeführt wurde, wird, sofern vorhanden, die Script Datei "/etc/teslalogger/my-backup.sh" ausgeführt. In dieser Datei könnten Operationen hinterlegt werden, um die Sicherungsdateien automatisiert beispielsweise auf ein lokales NAS oder einen persönlichen Cloudspeicher zu kopieren.
