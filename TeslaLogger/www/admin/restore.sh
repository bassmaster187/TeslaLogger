#!/bin/bash

# restore.sh - Asynchronous database restore script
# Called by restore_upload.php to perform the actual restore

# Arguments:
# $1 - path to the .sql file (decompressed)
# $2 - progress file path (for tracking status)

SQLFILE="$1"
PROGRESS_FILE="$2"

if [ -z "$SQLFILE" ] || [ -z "$PROGRESS_FILE" ]; then
    echo "Usage: restore.sh <sqlfile> <progressfile>" >&2
    exit 1
fi

if [ ! -f "$SQLFILE" ]; then
    echo "SQL file not found: $SQLFILE" >&2
    exit 1
fi

# Initialize progress file
echo "0:Startende:0" > "$PROGRESS_FILE"

# Determine if we're in Docker
if [ -f "/tmp/teslalogger-DOCKER" ]; then
    DB_HOST="-hdatabase"
else
    DB_HOST=""
fi

# Write progress - Step 1: Preparing restore
echo "1:Preparieren der Datenbank..." > "$PROGRESS_FILE"

# Disable foreign key checks and sandbox mode for speed
TEMP_SQL="/tmp/restore_temp.sql"
grep -v '/\*M!999999-/' "$SQLFILE" > "$TEMP_SQL"

# Write progress - Step 2: Restoring database
echo "2:Datenbank wird zurückgespielt..." > "$PROGRESS_FILE"

# Perform the restore
mysql_error=""
if [ -f "/tmp/teslalogger-DOCKER" ]; then
    mysql_error=$(mysql -uroot -pteslalogger -hdatabase teslalogger < "$TEMP_SQL" 2>&1)
else
    mysql_error=$(mysql -uroot -pteslalogger teslalogger < "$TEMP_SQL" 2>&1)
fi

if [ $? -eq 0 ]; then
    # Write progress - Step 3: Restore successful
    echo "3:OK:Restore erfolgreich abgeschlossen!" > "$PROGRESS_FILE"
    echo "RESTORE_SUCCESS"
else
    # Write progress - Step 4: Error
    echo "4:FEHLER:$mysql_error" > "$PROGRESS_FILE"
    echo "RESTORE_ERROR"
    rm -f "$TEMP_SQL"
    exit 1
fi

# Cleanup
rm -f "$TEMP_SQL"
rm -f "$SQLFILE"
