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

<div id="content" style="max-width:1036px;">
<h1><?php t("MQTTSettings"); ?></h1>
<?php t("MQTTSettingsInfo"); ?>
<h2><?php t("TopicStructure"); ?></h2>
    <li><?php t("MQTT1"); ?></li>
    <li><?php t("MQTT2"); ?></li>
    <li><?php t("MQTT3"); ?></li>
    <li><?php t("MQTT4"); ?></li>
    <li><?php t("MQTT5"); ?></li>

<h3><?php t("ADDescription"); ?></h3>
    <li><?php t("MQTTAD1"); ?></li>

<h2><?php t("ControlTopic"); ?></h2>
    <li><?php t("MQTT6"); ?></li>
    <li><a href="https://github.com/bassmaster187/TeslaLogger/blob/master/TeslaLogger/WebServer.md#send-commands-to-car"><?php t("MQTT7"); ?></a></li>

<h5><?php t("RebootAfterSave"); ?><h5>
</div>
<table>
<tr>
    <td><?php t("Host"); ?>: </td><td><input id="host" size="40" value="<?= $mqtt_host ?>"/></td>
</tr>
<tr>
    <td><?php t("Port"); ?>: </td><td><input id="port" size="40" value="<?= $mqtt_port ?>"/></td>
</tr>
<tr>
    <td><?php t("Username"); ?>: </td><td><input id="user" size="40" value="<?= $mqtt_user ?>"/></td>
</tr>
<tr>
    <td><?php t("Password"); ?>: </td><td><input id="passwd" type="password" size="40" value="<?= $mqtt_passwd ?>"/></td>
</tr>
<tr>
    <td><?php t("Topic"); ?>: </td><td><input id="topic" size="40" value="<?= $mqtt_topic ?>"/></td>
</tr>
<tr>
    <td><?php t("PublishJson"); ?>: </td><td><input id="publishjson" type="checkbox" <?PHP if ($mqtt_publishjson === 1) echo("checked");  ?>/></td>
</tr>
<tr>
    <td><?php t("Singletopics"); ?>: </td><td><input id="singletopics" type="checkbox" <?PHP if ($mqtt_singletopics === 1) echo("checked");  ?>/></td>
</tr>
<tr>
    <td><?php t("AutoDiscovery"); ?>: </td><td><input id="discoveryenable" type="checkbox" <?PHP if ($mqtt_discoveryenable === 1) echo("checked");  ?>/></td>
</tr>
<tr>
    <td><?php t("AutoDiscoveryTopic"); ?>: </td><td><input id="discovertopic" size="100" value="<?= $mqtt_discoverytopic ?>"/></td>
</tr>
<tr>
    <td><?php t("ClientID"); ?>: </td><td><input id="clientid" size="100" value="<?= $mqtt_clientid ?>"/></td>
</tr>
<tr>    
    <td></td><td><button onClick="javascript:Save();"><?php t("Save"); ?></button></td>
</tr>
