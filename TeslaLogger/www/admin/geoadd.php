<!DOCTYPE html>
<?php
require("language.php");
$lat = $_REQUEST["lat"];
$lng = $_REQUEST["lng"];
?>
<html lang="<?php echo $json_data["Language"]; ?>"">
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
	<script>	
	var circle = null;
  $( function() {
  var map = new L.Map('map', {center: [<?= $lat ?>,<?= $lng ?>], zoom:18});
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
	
	$("button").button();
	$("#radius").on("change paste keyup",function()
	{
		circle.setRadius($("#radius").val());
	});
	
	var markerLocation = new L.LatLng(<?= $lat ?>,<?= $lng ?>);
	circle = new L.circle(markerLocation, {radius: 20.0});
	circle.draggable = true;
	circle.addTo(map);
	
	circle.on({
	  mousedown: function () {
		map.on('mousemove', function (e) {
			map.dragging.disable();
		  circle.setLatLng(e.latlng);
		});
	  }
	}); 
	map.on('mouseup',function(e){
	  map.removeEventListener('mousemove');
	  map.dragging.enable();
	}); 
  });
  
  function save()
  {
	//alert("Radius: "+ circle.getRadius() + " lat: " + circle.getLatLng().lat + " lng: " + circle.getLatLng().lng);
	// return;
	
	if (!$("#text").val())
	{
		alert("Error");
		return;
	}
		
	var jqxhr = $.post("geoadd_write.php", 
	{
	Text: $("#text").val(), 
	lat: circle.getLatLng().lat, 
	lng: circle.getLatLng().lng,
	radius: circle.getRadius(),
	flag: $("#flag").val()       
	}).always(function() {
	alert("Saved!");
	//location.reload();
	location.href = document.referrer;
	});		
  }
  
  </script>
	</head>
	<body>
	<div>
	<table>
	<tr><td><?php t("Bezeichnung"); ?>:</td><td><input id="text"/></td></tr>
	<tr><td><?php t("Radius"); ?>:</td><td><input id="radius" value="20" type="number"/></td></tr>
	<tr><td><?php t("Special Flag"); ?>:</td><td><input id="flag"/></td></tr>
	<tr><td></td><td><button onclick="save();" >Save</button></td></tr>
	</table>
	</div>
	<div id="map" style="height:800px;"></div>
</body>
</html>
