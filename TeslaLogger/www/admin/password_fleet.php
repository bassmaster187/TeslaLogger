<!DOCTYPE html>
<?php
require("language.php");
require_once("tools.php");

$actual_link = (empty($_SERVER['HTTPS']) ? 'http' : 'https') . "://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]";
$actual_link = htmlspecialchars( $actual_link, ENT_QUOTES, 'UTF-8' );
$actual_link = str_replace("&","%26", $actual_link);


?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?php t("Teslalogger Tesla Credentials"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css">
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
		$('input[type="checkbox"]').on('click keyup keypress keydown', function (event) {
    		if($(this).is('[readonly]')) { return false; }
		});

		$("#cars").DataTable({
			lengthChange: false,
			bFilter: false,
			paging: false,
			info: false
		});

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

		if (dbid > 0)
		{
			$("#carid").hide();
			$("#vinlabel").text("<?php echo $_REQUEST["vin"] ?>");
		}
	});

	function tokenAvailable() {
		if ($("#access_token").val() == null || $("#access_token").val().length < 2)
		{
			alert("<?php t('Please enter the access token!'); ?>");
			return false;
		}

		if ($("#refresh_token").val() == null || $("#refresh_token").val().length < 2)
		{
			alert("<?php t('Please enter the refresh token!'); ?>");
			return false;
		}

		return true;
	}

	function save() {
		if (tokenAvailable())
		{
			sendRequest();
		}
	}

	function sendRequest()
	{
		var teslacarid = $('#carid option:selected').attr('id');

		if (teslacarid == undefined || teslacarid == "")
		{
			alert("<?php t('Please select a vehicle'); ?>");
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
					fleetAPI: true,
				};

			var jqxhr = $.post("teslaloggerstream.php", {url: "setpassword", data: JSON.stringify(d)})
			.always(function (data) {
				if (tokenAvailable())
				{
					alert("<?php t('Check Logfile!'); ?>");
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
		if (confirm("<?php t('Do you really want to delete this vehicle?'); ?>"))
		{
			var d = {
					id: dbid,
					deletecar: 1
				};

			var jqxhr = $.post("teslaloggerstream.php", {url: "setpassword", data: JSON.stringify(d)}).always(function () {
					alert("<?php t('Check Logfile in one minute!'); ?>");
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

	function ChangeAccessTokenAndRefreshToken()
	{
		var d = {
					id: dbid,
					carid: "<?php echo $_REQUEST["vin"] ?>",
					freesuc: $("#freesuc").is(':checked'),
					access_token: $("#access_token").val(),
					refresh_token: $("#refresh_token").val(),
					fleetAPI: true,
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

	function CheckAccessToken()
	{
		if (!tokenAvailable())
			return;

		if (dbid > 0)
		{
			ChangeAccessTokenAndRefreshToken()
			return;
		}

		// new car
		var d = {
					access_token: $("#access_token").val()
				};

		var jqxhr = $.post("teslaloggerstream.php", {url: "getcarsfromaccount", data: JSON.stringify(d)}, function(data){
			$("#carid").empty();

			if (data == "Unauthorized")
				alert("<?php t('Unauthorized'); ?>");
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

	function BrowserAuth()
	{
		var jqxhr = $.post("teslaloggerstream.php", {url: "teslaauthurl", data: "url"}, function(data){
			$("#authlink").attr("href", data);
		});

		$("#access_token").val("");
		$("#refresh_token").val("");
		$("#authresulturl").val("");

		$("#browserauth").show();
	}


</script>
</head>
<body>
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
<h1><?php t("Please enter your Tesla account credentials"); ?>:</h1>
<div id="dialog-TokenHelp" title="Info">
Go to Tesla Authentification Page and gain access to Teslalogger. After you got your Access Token / Refresh Token, hit Ok to use the Fleet Api.
<ul>
<li>Fleet API: <a href="https://teslalogger.de/fleet-token.php?url=<?php echo $actual_link;?>">From Tesla</a>
<li>Send command: <a href="https://www.tesla.com/_ak/teslalogger.de">Link</a> (not required for pre 2021 Model S/X)</li>
</ul>
<table>
<tr><td><?php t("Access Token"); ?>:&nbsp;</td><td><input id="access_token" type="text" autocomplete="new-password"
<?php
	if (isset($_GET["AT"]))
		echo ' value="'.$_GET["AT"].'"';
?>
></td></tr>
<tr><td><?php t("Refresh Token"); ?>:&nbsp;</td><td><input id="refresh_token" type="text" autocomplete="new-password"
<?php
	if (isset($_GET["RT"]))
		echo ' value="'.$_GET["RT"].'"';
?>
></td></tr>

<tr><td colspan="2"><button onclick="CheckAccessToken();" style="float: right;"><?php t("OK"); ?></button></td></tr>

<tr><td><?php t("Car"); ?>:&nbsp;</td><td> <select id="carid" style="width: 100%;"></select><span id="vinlabel"></span></td></tr>
<tr height="35px"><td><?php t("Free Supercharging"); ?>:&nbsp;</td><td><input id="freesuc" type="checkbox" <?= $freesuc ?> /></td></tr>

<tr><td>&nbsp;</td></tr>
<!-- <tr><td colspan="2"><b>Or you can use Tesla Access Token &amp; Refresh Token to login:&nbsp;&nbsp;</b></td><td><img id="TokenHelp" src="img/icon-help-24.png" class="pointer"/></td></tr> -->
<tr><td>&nbsp;</td></tr>

<tr><td colspan="2">
<?PHP
if ($_REQUEST["id"] != -1)
	{ ?><button id="deletebutton" onclick="deleteCar();" class="redbutton"><?php t("Delete"); ?></button>
	<!-- &nbsp;<button onclick="reconnect();"><?php t("Reconnect"); ?></button>&nbsp; -->
	<?PHP }
?>
<button id="btnSave" style='visibility:collapse' onclick="save();" style="float: right;"><?php t("Save"); ?></button></td></tr>
</table>
</div>
<?php
}
else
{
?>
<div>
<h1><?php t("Please choose your vehicle"); ?>:</h1>
<table id="cars" class="">
	<thead>
		<tr>
			<th><?php t("ID"); ?></th>
			<th><?php t("Email"); ?></th>
			<th>#</th>
			<th><?php t("Name"); ?></th>
			<th><?php t("Model"); ?></th>
			<th><?php t("VIN"); ?></th>
			<th><?php t("Tasker Token"); ?></th>
			<th style='text-align:center;'><?php t("Free SUC"); ?></th>
			<th><?php t("Edit"); ?></th>
		</tr>
	</thead>
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

			echo("	<tr>\r\n");
			echo("		<td>$id</td>\r\n");
			echo("		<td>$email</td>\r\n");
			echo("		<td>$tesla_carid</td>\r\n");
			echo("		<td>$display_name</td>\r\n");
			echo("		<td>$car</td>\r\n");
			echo("		<td>$vin</td>\r\n");
			echo("		<td>$tasker_token</td>\r\n");
			echo("		<td style='text-align:center;'>$freesuccheckbox</td>\r\n");
			echo("		<td><a href='password.php?id=$id&vin=$vin'>");
			echo t("Edit");
			echo("</a></td>\r\n");
			echo("	</tr>\r\n");
		}
	?>
	</tbody>
</table>
<p></p>
<button onclick="location.href='password.php?id=-1'"><?php t("New car"); ?></button>
</div>
<?php
}
?>
</div>
