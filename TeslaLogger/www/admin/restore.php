<!DOCTYPE html>
<?php
require("language.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger Restore Database 1.0</title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="https://teslalogger.de/teslalogger_style.css">
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.css">
	<script src="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.js"></script>
	<script>
	
	$( function() {
		$("button").button();
	});
	
	function BackgroudRun($target, $text)
	{
	  $.ajax($target, {
		data: {
			id: ''
		}
		})
		.then(
		function success(name) {
			alert($text);
		},
		function fail(data, status) {
			alert($text);
		}
		);
	}
   

</script>

<button style="width:120px;" onclick="BackgroudRun('restartlogger.php', 'Reboot!');">Restart</button>
<button style="width:120px;" onclick="window.location.href='backup.php';">Backup</button>
<button style="width:120px;" onclick="window.location.href='settings.php';">Settings</button>
<h1>RESTORE DATABASE</h1>

    Please make sure you backup your Teslalogger before restoring any databases. 
	Move your backup folder from \\RASPBERRY\teslalogger\backup to your hard drive!<br><br>
	The restore process may take up to 10 minutes!<br><br>
	Don't interrupt the restore process in any way! Don't reload the page! Don't close this page! 	
	<br><br>
	
<form action="restore_upload.php" method="post" enctype="multipart/form-data">

    <input type="file" name="fileToUpload" id="fileToUpload"><br>
    <input type="submit" value="Restore" name="submit">
</form>
