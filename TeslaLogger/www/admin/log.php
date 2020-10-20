<?php
require("tools.php");

header('Content-type: application/zip');
header('Content-Disposition: attachment; filename="logfile.zip"');
echo(file_get_contents(GetTeslaloggerURL("logfile")));
