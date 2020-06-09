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
	<script>	
	<?PHP
	$csv = array();
	$fp = fopen('/etc/teslalogger/geofence.csv', 'rb');
	while(!feof($fp)) {
		$csv[] = fgetcsv($fp);
	}
	fclose($fp);
	
	$csv2 = array();
	if (file_exists('/etc/teslalogger/geofence-private.csv'))
	{
		$fp = fopen('/etc/teslalogger/geofence-private.csv', 'rb');
		while(!feof($fp)) {
			$csv2[] = fgetcsv($fp);
		}
		fclose($fp);
	}
	?>	
  $( function() {
  var map = new L.Map('map');
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
	
	var greenIcon = L.icon({iconUrl: 'img/marker-icon-green.png', shadowUrl: 'https://unpkg.com/leaflet@1.4.0/dist/images/marker-shadow.png', iconAnchor:   [12, 40], popupAnchor:  [0, -25]});
	var markerArray = [];
	<?PHP
	
	echo("<!-- Start geofence private -->\r\n");
	$inserted = false;
	foreach ($csv2 as $value)
	{
		if  ($value[1] =="")
			continue;
			
		echo("var markerLocation = new L.LatLng($value[1], $value[2]);\r\n");
		echo("var marker = new L.Marker(markerLocation, {icon: greenIcon});\r\n");
		echo("markerArray.push(marker);\r\n");
		echo("marker.bindPopup('".addslashes($value[0])."');\r\n");
		echo("marker.addTo(map);\r\n");
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
			
		echo("var markerLocation = new L.LatLng($value[1], $value[2]);\r\n");
		echo("var marker = new L.Marker(markerLocation);\r\n");
		echo("marker.bindPopup('".addslashes($value[0])."');\r\n");
		echo("marker.addTo(map);\r\n");
	}
	
	if (!$inserted)
		echo("map.setView(new L.LatLng(50, 8),6);");
	?>
    
  });
  </script>
	</head>
	<body style="padding-top: 5px; padding-left: 10px;">
	<?php 
    include "menu.php";
    echo(menu("Geofencing"));
?>
	<div id="map" style="height:800px; max-width: 1260px; z-index:0;"></div>
</body>
</html>