# Teslalogger Docker Setup on Synology NAS - DSM 6

![Image](https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/screenshots/docker-synology.PNG)

I'm running on DSM 6.1.7-15284 Update 3. YMMV on other versions.


From Synology Package Centre, install both Docker and Git Server.

Once installed, click on each package and ensure that they are in the 'Running' state.

SSH to your Synology and login as 'admin'.

When you're logged in, type 'sudo -i' and become root user. The password is the same as admin.

In Package Centre, click on Docker, and check the 'Installed Volume'. Make a note of it.

In your SSH session, cd to this volume. e.g. cd /volume1/docker

Run git clone https://github.com/bassmaster187/TeslaLogger in the Terminal.

Create an empty Folder GrafanaDashboards in:

/volume1/docker/TeslaLogger/TeslaLogger/GrafanaDashboards

When finished, follow rest of instructions here: https://github.com/bassmaster187/TeslaLogger/blob/master/docker_setup.md

The above page mentions the following to test:

Try to connect to Grafana with you favorite browser: http://localhost:3000 (admin/teslalogger)


This is incorrect for Synology. The URL should be http://IPofYourSynology:3000

e.g. http://192.168.0.100:3000

The username is admin, but the password is teslalogger.

