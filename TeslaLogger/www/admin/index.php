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

$tlVersion = getTeslaloggerVersionInfo();
?>
<!DOCTYPE html>
<html lang="<?php echo isset($json_data["Language"]) ? $json_data["Language"] : "en"; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="apple-mobile-web-app-capable" content="yes">
    <meta name="apple-mobile-web-app-title" content="Teslalogger Config">
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="theme-color" content="#0b0f14">
	<link rel="icon" type="image/png" href="img/apple-touch-icon.png" sizes="131x133">
	<link rel="icon" type="image/png" href="img/apple-touch-icon-192.png" sizes="192x192">
    <link rel="apple-touch-icon" href="img/apple-touch-icon.png">
    <title>Teslalogger</title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/cockpit.css?v=1">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
   <!-- Make sure you put this AFTER Leaflet's CSS -->
	<script src="static/leaflet/1.4.0/leaflet.js"></script>
	<script src="static/leaflet/1.4.0/leaflet.rotatedMarker.js"></script>
	<script>
	var km2mls = 1.609344;
	var map = null;
	var marker = null;
	var markerLatLng = null;
	var mapInit = false;
	var loc;
	var LengthUnit = "<?php echo($LengthUnit); ?>";
	var TemperatureUnit = "<?php echo($TemperatureUnit); ?>";
	var PowerUnit = "<?php echo($PowerUnit); ?>";
	var Range  = "<?php echo ($Range); ?>";
	var isDockerNet8 = <?php echo (isDockerNet8() ? "true" : "false"); ?>;
	var Display100pctEnable = "<?php echo($Display100pctEnable); ?>";
	var car_inactive = <?php echo ((strval($car_inactive) === "1") ? "true" : "false"); ?>;

	var perfEntries = performance.getEntriesByType("navigation");
	if (perfEntries && perfEntries.length > 0 && perfEntries[0].type === "back_forward") {
		location.reload();
	}

	function ceil0(v) { return Math.round(Number(v)); }

	function nf(v, min, max)
	{
		return Number(v).toLocaleString(loc, {maximumFractionDigits: max, minimumFractionDigits: min});
	}

	function fullRangeText(rangeValue, batteryLevel, miles)
	{
		var bl = parseFloat(batteryLevel);
		var rv = parseFloat(rangeValue);

		if (!isFinite(bl) || bl <= 0 || !isFinite(rv))
			return "---";

		var v = rv / bl * 100;
		if (miles)
			v = v / km2mls;

		return nf(v, 1, 1) + (miles ? " <?php t("mi"); ?>" : " <?php t("km"); ?>");
	}

	function setText(id, txt)
	{
		var e = document.getElementById(id);
		if (e)
			e.textContent = txt;
	}

	function setHtml(id, html)
	{
		var e = document.getElementById(id);
		if (e)
			e.innerHTML = html;
	}

	function setRow(id, show)
	{
		var e = document.getElementById(id);
		if (e)
			e.hidden = !show;
	}

	function setFlag(id, show)
	{
		var e = document.getElementById(id);
		if (e)
			e.classList.toggle('is-on', !!show);
	}

	function setStatus(kind, badgeText, detailHtml)
	{
		var badge = document.getElementById('statusBadge');
		if (badge)
		{
			badge.className = 'tl-badge tl-badge--' + kind;
			var bt = badge.querySelector('.tl-badge__text');
			if (bt)
				bt.textContent = badgeText;
		}

		var detail = document.getElementById('car_status');
		if (detail)
		{
			detail.innerHTML = detailHtml;
			detail.classList.toggle('is-error', kind === 'error');
		}
	}

	$( function() {
		CreateMap();
		GetCurrentData();

		if (Display100pctEnable == "true")
			document.getElementById('full_battery_range_km_span').hidden = false;
		else
			document.getElementById('full_battery_range_km_span').hidden = true;

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

	function CreateMap()
	{
		map = new L.Map('map');
		map.setView([0, 0], 2);

		L.control.layers({
			'OpenStreetMap': L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
				attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>',
				maxZoom: 19
			}).addTo(map),
			'OpenTopoMap': L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
				attribution: 'Map data: &copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)',
				maxZoom: 17
			}),
			'Satellite': L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
				attribution: 'Imagery &copy; Esri &mdash; Source: Esri, i-cubed, USDA, USGS, AEX, GeoEye, Getmapping, Aerogrid, IGN, IGP, UPR-EGP, and the GIS User Community',
				forcedOverlay: L.tileLayer('https://stamen-tiles-{s}.a.ssl.fastly.net/toner-labels/{z}/{x}/{y}.png', {
					attribution: 'Labels by <a href="http://stamen.com">Stamen Design</a>, <a href="http://creativecommons.org/licenses/by/3.0">CC BY 3.0</a> &mdash; Map data &copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>',
					subdomains: 'abcd',
					maxZoom: 20,
				})
			})
		}).addTo(map);

		if (navigator.languages != undefined) loc = navigator.languages[0];
		else loc = navigator.language;
	}

	function GetCurrentData()
	{
		$.ajax({
		  url: "current_json.php",
		  dataType: "json"
		  }).done(function( jsonData ) {
			if ("ideal_battery_range_km" in jsonData && "battery_level" in jsonData && "battery_range_km" in jsonData && "odometer" in jsonData) {
				if (LengthUnit == "mile")
				{
					if ( Range == 'IR' )
					{
						setText('ideal_battery_range_km', nf(jsonData["ideal_battery_range_km"] / km2mls, 1, 1) + " <?php t("mi"); ?>");
						setText('full_battery_range_km', fullRangeText(jsonData["ideal_battery_range_km"], jsonData["battery_level"], true));
					}
					else
					{
						setText('ideal_battery_range_km', nf(jsonData["battery_range_km"] / km2mls, 1, 1) + " <?php t("mi"); ?>");
						setText('full_battery_range_km', fullRangeText(jsonData["battery_range_km"], jsonData["battery_level"], true));
					}
					setText('odometer', nf(jsonData["odometer"] / km2mls, 1, 1) + " <?php t("mi"); ?>");
				}
				else
				{
					if ( Range == 'IR' )
					{
						setText('ideal_battery_range_km', nf(jsonData["ideal_battery_range_km"], 1, 1) + " <?php t("km"); ?>");
						setText('full_battery_range_km', fullRangeText(jsonData["ideal_battery_range_km"], jsonData["battery_level"], false));
					}
					else
					{
						setText('ideal_battery_range_km', nf(jsonData["battery_range_km"], 1, 1) + " <?php t("km"); ?>");
						setText('full_battery_range_km', fullRangeText(jsonData["battery_range_km"], jsonData["battery_level"], false));
					}
					setText('odometer', nf(jsonData["odometer"], 1, 1) + " <?php t("km"); ?>");
				}
			}
			else if ("odometer" in jsonData) {
				if (LengthUnit == "mile")
					setText('odometer', nf(jsonData["odometer"] / km2mls, 1, 1) + " <?php t("mi"); ?>");
				else
					setText('odometer', nf(jsonData["odometer"], 1, 1) + " <?php t("km"); ?>");
			}

			if ("battery_level" in jsonData)
				setText('battery_level', jsonData["battery_level"]);

			if (jsonData["car_version"]) {
				var car_version = jsonData["car_version"];
				var hashpos = car_version.lastIndexOf(" ");
				if (hashpos > 0)
					car_version = car_version.substring(0, hashpos);

				setText('car_version', car_version);

				var versionLink = document.getElementById('car_version_link');
				if (versionLink)
				{
					versionLink.setAttribute("href", "https://www.notateslaapp.com/software-updates/version/"+ encodeURIComponent(car_version) +"/release-notes");
					versionLink.setAttribute("data-ready", "1");
				}
			}

			if (car_inactive)
			{
				setStatus('error', "<?php t("Inactive"); ?>", '' );
				hideSMT();
			}
			else if (jsonData["FatalError"])
			{
				if (jsonData["FatalError"] == "missing_key")
				{
					var missingkeytext = "<?php t("FatalErrorMissingKey"); ?>";
					missingkeytext = missingkeytext.replace("{LINK}", '<a href="https://www.tesla.com/_ak/teslalogger.de" target="_blank" rel="noopener">LINK</a>');

					setStatus('error', "<?php t("Fatal Error"); ?>", '<span class="tl-error">'+missingkeytext+'</span>');
				}
				else
				{
					setStatus('error', "<?php t("Fatal Error"); ?>", '<span class="tl-error">'+ encodeHTML(jsonData["FatalError"]) +'</span>');
				}

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

				setStatus('charging', "<?php t("Charging"); ?>",
					jsonData["charger_power"] + " kW &middot; +" + jsonData["charge_energy_added"] + " kWh<br>" +
					"<?php t("Done"); ?>: "+ hour +"h "+minute+"m<br>" +
					"<?php t("Done at"); ?>: " + datetime + " &middot; " + jsonData["charge_limit_soc"] + "%"
				);

				updateSMT(jsonData);
			}
			else if (jsonData["driving"])
			{
				var str = "";
				if (LengthUnit == "mile")
					str = (jsonData["speed"]/ km2mls).toFixed(0) + " mph";
				else
					str = jsonData["speed"] + " <?php t("km/h"); ?>";

				if (jsonData["active_route_destination"])
				{
					var destination = encodeHTML(jsonData["active_route_destination"]);
					str += "<br>" + "To: " + destination;
					str += "<br>" + "In: " + Math.round(Number(jsonData["active_route_minutes_to_arrival"])) +  " min &middot; " + jsonData["active_route_energy_at_arrival"]+"% SOC";

					if (jsonData["active_route_traffic_minutes_delay"] != "0.0")
						str += "<br>" + "Delay: " + jsonData["active_route_traffic_minutes_delay"] + " min";
				}

				setStatus('driving', "<?php t("Driving"); ?>", str);
				updateSMT(jsonData);
			}
			else if (jsonData["online"] && !jsonData["falling_asleep"])
			{
				var text = "";

				if (jsonData["is_preconditioning"])
				{
					text += "<?php t("Preconditioning"); ?> ";
					if (TemperatureUnit == "fahrenheit")
						text += (parseFloat(jsonData["inside_temperature"]) * 9/5 + 32).toFixed(1) + " °F<br>";
					else
						text += parseFloat(jsonData["inside_temperature"]).toFixed(1) + " °C<br>";
				}

				if (jsonData["sentry_mode"])
					text = text + "<?php t("Sentry Mode"); ?><br>";

				if (jsonData["battery_heater"])
					text = text + "<?php t("Battery Heater"); ?>";

				setStatus('online', "<?php t("Online"); ?>", text);
				updateSMT(jsonData);
			}
			else if (jsonData["sleeping"])
			{
				setStatus('sleep', "<?php t("Sleeping"); ?>", '' );
				hideSMT();
			}
			else if (jsonData["falling_asleep"])
			{
				setStatus('sleep', "<?php t("Falling asleep"); ?>", '' );
				hideSMT();
			}
			else
			{
				setStatus('offline', "<?php t("Offline"); ?>", '' );
				hideSMT();
			}

			if (jsonData["trip_max_speed"] && jsonData["trip_avg_kwh"] && jsonData["trip_distance"]) {
				if (LengthUnit == "mile")
				{
					setText('max_speed', (jsonData["trip_max_speed"]/ km2mls).toFixed(0));
					setText('lt_kmh', "mph");
					setText('trip_avg_kwh', nf(jsonData["trip_avg_kwh"]* km2mls, 1, 1));
					setText('lt_whkm', "wh/mi");
					setText('trip_distance', nf(jsonData["trip_distance"]/km2mls, 1, 1));
					setText('lt_trip_distance_km', "mi");
				}
				else
				{
					setText('max_speed', jsonData["trip_max_speed"]);
					setText('trip_avg_kwh', nf(jsonData["trip_avg_kwh"], 1, 1));
					setText('trip_distance', nf(jsonData["trip_distance"], 1, 1));
				}
			}

			if (jsonData["trip_start_dt"]) {
				var ts2 = new Date(Date.parse(jsonData["trip_start_dt"]));
				if (isFinite(ts2.getTime()) && ts2.getFullYear() > 1900)
					setText('trip_start', ts2.toLocaleDateString(loc, { day: '2-digit', month: '2-digit', year: 'numeric' }));
				else
					setText('trip_start', "---");
			}

			if (typeof jsonData["trip_kwh"] !== "undefined" && jsonData["trip_kwh"] !== null)
				setText('trip_kwh', nf(Math.round(jsonData["trip_kwh"] *10)/10, 0, 0));

			if (jsonData["ts"]) {
				var ts = new Date(Date.parse(jsonData["ts"]));
				if (isFinite(ts.getTime()) && ts.getFullYear() > 1900)
					setText('last_update', ts.toLocaleString(loc, { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }));
				else
					setText('last_update', "---");
			}

			if (typeof jsonData["trip_duration_sec"] !== "undefined" && jsonData["trip_duration_sec"] !== null) {
				var trip_duration_sec = jsonData["trip_duration_sec"];
				var min = Math.floor(trip_duration_sec / 60);
				var sec = trip_duration_sec % 60;
				if (sec < 10)
					sec = "0"+sec;

				setText('trip_duration_sec', min + ":" + sec);
			}

			if (jsonData["software_update_status"] && jsonData["software_update_status"].length > 0)
			{
				var temp = jsonData["software_update_status"];
				temp = temp.replaceAll("_", " ");
				temp = encodeHTML(temp);

				if (jsonData["software_update_version"] && jsonData["software_update_version"].length > 0)
					temp += ": " + "<a href=\"https://www.notateslaapp.com/software-updates/version/"+ encodeURIComponent(jsonData["software_update_version"])+"/release-notes\" target=\"_blank\" rel=\"noopener\">"+ encodeHTML(jsonData["software_update_version"])+ "</a>";

				setHtml('software_update', temp);
				setRow('SoftwareUpdateRow', true);
			}
			else
			{
				setRow('SoftwareUpdateRow', false);
			}

			setFlag('window_open', jsonData["open_windows"] > 0);
			setFlag('frunk_open', jsonData["frunk"] > 0);
			setFlag('trunk_open', jsonData["trunk"] > 0);
			setFlag('door_open', jsonData["open_doors"] > 0);

			if ("locked" in jsonData)
				setFlag('unlocked', !jsonData["locked"]);
			else
				setFlag('unlocked', false);

			UpdateMap(jsonData);
		});
	}

	function UpdateMap(jsonData)
	{
		if (!map)
			return;

		var lat = parseFloat(jsonData["latitude"]);
		var lng = parseFloat(jsonData["longitude"]);

		if (!isFinite(lat) || !isFinite(lng))
			return;

		var p = L.latLng(lat, lng);

		if (!mapInit)
		{
			map.setView(p, <?php echo getZoomLevel(); ?>);
			mapInit = true;
		}

		var icon = new L.Icon(
			{
				iconUrl: "static/images/arrow.png",
				iconAnchor:   [10, 10],
				shadowSize: [0,0]
			}
		);

		var heading = parseFloat(jsonData["heading"]) || 0;

		if (marker == null)
		{
			marker = L.marker(p, { icon: icon, rotationAngle: heading, keyboard: false });
			marker.addTo(map);
			markerLatLng = p;
		}
		else
		{
			marker.setLatLng(p);
			marker.setRotationAngle(heading);

			if (!markerLatLng || markerLatLng.distanceTo(p) > 2)
			{
				map.panTo(p);
				markerLatLng = p;
			}
		}
	}

	function encodeHTML(dirtyString) {
		var container = document.createElement('div');
		var text = document.createTextNode(dirtyString);
		container.appendChild(text);
		return container.innerHTML; // innerHTML will be a xss safe string
	}

	function hideSMT()
	{
		setRow('CellTempRow', false);
		setRow('BMSMaxChargeRow', false);
		setRow('BMSMaxDischargeRow', false);
		setRow('CellImbalanceRow', false);
	}

	function updateSMT(jsonData)
	{
		if (jsonData["SMTCellTempAvg"])
		{
			if (TemperatureUnit == "fahrenheit")
				setText('CellTemp', Math.round((jsonData["SMTCellTempAvg"] * 9/5 + 32) * 10)/10 + "°F");
			else
				setText('CellTemp', Math.round(jsonData["SMTCellTempAvg"] * 10)/10 + "°C");

			setRow('CellTempRow', true);
		}
		else
		{
			setRow('CellTempRow', false);
		}

		if (jsonData["SMTBMSmaxCharge"])
		{
			setText('BMSMaxCharge', Math.round(jsonData["SMTBMSmaxCharge"]) +" kW");
			setRow('BMSMaxChargeRow', true);
		}
		else
		{
			setRow('BMSMaxChargeRow', false);
		}

		if (jsonData["SMTBMSmaxDischarge"])
		{
			setText('BMSMaxDischarge', Math.round(jsonData["SMTBMSmaxDischarge"]) +" kW");
			setRow('BMSMaxDischargeRow', true);
		}
		else
		{
			setRow('BMSMaxDischargeRow', false);
		}

		if (jsonData["SMTCellImbalance"])
		{
			setText('CellImbalance', Math.round(jsonData["SMTCellImbalance"]) +" mV");
			setRow('CellImbalanceRow', true);
		}
		else
		{
			setRow('CellImbalanceRow', false);
		}
	}
  </script>
  </head>
  <body class="tl-page">
  <?php
	$teslaloggername = "Teslalogger";
	if (isDockerNet8())
		$teslaloggername = "Teslalogger Docker NET8";
	else if (isDocker())
		$teslaloggername = "Teslalogger <span class='tl-mono-version'>Docker MONO</span>";
	else if (isRaspberryNET8())
	{
		$teslaloggername = "Teslalogger NET8";
	}
	else
	{
		exec('lsb_release -cs', $out);
		$codename = isset($out[0]) ? $out[0] : "";
		if (strpos($codename, 'buster') !== false)
			$teslaloggername = "Teslalogger <span class='tl-mono-version'>MONO</span>";
		else
			$teslaloggername = "Teslalogger <span class='tl-mono-version'>MONO old</span>";
	}

    echo(menu($teslaloggername));
?>

  <main class="tl-app">

	<section id="info" class="tl-banner HeaderT" role="alert" aria-live="polite">
		<div class="tl-banner__inner">
			<div id="InfoText" class="tl-banner__text"></div>
			<div class="tl-banner__actions">
				<button type="button" id="NegativeButton" class="tl-btn tl-btn--ghost"><?php t("No"); ?></button>
				<button type="button" id="PositiveButton" class="tl-btn tl-btn--primary"><?php t("Yes"); ?></button>
			</div>
		</div>
	</section>

	<?php if ($carNeedFleetAPI || $carNeedSubscription): ?>
	<div class="tl-alerts">
		<?php if ($carNeedFleetAPI): ?>
		<div class="tl-alert tl-alert--danger" role="alert">
			<div class="tl-alert__body">
				<strong><?php echo get_text("FleetAPI"); ?></strong>
				<span><?php echo get_text("FleetAPIRequired"); ?></span>
			</div>
			<a class="tl-btn tl-btn--danger" href="password_fleet.php?id=<?= intval($carid) ?>&amp;vin=<?= rawurlencode(strval($carVIN)) ?>"><?php t("Credentials"); ?></a>
		</div>
		<?php elseif ($carNeedSubscription): ?>
		<div id="subscriptioninfo" class="tl-alert tl-alert--warn" role="status" hidden>
			<div class="tl-alert__body">
				<strong><?php t("Subscription"); ?></strong>
				<span><?php t("SubscriptionRequired"); ?></span>
				<a href="javascript:showInfoRestricted();"><?php t("Details"); ?></a>
			</div>
			<a class="tl-btn tl-btn--warn" href="https://buy.stripe.com/9AQaHNdU33k29Vu144?client_reference_id=<?= rawurlencode(strval($carVIN)) ?>"><?php t("Subscribe"); ?></a>
		</div>
		<script>
		(function () {
			var el = document.getElementById('subscriptioninfo');
			if (!el)
				return;

			$.ajax({ url: "subscription-check.php?vin=<?= rawurlencode(strval($carVIN)) ?>" })
				.done(function (data) {
					if (String(data).trim() === "No subscription")
						el.hidden = false;
				})
				.fail(function (jqXHR, textStatus, errorThrown) {
					console.error("Error: " + textStatus, errorThrown);
				});
		})();
		</script>
		<?php endif; ?>
	</div>
	<?php endif; ?>

	<section class="tl-hero" aria-labelledby="tlCarName">
		<div class="tl-hero__main">
			<div class="tl-identity">
				<div class="tl-identity__text">
					<h1 class="tl-car-name" id="tlCarName"><?= htmlspecialchars(strval($display_name), ENT_QUOTES) ?></h1>
					<div class="tl-status">
						<span id="statusBadge" class="tl-badge tl-badge--offline" aria-live="polite"><span class="tl-badge__dot" aria-hidden="true"></span><span class="tl-badge__text"><?php t("Offline"); ?></span></span>
						<div class="tl-status__detail">
							<div id="car_status" class="tl-status__value" aria-live="polite">---</div>
						</div>
					</div>
				</div>
				<div class="tl-flags" id="carFlags">
					<span class="tl-flag" id="window_open" title="Open Window"><img src="img/window_open.png" alt="Open Window"></span>
					<span class="tl-flag" id="frunk_open" title="Frunk Open"><img src="img/frunk_open.png" alt="Frunk Open"></span>
					<span class="tl-flag" id="trunk_open" title="Trunk Open"><img src="img/trunk_open.png" alt="Trunk Open"></span>
					<span class="tl-flag" id="door_open" title="Door Open"><img src="img/door_open.png" alt="Door Open"></span>
					<span class="tl-flag" id="unlocked" title="Unlocked"><img src="img/unlocked.png" alt="Unlocked"></span>
				</div>
			</div>

			<div class="tl-kpis">
				<div class="tl-kpi tl-kpi--primary">
					<span class="tl-kpi__label"><?php t("SOC"); ?></span>
					<div class="tl-kpi__value"><span id="battery_level">--</span><span class="tl-kpi__unit">%</span></div>
				</div>
				<div class="tl-kpi">
					<span class="tl-kpi__label"><?php t("Typical Range"); ?></span>
					<div class="tl-kpi__value tl-kpi__value--sm"><span id="ideal_battery_range_km">---</span></div>
					<span class="tl-kpi__hint" id="full_battery_range_km_span" hidden>100%: <span id="full_battery_range_km">---</span></span>
				</div>
				<div class="tl-kpi">
					<span class="tl-kpi__label"><?php t("Odometer"); ?></span>
					<div class="tl-kpi__value tl-kpi__value--sm"><span id="odometer">---</span></div>
				</div>
				<div class="tl-kpi">
					<span class="tl-kpi__label"><?php t("Last Update"); ?></span>
					<div class="tl-kpi__value tl-kpi__value--sm"><span id="last_update">---</span></div>
				</div>
			</div>
		</div>

		<aside class="tl-card tl-mapcard">
			<h2 class="tl-card__title"><?php t("Current Pos"); ?></h2>
			<div class="tl-map"><div id="map"></div></div>
		</aside>
	</section>

	<div class="tl-grid">
		<section class="tl-card" aria-labelledby="cardCarInfo">
			<h2 class="tl-card__title" id="cardCarInfo"><?php t("Car Info"); ?></h2>

			<details class="tl-details" id="bmsDetails">
				<summary>BMS</summary>
				<dl class="tl-kv tl-kv--compact">
					<div class="tl-kv__row" id="CellTempRow" hidden>
						<dt><?php t("Cell Temp"); ?></dt>
						<dd><span id="CellTemp"></span></dd>
					</div>
					<div class="tl-kv__row" id="BMSMaxChargeRow" hidden>
						<dt><?php t("Max Charge"); ?></dt>
						<dd><span id="BMSMaxCharge"></span></dd>
					</div>
					<div class="tl-kv__row" id="BMSMaxDischargeRow" hidden>
						<dt><?php t("Max Discharge"); ?></dt>
						<dd><span id="BMSMaxDischarge"></span></dd>
					</div>
					<div class="tl-kv__row" id="CellImbalanceRow" hidden>
						<dt><?php t("Cell Imbalance"); ?></dt>
						<dd><span id="CellImbalance"></span></dd>
					</div>
				</dl>
			</details>

			<dl class="tl-kv">
				<div class="tl-kv__row">
					<dt><?php t("Car Version"); ?></dt>
					<dd><a href="#" id="car_version_link" target="_blank" rel="noopener" title="Release Notes"><span id="car_version">---</span></a></dd>
				</div>
				<div class="tl-kv__row" id="SoftwareUpdateRow" hidden>
					<dt><?php t("Software Update"); ?></dt>
					<dd><span id="software_update"></span></dd>
				</div>
				<div class="tl-kv__row">
					<dt>Teslalogger</dt>
					<dd class="tl-version">
						<span class="tl-version__installed"><?= htmlspecialchars($tlVersion['installed']) ?></span>
						<?php if (!empty($tlVersion['branch'])): ?>
							<span class="tl-chip tl-chip--branch"><?= htmlspecialchars($tlVersion['branch']) ?></span>
						<?php endif; ?>
						<?php if ($tlVersion['update']): ?>
							<span class="tl-chip tl-chip--update">Update <?= htmlspecialchars($tlVersion['online']) ?></span>
						<?php endif; ?>
					</dd>
				</div>
			</dl>
		</section>

		<section class="tl-card" aria-labelledby="cardLastTrip">
			<h2 class="tl-card__title" id="cardLastTrip"><?php t("Last Trip"); ?></h2>
			<dl class="tl-kv">
				<div class="tl-kv__row">
					<dt><?php t("Start"); ?></dt>
					<dd><span id="trip_start">---</span></dd>
				</div>
				<div class="tl-kv__row">
					<dt><?php t("Duration"); ?></dt>
					<dd><span id="trip_duration_sec">---</span></dd>
				</div>
				<div class="tl-kv__row">
					<dt><?php t("Distance"); ?></dt>
					<dd><span id="trip_distance">---</span> <span id="lt_trip_distance_km"><?php t("km"); ?></span></dd>
				</div>
				<div class="tl-kv__row">
					<dt><?php t("Consumption"); ?></dt>
					<dd><span id="trip_kwh">---</span> <?php t("kWh"); ?></dd>
				</div>
				<div class="tl-kv__row">
					<dt><?php t("Ø Consumption"); ?></dt>
					<dd><span id="trip_avg_kwh">---</span> <span id="lt_whkm"><?php t("Wh/km"); ?></span></dd>
				</div>
				<div class="tl-kv__row">
					<dt><?php t("Max km/h"); ?></dt>
					<dd><span id="max_speed">---</span> <span id="lt_kmh"><?php t("km/h"); ?></span></dd>
				</div>
			</dl>
		</section>
	</div>

	<?php
	$remoteContent = false;
	if (isset($language) && strlen($language) > 1 && $language != "de")
	{
		$remoteContent = @file_get_contents("https://teslalogger.de/teslalogger_content_index-".$language.".php", false, stream_context_create(["http"=>["timeout"=>3]]));
	}
	else
	{
		$remoteContent = @file_get_contents("https://teslalogger.de/teslalogger_content_index.php", false, stream_context_create(["http"=>["timeout"=>3]]));
	}

	if (!empty($remoteContent))
	{
		?>
		<section class="tl-card tl-remote">
			<?php echo $remoteContent; ?>
		</section>
		<?php
	}
	?>

  </main>

  <script>
	<?php require_once("info.php"); ?>
  </script>
  </body>
</html>
<?php

function getTeslaloggerVersionInfo()
{
	$info = [
		"installed" => "?",
		"online" => "",
		"branch" => "",
		"update" => false
	];

	if (file_exists("/etc/teslalogger/VERSION"))
		$info["installed"] = trim(strval(file_get_contents("/etc/teslalogger/VERSION")));
	else
		$info["installed"] = strval(getTeslaloggerVersion("/etc/teslalogger/git/TeslaLogger/Properties/AssemblyInfo.cs"));

	if (empty($info["installed"]))
		$info["installed"] = strval(GetFromTeslalogger("getversion"));

	if (file_exists("/etc/teslalogger/BRANCH"))
		$info["branch"] = trim(strval(file_get_contents("/etc/teslalogger/BRANCH")));

	if (!empty($info["branch"]))
		$info["online"] = strval(getTeslaloggerVersion("https://raw.githubusercontent.com/bassmaster187/TeslaLogger/" . $info["branch"] . "/TeslaLogger/Properties/AssemblyInfo.cs"));
	else if (isDockerNET8())
		$info["online"] = trim(strval(@file_get_contents("https://teslalogger.de/latest_teslalogger_docker_version.txt")));
	else
		$info["online"] = strval(getTeslaloggerVersion("https://raw.githubusercontent.com/bassmaster187/TeslaLogger/master/TeslaLogger/Properties/AssemblyInfo.cs"));

	if (!empty($info["online"]) && $info["installed"] != $info["online"])
		$info["update"] = true;

	return $info;
}

function getTeslaloggerVersion($path)
{
	$f = @file_get_contents($path);
	if ($f === false)
		return "";

	preg_match('/AssemblyVersion\(\"([0-9\.]+)\"/', $f, $matches);
	return isset($matches[1]) ? $matches[1] : "";
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
