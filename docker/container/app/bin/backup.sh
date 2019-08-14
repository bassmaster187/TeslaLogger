#!/bin/bash
NOW=$(date +%Y%m%d%H%M%S)
mysqldump -uroot -pteslalogger  --single-transaction --routines --triggers teslalogger | gzip > /etc/teslalogger/backup/mysqldump$NOW.gz
cd /etc/teslalogger/Exception 
tar -czf ex_$(date +%Y%m%d%H%M%S).tar.gz --remove-files *.txt 
