<!DOCTYPE html>
<?php
require("language.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger Settings V1.4</title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="https://teslalogger.de/teslalogger_style.css">
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.css">
	<script src="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.js"></script>
	<script>
	
	$( function() {
		$( "button" ).button();
	
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
		<?PHP
		
		$content = FALSE;
		if (file_exists("/etc/teslalogger/settings.json"))
			$content = file_get_contents("/etc/teslalogger/settings.json");
		
		if ($content === FALSE)
		{
			echo ("$('#radio_kw').prop('checked', true);\r\n");
			echo ("$('#radio_celsius').prop('checked', true);\r\n");
			echo ("$('#radio_km').prop('checked', true);\r\n");
			echo ("$('#radio_en').prop('checked', true);\r\n");			
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

			$ScanMyTesla = "false";
			
			if (property_exists($j,"ScanMyTesla"))
				$ScanMyTesla = $j->{"ScanMyTesla"};
			
			echo ("$('.startdate').val('$start');\r\n");
			echo ("$('.enddate').val('$end');\r\n");
			echo ("$('#checkboxSleep')[0].checked = $enable;\r\n");
			echo ("$('#checkboxScanMyTesla')[0].checked = $ScanMyTesla;\r\n");
			
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
				
			if($Language =="en")
				echo ("$('#radio_en').prop('checked', true);\r\n");
			else if($Language =="no")
				echo ("$('#radio_no').prop('checked', true);\r\n");
			else
				echo ("$('#radio_de').prop('checked', true);\r\n");
				
			if (isset($URL_Admin))
				echo ("$('#URL_Admin').val('$URL_Admin');\r\n");
			
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
		ScanMyTesla: $("#checkboxScanMyTesla").is(':checked'),
		}).always(function() {
		alert("Saved!");
		location.reload();
		});		
  }

</script>
<button onclick="window.location.href='password.php';"><?php t("Zugangsdaten"); ?></button>

<br><br>
<div>
<table>
<tr><td valign="top"><b><?php t("Language"); ?>:</b></td><td>
<input id="radio_de" type="radio" value="de" name="Language" /> Deutsch<br>
<input id="radio_en" type="radio" value="en" name="Language" /> English<br>
<input id="radio_no" type="radio" value="no" name="Language" /> Norsk 
</td></tr>
<tr><td valign="top"><b><?php t("Leistung"); ?>:</b></td><td><input id="radio_hp" type="radio" value="hp" name="power" /> PS<br><input id="radio_kw" type="radio" value="kw" name="power" /> kW</td></tr>
<tr><td valign="top"><b><?php t("Temperatur"); ?>:</b></td><td><input id="radio_celsius" type="radio" value="celsius" name="Temperature"> Celsius<br><input id="radio_fahrenheit" type="radio" value="fahrenheit" name="Temperature"> Fahrenheit </td></tr>
<tr><td valign="top"><b><?php t("Längenmaß"); ?>:</b></td><td><input id="radio_km" type="radio" value="km" name="Length"> km<br><input id="radio_mile" type="radio" value="mile" name="Length"> mile </td></tr>
<tr><td><b><?php t("Schlafen"); ?>:</b></td><td><input id="checkboxSleep" type="checkbox" value="sleep"> Enable</td></tr>
<tr><td></td><td><input class="startdate timepicker text-center"></input> to <input class="enddate timepicker text-center"></input></td></tr>
<tr><td valign="top"><b><?php t("URL Admin Panel"); ?>:</b></td><td><input id="URL_Admin" style="width:100%;" placeholder="http://raspberry/admin/"></td></tr>
<tr><td><b><?php t("ScanMyTesla integration"); ?>:</b></td><td><input id="checkboxScanMyTesla" type="checkbox" value="ScanMyTesla"> Enable</td><td><a href="https://teslalogger.de/smt.php" target=”_blank”><img src="img/icon-help-24.png" /></a></td></tr>
<tr><td><b><?php t("ScanMyTesla last received"); ?>:</b></td><td>
<?php
if (file_exists("/etc/teslalogger/LASTSCANMYTESLA"))
{
	$taskertoken = file_get_contents("/etc/teslalogger/LASTSCANMYTESLA");
	echo $taskertoken;
}
else
{
	echo "-";
}
?>
</td></tr>
<tr><td valign="top"><b>Tasker Token:</b></td><td>
<?php
$taskertoken = file_get_contents("/etc/teslalogger/TASKERTOKEN");
echo $taskertoken;
?>
</tr>
<tr><td valign="top"><b>Tasker URL:</b></td><td>
<?php
if (strlen($taskertoken) > 7)
	echo "https://teslalogger.de/wakeup.php?t=".$taskertoken;
?>
</td></td><td><a href="https://teslalogger.de/faq-1.php" target=”_blank”><img src="img/icon-help-24.png" /></a></td></tr>
<tr><td valign="top"><b>Received Tasker Token:</b></td><td>
<?php
echo file_get_contents("http://teslalogger.de/tasker_date.php?t=".$taskertoken);
?>
</td></tr>
<tr><td></td><td>&nbsp;</td></tr>
<tr><td></td><td><button onclick="save();" style="float: right;">Save</button></td></tr>
</table>
</div>
