<!DOCTYPE html>
<?php
require_once("language.php");
require_once("tools.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?php t("Teslalogger Tesla Zugangsdaten"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />

	<script>
	<?php
	if (isset($_REQUEST["id"]))
        echo("var dbid=".$_REQUEST["id"].";\n");
    else
        echo("var dbid=-1;\n");
	?>
	$(document).ready(function(){
        setTimeout(updateInfo, 1000);

        $("#MFA").on("keydown keyup change", function(){
            var value = $(this).val();
            if (value.length == 6)
            {
                $("#MFA" ).prop( "disabled", true );
                console.log("MFA entered");
                var url = "mfa/"+dbid+"/"+value;
                console.log("url: " + url);
                var jqxhr = $.post("teslaloggerstream.php", {url: url, data: ''}).always(function () {
                    setTimeout(updateInfo, 500);
				});
            }
        });

	});

    function updateInfo()
    {
        var d = {
					id: dbid	
				};

        var jqxhr = $.post("teslaloggerstream.php", {url: "passwortinfo", data: JSON.stringify(d)})
        .done(function(data) {
            $("#info").html(data);
            if (data.includes("Wait for MFA code"))
            {
                $("#mfapanel").show();           
                $("#captchapanel").hide();           
            }
            
            setTimeout(updateInfo, 1000);
        })
        .fail(function(data) {
            $("#info").html("Error");
        });
		
    }

    function btn_ok_click()
    {
        var info = $("#info").text();
        if (info.includes("Everything is OK"))
            window.location.href='index.php';
        else
            window.location.href='password.php';
    }

    function btn_captcha_click()
    {
        $("#captcha" ).prop( "disabled", true );
        var value = $("#captcha").val();
        console.log("captcha entered");
        var url = "captcha/"+dbid+"/"+value;
        console.log("url: " + url);
        var jqxhr = $.post("teslaloggerstream.php", {url: url, data: ''}).always(function () {
            
        });
    }

</script>
</head>
<body>
<div style="max-width: 1260px;">
<?php 
include "menu.php";
menu("Credentials");
?>
<div>
<h1><?php t("Credentials Info"); ?>:</h1>
<div id="captchapanel">
    <h2>Captcha:<h2> <br>
    <img src="teslaloggerstream_svg.php?url=captchapic/<?php
	if (isset($_REQUEST["id"]))
        echo($_REQUEST["id"]);
    else
        echo("-1");
?>" alt=""/>
    <input id="captcha" type="text" /> <button onclick="btn_captcha_click();"><?php t("OK"); ?>
</div>
<div id="mfapanel" style="display: none;">
    <h2>MFA:</h2> 
    <input id="MFA" type="text" autocomplete="new-password" />
</div>
</div>
<h1>Info:</h1>
<div id="info" style="overflow: auto; height: 400px; max-width: 1260px;">
</div>
<button onclick="btn_ok_click();" style="float: right;"><?php t("OK"); ?>
</body>
</html>

