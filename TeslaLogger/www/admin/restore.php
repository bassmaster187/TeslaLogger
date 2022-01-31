<!DOCTYPE html>
<?php
require("language.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger Restore Database 1.0</title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.css">
	<script src="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
  </head>
<body style="padding-top: 5px; padding-left: 10px;">
<?php 
    include "menu.php";
    echo(menu("RESTORE DATABASE"));
?>
    Please make sure you backup your Teslalogger before restoring any databases. 
	Move your backup folder from \\RASPBERRY\teslalogger\backup to your hard drive!<br><br>
	The restore process may take up to 10 minutes!<br><br>
	Don't interrupt the restore process in any way! Don't reload the page! Don't close this page! 	
	<br><br>
	
<form action="restore_upload.php" method="post" enctype="multipart/form-data">

    <input type="file" name="fileToUpload" id="fileToUpload"><br>
    <input type="submit" value="Restore" name="submit">
</form>
