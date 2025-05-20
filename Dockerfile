FROM mcr.microsoft.com/dotnet/aspnet:8.0.15
# timezone / date
RUN echo "Europe/Berlin" > /etc/timezone && dpkg-reconfigure -f noninteractive tzdata

# install packages
RUN apt-get update && \
 apt-get upgrade -y && \
 apt-get install -y --no-install-recommends git && \
 apt-get install -y --no-install-recommends mariadb-client && \
 # apt-get install -y optipng python3 python3.pip && \
 apt-get install -y wget build-essential libreadline-dev libncursesw5-dev libssl-dev libsqlite3-dev tk-dev libgdbm-dev libc6-dev libbz2-dev libffi-dev zlib1g-dev && \
 wget -c https://www.python.org/ftp/python/3.13.3/Python-3.13.3.tar.xz && \
 tar -Jxvf Python-3.13.3.tar.xz && \
 cd Python-3.13.3 && \
 ./configure --enable-optimizations && \
 make -j $(nproc) && \
 make altinstall && \
 apt-get clean && \
 apt-get autoremove -y && \
 rm -rf /var/lib/apt/lists/* && \
 echo "export TERM=xterm" >> /root/.bashrc  && \
 echo "DOCKER" >> /tmp/teslalogger-DOCKER && \
 echo "DOCKER" >> /tmp/teslalogger-dockernet8 && \
 pip3.13 install grpc-stubs==1.53.0.5 --break-system-packages && \
 pip3.13 install grpcio==1.67.1 --break-system-packages && \
 pip3.13 install protobuf==5.29.1 --break-system-packages && \
 pip3.13 install rich --break-system-packages

RUN mkdir -p /etc/lucidapi
RUN mkdir -p /etc/teslalogger
RUN mkdir -p /etc/teslalogger/sqlschema
RUN mkdir -p /etc/teslalogger/git/TeslaLogger/Grafana
RUN mkdir -p /etc/teslalogger/git/TeslaLogger/GrafanaConfig
RUN mkdir -p /etc/teslalogger/git/TeslaLogger/GrafanaPlugins

COPY lucidapi /etc/lucidapi
COPY TeslaLogger/sqlschema.sql /etc/teslalogger/sqlschema
COPY --chmod=777 TeslaLogger/bin /etc/teslalogger/
COPY TeslaLogger/Grafana /etc/teslalogger/git/TeslaLogger/Grafana
COPY TeslaLogger/GrafanaConfig /etc/teslalogger/git/TeslaLogger/GrafanaConfig
COPY TeslaLogger/GrafanaPlugins /etc/teslalogger/git/TeslaLogger/GrafanaPlugins

WORKDIR /etc/teslalogger/Debug/net8.0

ENTRYPOINT ["dotnet", "./TeslaLoggerNET8.dll"]
