# Development environment for Teslalogger
I'm using the free Visual Studio 2019 Community Edition https://visualstudio.microsoft.com/de/vs/ on Windows. You can also use it on MacOS but with a couple of limitations. 
For web development I'm using the free Visual Studio Code https://code.visualstudio.com/ with a couple of plugins. PHP, JavaScript, CCS, Emmet

I think the best way to setup a dev enviroment is to run a docker on local machine and stop the Teslalogger container. https://github.com/bassmaster187/TeslaLogger/blob/master/docker_setup.md

If you want to debug the internal WebServer of Teslalogger (HttpListener in WebServer.cs) you have to change some files:
- GetTeslaloggerURL() in tools.php
- Teslaloggerstrem.php
use http://host.docker.internal on linux and macos it is any other address: https://stackoverflow.com/questions/24319662/from-inside-of-a-docker-container-how-do-i-connect-to-the-localhost-of-the-mach


