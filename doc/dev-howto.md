# Development environment for Teslalogger
I'm using the free Visual Studio 2019 Community Edition https://visualstudio.microsoft.com/de/vs/ on Windows. You can also use it on MacOS but with a couple of limitations. 
For web development I'm using the free Visual Studio Code https://code.visualstudio.com/ with a couple of plugins. PHP, JavaScript, CCS, Emmet

I think the best way to setup a dev enviroment is to run a docker on local machine and stop the Teslalogger container. https://github.com/bassmaster187/TeslaLogger/blob/master/docker_setup.md

If you want to debug the internal WebServer of Teslalogger (HttpListener in WebServer.cs) you have to change some files:
- GetTeslaloggerURL() in tools.php
- teslaloggerstream.php
use http://host.docker.internal on Windows. On linux and macos it may be another address: https://stackoverflow.com/questions/24319662/from-inside-of-a-docker-container-how-do-i-connect-to-the-localhost-of-the-mach


# Automated testing
There are a couple of unit tests in the Project UnitTestsTeslalogger. As far as I know they don't work on Visual Studio for MacOS.
Feel free to add tests for your contribution or any other. Make sure you have backup everything as the tests may alter your data. 

You can start the tests with the Test-Explorer. 

For automated UI tests Selenium and Selenium Chrome Driver is used. You need to install latest Chrome Browser.
https://www.youtube.com/watch?v=hIYyDMiWXkw

