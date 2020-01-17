
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
	<link rel="stylesheet" href="dashboard.css?v=<?=time()?>" />
	<link rel="stylesheet" href="my_dashboard.css?v=<?=time()?>" />
	<link href='https://fonts.googleapis.com/css?family=Roboto:300,400,500,300italic' rel='stylesheet' type='text/css'>
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script>

	$( function() {
	<?php 
		$files = scandir("wallpapers");
		foreach ($files as $i => &$f)		
		{
			if (stripos($f,".") === 0)
				continue;
		
			if (stripos($f,".jpg") > 0 || stripos($f,".png") > 0)
			{		
				echo("$('#error').text('');\n");
				echo("$('body').css('background-image','url(\"wallpapers/" .$f. "\")');\n");
				break;
			}
		}
	?>

		GetCurrentData();
		
		setInterval(function()
		{
			GetCurrentData();	
		}
		,5000);
	} );

	function GetCurrentData()
	{
		updateClock();
	
		$.ajax({
		  url: "current_json.php",
		  dataType: "json"
		  }).done(function( jsonData ) {
			$('#ideal_battery_range_km').text(jsonData["ideal_battery_range_km"].toFixed(0));
			$('#battery_level').text(jsonData["battery_level"]);
			
			updateBat(jsonData["battery_level"]);

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
					text = text + "<br>Preconditioning "+ jsonData["inside_temperature"] +"°C";

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

	function updateClock()
	{
		var currentTime = new Date ( );

		var currentHours = currentTime.getHours ( );
		var currentMinutes = currentTime.getMinutes ( );
		var currentSeconds = currentTime.getSeconds ( );
		
		currentMinutes = ( currentMinutes < 10 ? "0" : "" ) + currentMinutes;
		var currentTimeString = currentHours + ":" + currentMinutes;
		
		$('#clock').text(currentTimeString);
	}
	
	function updateBat($percent)
	{
		$batwidth = $('#batimg').width();
		$batheight = $('#batimg').height();
		$('#batimg_end').css('height', $batheight);
		$('#batimg_m').css('height', $batheight );
		
		$newwidth = $batwidth * 0.92 * $percent / 100;
		
		$topspace = 4;
		$leftspace = 7;
		
		if ($batwidth < 150)
		{
			$topspace = 8;
			$leftspace = 3;
		}
		else if ($batwidth < 200)
		{
			$topspace = 7;
			$leftspace = 5;
		}
		
		$('#batimg_m').css('top', $topspace);
		$('#batimg_end').css('top', $topspace);
		
		$('#batimg_m').css('width', $newwidth);
		$('#batimg_m').css('left', $leftspace);
		$('#batimg_end').css('left', $newwidth + $leftspace);
		
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
	  <div id="rangeline"><span id="batdiv"><img id="batimg" src="img/bat-icon.png"><img id="batimg_m" src="img/bat-icon-gr.png"><img id="batimg_end" src="img/bat-icon-end.png"></span>
	  <span id="ideal_battery_range_km" style="">-</span><font id="km">km</font>
	  <span id="battery_level" style="">-</span><font id="percent">%</font>
	  </div>
	  <div id="car_statusLabel">-</div>
	  <div id="car_status">-</div>
	  <div id="error">No wallpapers found in \\raspberry\teslalogger-web\admin\wallpapers</div>
  </div>
  <div id="clock">00:00</div>
  </body>
</html>
