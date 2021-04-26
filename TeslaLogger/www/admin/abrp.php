<!DOCTYPE html>
<?php
require_once("language.php");
session_start();
global $display_name;
$carid = 1;
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
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="https://teslalogger.de/teslalogger_style.css">
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<script src="https://code.jquery.com/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="https://unpkg.com/leaflet@1.4.0/dist/leaflet.css" integrity="sha512-puBpdR0798OZvTTbP4A8Ix/l+A4dHDD0DGqYW6RQ+9jxkRFclaxxQb/SJAWZfWAkuyeQUytO7+7N4QKrDh+drA==" crossorigin=""/>
	<link rel='stylesheet' id='genericons-css'  href='https://www.impala64.de/blog/tesla/wp-content/themes/twentyfourteen/genericons/genericons.css?ver=3.0.3' type='text/css' media='all' />
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
<h1>Abetterrouteplanner Link</h1>
You can setup a link from Teslalogger to Abetterrouteplanner to avoid giving your Tesla credentials to a 3rd Party. Another benefit is to minimize the possibility to prevent the car from going to sleep if more than one service is using your credentials. 
</div>
<h1>Setup</h1>
<ul>
    <li>Create an Abetterrouteplanner account</li>
    <li>Go to Settings</li>
    <li>Choose your car</li>
    <li>Save your car to your account</li>
    <li>Give your car a name</li>
    <li>Click on "Link Generic" to get a ABRP token</li>
    <li>Copy the token and click "Done"</li>
    <li>Insert the token in Teslalogger</li>
    <li>Check if the Teslalogger is connected</li>
</ul>
Check YouTube tutorial for live demo: <a href="https://youtu.be/00s7Y8Iv2iw">YouTube</a>
<h1>Settings</h1>
<table>
<tr>
    <td>Abetterrouteplanner Token:</td><td><input id="token" size="40" value="<?= $token ?>"/></td>
</tr>
<tr>
    <td>Enabled:</td><td><input id="mode" type="checkbox" <?PHP if ($mode === 1) echo("checked");  ?>/></td>
</tr>
<tr>    
    <td></td><td><button onClick="javascript:Save();">Save</button></td>
</tr>