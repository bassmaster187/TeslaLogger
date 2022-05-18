<!DOCTYPE html>
<?php
require_once("language.php");
require_once("tools.php");

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
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<link rel="stylesheet" href="static/jquery/timepicker/1.3.5/jquery.timepicker.min.css?v=1.3.5">
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
	<script src="static/jquery/timepicker/1.3.5/jquery.timepicker.min.js?v=1.3.5"></script>
	<style>
	.pointer {cursor: pointer;}
	</style>
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
			$( "#dialog-ShareDataHelp" ).dialog({
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
		$("#StreamingPosHelp").click(function() {
			$( "#dialog-StreamingPosHelp").dialog({
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
			
			$display100pctEnable = "false";
			if (property_exists($j,"Display100pctEnable"))
				$display100pctEnable = $j->{"Display100pctEnable"};
		
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

			$StreamingPos = "false";
			if (property_exists($j,"StreamingPos"))
				$StreamingPos = $j->{"StreamingPos"};
	
			$update = "all";
			if (property_exists($j,"update"))
				$update = $j->{"update"};

			$Range = "IR";
			if (property_exists($j,"Range"))
				$Range = $j->{"Range"};
			
			echo ("$('.startdate').val('$start');\r\n");
			echo ("$('.enddate').val('$end');\r\n");
			echo ("$('#checkboxSleep')[0].checked = $enable;\r\n");
			echo ("$('#checkbox100pct')[0].checked = $display100pctEnable;\r\n");
			echo ("$('#checkboxScanMyTesla')[0].checked = $ScanMyTesla;\r\n");
			echo ("$('#StreamingPos')[0].checked = $StreamingPos;\r\n");			

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
				echo ("$('#Language').val('$Language');\r\n");
			else			
				echo ("$('#Language').val('de');\r\n");
				
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
		Display100pctEnable: $("#checkbox100pct").is(':checked'),
		Power: $("input:radio[name ='power']:checked").val(),
		Temperature: $("input:radio[name ='Temperature']:checked").val(),
		Length: $("input:radio[name ='Length']:checked").val(),
		Language: $("#Language").find("option:selected").val(),
		URL_Admin: $("#URL_Admin").val(),
		URL_Grafana: $("#URL_Grafana").val(),
		HTTPPort: $("#HTTPPort").val(),
		ZoomLevel: $("#ZoomLevel").val(),
		ScanMyTesla: $("#checkboxScanMyTesla").is(':checked'),
		ShareData: $('#checkboxSharedata').is(':checked'),
		update: $("input:radio[name ='update']:checked").val(),
		Range: $("input:radio[name ='Range']:checked").val(),
		defaultcar: $("#defaultcar").find("option:selected").text(),
		defaultcarid: $("#defaultcar").find("option:selected").val(),
		StreamingPos: $("#StreamingPos").is(':checked')

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
<tr><td><h1 style="margin-top:0px;"><?php t("Credentials"); ?></h1></td><td></td></tr>
<tr><td><?php t("MyTesla"); ?></td><td><button onclick="window.location.href='password.php';"><?php t("Credentials"); ?></button></td></tr>
<tr><td><?php t("Teslalogger Adminpanel"); ?></td><td><button onclick="window.location.href='adminpanelpassword.php';"><?php t("Credentials"); ?></button></td></tr>
<tr><td><h1><?php t("Settings"); ?></h1></td><td></td></tr>
	<tr><td valign="top"><b><?php t("Language"); ?>:</b></td><td>
	<select id="Language">
		<option value="da">Dansk</option>
		<option value="de">Deutsch</option>
		<option value="en">English</option>
		<option value="es">Español</option>
		<option value="fr">Français</option>
		<option value="it">Italiano</option>
		<option value="nl">Nederlands</option>
		<option value="no">Norsk</option>
		<option value="pt">Português</option>
		<option value="ru">Русский</option>
		<option value="cn">漢語</option>
	</select>
	</td></tr>
	<tr><td valign="top"><b><?php t("Power"); ?>:</b></td><td><input id="radio_hp" type="radio" value="hp" name="power" /> <?php t("PS"); ?><br><input id="radio_kw" type="radio" value="kw" name="power" /> <?php t("kW"); ?></td></tr>
	<tr><td valign="top"><b><?php t("Temperature"); ?>:</b></td><td><input id="radio_celsius" type="radio" value="celsius" name="Temperature"> <?php t("Celsius"); ?><br><input id="radio_fahrenheit" type="radio" value="fahrenheit" name="Temperature"> <?php t("Fahrenheit"); ?> </td></tr>
	<tr><td valign="top"><b><?php t("Unit of length"); ?>:</b></td><td><input id="radio_km" type="radio" value="km" name="Length"> <?php t("km"); ?><br><input id="radio_mile" type="radio" value="mile" name="Length"> <?php t("mile"); ?> </td></tr>
	<tr><td valign="top"><b><?php t("Range"); ?>:</b></td><td><input id="radio_Ideal" type="radio" value="IR" name="Range"> <?php t("Ideal"); ?><br><input id="radio_Rated" type="radio" value="RR" name="Range"> <?php t("Rated"); ?></td></tr>
	<tr><td><b><?php t("Share data anonymously"); ?>:</b></td><td><input id="checkboxSharedata" type="checkbox" value="sharedata"> <?php t("Enable"); ?></td><td><img id="ShareDataHelp" src="img/icon-help-24.png" /></td></tr>
	<tr><td valign="top"><b><?php t("Automatic updates"); ?>:</b></td><td><input id="radio_all" type="radio" value="all" name="update"> <?php t("All"); ?><br><input id="radio_stable" type="radio" value="stable" name="update"> <?php t("Stable"); ?><br><input id="radio_none" type="radio" value="none" name="update"> <?php t("None"); ?></td></tr>
	<tr><td><b><?php t("Sleep"); ?>:</b></td><td><input id="checkboxSleep" type="checkbox" value="sleep"> <?php t("Enable"); ?></td></tr>
	<tr><td></td><td><input class="startdate timepicker text-center"></input> <?php t("to"); ?> <input class="enddate timepicker text-center"></input></td></tr>
	<tr><td><b><?php t("Show calc. 100% range"); ?>:</b></td><td><input id="checkbox100pct" type="checkbox" value="100pct"> <?php t("Enable"); ?></td></tr>
	<tr><td valign="top"><b><?php t("URL Admin Panel"); ?>:</b></td><td><input id="URL_Admin" style="width:100%;" placeholder="http://raspberry/admin/"></td></tr>
	<tr><td valign="top"><b><?php t("URL Grafana"); ?>:</b></td><td><input id="URL_Grafana" style="width:100%;" placeholder="http://raspberry:3000/"></td></tr>
	<tr><td valign="top"><b><?php t("Teslalogger HTTP Port"); ?>:</b></td><td><input id="HTTPPort" style="width:100%;" placeholder="5000"></td></tr>
	<tr><td valign="top"><b><?php t("Zoom Level"); ?>:</b></td><td><input id="ZoomLevel" size="4"></td></tr>
	<tr><td><b><?php t("ScanMyTesla integration"); ?>:</b></td><td><input id="checkboxScanMyTesla" type="checkbox" value="ScanMyTesla"> <?php t("Enable"); ?></td><td><a href="https://teslalogger.de/smt.php" target=”_blank”><img src="img/icon-help-24.png"/></a></td></tr>
	<tr><td><b><?php t("Position by StreamingAPI"); ?>:</b></td><td><input id="StreamingPos" type="checkbox" value="StreamingPos"> <?php t("Enable"); ?></td><td><img id="StreamingPosHelp" src="img/icon-help-24.png" class="pointer"/></td></tr>
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
	echo file_get_contents("http://teslalogger.de/tasker_date.php?t=".$taskertoken, 0, stream_context_create(["http"=>["timeout"=>2]]));
?>
</td></tr>

<?php
}

?>
<tr><td></td><td>&nbsp;</td></tr>
<tr><td></td><td><button onclick="save();" style="float: right;"><?php t("Save"); ?></button></td></tr>
</table>
</div>

<div id="dialog-ShareDataHelp" title="Info" style="display:none;">
<?php t("TextShare"); ?>
</div>
<div id="dialog-StreamingPosHelp" title="Info" style="display:none;">
<?php t("StreamingPosHelp"); ?>
</div>
