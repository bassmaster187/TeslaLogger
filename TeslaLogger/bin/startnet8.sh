#!/bin/bash
pkill mono
pkill dotnet
cd /etc/teslalogger/Debug/net8.0
nohup /home/cli/dotnet TeslaLoggerNET8.dll >> /etc/teslalogger/nohup.out  &
