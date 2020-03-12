<?php 
header("Location: index.php");
if ($_REQUEST[a]== "yes")
{
    file_put_contents("/etc/teslalogger/sharedata.txt","");
    unlink("/etc/teslalogger/nosharedata.txt");
}
else if ($_REQUEST[a]== "no")
{
    file_put_contents("/etc/teslalogger/nosharedata.txt","");
    unlink("/etc/teslalogger/sharedata.txt");
}
?>