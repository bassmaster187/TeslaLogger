
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
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>

	<script>

	$( function() {
	<?php 
		$f = scandir("wallpapers");
		if (count($f) > 2)
		{
			echo("$('#wallpaper').text('');\n");
			echo("$('#wallpaper').css('background-image','url(\"wallpapers/" .$f[2]. "\")');\n");
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
  <div id="wallpaper" style="color: red; background-size: cover;background-color: black; width: 100%; height: 100%; position: absolute; font-size: 20px;">
  No wallpapers found in \\raspberry\teslalogger-web\admin\wallpapers<br>
  </div>
  
  <div style="position: absolute; top: 40px; left: 0; width: 100%; text-align: center; font-size: 100px; color: white; font-family: HeiT ASC Traditional Chinese,M Hei PRC W45,AXIS Font Japanese W55,FB New Gothic,Swissra,Gotham Medium,system,sans-serif;">
  Teslalogger Dashboard
  <div style="margin-top: 50px; vertical-align: middle; color: #D1D1D1;font-size: 100px;"><img src="img/bat-icon.png"><span id="ideal_battery_range_km" style="margin-left:20px;">-</span><font size=24px>km</font></div>
  <div id="car_statusLabel" style="margin-top: 50px;font-size: 60px; color:#EAEAEA; text-shadow: 4px 4px 4px #555;">-</div>
  <div id="car_status" style="margin-top: 10px;font-size: 48px; color:#EAEAEA;">-</div>
  </div>
  </body>
</html>
