FROM debian:stretch-slim

RUN apt-get -y update

RUN apt-get -y install apt-transport-https
RUN apt-get -y install curl
RUN apt-get -y install gnupg2

# Install Node
RUN curl -sL https://deb.nodesource.com/setup_11.x | bash -
RUN apt-get install -y nodejs

# Install Grafana
RUN curl https://packages.grafana.com/gpg.key | apt-key add -
RUN echo "deb https://packages.grafana.com/oss/deb stable main" | tee /etc/apt/sources.list.d/grafana.list
RUN apt-get -y update
RUN apt-get -y install grafana

RUN apt-get -y install git
WORKDIR /var/lib/grafana/plugins
RUN git clone https://github.com/lephisto/grafana-trackmap-panel
WORKDIR /var/lib/grafana/plugins/grafana-trackmap-panel
RUN npm install
RUN npm audit fix
RUN npm run build
RUN grafana-cli plugins install natel-discrete-panel

RUN apt-get -y install mariadb-client mariadb-server

# Define our startup script
RUN echo "#!/bin/bash" > /start.sh
RUN echo "service grafana-server start" >> /start.sh
RUN chmod +x /start.sh

VOLUME ["/var/www/","/var/mail/","/var/backup/","/var/lib/mysql","/var/log/"]

# Run it
EXPOSE 3000
CMD /start.sh
