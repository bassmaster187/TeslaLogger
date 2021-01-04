#!/bin/bash

mkdir -p /etc/teslalogger/backup
mkdir -p /etc/teslalogger/Exception

NOW=$(date +%Y%m%d%H%M%S)
if test -f "/tmp/teslalogger-DOCKER"; then
    mysqldump -uroot -pteslalogger -hdatabase --single-transaction --routines --triggers teslalogger | gzip > /etc/teslalogger/backup/mysqldump$NOW.gz
else
    if test -x /usr/bin/lzma; then
        mysqldump -uroot -pteslalogger --single-transaction --routines --triggers --ignore-table=mothership teslalogger | lzma -1 > /etc/teslalogger/backup/mysqldump$NOW.lzma
    else
        mysqldump -uroot -pteslalogger --single-transaction --routines --triggers --ignore-table=mothership teslalogger | gzip > /etc/teslalogger/backup/mysqldump$NOW.gz
    fi
fi
cd /etc/teslalogger/Exception 
tar -czf ex_$(date +%Y%m%d%H%M%S).tar.gz --remove-files *.txt 

if test -f "/etc/teslalogger/my-backup.sh"; then
    source /etc/teslalogger/my-backup.sh
fi