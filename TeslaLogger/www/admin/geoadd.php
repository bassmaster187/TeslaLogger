<!DOCTYPE html>
<?php
require_once("language.php");
$lat = isset($_REQUEST["lat"]) ? $_REQUEST["lat"] : "";
$lng = isset($_REQUEST["lng"]) ? $_REQUEST["lng"] : "";
$radius = 20.0;
$poiname = "";
$id = null;
$csv = null;
$sf = null;

$isnew = !isset($_REQUEST["id"]);
if (isset($_REQUEST["id"]))
{
	$id = $_REQUEST["id"];

	$n = 0;
	GetFileFromTeslaloggerAndWriteToTMP("geofence-private.csv");
	$fp = fopen("/tmp/geofence-private.csv", "r+");

	if ($fp === FALSE)
	{
		echo("Error open geofence-private.csv");
		return;
	}

	while ($line = fgets ($fp)) {
		// echo("Line : $n : $line <br>");
		if ($n == $id)
		{
			//echo($line)
			$csv = explode(",", $line);
			$lat = trim($csv[1]);
			$lng = trim($csv[2]);
			$poiname = trim($csv[0]);

			if (isset($csv[3]) && strlen(trim($csv[3])) > 0)
				$radius = trim($csv[3]);

			if (isset($csv[4]) && strlen(trim($csv[4])) > 0)
				$sf = explode("+", trim($csv[4]));

			break;
		}

		$n++;
	}
	fclose($fp);
}

?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger <?php t("Geofence"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=3">
	<link rel="stylesheet" href="static/teslalogger_geofencing.css?v=3">
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
	<link rel="stylesheet" href="static/leaflet/1.4.0/MarkerCluster.css" />
   	<link rel="stylesheet" href="static/leaflet/1.4.0/MarkerCluster.Default.css" />

    <!-- Make sure you put this AFTER Leaflet's CSS -->
	<script src="static/leaflet/1.4.0/leaflet.js"></script>
	<script src="static/leaflet/1.4.0/leaflet.markercluster.js"></script>
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
	<script>
	var circle = null;
	var map = null;
	var lat = <?= json_encode($lat) ?>;
	var lng = <?= json_encode($lng) ?>;
	var radius = <?= json_encode($radius) ?>;
	var isnew = <?= $isnew ? "true" : "false" ?>;

	function createMap(centerLat, centerLng, zoom)
	{
		map = new L.Map('map', {center: [centerLat, centerLng], zoom: zoom});
		// Define layers and add them to the control widget
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
				// This map doesn't have labels so we force a label-only layer on top of it
				forcedOverlay: L.tileLayer('https://stamen-tiles-{s}.a.ssl.fastly.net/toner-labels/{z}/{x}/{y}.png', {
					attribution: 'Labels by <a href="http://stamen.com">Stamen Design</a>, <a href="http://creativecommons.org/licenses/by-3.0">CC BY 3.0</a> &mdash; Map data &copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>',
					subdomains: 'abcd',
					maxZoom: 20,
				})
			})
		}).addTo(map);

		// Create a circle for the geofence
		circle = new L.circle([centerLat, centerLng], {radius: radius, fillColor: '#24890d', fillOpacity: 0.15, color: '#24890d', weight: 2});
		circle.addTo(map);
		circle.on({
			mousedown: function () {
				map.on('mousemove', function (e) {
					map.dragging.disable();
					circle.setLatLng(e.latlng);
				});
			}
		});
		map.on('mouseup', function () {
			map.removeEventListener('mousemove');
			map.dragging.enable();
		});
	}

	$(function() {
		$("input").change(function(){ OnSpecialFlagsChanged(); });
		$("select").change(function(){ OnSpecialFlagsChanged(); });

		$("#radius").on("change paste keyup", function() {
			if (circle != null)
				circle.setRadius($("#radius").val());
		});

		<?PHP
		if ($sf != null)
			echo("var sf = ". json_encode($sf).";\n");
		else
			echo('var sf = [""];');
		echo("\n")
		?>

		sf.forEach(function(e) {
			if (e.startsWith("home"))
				$("#home").attr('checked', 'checked');
			else if (e.startsWith("work"))
				$("#work").attr('checked', 'checked');
			else if (e.startsWith("charger"))
				$("#charger").attr('checked', 'checked');
			else if (e.startsWith("ccp"))
				$("#ccp").attr('checked', 'checked');
			else if (e.startsWith("scl"))
			{
				$("#scl").attr('checked', 'checked');
				var limit = e.substring(4);
				var lpos = limit.indexOf(":");
				if (lpos > 0)
				{
					var sclend = limit.substring(lpos);
					limit = limit.substring(0, lpos);
					if (sclend.startsWith(":A"))
						$("#scl_soa").attr('checked', 'checked');
				}
				$("#scl_limit").val(limit);
			}
			else if (e.startsWith("ocp"))
			{
				$("#ocp").attr('checked', 'checked');
				if (e.length == 0)
					$("#ocp_gear").val("DR->P");
				else
					$("#ocp_gear").val(e.substring(4));
			}
			else if (e.startsWith("hfl"))
			{
				$("#hfl").attr('checked', 'checked');
				if (e.substring(4).includes("m"))
				{
					$("#hfl_minutes").val(e.substring(4));
					$("#hfl_count").prop("disabled", true);
				}
				else
				{
					$("#hfl_count").val(e.substring(4));
					$("#hfl_minutes").prop("disabled", true);
				}
			}
			else if (e.startsWith("esm"))
			{
				$("#esm").attr('checked', 'checked');
				if (e.substring(4).length == 0)
					$("#esm_gear").val("DR->P");
				else
					$("#esm_gear").val(e.substring(4));
			}
			else if (e.startsWith("dsm"))
			{
				$("#dsm").attr('checked', 'checked');
				if (e.substring(4).length == 0)
					$("#dsm_gear").val("DR->P");
				else
					$("#dsm_gear").val(e.substring(4));
			}
			else if (e.startsWith("cof"))
			{
				$("#cof").attr('checked', 'checked');
				if (e.substring(4).length == 0)
					$("#cof_gear").val("DR->P");
				else
					$("#cof_gear").val(e.substring(4));
			}
			else if (e.startsWith("nosleep"))
				$("#nosleep").attr('checked', 'checked');
			else if (e.startsWith("dnc"))
				$("#dnc").attr('checked', 'checked');
			else if (e.startsWith("occ"))
			{
				$("#occ").attr('checked', 'checked');
				if (e.substring(4).length == 0)
					$("#occ_limit").val("75");
				else
					$("#occ_limit").val(e.substring(4));
			}

			OnSpecialFlagsChanged();
		});

		// New geofence: start at the browser position, otherwise at the stored position.
		// Create the map immediately at a fallback, then recenter when the location resolves.
		var fallbackLat = lat != "" ? lat : 50;
		var fallbackLng = lng != "" ? lng : 8;
		var fallbackZoom = isnew ? 6 : 18;
		createMap(fallbackLat, fallbackLng, fallbackZoom);

		if (isnew && typeof navigator !== 'undefined' && navigator.geolocation)
		{
			navigator.geolocation.getCurrentPosition(
				function(pos) {
					map.setView([pos.coords.latitude, pos.coords.longitude], 16);
				},
				function() {
					// Fallback if the location is denied or unavailable: zoom in
					// enough that the 20m circle is visible
					map.setZoom(16);
				},
				{ enableHighAccuracy: true }
			);
		}
	});

	function OnSpecialFlagsChanged()
	{
		var f = "";
		if ($("#home").is(':checked'))
			f += "+home";

		if ($("#work").is(':checked'))
			f += "+work";

		if ($("#charger").is(':checked'))
			f += "+charger";

		if ($("#ccp").is(':checked'))
			f += "+ccp";

		if ($("#dnc").is(':checked'))
			f += "+dnc";

		if ($("#scl").is(':checked'))
		{
			f += "+scl:";
			if ($("#scl_limit").val().length == 0)
				f += "100";
			else
				f += $("#scl_limit").val();

			if ($("#scl_soa").is(':checked'))
				f += ":A";
		}

		if ($("#occ").is(':checked'))
		{
			f += "+occ:";
			if ($("#occ_limit").val().length == 0)
				f += "75";
			else
				f += $("#occ_limit").val();
		}

		if ($("#ocp").is(':checked'))
			f += "+ocp:"+$("#ocp_gear").val();

		if ($("#hfl_minutes").val().length > 0)
			$("#hfl_count").prop("disabled", true);
		else if ($("#hfl_count").val().length > 0)
			$("#hfl_minutes").prop("disabled", true);
		else
		{
			$("#hfl_count").prop("disabled", false);
			$("#hfl_minutes").prop("disabled", false);
		}

		if ($("#hfl").is(':checked'))
		{
			f += "+hfl:";
			if ($("#hfl_minutes").val().length > 0)
				f += $("#hfl_minutes").val() + "m";
			else if ($("#hfl_count").val().length > 0)
				f += $("#hfl_count").val().replace("c","") + "c";
			else
				f += "100c";
		}

		if ($("#esm").is(':checked'))
			f += "+esm:"+$("#esm_gear").val();

		if ($("#dsm").is(':checked'))
			f += "+dsm:"+$("#dsm_gear").val();

		if ($("#cof").is(':checked'))
			f += "+cof:"+$("#cof_gear").val();
		if ($("#nosleep").is(':checked'))
			f += "+nosleep";

		$("#flag").val(f);
	}

	function del()
	{
		if (confirm('Are you sure you want to delete this geofencing location?')) {
			if (!$("#text").val())
			{
				alert("<?php t('Error'); ?>");
				return;
			}

			$.post("geoadd_write.php",
			{
				delete: "yes",
			<?PHP
				if (isset($id))
					echo("id: $id");
				?>
			}).always(function() {
				alert("<?php t('Deleted!'); ?>");
				location.href = document.referrer;
			});
		}
	}

	function save()
	{
		if (!$("#text").val())
		{
			alert("<?php t('Error'); ?>");
			return;
		}

		if (circle == null)
			return;

		$.post("geoadd_write.php",
		{
			Text: $("#text").val(),
			lat: circle.getLatLng().lat,
			lng: circle.getLatLng().lng,
			radius: circle.getRadius(),
			flag: $("#flag").val(),
		<?PHP
			if (isset($id))
				echo("id: $id");
			?>
		}).always(function() {
			alert("<?php t('Saved!'); ?>");
			location.href = document.referrer;
		});
	}
	</script>
	</head>
	<body>
<?php
    include "menu.php";
    echo(menu("Geofencing"));
?>
<div class="tl-page">
	<div class="tl-page-head">
		<h2 class="tl-page-title"><?php t($isnew ? "New Geofence" : "Edit Geofence"); ?></h2>
		<?php
		$helpUrl = ($language == "de") ? "https://teslalogger.de/docs/extras/geofence/" : "https://teslalogger.de/en/docs/extras/geofence/";
		?>
		<a href="<?= $helpUrl ?>" target="_blank"><img src="img/icon-help-24.png" class="tl-help" /></a>
	</div>
	<div class="tl-geo">
		<div class="tl-card tl-geo-form">
			<h2 class="tl-card-title"><?php t("Name & Position"); ?></h2>
			<div class="tl-geo-field">
				<label><?php t("Description"); ?></label>
				<input id="text" value="<?= htmlspecialchars($poiname) ?>"/>
			</div>
			<div class="tl-geo-field">
				<label><?php t("Radius"); ?></label>
				<input id="radius" value="<?= intval($radius) ?>" type="number"/>
			</div>

			<h2 class="tl-card-title"><?php t("Special Flags"); ?></h2>
			<div class="tl-geo-flag-group">
				<h4 class="tl-geo-flag-group-title"><?php t("Type"); ?></h4>
				<label class="tl-geo-checkbox">🏠 <?php t("Home"); ?> <input id="home" type="checkbox" value="home" /></label>
				<label class="tl-geo-checkbox">💼 <?php t("Work"); ?> <input id="work" type="checkbox" value="work" /></label>
				<label class="tl-geo-checkbox">🔌 <?php t("Charger"); ?> <input id="charger" type="checkbox" value="charger" name="type" /></label>
			</div>
			<div class="tl-geo-flag-group">
				<h4 class="tl-geo-flag-group-title"><?php t("Charging"); ?></h4>
				<label class="tl-geo-checkbox"><?php t("Copy Charging Costs"); ?> <input id="ccp" type="checkbox" value="" name="type" /></label>
				<label class="tl-geo-checkbox"><?php t("Don't Combine Charging Sessions"); ?> <input id="dnc" type="checkbox" value="" name="type" /></label>
				<div class="tl-geo-row">
					<label class="tl-geo-checkbox"><?php t("Set Charge Limit"); ?> <input id="scl" type="checkbox" value=""/></label>
					<?php t("SOC"); ?> <input size="6" id="scl_limit" placeholder="100"/>%
					<input id="scl_soa" type="checkbox" value=""/> <?php t("Set on arrival"); ?>
				</div>
				<div class="tl-geo-row">
					<label class="tl-geo-checkbox"><?php t("Set Charge Limit After Charging"); ?> <input id="occ" type="checkbox" value=""/></label>
					<?php t("SOC"); ?> <input size="6" id="occ_limit" placeholder="75"/>%
				</div>
				<div class="tl-geo-row">
					<label class="tl-geo-checkbox"><?php t("Open Charge Port"); ?> <input id="ocp" type="checkbox" value=""/></label>
					<?php t("Gear"); ?>
					<select id="ocp_gear">
						<option value="DR->P"><?php t("D/R → P"); ?></option>
						<option value="D->P"><?php t("D → P"); ?></option>
						<option value="R->P"><?php t("R → P"); ?></option>
					</select>
				</div>
				<div class="tl-geo-row">
					<label class="tl-geo-checkbox"><?php t("High Frequency Logging"); ?> <input id="hfl" type="checkbox" value=""/></label>
					<?php t("Duration"); ?> <input size="6" id="hfl_minutes"/><?php t("Minutes"); ?>
					<input size="6" id="hfl_count" placeholder="100"/><?php t("Count"); ?>
				</div>
			</div>
			<div class="tl-geo-flag-group">
				<h4 class="tl-geo-flag-group-title"><?php t("Special Features"); ?></h4>
				<div class="tl-geo-row">
					<label class="tl-geo-checkbox"><?php t("Enable Sentry Mode"); ?> <input id="esm" type="checkbox" value="" name="type" /></label>
					<?php t("Gear"); ?>
					<select id="esm_gear">
						<option value="DR->P"><?php t("D/R → P"); ?></option>
						<option value="D->P"><?php t("D → P"); ?></option>
						<option value="R->P"><?php t("R → P"); ?></option>
					</select>
				</div>
				<div class="tl-geo-row">
					<label class="tl-geo-checkbox"><?php t("Disable Sentry Mode"); ?> <input id="dsm" type="checkbox" value="" name="type" /></label>
					<?php t("Gear"); ?>
					<select id="dsm_gear">
						<option value="DR->P"><?php t("D/R → P"); ?></option>
						<option value="D->P"><?php t("D → P"); ?></option>
						<option value="R->P"><?php t("R → P"); ?></option>
					</select>
				</div>
				<div class="tl-geo-row">
					<label class="tl-geo-checkbox"><?php t("Turn HVAC off"); ?> <input id="cof" type="checkbox" value="" name="type" /></label>
					<?php t("Gear"); ?>
					<select id="cof_gear">
						<option value="DR->P"><?php t("D/R → P"); ?></option>
						<option value="D->P"><?php t("D → P"); ?></option>
						<option value="R->P"><?php t("R → P"); ?></option>
					</select>
				</div>
				<div class="tl-geo-row">
					<label class="tl-geo-checkbox"><?php t("No sleep"); ?> <input id="nosleep" type="checkbox" value="" name="type" /></label>
				</div>
			</div>
			<input style="width:100%" id="flag" disabled/>
			<div class="tl-actions tl-geo-actions-btn">
				<button id="btn_save" onclick="save();"><?php t("Save"); ?></button>
				<?php if (!$isnew): ?>
				<button id="btn_delete" onclick="del();" class="redbutton"><?php t("Delete"); ?></button>
				<?php endif; ?>
			</div>
		</div>
		<div class="tl-card tl-geo-map">
			<div id="map"></div>
		</div>
	</div>
</div>
</body>
</html>
