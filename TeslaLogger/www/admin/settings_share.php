<?php 
require("tools.php");

$prefix = "/etc/teslalogger/";
if (isDocker())
    $prefix = "/tmp/";

header("Location: index.php");

if ($_REQUEST[a]== "yes")
{
    file_put_contents($prefix."sharedata.txt","");
    unlink($prefix."nosharedata.txt");
}
else if ($_REQUEST[a]== "no")
{
    file_put_contents($prefix."nosharedata.txt","");
    unlink($prefix."sharedata.txt");
}
?>