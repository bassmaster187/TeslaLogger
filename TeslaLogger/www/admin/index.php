<?php
require_once("redirect.php");
require_once("language.php");
require_once("tools.php");

if (session_status() == PHP_SESSION_NONE) {
    session_start();
}

include "menu.php";
global $display_name;
global $carNeedFleetAPI;
global $carVIN;
global $carNeedSubscription;
global $fleetapiinfo;
global $car_inactive;

$carNeedSubscription = false;
$carid = GetDefaultCarId();
if (isset($_REQUEST["carid"]))
{
	$_SESSION["carid"] = intval($_REQUEST["carid"]);
	$carid = intval($_REQUEST["carid"]);
}
else
{
	$_SESSION["carid"] = $carid;
}
?>
<!DOCTYPE html>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="apple-mobile-web-app-capable" content="yes">
    <meta name="apple-mobile-web-app-title" content="Teslalogger Config">
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<link rel="icon" type="image/png" href="img/apple-touch-icon.png" sizes="131x133">
	<link rel="icon" type="image/png" href="img/apple-touch-icon-192.png" sizes="192x192">
    <link rel="apple-touch-icon" href="img/apple-touch-icon.png">
    <title>Teslalogger</title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=4">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
   <!-- Make sure you put this AFTER Leaflet's CSS -->
	<script src="static/leaflet/1.4.0/leaflet.js"></script>
	<script src="static/leaflet/1.4.0/leaflet.rotatedMarker.js"></script>
	<style>
		#changelog{height:350px; overflow: auto;}
	</style>
	<script>
	var km2mls = 1.609344;
	var map = null;
	var marker = null;
	var mapInit = false;
	var loc;
	var LengthUnit = "<?php echo($LengthUnit); ?>";
	var TemperatureUnit = "<?php echo($TemperatureUnit); ?>";
	var PowerUnit = "<?php echo($PowerUnit); ?>";
	var Range  = "<?php echo ($Range); ?>";

	var Display100pctEnable = "<?php echo($Display100pctEnable); ?>";

	var perfEntries = performance.getEntriesByType("navigation");
	if (perfEntries && perfEntries.length > 0 && perfEntries[0].type === "back_forward") {
		location.reload(true);
	}

  $( function() {
    // $("button").button();
	GetCurrentData();

	map = new L.Map('map');
  // Define layers and add them to the control widget
    L.control.layers({
      'OpenStreetMap': L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        maxZoom: 19
      }).addTo(map), // Add default layer to map
      'OpenTopoMap': L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
        attribution: 'Map data: &copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)',
        maxZoom: 17
      }),
      'Satellite': L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
        attribution: 'Imagery &copy; Esri &mdash; Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community',
        // This map doesn't have labels so we force a label-only layer on top of it
        forcedOverlay: L.tileLayer('https://stamen-tiles-{s}.a.ssl.fastly.net/toner-labels/{z}/{x}/{y}.png', {
          attribution: 'Labels by <a href="http://stamen.com">Stamen Design</a>, <a href="http://creativecommons.org/licenses/by/3.0">CC BY 3.0</a> &mdash; Map data &copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>',
          subdomains: 'abcd',
          maxZoom: 20,
        })
      })
    }).addTo(map);

	var greenIcon = L.icon({iconUrl: 'img/marker-icon-green.png', shadowUrl: 'static/images/marker-shadow.png', iconAnchor:   [12, 40], popupAnchor:  [0, -25]});

	if (navigator.languages != undefined) loc = navigator.languages[0];
			else loc = navigator.language;

	setInterval(function()
		{
			if ( document.hasFocus() )
			{
				GetCurrentData();
			}
		}
		,250);

	ShowInfo();
  } );

	function GetCurrentData()
	{
		$.ajax({
		  url: "current_json.php",
		  dataType: "json"
		  }).done(function( jsonData ) {
			if (LengthUnit == "mile")
			{
				if ( Range == 'IR'){
					$('#ideal_battery_range_km').text((jsonData["ideal_battery_range_km"] / km2mls).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("mi"); ?>");
					$('#full_battery_range_km').text((jsonData["ideal_battery_range_km"]/jsonData["battery_level"]*100/km2mls).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("mi"); ?>");
				}
				else
				{
					$('#ideal_battery_range_km').text((jsonData["battery_range_km"] / km2mls).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("mi"); ?>");
					$('#full_battery_range_km').text((jsonData["battery_range_km"]/jsonData["battery_level"]*100/km2mls).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("mi"); ?>");
				}
				$('#odometer').text((jsonData["odometer"] / km2mls).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("mi"); ?>");
			}
			else
			{
				if ( Range == 'IR'){
					$('#ideal_battery_range_km').text(jsonData["ideal_battery_range_km"].toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("km"); ?>");
					$('#full_battery_range_km').text((jsonData["ideal_battery_range_km"]/jsonData["battery_level"]*100).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("km"); ?>");
				}
				else
				{	$('#ideal_battery_range_km').text(jsonData["battery_range_km"].toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("km"); ?>");
					$('#full_battery_range_km').text((jsonData["battery_range_km"]/jsonData["battery_level"]*100).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("km"); ?>");
				}
				$('#odometer').text((jsonData["odometer"]).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("km"); ?>");
			}

			if (Display100pctEnable == "true")
			{
				$('#full_battery_range_km_span').show();
			}
			else
			{
				$('#full_battery_range_km_span').hide();
			}

			$('#battery_level').text(jsonData["battery_level"]);
			var car_version = jsonData["car_version"];
			car_version = car_version.substring(0,car_version.lastIndexOf(" "));
			$('#car_version').text(car_version);
			$('#car_version_link').attr("href", "https://www.notateslaapp.com/software-updates/version/"+ car_version +"/release-notes");

			if (car_inactive)
			{
				$('#car_status').html("<font color='red'><?php t("Inactive"); ?></font>");
				$('#car_statusLabel').text("<?php t("Status"); ?>:");
				hideSMT();
			}
			else if (jsonData["FatalError"])
			{
				if (jsonData["FatalError"] == "missing_key")
				{
					var missingkeytext = "<?php t("FatalErrorMissingKey"); ?>";
					missingkeytext = missingkeytext.replace("{LINK}", '<a href="https://www.tesla.com/_ak/teslalogger.de" target="_blank">LINK</a>');

					$('#car_status').html('<font color="red">'+missingkeytext+'</font>');
				}
				else
					$('#car_status').html("<font color='red'>"+ jsonData["FatalError"] +"</font>");

				$('#car_statusLabel').html("<font color='red'><?php t("Fatal Error"); ?>: </font>");
				
				updateSMT(jsonData);
			}
			else if (jsonData["charging"])
			{
				var ttfc = jsonData["time_to_full_charge"];
				var hour = parseInt(ttfc);
				var minute = Math.round((ttfc - hour) *60);
				var at = new Date();
				at.setMinutes(at.getMinutes() + minute);
				at.setHours(at.getHours() + hour);

				var datetime = at.toLocaleTimeString(loc, { hour: '2-digit', minute: '2-digit' });

				$('#car_statusLabel').text("<?php t("Charging"); ?>:");
				$('#car_status').html(jsonData["charger_power"] + " kW / +" + jsonData["charge_energy_added"] + " kWh<br>" +
                                        "<?php t("Done"); ?>: "+ hour +"h "+minute+"m <br><?php t("Done at"); ?>: " + datetime +  " / " + jsonData["charge_limit_soc"] +"%");

				updateSMT(jsonData);
			}
			else if (jsonData["driving"])
			{
				$('#car_statusLabel').text("<?php t("Driving"); ?>:");
				var str = "";
				if (LengthUnit == "mile")
					str = (jsonData["speed"]/ km2mls).toFixed(0) + " mph"
				else
					str = jsonData["speed"] + " <?php t("km/h"); ?>";

				if (jsonData["active_route_destination"])
				{
					var destination = encodeHTML(jsonData["active_route_destination"]);
					str += "<br>"+"To: " + destination;
					str += "<br>"+"In: " + Math.round(Number(jsonData["active_route_minutes_to_arrival"])) +  " min / " + jsonData["active_route_energy_at_arrival"]+"% SOC";

					if (jsonData["active_route_traffic_minutes_delay"] != "0.0")
					{
						str += "<br>"+"Delay:" + jsonData["active_route_traffic_minutes_delay"] + " min";
					}
				}

				$('#car_status').html(str);

				updateSMT(jsonData);
			}
			else if (jsonData["online"] && !jsonData["falling_asleep"])
			{
				var text = "<?php t("Online"); ?>";

				if (jsonData["is_preconditioning"])
					text = text + "<br><?php t("Preconditioning"); ?> " + parseFloat(jsonData["inside_temperature"]).toFixed(1) +"°C";

				if (jsonData["sentry_mode"])
					text = text + "<br><?php t("Sentry Mode"); ?>";

				if (jsonData["battery_heater"])
					text = text + "<br><?php t("Battery Heater"); ?>";

				$('#car_statusLabel').text("<?php t("Status"); ?>:");
				$('#car_status').html(text);

				updateSMT(jsonData);
			}
			else if (jsonData["sleeping"])
			{
				$('#car_statusLabel').text("<?php t("Status"); ?>:");
				$('#car_status').text("<?php t("Sleeping"); ?>");

				hideSMT();
			}
			else if (jsonData["falling_asleep"])
			{
				$('#car_statusLabel').text("<?php t("Status"); ?>:");
				$('#car_status').text("<?php t("Falling asleep"); ?>");

				hideSMT();
			}
			else
			{
				$('#car_statusLabel').text("<?php t("Status"); ?>:");
				$('#car_status').text("<?php t("Offline"); ?>");

				hideSMT();
			}

			if (LengthUnit == "mile")
			{
				$("#max_speed").text((jsonData["trip_max_speed"]/ km2mls).toFixed(0));
				$("#lt_kmh").text("mph");

				$("#trip_avg_kwh").text((jsonData["trip_avg_kwh"]* km2mls).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}));
				$("#lt_whkm").text("wh/mi");

				$("#trip_distance").text((jsonData["trip_distance"]/km2mls).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}));
				$("#lt_trip_distance_km").text("mi");
			}
			else
			{
				$("#max_speed").text(jsonData["trip_max_speed"]);

				$("#trip_avg_kwh").text(jsonData["trip_avg_kwh"].toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}));
				$("#trip_distance").text(jsonData["trip_distance"].toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}));
			}

			var ts2 = new Date(Date.parse(jsonData["trip_start_dt"]));
			$("#trip_start").text(ts2.toLocaleTimeString(loc, { day: '2-digit', month: '2-digit', year: 'numeric' }));

			$("#trip_kwh").text((Math.round(jsonData["trip_kwh"] *10)/10).toLocaleString(loc));

			var ts = new Date(Date.parse(jsonData["ts"]));
			$("#last_update").text(ts.toLocaleTimeString(loc, { day: '2-digit', month: '2-digit', year: 'numeric' }));

			var trip_duration_sec = jsonData["trip_duration_sec"];
			var min = Math.floor(trip_duration_sec / 60);
			var sec = trip_duration_sec % 60;
			if (sec < 10)
				sec = "0"+sec;

			$("#trip_duration_sec").text(min + ":" + sec);

			if (jsonData["software_update_status"].length > 0)
			{
				$("#SoftwareUpdateRow").show();
				var temp = jsonData["software_update_status"];
				temp = temp.replaceAll("_", " ");
				temp = encodeHTML(temp);

				if (jsonData["software_update_version"].length > 0)
					temp += ": " + "<a href=\"https://www.notateslaapp.com/software-updates/version/"+ jsonData["software_update_version"]+"/release-notes\">"+ jsonData["software_update_version"]+ "</a>";

				$("#software_update").html(temp);
			}
			else
			{
				$("#SoftwareUpdateRow").hide();
			}

			if (jsonData["open_windows"] > 0)
				$("#window_open").show();
			else
				$("#window_open").hide();

			if (jsonData["frunk"] > 0)
				$("#frunk_open").show();
			else
				$("#frunk_open").hide();

			if (jsonData["trunk"] > 0)
				$("#trunk_open").show();
			else
				$("#trunk_open").hide();

			if (jsonData["open_doors"] > 0)
				$("#door_open").show();
			else
				$("#door_open").hide();

			if (jsonData["locked"])
				$("#unlocked").hide();
			else
				$("#unlocked").show();

				

			var p = L.latLng(parseFloat(jsonData["latitude"]), parseFloat(jsonData["longitude"]));

			if (!mapInit)
			{
				map.setView(p, <?php echo getZoomLevel(); ?>);
				mapInit = true;
			}
			else
				map.panTo(p);

			if (marker != null)
				map.removeLayer(marker)

			var icon = new L.Icon(
				{
					iconUrl: "static/images/arrow.png",
					iconAnchor:   [10, 10],
					shadowSize: [0,0]
				}
			);

			marker = L.marker(p, {icon : icon, rotationAngle: jsonData["heading"] });
			marker.addTo(map);
		});
	}

	function encodeHTML(dirtyString) {
		var container = document.createElement('div');
		var text = document.createTextNode(dirtyString);
		container.appendChild(text);
		return container.innerHTML; // innerHTML will be a xss safe string
	}

	function hideSMT()
	{
		$('#CellTempRow').hide();
		$('#BMSMaxChargeRow').hide();
		$('#BMSMaxDischargeRow').hide();
		$('#CellImbalanceRow').hide();
	}

	function updateSMT(jsonData)
	{
		if (jsonData["SMTCellTempAvg"])
		{
			$('#CellTempRow').show();
			$('#CellTemp').text(Math.round(jsonData["SMTCellTempAvg"] * 10)/10 + "°C");
		}
		else
		{
			$('#CellTempRow').hide();
		}

		if (jsonData["SMTBMSmaxCharge"])
		{
			$('#BMSMaxChargeRow').show();
			$('#BMSMaxCharge').text( Math.round(jsonData["SMTBMSmaxCharge"]) +" kW");
		}
		else
		{
			$('#BMSMaxChargeRow').hide();
		}

		if (jsonData["SMTBMSmaxDischarge"])
		{
			$('#BMSMaxDischargeRow').show();
			$('#BMSMaxDischarge').text( Math.round(jsonData["SMTBMSmaxDischarge"]) +" kW");
		}
		else
		{
			$('#BMSMaxDischargeRow').hide();
		}

		if (jsonData["SMTCellImbalance"])
		{
			$('#CellImbalanceRow').show();
			$('#CellImbalance').text(Math.round(jsonData["SMTCellImbalance"]) +" mV");
		}
		else
		{
			$('#CellImbalanceRow').hide();
		}

	}

	<?php
	?>
  </script>

  </head>
  <body>
  <?php
    echo(menu("Teslalogger"));
?>

  <div id="content" style="max-width:1036px;">
  <div id="info">
  <table class="HeaderT">
	  <thead><td colspan="2" class="HeaderStyle"><?php t("Info"); ?></td></thead>
	  <tr><td colspan="2"><span id="InfoText"></span></td></tr>
	  <tr><td></td><td style="float:right;"><button id="NegativeButton"><?php t("No"); ?></button> <button id="PositiveButton"><?php t("Yes"); ?></button></td></tr>
  </table>
  </div>
  <div style="float:left;">
  <table class="b1 THeader">
	  <thead><td colspan="2" class="HeaderL HeaderStyle">
	  	<?php t("Car Info"); ?> <span id="displayname">- <?= $display_name ?></span>
	  		<img id="window_open" class="caricons" src="img/window_open.png" title="Open Window">
			<img id="frunk_open"class="caricons" src="img/frunk_open.png" title="Frunk Open">
			<img id="trunk_open"class="caricons" src="img/trunk_open.png" title="Trunk Open">
			<img id="door_open"class="caricons" src="img/door_open.png" title="Door Open">
			<img id="unlocked"class="caricons" src="img/unlocked.png" title="Unlocked">
		</td>
	  </thead>
	  <?php
	  	if ($carNeedFleetAPI)
	  		echo("<tr><td><font color='red'><b>".get_text("FleetAPI")."</b></font></td><td><a href='password_fleet.php?id=$carid&vin=$carVIN'>".get_text("FleetAPIRequired")." ⚠️</a></td></tr>");
		else if ($carNeedSubscription)
		{
			?>
			<!-- car need subscription -->
			<tr id="subscriptioninfo" style='display: none;'><td><font color='red'><b><?php t("Subscription") ?></b></font></td><td><a href='https://buy.stripe.com/9AQaHNdU33k29Vu144?client_reference_id=<?=$carVIN?>'><?php t("SubscriptionRequired") ?>⚠️</a><br><a href="javascript:showInfoRestricted();">Funktion eingeschränkt⚠️</a></td></tr>
			<script>
				$(document).ready(function(){
					$.ajax({
						url: "subscription-check.php?vin=<?=$carVIN?>",
					}).done(function(data) {
						if (data == "No subscription") {
							$("#subscriptioninfo").show();
						} 
                    }).fail(function(jqXHR, textStatus, errorThrown) {
                        console.error("Error: " + textStatus, errorThrown);
                    });
                });
            </script>
			<?php
		}
	
	  ?>
	  <tr><td width="130px"><b><span id="car_statusLabel"></span></b></td><td width="180px"><span id="car_status"></span></td></tr>
	  <tr id='CellTempRow'><td><b><?php t("Cell Temp"); ?>:</b></td><td><span id="CellTemp"></span></td></tr>
	  <tr id='BMSMaxChargeRow'><td><b><?php t("Max Charge"); ?>:</b></td><td><span id="BMSMaxCharge"></span></td></tr>
	  <tr id='BMSMaxDischargeRow'><td><b><?php t("Max Discharge"); ?>:</b></td><td><span id="BMSMaxDischarge"></span></td></tr>
	  <tr id='CellImbalanceRow'><td><b><?php t("Cell Imbalance"); ?>:</b></td><td><span id="CellImbalance"></span></td></tr>
	  <tr><td><b><?php t("Typical Range"); ?>:</b></td><td><span id="ideal_battery_range_km">---</span> / <span id="battery_level">---</span> %<span id="full_battery_range_km_span"><br>= <span id="full_battery_range_km">---</span> / 100 %</span>
</td></tr>
	  <tr><td><b><?php t("Odometer"); ?>:</b></td><td><span id="odometer">---</span></td></tr>
	  <tr><td><b><?php t("Car Version"); ?>:</b></td><td><a href='' id="car_version_link" target="_blank" title="Release Notes"><span id="car_version"></a></span></td></tr>
	  <tr><td><b><?php t("Last Update"); ?>:</b></td><td><span id="last_update">---</span></td></tr>
	  <tr><td><b>Teslalogger:</b></td><td><?php checkForUpdates();?></td></tr>
	  <tr id="SoftwareUpdateRow"><td><b><?php t("Software Update"); ?>:</b></td><td><span id="software_update"></span></td></tr>
<!--  </table>

  <table style="float:left;" class="THeader"> -->
	  <thead><td colspan="2" class="HeaderL HeaderStyle"><?php t("Last Trip"); ?></td></thead>
	  <tr><td width="130px"><b><?php t("Start"); ?>:</b></td><td width="180px"><span id="trip_start"></span></td></tr>
	  <tr><td><b><?php t("Duration"); ?>:</b></td><td><span id="trip_duration_sec">---</span> <?php t("min"); ?></td></tr>
	  <tr><td><b><?php t("Distance"); ?>:</b></td><td><span id="trip_distance">---</span> <span id="lt_trip_distance_km"><?php t("km"); ?></span></td></tr>
	  <tr><td><b><?php t("Consumption"); ?>:</b></td><td><span id="trip_kwh">---</span> <?php t("kWh"); ?></td></tr>
	  <tr><td><b><?php t("Ø Consumption"); ?>:</b></td><td><span id="trip_avg_kwh">---</span> <span id="lt_whkm"><?php t("Wh/km"); ?></span></td></tr>
	  <tr><td><b><?php t("Max km/h"); ?>:</b></td><td><span id="max_speed">---</span> <?php t("km/h"); ?></span> </td></tr>
  </table>
  </div>

  <table style="float:left;">
	  <thead style="background-color:#d0d0d0; color:#000000;"><td colspan="2" style="font-weight:bold;"><?php t("Current Pos"); ?></td></thead>
	  <tr><td width="680px"><div id="map" style="height: 400px; z-index:0;" /></td></tr>
  </table>

  <?php

  function checkForUpdates()
  {
	$installed = "?";

	if (file_exists("/etc/teslalogger/VERSION"))
		$installed = file_get_contents("/etc/teslalogger/VERSION");
	else
		$installed = getTeslaloggerVersion("/etc/teslalogger/git/TeslaLogger/Properties/AssemblyInfo.cs");

	if (file_exists("/etc/teslalogger/BRANCH"))
		$branch = file_get_contents("/etc/teslalogger/BRANCH");

	if (!empty($branch))
	{
		echo("<font color='red'>$branch</font>/");
		$onlineversion = getTeslaloggerVersion("https://raw.githubusercontent.com/bassmaster187/TeslaLogger/$branch/TeslaLogger/Properties/AssemblyInfo.cs");
	}
	else
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
function getZoomLevel()
{
	if (file_exists("/etc/teslalogger/settings.json"))
	{
		$content = file_get_contents("/etc/teslalogger/settings.json");
		$j = json_decode($content);
		if (!empty($j->{"ZoomLevel"}))
			return $j->{"ZoomLevel"};
	}

	return 15;
}
?>

<?PHP
  global $language;

  if (isset($language) && strlen($language) > 1 && $language != "de")
	echo(file_get_contents("https://teslalogger.de/teslalogger_content_index-".$language.".php", 0, stream_context_create(["http"=>["timeout"=>3]])));
  else
	echo(file_get_contents("https://teslalogger.de/teslalogger_content_index.php", 0, stream_context_create(["http"=>["timeout"=>3]])));

  ?>
  </div>
  <script>
	var car_inactive = <?php 
	if ($car_inactive === "1")
	 	echo "true";
	else 
		echo "false";?>;

	<?php require_once("info.php"); ?>
  </script>
  </body>
</html>
