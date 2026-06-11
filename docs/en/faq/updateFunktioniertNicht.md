# Update does not work in Docker
For an automatic update or update in the admin panel to work with .NET 8 in Docker, several things must work.
The most important thing is that Watchtower works. 

For this, you can view the Watchtower log file. 
```
docker logs teslalogger-watchtower
```

Best is to press Update in the admin panel and watch the log:


```
time="2025-12-22T15:23:51Z" level=debug msg="Valid token authenticated" path=/v1/update
time="2025-12-22T15:23:51Z" level=debug msg="Log entry sent immediately (not batching)" fields.level=info legacy_template=true message="Received HTTP API update request" notify=no
time="2025-12-22T15:23:51Z" level=debug msg="Starting template processing for notification message" container_count=0 entries_count=1 legacy_template=true notify=no report_available=false
time="2025-12-22T15:23:51Z" level=debug msg="Template processing completed successfully" entries_count=1 legacy_template=true msg_length=60 notify=no template_name=
time="2025-12-22T15:23:51Z" level=debug msg="Generated notification message" message="Received HTTP API update request | method=GETpath=/v1/update" notify=no
time="2025-12-22T15:23:51Z" level=debug msg="Preparing to send entries" error="<nil>" message="Received HTTP API update request | method=GETpath=/v1/update" notify=no
time="2025-12-22T15:23:51Z" level=debug msg="Checking notifier state before queuing message" closed=false entries_count=1 msg_length=60 notify=no receiving=true
time="2025-12-22T15:23:51Z" level=debug msg="Queuing notification message to channel" entries_count=1 msg_length=60 notification_type=shoutrrr notify=no
time="2025-12-22T15:23:51Z" level=debug msg="Successfully sent message to notification channel" channel_status=sent entries_count=1 msg_length=60 notify=no
time="2025-12-22T15:23:51Z" level=info msg="Received HTTP API update request" method=GET path=/v1/update
time="2025-12-22T15:23:51Z" level=debug msg="No image query parameters provided"
time="2025-12-22T15:23:51Z" level=debug msg="Handler: trying to acquire lock"
time="2025-12-22T15:23:51Z" level=debug msg="Handler: acquired lock"
...
time="2025-12-22T15:23:51Z" level=info msg="Executing full update"
time="2025-12-22T15:23:51Z" level=debug msg="Handler: executing update function"
time="2025-12-22T15:23:51Z" level=debug msg="Starting RunUpdatesWithNotifications"
time="2025-12-22T15:23:51Z" level=debug msg="StartNotification called - batching mode enabled" entries_count=0 legacy_template=true notify=no receiving=true suppress_summary=false
time="2025-12-22T15:23:51Z" level=debug msg="About to call Update function"
time="2025-12-22T15:23:51Z" level=debug msg="Starting container update check"
time="2025-12-22T15:23:51Z" level=debug msg="Sending notification" message="Received HTTP API update request | method=GETpath=/v1/update" notify=no
time="2025-12-22T15:23:51Z" level=debug msg="Retrieving container list" include_restarting=false include_stopped=false
time="2025-12-22T15:23:51Z" level=debug msg="Sending notification" message="Executing full update" notify=no
...
time="2025-12-22T15:23:55Z" level=debug msg="Update function returned, about to check cleanup"
time="2025-12-22T15:23:55Z" level=debug msg="Report before notification" failed=0 scanned=4 updated=0 updated_names="[]"
time="2025-12-22T15:23:55Z" level=debug msg="About to send notifications" notification_report=false notification_split_by_container=false notifier_present=true
time="2025-12-22T15:23:55Z" level=debug msg="About to send notifications"
time="2025-12-22T15:23:55Z" level=debug msg="SendNotification called - sending queued entries and report" entries_count=0 legacy_template=true notify=no report_available=true
time="2025-12-22T15:23:55Z" level=debug msg="Starting template processing for notification message" container_count=4 entries_count=0 legacy_template=true notify=no report_available=true
time="2025-12-22T15:23:55Z" level=debug msg="Template processing completed successfully" entries_count=0 legacy_template=true msg_length=0 notify=no template_name=
time="2025-12-22T15:23:55Z" level=debug msg="Generated notification message" message= notify=no
time="2025-12-22T15:23:55Z" level=debug msg="Preparing to send entries" error="<nil>" message= notify=no
time="2025-12-22T15:23:55Z" level=debug msg="Message empty, skipping send" notify=no
time="2025-12-22T15:23:55Z" level=info msg="Update session completed" failed=0 notify=no scanned=4 updated=0
time="2025-12-22T15:23:55Z" level=debug msg="Handler: update function completed"
time="2025-12-22T15:23:55Z" level=debug msg="Handler: releasing lock"

```

If you find such an error message in the log file, you need to update the docker-compose.yml.

```
client version 1.25 is too old. Minimum supported API version is 1.44, please upgrade your client to a newer version"
```

Manual update:
```
docker compose stop
wget https://raw.githubusercontent.com/bassmaster187/TeslaLogger/refs/heads/NET8/docker-compose.yml -O docker-compose.yml
docker compose pull
docker compose up -d
```

If nothing happens in the Watchtower log when pressing Update in the admin panel, it may be that you have a version of Watchtower that no longer accepts HTTP GET. 
In that case, also start a manual update.
