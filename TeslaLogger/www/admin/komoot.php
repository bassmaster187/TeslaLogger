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
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=23">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
</head>
<body>
	
	<?php 
    include "menu.php";
    echo(menu("Komoot"));
    $komoot_carid = -1;
    $komoot_user = "Kommot_User";
    $komoot_passwd = "secretPassword";
    $komoot_displayname = "MyBike";
    $komootinfo = file_get_contents(GetTeslaloggerURL("komoot/listSettings"),0, stream_context_create(["http"=>["timeout"=>2]]));
    // echo("<!-- komootinfo:\n"); 
    // var_dump($komootinfo);
    // echo ("-->\n");
    $jkomoot = json_decode($komootinfo); 
    // echo("<!-- jkomoot:\n"); 
    // var_dump($jkomoot);
    // echo ("-->\n");
    foreach($jkomoot as $item) { 
        // echo("<!-- item:\n"); 
        // var_dump($item);
        // echo ("-->\n");
        $komoot_carid = $item->carid;
        // echo("<!-- komoot_carid:\n"); 
        // var_dump($komoot_carid);
        // echo ("-->\n");
        $komoot_user = str_replace("KOMOOT:", "", $item->user);
        // echo("<!-- komoot_user:\n"); 
        // var_dump($komoot_user);
        // echo ("-->\n");
        $komoot_passwd = $item->passwd;
        // echo("<!-- komoot_passwd:\n"); 
        // var_dump($komoot_passwd);
        // echo ("-->\n");
        $komoot_displayname = $item->displayname;
        // echo("<!-- komoot_displayname:\n"); 
        // var_dump($komoot_displayname);
        // echo ("-->\n");
        break;
        // TODO enable for multiple accounts
    }
    // echo("<!-- Response of komoot/listSettings:\n"); 
    // var_dump($komootinfo);
    // echo ("-->\n");
    ?>

<script>
    function Save()
    {
        // TODO enable for multiple accounts
        var j = [ {
            "komoot_carid" : $("#carid").val(),
            "komoot_user" : $("#user").val(),
            "komoot_passwd" : $("#passwd").val(),
            "komoot_displayname" : $("#bikename").val()
        }];

        var jsonstring = JSON.stringify(j);

        var url = "komoot/saveSettings";
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
		<h2 class="tl-page-title"><?php t("KomootSettings"); ?></h2>
		<div class="tl-actions">
			<button onClick="javascript:Save();"><?php t("Save"); ?></button>
		</div>
	</div>

	<div class="tl-card">
		<p class="tl-muted" style="margin-top:0;"><?php t("KomootSettingsInfo"); ?></p>
	</div>

	<div class="tl-card tl-small-form-card">
		<div class="tl-form tl-small-form">
			<div class="tl-form-item">
				<label for="user"><?php t("Username"); ?></label>
				<input class="tl-small-input" id="user" value="<?= $komoot_user ?>"/>
			</div>
			<div class="tl-form-item">
				<label for="passwd"><?php t("Password"); ?></label>
				<input class="tl-small-input" id="passwd" type="password" value="<?= $komoot_passwd ?>"/>
			</div>
			<div class="tl-form-item">
				<label for="bikename"><?php t("DisplayName"); ?></label>
				<input class="tl-small-input" id="bikename" value="<?= $komoot_displayname ?>"/>
			</div>
			<input type="hidden" id="carid" value="<?= $komoot_carid ?>">
		</div>
	</div>
</div>
</body>
</html>