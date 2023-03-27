<!DOCTYPE html>
<?php
require("language.php");
require_once("tools.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?php t("Teslalogger Tesla Zugangsdaten"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />

	<script>
	<?php
	if (isset($_REQUEST["carid"]))
		echo("var dbid=".$_REQUEST["carid"].";\n");
    else 
        echo("var dbid=-1;\n");
	?>
	$(document).ready(function(){
		
	});

	function save() 
	{
		var d = {
                    login: $("#login").val(),
					password: $("#password").val(),
					id: dbid,
					carname: $("#carname").val()
				};

			var jqxhr = $.post("teslaloggerstream.php", {url: "setpasswordovms", data: JSON.stringify(d)})
			.always(function (data) {
				if (tokenAvailable())
				{
					alert("Check Logfile!");
					window.location.href='logfile.php';
				}
				else if (data.includes("ID:"))
				{
					window.location.href='password_info.php?id='+data.substr(3);
				}
				else
				{
					window.location.href='password_info.php?id='+dbid;
				}
				});
	}

	
	function deleteCar()
	{
		if (confirm("Do you want to delete?"))
		{
			var d = {
					id: dbid,
					deletecar: 1
				};

			var jqxhr = $.post("teslaloggerstream.php", {url: "setpassword", data: JSON.stringify(d)}).always(function () {
					alert("Check Logfile in one minute!");
					window.location.href='index.php';
				});
		}
	}

</script>
</head>
<style>
.redbutton{background-color:#890d24;float: left;}
.redbutton:hover, .redbutton:focus{background-color:#A62A41;}
</style>
<body style="padding-top: 5px; padding-left: 10px;">
<div style="max-width: 1260px;">
<?php 
include "menu.php";
menu("Credentials");
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
	
	if (strpos($allcars, "not found!") === false)
	{
		$jcars = json_decode($allcars);
		//var_dump($allcars);
		//var_dump($jcars);

		if ($jcars == NULL)
		{
			echo("<h1>JSON Parse Error!</h1>");
			echo("JSON: ". htmlspecialchars($allcars));
			return;
		}
	}


	$email = "";
	$tesla_carid = "0";
	$disablecarid = "";
	$freesuc = "";
	foreach ($jcars as $k => $v) {
		if ($v->{"id"} == $_REQUEST["id"])
		{
			$email = $v->{"tesla_name"};
			$tesla_carid = $v->{"tesla_carid"};
			$vin = $v->{"vin"};
			if (isset($vin) && strlen($vin)>14)
			{
				// $disablecarid = " disabled ";
			}
			if ( $v->{"freesuc"} == "1")
				$freesuc = "checked";
		}
	}

?>
<div>
<h1><?php t("Please enter your dexters-web.de account credentials"); ?>:</h1>
<table>
<tr><td><?php t("Dexter Login"); ?>:</td><td><input id="login" type="text" autocomplete="new-password"></td></tr>
<tr><td><?php t("Dexter Password"); ?>:</td><td><input id="password" type="text" autocomplete="new-password"></td></tr>
<tr><td><?php t("Car Name"); ?>:</td><td><input id="carname" type="text" autocomplete="new-password"></td></tr>
<tr><td colspan="2">
<?PHP 
if ($_REQUEST["id"] > 0)
	{ ?><button id="deletebutton" onclick="deleteCar();" class="redbutton"><?php t("Löschen"); ?></button>
	<?PHP }
?>
<button id="btnSave" onclick="save();" style="float: right;"><?php t("Speichern"); ?></button></td></tr>
</table>
</div>
<?php

?>
</div>
