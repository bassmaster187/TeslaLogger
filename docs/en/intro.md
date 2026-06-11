---
slug: /
sidebar_position: 1
---
# Introduction

![IMAGE](/img/grafana-08.png)

The goal is to read the data of a Tesla vehicle directly from the car without having to hand over the Tesla login credentials (password of your personal Tesla account) to cloud services, which would also mean giving up control over the vehicle or storing highly personal data such as driving patterns on foreign servers.
In addition to the desire to keep your own data in your own hands, another goal is to be able to argue with this data in case of claims against Tesla. By presenting the detailed data, a battery replacement can be argued much better if it appears necessary. Without this data, only Tesla has all the information about the vehicle. This also relates to so-called "prospect liability" within the framework of the performance promises made by the manufacturer.
TeslaLogger is a software that runs on a Raspberry Pi or in a Docker environment at home, reads the data from the vehicle and stores it locally. For evaluation, there is a web interface that offers various overviews such as charging statistics, trips, consumption, battery degradation, vampire drain and much more. The device does not need to be accessible from the outside for its function. TeslaLogger is not a smartphone app; anyone who wants to access the data from outside needs a secure connection home (VPN).
