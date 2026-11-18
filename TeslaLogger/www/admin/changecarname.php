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
	<title><?php t("CarNameTitle"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=17">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
</head>
<body>
	<?php
    include "menu.php";
    menu("Credentials");

    $current_carid = $_SESSION["carid"];
    if (!isset($current_carid))
        $current_carid = 1;

    $carinfo = file_get_contents(GetTeslaloggerURL("carname/$current_carid/info"),0, stream_context_create(["http"=>["timeout"=>2]]));
    $jcarinfo = json_decode($carinfo);
    $car_name = $jcarinfo->{"car_name"};

    echo("<!-- Response of carname/$current_carid/info:\n");
    var_dump($abrpinfo);
    echo ("-->\n");
?>

<script>
    function Save()
    {
        var j = {
            "car_name" : $("#car_name").val()
        };

        var jsonstring = JSON.stringify(j);

        var url = "carname/<?= $current_carid ?>/set";
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
		<h2 class="tl-page-title"><?php t("CarNameTitle"); ?></h2>
	</div>

	<div class="tl-card">
		<p class="tl-muted" style="margin-top:0;"><?php t("TextCarName"); ?></p>
	</div>

	<div class="tl-card tl-small-form-card">
		<h2 class="tl-card-title"><?php t("RebootAfterSave"); ?></h2>
		<div class="tl-form tl-small-form">
			<div class="tl-form-item">
				<label for="car_name"><?php t("CarName"); ?></label>
				<input class="tl-small-input" id="car_name" value="<?= $car_name ?>"/>
			</div>
			<div class="tl-form-actions">
				<button onClick="javascript:Save();"><?php t("Save"); ?></button>
			</div>
		</div>
	</div>
</div>
</body>
</html>