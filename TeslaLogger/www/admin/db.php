<?php
$mysqlhost="localhost"; 
$mysqluser="root"; 
$mysqlpwd="teslalogger"; 
$mysqldb="teslalogger"; 

$con=new mysqli($mysqlhost, $mysqluser, $mysqlpwd, $mysqldb);
/* check connection */

if (mysqli_connect_errno()) {
    printf("Connect failed: %s\n", mysqli_connect_error());
    exit();
}
?>