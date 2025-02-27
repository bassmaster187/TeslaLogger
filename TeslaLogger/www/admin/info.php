
function ShowInfo()
{
    <?php
	if (session_status() == PHP_SESSION_NONE) {
		session_start();
	}
	global $fleetapiinfo;
	global $vehicle_location;
	global $carVIN;

    $fileinfofleetapi = "/tmp/fleetapiinfo".date("Y-m-d").".txt";
	$filevehicle_location = "/tmp/vehicle_location-$carid-".date("Y-m-d").".txt";
	$prefix = "/etc/teslalogger/";
    if (isDocker())
		$prefix = "/tmp/";

    
	echo ("\r\n<!-- fleetapiinfo: show: $fleetapiinfo -->\r\n");

	if ($fleetapiinfo === true
		&& !file_exists($fileinfofleetapi)
		)
    {
        file_put_contents($fileinfofleetapi, ''); 
        $tinfo = get_text("INFO_FLEETAPI"). "<br>FleetAPI: $fleetapiinfo";
        $tinfo=str_replace("{LINK1}", "<a href='https://developer.tesla.com/docs/fleet-api/announcements#2024-11-27-pay-per-use-pricing' target='_blank'>Tesla Pay per use pricing</a>", $tinfo);
        $tinfo=str_replace("{LINK2}", "<a href='https://digitalassets.tesla.com/tesla-contents/image/upload/Fleet-API-Agreement-EN.pdf' target='_blank'>Fleet API Agreement</a>", $tinfo);
        ?>

        $("#InfoText").html("<h1><?php t("INFO_important"); ?></h1><p><?php echo($tinfo); ?></p>");
        $(".HeaderT").show();
        $("#PositiveButton").text("<?php t("OK"); ?>");
        $("#PositiveButton").click(function(){location.reload();});
        $("#NegativeButton").text("<?php t("Credentials"); ?>");
        $("#NegativeButton").click(function(){window.location.href='password.php';});

        <?php
    }
	else if ($vehicle_location != null && $vehicle_location != "True"  && !file_exists($filevehicle_location))
	{
		file_put_contents($filevehicle_location, ''); 
		$passwordlink = "password_fleet.php?id=$carid&vin=$carVIN";
		$tinfo = get_text("INFO_VEHICLE_LOCATION");
        $tinfo=str_replace("{LINK1}", "<a href='https://developer.tesla.com/docs/fleet-api/announcements#2024-11-26-introducing-a-new-oauth-scope-vehicle-location' target='_blank'>LINK</a>", $tinfo);
        $tinfo=str_replace("{LINK2}", "<a href='$passwordlink' target='_blank'>LINK</a>", $tinfo);
        ?>

        $("#InfoText").html("<h1><?php t("INFO_important"); ?></h1><p><?php echo($tinfo); ?></p>");
        $(".HeaderT").show();
        $("#PositiveButton").text("<?php t("OK"); ?>");
        $("#PositiveButton").click(function(){location.reload();});
        $("#NegativeButton").text("<?php t("Credentials"); ?>");
        $("#NegativeButton").click(function(){window.location.href='<?php echo $passwordlink; ?>';});

        <?php
	}
	else if (file_exists($prefix."cmd_gosleep_$carid.txt"))
	{?>
		$("#InfoText").html("<h1><?php t("TextSuspendTeslalogger"); ?></h1>");
		$(".HeaderT").show();
		$("#PositiveButton").text("<?php t("Resume Teslalogger"); ?>");
		$("#PositiveButton").click(function(){window.location.href='/wakeup.php?id=' + <?= $carid ?>;});
		$("#NegativeButton").hide();
	<?php
	}
	else if (!file_exists("/etc/teslalogger/sharedata.txt") &&
	!file_exists("/etc/teslalogger/nosharedata.txt") &&
	!file_exists("/tmp/sharedata.txt") &&
	!file_exists("/tmp/nosharedata.txt")
	)
	{?>
		$("#InfoText").html("<?php t("TextShare"); ?>");
		$(".HeaderT").show();
		$("#PositiveButton").click(function(){window.location.href='settings_share.php?a=yes';});
		$("#NegativeButton").click(function(){window.location.href='settings_share.php?a=no';});
	<?php
	}
	else if(isDocker() && GrafanaVersion() != "10.0.1")
	{?>
		<?php
		$t1=get_text("Please update to latest docker-compose.yml file. Check: {LINK}");
		$t1=str_replace("{", "<a href='https://github.com/bassmaster187/TeslaLogger/blob/master/docker_setup.md#docker-update--upgrade'>", $t1);
		$t1=str_replace("}", '</a>', $t1);
		?>
		$("#InfoText").html("<h1><?php echo $t1; ?></h1>");
		$(".HeaderT").show();
		$("#PositiveButton").click(function(){window.location.href='https://github.com/bassmaster187/TeslaLogger/blob/master/docker_setup.md#docker-update--upgrade';});
		$("#NegativeButton").hide();
	<?php
	} else if (isDocker() && !isDockerNET8() && !DatasourceUpdated())
	{?>
		$("#InfoText").html("<h1>Please update datasource.yaml file. Check: <a href='https://github.com/bassmaster187/TeslaLogger/blob/master/docker_setup.md#docker-update--upgrade'>LINK</a></h1>");
		$(".HeaderT").show();
		$("#PositiveButton").click(function(){window.location.href='https://github.com/bassmaster187/TeslaLogger/blob/master/docker_setup.md#docker-update--upgrade';});
		$("#NegativeButton").hide();
	<?php
	}
	else if (!files_are_equal("/etc/teslalogger/changelog.md","/tmp/changelog.md"))
	{?>
		$.get("changelog_plain.php").success(function(data){
			$("#InfoText").html(data);
		});

		$(".HeaderT").show();
		$("#PositiveButton").text("<?php t("OK"); ?>");
		$("#PositiveButton").click(function(){window.location.href='changelogread.php';});
		$("#NegativeButton").hide();
	<?php
	}
	?>
}

function showInfoRestricted() {
	$("#InfoText").html("<?php t("INFO_RESTRICTED"); ?>");
	$(".HeaderT").show();
	$("#PositiveButton").text("<?php t("Subscribe"); ?>");
	$("#PositiveButton").click(function(){window.location.href='https://buy.stripe.com/9AQaHNdU33k29Vu144?client_reference_id=<?php echo $carVIN; ?>';});
	$("#NegativeButton").text("<?php t("OK"); ?>");
	$("#NegativeButton").click(function(){location.reload();});
}