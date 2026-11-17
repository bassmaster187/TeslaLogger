<!DOCTYPE html>
<?php
require_once("language.php");
?>
<html lang="de">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger <?php t("Geofence"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=6">
	<link rel="stylesheet" href="static/teslalogger_geofencing.css?v=5">
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
	var map = null;	
	var greenIcon = null;
	var markerArray = [];
	var circle = null;
	var markers = L.markerClusterGroup();

	<?PHP
	$csv = array();
	
	
	GetFileFromTeslaloggerAndWriteToTMP("geofence-private.csv");
	GetFileFromTeslaloggerAndWriteToTMP("geofence.csv");

	if (file_exists('/tmp/geofence.csv'))
	{
		$fp = fopen('/tmp/geofence.csv', 'rb');
		
		while(!feof($fp)) {
			$v = fgetcsv($fp);
				
			if ($v != FALSE && count($v) >= 3)
			{
				$csv[] = array($v[0],$v[1],$v[2]);
			}
			
		}
		fclose($fp);
	}
	
	$i = 0;
	$csv2 = array();
	if (file_exists('/tmp/geofence-private.csv'))
	{
		$fp = fopen('/tmp/geofence-private.csv', 'rb');
		while(!feof($fp)) {
			$v = fgetcsv($fp);
			
			if ($v != FALSE && count($v) >= 3)
			{
				$t = trim( preg_replace('/[\x00-\x1F\x80-\xFF]/', '', mb_convert_encoding( $v[0], "UTF-8" ) ) ); // remove all smileys for sorting purpose
				$radius = "20";

				if (count($v) > 3)
				{
					$radius = trim($v[3]);
					if (strlen($radius) == 0)
						$radius = "20";
				}
				$csv2[] = array($v[0],$v[1],$v[2],$i, $t, $radius);
			}
			$i++;
		}
		fclose($fp);
	}
	?>	
  $( function() {
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
	
	greenIcon = L.icon({iconUrl: 'img/marker-icon-green.png', shadowUrl: 'https://unpkg.com/leaflet@1.4.0/dist/images/marker-shadow.png', iconAnchor:   [12, 40], popupAnchor:  [0, -25]});	
	<?PHP
	
	echo("<!-- Start geofence private -->\r\n");
	$inserted = false;
	foreach ($csv2 as $value)
	{
		if  ($value[1] =="" || floatval($value[1]) == 0 || floatval($value[2]) == 0)
			continue;
		
		$name = addslashes($value[0]);
		echo("ig('$name',$value[1],$value[2]);\r\n");
		$inserted = true;
	}
	if ($inserted)
	{
		echo("var group = L.featureGroup(markerArray);\r\n");
		echo("map.fitBounds(group.getBounds());\r\n\r\n");
	}
	
	echo("<!-- Start geofence -->\r\n");
	foreach ($csv as $value)
	{
		if  ($value[1] =="" || floatval($value[1]) == 0 || floatval($value[2]) == 0)
			continue;

		$name = addslashes($value[0]);
		echo("im('$name',$value[1],$value[2]);\r\n");
	}

	echo "map.addLayer(markers);\r\n";
	
	if (!$inserted)
		echo("map.setView(new L.LatLng(50, 8),6);");
	?>
    
  });
function ig(name, lat, lng)
{
	var markerLocation = new L.LatLng(lat, lng);
	var marker = new L.Marker(markerLocation, {icon: greenIcon});
	markerArray.push(marker);
	marker.bindPopup(name);
	marker.addTo(map);
}

function im(name, lat, lng)
{
	var markerLocation = new L.LatLng(lat, lng);
	var marker = new L.Marker(markerLocation);
	marker.bindPopup(name);
	//marker.addTo(map);
	markers.addLayer(marker); 
}

function sf(lat, lng, radius)
{
	if (circle != null)
		map.removeLayer(circle);

	var markerLocation = new L.LatLng(lat, lng);
	map.setView(new L.LatLng(lat, lng),17);
	circle = new L.circle(markerLocation, {radius: radius});
	circle.addTo(map);
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
		<h2 class="tl-page-title"><?php t("Geofence"); ?></h2>
	</div>
	<div class="tl-geo">
		<div class="tl-card tl-geo-list">
			<div class="tl-card-head">
				<h2 class="tl-card-title"><?php t("Geofence"); ?></h2>
				<?PHP
				$helpUrl = ($language == "de") ? "https://teslalogger.de/docs/extras/geofence/" : "https://teslalogger.de/en/docs/extras/geofence/";
				?>
				<a href="<?= $helpUrl ?>" target="_blank"><img src="img/icon-help-24.png" class="tl-help" /></a>
			</div>
			<?PHP
			usort($csv2, function ($a, $b) { return strcmp($a[4], $b[4]); });
			?>
			<ul class="tl-geo-items">
			<?PHP
			foreach ($csv2 as $v)
			{
				$name = htmlspecialchars($v[0]);
				$lat = $v[1];
				$lng = $v[2];
				$radius = $v[5];
				$id = $v[3];
				?>
				<li>
					<span class="tl-geo-name"><?= $name ?></span>
					<span class="tl-geo-actions">
						<a href='geoadd.php?id=<?= $id ?>'><span class="genericon genericon-edit" /></a>
						<a href='javascript:sf(<?= $lat ?>,<?= $lng ?>, <?= $radius ?>);'><span class="genericon genericon-search" /></a>
					</span>
				</li>
				<?PHP
			}
			?>
			</ul>
			<button type="button" class="tl-geo-new-btn" onclick="window.location.href='geoadd.php';"><?php t("New Geofence"); ?></button>
		</div>
		<div class="tl-card tl-geo-map">
			<div id="map"></div>
		</div>
	</div>
</div>
</body>
</html>