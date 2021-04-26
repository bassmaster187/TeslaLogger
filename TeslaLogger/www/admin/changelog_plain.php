<?php 
require_once("Parsedown.php");
echo('<div id="changelog">');
$md = file_get_contents("/etc/teslalogger/changelog.md");
echo Parsedown::instance()->text($md); 
echo("</div>");
?>