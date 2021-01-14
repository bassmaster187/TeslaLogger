FROM php:7.3-apache

# install packages
RUN apt-get update && \
  apt-get install -y --no-install-recommends mariadb-client && \
  apt-get clean && \
  apt-get autoremove -y && \
  rm -rf /var/lib/apt/lists/*