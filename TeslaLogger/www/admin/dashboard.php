
<!DOCTYPE html>
<?php
require("language.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="apple-mobile-web-app-title" content="Teslalogger Dashboard">
    <link rel="apple-touch-icon" href="img/apple-touch-icon.png">
    <title>Teslalogger Dashboard</title>
	<link rel="stylesheet" href="dashboard.css" />
	<link rel="stylesheet" href="my_dashboard.css" />
	<link href='https://fonts.googleapis.com/css?family=Roboto:300,400,500,300italic' rel='stylesheet' type='text/css'>
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script>

	$( function() {
	<?php 
		$f = scandir("wallpapers");
		if (count($f) > 3)
		{
			echo("$('#error').text('');\n");
			echo("$('html').css('background-image','url(\"wallpapers/" .$f[3]. "\")');\n");
		}
	?>

		GetCurrentData();
		
		setInterval(function()
		{
			GetCurrentData();	
		}
		,60000);
	} );

	function GetCurrentData()
	{
		$.ajax({
		  url: "current_json.php",
		  dataType: "json"
		  }).done(function( jsonData ) {
			$('#ideal_battery_range_km').text(jsonData["ideal_battery_range_km"].toFixed(0));

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
				var text = "Online";

				if (jsonData["is_preconditioning"])
					text = text + "<br>Preconditioning";

				if (jsonData["sentry_mode"])
					text = text + "<br>Sentry Mode";

				if (jsonData["battery_heater"])
					text = text + "<br>Battery Heater";

				$('#car_statusLabel').text("Status:");
				$('#car_status').html(text);
			}
			else if (jsonData["sleeping"])
			{
				$('#car_statusLabel').text("Status:");
				$('#car_status').text("Schlafen");
			}
			else
			{
				$('#car_statusLabel').text("Status:");
				$('#car_status').text("Offline");
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
  <div id="panel">
	  <div id="headline">Teslalogger Dashboard</div>
	  <div id="rangeline"><img id="batimg" src="img/bat-icon.png">
		  <span id="ideal_battery_range_km" style="">-</span>
		  <font id="km">km</font>
	  </div>
	  <div id="car_statusLabel">-</div>
	  <div id="car_status">-</div>
	  <div id="error">No wallpapers found in \\raspberry\teslalogger-web\admin\wallpapers</div>
  </div>
  </body>
</html>
