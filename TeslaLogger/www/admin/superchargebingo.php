<!DOCTYPE html>
<?php
require_once("language.php");
require_once("tools.php");
session_start();
global $display_name;
$carid = GetDefaultCarId();
if (isset($_REQUEST["carid"]))
{
	$_SESSION["carid"] = $_REQUEST["carid"];
	$carid = $_REQUEST["carid"];
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
    <title><?php t("SuperChargeBingo"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=23">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
</head>
  	<body>
	
	<?php 
    include "menu.php";
    echo(menu("SuperChargeBingo"));

    $current_carid = $_SESSION["carid"];
    if (!isset($current_carid))
        $current_carid = 1;

    $sucbingoinfo = file_get_contents(GetTeslaloggerURL("sucbingo/$current_carid/info"),0, stream_context_create(["http"=>["timeout"=>2]]));
    $jsucbingo = json_decode($sucbingoinfo);
    $sucBingo_user = $jsucbingo->{"sucBingo_user"};
    $sucBingo_apiKey = $jsucbingo->{"sucBingo_apiKey"};

    echo("<!-- Response of sucbingo/$current_carid/info:\n"); 
    var_dump($sucbingoinfo);
    echo ("-->\n");
?>

<script>
    function Save()
    {
        var j = {
            "sucBingo_user" : $("#user").val(),
            "sucBingo_apiKey" : $("#apikey").val()
        };

        var jsonstring = JSON.stringify(j);

        var url = "sucbingo/<?= $current_carid ?>/set";
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
		<h2 class="tl-page-title"><?php t("SuperChargeBingo"); ?></h2>
		<?php
		$helpUrl = ($language == "de") ? "https://teslalogger.de/docs/extras/superchagebingo" : "https://teslalogger.de/en/docs/extras/superchagebingo";
		?>
		<a href="<?= $helpUrl ?>" target="_blank"><img src="img/icon-help-24.png" class="tl-help" /></a>
		<div class="tl-actions">
			<button onClick="javascript:Save();"><?php t("Save"); ?></button>
		</div>
	</div>

	<div class="tl-card">
		<p class="tl-muted" style="margin-top:0;"><?php t("SuperChargeBingoComment"); ?></p>
	</div>

	<div class="tl-card tl-suc-card">
		<div class="tl-card-head">
			<h2 class="tl-card-title"><?php t("Setup"); ?></h2>
		</div>
		<ol class="tl-suc-steps">
			<li><a href="https://supercharge.bingo/#/register?pk_campaign=integration&pk_kwd=teslalogger" target="_blank"><?php t("Create a SuperChargeBingo account"); ?></a></li>
			<li><?php t("Create API key (must be secure)"); ?></li>
			<li><?php t("Copy Username and API key and insert it in TeslaLogger"); ?></li>
			<li><?php t("Save and restart TeslaLogger"); ?></li>
		</ol>
	</div>

	<div class="tl-card">
		<h2 class="tl-card-title"><?php t("Settings"); ?></h2>
		<div class="tl-form tl-small-form">
			<div class="tl-form-item">
				<label for="user"><?php t("Username"); ?></label>
				<input class="tl-small-input" id="user" value="<?= $sucBingo_user ?>"/>
			</div>
			<div class="tl-form-item">
				<label for="apikey"><?php t("API key"); ?></label>
				<input class="tl-small-input" id="apikey" value="<?= $sucBingo_apiKey ?>"/>
			</div>
		</div>
	</div>
</div>
</body>
</html>
