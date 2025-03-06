<!DOCTYPE html>
<?php
require_once("language.php");
require_once("tools.php");
session_start();
global $display_name;
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="apple-mobile-web-app-title" content="Teslalogger Config">
    <link rel="apple-touch-icon" href="img/apple-touch-icon.png">
    <title><?php t("KomootSettings"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
</head>
  	<body>
	
	<?php 
    include "menu.php";
    echo(menu("Komoot"));

    $komootinfo = file_get_contents(GetTeslaloggerURL("komoot/info"),0, stream_context_create(["http"=>["timeout"=>2]]));
    $jkomoot = json_decode($komootinfo);
    $komoot_carid = $jkomoot->{"komoot_carid"};
    $komoot_user = $jkomoot->{"komoot_user"};
    $komoot_passwd = $jkomoot->{"komoot_passwd"};
    echo("<!-- Response of komoot/info:\n"); 
    var_dump($komootinfo);
    echo ("-->\n");
?>

<script>
    function Save()
    {
        var j = {
            "komoot_carid" : $("#carid").val(),
            "mqtt_user" : $("#user").val(),
            "mqtt_passwd" : $("#passwd").val(),
        };

        var jsonstring = JSON.stringify(j);

        var url = "komoot/set";
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
<h1><?php t("KomootSettings"); ?></h1>
<?php t("KomootSettingsInfo"); ?>
</div>
<table>
<tr>
    <td><?php t("Username"); ?>: </td><td><input id="user" size="40" value="<?= $komoot_user ?>"/></td>
</tr>
<tr>
    <td>
        <?php t("Password"); ?>: </td><td><input id="passwd" type="password" size="40" value="<?= $komoot_passwd ?>"/>
        <input type="hidden" id="carid" value="<?= $komoot_carid ?>">
    </td>
</tr>
<tr>    
    <td></td><td><button onClick="javascript:Save();"><?php t("Save"); ?></button></td>
</tr>
