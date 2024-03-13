<?php 
require_once("Parsedown.php");
require_once("tools.php");
echo('<div id="changelog">');
GetFileFromTeslaloggerAndWriteToTMP("changelog.md");
$md = file_get_contents("/tmp/changelog.md");
echo Parsedown::instance()->text($md); 
echo("</div>");
?>