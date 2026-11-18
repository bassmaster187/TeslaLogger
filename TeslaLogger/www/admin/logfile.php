<!DOCTYPE html>
<?php
require_once("language.php");
$lines = 1000;
if (isset($_REQUEST["lines"]))
	$lines = $_REQUEST["lines"];

if (!is_numeric($lines))
{
	echo("Error in lines");
	return;
}

$hk = "checked";
if (!isset($_REQUEST["hk"]) && isset($_REQUEST["lines"]))
	$hk = "";

$update = "checked";
if (!isset($_REQUEST["update"]) && isset($_REQUEST["lines"]))
	$update = "";

$sleep = "checked";
if (!isset($_REQUEST["sleep"]) && isset($_REQUEST["lines"]))
	$sleep = "";

?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="apple-mobile-web-app-title" content="Teslalogger Config">
    <link rel="apple-touch-icon" href="img/apple-touch-icon.png">
    <title>Teslalogger Logfile</title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=4">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=17">
	<link rel="stylesheet" href="static/teslalogger_logfile.css?v=3">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
    <!-- Make sure you put this AFTER Leaflet's CSS -->
	<script src="static/leaflet/1.4.0/leaflet.js" integrity="sha512-QVftwZFqvtRNi0ZyCtsznlKSWOStnDORoefr1enyq5mVL4tmKB3S/EnC3rRJcxCPavG10IcrVGSmPh6Qw5lwrg==" crossorigin=""></script>
	<script>
	function logScrollToBottom() {
		var el = document.getElementById("log");
		if (el) el.scrollTop = el.scrollHeight;
	}
	window.addEventListener("load", logScrollToBottom);
	</script>
  </head>
  	<body>
  <?php
    include "menu.php";
    echo(menu("Logfile"));

	$carfilter = $_REQUEST["c"];
 ?>

 <div class="tl-page">
	<div class="tl-page-head">
		<h2 class="tl-page-title"><?php t("Logfile"); ?></h2>
		<a href="log.php" class="tl-download"><?php t("Download Logfile"); ?></a>
	</div>

	<form action="logfile.php" class="tl-card">
		<h2 class="tl-card-title"><?php t("Logfile"); ?></h2>
		<div class="tl-form">
			<div class="tl-form-item">
				<label for="f_lines"><?php t("Lines"); ?></label>
				<input type="number" id="f_lines" name="lines" value="<?= $lines ?>" min="10" max="25000">
			</div>
			<div class="tl-form-item">
				<label for="f_car">car:</label>
				<select id="f_car" name="c">
					<option value="">All</option>
<?php
					$allcars = file_get_contents(GetTeslaloggerURL("getallcars"),0, stream_context_create(["http"=>["timeout"=>2]]));
					$jcars = json_decode($allcars);

					foreach($jcars as $k => $v) {
                        $dn = $v->{"display_name"};
                        $carid = $v->{"id"};

						echo(str_repeat("\t",3));
						echo('<option value="'.$carid.'"');
						if ($carid == $carfilter)
							echo(" selected");

						echo('>'.$dn.'</option>');
						echo("\n");
                    }
					?>
				</select>
			</div>
			<div class="tl-form-item">
				<label for="f_hk"><?php t("Housekeeping"); ?></label>
				<input type="checkbox" id="f_hk" class="tl-toggle" name="hk" value="1" <?= $hk ?>>
			</div>
			<div class="tl-form-item">
				<label for="f_update"><?php t("Update"); ?></label>
				<input type="checkbox" id="f_update" class="tl-toggle" name="update" value="1" <?= $update ?>>
			</div>
			<div class="tl-form-item">
				<label for="f_sleep"><?php t("Sleep Attempt"); ?></label>
				<input type="checkbox" id="f_sleep" class="tl-toggle" name="sleep" value="1" <?= $sleep ?>>
			</div>
			<div class="tl-form-item tl-form-actions">
				<button type="submit"><?php t("OK"); ?></button>
			</div>
		</div>
	</form>

	<div class="tl-card tl-logcard">
		<div class="tl-card-head">
			<h2 class="tl-card-title"><?php t("Logfile"); ?></h2>
		</div>
		<div id="log" class="tl-log"><?PHP
	$url = GetTeslaloggerURL("taillogfile/$lines");
    $output = @file_get_contents($url);

	$output = str_replace("Start update","<b>Start update</b>", $output);
	$output = str_replace("End update","<b>End update</b>", $output);
	$output = str_replace("Rebooting","<b>Rebooting</b>", $output);
	$output = str_replace("Reboot request!","<b>Reboot request!</b>", $output);

	$output = str_replace("Error: Cloning into '/etc/teslalogger/git'...","...", $output);

	$output = str_replace("Rebooting","<b>Rebooting</b>", $output);

	$output = str_replace("Key","<b>Key</b>", $output);
	$output = str_replace("not translated!","<b>not translated!</b>", $output);

	$output = preg_replace("/(.*(Exception|Error|ERROR|No such host is known|= NULL|: NULL|vehicle unavailable|upstream internal error|NameResolutionFailure).*)/", "<font color='red'>$1</font>", $output);
	$output = preg_replace("/(state: .*)/", "<b>$1</b>", $output);
	$output = preg_replace("/(http[s]?:\/\/[\w\.\/\-]+)/", "<a href='$1'>$1</a>", $output);
	$output = preg_replace("/(.*\*\*\* Exit Loop !!!)/", "<font color='red'>$1</font>", $output);
	$output = preg_replace("/(Missing:)(.*)(Check:)/", "<font color='red'>$1$2$3</font>", $output);

	$output = preg_replace("/<font color='red'>(.*)(.*execute: \/usr\/bin\/du -sk \/etc\/teslalogger\/Exception.*)<\/font>/", "$1$2", $output);
	$output = preg_replace("/<font color='red'>(.*)(\/etc\/teslalogger\/Exception)<\/font>/", "$1$2", $output);

	$output = preg_replace("/<font color='red'>(.*)(deleted in Exception direcotry)<\/font>/", "$1$2", $output);

	if ($hk != "checked")
	{
		$output = preg_replace('/.*Exec_mono: \/bin\/df.*/', "", $output);
		$output = preg_replace('/.*execute: \/bin\/df.*/', "", $output);
		$output = preg_replace('/.*(\d+)\s+(\d+)\s+(\d+)\s+(\d+)%.*/', "", $output);
		$output = preg_replace('/.*Filesystem.*blocks.*Used.*Available.*/', "", $output);

		$output = preg_replace('/.*Exec_mono: \/usr\/bin\/du.*$(?:\r\n|\n).*$(?:\r\n|\n)/', "", $output);
		$output = preg_replace('/.*Housekeeping:.*/', "", $output);
		$output = preg_replace('/.*RunHousekeepingInBackground .*/', "", $output);
		$output = preg_replace('/.*Table:.*data:.*index:.*rows:.*/', "", $output);

		$output = preg_replace('/.*UpdateChargePrice.*/', "", $output);
		$output = preg_replace('/.*CopyChargePrice.*/', "", $output);
		$output = preg_replace('/.*UpdateChargeEnergyAdded.*/', "", $output);

		// $output = preg_replace('/.*TeslaLogger process statistics.*/', "", $output);
		$output = preg_replace('/.*WorkingSet64:\s+(\d+)+.*/', "", $output);
		$output = preg_replace('/.*PrivateMemorySize64:\s+(\d+)+.*/', "", $output);
		$output = preg_replace('/.*VirtualMemorySize64:\s+(\d+)+.*/', "", $output);
		$output = preg_replace('/.*HandleCount:\s+(\d+)+.*/', "", $output);
		$output = preg_replace('/^StartTime:\s+\d\d\/\d\d\/\d\d\d\d.*/', "", $output);

		$output = preg_replace('/^.*execute: \/usr\/bin\/du.*[\r\n].*(\d+)\s+\/etc\/.*[\r\n]/m', "", $output);
		$output = preg_replace('/^.*Exec_mono: \/usr\/bin\/du.*[\r\n].*(\d+)\s+\/etc\/.*[\r\n]/m', "", $output);
	}

	if ($update != "checked")
	{
		$output = preg_replace('/.*Update: \/etc\/teslalogger\/tmp\/.*/', "", $output);
		$output = preg_replace('/.*Copy \'\/etc\/teslalogger\/git\/.*/', "", $output);
		$output = preg_replace('/.*Copy \'\/etc\/teslalogger\/tmp\/.*/', "", $output);
		$output = preg_replace('/.*Exec_mono: rm -rf \/etc\/teslalogger\/tmp.*/', "", $output);
		$output = preg_replace('/.*Exec_mono: mkdir \/etc\/teslalogger\/tmp.*/', "", $output);
		$output = preg_replace('/.*chmod.*/', "", $output);
		$output = preg_replace('/.*GeofencePrivate:.*/', "", $output);
		$output = preg_replace('/.*service grafana-server restart.*/', "", $output);
		$output = preg_replace('/.*End Grafana update.*/', "", $output);
		$output = preg_replace('/.*execute: git.*/', "", $output);
		$output = preg_replace('/.*execute: rm -rf \/etc\/teslalogger\/git.*/', "", $output);
		$output = preg_replace('/.*execute: mkdir \/etc\/teslalogger\/git.*/', "", $output);
		$output = preg_replace('/.*execute: cert-sync.*/', "", $output);
		$output = preg_replace('/.*Mono Certificate Store Sync.*/', "", $output);
		$output = preg_replace('/.*Populate Mono certificate store:.*/', "", $output);
		$output = preg_replace('/.*Motus Technologies.*/', "", $output);
		$output = preg_replace('/.*Importing into legacy system store:.*/', "", $output);
		$output = preg_replace('/.*I already trust (\d+), your new list has (\d+).*/', "", $output);
		$output = preg_replace('/.*Import process completed.*/', "", $output);
		$output = preg_replace('/.*Importing into BTLS system store:*/', "", $output);
		$output = preg_replace('/.*downloading update package from*/', "", $output);
		$output = preg_replace('/.*update package downloaded to.*/', "", $output);
		$output = preg_replace('/.*unzip update package.*/', "", $output);
		$output = preg_replace('/.*move update files from.*/', "", $output);
		$output = preg_replace('/.*archive\/master.zip.*/', "", $output);
		$output = preg_replace('/.*\/etc\/teslalogger\/tmp\/.*/', "", $output);
		$output = preg_replace('/.*update package: download and unzip successful.*/', "", $output);
		$output = preg_replace('/.*CopyFilesRecursively: skip TeslaLogger.exe.*/', "", $output);
		$output = preg_replace('/.*UpdateDbInBackground finished.*/', "", $output);
	}

	if ($sleep != "checked")
	{
		$output = preg_replace('/.*Waiting for car to go to sleep.*/', "", $output);
	}


	if (!empty($carfilter))
	{
		// spezific car
		$output = preg_replace('/(.*)(\#'.$carfilter.'\[)(.*)/', "$1#xxxxx[$3", $output);
		$output = preg_replace('/.*(\#[0-9]+\[).*/', "", $output);
		$output = preg_replace('/(.*)(\#xxxxx\[)(.*)/', '$1#'.$carfilter.'[$3', $output);
	}

	$output = preg_replace("/[\r\n]{2,}/", "\n", $output); // unnecessary new lines
	// white-space: pre-wrap renders real line breaks, so nl2br() would double
	// every line: convert them to <br> without keeping the line break
	echo str_replace("\r\n", "<br>", str_replace("\n", "<br>", $output));
?>		</div>
	</div>
 </div>
</body>
</html>
