#!/bin/bash

mkdir -p /etc/teslalogger/backup
mkdir -p /etc/teslalogger/Exception

NOW=$(date +%Y%m%d%H%M%S)
DAY=$(date +%d)
MONTH=$(date +%m)
YEAR=$(date +%Y)
LYEAR=$((YEAR-1))
SQLDUMP=/etc/teslalogger/backup/mysqldump$NOW.gz
GEOFENCE=/etc/teslalogger/backup/geofence-private$NOW.gz
if [ $DAY -eq 1 ] && [ $MONTH -eq 1 ]; then
	SQLDUMP=/etc/teslalogger/backup/yeardump-$LYEAR.gz
	GEOFENCE=/etc/teslalogger/backup/yeargeofence-$LYEAR.gz
fi

if test -f "/tmp/teslalogger-DOCKER"; then
    mysqldump -uroot -pteslalogger -hdatabase --single-transaction --routines --triggers teslalogger | gzip -9 > $SQLDUMP
else
    mysqldump -uroot -pteslalogger  --single-transaction --routines --triggers teslalogger | gzip -9 > $SQLDUMP
fi

gzip -c /etc/teslalogger/geofence-private.csv > $GEOFENCE

cd /etc/teslalogger/Exception
if ls *.txt >/dev/null 2>&1; then
    tar -czf ex_$(date +%Y%m%d%H%M%S).tar.gz --remove-files *.txt
fi

if [ $DAY -eq 1 ]; then
	#echo processing logfile backup and consolidation
	LOGBACKUP=/etc/teslalogger/backup/logfile-$YEAR$MONTH.gz
	echo $LOGBACKUP
	if ! test -f "$LOGBACKUP"; then
		cp /etc/teslalogger/nohup.out /etc/teslalogger/logfile-$YEAR$MONTH.log
		gzip -c9 /etc/teslalogger/logfile-*.log > $LOGBACKUP
		if test -f "$LOGBACKUP"; then
			rm /etc/teslalogger/logfile-*.log
		fi
		echo > /etc/teslalogger/nohup.out
	fi
	#echo processing cleanup of files older than one year except yearly backups - thank you @saibot
	find /etc/teslalogger/backup/ -type f -name "mysqldump*.gz" -mtime +365 -delete
	find /etc/teslalogger/backup/ -type f -name "geofence-*.gz" -mtime +365 -delete
	find /etc/teslalogger/backup/ -type f -name "logfile-*.gz"  -mtime +365 -delete
fi

if test -f "/etc/teslalogger/my-backup.sh"; then
    source /etc/teslalogger/my-backup.sh
fi

