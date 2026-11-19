FROM php:8.1-apache-bullseye

# install packages
RUN apt-get update && \
  docker-php-ext-install mysqli && \
  apt-get install -y --no-install-recommends mariadb-client && \
  apt-get clean && \
  apt-get autoremove -y && \
  rm -rf /var/lib/apt/lists/* && \
  sed -e '/<Directory \/var\/www\/>/,/<\/Directory>/s/AllowOverride None/AllowOverride All/' -i /etc/apache2/apache2.conf && \
  echo "1" > /var/tmp/dockerwebserverversion

