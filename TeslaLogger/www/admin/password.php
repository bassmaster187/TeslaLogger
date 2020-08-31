<!DOCTYPE html>
<?php
require("language.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?php t("Teslalogger Tesla Zugangsdaten"); ?></title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="https://teslalogger.de/teslalogger_style.css">
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<script src="https://code.jquery.com/jquery-migrate-1.4.1.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='https://www.impala64.de/blog/tesla/wp-content/themes/twentyfourteen/genericons/genericons.css?ver=3.0.3' type='text/css' media='all' />

	<script>
	function save() {
		if ($("#email").val() == null || $("#email").val() == "") {
			alert("Bitte Email eingeben!");
		} else if ($("#password1").val() == null || $("#password1").val() == "") {
			alert("Bitte Passwort eingeben!");
		} else if ($("#password1").val() != $("#password2").val()) {
			alert("Passwörter stimmen nicht überein!");
		} else {
			var jqxhr = $.post("password_write.php", {
					email: $("#email").val(),
					password: $("#password1").val()
				}).always(function () {
					alert("Bitte eine Minute warten!");
					window.location.href='index.php';
				});
		}
	}
</script>
</head>
<body style="padding-top: 5px; padding-left: 10px;">
<?php 
include "menu.php";
menu("Credentials");
if (isset($_REQUEST["id"]))
{
?>
<div>
<h1><?php t("Bitte Tesla Account Zugangsdaten eingeben"); ?>:</h1>
<table>
<tr><td><b><?php t("Email"); ?>:</b></td><td><input id="email" type="text" autocomplete="new-password"  /></td></tr>
<tr><td><?php t("Passwort"); ?>:</td><td><input id="password1" type="password" autocomplete="new-password" /></td></tr>
<tr><td><?php t("Passwort wiederholen"); ?>:</td><td><input id="password2" type="password" autocomplete="new-password" /></td></tr>
<tr><td></td><td><button onclick="save();" style="float: right;"><?php t("Speichern"); ?></button></td></tr>
</table>
</div>
<?php
}
else
{
?>
<div>
<h1><?php t("Bitte Fahrzeug auswählen"); ?>:</h1>
<table>
<tr><th>DB ID</th><th>Email</th><th>Car in Account</th><th>Name</th><th>Model</th><th>VIN</th><th>Tasker Token</th></tr>
<?php
	$url = GetTeslaloggerURL("getallcars");
	$allcars = file_get_contents($url);
	$jcars = json_decode($allcars);
	// var_dump($url);

	foreach ($jcars as $k => $v) {
		$email = $v->{"tesla_name"};
		$display_name = $v->{"display_name"};
		$tasker_token = $v->{"tasker_hash"};    
		$car = $v->{"model_name"};  
		$id = $v->{"id"};
		$vin = $v->{"vin"};
		$tesla_carid = $v->{"tesla_carid"};
		
		echo("   <tr><td>$id</td><td>$email</td><td>$tesla_carid</td><td>$display_name</td><td>$car</td><td>$vin</td><td>$tasker_token</td><td><a href='password.php?id=$id'>EDIT</a></td></tr>\r\n");
	}
?>
<tr><td><a href='password.php?id=-1'>NEUS FAHRZEUG</a></td><td></td><td></td><td></td></tr>
</table>
</div>
<?php
}

