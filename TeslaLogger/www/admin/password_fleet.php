<!DOCTYPE html>
<?php
require "language.php";
require_once "tools.php";

$actual_link = GetRequestProtocol() . "://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]";
$actual_link = htmlspecialchars($actual_link, ENT_QUOTES, 'UTF-8');
$actual_link = str_replace("&", "%26", $actual_link);

$dbid = isset($_REQUEST["id"]) ? intval($_REQUEST["id"]) : 0;
$vin = isset($_REQUEST["vin"]) ? $_REQUEST["vin"] : "";
$at = isset($_GET["AT"]) ? $_GET["AT"] : "";
$rt = isset($_GET["RT"]) ? $_GET["RT"] : "";
?>
<html lang="<?= $json_data["Language"]; ?>">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<title><?php t("Teslalogger Tesla Credentials"); ?></title>
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=28">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<script>
		var dbid = <?= $dbid ?>;
		var vin = <?= json_encode($vin) ?>;
		var tlDone = <?= json_encode(get_text("Done")) ?>;
		var tlPending = <?= json_encode(get_text("Pending")) ?>;

		// A pending status chip turns into a "Done" chip
		function setState(id) {
			var e = document.getElementById(id);
			e.textContent = tlDone;
			e.classList.add("done");
		}

		// The other way round, e.g. when a live check fails
		function setPending(id) {
			var e = document.getElementById(id);
			e.textContent = tlPending;
			e.classList.remove("done");
		}

		$(document).ready(function() {
			if (dbid > 0)
				enableCarControls(vin);
		});

		// After saving a new car the page behaves like the edit view: the VIN
		// replaces the select and the virtual key can be checked
		function enableCarControls(newvin) {
			$("#carid").hide();
			if (newvin && newvin.length > 5) {
				$("#vinlabel").text(newvin);
				// Once the vin is known: does it already have a subscription?
				checkSubscription(newvin);
			}
			$("#btncheckvkey").removeClass("tl-pf-hidden");
			$("#finirow").removeClass("tl-pf-sub");
		}

		// Last step: leave the setup and open the main page with the new car
		function finish() {
			if (dbid > 0)
				window.location.href = "index.php?carid=" + dbid;
		}

		// Ask (1 minute cache) whether the vin already has a subscription, so a
		// second one is not offered. If one exists the step shows as done.
		function checkSubscription(checkvin) {
			$.ajax({
				url: "subscription-check.php?vin=" + encodeURIComponent(checkvin)
			}).done(function(data) {
				$("#aborow").removeClass("tl-pf-sub");
				$("#checkabo").removeClass("tl-pf-hidden");
				if (data == "No subscription") {
					$("#abolink").removeClass("tl-pf-hidden");
				} else {
					// A subscription exists: show the step as done, no second one
					setState("checkabo");
				}
			}).fail(function() {
				// Unknown: offer the subscription like before
				$("#aborow").removeClass("tl-pf-sub");
				$("#checkabo").removeClass("tl-pf-hidden");
				$("#abolink").removeClass("tl-pf-hidden");
			});
		}

		// after window is loaded
		$(window).on('load', function() {
			<?php if (isset($_GET["AT"])) { ?>
				CheckAccessToken();
				$("#carid").prop("disabled", false);
				setState("checkauth");
				setState("checkAT");
				setState("checkRT");
			<?php } ?>
		});

		// Live check of the virtual key: ask Tesla (api/1/vehicles/fleet_status)
		// through the /checkvirtualkey/<carid> endpoint and set the chip to the result
		function checkVirtualKey() {
			if (dbid <= 0)
				return;

			$.post("teslaloggerstream.php", {
				url: "checkvirtualkey/" + dbid
			}, function(data) {
				var result = (data || "").trim();
				if (result == "True")
					setState("checkvkey");
				else if (result == "False")
					setPending("checkvkey");
				else
					console.log("checkvirtualkey: " + result);
			});
		}

		function save() {
			var teslacarid = $('#carid option:selected').attr('id');
			sendRequest();

			// The car now exists in the database: show its VIN and enable the
			// virtual key check even though the page was opened with id=-1
			if (teslacarid != undefined && teslacarid.length > 5)
				enableCarControls(teslacarid);

			if (teslacarid != undefined && teslacarid.length > 10) {
				$("#abolink").attr("href", "https://buy.stripe.com/9AQaHNdU33k29Vu144?client_reference_id=" + teslacarid);
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
					// A new car: remember its database id so the live checks work
					// and a reload opens the edit view
					if (dbid <= 0 && data && data.startsWith("ID:")) {
						dbid = parseInt(data.substring(3), 10);
						var teslacarid = $('#carid option:selected').attr('id');
						if (teslacarid)
							history.replaceState(null, "", "password_fleet.php?id=" + dbid + "&vin=" + teslacarid);
					}
					$("#carid").prop("disabled", false);
					setState("checkcar");
					setState("checkfreesuc");
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
				carid: vin,
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
					if (typeof tokenAvailable === "function" && tokenAvailable()) {
						alert("Check Logfile!");
						window.location.href = 'logfile.php';
					} else if (data && data.includes("ID:")) {
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
	?>
	<div class="tl-page">
		<div class="tl-alert tl-alert-danger"><?php t("Error"); ?>: <?= htmlspecialchars($error) ?> (URL: <?= htmlspecialchars($url) ?>)</div>
	</div>
	<?php
		return;
	}

	if (strpos($allcars, "not found!") === false) {
		$jcars = json_decode($allcars);

		if ($jcars == NULL) {
	?>
	<div class="tl-page">
		<div class="tl-alert tl-alert-danger"><?php t("Error"); ?>: JSON Parse Error! <?= htmlspecialchars($allcars) ?></div>
	</div>
	<?php
			return;
		}
	}

	$email = "";
	$tesla_carid = "0";
	$freesuc = "";
	$vkeydone = "";
	if (isset($_REQUEST["id"])) {
		foreach ($jcars as $k => $v) {
			if ($v->{"id"} == $_REQUEST["id"]) {
				$email = $v->{"tesla_name"};
				$tesla_carid = $v->{"tesla_carid"};
				if (!isset($vin) || strlen($vin) <= 14)
					$vin = $v->{"vin"};
				if ($v->{"freesuc"} == "1")
					$freesuc = "checked";
				// Virtual key check: needed and set, or not needed at all
				if ($v->{"needVirtualKey"} != 1 || $v->{"virtualkey"} == 1)
					$vkeydone = "done";
			}
		}
	}
	?>
	<div class="tl-page">
		<div class="tl-page-head">
			<h2 class="tl-page-title"><?php t("Teslalogger Tesla Credentials"); ?></h2>
			<a href="https://github.com/bassmaster187/TeslaLogger/blob/master/docs/en/installation/tesla-fleet-permission.md" target="_blank"><img src="img/icon-help-24.png" class="tl-help" /></a>
		</div>

		<div class="tl-pf">
			<div class="tl-card">
				<h2 class="tl-card-title"><?php t("Setup"); ?></h2>
				<ol class="tl-pf-steps">
					<li class="tl-pf-step">
						<div class="tl-pf-step-body">
							<div class="tl-pf-step-text"><?php t("PF_LOGOUT"); ?></div>
							<div class="tl-pf-links">
								<a class="tl-pf-pill tl-pf-pill-ext" href="https://www.tesla.com/teslaaccount/owner-xp/auth/logout?redirect=true&locale=en_US" target="_blank"><?php t("Log out"); ?></a>
								<a class="tl-pf-pill" href="https://www.youtube.com/watch?v=CjJPFdaAk44" target="_blank"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="16" height="16" aria-hidden="true"><path fill="#f00" d="M23.5 6.2a3 3 0 0 0-2.1-2.1C19.5 3.5 12 3.5 12 3.5s-7.5 0-9.4.6A3 3 0 0 0 .5 6.2 31.4 31.4 0 0 0 0 12a31.4 31.4 0 0 0 .5 5.8 3 3 0 0 0 2.1 2.1c1.9.6 9.4.6 9.4.6s7.5 0 9.4-.6a3 3 0 0 0 2.1-2.1A31.4 31.4 0 0 0 24 12a31.4 31.4 0 0 0-.5-5.8z"/><path fill="#fff" d="M9.5 15.5v-7l6.5 3.5z"/></svg><?php t("PF_HELP"); ?></a>
								<a class="tl-pf-pill tl-pf-pill-ext" href="https://github.com/bassmaster187/TeslaLogger/blob/master/docs/en/installation/tesla-fleet-permission.md" target="_blank"><?php t("PF_PERMISSION"); ?></a>
							</div>
						</div>
					</li>
					<li class="tl-pf-step">
						<div class="tl-pf-step-body">
							<div class="tl-pf-step-text">
								<span><?php t("PF_LOGIN"); ?></span>
								<span class="tl-pf-state" id="checkauth"><?php t("Pending"); ?></span>
							</div>
							<div class="tl-pf-links">
								<a class="tl-pf-pill tl-pf-pill-primary tl-pf-pill-ext" href="https://teslalogger.de/fleet-token.php?url=<?= $actual_link; ?>"><?php t("Get Tokens"); ?></a>
							</div>
						</div>
					</li>
					<li class="tl-pf-step">
						<div class="tl-pf-step-body">
							<div class="tl-pf-step-text">
								<span><?php t("Select your vehicle"); ?></span>
								<span class="tl-pf-vin" id="vinlabel"></span>
								<span class="tl-pf-state" id="checkcar"><?php t("Pending"); ?></span>
							</div>
							<select id="carid" disabled></select>
							<div class="tl-pf-links">
								<button type="button" id="btnSave" class="tl-pf-pill tl-pf-pill-primary" onclick="save();"><?php t("Save"); ?></button>
							</div>
						</div>
					</li>
					<li class="tl-pf-step">
						<div class="tl-pf-step-body">
							<div class="tl-pf-step-text"><?php t("PF_VKEY"); ?></div>
							<div class="tl-pf-links">
								<a class="tl-pf-pill tl-pf-pill-ext" target="_blank" href="https://www.tesla.com/_ak/teslalogger.de"><?php t("Virtual Key"); ?></a>
							</div>
						</div>
					</li>
					<li class="tl-pf-step">
						<div class="tl-pf-step-body">
							<div class="tl-pf-step-text">
								<span><?php t("PF_VKEYCHECK"); ?></span>
								<span class="tl-pf-state<?= $vkeydone ? " done" : "" ?>" id="checkvkey"><?php t($vkeydone ? "Done" : "Pending"); ?></span>
							</div>
							<div class="tl-pf-links">
								<button type="button" id="btncheckvkey" class="tl-pf-pill tl-pf-pill-primary<?= $dbid > 0 ? "" : " tl-pf-hidden" ?>" onclick="checkVirtualKey();"><?php t("Check"); ?></button>
							</div>
						</div>
					</li>
					<li class="tl-pf-step tl-pf-sub" id="aborow">
						<div class="tl-pf-step-body">
							<div class="tl-pf-step-text">
								<span><?php t("PF_SUBSCRIPTION"); ?></span>
								<span class="tl-pf-state tl-pf-hidden" id="checkabo"><?php t("Pending"); ?></span>
							</div>
							<div class="tl-pf-links">
								<a class="tl-pf-pill tl-pf-pill-ext tl-pf-hidden" target="_blank" href="#" id="abolink"><?php t("Subscription"); ?></a>
							</div>
						</div>
					</li>
					<li class="tl-pf-step tl-pf-sub" id="finirow">
						<div class="tl-pf-step-body">
							<div class="tl-pf-step-text"><?php t("PF_FINISH"); ?></div>
							<div class="tl-pf-links">
								<button type="button" id="btnfinish" class="tl-pf-pill tl-pf-pill-primary tl-pf-donebtn" onclick="finish();"><?php t("Done"); ?></button>
							</div>
						</div>
					</li>
				</ol>
			</div>

			<div class="tl-card">
				<h2 class="tl-card-title"><?php t("Credentials"); ?></h2>
				<div class="tl-form tl-small-form">
					<div class="tl-form-item">
						<div class="tl-pf-labelline">
							<label for="access_token"><?php t("Access Token"); ?></label>
							<span class="tl-pf-state" id="checkAT"><?php t("Pending"); ?></span>
						</div>
						<input class="tl-small-input" id="access_token" type="text" autocomplete="off" readonly value="<?= htmlspecialchars($at, ENT_QUOTES) ?>" />
					</div>
					<div class="tl-form-item">
						<div class="tl-pf-labelline">
							<label for="refresh_token"><?php t("Refresh Token"); ?></label>
							<span class="tl-pf-state" id="checkRT"><?php t("Pending"); ?></span>
						</div>
						<input class="tl-small-input" id="refresh_token" type="text" autocomplete="off" readonly value="<?= htmlspecialchars($rt, ENT_QUOTES) ?>" />
					</div>
					<div class="tl-form-item">
						<div class="tl-pf-labelline">
							<label><?php t("Free Supercharging"); ?></label>
							<span class="tl-pf-state" id="checkfreesuc"><?php t("Pending"); ?></span>
						</div>
						<label class="tl-switch"><input type="checkbox" id="freesuc" value="1" <?= $freesuc ?>><span class="tl-switch-track"></span></label>
					</div>
					<?php if ($dbid != -1) { ?>
					<div class="tl-form-actions">
						<button id="deletebutton" onclick="deleteCar();" class="redbutton"><?php t("Delete"); ?></button>
					</div>
					<?php } ?>
				</div>
			</div>
		</div>
	</div>
	<?php
	?>
</body>
</html>
