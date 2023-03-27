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
	<link rel="stylesheet" href="static/teslalogger_style.css">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="jquery/jquery-migrate-1.4.1.min.js"></script>
	<script src="static/jquery/datatables/1.10.22/js/jquery.dataTables.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
	<link rel='stylesheet' href="static/jquery/datatables/1.10.22/css/jquery.dataTables.min.css">

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
        if (!($("#host").val().startsWith("http://") || $("#host").val().startsWith("https://")))
            alert("Host must start with http:// or https://");

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
<div style="max-width: 1260px;">
<?php 
include "menu.php";
menu("Wallbox Test");
?>
<div>
    <h1><?php t("Wallbox"); ?>:  <a href="https://github.com/bassmaster187/TeslaLogger/blob/master/wallbox.md"><img src="img/icon-help-24.png" class="pointer"/></a></h1>
<div>
<p><?php t("WallboxComment"); ?></p>
<table>
    <tr><td><?php t("Type"); ?>:</td><td>
        <select name="type" id="type">
        <option value=""></option>
            <option value="go-e">Go e-Charger</option>    
            <option value="openwb">Open WB</option>
            <option value="shelly3em">Shelly 3EM</option>
            <option value="shellyem">Shelly EM</option>
            <option value="tesla-gen3">Tesla Wallbox Gen 3</option>
            <option value="keba">Keba KeContact P30</option>
        </select>
        </td></tr>
    <tr><td><?php t("Host"); ?>:</td><td><input id="host" type="text" /></td></tr>
    <tr><td><?php t("Param"); ?>:</td><td><input id="param" type="text" /></td></tr>
    <tr><td></td><td style="text-align: right;"><button onclick="btn_test_click();"><?php t("Test"); ?></td></tr>
    <tr><td></td><td></td></tr>
    <tr><td colspan=2><h1><?php t("Info"); ?>:</h1></td></tr>
    <tr><td><?php t("Version"); ?>:</td><td><span id="Version"></td></span></tr>
    <tr><td><?php t("Utility kWh"); ?>: </td><td><span id="Utility_kWh"></td></span></tr>
    <tr><td><?php t("Vehicle kWh"); ?>: </td><td><span id="Vehicle_kWh"></td></span></tr>
    <tr><td></td><td style="text-align: right;"><button onclick="btn_save_click();"><?php t("Save"); ?></td></tr>
</table>
</div>
</body>
</html>

