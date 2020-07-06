<?php
$mysqlhost="localhost"; 
$mysqluser="root"; 
$mysqlpwd="teslalogger"; 
$mysqldb="teslalogger"; 

$connection=mysql_connect($mysqlhost, $mysqluser, $mysqlpwd) or die ("Verbindungsversuch fehlgeschlagen: " . mysql_error());
mysql_select_db($mysqldb, $connection) or die("Konnte die Datenbank nicht waehlen.");

?>