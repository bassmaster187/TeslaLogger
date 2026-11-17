<!DOCTYPE html>
<?php
require_once("language.php");
require_once("tools.php");

function CarsCombobox($cars, $selected)
{
	echo "\r\n<select id='defaultcar' class='tl-defaultcar'>";
	foreach ($cars as $k => $v) {
		$displayname = $v->{"display_name"};
		$id = $v->{"id"};
		$s = "";
		if ($displayname == $selected)
			$s = 'selected="selected"';

		echo "\r\n   <option $s value='$id'>$displayname</option>";
	}
	echo "\r\n</select>";
}

?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger Settings V1.6</title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=9">
	<link rel="stylesheet" href="static/teslalogger_settings.css?v=11">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="static/jquery/timepicker/1.3.5/jquery.timepicker.min.css?v=1.3.5">
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
	<script src="static/jquery/timepicker/1.3.5/jquery.timepicker.min.js?v=1.3.5"></script>
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

		// Keep the inline radio group (wide) and the dropdown (mobile) in sync
		function setUnit(selId, name, value) {
			$('#' + selId).val(value);
			$('input[name="' + name + '"]').prop('checked', function(){ return $(this).val() === value; });
		}
		$('.tl-select[data-name]').on('change', function() {
			var name = $(this).attr('data-name');
			var v = $(this).val();
			$('input[name="' + name + '"]').prop('checked', function(){ return $(this).val() === v; });
		});
		$('input[type="radio"]').on('change', function() {
			var name = $(this).attr('name');
			var sel = $(this).closest('.tl-form-item').find('.tl-select[data-name="' + name + '"]');
			sel.val($(this).val());
		});

		<?PHP
		
		$content = FALSE;

		GetFileFromTeslaloggerAndWriteToTMP("settings.json");

		if (file_exists("/tmp/settings.json"))
			$content = file_get_contents("/tmp/settings.json");
		
		if ($content === FALSE)
		{
			// Default Values for fresh install
			echo ("setUnit('Power','power','kw');\r\n");
			echo ("setUnit('Temperature','Temperature','celsius');\r\n");
			echo ("setUnit('Pressure','Pressure','bar');\r\n");
			echo ("setUnit('Length','Length','km');\r\n");
			echo ("setUnit('update','update','all');\r\n");
			echo ("setUnit('Range','Range','IR');\r\n");
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
			$Pressure = $j->{"Pressure"};
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
	
			echo ("setUnit('Range','Range','$Range');\r\n");
			echo ("setUnit('Power','power','$power');\r\n");
			echo ("setUnit('Temperature','Temperature','$Temperature');\r\n");
			echo ("setUnit('Length','Length','$Length');\r\n");
			echo ("setUnit('Pressure','Pressure','$Pressure');\r\n");
			
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

			echo ("setUnit('update','update','$update');\r\n");

		}
		?>
	});

  function unitValue(selId, name)
  {
		// Read from the visible control: the inline radio group (wide) or
		// the dropdown (mobile)
		if ($('.tl-radio-group input[name="' + name + '"]').is(':visible'))
			return $('input[name="' + name + '"]:checked').val();
		return $('#' + selId).find("option:selected").val();
  }

  function save()
  {
		var jqxhr = $.post("settings_write.php", 
		{
		SleepTimeSpanStart: $(".startdate").val(), 
		SleepTimeSpanEnd: $(".enddate").val(), 
		SleepTimeSpanEnable: $("#checkboxSleep").is(':checked'),
		Display100pctEnable: $("#checkbox100pct").is(':checked'),
		Power: unitValue("Power", "power"),
		Temperature: unitValue("Temperature", "Temperature"),
		Length: unitValue("Length", "Length"),
		Pressure: unitValue("Pressure", "Pressure"),
		Language: $("#Language").find("option:selected").val(),
		URL_Admin: $("#URL_Admin").val(),
		URL_Grafana: $("#URL_Grafana").val(),
		HTTPPort: $("#HTTPPort").val(),
		ZoomLevel: $("#ZoomLevel").val(),
		ScanMyTesla: $("#checkboxScanMyTesla").is(':checked'),
		ShareData: $('#checkboxSharedata').is(':checked'),
		update: unitValue("update", "update"),
		Range: unitValue("Range", "Range"),
		defaultcar: $("#defaultcar").find("option:selected").text(),
		defaultcarid: $("#defaultcar").find("option:selected").val(),
		StreamingPos: $("#StreamingPos").is(':checked')

		}).always(function() {
		alert("<?php t('Saved!'); ?>");
		location.reload();
		});		
  }

</script>
<body>
<?php 
include "menu.php";
echo(menu("Settings"));
?>
<div class="tl-page">
	<div class="tl-page-head">
		<h2 class="tl-page-title"><?php t("Settings"); ?></h2>
		<div class="tl-actions">
			<button onclick="save();"><?php t("Save"); ?></button>
		</div>
	</div>

	<div class="tl-card">
		<h2 class="tl-card-title"><?php t("Credentials"); ?></h2>
		<div class="tl-form">
			<div class="tl-form-item">
				<label><?php t("MyTesla"); ?> / <?php t("MyLucid"); ?></label>
				<button onclick="window.location.href='password.php';"><?php t("Credentials"); ?></button>
			</div>
			<div class="tl-form-item">
				<label><?php t("Teslalogger Adminpanel"); ?></label>
				<button onclick="window.location.href='adminpanelpassword.php';"><?php t("Credentials"); ?></button>
			</div>
		</div>
	</div>

	<div class="tl-card">
		<h2 class="tl-card-title"><?php t("Settings"); ?></h2>
		<div class="tl-form tl-form--settings">
			<div class="tl-section"><?php t("General"); ?></div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Language"); ?></span>
				<select id="Language">
					<option value="da">Dansk</option>
					<option value="de">Deutsch</option>
					<option value="en">English</option>
					<option value="es">Español</option>
					<option value="fr">Français</option>
					<option value="it">Italiano</option>
					<option value="nl">Nederlands</option>
					<option value="no">Norsk</option>
					<option value="pl">Polski</option>
					<option value="pt">Português</option>
					<option value="ru">Русский</option>
					<option value="ta">தமிழ் – Tamil</option>
					<option value="tr">Türkçe</option>
					<option value="cn">漢語</option>
				</select>
			</div>
			<div class="tl-section"><?php t("Units"); ?></div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Power"); ?></span>
				<div class="tl-radio-group">
					<input id="radio_kw" type="radio" value="kw" name="power" /> <label for="radio_kw"><?php t("kW"); ?></label>
					<input id="radio_hp" type="radio" value="hp" name="power" /> <label for="radio_hp"><?php t("PS"); ?></label>
				</div>
				<select id="Power" class="tl-select" data-name="power">
					<option value="kw"><?php t("kW"); ?></option>
					<option value="hp"><?php t("PS"); ?></option>
				</select>
			</div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Temperature"); ?></span>
				<div class="tl-radio-group">
					<input id="radio_celsius" type="radio" value="celsius" name="Temperature"> <label for="radio_celsius"><?php t("Celsius"); ?></label>
					<input id="radio_fahrenheit" type="radio" value="fahrenheit" name="Temperature"> <label for="radio_fahrenheit"><?php t("Fahrenheit"); ?></label>
				</div>
				<select id="Temperature" class="tl-select" data-name="Temperature">
					<option value="celsius"><?php t("Celsius"); ?></option>
					<option value="fahrenheit"><?php t("Fahrenheit"); ?></option>
				</select>
			</div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Unit of length"); ?></span>
				<div class="tl-radio-group">
					<input id="radio_km" type="radio" value="km" name="Length"> <label for="radio_km"><?php t("km"); ?></label>
					<input id="radio_mile" type="radio" value="mile" name="Length"> <label for="radio_mile"><?php t("mile"); ?></label>
				</div>
				<select id="Length" class="tl-select" data-name="Length">
					<option value="km"><?php t("km"); ?></option>
					<option value="mile"><?php t("mile"); ?></option>
				</select>
			</div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Pressure unit"); ?></span>
				<div class="tl-radio-group">
					<input id="radio_bar" type="radio" value="bar" name="Pressure"> <label for="radio_bar"><?php t("bar"); ?></label>
					<input id="radio_psi" type="radio" value="psi" name="Pressure"> <label for="radio_psi"><?php t("psi"); ?></label>
				</div>
				<select id="Pressure" class="tl-select" data-name="Pressure">
					<option value="bar"><?php t("bar"); ?></option>
					<option value="psi"><?php t("psi"); ?></option>
				</select>
			</div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Range"); ?></span>
				<div class="tl-radio-group">
					<input id="radio_Ideal" type="radio" value="IR" name="Range"> <label for="radio_Ideal"><?php t("Ideal"); ?></label>
					<input id="radio_Rated" type="radio" value="RR" name="Range"> <label for="radio_Rated"><?php t("Rated"); ?></label>
				</div>
				<select id="Range" class="tl-select" data-name="Range">
					<option value="IR"><?php t("Ideal"); ?></option>
					<option value="RR"><?php t("Rated"); ?></option>
				</select>
			</div>
			<div class="tl-section"><?php t("Data & Updates"); ?></div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Share data anonymously"); ?> <img id="ShareDataHelp" src="img/icon-help-24.png" class="tl-help" /></span>
				<div class="tl-checkbox">
					<label class="tl-switch">
						<input id="checkboxSharedata" type="checkbox" value="sharedata">
						<span class="tl-switch-track"></span>
						<span><?php t("Enable"); ?></span>
					</label>
				</div>
			</div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Automatic updates"); ?></span>
				<div class="tl-radio-group">
					<input id="radio_all" type="radio" value="all" name="update"> <label for="radio_all"><?php t("All"); ?></label>
					<input id="radio_stable" type="radio" value="stable" name="update"> <label for="radio_stable"><?php t("Stable"); ?></label>
					<input id="radio_none" type="radio" value="none" name="update"> <label for="radio_none"><?php t("None"); ?></label>
				</div>
				<select id="update" class="tl-select" data-name="update">
					<option value="all"><?php t("All"); ?></option>
					<option value="stable"><?php t("Stable"); ?></option>
					<option value="none"><?php t("None"); ?></option>
				</select>
			</div>
			<div class="tl-section"><?php t("Sleep"); ?></div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Enable"); ?></span>
				<div class="tl-checkbox">
					<label class="tl-switch">
						<input id="checkboxSleep" type="checkbox" value="sleep">
						<span class="tl-switch-track"></span>
					</label>
				</div>
			</div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Time span"); ?></span>
				<div class="tl-checkbox">
					<input class="startdate timepicker text-center"></input> <?php t("to"); ?> <input class="enddate timepicker text-center"></input>
				</div>
			</div>
			<div class="tl-section"><?php t("Display"); ?></div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Show calc. 100% range"); ?></span>
				<div class="tl-checkbox">
					<label class="tl-switch">
						<input id="checkbox100pct" type="checkbox" value="100pct">
						<span class="tl-switch-track"></span>
						<span><?php t("Enable"); ?></span>
					</label>
				</div>
			</div>
			<div class="tl-section"><?php t("URL"); ?></div>
			<div class="tl-form-item">
				<label for="URL_Admin" class="tl-label"><?php t("URL Admin Panel"); ?></label>
				<input id="URL_Admin" placeholder="http://raspberry/admin/">
			</div>
			<div class="tl-form-item">
				<label for="URL_Grafana" class="tl-label"><?php t("URL Grafana"); ?></label>
				<input id="URL_Grafana" placeholder="http://raspberry:3000/">
			</div>
			<div class="tl-form-item">
				<label for="HTTPPort" class="tl-label"><?php t("Teslalogger HTTP Port"); ?></label>
				<input id="HTTPPort" placeholder="5000">
			</div>
			<div class="tl-form-item">
				<label for="ZoomLevel" class="tl-label"><?php t("Zoom Level"); ?></label>
				<input id="ZoomLevel" size="4">
			</div>
			<div class="tl-section"><?php t("Integrations"); ?></div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("ScanMyTesla integration"); ?> <a href="https://teslalogger.de/smt.php" target="_blank"><img src="img/icon-help-24.png" class="tl-help" /></a></span>
				<div class="tl-checkbox">
					<label class="tl-switch">
						<input id="checkboxScanMyTesla" type="checkbox" value="ScanMyTesla">
						<span class="tl-switch-track"></span>
						<span><?php t("Enable"); ?></span>
					</label>
				</div>
			</div>
			<div class="tl-form-item">
				<span class="tl-label"><?php t("Position by StreamingAPI"); ?> <img id="StreamingPosHelp" src="img/icon-help-24.png" class="tl-help" /></span>
				<div class="tl-checkbox">
					<label class="tl-switch">
						<input id="StreamingPos" type="checkbox" value="StreamingPos">
						<span class="tl-switch-track"></span>
						<span><?php t("Enable"); ?></span>
					</label>
				</div>
			</div>
<?php
	$url = GetTeslaloggerURL("getallcars");
	$allcars = @file_get_contents($url);
	if ($allcars === false)
    {
        $error = error_get_last();
        $error = explode(': ', $error['message']);
        $error = trim($error[2]);
		echo("<div class=\"tl-alert tl-alert-danger\">Error: $error - URL: $url</div>");
		return;
    }
	$jcars = json_decode($allcars);
?>
			<div class="tl-section"><?php t("Vehicle"); ?></div>
			<div class="tl-form-item">
				<label for="defaultcar" class="tl-label"><?php t("Main Car"); ?></label>
				<?PHP CarsCombobox($jcars, $defaultcar); ?>
			</div>
		</div>
	</div>

<?php
	if ($jcars && count($jcars) > 0)
		echo("<div class=\"tl-car-grid\">");
	foreach ($jcars as $k => $v) {
		$displayname = $v->{"display_name"};
		$taskertoken = $v->{"tasker_hash"};    
		$lastscanmytesla = JSONDatetoString($v->{"lastscanmytesla"});
		if ($taskertoken == null)
			continue;
?>
	<div class="tl-card">
		<h2 class="tl-card-title"><?= htmlspecialchars($displayname) ?></h2>
		<dl class="tl-car-list">
			<dt><?php t("ScanMyTesla last received"); ?></dt>
			<dd><?= htmlspecialchars($lastscanmytesla) ?></dd>
			<dt><?php t("Tasker Token"); ?></dt>
			<dd><?= htmlspecialchars($taskertoken) ?></dd>
			<dt><?php t("Tasker URL"); ?></dt>
			<dd>
<?php
if (strlen($taskertoken) > 7)
	echo "https://teslalogger.de/wakeup.php?t=".$taskertoken;
?>
				<a href="https://teslalogger.de/faq-1.php" target="_blank"><img src="img/icon-help-24.png" class="tl-help" /></a>
			</dd>
			<dt><?php t("Received Tasker Token"); ?></dt>
			<dd>
<?php
if (strlen($taskertoken) > 7)
	echo file_get_contents("http://teslalogger.de/tasker_date.php?t=".$taskertoken, 0, stream_context_create(["http"=>["timeout"=>2]]));
?>
			</dd>
		</dl>
	</div>
<?php
}
	if ($jcars && count($jcars) > 0)
		echo("</div>");
?>
</div>

<div id="dialog-ShareDataHelp" title="Info" style="display:none;">
<?php t("TextShare"); ?>
</div>
<div id="dialog-StreamingPosHelp" title="Info" style="display:none;">
<?php t("StreamingPosHelp"); ?>
</div>
</body>
</html>
