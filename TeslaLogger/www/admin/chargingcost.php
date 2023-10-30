<!DOCTYPE html>
<?php
require_once("language.php");
require_once("tools.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?php t("Charging Costs"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.css">
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
	<script src="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.js"></script>
<style>
.sum {padding-left: 20px; text-align: right;}
input {width: 100px; padding-right: 10px;}
input[type=number] {text-align: right;}
</style>

	<script>
    var minutes;
    var kwh;
    var errortext;
<?php

    $url = GetTeslaloggerURL("getchargingstate?id=". $_REQUEST["id"]);

    $output = @file_get_contents($url);
    if ($output === false)
    {
        $error = error_get_last();
        $error = explode(': ', $error['message']);
        $error = trim($error[2]);
        echo("errortext = 'Error: $error - URL: $url'");
    }
    else
    {
        $output = str_replace( "'", "\'", $output);
        echo("    var json = JSON.parse('$output');\n");
    }
?>

	var loc;
	$( function() {
        if (navigator.languages != undefined)
            loc = navigator.languages[0];
		else
            loc = navigator.language;

        if (errortext != undefined)
        {
            $("#errortext").text(errortext);
        }
        else
        {
            $("#address").text(json[0]["address"]);
        }

        kwh = Number(json[0]["kWh"]);
        kwh_disp = Number(json[0]["kWh"]).toLocaleString(loc,{maximumFractionDigits:2, minimumFractionDigits: 2});
        $("#charge_energy_added").text(kwh_disp + " <?php t("kWh"); ?>");

        var StartDate = new Date(json[0]["StartDate"]);
        var EndDate = new Date(json[0]["EndDate"]);

        minutes = diff_minutes(StartDate, EndDate);
        $("#minutes").text(minutes + " <?php t("Minutes"); ?>");
        $("#StartDate").text(StartDate.toLocaleTimeString(loc,{ day: '2-digit', month: '2-digit', year: 'numeric' }));
        $("#cost_currency").val(json[0]["cost_currency"]);
        $("#cost_per_kwh").val(json[0]["cost_per_kwh"]);
        $("#cost_per_session").val(json[0]["cost_per_session"]);
        $("#cost_per_minute").val(json[0]["cost_per_minute"]);
        $("#cost_idle_fee_total").val(json[0]["cost_idle_fee_total"]);
        $("#cost_kwh_meter_invoice").val(Round(json[0]["cost_kwh_meter_invoice"],2));

		updatecalculation();

        $("input").change(function(){updatecalculation();});
	});

    function diff_minutes(dt2, dt1)
    {
        var diff =(dt2.getTime() - dt1.getTime()) / 1000;
        diff /= 60;
        return Math.abs(Math.round(diff));
    }

    function parseLocalNum(num) {
        return +(num.replace(",", "."));
    }

    function Round(value, digits)
    {
        if (value === null)
            return value;

        var factor = Math.pow(10,digits);
        return Math.round(value * factor)/factor;
    }

    function updatecalculation()
    {
        $("#minutes_charged").text(minutes);
        var kwh_calc = kwh;
        var cost_kwh_meter_invoice = $("#cost_kwh_meter_invoice").val();
        if (cost_kwh_meter_invoice !== "")
        {
            cost_kwh_meter_invoice = parseLocalNum($("#cost_kwh_meter_invoice").val());
            cost_kwh_meter_invoice = Round(cost_kwh_meter_invoice, 2);
            var efficiency = kwh_calc / cost_kwh_meter_invoice * 100;
            $("#charge_efficiency").text(efficiency.toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) + " %");
            kwh_calc = cost_kwh_meter_invoice;
        }
        else
            $("#charge_efficiency").text("");

        $("#kwh_charged").text(kwh_calc.toLocaleString(loc,{maximumFractionDigits:2, minimumFractionDigits: 2}));
        var cost_per_kwh_sum = kwh_calc * parseLocalNum($("#cost_per_kwh").val());
        $("#cost_per_kwh_sum").text(cost_per_kwh_sum.toLocaleString(loc,{maximumFractionDigits:2, minimumFractionDigits: 2}));

        var cost_per_session_sum = parseLocalNum($("#cost_per_session").val());
        if (cost_per_session_sum === "")
            cost_per_session_sum = 0;

        $("#cost_per_session_sum").text(Number(cost_per_session_sum).toLocaleString(loc,{maximumFractionDigits:2, minimumFractionDigits: 2}));

        var cost_idle_fee_total_sum = parseLocalNum($("#cost_idle_fee_total").val());
        if (cost_idle_fee_total_sum === "")
            cost_idle_fee_total_sum = 0;

        $("#cost_idle_fee_total_sum").text(Number(cost_idle_fee_total_sum).toLocaleString(loc,{maximumFractionDigits:2, minimumFractionDigits: 2}));


        var cost_per_minute_sum = minutes * parseLocalNum($("#cost_per_minute").val());
        $("#cost_per_minute_sum").text(cost_per_minute_sum.toLocaleString(loc,{maximumFractionDigits:2, minimumFractionDigits: 2}));

        var cost_total = Number(cost_per_kwh_sum) + Number(cost_per_minute_sum) + Number(cost_per_session_sum) + Number(cost_idle_fee_total_sum);
        $("#cost_total").text(cost_total.toLocaleString(loc,{maximumFractionDigits:2, minimumFractionDigits: 2}));

        $("#currency").text($("#cost_currency").val());

        return cost_total.toFixed(2);
    }

    function save(event)
    {
	event.preventDefault();

        var total = updatecalculation();

        var jj = {
            "id" : <?php echo($_REQUEST["id"]); ?>,
            "cost_currency" : $("#cost_currency").val(),
            "cost_per_kwh" : $("#cost_per_kwh").val().replace(",", "."),
            "cost_per_session" : $("#cost_per_session").val().replace(",", "."),
            "cost_per_minute" : $("#cost_per_minute").val().replace(",", "."),
            "cost_idle_fee_total" : $("#cost_idle_fee_total").val().replace(",", "."),
            "cost_kwh_meter_invoice" : $("#cost_kwh_meter_invoice").val().replace(",", "."),
            "cost_total" : total,
        }

        var json_string = JSON.stringify(jj);

        var jqxhr = $.post("chargingcost_write.php",
		{
            JSON: json_string
		}).always(function() {
		alert("<?php t('Saved!'); ?>");
		window.location.href = document.referrer;
		});

    }


</script>
<body>
<?php
include "menu.php";
echo(menu("Charging Costs"));
?>
<div>
<h1 style="color: red;"><span id="errortext"></span></h1>
<form onsubmit="save(event);">
<table>
<tr><td><h1><?php t("Charging Costs"); ?></h1></td><td></td></tr>
<tr><td><?php t("Charger"); ?>:&nbsp;</td><td colspan="4"><span id="address"></span></td></tr>
<tr><td><?php t("Date"); ?>:&nbsp;</td><td colspan="4"><span id="StartDate"></span></td></tr>
<tr><td><?php t("Duration"); ?>:&nbsp;</td><td colspan="4"><span id="minutes"></span></td></tr>
<tr><td><?php t("Charged"); ?>:&nbsp;</td><td colspan="4"><span id="charge_energy_added"></span></td></tr>
<tr><td><?php t("Efficiency"); ?>:&nbsp;</td><td colspan="4"><span id="charge_efficiency"></span></td></tr>
<tr><td>&nbsp;</td></tr>
<tr><td><?php t("Currency"); ?>:&nbsp;</td><td><input id="cost_currency" placeholder="EUR" tabindex="1"></span></td><td></td><td></td></tr>
<tr><td><?php t("kWh according to meter/invoice"); ?>:&nbsp;</td><td><input id="cost_kwh_meter_invoice" type="number" step="any" inputmode="decimal" tabindex="2"></span></td></tr>
<tr><td><?php t("Cost per kWh"); ?>:&nbsp;</td><td><input id="cost_per_kwh" type="number" step="any" inputmode="decimal" tabindex="3"></span></td><td> * <span id="kwh_charged"></span>&nbsp;<?php t("kWh"); ?></td><td class="sum"><span id="cost_per_kwh_sum"></span></td></tr>
<tr><td><?php t("Cost per charge"); ?>:&nbsp;</td><td><input id="cost_per_session" type="number" step="any" inputmode="decimal" tabindex="4"></span></td><td></td><td class="sum"><span id="cost_per_session_sum"></span></td></tr>
<tr><td><?php t("Cost per minute"); ?>:&nbsp;</td><td><input id="cost_per_minute" type="number" step="any" inputmode="decimal" tabindex="5"></span></td><td> * <span id="minutes_charged"></span>&nbsp;<?php t("Minutes"); ?></td><td class="sum"><span id="cost_per_minute_sum"></span></td></tr>
<tr><td><?php t("Idle cost per minute"); ?>:&nbsp;</td><td><input id="cost_idle_fee_total" type="number" step="any" inputmode="decimal" tabindex="6"></span></td><td></td><td class="sum"><span id="cost_idle_fee_total_sum"></span></td></tr>
<tr><td colspan="4"><hr></td></tr>
<tr><td><b><?php t("Total"); ?>:&nbsp;</b></td><td></td><td></td><td class="sum"><b><span id="cost_total"></span></b></td><td><b><span id="currency"></span></b></td></tr>
<tr><td></td><td></td><td></td><td>&nbsp;</td></tr>
<tr><td></td><td></td><td></td><td><button style="float: right;" tabindex="7"><?php t("Save"); ?></button></td></tr>
</table>
</form>
</div>
