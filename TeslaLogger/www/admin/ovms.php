<!DOCTYPE html>
<?php
require("language.php");
require_once("tools.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<title>Teslalogger <?php t("Tesla Credentials"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=14">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel='stylesheet' id='genericons-css' href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />

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
<body>
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
<div class="tl-page">
	<div class="tl-page-head">
		<h2 class="tl-page-title"><?php t("Please enter your dexters-web.de account credentials"); ?></h2>
	</div>

	<div class="tl-card tl-small-form-card">
		<div class="tl-form tl-small-form">
			<div class="tl-form-item">
				<label for="login"><?php t("Dexter Login"); ?></label>
				<input class="tl-small-input" id="login" type="text" autocomplete="new-password"></div>
			<div class="tl-form-item">
				<label for="password"><?php t("Dexter Password"); ?></label>
				<input class="tl-small-input" id="password" type="text" autocomplete="new-password"></div>
			<div class="tl-form-item">
				<label for="carname"><?php t("Car Name"); ?></label>
				<input class="tl-small-input" id="carname" type="text" autocomplete="new-password"></div>
			<div class="tl-form-actions">
				<?PHP if ($_REQUEST["id"] > 0) { ?>
				<button id="deletebutton" onclick="deleteCar();" class="redbutton" style="margin-right:auto"><?php t("Delete"); ?></button>
				<?PHP } ?>
				<button id="btnSave" onclick="save();"><?php t("Save"); ?></button>
			</div>
		</div>
	</div>
</div>
<?php

?>
</body>
</html>