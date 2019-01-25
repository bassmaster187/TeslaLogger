<!DOCTYPE html>
<html lang="de">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<link rel="stylesheet" href="https://teslalogger.de/teslalogger_style.css">
    <title>Teslalogger Config V1.1</title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<script>
  $( function() {
    $( "button" ).button();
	$("#name").val("Hallo");
  } );
  
  function BackgroudRun($target)
  {
	  $.ajax($target, {
		data: {
			id: ''
		}
		})
		.then(
		function success(name) {
			alert('Reboot!');
		},
		function fail(data, status) {
			alert('Reboot!');
		}
	);
  }
  </script>
  </head>
  <body>
  <div style="background-color: #fff;">
  <button onclick="window.location.href='logfile.php';">Logfile</button>
  <button onclick="BackgroudRun('restartlogger.php');">Restart</button>
  <button onclick="BackgroudRun('update.php');">Update</button>
  <button onclick="window.location.href='backup.php';">Backup</button>
  <button onclick="window.location.href='geofencing.php';">Geofence</button>
  <button onclick="window.location.href='/wakeup.php';">Wakeup</button>
  
  <br><br>
 <div id="content">
 <?PHP
 echo file_get_contents('https://teslalogger.de/teslalogger_content_index.php');
 ?>
  </div>
  </div>
  </body>
</html>