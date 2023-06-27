#!/bin/bash
cat /etc/os-release
export DEBIAN_FRONTEND=noninteractive

sed -i 's/stretch/buster/g' /etc/apt/sources.list
sed -i 's/stretch/buster/g' /etc/apt/sources.list.d/raspi.list
sed -i 's/stretch/buster/g' /etc/apt/sources.list.d/mono-official-stable.list
sed -i 's/stretch/buster/g' /etc/apt/sources.list.d/nodesource.list
apt-get -y remove apt-listchanges
apt-get update
apt -y --fix-broken install
apt-get -y -o "Dpkg::Options::=--force-confdef" -o "Dpkg::Options::=--force-confold" full-upgrade
apt-get -y install php.mysql
a2enmod php7.3
mysql_upgrade -uroot -pteslalogger
cat /etc/os-release
ldd --version
reboot now
 
