FROM mono:6.0.0.313

# timezone / date
RUN echo "Europe/Berlin" > /etc/timezone && dpkg-reconfigure -f noninteractive tzdata

# install packages
RUN apt-get update && \
  apt-get install -y --no-install-recommends git && \
  apt-get clean && \
  apt-get autoremove -y && \
  rm -rf /var/lib/apt/lists/* && \
  echo "export TERM=xterm" >> /root/.bashrc && \
  echo "DOCKER" >> /etc/teslalogger/DOCKER

WORKDIR /etc/teslalogger

CMD ["mono", "./TeslaLogger.exe"]
