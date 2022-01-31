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
	<script src="static/jquery/datatables/1.10.22/js/jquery.dataTables.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
	<link rel='stylesheet' href="static/jquery/datatables/1.10.22/css/jquery.dataTables.min.css">

	<script>
	<?php
	if (isset($_REQUEST["id"]))
		echo("var dbid=".$_REQUEST["id"].";\n");
	?>
	$(document).ready(function(){
		$('input[type="checkbox"]').on('click keyup keypress keydown', function (event) {
    		if($(this).is('[readonly]')) { return false; }
		});

		$("#cars").DataTable();

		$("#TokenHelp").click(function() {
			$("#dialog-TokenHelp").dialog({
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
	});

	function tokenAvailable() {
		if ($("#access_token").val() == null || $("#access_token").val().length < 2)
		{
			alert("Please enter the access token!");
			return false;
		}

		if ($("#refresh_token").val() == null || $("#refresh_token").val().length < 2)
		{
			alert("Please enter the refresh token!");
			return false;
		}

		return true;
	}

	function save() {
		if (tokenAvailable())
		{
			sendRequest();
		}
		/* email authentification not supported anymore
		else if ($("#email").val() == null || $("#email").val() == "")  {
			alert("Bitte Email eingeben!");
		} else if ($("#password1").val() == null || $("#password1").val() == "") {
			alert("Bitte Passwort eingeben!");
		} else if ($("#password1").val() != $("#password2").val()) {
			alert("Passwörter stimmen nicht überein!");
		} else {			
			sendRequest();
		}
		*/
	}

	function sendRequest()
	{
		var teslacarid = $('#carid option:selected').attr('id');

		if (teslacarid == undefined || teslacarid == "")
		{
			alert("Please select a car!");
			return;
		}

		var d = {
					email: $("#email").val(),
					password: $("#password1").val(),
					carid: teslacarid,
					id: dbid,
					freesuc: $("#freesuc").is(':checked'),
					access_token: $("#access_token").val(),
					refresh_token: $("#refresh_token").val(),
				};

			var jqxhr = $.post("teslaloggerstream.php", {url: "setpassword", data: JSON.stringify(d)})
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

	function reconnect()
	{
		var d = {
					id: dbid,
					reconnect: 1
				};

		var jqxhr = $.post("teslaloggerstream.php", {url: "setpassword", data: JSON.stringify(d)}).always(function () {
				window.location.href='password_info.php?id='+dbid;
			});
	}

	function CheckAccessToken()
	{
		if (!tokenAvailable())
			return;

		var d = {
					access_token: $("#access_token").val()
				};

		var jqxhr = $.post("teslaloggerstream.php", {url: "getcarsfromaccount", data: JSON.stringify(d)}, function(data){
			$("#carid").empty();

			if (data == "Unauthorized")
				alert("Unauthorized");
			else if (data.startsWith("ERROR:"))
				alert(data);
			else
			{
				var obj = JSON.parse(data);
				for (var i=0; i < obj.length; i++)
				{
					$("#carid").append("<option id='"+obj[i]['Key']+"'>"+obj[i]['Value']+"</option>");
					$("#btnSave").css("visibility","");
				}
			}
		});
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

if (isset($_REQUEST["id"]))
{
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
<h1><?php t("Bitte Tesla Account Zugangsdaten eingeben"); ?>:</h1>
<div id="dialog-TokenHelp" title="Info">
<?php t("TeslaAuthApps"); ?>
<ul>
<li>Android: <a href="https://play.google.com/store/apps/details?id=net.leveugle.teslatokens">Tesla Tokens</a></li>
<li>iOS: <a href="https://apps.apple.com/us/app/auth-app-for-tesla/id1552058613#?platform=iphone">Auth app for Tesla</a></li>
</ul>
</div>
<table>
<tr><td><?php t("Access Token"); ?>:</td><td><input id="access_token" type="text" autocomplete="new-password"></td></tr>
<tr><td><?php t("Refresh Token"); ?>:</td><td><input id="refresh_token" type="text" autocomplete="new-password"></td></tr>

<tr><td colspan="2"><button onclick="CheckAccessToken();" style="float: right;"><?php t("OK"); ?></button></td></tr>

<tr style='visibility:collapse'><td><b><?php t("Email"); ?>:</b></td><td><input id="email" type="text" autocomplete="new-password" value="<?php echo($email) ?>" <?php echo($disablecarid) ?>/></td></tr>
<tr style='visibility:collapse'><td><?php t("Passwort"); ?>:</td><td><input id="password1" type="password" autocomplete="new-password" /></td></tr>
<tr style='visibility:collapse'><td><?php t("Passwort wiederholen"); ?>:</td><td><input id="password2" type="password" autocomplete="new-password" /></td></tr>

<tr><td><?php t("Car"); ?>:</td><td> <select id="carid"></select></td></tr>
<tr height="35px"><td><?php t("Free Supercharging"); ?>:</td><td><input id="freesuc" type="checkbox" <?= $freesuc ?> /></td></tr>

<tr><td>&nbsp;</td></tr>
<!-- <tr><td colspan="2"><b>Or you can use Tesla Access Token &amp; Refresh Token to login:&nbsp;&nbsp;</b></td><td><img id="TokenHelp" src="img/icon-help-24.png" class="pointer"/></td></tr> -->
<tr><td>&nbsp;</td></tr>

<tr><td colspan="2">
<?PHP 
if ($_REQUEST["id"] != -1)
	{ ?><button id="deletebutton" onclick="deleteCar();" class="redbutton"><?php t("Löschen"); ?></button>
	<!-- &nbsp;<button onclick="reconnect();"><?php t("Reconnect"); ?></button>&nbsp; -->
	<?PHP }
?>
<button id="btnSave" style='visibility:collapse' onclick="save();" style="float: right;"><?php t("Speichern"); ?></button></td></tr>
</table>
</div>
<?php
}
else
{
?>
<div>
<h1><?php t("Bitte Fahrzeug auswählen"); ?>:</h1>
<table id="cars" class="">
<thead><tr><th>ID</th><th>Email</th><th>#</th><th>Name</th><th>Model</th><th>VIN</th><th>Tasker Token</th><th style='text-align:center;'>Free SUC</th><th>Edit</th></tr></thead>
<tbody>
<?php
	//var_dump($url);

	foreach ($jcars as $k => $v) {
		$email = $v->{"tesla_name"};
		$display_name = $v->{"display_name"};
		$tasker_token = $v->{"tasker_hash"};    
		$car = $v->{"model_name"};  
		$id = $v->{"id"};
		$vin = $v->{"vin"};
		$tesla_carid = $v->{"tesla_carid"};
		$freesuc = $v->{"freesuc"};
		$freesuccheckbox = '<input type="checkbox" readonly valign="center" />';
		if ($freesuc == "1")
			$freesuccheckbox = '<input type="checkbox" checked="checked" readonly valign="center" />';
		
		
		echo("   <tr><td>$id</td><td>$email</td><td>$tesla_carid</td><td>$display_name</td><td>$car</td><td>$vin</td><td>$tasker_token</td><td style='text-align:center;'>$freesuccheckbox</td><td><a href='password.php?id=$id'>EDIT</a></td></tr>\r\n");
	}
?>
<tr><td colspan="6"><button onclick="location.href='password.php?id=-1'">NEW CAR</button></td><td></td><td></td><td></td></tr>
</tbody>
</table>
</div>
<?php
}
?>
</div>
