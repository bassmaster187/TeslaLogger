<!DOCTYPE html>
<html lang="de">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger Config V1.5</title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="http://teslalogger.de/teslalogger_style.css">
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<script>
  $( function() {
    $( "button" ).button();
	GetCurrentData();
	
	setInterval(function()
		{
			if ( document.hasFocus() ) 
			{
				GetCurrentData();
			}
		}
		,5000);
  } );
  
	function GetCurrentData()
	{
		$.ajax({
		  url: "current_json.php",
		  dataType: "json"
		  }).done(function( jsonData ) {
			$('#ideal_battery_range_km').text(jsonData["ideal_battery_range_km"].toFixed(1));
			$('#odometer').text(jsonData["odometer"].toFixed(1));
			$('#battery_level').text(jsonData["battery_level"]);
			$('#car_version').text(jsonData["car_version"]);
			
			if (jsonData["charging"])
			{
				$('#car_statusLabel').text("Wird geladen:");
				$('#car_status').html(jsonData["charger_power"] + " kW / +" + jsonData["charge_energy_added"] + " kWh<br>" + jsonData["charger_voltage"]+"V / " + jsonData["charger_actual_current"]+"A / "+ jsonData["charger_phases"]+"P");
			}
			else if (jsonData["driving"])
			{
				$('#car_statusLabel').text("Fahren:");
				$('#car_status').text(jsonData["speed"] + " km/h / " + jsonData["power"]+"PS");
			}
			else if (jsonData["online"])
			{
				$('#car_statusLabel').text("Status:");
				$('#car_status').text("Online");
			}
			else if (jsonData["sleeping"])
			{
				$('#car_statusLabel').text("Status:");
				$('#car_status').text("Schlafen");
			}
			else
			{
				$('#car_statusLabel').text("Status:");
				$('#car_status').text("?");
			}
			});
			
	}
  
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
  </head>
  <body>
  <button onclick="window.location.href='logfile.php';">Logfile</button>
  <button onclick="BackgroudRun('restartlogger.php', 'Reboot!');">Restart</button>
  <button onclick="BackgroudRun('update.php', 'Reboot!');">Update</button>
  <button onclick="window.location.href='backup.php';">Backup</button>
  <button onclick="window.location.href='geofencing.php';">Geofence</button>
  <button onclick="BackgroudRun('/wakeup.php', 'Wakeup!');">Wakeup</button>
  <button onclick="BackgroudRun('gosleep.php', 'Sleep!');">Sleep</button>
  <button onclick="window.location.href='settings.php';">Settings</button>
  

  <div id="content">
  <h1>Fahrzeuginfo:</h1>
  <table>
  <tr><td><b><span id="car_statusLabel"></span></b></td><td><span id="car_status"></span></td></tr>
  <tr><td><b>Typical Range:</b></td><td><span id="ideal_battery_range_km">---</span> km / <span id="battery_level">---</span> %</td></tr>
  <tr><td><b>KM Stand:</b></td><td><span id="odometer">---</span> km</td></tr>
  <tr><td><b>Car Version:</b></td><td><span id="car_version">---</span></td></tr>
  </table>
  <?PHP
  echo(file_get_contents("http://teslalogger.de/teslalogger_content_index.php"));
  ?>
  </div>
  <br><br>	
  </body>
</html>