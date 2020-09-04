#!/bin/bash
git --version
rm -rf TeslaLogger/*
rm -rf TeslaLogger
rm -rf /etc/teslalogger/git/*
rm -rf /etc/teslalogger/git
mkdir /etc/teslalogger/git

git clone -b multicarfeature https://github.com/bassmaster187/TeslaLogger /etc/teslalogger/git/
cp -rf /etc/teslalogger/git/TeslaLogger/bin/* /etc/teslalogger
cp -rf /etc/teslalogger/git/TeslaLogger/www/* /var/www/html