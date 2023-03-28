<!DOCTYPE html>
<?php
require_once("language.php");
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

	if ($fp === FALSE)
	{
		echo("Error open geofence-private.csv");
		return;
	}

	while ($line = fgets ($fp)) {
		// echo("Line : $n : $line <br>");
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
    <title>Teslalogger Geofencing V1.1</title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css">
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
   <!-- Make sure you put this AFTER Leaflet's CSS -->
	<script src="static/leaflet/1.4.0/leaflet.js"></script>
	
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
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
			var limit = e.substring(4);
			var lpos = limit.indexOf(":");
			if (lpos > 0)
			{
				var sclend = limit.substring(lpos);
				limit = limit.substring(0,lpos);

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
			$("#occ_limit").val(e.substring(4));
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
			f += $("#hfl_count").val().replace("c","") + "c";
		else
			f += "100c";
	}

	if ($("#esm").is(':checked'))
	{
		f += "+esm:"+$("#esm_gear").val();
	}

	if ($("#dsm").is(':checked'))
	{
		f += "+dsm:"+$("#dsm_gear").val();
	}
	  
	if ($("#cof").is(':checked'))
	{
		f += "+cof:"+$("#cof_gear").val();
	}	
	if ($("#nosleep").is(':checked'))
		f += "+nosleep";

	$("#flag").val(f);
  }
  
	function del()
  {
		//alert("Radius: "+ circle.getRadius() + " lat: " + circle.getLatLng().lat + " lng: " + circle.getLatLng().lng);
		// return;
		if (confirm('Are you sure you want to delete this geofencing location?')) {
		  // Save it!
			if (!$("#text").val())
				{
					alert("Error");
					return;
				}

			var jqxhr = $.post("geoadd_write.php",
			{
				delete: "yes",
			<?PHP
				if (isset($_REQUEST["id"]))
					echo("id: $id");
				?>
			}).always(function() {
			alert("Deleted!");
			//location.reload();
			location.href = document.referrer;
			});
		} else {
		  // Do nothing!
		  return;
		}
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
	alert("<?php t('Saved!'); ?>");
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
    echo(menu("Geofencing"));
?>
<div style="max-width: 1260px;">
	<div style="float:left;">
		<div>
  			<h2 style="margin-top: 0px;"><?php t("Name & Position"); ?></h2>
			<table>
				<tr><td><?php t("Description"); ?>:</td><td><input id="text" value="<?= $poiname ?>"/></td></tr>
				<tr><td><?php t("Radius"); ?>:</td><td><input id="radius" value="<?= intval($radius) ?>" type="number"/></td></tr>
			</table>
		</div>
		<div>
  			 
			<table>
				<tr><td><h2><?php t("Special Flags"); ?></h2></td><td><a href="https://github.com/bassmaster187/TeslaLogger/blob/master/TeslaLogger/Geofence.md#special-flags-for-pois"><img src="img/icon-help-24.png" /></a></td></tr>
				<tr><td><h4 style="margin-top: 0px;"><?php t("Type"); ?></h4></td></tr>
				<tr><td>🏠 <?php t("Home"); ?></td><td><input id="home" type="checkbox" value="home" /></td></tr>
				<tr><td>💼 <?php t("Work"); ?></td><td> <input id="work" type="checkbox" value="work" /></td></tr>
				<tr><td>🔌 <?php t("Charger"); ?></td><td> <input id="charger" type="checkbox" value="charger" name="type" /></td></tr>
				<tr><td><h4 style="margin-top: 20px;"><?php t("Charging"); ?></h4></td></tr>
				<tr><td><?php t("Copy Charging Costs"); ?></td><td> <input id="ccp" type="checkbox" value="" name="type" /></td></tr>
				<tr><td><?php t("Don't Combine Charging Sessions"); ?></td><td> <input id="dnc" type="checkbox" value="" name="type" /></td></tr>
				<tr><td><?php t("Set Charge Limit"); ?></td><td> <input id="scl" type="checkbox" value=""/></td><td>&nbsp;</td><td><?php t("SOC"); ?></td><td><input size="6" id="scl_limit" placeholder="100"/>%</td><td><input id="scl_soa" type="checkbox" value=""/> <?php t("Set on arrival"); ?></td></tr>
				<tr><td><?php t("Set Charge Limit After Charging"); ?></td><td> <input id="occ" type="checkbox" value=""/></td><td>&nbsp;</td><td><?php t("SOC"); ?></td><td><input size="6" id="occ_limit" placeholder="75"/>%</td></tr>
				<tr><td><?php t("Open Charge Port"); ?></td><td> <input id="ocp" type="checkbox" value=""/></td><td></td><td><?php t("Gear"); ?></td>
					<td>
  						<select id="ocp_gear">
						  <option value="DR->P"><?php t("D/R → P"); ?></option>
						  <option value="D->P"><?php t("D → P"); ?></option>
						  <option value="R->P"><?php t("R → P"); ?></option>
						</select>
					</td></tr>
				<tr><td><?php t("High Frequency Logging"); ?></td><td> <input id="hfl" type="checkbox" value=""/></td><td>&nbsp;</td><td><?php t("Duration"); ?></td><td><input size="6" id="hfl_minutes"/><?php t("Minutes"); ?></td></tr>
				<tr><td></td><td></td><td>&nbsp;</td><td><?php t("Special Count"); ?></td><td><input size="6" id="hfl_count" placeholder="100"/><?php t("Count"); ?></td></tr>
				<tr><td><h4 style="margin-top: 20px;"><?php t("Special Features"); ?></h4></td></tr>
				<tr><td><?php t("Enable Sentry Mode"); ?></td><td> <input id="esm" type="checkbox" value="" name="type" /></td><td></td><td><?php t("Gear"); ?></td>
					<td>
						<select id="esm_gear">
						  <option value="DR->P"><?php t("D/R → P"); ?></option>
						  <option value="D->P"><?php t("D → P"); ?></option>
						  <option value="R->P"><?php t("R → P"); ?></option>
						</select>
					</td></tr>
				<tr><td><?php t("Disable Sentry Mode"); ?></td><td> <input id="dsm" type="checkbox" value="" name="type" /></td><td></td><td><?php t("Gear"); ?></td>
					<td>
						<select id="dsm_gear">
						  <option value="DR->P"><?php t("D/R → P"); ?></option>
						  <option value="D->P"><?php t("D → P"); ?></option>
						  <option value="R->P"><?php t("R → P"); ?></option>
						</select>
					</td></tr>
				<tr><td><?php t("Turn HVAC off"); ?></td><td> <input id="cof" type="checkbox" value="" name="type" /></td><td></td><td><?php t("Gear"); ?></td>
					<td>
						<select id="cof_gear">
						  <option value="DR->P"><?php t("D/R → P"); ?></option>
						  <option value="D->P"><?php t("D → P"); ?></option>
						  <option value="R->P"><?php t("R → P"); ?></option>
						</select>
					</td></tr>
				<tr><td><?php t("No sleep"); ?></td><td> <input id="nosleep" type="checkbox" value="" name="type" /></td></tr>
				<tr><td colspan=5 ><input style="width: 100%" id="flag" disabled/></td></tr>
			</table>
		</div>
		<button id="btn_save" onclick="save();"><?php t("Save"); ?></button>
		<button id="btn_delete" onclick="del();" class="redbutton"><?php t("Delete"); ?></button>

	</div>
	<div id="map" style="height:700px; z-index:0;"></div>
</div>
</body>
</html>
