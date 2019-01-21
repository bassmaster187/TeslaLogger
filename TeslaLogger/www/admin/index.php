<!DOCTYPE html>
<html lang="de">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Config</title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<script>
  $( function() {
    $( "button" ).button();
	$("#name").val("Hallo");
  } );
  </script>
  </head>
  <body>
  <button onclick="window.location.href='logfile.php';">Logfile</button>
  <button onclick="window.location.href='restartlogger.php';">Restart</button>
  <button onclick="window.location.href='backup.php';">Backup</button>
  <button onclick="window.location.href='geofencing.php';">Geofence</button>
  <button onclick="window.location.href='/wakeup.php';">Wakeup</button>
  
  <br><br>
<?PHP
	  
	  $xml=simplexml_load_file('/etc/teslalogger/TeslaLogger.exe.config') or die("Error: Cannot create object");
	  $teslaname = $xml->xpath("/configuration/applicationSettings/TeslaLogger.ApplicationSettings/setting[@name='TeslaName']/value/text()")[0];
	  $teslpassword = $xml->xpath("/configuration/applicationSettings/TeslaLogger.ApplicationSettings/setting[@name='TeslaPasswort']/value/text()")[0]; 
?>
	  <table>
		  <tr>
			  <td>Name:</td>
			  <td><input type="text" name="name" id="name" value="<?PHP echo $teslaname; ?>"></td>
		  </tr>
		  <tr>
			  <td>Password:</td>
			  <td><input type="password" name="password" id="password" value="<?PHP echo $teslpassword; ?>"></td>
		  </tr>
		  <tr>
			  <td></td>
			  <td align="right"><button>Update</button></td>
		  </tr>
	  </table>
	
	
	
  </body>
</html>