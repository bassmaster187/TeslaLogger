---
sidebar_position: 9
slug: /os_upgrade
---
# Raspberry PI3-OS aktualisieren
Das Betriebssystem des Raspberry PI3 ist veraltet und wird nicht mehr unterstützt. Das neueste Grafana unterstützt es ebenfalls nicht mehr. Deshalb muss es manuell aktualisiert werden.

**Ein Backup anlegen und die Backups auf die eigene Festplatte kopieren.** Sie liegen unter: \\raspberry\teslalogger\backup

Sich per SSH mit dem Raspberry verbinden (z. B. `ssh pi@raspberry`).
Das Update kann länger als 1 Stunde dauern, also geduldig bleiben!

Prüfen, ob bereits «Raspbian GNU/Linux 10 (buster)» installiert ist. Bei «Linux 9 (stretch)» muss das OS aktualisiert werden!

```
cd /etc/teslalogger
sudo chmod 777 upgrade2buster.sh
sudo ./upgrade2buster.sh
```

Nach dem Update startet der Raspberry von selbst neu.
