<!DOCTYPE html>
<html lang="de">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger geofencing V1.0</title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="http://teslalogger.de/teslalogger_style.css">
	<link rel="stylesheet" href="https://unpkg.com/leaflet@1.4.0/dist/leaflet.css" integrity="sha512-puBpdR0798OZvTTbP4A8Ix/l+A4dHDD0DGqYW6RQ+9jxkRFclaxxQb/SJAWZfWAkuyeQUytO7+7N4QKrDh+drA==" crossorigin=""/>
   <!-- Make sure you put this AFTER Leaflet's CSS -->
	<script src="https://unpkg.com/leaflet@1.4.0/dist/leaflet.js" integrity="sha512-QVftwZFqvtRNi0ZyCtsznlKSWOStnDORoefr1enyq5mVL4tmKB3S/EnC3rRJcxCPavG10IcrVGSmPh6Qw5lwrg==" crossorigin=""></script>
	
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<script>
	<?PHP
	$csv = array();
	$fp = fopen('/etc/teslalogger/geofence.csv', 'rb');
	while(!feof($fp)) {
		$csv[] = fgetcsv($fp);
	}
	fclose($fp);
	
	$csv2 = array();
	$fp = fopen('/etc/teslalogger/geofence-private.csv', 'rb');
	while(!feof($fp)) {
		$csv2[] = fgetcsv($fp);
	}
	fclose($fp);
	?>	
  $( function() {
  var map = new L.Map('map');
  var osmUrl='https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
	var osmAttrib='Map data © <a href="https://openstreetmap.org">OpenStreetMap</a> contributors';
	var osm = new L.TileLayer(osmUrl, {minZoom: 3, maxZoom: 12, attribution: osmAttrib});		
	map.setView(new L.LatLng(50, 8),6);
	map.addLayer(osm);
	
	<?PHP
	foreach ($csv as $value)
	{
		if  ($value[1] =="")
			continue;
			
		echo("var markerLocation = new L.LatLng($value[1], $value[2]);\r\n");
		echo("var marker = new L.Marker(markerLocation);\r\n");
		echo("marker.bindPopup('$value[0]');\r\n");
		echo("marker.addTo(map);\r\n");
	}
	
	foreach ($csv2 as $value)
	{
		if  ($value[1] =="")
			continue;
			
		echo("var markerLocation = new L.LatLng($value[1], $value[2]);\r\n");
		echo("var marker = new L.Marker(markerLocation);\r\n");
		echo("marker.bindPopup('$value[0]');\r\n");
		echo("marker.addTo(map);\r\n");
	}
	?>
    
  });
  </script>
	</head>
	<body>
	<div id="map" style="height:800px;"></div>
</body>
</html>