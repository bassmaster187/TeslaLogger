FROM bassmaster187/teslalogger-base:1.0.0

ARG TARGETARCH

# timezone / date
RUN echo "Europe/Berlin" > /etc/timezone && dpkg-reconfigure -f noninteractive tzdata

# install packages
RUN echo "TARGETARCH=${TARGETARCH}"  
RUN mkdir -p /etc/lucidapi
RUN mkdir -p /etc/teslalogger
RUN mkdir -p /etc/teslalogger/sqlschema
RUN mkdir -p /etc/teslalogger/git/TeslaLogger/Grafana
RUN mkdir -p /etc/teslalogger/git/TeslaLogger/GrafanaConfig
RUN mkdir -p /etc/teslalogger/git/TeslaLogger/GrafanaPlugins

ENV PATH="/usr/share/dotnet:${PATH}"

COPY lucidapi /etc/lucidapi
COPY TeslaLogger/sqlschema.sql /etc/teslalogger/sqlschema
COPY --chmod=777 TeslaLogger/bin /etc/teslalogger/
COPY TeslaLogger/Grafana /etc/teslalogger/git/TeslaLogger/Grafana
COPY TeslaLogger/GrafanaConfig /etc/teslalogger/git/TeslaLogger/GrafanaConfig
COPY TeslaLogger/GrafanaPlugins /etc/teslalogger/git/TeslaLogger/GrafanaPlugins

WORKDIR /etc/teslalogger/Debug/net8.0

ENTRYPOINT ["dotnet", "./TeslaLoggerNET8.dll"]
