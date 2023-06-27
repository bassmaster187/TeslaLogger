# Upgrade your Raspberry PI3 OS
The OS of your Raspberry PI3 is outdated and not supported anymore. The latest Grafana doesn't support it as well. So you have to upgrade it manually.

**Make a backup and make sure you move your backups to your harddrive.** They are located under: \\raspberry\teslalogger\backup

Connect via [SSH](https://github.com/bassmaster187/TeslaLogger/blob/master/docs/en/faq.md#connect-to-your-raspberry-with-ssh) to your Raspberry. 
The update may take longer than 1 hour, so be patiant!

Check if you already have "Raspbian GNU/Linux 10 (buster)". If you have "Linux 9 (stretch)", you need to upgrade your OS!

```
cd /etc/teslalogger
sudo chmod 777 upgrade2buster.sh
sudo ./upgrade2buster.sh
```

After the upgrade the Raspberry will reboot itself.
