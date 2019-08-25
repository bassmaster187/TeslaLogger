## Instructions

### Setup machine

Install docker on an empty Linux VPS (only tested with Ubuntu 18.04 so far)

```
curl -fsSL get.docker.com -o get-docker.sh
sh get-docker.sh
apt update && apt upgrade -y
apt install apache2-utils
apt autoremove -y
docker network create webgateway
```

Install docker compose manually or from your system's installer (e.g. 'apt install docker-compose).

For manual installation follow the instructions [here](https://docs.docker.com/compose/install/#install-compose)

### Clone repository

Clone this repository into a folder of your convenience

**Attention** As long as docker is in testing be sure you clone the right branch (for advanced users only) - also be aware that at the time of your reading the branch might differ from the instructions here. This is all very much Work in Progress.

```
cd /opt
git clone https://github.com/bassmaster187/TeslaLogger
cd profit-docker
```

### Do the basic setup

Open the .env file in your Text/code editor of choice (you know that you can't use Word for that, right?).

Change the values if needed. Set the FQDN for grafana and admin so SSL certs can be obtained.

Note that the domain/subdomain you enter needs to point to your server. The email address in LETSENCRYPT_EMAIL is used to generate free SSL certificates (Letsencrypt)

Also generate an .htpasswd file and move it to `TeslaLogger/www/` - this is used to protect your admin access.

```
htpasswd -c TeslaLogger/www/.htpasswd myUserName
```

Put your TeslaLogger config next to the .exe file in `TeslaLogger/bin` and remember to change all the values accordingly.
