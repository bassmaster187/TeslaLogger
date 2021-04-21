<?php
$installed = "?";

if (file_exists("/etc/teslalogger/VERSION"))
{
    $installed = file_get_contents("/etc/teslalogger/VERSION");
    
    file_put_contents("/tmp/changelogversion",$installed);
}
header("Location: index.php");
?>