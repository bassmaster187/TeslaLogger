<!DOCTYPE html>
<?php
require("language.php");
require_once("tools.php");

$actual_link = (empty($_SERVER['HTTPS']) ? 'http' : 'https') . "://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]";

?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?php t("Teslalogger Tesla Credentials"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=4">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<script src="static/jquery/datatables/1.13.4/datatables.min.js"></script>
	<link rel='stylesheet' href="static/jquery/datatables/1.13.4/datatables.min.css">
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />

	<script>
	<?php
	if (isset($_REQUEST["id"]))
		echo("var dbid=".$_REQUEST["id"].";\n");
	?>
	$(document).ready(function(){
		
	});

	
function save() {
	var d = {
		email: $("#email").val(),
		password: $("#password").val(),
		region: $("#region").val(),
	};

	var jqxhr = $.post("teslaloggerstream.php", {url: "lucid/getallcars", data: JSON.stringify(d)})
	.always(function (data) {
		$("#carid").empty();

		if (data == "Error: StatusCode.UNAUTHENTICATED")
		{
			alert("<?php t('Wrong credentials!'); ?>");
			return;
		}
		else {
			$("#carid").closest("tr").css("visibility", "visible");
			var obj = JSON.parse(data);
			for (var i=0; i < obj.length; i++)
			{
				$("#btnSave").closest("tr").css("visibility", "visible");
				$("#carid").append("<option id='"+obj[i]['VIN']+"'>"+obj[i]['VIN']+ " - "+obj[i]['Nickname']+  " - "+obj[i]['Model'] + "</option>");
				$("#btnSave").css("visibility","");
			}
		}
	});

}

function saveCar() {
	var d = {
		email: $("#email").val(),
		password: $("#password").val(),
		region: $("#region").val(),
		vin: $("#carid option:selected").attr("id"),
		id: dbid
	};

	var jqxhr = $.post("teslaloggerstream.php", {url: "lucid/savecar", data: JSON.stringify(d)})
	.always(function (data) {
		$("#carid").empty();

		if (data == "Error: StatusCode.UNAUTHENTICATED")
		{
			alert("<?php t('Wrong credentials!'); ?>");
			return;
		}
		else {
			alert("<?php t('Check Logfile!'); ?>");
			window.location.href='logfile.php';
		}
	});

}

</script>
</head>
<body>
<div style="max-width: 1260px;">
<?php
include "menu.php";
menu("Credentials");

if (isset($_REQUEST["id"]))
{
?>
<div>
<h1><?php t("Please enter your Lucid Motors account credentials"); ?>:</h1>

<table>

<tr><td><b><?php t("Email"); ?>:&nbsp;</b></td><td><input id="email" type="text" autocomplete="new-password" value="<?php echo($email) ?>" <?php echo($disablecarid) ?>/></td></tr>
<tr><td><?php t("Password"); ?>:&nbsp;</td><td><input id="password" type="password" autocomplete="new-password" /></td></tr>
<tr><td><?php t("Region"); ?>:&nbsp;</td><td>
<select id="region" style="width: 100%;">
	<option value="us">USA</option>
	<option value="eu">Europe</option>
	<option value="sa">Saudi Arabia</option>
</select></td></tr>
<tr><td></td><td><button id="btnGetCars" onclick="save();" style="float: right;"><?php t("Get Cars"); ?></button></td></tr>

<tr style='visibility:collapse'><td>&nbsp;</td></tr>
<tr style='visibility:collapse'><td><?php t("Car"); ?>:&nbsp;</td><td> <select id="carid" style="width: 100%;"></select><span id="vinlabel"></span></td></tr>
<tr><td>&nbsp;</td></tr>

<tr style='visibility:collapse'><td colspan="2">
<?PHP
if ($_REQUEST["id"] != -1)
	{ ?><button id="deletebutton" onclick="deleteCar();" class="redbutton"><?php t("Delete"); ?></button>
	<!-- &nbsp;<button onclick="reconnect();"><?php t("Reconnect"); ?></button>&nbsp; -->
	<?PHP }
?>
<button id="btnSave" onclick="saveCar();" style="float: right;"><?php t("Save"); ?></button></td></tr>
</table>
</div>
<?php
}

?>
</div>

<?php
function GetCheckbox($v)
{
	if ($v == "1")
		return '<input type="checkbox" checked="checked" readonly valign="center" />';

	return '<input type="checkbox" readonly valign="center" />';	
}
