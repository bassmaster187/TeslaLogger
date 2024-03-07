---
sidebar_position: 2
---
# Datensicherung

Hiermit kann manuell die Erstellung einer Sicherungsdatei der Datenbank erstellt werden. Grundsätzlich passiert dies auch automatisch einmal pro Tag bzw. vor einem Update und ist daher nur sinnvoll vor Änderungen. Wurde die Sicherungsdatei erstellt, wird "ok" angezeigt.
Die Sicherungsdatei kann über den Windows-Explorer abgeholt werden, in dem in der Adresszeile oben "\\raspberry\teslalogger\backup" eingegeben wird.
Wir empfehlen, die aktuelle Datei dort von Zeit zu Zeit an einem anderen Ort, beispielsweise auf dem persönlichen PC abzulegen und alte Dateien, um Platz zu sparen dort zu löschen. Da mit der Zeit immer mehr Daten anfallen wird auch die Datensicherung immer grösser!
Über die Datensicherung wird nicht nur die Datenbank gesichert, sondern auch persönlichen "Geofence" Daten sowie die Protokolldateien der letzten Monate.
Wir empfehlen nicht, Änderungen an der Datei "geofence.csv" vorzunehmen, da diese vom Projekt verwaltet und bei Aktualisierungen überschrieben werden kann. Deshalb ist es auch nicht notwendig, diese Datei zu sichern oder zurück zu spielen. Die Datei "geofence-private.csv" muss nicht manuell bearbeitet werden, hierfür gibt es einen Dialog "Geofence"
