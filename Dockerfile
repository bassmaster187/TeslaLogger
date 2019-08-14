FROM mono:6.0.0.313
WORKDIR /etc/teslalogger
COPY TeslaLogger/bin/* ./
CMD ["mono", "./TeslaLogger.exe"]