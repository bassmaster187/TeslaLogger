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
    <title>Teslalogger Config V1.7</title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="https://teslalogger.de/teslalogger_style.css">
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

			$("#trip_start").text(jsonData["trip_start"]);
			$("#max_speed").text(jsonData["trip_max_speed"]);
			$("#max_power").text(jsonData["trip_max_power"]);
			$("#trip_kwh").text(Math.round(jsonData["trip_kwh"] *10)/10);
			$("#trip_avg_kwh").text(Math.round(jsonData["trip_avg_kwh"] *10)/10);
			$("#trip_distance").text(Math.round(jsonData["trip_distance"]*10)/10);

			var trip_duration_sec = jsonData["trip_duration_sec"];
			var min = Math.floor(trip_duration_sec / 60);
			var sec = trip_duration_sec % 60;
			if (sec < 10)
				sec = "0"+sec;

			$("#trip_duration_sec").text(min + ":" + sec);
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
  <body style="padding-top: 5px; padding-left: 10px;">
  <button style="width:120px;" onclick="window.location.href='logfile.php';">Logfile</button>
  <button style="width:120px;" onclick="BackgroudRun('restartlogger.php', 'Reboot!');">Restart</button>
  <button style="width:120px;" onclick="BackgroudRun('update.php', 'Reboot!');">Update</button>
  <button style="width:120px;" onclick="window.location.href='backup.php';">Backup</button>
  <button style="width:120px;" onclick="window.location.href='geofencing.php';">Geofence</button>
  <button style="width:120px;" onclick="BackgroudRun('/wakeup.php', 'Wakeup!');">Wakeup</button>
  <button style="width:120px;" onclick="BackgroudRun('gosleep.php', 'Sleep!');">Sleep</button>
  <button style="width:120px;" onclick="window.location.href='settings.php';">Settings</button>
  <br />
  <br />
  <div id="content">
  <table style="float:left;">
  <thead style="background-color:#d0d0d0; color:#000000;"><td colspan="2" style="font-weight:bold;"><?php t("Fahrzeuginfo"); ?></td></thead>
  <tr><td width="120px"><b><span id="car_statusLabel"></span></b></td><td width="180px"><span id="car_status"></span></td></tr>
  <tr><td><b><?php t("Typical Range"); ?>:</b></td><td><span id="ideal_battery_range_km">---</span> km / <span id="battery_level">---</span> %</td></tr>
  <tr><td><b><?php t("KM Stand"); ?>:</b></td><td><span id="odometer">---</span> km</td></tr>
  <tr><td><b><?php t("Car Version"); ?>:</b></td><td><span id="car_version">---</span></td></tr>
  <tr><td><b>Teslalogger:</b></td><td><?php checkForUpdates();?></td></tr>
  </table>

  <table style="float:left;">
  <thead style="background-color:#d0d0d0; color:#000000;"><td colspan="2" style="font-weight:bold;"><?php t("Letzter Trip"); ?></td></thead>
  <tr><td width="130px"><b>Start:</b></td><td width="180px"><span id="trip_start"></span></td></tr>
  <tr><td><b><?php t("Dauer"); ?>:</b></td><td><span id="trip_duration_sec">---</span> min</td></tr>
  <tr><td><b><?php t("Distanz"); ?>:</b></td><td><span id="trip_distance">---</span> km</td></tr>
  <tr><td><b><?php t("Verbrauch"); ?>:</b></td><td><span id="trip_kwh">---</span> kWh</td></tr>
  <tr><td><b><?php t("Ø Verbrauch"); ?>:</b></td><td><span id="trip_avg_kwh">---</span> Wh/km</td></tr>
  <tr><td><b><?php t("Max km/h"); ?> / <?php t("PS"); ?>:</b></td><td><span id="max_speed">---</span> km/h / <span id="max_power">---</span> PS</td></tr>
  </table>

  <?php

  function checkForUpdates()
  {
	$installed = "?";

	if (file_exists("/etc/teslalogger/VERSION"))
		$installed = file_get_contents("/etc/teslalogger/VERSION");
	else
		$installed = getTeslaloggerVersion("/etc/teslalogger/git/TeslaLogger/Properties/AssemblyInfo.cs");

	$onlineversion = getTeslaloggerVersion("https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/Properties/AssemblyInfo.cs");

	if ($installed != $onlineversion)
	{
		echo($installed . "<br><b>Update available: " .$onlineversion."</b>");
	}
	else
	{
		echo($installed);
	}
  }

function getTeslaloggerVersion($path)
{
	$f = file_get_contents($path);
	preg_match('/AssemblyVersion\(\"([0-9\.]+)\"/',$f, $matches);
	return $matches[1];
}

?>

  <?PHP
  global $language;

  if (isset($language) && strlen($language) > 1 && $language != "de")
	echo(file_get_contents("https://teslalogger.de/teslalogger_content_index-".$language.".php"));
  else
	echo(file_get_contents("https://teslalogger.de/teslalogger_content_index.php"));

  ?>
  </div>
  </body>
</html>
