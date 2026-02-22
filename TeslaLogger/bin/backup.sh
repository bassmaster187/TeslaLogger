#!/bin/bash

mkdir -p /etc/teslalogger/backup
mkdir -p /etc/teslalogger/Exception

# some preparations
NOW=$(date +%Y%m%d%H%M%S)
DAY=$(date +%d)
MONTH=$(date +%m)
YEAR=$(date +%Y)
LYEAR=$((YEAR-1))
PREFIX=DAY
if [ $DAY -eq 1 ]; then
	PREFIX=MON
fi
SQLDUMP=/etc/teslalogger/backup/$PREFIX-mysqldump$NOW.gz
GEOFENCE=/etc/teslalogger/backup/$PREFIX-geofence-private$NOW.gz
if [ $DAY -eq 1 ] && [ $MONTH -eq 1 ]; then
	PREFIX=YEAR
	SQLDUMP=/etc/teslalogger/backup/yeardump-$LYEAR.gz
	GEOFENCE=/etc/teslalogger/backup/yeargeofence-$LYEAR.gz
fi

# perform backups
if test -f "/tmp/teslalogger-DOCKER"; then
    mysqldump -uroot -pteslalogger -hdatabase --single-transaction --routines --triggers teslalogger | gzip -9 > $SQLDUMP
else
    mysqldump -uroot -pteslalogger  --single-transaction --routines --triggers teslalogger | gzip -9 > $SQLDUMP
fi

if test -f "/etc/teslalogger/data/geofence-private.csv"; then 
	gzip -c /etc/teslalogger/data/geofence-private.csv > $GEOFENCE
elif test -f "/etc/teslalogger/geofence-private.csv"; then
	gzip -c /etc/teslalogger/geofence-private.csv > $GEOFENCE
else
	echo "No geofence-private.csv file found"
fi

cd /etc/teslalogger/Exception
if ls *.txt >/dev/null 2>&1; then
    tar -czf ex_$(date +%Y%m%d%H%M%S).tar.gz --remove-files *.txt
fi
cd -

if [ $DAY -eq 1 ]; then
	# echo processing logfile backup and consolidation
	LOGBACKUP=/etc/teslalogger/backup/logfile-$YEAR$MONTH.gz
	if ! test -f "$LOGBACKUP"; then
		cp /etc/teslalogger/nohup.out /etc/teslalogger/logfile-$YEAR$MONTH.log
		gzip -c9 /etc/teslalogger/logfile-*.log > $LOGBACKUP
		if test -f "$LOGBACKUP"; then
			rm /etc/teslalogger/logfile-*.log
		fi
		echo > /etc/teslalogger/nohup.out
	fi

	# echo processing cleanup of older files except yearly backups - thank you @saibot
	# echo cleanup daily files older than a month
	find /etc/teslalogger/backup/ -type f -name "DAY-mysqldump*.gz" -mtime +31 -delete
	find /etc/teslalogger/backup/ -type f -name "DAY-geofence-*.gz" -mtime +31 -delete
	# echo cleanup monthly files older than a year
	find /etc/teslalogger/backup/ -type f -name "MON-mysqldump*.gz" -mtime +365 -delete
	find /etc/teslalogger/backup/ -type f -name "MON-geofence-*.gz" -mtime +365 -delete
	find /etc/teslalogger/backup/ -type f -name "logfile-*.gz"  -mtime +365 -delete
	# echo cleanup legacy (non tagged) files older than half a year / only temporary required
	find /etc/teslalogger/backup/ -type f -name "mysqldump*.gz" -mtime +180 -delete
	find /etc/teslalogger/backup/ -type f -name "geofence-*.gz" -mtime +180 -delete
fi

# perform individual activities like syncing the backup content to a cloud or another storage
if test -f "/etc/teslalogger/my-backup.sh"; then
    source /etc/teslalogger/my-backup.sh
fi
