# Lucid install (Beta)

LucidLogger is enbedded into TeslaLogger and reuse many components and features. Right now only a docker installation is supported. Raspberry Images doesn't support the LucidLogger right now.

## Installation
Create 2 folders:
```
backup
mysql
```
download docker-compose.yml file: https://github.com/bassmaster187/TeslaLogger/blob/NET8/docker-compose.yml

run:
```
docker compose pull
docker compose up -d
```

