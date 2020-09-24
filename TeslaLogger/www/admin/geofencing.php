<!DOCTYPE html>
<html lang="de">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger geofencing V1.1</title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="https://teslalogger.de/teslalogger_style.css">
	<link rel="stylesheet" href="https://unpkg.com/leaflet@1.4.0/dist/leaflet.css" integrity="sha512-puBpdR0798OZvTTbP4A8Ix/l+A4dHDD0DGqYW6RQ+9jxkRFclaxxQb/SJAWZfWAkuyeQUytO7+7N4QKrDh+drA==" crossorigin=""/>
   <!-- Make sure you put this AFTER Leaflet's CSS -->
	<script src="https://unpkg.com/leaflet@1.4.0/dist/leaflet.js" integrity="sha512-QVftwZFqvtRNi0ZyCtsznlKSWOStnDORoefr1enyq5mVL4tmKB3S/EnC3rRJcxCPavG10IcrVGSmPh6Qw5lwrg==" crossorigin=""></script>
	
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<script src="https://code.jquery.com/jquery-migrate-1.4.1.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='https://www.impala64.de/blog/tesla/wp-content/themes/twentyfourteen/genericons/genericons.css?ver=3.0.3' type='text/css' media='all' />
	<style>
	.icon {font-size:30px; color: #2b2b2b; height: 40px; margin-right: 15px;}
	</style>
	<script>
	var map = null;	
	var greenIcon = null;
	var markerArray = [];
	var circle = null;

	<?PHP
	$csv = array();
	$fp = fopen('/etc/teslalogger/geofence.csv', 'rb');
	
	while(!feof($fp)) {
		$v = fgetcsv($fp);
			
		if (count($v) >= 3)
		{
			$csv[] = array($v[0],$v[1],$v[2]);
		}
		
	}
	fclose($fp);
	
	$i = 0;
	$csv2 = array();
	if (file_exists('/etc/teslalogger/geofence-private.csv'))
	{
		$fp = fopen('/etc/teslalogger/geofence-private.csv', 'rb');
		while(!feof($fp)) {
			$v = fgetcsv($fp);
			
			if (count($v) >= 3)
			{
				$t = trim( preg_replace('/[\x00-\x1F\x80-\xFF]/', '', mb_convert_encoding( $v[0], "UTF-8" ) ) ); // remove all smileys for sorting purpose
				$radius = trim($v[3]);
				if (strlen($radius) == 0)
					$radius = "20";

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
		if  ($value[1] =="")
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
		if  ($value[1] =="")
			continue;
		
		$name = addslashes($value[0]);
		echo("im('$name',$value[1],$value[2]);\r\n");
	}
	
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
	marker.addTo(map);
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
	<body style="padding-top: 5px; padding-left: 10px;">
	<?php 
    include "menu.php";
    echo(menu("Geofencing"));
?>
<div style="max-width: 1260px;">
	<div style="height:800px; overflow: auto; float: left;">
	<table id="locations">
	<?PHP 
	$id = 0;
	usort($csv2, function ($a, $b) { return strcmp($a[4], $b[4]); });

	foreach ($csv2 as $v)
	{
		echo("<tr><td>$v[0]</td><td><a href='geoadd.php?id=$v[3]'>");
		echo('<span class="icon genericon genericon-edit" />');
		echo("</a> <a href='javascript:sf($v[1],$v[2], $v[5]);'>");
		echo('<span class="icon genericon genericon-search" />');
		echo("</a></td></tr>\n");
	}
	?>
	</table>
	</div>
	<div id="map" style="height:800px; max-width: 1260px; z-index:0;"></div>
</div>
</body>
</html>