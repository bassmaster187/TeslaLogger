<!DOCTYPE html>
<?php
require("language.php");
$lat = $_REQUEST["lat"];
$lng = $_REQUEST["lng"];
$radius = 20.0;
$poiname = "";
$id = $_REQUEST["id"];
$csv = null;
$sf = null;


if (isset($id))
{
	$n = 0;
	$fp = fopen("/etc/teslalogger/geofence-private.csv", "r+");
	while ($line = stream_get_line($fp, 1024 * 1024, "\n")) {
		if ($n == $id)
		{
			//echo($line);
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

	$("input").change(function(){OnSpecialFlagsChanged();});
	$("select").change(function(){OnSpecialFlagsChanged();});

	$("#radius").on("change paste keyup",function()
	{
		circle.setRadius($("#radius").val());
	});
	
	var markerLocation = new L.LatLng(<?= $lat ?>,<?= $lng ?>);

	circle = new L.circle(markerLocation, {radius: <?= $radius ?>});
	circle.draggable = true;
	circle.addTo(map);

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
			$("#scl_limit").val(e.substring(4));
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
		else if (e.startsWith("cof"))
		{
			$("#cof").attr('checked', 'checked');
			if (e.substring(4).length == 0)
				$("#cof_gear").val("DR->P");
			else
				$("#cof_gear").val(e.substring(4));
		}
    	
		OnSpecialFlagsChanged();
	});



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

	if ($("#scl").is(':checked'))
	{
		f += "+scl:";
		if ($("#scl_limit").val().length == 0)
			f += "100";
		else
			f += $("#scl_limit").val();
	}

	if ($("#ocp").is(':checked'))
	{
		f += "+ocp:"+$("#ocp_gear").val();
	}

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
			f += $("#hfl_count").val() + "c";
		else
			f += "100c";
	}

	if ($("#esm").is(':checked'))
	{
		f += "+esm:"+$("#esm_gear").val();
	}

	if ($("#cof").is(':checked'))
	{
		f += "+cof:"+$("#cof_gear").val();
	}		

	$("#flag").val(f);
  }
  
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
		flag: $("#flag").val(),
<?PHP 
		if (isset($_REQUEST["id"]))
			echo("id: $id");
		?>     
	}).always(function() {
	alert("Saved!");
	//location.reload();
	location.href = document.referrer;
	});		
  }
  
  </script>
  <style>
  	td {padding-right:10px}
	@media screen and (max-width: 1055px) {
		#map { 
			float:left;
			width: -webkit-fill-available;
			margin-top: 36px;
		}
	}
  </style>
	</head>
	<body>
<?php 
    include "menu.php";
    echo(menu("Geofence"));
?>
<div style="max-width: 1260px;">
	<div style="float:left;">
		<div>
  			<h2 style="margin-top: 0px;">Name & Position</h2>
			<table>
				<tr><td><?php t("Bezeichnung"); ?>:</td><td><input id="text" value="<?= $poiname ?>"/></td></tr>
				<tr><td><?php t("Radius"); ?>:</td><td><input id="radius" value="<?= intval($radius) ?>" type="number"/></td></tr>
			</table>
		</div>
		<div>
  			 
			<table>
				<tr><td><h2>Special Flags</h2></td><td><a href="https://github.com/bassmaster187/TeslaLogger/blob/master/TeslaLogger/Geofence.md#special-flags-for-pois"><img src="img/icon-help-24.png" /></a></td></tr>
				<tr><td><h4 style="margin-top: 0px;">Type</h4></td></tr>
				<tr><td>🏠 Home</td><td><input id="home" type="checkbox" value="home" /></td></tr>
				<tr><td>💼 Work</td><td> <input id="work" type="checkbox" value="work" /></td></tr>
				<tr><td>🔌 Charger</td><td> <input id="charger" type="checkbox" value="charger" name="type" /></td></tr>
				<tr><td><h4 style="margin-top: 20px;">Charging</h4></td></tr>
				<tr><td>Copy Charging Costs</td><td> <input id="ccp" type="checkbox" value="" name="type" /></td></tr>
				<tr><td>Set Charge Limit</td><td> <input id="scl" type="checkbox" value=""/></td><td>&nbsp;</td><td>SOC</td><td><input size="6" id="scl_limit" placeholder="100"/>%</td></tr>
				<tr><td>Open Charge Port</td><td> <input id="ocp" type="checkbox" value=""/></td><td></td><td>Gear</td>
					<td>
  						<select id="ocp_gear">
						  <option value="DR->P">D/R → P</option>
						  <option value="D->P">D → P</option>
						  <option value="R->P">R → P</option>
						</select>
					</td></tr>
				<tr><td>High Frequency Logging</td><td> <input id="hfl" type="checkbox" value=""/></td><td>&nbsp;</td><td>Duration</td><td><input size="6" id="hfl_minutes"/>Minutes</td></tr>
				<tr><td></td><td></td><td>&nbsp;</td><td>Count</td><td><input size="6" id="hfl_count" placeholder="100"/>Count</td></tr>
				<tr><td><h4 style="margin-top: 20px;">Features</h4></td></tr>
				<tr><td>Sentry Mode</td><td> <input id="esm" type="checkbox" value="" name="type" /></td><td></td><td>Gear</td>
					<td>
						<select id="esm_gear">
						  <option value="DR->P">D/R → P</option>
						  <option value="D->P">D → P</option>
						  <option value="R->P">R → P</option>
						</select>
					</td></tr>
				<tr><td>Turn HVAC off</td><td> <input id="cof" type="checkbox" value="" name="type" /></td><td></td><td>Gear</td>
					<td>
						<select id="cof_gear">
						  <option value="DR->P">D/R → P</option>
						  <option value="D->P">D → P</option>
						  <option value="R->P">R → P</option>
						</select>
					</td></tr>

				<tr><td colspan=5 ><input style="width: 100%" id="flag" disabled/></td></tr>
			</table>
		</div>
		<button id="btn_save" onclick="save();">Save</button>
	</div>
	<div id="map" style="height:700px; z-index:0;"></div>
</div>
</body>
</html>
