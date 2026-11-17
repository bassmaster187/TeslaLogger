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
    <title><?php t("MQTT Settings"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=14">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="static/leaflet/1.4.0/leaflet.css" />
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
</head>
  	<body>
	
	<?php 
    include "menu.php";
    echo(menu("MQTT"));

    $mqttinfo = file_get_contents(GetTeslaloggerURL("mqtt/info"),0, stream_context_create(["http"=>["timeout"=>2]]));
    $jmqtt = json_decode($mqttinfo);
    $mqtt_host = $jmqtt->{"mqtt_host"};
    $mqtt_port = ($jmqtt->{"mqtt_port"} === null OR $jmqtt->{"mqtt_port"} === "") ? "1883" : $jmqtt->{"mqtt_port"};
    $mqtt_user = $jmqtt->{"mqtt_user"};
    $mqtt_passwd = $jmqtt->{"mqtt_passwd"};
    $mqtt_topic = ($jmqtt->{"mqtt_topic"} === null OR $jmqtt->{"mqtt_topic"} === "") ? "teslalogger" : $jmqtt->{"mqtt_topic"};
    $mqtt_publishjson = $jmqtt->{"mqtt_publishjson"};
    $mqtt_singletopics = $jmqtt->{"mqtt_singletopics"};
    $mqtt_discoveryenable = $jmqtt->{"mqtt_discoveryenable"};
    $mqtt_discoverytopic = ($jmqtt->{"mqtt_discoverytopic"} === null OR $jmqtt->{"mqtt_discoverytopic"} === "") ? "homeassistant" : $jmqtt->{"mqtt_discoverytopic"};
    $mqtt_clientid = $jmqtt->{"mqtt_clientid"};
    echo("<!-- Response of mqtt/info:\n"); 
    var_dump($mqttinfo);
    echo ("-->\n");
?>

<script>
    function Save()
    {
        var publishjson = $("#publishjson").is(':checked') ? 1 : 0;
        var singletopics = $("#singletopics").is(':checked') ? 1 : 0;
        var discoveryenable = $("#discoveryenable").is(':checked') ? 1 : 0;
        var j = {
            "mqtt_host" : $("#host").val(),
            "mqtt_port" : $("#port").val(),
            "mqtt_user" : $("#user").val(),
            "mqtt_passwd" : $("#passwd").val(),
            "mqtt_topic" : $("#topic").val(),
            "mqtt_publishjson" : publishjson,
            "mqtt_singletopics" : singletopics,
            "mqtt_discoveryenable" : discoveryenable,
            "mqtt_discoverytopic" : $("#discovertopic").val(),
            "mqtt_clientid" : $("#clientid").val()
        };

        var jsonstring = JSON.stringify(j);

        var url = "mqtt/set";
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
		<h2 class="tl-page-title"><?php t("MQTTSettings"); ?></h2>
		<?php
		$helpUrl = ($language == "de") ? "https://teslalogger.de/docs/extras/mqtt/" : "https://teslalogger.de/en/docs/extras/mqtt/";
		?>
		<a href="<?= $helpUrl ?>" target="_blank"><img src="img/icon-help-24.png" class="tl-help" /></a>
		<div class="tl-actions">
			<button onClick="javascript:Save();"><?php t("Save"); ?></button>
		</div>
	</div>

	<div class="tl-card tl-mqtt-info-card">
		<p class="tl-mqtt-info tl-muted"><?php t("MQTTSettingsInfo"); ?></p>
		<h2 class="tl-mqtt-section"><?php t("TopicStructure"); ?></h2>
		<ul class="tl-mqtt-list">
			<li><?php t("MQTT1"); ?></li>
			<li><?php t("MQTT2"); ?></li>
			<li><?php t("MQTT3"); ?></li>
			<li><?php t("MQTT4"); ?></li>
			<li><?php t("MQTT5"); ?></li>
		</ul>
		<h2 class="tl-mqtt-section"><?php t("ADDescription"); ?></h2>
		<ul class="tl-mqtt-list">
			<li><?php t("MQTTAD1"); ?></li>
		</ul>
		<h2 class="tl-mqtt-section"><?php t("ControlTopic"); ?></h2>
		<ul class="tl-mqtt-list">
			<li><?php t("MQTT6"); ?></li>
			<li><a href="https://github.com/bassmaster187/TeslaLogger/blob/master/TeslaLogger/WebServer.md#send-commands-to-car"><?php t("MQTT7"); ?></a></li>
		</ul>
		<p class="tl-mqtt-note"><?php t("RebootAfterSave"); ?></p>
	</div>

	<div class="tl-card tl-mqtt-form-card">
		<div class="tl-form tl-mqtt-form">
			<div class="tl-form-item">
				<label for="host"><?php t("Host"); ?></label>
				<input class="tl-mqtt-input" id="host" value="<?= $mqtt_host ?>"/>
			</div>
			<div class="tl-form-item">
				<label for="port"><?php t("Port"); ?></label>
				<input class="tl-mqtt-input" id="port" value="<?= $mqtt_port ?>"/>
			</div>
			<div class="tl-form-item">
				<label for="user"><?php t("Username"); ?></label>
				<input class="tl-mqtt-input" id="user" value="<?= $mqtt_user ?>"/>
			</div>
			<div class="tl-form-item">
				<label for="passwd"><?php t("Password"); ?></label>
				<input class="tl-mqtt-input" id="passwd" type="password" value="<?= $mqtt_passwd ?>"/>
			</div>
			<div class="tl-form-item">
				<label for="topic"><?php t("Topic"); ?></label>
				<input class="tl-mqtt-input" id="topic" value="<?= $mqtt_topic ?>"/>
			</div>
			<div class="tl-form-item">
				<label for="discovertopic"><?php t("AutoDiscoveryTopic"); ?></label>
				<input class="tl-mqtt-input" id="discovertopic" value="<?= $mqtt_discoverytopic ?>"/>
			</div>
			<div class="tl-form-item">
				<label for="clientid"><?php t("ClientID"); ?></label>
				<input class="tl-mqtt-input" id="clientid" value="<?= $mqtt_clientid ?>"/>
			</div>
			<div class="tl-form-item">
				<label for="publishjson"><?php t("PublishJson"); ?></label>
				<label class="tl-switch">
					<input id="publishjson" type="checkbox" value="1" <?PHP if ($mqtt_publishjson === 1) echo("checked");  ?>/>
					<span class="tl-switch-track"></span>
				</label>
			</div>
			<div class="tl-form-item">
				<label for="singletopics"><?php t("Singletopics"); ?></label>
				<label class="tl-switch">
					<input id="singletopics" type="checkbox" value="1" <?PHP if ($mqtt_singletopics === 1) echo("checked");  ?>/>
					<span class="tl-switch-track"></span>
				</label>
			</div>
			<div class="tl-form-item">
				<label for="discoveryenable"><?php t("AutoDiscovery"); ?></label>
				<label class="tl-switch">
					<input id="discoveryenable" type="checkbox" value="1" <?PHP if ($mqtt_discoveryenable === 1) echo("checked");  ?>/>
					<span class="tl-switch-track"></span>
				</label>
			</div>
		</div>
	</div>
</div>
</body>
</html>
