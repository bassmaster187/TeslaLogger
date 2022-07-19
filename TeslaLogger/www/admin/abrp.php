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
    <title>Teslalogger Abetterrouteplanner</title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
</head>
  	<body style="padding-top: 5px; padding-left: 10px;">
	
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

    echo("<!-- Response of abrp/$current_carid/info:\n"); 
    var_dump($abrpinfo);
    echo ("-->\n");
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

<div id="content" style="max-width:1036px;">
<h1><?php t("ABRPTitle"); ?></h1>
<?php t("TextABRP"); ?>
</div>
<h1><?php t("Setup"); ?></h1>
<ul>
	<li><?php t("ABRP1"); ?></li>
	<li><?php t("ABRP2"); ?></li>
	<li><?php t("ABRP3"); ?></li>
	<li><?php t("ABRP4"); ?></li>
	<li><?php t("ABRP5"); ?></li>
	<li><?php t("ABRP6"); ?></li>
	<li><?php t("ABRP7"); ?></li>
	<li><?php t("ABRP8"); ?></li>
	<li><?php t("ABRP9"); ?></li>
</ul>
<?php t("ABRPYT"); ?>: <a href="https://youtu.be/00s7Y8Iv2iw">YouTube</a>
<h1><?php t("Settings"); ?></h1>
<table>
<tr>
    <td><?php t("ABRPToken"); ?>:</td><td><input id="token" size="40" value="<?= $token ?>"/></td>
</tr>
<tr>
    <td><?php t("Enabled"); ?>:</td><td><input id="mode" type="checkbox" <?PHP if ($mode === 1) echo("checked");  ?>/></td>
</tr>
<tr>    
    <td></td><td><button onClick="javascript:Save();"><?php t("Save"); ?></button></td>
</tr>