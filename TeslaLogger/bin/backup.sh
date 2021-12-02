#!/bin/bash

mkdir -p /etc/teslalogger/backup
mkdir -p /etc/teslalogger/Exception

NOW=$(date +%Y%m%d%H%M%S)
if test -f "/tmp/teslalogger-DOCKER"; then
    mysqldump -uroot -pteslalogger -hdatabase --single-transaction --routines --triggers teslalogger | gzip -9 > /etc/teslalogger/backup/mysqldump$NOW.gz
else
    mysqldump -uroot -pteslalogger  --single-transaction --routines --triggers teslalogger | gzip -9 > /etc/teslalogger/backup/mysqldump$NOW.gz
fi

gzip -c /etc/teslalogger/geofence-private.csv > /etc/teslalogger/backup/geofence-private$NOW.gz

cd /etc/teslalogger/Exception 
if ls *.txt >/dev/null 2>&1; then
    tar -czf ex_$(date +%Y%m%d%H%M%S).tar.gz --remove-files *.txt
fi

DAY=$(date +%d)
if [ $DAY -eq 1 ]; then
	MONTH=$(date +%m)
	YEAR=$(date +%Y)
	LYEAR=$((YEAR-1))
	#echo processing logfile backup
	cp /etc/teslalogger/nohup.out /etc/teslalogger/logfile-$YEAR$MONTH.log
	gzip -c9 /etc/teslalogger/logfile-*.log > /etc/teslalogger/backup/logfile-$YEAR$MONTH.gz
	if test -f "/etc/teslalogger/backup/logfile-$YEAR$MONTH.gz"; then
		rm /etc/teslalogger/logfile-*.log
	fi
	echo > /etc/teslalogger/nohup.out

	#echo processing cleanup of files older than $LYEAR$MONTH*
	if test -f "/etc/teslalogger/backup/logfile-$LYEAR$MONTH.gz"; then
		rm /etc/teslalogger/backup/logfile-$LYEAR$MONTH*.gz
	fi
	if test -f "/etc/teslalogger/backup/geofence-private$LYEAR$MONTH*.gz"; then
		rm /etc/teslalogger/backup/geofence-private$LYEAR$MONTH*.gz
	fi
	if test -f "/etc/teslalogger/backup/mysqldump$LYEAR$MONTH*.gz"; then
		rm /etc/teslalogger/backup/mysqldump$LYEAR$MONTH*.gz
	fi
    if test -f "\\teslalogger\teslalogger\Exception\ex_$LYEAR$MONTH*.gz"; then
		rm \\teslalogger\teslalogger\Exception\ex_$LYEAR$MONTH*.gz
	fi
fi


if test -f "/etc/teslalogger/my-backup.sh"; then
    source /etc/teslalogger/my-backup.sh
fi
