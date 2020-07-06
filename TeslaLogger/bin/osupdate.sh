#!/bin/bash

apt-get -y update
apt-get -y upgrade
apt -y --fix-broken install
apt-get -y upgrade
apt-get -y dist-upgrade
apt -y autoremove
mozroots --import --sync --machine
apt-get -y install php.mysql
