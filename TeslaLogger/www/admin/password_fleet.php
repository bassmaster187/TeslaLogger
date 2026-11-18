<!DOCTYPE html>
<?php
require "language.php";
require_once "tools.php";

$actual_link = GetRequestProtocol() . "://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]";
$actual_link = htmlspecialchars($actual_link, ENT_QUOTES, 'UTF-8');
$actual_link = str_replace("&", "%26", $actual_link);
?>
<html lang="<?= $json_data["Language"]; ?>">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<title><?php t("Teslalogger Tesla Credentials"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=17">
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
	<div class="tl-page">
		<div class="tl-page-head">
			<h2 class="tl-page-title"><?php t("Teslalogger Tesla Credentials"); ?></h2>
		</div>

		<div id="dialog-TokenHelp" title="Info">
			<div class="tl-card">
				<h2 class="tl-card-title"><?php t("Please enter your Tesla account credentials"); ?></h2>
				<ol class="tl-pf-steps">
					<li>
						<div class="tl-pf-field">
							<span class="tl-pf-line"><?php t("PF_LOGOUT"); ?> <a href="https://www.tesla.com/teslaaccount/owner-xp/auth/logout?redirect=true&locale=en_US" target="_blank"><?php t("PF_LINK"); ?></a></span>
							<span class="tl-pf-line"><?php t("PF_HELP"); ?>: <a class="tl-youtube-logo" href="https://www.youtube.com/watch?v=CjJPFdaAk44" target="_blank"><svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 492 110" height="18" aria-hidden="true"><path d="M154.3 17.5a19.6 19.6 0 0 0-13.8-13.8C128.4.4 79.7.4 79.7.4S31 .5 18.9 3.8A19.6 19.6 0 0 0 5.1 17.6C1.44 39.1.02 71.86 5.2 92.5A19.6 19.6 0 0 0 19 106.3c12.1 3.3 60.8 3.3 60.8 3.3s48.7 0 60.8-3.3a19.6 19.6 0 0 0 13.8-13.8c3.86-21.53 5.05-54.27-.1-75z" fill="red"/><path fill="#fff" d="M64.2 78.4L104.6 55 64.2 31.6z"/><g fill="currentColor"><path d="M227.9 99.7c-3.1-2.1-5.3-5.3-6.6-9.7s-1.9-10.2-1.9-17.5v-9.9c0-7.3.7-13.3 2.2-17.7 1.5-4.5 3.8-7.7 7-9.7s7.3-3.1 12.4-3.1c5 0 9.1 1 12.1 3.1s5.3 5.3 6.7 9.7 2.1 10.3 2.1 17.6v9.9c0 7.3-.7 13.1-2.1 17.5s-3.6 7.6-6.7 9.7c-3.1 2-7.3 3.1-12.5 3.1-5.4.1-9.6-1-12.7-3zM245.2 89c.9-2.2 1.3-5.9 1.3-10.9V56.8c0-4.9-.4-8.5-1.3-10.7-.9-2.3-2.4-3.4-4.5-3.4s-3.5 1.1-4.4 3.4-1.3 5.8-1.3 10.7v21.3c0 5 .4 8.7 1.2 10.9s2.3 3.3 4.5 3.3c2.1 0 3.6-1.1 4.5-3.3zm219.2-16.3v3.5l.4 9.9c.3 2.2.8 3.8 1.6 4.8s2.1 1.5 3.8 1.5c2.3 0 3.9-.9 4.7-2.7.9-1.8 1.3-4.8 1.4-8.9l13.3.8c.1.6.1 1.4.1 2.4 0 6.3-1.7 11-5.2 14.1s-8.3 4.7-14.6 4.7c-7.6 0-12.9-2.4-15.9-7.1s-4.6-12.1-4.6-22V61.6c.34-17 3.33-29.45 20.9-29.5 5.3 0 9.3 1 12.1 2.9s4.8 4.9 6 9 1.7 9.7 1.7 16.9v11.7h-25.7zm2-28.8c-.8 1-1.3 2.5-1.6 4.7s-.4 10-.4 10v4.9h11.2v-4.9c0 4.9-.1-7.7-.4-10s-.8-3.9-1.6-4.8-2-1.4-3.6-1.4c-1.7.1-2.9.6-3.6 1.5zM190.5 71.4L173 8.2h15.3s7.15 31.7 9.6 46.6h.4c2.78-15.82 9.8-46.6 9.8-46.6h15.3l-17.7 63.1v30.3h-15.1V71.4z"/><path id="A" d="M311.5 33.4v68.3h-12l-1.3-8.4h-.3c-3.3 6.3-8.2 9.5-14.7 9.5-11.77-.03-13.08-10-13.2-18.4v-51h15.4v50.1c0 3 .3 5.2 1 6.5 1.42 2.78 5.1 2.07 7.1.7a8 8 0 0 0 2.7-3.1V33.4z"/><path d="M353.3 20.6H338v81.1h-15V20.6h-15.3V8.2h45.5v12.4zm87.9 23.7c-.9-4.3-2.4-7.4-4.5-9.4-2.1-1.9-4.9-2.9-8.6-2.9a14.1 14.1 0 0 0-7.9 2.4c-2.5 1.6-4.3 3.7-5.7 6.3h-.1v-36h-14.8v96.9h12.7l1.6-6.5h.3a14 14 0 0 0 5.3 5.5c2.4 1.3 5 2 7.9 2 5.2 0 9-2.4 11.5-7.2 2.4-4.8 3.7-12.3 3.7-22.4V62.2c0-7.6-.5-13.6-1.4-17.9zm-14.1 27.9c0 5-.2 8.9-.6 11.7s-1.1 4.8-2.1 6-2.3 1.8-3.9 1.8c-3.1-.1-4.86-1.5-6.1-3.6V49.3c.5-1.9 1.4-3.4 2.7-4.6 2.2-2.47 5.96-2.5 7.7 0 .9 1.2 1.4 3.3 1.8 6.2.3 2.9.5 7 .5 12.4z"/></g><use xlink:href="#A" x="78.9" fill="currentColor"/></svg></a></span>
							<span class="tl-pf-line"><?php t("PF_PERMISSION"); ?>: <a href="https://github.com/bassmaster187/TeslaLogger/blob/master/docs/en/tesla-fleet-permission.md" target="_blank"><?php t("PF_LINK"); ?></a></span>
						</div>
					</li>
					<li>
						<div class="tl-pf-field">
							<span><?php t("PF_LOGIN"); ?>: <a href="https://teslalogger.de/fleet-token.php?url=<?= $actual_link; ?>"><?php t("PF_LINK"); ?></a></span>
							<span class="tl-pf-check" id="checkauth"></span>
						</div>
					</li>
					<li>
						<div class="tl-pf-field">
							<span><?php t("Access Token"); ?></span>
							<input id="access_token" type="text" disabled <?php
							if (isset($_GET["AT"]))
								echo ' value="' . $_GET["AT"] . '"';
							?>>
							<span class="tl-pf-check" id="checkAT"></span>
						</div>
					</li>
					<li>
						<div class="tl-pf-field">
							<span><?php t("Refresh Token"); ?></span>
							<input id="refresh_token" type="text" disabled <?php
							if (isset($_GET["RT"]))
								echo ' value="' . $_GET["RT"] . '"';
							?>>
							<span class="tl-pf-check" id="checkRT"></span>
						</div>
					</li>
					<li>
						<div class="tl-pf-field">
							<span><?php t("Car"); ?></span>
							<select id="carid" disabled></select>
							<span id="vinlabel"></span>
							<span class="tl-pf-check" id="checkcar"></span>
						</div>
					</li>
					<li>
						<div class="tl-pf-field">
							<span><?php t("Free Supercharging"); ?></span>
							<label class="tl-switch"><input type="checkbox" id="freesuc" value="1" <?= $freesuc ?>><span class="tl-switch-track"></span></label>
							<span class="tl-pf-check" id="checkfreesuc"></span>
						</div>
					</li>
					<li>
						<div class="tl-pf-field">
							<?php t("PF_VKEY"); ?>: <a target="_blank" href="https://www.tesla.com/_ak/teslalogger.de"><?php t("PF_LINK"); ?></a>
						</div>
					</li>
					<li id="aborow" style="display:none;">
						<div class="tl-pf-field">
							<?php t("PF_SUBSCRIPTION"); ?>: <a target="_blank" href="#" id="abolink"><?php t("PF_LINK"); ?></a>
						</div>
					</li>
				</ol>
				<div class="tl-form-actions" style="display:flex; justify-content:flex-end; margin-top:16px;">
					<?php if ($_REQUEST["id"] != -1) { ?>
					<button id="deletebutton" onclick="deleteCar();" class="redbutton" style="margin-right:auto"><?php t("Delete"); ?></button>
					<?php } ?>
					<button id="btnSave" onclick="save();" style="visibility: collapse"><?php t("Save"); ?></button>
				</div>
			</div>
		</div>
	</div>
	<?php
	} else {
	?>
	<?php
	}
	?>
</body>
</html>
