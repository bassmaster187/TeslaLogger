#!/bin/bash

mkdir -p /etc/teslalogger/backup
mkdir -p /etc/teslalogger/Exception

NOW=$(date +%Y%m%d%H%M%S)
if test -f "/tmp/teslalogger-DOCKER"; then
    mysqldump -uroot -pteslalogger -hdatabase --single-transaction --routines --triggers teslalogger | gzip > /etc/teslalogger/backup/mysqldump$NOW.gz
else
    mysqldump -uroot -pteslalogger  --single-transaction --routines --triggers teslalogger | gzip > /etc/teslalogger/backup/mysqldump$NOW.gz
fi

gzip -c /etc/teslalogger/geofence-private.csv > /etc/teslalogger/backup/geofence-private$NOW.gz

cd /etc/teslalogger/Exception 
if ls *.txt >/dev/null 2>&1; then
    tar -czf ex_$(date +%Y%m%d%H%M%S).tar.gz --remove-files *.txt
fi

if test -f "/etc/teslalogger/my-backup.sh"; then
    source /etc/teslalogger/my-backup.sh
fi
