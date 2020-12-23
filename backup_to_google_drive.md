# HowTo: upload TeslaLogger backups to Google Drive automaticaly

1. connect via ssh to teslalogger
2. run `rclone config`
3. create connection (e.g.: "tl_backup") and click **ENTER**
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive1.png)
4. find number list number for Google Drive, enter it and click **ENTER**
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive2.png)
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive3.png)
5. **ENTER** for *client_id* and *client_secret*
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive4.png)
6. **1** and **ENTER** for *scope* and **ENTER** for *root_folder_id*:
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive5.png)
7. **ENTER** for *service_account_file*, **n** for *Use auto config*
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive6.png)
8. Copy and paste diplayed link in browser. Login with you Google Account and allow new connection:
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive7.png)
9. key will be shown in browser, copy it with "copy-button", paste into command line window and press **ENTER** and one more **ENTER**:
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive8.png)
10. now you see a settings file with token, click **ENTER**
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive9.png)
11. end than **q** to exit setup
12. open nano: `nano /etc/teslalogger/my-backup.sh` and paste this:
```
#!/bin/bash

/usr/bin/rclone copy --update --verbose --transfers 3 --contimeout 60s --timeout 300s --retries 3 --low-level-retries 10 --stats 1s "/etc/teslalogger/backup" "tl_backup:TeslaLoggerBackup"
```

13. with this command we will save all backup files to *tl_backup* connection we created in step 3 in folder "TeslaLoggerBackup", change it for you needs
14. save file by clicking **CTRL+X**, then y and **ENTER**
15. make it executable: `chmod +x /etc/teslalogger/my-backup.sh`
16. try if it woks: `/etc/teslalogger/my-backup.sh`
17. if you have alot of backups it will take alot time first run... Success looks like this:
![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/backup_gdrive10.png)
18. now go back to nano: `nano /etc/teslalogger/my-backup.sh` and remove **--verbose**, than save file again (**CTRL+X**, **y**, **ENTER**)
19. ***YOU ARE DONE*** :) After each new backup, TeslaLogger will run "my-backup.sh" automaticaly.