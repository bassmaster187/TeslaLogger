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

	function GetTokensFromURL()
	{
		var url = $("#authresulturl").val();

		if (!url.toLowerCase().startsWith('https://'))
		{
			alert("<?php t('This isn\'t a valid URL!'); ?>");
			return;
		}

		if (!url.toLowerCase().startsWith('https://auth.tesla.'))
		{
			alert("<?php t('This isn\'t an auth link by Tesla!'); ?>");
			return;
		}

		if (!url.toLowerCase().includes('code='))
		{
			alert("<?php t('The URL doesn\'t contain the expected format!'); ?>");
			return;
		}

		var d = {
			url : url
		};
		var jqxhr = $.post("teslaloggerstream.php", {url: "teslaauthtoken", data: JSON.stringify(d)}, function(data){
			const obj = JSON.parse(data);

			if (obj.error != null)
			{
				alert(obj.error);
				return;
			}

			$("#browserauth").hide();
			$("html,body").scrollTop(0);
			$("#access_token").val(obj.AccessToken);
			$("#refresh_token").val(obj.RefreshToken);
			CheckAccessToken();
		});
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
<?php t("TeslaAuthApps"); ?>

<h3><?php t("BA_ALLCARS"); ?>:</h3>
<ul>
<li><?php t("BA_FLEETAPI"); ?>: <a href="<?php 
$TeslaFleetURL = str_replace("password.php", "password_fleet.php", $actual_link);
echo $TeslaFleetURL;
?>"><?php t("PF_LINK"); ?></a></li>
</ul>
<h3><?php t("BA_MODELSXOLD"); ?>:</h3>
<ul>
<li><?php
	$t1=get_text("BA_Browser");
	$t1=str_replace("{", '<a href="javascript:BrowserAuth();">', $t1);
	$t1=str_replace("}", '</a>', $t1);
	echo $t1;
?></li>
<li>Android: <a href="https://play.google.com/store/apps/details?id=net.leveugle.teslatokens">Tesla Tokens</a></li>
<li>iOS: <a href="https://apps.apple.com/us/app/auth-app-for-tesla/id1552058613#?platform=iphone">Auth app for Tesla</a></li>
</ul>
<div style="display: none" id="browserauth">
<hr>
<h1><?php t("BA_Read"); ?></h1>
<ul>
<li><?php t("BA_Logon"); ?></li>
<li><?php t("BA_Auth"); ?></li>
<li><?php t("BA_NotFound"); ?></li>
<li><?php t("BA_URL"); ?></li>
<li><?php t("BA_Invalid"); ?></li>
<li><?php
	$t1=get_text("BA_Logoff");
	$t1=str_replace("{", '<a href="https://www.tesla.com/teslaaccount/owner-xp/auth/logout?redirect=true&locale=en_US" target="_blank">', $t1);
	$t1=str_replace("}", '</a>', $t1);
	echo $t1;
?></li>
<li><?php t("BA_GetToken"); ?></li>
<li><?php t("BA_SelectCar"); ?></li>
</ul>
<h2><?php t("BA_Step1"); ?></h2>
	<?php t("BA_FillOut"); ?> <a href="#" id="authlink" target="_blank">Tesla Logon.</a>
<h2><?php t("BA_Step2"); ?></h2>
<?php t("BA_CopyUrl"); ?><br>
	<img src="img/auth_screenshot.png">
<h2><?php t("BA_Step3"); ?></h2>
<?php t("BA_Paste"); ?>
<input id="authresulturl"></input>
<br>
<button onclick="GetTokensFromURL();"><?php t("Get Tokens"); ?></button>
<br><br>
<hr>
</div>

<table>
<tr><td><?php t("Access Token"); ?>:&nbsp;</td><td><input id="access_token" type="text" autocomplete="new-password"></td></tr>
<tr><td><?php t("Refresh Token"); ?>:&nbsp;</td><td><input id="refresh_token" type="text" autocomplete="new-password"></td></tr>

<tr><td colspan="2"><button onclick="CheckAccessToken();" style="float: right;"><?php t("OK"); ?></button></td></tr>

<tr style='visibility:collapse'><td><b><?php t("Email"); ?>:&nbsp;</b></td><td><input id="email" type="text" autocomplete="new-password" value="<?php echo($email) ?>" <?php echo($disablecarid) ?>/></td></tr>
<tr style='visibility:collapse'><td><?php t("Password"); ?>:&nbsp;</td><td><input id="password1" type="password" autocomplete="new-password" /></td></tr>
<tr style='visibility:collapse'><td><?php t("Repeat Password"); ?>:&nbsp;</td><td><input id="password2" type="password" autocomplete="new-password" /></td></tr>

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
	$tinfo = get_text("INFO_FLEETAPI");
	$tinfo=str_replace("{LINK1}", "<a href='https://developer.tesla.com/docs/fleet-api/announcements#2024-11-27-pay-per-use-pricing' target='_blank'>Tesla Pay per use pricing</a>", $tinfo);
	$tinfo=str_replace("{LINK2}", "<a href='https://digitalassets.tesla.com/tesla-contents/image/upload/Fleet-API-Agreement-EN.pdf' target='_blank'>Fleet API Agreement</a>", $tinfo);    
?>
<div>
<h1><?php t("INFO_important"); ?>:</h1>
<p><?php echo($tinfo); ?>:</p>
<h1><?php t("Please choose your vehicle"); ?>:</h1>
<table id="cars" class="">
	<thead>
		<tr>
			<th><?php t("ID"); ?></th>
			<th><?php t("Name"); ?></th>
			<th><?php t("Model"); ?></th>
			<th><?php t("VIN"); ?></th>
			<th><?php t("Tasker Token"); ?></th>
			<th><?php t("Aktiv"); ?></th>
			<th style='text-align:center;'><?php t("Free SUC"); ?></th>
			<th style='text-align:center;'>Fleet API</th>
			<th style='text-align:center;'>Virtual Key</th>
			<!-- <th style='text-align:center;'>Access Type</th> -->
			<th style='text-align:center;'>Signal Counter</th>
			<th style='text-align:center;'><?php t("Subscription"); ?></th>
			<th><?php t("Edit"); ?></th>
		</tr>
	</thead>
	<tbody>
<?php
		//var_dump($url);

		foreach ($jcars as $k => $v) {
			$display_name = $v->{"display_name"};
			$tasker_token = $v->{"tasker_hash"};
			$car = $v->{"model_name"};
			$id = $v->{"id"};
			$vin = $v->{"vin"};
			$tesla_carid = $v->{"tesla_carid"};
			$access_type = $v->{"access_type"};
			$inactive = $v->{"inactive"} == 1;

			$cartype = $v->{"car_type"};
			$NeedSubscription = $v->{"SupportedByFleetTelemetry"} == "1";
			
			$freesuccheckbox = GetCheckbox($v->{"freesuc"});
			$fleetAPICheckBox = "";
			
			if ($v->{"fleetAPI"} == "0" && $NeedSubscription)
				$fleetAPICheckBox = "<a href='password_fleet.php?id=$id&vin=$carVIN'>".get_text("FleetAPIRequired")." ⚠️</a>";
			else
				$fleetAPICheckBox = GetCheckbox($v->{"fleetAPI"});

			$virtualKeyCheckBox = GetCheckbox($v->{"virtualkey"});

			$activeCheckBox = GetCheckbox(!$inactive);

			echo("	<tr>\r\n");
			echo("		<td>$id</td>\r\n");
			echo("		<td>$display_name <a href='changecarname.php?carid=$id'>&#9998</a></td>\r\n");
			echo("		<td>$car</td>\r\n");
			echo("		<td>$vin</td>\r\n");
			echo("		<td>$tasker_token</td>\r\n");
			echo("		<td style='text-align:center;'>$activeCheckBox</td>\r\n");
			echo("		<td style='text-align:center;'>$freesuccheckbox</td>\r\n");
			echo("		<td style='text-align:center;'>$fleetAPICheckBox</td>\r\n");
			echo("		<td style='text-align:center;'>$virtualKeyCheckBox</td>\r\n");
			// echo("		<td style='text-align:center;'>$access_type</td>\r\n");
			
			echo("		<td style='text-align:center;'>");
			if ($v->{"fleetAPI"} == "1")
				echo(file_get_contents("https://teslalogger.de:4501/SignalCounter/$vin"));
			echo("</td>\r\n");

			echo("		<td style='text-align:center;'>");
			if ($NeedSubscription && !$inactive)
			{
				$subscription = file_get_contents("https://teslalogger.de/stripe/subscription-check.php?vin=$vin");
				
				if (strpos($subscription, "current_period_end") > 0)
				{
					echo(GetCheckbox("1"));
					echo("&nbsp;<a target='_blank' href='https://billing.stripe.com/p/login/8wMaGogxma56fGUdQQ'>". get_text("SubscribeManage") ."</a>");
				}
				else
					echo("<a target='_blank' href='https://buy.stripe.com/9AQaHNdU33k29Vu144?client_reference_id=$vin'>⚠️ ". get_text("Subscribe") ."</a>");
			}
			echo("</td>\r\n");


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

<?php
function GetCheckbox($v)
{
	if ($v == "1")
		return '<input type="checkbox" checked="checked" readonly valign="center" />';

	return '<input type="checkbox" readonly valign="center" />';	
}