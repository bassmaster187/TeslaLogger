<!DOCTYPE html>
<?php
require("language.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="apple-mobile-web-app-title" content="Teslalogger Config">
    <link rel="apple-touch-icon" href="img/apple-touch-icon.png">
    <title>Teslalogger Logfile</title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="https://teslalogger.de/teslalogger_style.css">
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<script src="https://code.jquery.com/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="https://unpkg.com/leaflet@1.4.0/dist/leaflet.css" integrity="sha512-puBpdR0798OZvTTbP4A8Ix/l+A4dHDD0DGqYW6RQ+9jxkRFclaxxQb/SJAWZfWAkuyeQUytO7+7N4QKrDh+drA==" crossorigin=""/>
	<link rel='stylesheet' id='genericons-css'  href='https://www.impala64.de/blog/tesla/wp-content/themes/twentyfourteen/genericons/genericons.css?ver=3.0.3' type='text/css' media='all' />
   <!-- Make sure you put this AFTER Leaflet's CSS -->
	<script src="https://unpkg.com/leaflet@1.4.0/dist/leaflet.js" integrity="sha512-QVftwZFqvtRNi0ZyCtsznlKSWOStnDORoefr1enyq5mVL4tmKB3S/EnC3rRJcxCPavG10IcrVGSmPh6Qw5lwrg==" crossorigin=""></script>

	</head>
  	<body style="padding-top: 5px; padding-left: 10px;">
	<script>$( function() {	
		var objDiv = document.getElementById("log");
		objDiv.scrollTop = objDiv.scrollHeight;
	});
	</script>
	
	<?php 
    include "menu.php";
    echo(menu("Logfile"));
?>
<div id="log" style="overflow: auto; height: 870px; max-width: 1260px;">
<?PHP
	$output  = shell_exec('tail -n1000 /etc/teslalogger/nohup.out');
	if ($output === false) {
        // Handle the error
		echo "error";
    }
	
	$output = str_replace("Start update","<b>Start update</b>", $output);
	$output = str_replace("End update","<b>End update</b>", $output);
	$output = str_replace("Rebooting","<b>Rebooting</b>", $output);
	$output = str_replace("Reboot request!","<b>Reboot request!</b>", $output);

	$output = str_replace("Error: Cloning into '/etc/teslalogger/git'...","...", $output);
	
	$output = str_replace("Rebooting","<b>Rebooting</b>", $output);
	
	$output = str_replace("Key","<b>Key</b>", $output);
	$output = str_replace("not translated!","<b>not translated!</b>", $output);
	
	$output = preg_replace("/(.*(Exception|Error).*)/", "<font color='red'>$1</font>", $output);
	$output = preg_replace("/(state: .*)/", "<b>$1</b>", $output);
	$output = preg_replace("/(http[s]?:\/\/[\w\.\/\-]+)/", "<a href='$1'>$1</a>", $output);
	$output = preg_replace("/(.*\*\*\* Exit Loop !!!)/", "<font color='red'>$1</font>", $output);

	$output = preg_replace("/<font color='red'>(.*)(.*execute: \/usr\/bin\/du -sk \/etc\/teslalogger\/Exception.*)<\/font>/", "$1$2", $output);
	$output = preg_replace("/<font color='red'>(.*)(\/etc\/teslalogger\/Exception)<\/font>/", "$1$2", $output);

	$output = preg_replace("/<font color='red'>(.*)(deleted in Exception direcotry)<\/font>/", "$1$2", $output);
	
	echo nl2br($output);
?>
</div>
</div>
</div>