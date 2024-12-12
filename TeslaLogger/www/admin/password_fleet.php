<!DOCTYPE html>
<?php
require "language.php";
require_once "tools.php";

$actual_link = (empty($_SERVER['HTTPS']) ? 'http' : 'https') . "://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]";
$actual_link = htmlspecialchars($actual_link, ENT_QUOTES, 'UTF-8');
$actual_link = str_replace("&", "%26", $actual_link);
?>
<html lang="<?= $json_data["Language"]; ?>">
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
	<link rel='stylesheet' id='genericons-css' href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
	<script>
		<?php
		if (isset($_REQUEST["id"]))
			echo ("var dbid=" . $_REQUEST["id"] . ";\n");
		?>
		$(document).ready(function() {
			$('input[type="checkbox"]').on('click keyup keypress keydown', function(event) {
				if ($(this).is('[readonly]')) {
					return false;
				}
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
							$(this).dialog("close");
						}
					}
				});
			});

			if (dbid > 0) {
				$("#carid").hide();
				$("#vinlabel").text("<?= $_REQUEST["vin"] ?>");
			}
		});

		// after window is loaded
		$(window).on('load', function() {
			<?php
			if (isset($_GET["AT"])) {
			?>
				CheckAccessToken();
				$("#carid").prop("disabled", false);
				$("#btn").prop("disabled", false);
				$("#checkauth").html("&#9989;");
				$("#checkAT").html("&#9989;");
				$("#checkRT").html("&#9989;");

			<?php
			} ?>
		});

		function save() {
			sendRequest();

			var teslacarid = $('#carid option:selected').attr('id');
			if (teslacarid.length > 10) {
				$("#abolink").attr("href", "https://buy.stripe.com/9AQaHNdU33k29Vu144?client_reference_id=" + teslacarid);
				$("#aborow").show();
			}
		}

		function sendRequest() {
			var teslacarid = $('#carid option:selected').attr('id');

			if (teslacarid == undefined || teslacarid == "") {
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

			var jqxhr = $.post("teslaloggerstream.php", {
					url: "setpassword",
					data: JSON.stringify(d)
				})
				.always(function(data) {
					$("#carid").prop("disabled", false);
					$("#btnSave").hide();
					$("#checkcar").html("&#9989;");
					$("#checkfreesuc").html("&#9989;");
					
				});
		}

		function deleteCar() {
			if (confirm("<?php t('Do you really want to delete this vehicle?'); ?>")) {
				var d = {
					id: dbid,
					deletecar: 1
				};

				var jqxhr = $.post("teslaloggerstream.php", {
					url: "setpassword",
					data: JSON.stringify(d)
				}).always(function() {
					alert("<?php t('Check Logfile in one minute!'); ?>");
					window.location.href = 'index.php';
				});
			}
		}

		function ChangeAccessTokenAndRefreshToken() {
			var d = {
				id: dbid,
				carid: "<?= $_REQUEST["vin"] ?>",
				freesuc: $("#freesuc").is(':checked'),
				access_token: $("#access_token").val(),
				refresh_token: $("#refresh_token").val(),
				fleetAPI: true,
			};

			var jqxhr = $.post("teslaloggerstream.php", {
					url: "setpassword",
					data: JSON.stringify(d)
				})
				.always(function(data) {
					if (tokenAvailable()) {
						alert("Check Logfile!");
						window.location.href = 'logfile.php';
					} else if (data.includes("ID:")) {
						window.location.href = 'password_info.php?id=' + data.substr(3);
					} else {
						window.location.href = 'password_info.php?id=' + dbid;
					}
				});

		}

		function CheckAccessToken() {
			if (dbid > 0) {
				ChangeAccessTokenAndRefreshToken()
				return;
			}

			// new car
			var d = {
				access_token: $("#access_token").val()
			};

			var jqxhr = $.post("teslaloggerstream.php", {
				url: "getcarsfromaccountfleetapi",
				data: JSON.stringify(d)
			}, function(data) {
				$("#carid").empty();

				if (data == "Unauthorized")
					alert("<?php t('Unauthorized'); ?>");
				else if (data.startsWith("ERROR:"))
					alert(data);
				else {
					var obj = JSON.parse(data);
					for (var i = 0; i < obj.length; i++) {
						$("#carid").append("<option id='" + obj[i]['Key'] + "'>" + obj[i]['Value'] + "</option>");
						$("#btnSave").css("visibility", "");
					}
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
		$url = GetTeslaloggerURL("getallcars");

		$allcars = @file_get_contents($url);
		if ($allcars === false) {
			$error = error_get_last();
			$error = explode(': ', $error['message']);
			$error = trim($error[2]);
			echo ("<h1>errortext = 'Error: $error - URL: $url'</h1>");
			return;
		}

		if (strpos($allcars, "not found!") === false) {
			$jcars = json_decode($allcars);
			//var_dump($allcars);
			//var_dump($jcars);

			if ($jcars == NULL) {
				echo ("<h1>JSON Parse Error!</h1>");
				echo ("JSON: " . htmlspecialchars($allcars));
				return;
			}
		}

		if (isset($_REQUEST["id"])) {
			$email = "";
			$tesla_carid = "0";
			$disablecarid = "";
			$freesuc = "";
			foreach ($jcars as $k => $v) {
				if ($v->{"id"} == $_REQUEST["id"]) {
					$email = $v->{"tesla_name"};
					$tesla_carid = $v->{"tesla_carid"};
					$vin = $v->{"vin"};
					if (isset($vin) && strlen($vin) > 14) {
						// $disablecarid = " disabled ";
					}
					if ($v->{"freesuc"} == "1")
						$freesuc = "checked";
				}
			}
		?>
			<div>
				<h1><?php t("Please enter your Tesla account credentials"); ?>:</h1>
				<div id="dialog-TokenHelp" title="Info">
					<table id="t1">
						<tr>
							<td width="25px">1.</td>
							<td colspan="2"><?php t("PF_LOGOUT"); ?> <a href="https://www.tesla.com/teslaaccount/owner-xp/auth/logout?redirect=true&locale=en_US" target="_blank"><?php t("PF_LINK"); ?></a><br>
							<?php t("PF_HELP"); ?>: <a href="https://www.youtube.com/watch?v=CjJPFdaAk44" target="_blank"><img src="https://teslalogger.de/youtube.svg" height="18px"></a><br>
							<?php t("PF_PERMISSION"); ?>: <a href="https://github.com/bassmaster187/TeslaLogger/blob/master/docs/en/tesla-fleet-permission.md" target="_blank"><?php t("PF_LINK"); ?></a>
							</td>
							<td></td>
						</tr>	
						<tr>
							<td>2.</td>
							<td colspan="2"><?php t("PF_LOGIN"); ?>: <a href="https://teslalogger.de/fleet-token.php?url=<?= $actual_link; ?>"><?php t("PF_LINK"); ?></a></td>
							<td><span id="checkauth"></span></td>
						</tr>
						<tr>
							<td>3.</td>
							<td><?php t("Access Token"); ?>:&nbsp;</td>
							<td><input id="access_token" type="text" disabled <?php
																				if (isset($_GET["AT"]))
																					echo ' value="' . $_GET["AT"] . '"';
																				?>></td>
							<td><span id="checkAT"></span></td>
						</tr>
						<tr>
							<td></td>
							<td><?php t("Refresh Token"); ?>:&nbsp;</td>
							<td><input id="refresh_token" type="text" disabled <?php
																				if (isset($_GET["RT"]))
																					echo ' value="' . $_GET["RT"] . '"';
																				?>></td>
							<td><span id="checkRT"></span></td>
						</tr>
						<tr>
							<td>4.</td>
							<td><?php t("Car"); ?>:&nbsp;</td>
							<td> <select id="carid" style="width: 100%;" disabled></select><span id="vinlabel"></span></td>
							<td><span id="checkcar"></span></td>
						</tr>
						<tr height="35px">
							<td></td>
							<td><?php t("Free Supercharging"); ?>:&nbsp;</td>
							<td><input id="freesuc" type="checkbox" <?= $freesuc ?> /></td>
							<td><span id="checkfreesuc"></span></td>
						</tr>
						<tr>
							<td></td>
							<td colspan="2">
								<?PHP
								if ($_REQUEST["id"] != -1) { ?><button id="deletebutton" onclick="deleteCar();" class="redbutton"><?php t("Delete"); ?></button>
								<?PHP }
								?>
								<button id="btnSave" onclick="save();" style="float: right; visibility: collapse"><?php t("Save"); ?></button>
							</td>
						</tr>
						<tr>
							<td>5.</td>
							<td colspan="2"><?php t("PF_VKEY"); ?>: <a target="_blank" href="https://www.tesla.com/_ak/teslalogger.de"><?php t("PF_LINK"); ?></a></td>
						<tr>
						<tr id="aborow" style="display:none;">
							<td>6.</td>
							<td colspan="2"><?php t("PF_SUBSCRIPTION"); ?>: <a target="_blank" href="#" id="abolink"><?php t("PF_LINK"); ?></a></td>
						<tr>
					</table>
				</div>
			<?php
		} else {
			?>
			<?php
		}
			?>
			</div>