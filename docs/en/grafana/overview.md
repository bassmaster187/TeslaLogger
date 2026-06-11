---
sidebar_position: 1
---
# Evaluations

The graphical evaluation system of TeslaLogger is based on "Grafana". Regardless of the name or IP address of the TeslaLogger, the port is always "3000", so the address is [http://raspberry:3000](http://raspberry:3000). The evaluations can also be accessed via the admin panel via the menu "Dashboards".
The browser will then show approximately the following image:

![IMAGE](/img/grafana-01.png)

The "Not secure" notice can be ignored, as we do not have an encryption certificate for a Raspberry that only runs in the own network. If this bothers you, you can add one.
It may happen that a warning appears on the first call that this website is not secure. In this case, an exception must be defined. How this happens and what this warning looks like depends on the browser used.
After installation and if this is not changed later, the username is "admin" and the password is "teslalogger", and after successful login, the overview page with "Consumption of the last 3 hours" appears:

![IMAGE](/img/grafana-02.png)
