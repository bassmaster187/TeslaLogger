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
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=17">
	<link rel="stylesheet" href="static/teslalogger_index.css?v=4">
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
	var isDockerNet8 = <?php echo (isDockerNet8() ? "true" : "false"); ?>;

	var Display100pctEnable = "<?php echo($Display100pctEnable); ?>";

	var followCar = true;		// map keeps the car centered until the user drags the map
	var lastPos = null;
	var connFailSince = null;	// time of the first failed request in a row
	var connLost = false;
	var ageFormat = null;

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

	// dragstart is only fired for user interaction, not for panTo()
	map.on('dragstart', function() { setFollowCar(false); });
	$('#follow_toggle').on('click', function() { setFollowCar(!followCar); });

	// the map card grows with its neighbours, Leaflet has to be told about the new size
	if (window.ResizeObserver)
		new ResizeObserver(function() { map.invalidateSize(); }).observe(document.getElementById('map'));

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
			connFailSince = null;
			setConnectionLost(false);

			if ("ideal_battery_range_km" in jsonData && "battery_level" in jsonData && "battery_range_km" in jsonData && "odometer" in jsonData) {
				if (LengthUnit == "mile")
				{
					if ( Range == 'IR')
					{
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
					if ( Range == 'IR')
					{
						$('#ideal_battery_range_km').text(jsonData["ideal_battery_range_km"].toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("km"); ?>");
						$('#full_battery_range_km').text((jsonData["ideal_battery_range_km"]/jsonData["battery_level"]*100).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("km"); ?>");
					}
					else
					{	
						$('#ideal_battery_range_km').text(jsonData["battery_range_km"].toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("km"); ?>");
						$('#full_battery_range_km').text((jsonData["battery_range_km"]/jsonData["battery_level"]*100).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("km"); ?>");
					}
					$('#odometer').text((jsonData["odometer"]).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("km"); ?>");
				}
			}
			else if ("odometer" in jsonData) {
				if (LengthUnit == "mile")
				{
					$('#odometer').text((jsonData["odometer"] / km2mls).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("mi"); ?>");
				}
				else
				{
					$('#odometer').text((jsonData["odometer"]).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " <?php t("km"); ?>");
				}
			}

			if (Display100pctEnable == "true")
			{
				$('#full_battery_range_km_span').show();
			}
			else
			{
				$('#full_battery_range_km_span').hide();
			}

			$('#battery_level').text(Number(jsonData["battery_level"]).toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}));
			if (jsonData["car_version"]) {
				var car_version = jsonData["car_version"];

				var hashpos = car_version.lastIndexOf(" ");
				if (hashpos > 0)
					car_version = car_version.substring(0, hashpos);
				
				$('#car_version').text(car_version);
				$('#car_version_link').attr("href", "https://www.notateslaapp.com/software-updates/version/"+ car_version +"/release-notes");
			}

			var isCharging = false;

			if (car_inactive)
			{
				setCarStatus("inactive", "<?php t("Inactive"); ?>", "");
				hideSMT();
			}
			else if (jsonData["FatalError"])
			{
				// FatalError may contain a link (HTML) from Teslalogger
				var errortext = jsonData["FatalError"];

				if (jsonData["FatalError"] == "missing_key")
				{
					errortext = "<?php t("FatalErrorMissingKey"); ?>";
					errortext = errortext.replace("{LINK}", '<a href="https://www.tesla.com/_ak/teslalogger.de" target="_blank">LINK</a>');
				}
				else if (jsonData["FatalError"] == "max_configs")
				{
					errortext = "<?php t("FatalErrorMaxConfigs"); ?>";
				}

				setCarStatus("error", "<?php t("Fatal Error"); ?>", errortext);

				updateSMT(jsonData);
			}
			else if (jsonData["charging"])
			{
				isCharging = true;

				var ttfc = jsonData["time_to_full_charge"];
				var hour = parseInt(ttfc);
				var minute = Math.round((ttfc - hour) *60);
				var at = new Date();
				at.setMinutes(at.getMinutes() + minute);
				at.setHours(at.getHours() + hour);

				var datetime = at.toLocaleTimeString(loc, { hour: '2-digit', minute: '2-digit' });

				setCarStatus("charging", "<?php t("Charging"); ?>",
					jsonData["charger_power"] + " kW / +" + jsonData["charge_energy_added"] + " kWh<br>" +
					"<?php t("Done"); ?>: "+ hour +"h "+minute+"m <br><?php t("Done at"); ?>: " + datetime +  " / " + jsonData["charge_limit_soc"] +"%");

				updateSMT(jsonData);
			}
			else if (jsonData["driving"])
			{
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

				setCarStatus("driving", "<?php t("Driving"); ?>", str);

				updateSMT(jsonData);
			}
			else if (jsonData["online"] && !jsonData["falling_asleep"])
			{
				var lines = [];

				if (jsonData["is_preconditioning"])
				{
					var text = "<?php t("Preconditioning"); ?> ";
					if (TemperatureUnit == "fahrenheit")
					{
						text += (parseFloat(jsonData["inside_temperature"]) * 9/5 + 32).toFixed(1) + " °F";
					}
					else
					{
						text += parseFloat(jsonData["inside_temperature"]).toFixed(1) + " °C";
					}
					lines.push(text);
				}

				if (jsonData["sentry_mode"])
					lines.push("<?php t("Sentry Mode"); ?>");

				if (jsonData["battery_heater"])
					lines.push("<?php t("Battery Heater"); ?>");

				setCarStatus("online", "<?php t("Online"); ?>", lines.join("<br>"));

				updateSMT(jsonData);
			}
			else if (jsonData["sleeping"])
			{
				setCarStatus("asleep", "<?php t("Sleeping"); ?>", "");

				hideSMT();
			}
			else if (jsonData["falling_asleep"])
			{
				setCarStatus("asleep", "<?php t("Falling asleep"); ?>", "");

				hideSMT();
			}
			else
			{
				setCarStatus("offline", "<?php t("Offline"); ?>", "");

				hideSMT();
			}

			updateBattery(jsonData, isCharging);

			if (jsonData["trip_max_speed"] && jsonData["trip_avg_kwh"] && jsonData["trip_distance"]) {
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
			}

			var ts2 = new Date(Date.parse(jsonData["trip_start_dt"]));
			$("#trip_start").text(ts2.toLocaleTimeString(loc, { day: '2-digit', month: '2-digit', year: 'numeric' }));

			$("#trip_kwh").text((Math.round(jsonData["trip_kwh"] *10)/10).toLocaleString(loc));

			var ts = new Date(Date.parse(jsonData["ts"]));
			$("#last_update").text(ts.toLocaleTimeString(loc, { day: '2-digit', month: '2-digit', year: 'numeric' }));
			$("#last_update_age").text(formatAge(ts));

			var trip_duration_sec = jsonData["trip_duration_sec"];
			var min = Math.floor(trip_duration_sec / 60);
			var sec = trip_duration_sec % 60;
			if (sec < 10)
				sec = "0"+sec;

			$("#trip_duration_sec").text(min + ":" + sec);

			// Ø speed = distance / duration, derived from the current trip data
			if (trip_duration_sec > 0 && jsonData["trip_distance"] > 0)
			{
				var avg_speed = jsonData["trip_distance"] * 3600 / trip_duration_sec;

				if (LengthUnit == "mile")
				{
					avg_speed /= km2mls;
					$("#lt_kmh_avg").text("mph");
				}

				$("#trip_avg_speed").text(avg_speed.toFixed(0));
			}
			else
				$("#trip_avg_speed").text("---");

			if (jsonData["software_update_status"] && jsonData["software_update_status"].length > 0)
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
			lastPos = p;

			if (!mapInit)
			{
				map.setView(p, <?php echo getZoomLevel(); ?>);
				mapInit = true;
			}
			else if (followCar)
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
		}).fail(function() {
			// a single failed request is ignored, the hint appears after 3 seconds without a valid answer
			if (connFailSince === null)
				connFailSince = Date.now();
			else if (Date.now() - connFailSince >= 3000)
				setConnectionLost(true);
		});
	}

	function setCarStatus(state, label, detailsHtml)
	{
		$('#car_statusLabel').text(label).attr('data-state', state);
		$('#car_status').html(detailsHtml);
	}

	function updateBattery(jsonData, isCharging)
	{
		var level = Number(jsonData["battery_level"]);
		if (!("battery_level" in jsonData) || !isFinite(level))
			return;

		level = Math.max(0, Math.min(100, level));

		$('#battery_fill').css('width', level + '%');
		$('#battery_bar').attr('aria-valuenow', Math.round(level))
			.toggleClass('is-critical', level <= 10)
			.toggleClass('is-low', level > 10 && level <= 20);

		var limit = Number(jsonData["charge_limit_soc"]);
		if (isCharging && limit > 0 && limit <= 100)
			$('#charge_limit_marker').css('left', limit + '%').show();
		else
			$('#charge_limit_marker').hide();
	}

	function setFollowCar(follow)
	{
		followCar = follow;
		$('#follow_toggle').attr('aria-checked', follow ? 'true' : 'false');

		if (follow && mapInit && lastPos != null)
			map.panTo(lastPos);
	}

	function setConnectionLost(lost)
	{
		if (lost == connLost)
			return;

		connLost = lost;
		$('#tl_grid').toggleClass('is-stale', lost);

		if (lost)
			$('#conn_error').show();
		else
			$('#conn_error').hide();
	}

	// "5 minutes ago" in the browser language, empty if the browser can't do it
	function formatAge(date)
	{
		if (isNaN(date.getTime()) || !(window.Intl && Intl.RelativeTimeFormat))
			return "";

		if (ageFormat == null)
			ageFormat = new Intl.RelativeTimeFormat(loc, { numeric: "auto" });

		var sec = Math.max(0, Math.round((Date.now() - date.getTime()) / 1000));

		if (sec < 60)
			return "(" + ageFormat.format(0, "second") + ")";
		if (sec < 3600)
			return "(" + ageFormat.format(-Math.floor(sec / 60), "minute") + ")";
		if (sec < 86400)
			return "(" + ageFormat.format(-Math.floor(sec / 3600), "hour") + ")";

		return "(" + ageFormat.format(-Math.floor(sec / 86400), "day") + ")";
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
			if (TemperatureUnit == "fahrenheit")
			{
				$('#CellTemp').text(Math.round((jsonData["SMTCellTempAvg"] * 9/5 + 32) * 10)/10 + "°F");
			}
			else
			{
				$('#CellTemp').text(Math.round(jsonData["SMTCellTempAvg"] * 10)/10 + "°C");
			}
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
	$teslaloggername = "Teslalogger";
	if (isDockerNet8())
		$teslaloggername = "Teslalogger Docker NET8";
	else if (isDocker())
		$teslaloggername = "Teslalogger <span style=color:red>Docker MONO</span>";
	else if (isRaspberryNET8())
	{
		$teslaloggername = "Teslalogger NET8";
	}
	else
	{
		exec('lsb_release -cs', $out);
		if (strpos($out[0], 'buster') !== false)
			$teslaloggername = "Teslalogger <span style=color:red>MONO</span>"; // Buster with MONO
		else
			$teslaloggername = "Teslalogger <span style=color:red>MONO old</span>"; // old Raspberry with MONO (stretch)
	}

    echo(menu($teslaloggername));
?>

  <div id="content" class="tl-page">
  <div id="info">
	<div class="HeaderT tl-card tl-info">
		<h2 class="tl-card-title"><?php t("Info"); ?></h2>
		<div id="InfoText"></div>
		<div class="tl-info-actions"><button id="NegativeButton"><?php t("No"); ?></button> <button id="PositiveButton"><?php t("Yes"); ?></button></div>
	</div>
  </div>

  <div id="conn_error" class="tl-alert tl-alert-danger" role="alert" style="display: none;"><?php t("No connection to Teslalogger"); ?></div>

  <div id="tl_grid" class="tl-grid">
	<section class="tl-card tl-hero">
	  <?php
	  	if ($carNeedFleetAPI)
	  		echo("<div class='tl-alert tl-alert-danger'><b>".get_text("FleetAPI")."</b><a href='password_fleet.php?id=$carid&vin=$carVIN'>".get_text("FleetAPIRequired")." ⚠️</a></div>");
		else if ($carNeedSubscription)
		{
			?>
			<!-- car need subscription -->
			<div id="subscriptioninfo" class="tl-alert tl-alert-danger" style='display: none;'><b><?php t("Subscription") ?></b><a href='https://buy.stripe.com/9AQaHNdU33k29Vu144?client_reference_id=<?=$carVIN?>'><?php t("SubscriptionRequired") ?>⚠️</a><a href="javascript:showInfoRestricted();">Funktion eingeschränkt⚠️</a></div>
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
		<div class="tl-hero-head">
			<h2 class="tl-hero-name"><?= htmlspecialchars($display_name ?? "") ?></h2>
			<div class="tl-caricons">
				<img id="window_open" class="caricons" src="img/window_open.png" alt="Open Window" title="Open Window">
				<img id="frunk_open" class="caricons" src="img/frunk_open.png" alt="Frunk Open" title="Frunk Open">
				<img id="trunk_open" class="caricons" src="img/trunk_open.png" alt="Trunk Open" title="Trunk Open">
				<img id="door_open" class="caricons" src="img/door_open.png" alt="Door Open" title="Door Open">
				<img id="unlocked" class="caricons" src="img/unlocked.png" alt="Unlocked" title="Unlocked">
			</div>
		</div>
		<div class="tl-hero-status">
			<span id="car_statusLabel" class="tl-badge"></span>
			<span id="car_status" class="tl-status-text"></span>
		</div>
		<div class="tl-hero-numbers">
			<div class="tl-soc"><span id="battery_level">---</span><span class="tl-soc-unit">%</span></div>
			<div class="tl-range">
				<span class="tl-range-value" id="ideal_battery_range_km">---</span>
				<span class="tl-range-label"><?php t("Typical Range"); ?><span id="full_battery_range_km_span" style="display: none;"> = <span id="full_battery_range_km">---</span> / 100 %</span></span>
			</div>
		</div>
		<div id="battery_bar" class="tl-battery" role="progressbar" aria-label="<?php t("SOC"); ?>" aria-valuemin="0" aria-valuemax="100">
			<div id="battery_fill" class="tl-battery-fill"></div>
			<div id="charge_limit_marker" class="tl-battery-limit" style="display: none;"></div>
		</div>
	</section>

	<section class="tl-card tl-details">
		<h2 class="tl-card-title"><?php t("Car Info"); ?></h2>
		<dl class="tl-list">
			<div class="tl-row"><dt><?php t("Odometer"); ?></dt><dd><span id="odometer">---</span></dd></div>
			<div class="tl-row"><dt><?php t("Car Version"); ?></dt><dd><a href='' id="car_version_link" target="_blank" title="Release Notes"><span id="car_version"></span></a></dd></div>
			<div class="tl-row" id="SoftwareUpdateRow" style="display: none;"><dt><?php t("Software Update"); ?></dt><dd><span id="software_update"></span></dd></div>
			<div class="tl-row"><dt><?php t("Last Update"); ?></dt><dd><span id="last_update">---</span> <span id="last_update_age" class="tl-muted"></span></dd></div>
			<div class="tl-row"><dt>Teslalogger</dt><dd><?php checkForUpdates();?></dd></div>
			<div class="tl-row" id='CellTempRow' style="display: none;"><dt><?php t("Cell Temp"); ?></dt><dd><span id="CellTemp"></span></dd></div>
			<div class="tl-row" id='BMSMaxChargeRow' style="display: none;"><dt><?php t("Max Charge"); ?></dt><dd><span id="BMSMaxCharge"></span></dd></div>
			<div class="tl-row" id='BMSMaxDischargeRow' style="display: none;"><dt><?php t("Max Discharge"); ?></dt><dd><span id="BMSMaxDischarge"></span></dd></div>
			<div class="tl-row" id='CellImbalanceRow' style="display: none;"><dt><?php t("Cell Imbalance"); ?></dt><dd><span id="CellImbalance"></span></dd></div>
		</dl>
	</section>

	<section class="tl-card tl-trip">
		<div class="tl-card-head">
			<h2 class="tl-card-title"><?php t("Last Trip"); ?></h2>
			<span class="tl-muted tl-trip-start"><?php t("Start"); ?>: <span id="trip_start"></span></span>
		</div>
		<div class="tl-stats">
			<div><span class="tl-stat-value"><span id="trip_distance">---</span> <span class="tl-stat-unit" id="lt_trip_distance_km"><?php t("km"); ?></span></span><span class="tl-stat-label"><?php t("Distance"); ?></span></div>
			<div><span class="tl-stat-value"><span id="trip_duration_sec">---</span> <span class="tl-stat-unit"><?php t("min"); ?></span></span><span class="tl-stat-label"><?php t("Duration"); ?></span></div>
			<div><span class="tl-stat-value"><span id="trip_kwh">---</span> <span class="tl-stat-unit"><?php t("kWh"); ?></span></span><span class="tl-stat-label"><?php t("Consumption"); ?></span></div>
			<div><span class="tl-stat-value"><span id="trip_avg_kwh">---</span> <span class="tl-stat-unit" id="lt_whkm"><?php t("Wh/km"); ?></span></span><span class="tl-stat-label"><?php t("Ø Consumption"); ?></span></div>
			<div><span class="tl-stat-value"><span id="max_speed">---</span> <span class="tl-stat-unit" id='lt_kmh'><?php t("km/h"); ?></span></span><span class="tl-stat-label"><?php t("Max km/h"); ?></span></div>
			<div><span class="tl-stat-value"><span id="trip_avg_speed">---</span> <span class="tl-stat-unit" id="lt_kmh_avg"><?php t("km/h"); ?></span></span><span class="tl-stat-label"><?php t("Ø Speed"); ?></span></div>
		</div>
	</section>

	<section class="tl-card tl-map-card">
		<div class="tl-card-head">
			<h2 class="tl-card-title"><?php t("Current Pos"); ?></h2>
			<button type="button" id="follow_toggle" class="tl-switch" role="switch" aria-checked="true"><span class="tl-switch-track"></span><?php t("Follow vehicle"); ?></button>
		</div>
		<div id="map"></div>
	</section>
  </div>

  <?php

  function checkForUpdates()
  {
	$installed = "?";

	if (file_exists("/etc/teslalogger/VERSION"))
		$installed = file_get_contents("/etc/teslalogger/VERSION");
	else
		$installed = getTeslaloggerVersion("/etc/teslalogger/git/TeslaLogger/Properties/AssemblyInfo.cs");

	if (empty($installed))
		$installed = GetFromTeslalogger("getversion");

	if (file_exists("/etc/teslalogger/BRANCH"))
		$branch = file_get_contents("/etc/teslalogger/BRANCH");

	if (!empty($branch))
	{
		echo("<font color='red'>$branch</font>/");
		$onlineversion = getTeslaloggerVersion("https://raw.githubusercontent.com/bassmaster187/TeslaLogger/$branch/TeslaLogger/Properties/AssemblyInfo.cs");
	}
	else
	{
		if (isDockerNET8())
			$onlineversion = file_get_contents("https://teslalogger.de/latest_teslalogger_docker_version.txt");
		else
			$onlineversion = getTeslaloggerVersion("https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/Properties/AssemblyInfo.cs");
	}

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
	if (file_exists("/tmp/settings.json"))
	{
		$content = file_get_contents("/tmp/settings.json");
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
	$remoteinfo = file_get_contents("https://teslalogger.de/teslalogger_content_index-".$language.".php", 0, stream_context_create(["http"=>["timeout"=>3]]));
  else
	$remoteinfo = file_get_contents("https://teslalogger.de/teslalogger_content_index.php", 0, stream_context_create(["http"=>["timeout"=>3]]));

  // no empty card if teslalogger.de is not reachable
  if (!empty($remoteinfo))
  {
	// the remote file starts with a UTF-8 BOM, which would show up as an empty line at the top of the card
	if (substr($remoteinfo, 0, 3) === "\xEF\xBB\xBF")
		$remoteinfo = substr($remoteinfo, 3);

	echo("<section class=\"tl-card tl-remote\">".$remoteinfo."</section>");
  }

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
