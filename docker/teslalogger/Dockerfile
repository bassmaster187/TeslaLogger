FROM mono:5.20.1.34

# timezone / date
RUN echo "Europe/Berlin" > /etc/timezone && dpkg-reconfigure -f noninteractive tzdata

# install packages
RUN apt-get update && \
  apt-get install -y --no-install-recommends git && \
  apt-get install -y --no-install-recommends mariadb-client && \
  apt-get install -y --no-install-recommends cron && \
  apt-get install -y --no-install-recommends rclone && \
  apt-get clean && \
  apt-get autoremove -y && \
  rm -rf /var/lib/apt/lists/* && \
  echo "export TERM=xterm" >> /root/.bashrc  && \
  echo "DOCKER" >> /tmp/teslalogger-DOCKER

WORKDIR /etc/teslalogger

CMD ["mono", "./TeslaLogger.exe"]
