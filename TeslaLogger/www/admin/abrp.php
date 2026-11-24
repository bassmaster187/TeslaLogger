<!DOCTYPE html>
<?php
require_once("language.php");
require_once("tools.php");
session_start();
global $display_name;
$carid = GetDefaultCarId();
if (isset($_REQUEST["carid"]))
{
	$_SESSION["carid"] = intval($_REQUEST["carid"]);
	$carid = intval($_REQUEST["carid"]);
}
else
{
	$_SESSION["carid"] = $carid;
}
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<meta name="apple-mobile-web-app-title" content="Teslalogger Config">
	<link rel="apple-touch-icon" href="img/apple-touch-icon.png">
	<title><?php t("ABRPTitle"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=28">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
</head>
<body>
	<?php
    include "menu.php";
    echo(menu("Abetterrouteplanner"));

    $current_carid = $_SESSION["carid"];
    if (!isset($current_carid))
        $current_carid = 1;

    $abrpinfo = file_get_contents(GetTeslaloggerURL("abrp/$current_carid/info"),0, stream_context_create(["http"=>["timeout"=>2]]));
    $jabrp = json_decode($abrpinfo);
    $token = $jabrp->{"token"};
    $mode = $jabrp->{"mode"};

?>

<script>
    function Save()
    {
        var mode = $("#mode").is(':checked') ? 1 : 0;

        var j = {
            "abrp_token" : $("#token").val(),
            "abrp_mode" : mode
        };

        var jsonstring = JSON.stringify(j);

        var url = "abrp/<?= $current_carid ?>/set";
        console.log("url: " + url);
        var jqxhr = $.post("teslaloggerstream.php", {url: url, data: jsonstring}).always(function (r) {
            console.log("Response of:"+ url + ":" + r);

            alert(r);

            if (r === "OK")
                location.href = document.referrer;
        });
    }

    $( function() {

    });
</script>

<div class="tl-page">
	<div class="tl-page-head">
		<h2 class="tl-page-title"><?php t("ABRPTitle"); ?></h2>
	</div>

	<div class="tl-card">
		<p class="tl-muted" style="margin-top:0;"><?php t("TextABRP"); ?></p>
	</div>

	<div class="tl-card">
		<h2 class="tl-card-title"><?php t("Setup"); ?></h2>
		<ol class="tl-suc-steps">
			<li><?php t("ABRP1"); ?></li>
			<li><?php t("ABRP2"); ?></li>
			<li><?php t("ABRP3"); ?></li>
			<li><?php t("ABRP4"); ?></li>
			<li><?php t("ABRP5"); ?></li>
			<li><?php t("ABRP6"); ?></li>
			<li><?php t("ABRP7"); ?></li>
			<li><?php t("ABRP8"); ?></li>
			<li><?php t("ABRP9"); ?></li>
		</ol>
		<p class="tl-muted" style="margin-bottom:0;"><?php t("ABRPYT"); ?>: <a href="https://youtu.be/00s7Y8Iv2iw">YouTube</a></p>
	</div>

	<div class="tl-card tl-small-form-card">
		<h2 class="tl-card-title"><?php t("Settings"); ?></h2>
		<div class="tl-form tl-small-form">
			<div class="tl-form-item">
				<label for="token"><?php t("ABRPToken"); ?></label>
				<input class="tl-small-input" id="token" value="<?= $token ?>"/>
			</div>
			<div class="tl-form-item">
				<label for="mode"><?php t("Enabled"); ?></label>
				<label class="tl-switch"><input type="checkbox" id="mode" <?PHP if ($mode === 1) echo("checked");  ?>><span class="tl-switch-track"></span></label>
			</div>
			<div class="tl-form-actions">
				<button onClick="javascript:Save();"><?php t("Save"); ?></button>
			</div>
		</div>
	</div>
</div>
</body>
</html>