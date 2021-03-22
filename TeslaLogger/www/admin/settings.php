﻿<!DOCTYPE html>
<?php
require("language.php");
require("tools.php");

function CarsCombobox($cars, $selected)
{
	echo "\r\n<select id='defaultcar' style='width:100%'>";
	foreach ($cars as $k => $v) {
		$displayname = $v->{"display_name"};
		$id = $v->{"id"};
		$s = "";
		if ($displayname == $selected)
			$s = 'selected="selected"';

		echo "\r\n   <option $s value='$id'>$displayname</option>";
	}
	echo "\r\n</select>\r\n";
}

?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger Settings V1.5</title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="https://teslalogger.de/teslalogger_style.css">
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.css">
	<link rel='stylesheet' id='genericons-css'  href='https://www.impala64.de/blog/tesla/wp-content/themes/twentyfourteen/genericons/genericons.css?ver=3.0.3' type='text/css' media='all' />
	<script src="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.js"></script>
	<script>
	
	$( function() {
		//$( "button" ).button();
	
		$('.timepicker').timepicker({
			timeFormat: 'HH:mm',
			interval: 30,
			minTime: '0:00am',
			maxTime: '23:30',
			startTime: '00:00',
			dynamic: false,
			dropdown: true,
			scrollbar: true
		});
		$('.timepicker').width("100px");
		$(".timepicker").show();
		$("#ShareDataHelp").click(function() {
			$( "#dialog-confirm" ).dialog({
				resizable: false,
				width: "auto",
				modal: true,
				buttons: {
					"OK": function() {
					$( this ).dialog( "close" );
					}
				}
				});
		});

		<?PHP
		
		$content = FALSE;
		if (file_exists("/etc/teslalogger/settings.json"))
			$content = file_get_contents("/etc/teslalogger/settings.json");
		
		if ($content === FALSE)
		{
			// Default Values for fresh install
			echo ("$('#radio_kw').prop('checked', true);\r\n");
			echo ("$('#radio_celsius').prop('checked', true);\r\n");
			echo ("$('#radio_km').prop('checked', true);\r\n");
			echo ("$('#radio_en').prop('checked', true);\r\n");		
			echo ("$('#radio_all').prop('checked', true);\r\n");
			echo ("$('#radio_Ideal').prop('checked', true);\r\n");
		}
		else
		{
			$j = json_decode($content);
			$start = $j->{"SleepTimeSpanStart"};			
			$end = $j->{"SleepTimeSpanEnd"};
			$enable = $j->{"SleepTimeSpanEnable"};
			$power = $j->{"Power"};
			$Temperature = $j->{"Temperature"};
			$Length = $j->{"Length"};
			$Language = $j->{"Language"};
			$URL_Admin = $j->{"URL_Admin"};
			$URL_Grafana = $j->{"URL_Grafana"};
			$ZoomLevel = $j->{"ZoomLevel"};
			$defaultcar = $j->{"defaultcar"};
			
			$HTTPPort = 5000;
			if (property_exists($j,"HTTPPort"))
				$HTTPPort = $j->{"HTTPPort"};

			$ScanMyTesla = "false";
			if (property_exists($j,"ScanMyTesla"))
				$ScanMyTesla = $j->{"ScanMyTesla"};
	
			$update = "all";
			if (property_exists($j,"update"))
				$update = $j->{"update"};

			$Range = "IR";
			if (property_exists($j,"Range"))
				$Range = $j->{"Range"};
			
			echo ("$('.startdate').val('$start');\r\n");
			echo ("$('.enddate').val('$end');\r\n");
			echo ("$('#checkboxSleep')[0].checked = $enable;\r\n");
			echo ("$('#checkboxScanMyTesla')[0].checked = $ScanMyTesla;\r\n");

			if ($Range == "IR")
				echo ("$('#radio_Ideal').prop('checked', true);\r\n");
			else
				echo ("$('#radio_Rated').prop('checked', true);\r\n");
			
			if ($power == "kw")
				echo ("$('#radio_kw').prop('checked', true);\r\n");
			else
				echo ("$('#radio_hp').prop('checked', true);\r\n");
				
			if ($Temperature == "fahrenheit")
				echo ("$('#radio_fahrenheit').prop('checked', true);\r\n");
			else
				echo ("$('#radio_celsius').prop('checked', true);\r\n");
				
			if ($Length == "mile")
				echo ("$('#radio_mile').prop('checked', true);\r\n");
			else
				echo ("$('#radio_km').prop('checked', true);\r\n");
				
			if(!empty($Language))
				echo ("$('#radio_$Language').prop('checked', true);\r\n");
			else
				echo ("$('#radio_de').prop('checked', true);\r\n");
				
			if (isset($URL_Admin))
				echo ("$('#URL_Admin').val('$URL_Admin');\r\n");

			if (isset($URL_Grafana))
				echo ("$('#URL_Grafana').val('$URL_Grafana');\r\n");
			
			if (isset($HTTPPort))
				echo ("$('#HTTPPort').val('$HTTPPort');\r\n");

			if (isset($ZoomLevel))
				echo ("$('#ZoomLevel').val('$ZoomLevel');\r\n");
			
			if (isShareData())
				echo ("$('#checkboxSharedata')[0].checked = true;\r\n");
			else
				echo ("$('#checkboxSharedata')[0].checked = false;\r\n");

			if ($update =="none")
				echo ("$('#radio_none').prop('checked', true);\r\n");
			else if ($update =="stable")
				echo ("$('#radio_stable').prop('checked', true);\r\n");
			else 
				echo ("$('#radio_all').prop('checked', true);\r\n");

		}
		?>
	});
  
  function save()
  {
		var jqxhr = $.post("settings_write.php", 
		{
		SleepTimeSpanStart: $(".startdate").val(), 
		SleepTimeSpanEnd: $(".enddate").val(), 
		SleepTimeSpanEnable: $("#checkboxSleep").is(':checked'),
		Power: $("input:radio[name ='power']:checked").val(),
		Temperature: $("input:radio[name ='Temperature']:checked").val(),
		Length: $("input:radio[name ='Length']:checked").val(),
		Language: $("input:radio[name ='Language']:checked").val(),
		URL_Admin: $("#URL_Admin").val(),
		URL_Grafana: $("#URL_Grafana").val(),
		HTTPPort: $("#HTTPPort").val(),
		ZoomLevel: $("#ZoomLevel").val(),
		ScanMyTesla: $("#checkboxScanMyTesla").is(':checked'),
		ShareData: $('#checkboxSharedata').is(':checked'),
		update: $("input:radio[name ='update']:checked").val(),
		Range: $("input:radio[name ='Range']:checked").val(),
		defaultcar: $("#defaultcar").find("option:selected").text(),
		defaultcarid: $("#defaultcar").find("option:selected").val()

		}).always(function() {
		alert("Saved!");
		location.reload();
		});		
  }

</script>
<body style="padding-top: 5px; padding-left: 10px;">
<?php 
include "menu.php";
echo(menu("Settings"));
?>
<div>
<table>
<tr><td><h1 style="margin-top:0px;"><?php t("Zugangsdaten"); ?></h1></td><td></td></tr>
<tr><td></td><td><button onclick="window.location.href='password.php';"  style="float: right;"><?php t("Zugangsdaten"); ?></button></td></tr>
<tr><td><h1><?php t("Settings"); ?></h1></td><td></td></tr>
	<tr><td valign="top"><b><?php t("Language"); ?>:</b></td><td>
		<input id="radio_da" type="radio" value="da" name="Language" /> Dansk<br>
		<input id="radio_de" type="radio" value="de" name="Language" /> Deutsch<br>
		<input id="radio_en" type="radio" value="en" name="Language" /> English<br>
		<input id="radio_es" type="radio" value="es" name="Language" /> Español<br>
		<input id="radio_it" type="radio" value="it" name="Language" /> Italiano<br>
		<input id="radio_nl" type="radio" value="nl" name="Language" /> Nederlands<br>
		<input id="radio_no" type="radio" value="no" name="Language" /> Norsk<br>
		<input id="radio_pt" type="radio" value="pt" name="Language" /> Português<br>
		<input id="radio_ru" type="radio" value="ru" name="Language" /> Русский<br>
		<input id="radio_cn" type="radio" value="cn" name="Language" /> 漢語<br>		
	</td></tr>
	<tr><td valign="top"><b><?php t("Leistung"); ?>:</b></td><td><input id="radio_hp" type="radio" value="hp" name="power" /> PS<br><input id="radio_kw" type="radio" value="kw" name="power" /> kW</td></tr>
	<tr><td valign="top"><b><?php t("Temperatur"); ?>:</b></td><td><input id="radio_celsius" type="radio" value="celsius" name="Temperature"> <?php t("Celsius"); ?><br><input id="radio_fahrenheit" type="radio" value="fahrenheit" name="Temperature"> <?php t("Fahrenheit"); ?> </td></tr>
	<tr><td valign="top"><b><?php t("Längenmaß"); ?>:</b></td><td><input id="radio_km" type="radio" value="km" name="Length"> km<br><input id="radio_mile" type="radio" value="mile" name="Length"> mile </td></tr>
	<tr><td valign="top"><b><?php t("Reichweite"); ?>:</b></td><td><input id="radio_Ideal" type="radio" value="IR" name="Range"> Ideal<br><input id="radio_Rated" type="radio" value="RR" name="Range"> Rated</td></tr>
	<tr><td><b><?php t("Daten anonym teilen"); ?>:</b></td><td><input id="checkboxSharedata" type="checkbox" value="sharedata"> <?php t("Enable"); ?></td><td><img id="ShareDataHelp" src="img/icon-help-24.png" /></td></tr>
	<tr><td valign="top"><b><?php t("Automatische Updates"); ?>:</b></td><td><input id="radio_all" type="radio" value="all" name="update"> <?php t("All"); ?><br><input id="radio_stable" type="radio" value="stable" name="update"> <?php t("Stable"); ?><br><input id="radio_none" type="radio" value="none" name="update"> <?php t("None"); ?></td></tr>
	<tr><td><b><?php t("Schlafen"); ?>:</b></td><td><input id="checkboxSleep" type="checkbox" value="sleep"> <?php t("Enable"); ?></td></tr>
	<tr><td></td><td><input class="startdate timepicker text-center"></input> to <input class="enddate timepicker text-center"></input></td></tr>
	<tr><td valign="top"><b><?php t("URL Admin Panel"); ?>:</b></td><td><input id="URL_Admin" style="width:100%;" placeholder="http://raspberry/admin/"></td></tr>
	<tr><td valign="top"><b><?php t("URL Grafana"); ?>:</b></td><td><input id="URL_Grafana" style="width:100%;" placeholder="http://raspberry:3000/"></td></tr>
	<tr><td valign="top"><b><?php t("Teslalogger HTTP Port"); ?>:</b></td><td><input id="HTTPPort" style="width:100%;" placeholder="5000"></td></tr>
	<tr><td valign="top"><b><?php t("Zoom Level"); ?>:</b></td><td><input id="ZoomLevel" size="4"></td></tr>
	<tr><td><b><?php t("ScanMyTesla integration"); ?>:</b></td><td><input id="checkboxScanMyTesla" type="checkbox" value="ScanMyTesla"> <?php t("Enable"); ?></td><td><a href="https://teslalogger.de/smt.php" target=”_blank”><img src="img/icon-help-24.png" /></a></td></tr>
	
	
	
	
<?php

	$url = GetTeslaloggerURL("getallcars");
	$allcars = @file_get_contents($url);
	if ($allcars === false)
    {
        $error = error_get_last();
        $error = explode(': ', $error['message']);
        $error = trim($error[2]);
		echo("<h1>errortext = 'Error: $error - URL: $url'</h1>");
		return;
    }
	
	$jcars = json_decode($allcars);

	//var_dump($jcars);
?>
<tr><td><b><?php t("Main Car"); ?>:</b></td><td><?PHP CarsCombobox($jcars, $defaultcar); ?></td></tr>
<?php
	
	foreach ($jcars as $k => $v) {
		$displayname = $v->{"display_name"};
		$taskertoken = $v->{"tasker_hash"};    
		
		$lastscanmytesla = JSONDatetoString($v->{"lastscanmytesla"});
	
	if ($taskertoken == null)
		continue;
?>

<tr><td>&nbsp;</td><td></td></tr>
<tr><td><b><?php t("Car Name"); ?>:</b></td><td><?= $displayname ?></td></tr>
<tr><td style="padding-left:20px;"><b><?php t("ScanMyTesla last received"); ?>:</b></td><td><?= $lastscanmytesla ?></td></tr>
<tr><td style="padding-left:20px;"valign="top"><b><?php t("Tasker Token"); ?>:</b></td><td><?= $taskertoken ?></td></tr>
<tr><td style="padding-left:20px;"valign="top"><b><?php t("Tasker URL"); ?>:</b></td><td>
<?php
if (strlen($taskertoken) > 7)
	echo "https://teslalogger.de/wakeup.php?t=".$taskertoken;
?>
</td></td><td><a href="https://teslalogger.de/faq-1.php" target=”_blank”><img src="img/icon-help-24.png" /></a></td></tr>
<tr><td style="padding-left:20px;" valign="top"><b><?php t("Received Tasker Token"); ?>:</b></td><td>
<?php
if (strlen($taskertoken) > 7)
	echo file_get_contents("http://teslalogger.de/tasker_date.php?t=".$taskertoken);
?>
</td></tr>

<?php
}

?>
<tr><td></td><td>&nbsp;</td></tr>
<tr><td></td><td><button onclick="save();" style="float: right;">Save</button></td></tr>
</table>
</div>

<div id="dialog-confirm" title="Info" style="display:none;">
<?php t("TextShare"); ?>
</div>
