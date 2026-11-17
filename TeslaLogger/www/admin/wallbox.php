<!DOCTYPE html>
<?php
require_once("language.php");
require_once("tools.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?php t("Wallbox"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=9">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />

	<script>
	<?php
	if (isset($_REQUEST["carid"]))
        echo("var carid=".$_REQUEST["carid"].";\n");
    else
        echo("var carid=-1;\n");
	?>

    $(document).ready(function(){
        var url = "wallbox";
        var d = {
                    load: "1",
                    carid: carid
				};
        var jqxhr = $.post("teslaloggerstream.php", {url: url, data: JSON.stringify(d)}).always(function (data) {
            var json = JSON.parse(data);
            $("#type").val(json.type);
            $("#host").val(json.host);
            $("#param").val(json.param);

            btn_test_click();
        });
	});
	
    function btn_test_click()
    {
        var host = $("#host").val();

        if (host == "")
            return;

        if (!(host.startsWith("http://") || host.startsWith("https://")))
        {
            alert("Host must start with http:// or https://");
            return;
        }

        var url = "wallbox";
        console.log("url: " + url);

        var d = {
                    test: "1",
					type: $("#type").val(),
					host: $("#host").val(),
					param: $("#param").val(),
				};

        var jqxhr = $.post("teslaloggerstream.php", {url: url, data: JSON.stringify(d)}).always(function (data) {
			var loc;
			if (navigator.languages != undefined) loc = navigator.languages[0]; 
				else loc = navigator.language;
            var json = JSON.parse(data);
            $("#Version").text(json.Version);
			
			if (typeof(json.Utility_kWh) === "number" ) {
				$("#Utility_kWh").text(json.Utility_kWh.toLocaleString(loc,{maximumFractionDigits:3, minimumFractionDigits: 3}));
			}
			if (typeof(json.Vehicle_kWh) === "number" ) {
				$("#Vehicle_kWh").text(json.Vehicle_kWh.toLocaleString(loc,{maximumFractionDigits:3, minimumFractionDigits: 3}));
			}
        });
    }

    function btn_save_click()
    {
        var url = "wallbox";
        console.log("url: " + url);

        var d = {
                    save: "1",
                    carid: carid,
					type: $("#type").val(),
					host: $("#host").val(),
					param: $("#param").val(),
				};

        var jqxhr = $.post("teslaloggerstream.php", {url: url, data: JSON.stringify(d)}).always(function (data) {
			var loc;
			if (navigator.languages != undefined) loc = navigator.languages[0]; 
				else loc = navigator.language;
			var json = JSON.parse(data);
			$("#Version").text(json.Version);
			if (typeof(json.Utility_kWh) === "number" ) {
				$("#Utility_kWh").text(json.Utility_kWh.toLocaleString(loc,{maximumFractionDigits:3, minimumFractionDigits: 3}));
			}
			if (typeof(json.Vehicle_kWh) === "number" ) {
				$("#Vehicle_kWh").text(json.Vehicle_kWh.toLocaleString(loc,{maximumFractionDigits:3, minimumFractionDigits: 3}));
			}
        });
    }

</script>
</head>
<body>
<?php 
include "menu.php";
menu("Wallbox Test");
?>
<div class="tl-page">
	<div class="tl-page-head">
		<h2 class="tl-page-title"><?php t("Wallbox"); ?></h2>
		<?php
		$helpUrl = ($language == "de") ? "https://teslalogger.de/docs/extras/wallbox/" : "https://teslalogger.de/en/docs/extras/wallbox/";
		?>
		<a href="<?= $helpUrl ?>" target="_blank"><img src="img/icon-help-24.png" class="tl-help" /></a>
		<div class="tl-actions">
			<button onclick="btn_test_click();"><?php t("Test"); ?></button>
			<button onclick="btn_save_click();"><?php t("Save"); ?></button>
		</div>
	</div>

	<div class="tl-card">
		<p class="tl-muted" style="margin-top:0;"><?php t("WallboxComment"); ?></p>
		<img class="tl-wallbox-screenshot" src="https://teslalogger.de/assets/images/extras-wallbox-01-fd2c7e445905431c37ab6c06e50ca18a.png" alt="<?php t("Wallbox"); ?>" />
	</div>

	<div class="tl-card tl-wallbox-form-card">
		<h2 class="tl-card-title"><?php t("Settings"); ?></h2>
		<div class="tl-form tl-small-form">
			<div class="tl-form-item">
				<label for="type"><?php t("Type"); ?></label>
				<select name="type" id="type">
					<option value=""></option>
					<option value="cfos">cFos</option>
					<option value="go-e">Go e-Charger</option>
					<option value="openwb">Open WB</option>
					<option value="openwb2">Open WB 2</option>
					<option value="shelly3em">Shelly 3EM</option>
					<option value="shellyem">Shelly EM</option>
					<option value="smartevse3">SmartEVSE 3</option>
					<option value="tesla-gen3">Tesla Wallbox Gen 3</option>
					<option value="keba">Keba KeContact P30</option>
					<option value="evcc">EVCC</option>
					<option value="warp">WARP</option>
				</select>
			</div>
			<div class="tl-form-item">
				<label for="host"><?php t("Host"); ?></label>
				<input class="tl-small-input" id="host" type="text" />
			</div>
			<div class="tl-form-item">
				<label for="param"><?php t("Param"); ?></label>
				<input class="tl-small-input" id="param" type="text" />
			</div>
		</div>
	</div>

	<div class="tl-card">
		<h2 class="tl-card-title"><?php t("Info"); ?></h2>
		<div class="tl-row-list">
			<div class="tl-row-list-item"><span class="tl-muted"><?php t("Version"); ?></span><span id="Version">-</span></div>
			<div class="tl-row-list-item"><span class="tl-muted"><?php t("Utility kWh"); ?></span><span id="Utility_kWh">-</span></div>
			<div class="tl-row-list-item"><span class="tl-muted"><?php t("Vehicle kWh"); ?></span><span id="Vehicle_kWh">-</span></div>
		</div>
	</div>
</div>
</body>
</html>

