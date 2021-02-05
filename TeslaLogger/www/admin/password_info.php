<!DOCTYPE html>
<?php
require("language.php");
require_once("tools.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?php t("Teslalogger Tesla Zugangsdaten"); ?></title>
	<link rel="stylesheet" href="https://code.jquery.com/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="https://teslalogger.de/teslalogger_style.css">
	<script src="https://code.jquery.com/jquery-1.12.4.js"></script>
	<script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
	<script src="https://code.jquery.com/jquery-migrate-1.4.1.min.js"></script>
	<script src="https://cdn.datatables.net/1.10.22/js/jquery.dataTables.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='https://www.impala64.de/blog/tesla/wp-content/themes/twentyfourteen/genericons/genericons.css?ver=3.0.3' type='text/css' media='all' />
	<link rel='stylesheet' href="https://cdn.datatables.net/1.10.22/css/jquery.dataTables.min.css">

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
                $("#mfapanel").show();
            
            
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

</script>
</head>
<body style="padding-top: 5px; padding-left: 10px;">
<div style="max-width: 1260px;">
<?php 
include "menu.php";
menu("Credentials");
?>
<div>
<h1><?php t("Credentials Info"); ?>:</h1>
</div>
<div id="info" style="overflow: auto; height: 400px; max-width: 1260px;">
</div>
<div id="mfapanel" style="display: none;">
    MFA: <input id="MFA" type="text" autocomplete="new-password" />
</div>
<button onclick="btn_ok_click();" style="float: right;"><?php t("OK"); ?>
</body>
</html>

