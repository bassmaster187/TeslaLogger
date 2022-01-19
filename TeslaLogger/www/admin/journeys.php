<!DOCTYPE html>
<?php
require_once("language.php");
require_once("tools.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?php t("Journeys"); ?></title>
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
	if (isset($_REQUEST["carid"]))
        echo("var carid=".$_REQUEST["carid"].";\n");
    else
        echo("var carid=-1;\n");
	?>

    $(document).ready(function(){
        var d = {
                    load: "1",
                    carid: carid
				};
        var url = "journeys/list";

        var dt = $("#myDT").DataTable({
            "order": [[1, "asc"]],
            "pageLength": 15,
            "columns": [
                { "data": "Id"},
                { "data": "name"},
                { "data": "Start_address"},
                { "render": function(data, type, row, meta){
                    if(type === 'display'){
                        var sdd = new Date(parseInt(row["StartDate"].substr(6)));
                        return sdd.toLocaleString();
                    }

                    return "";
                }},
                { "data": "End_address"},
                { "render": function(data, type, row, meta){
                    if(type === 'display'){
                        var sdd = new Date(parseInt(row["EndDate"].substr(6)));
                        return sdd.toLocaleString();
                    }

                    return "";
                }},
                { "render": function(data, type, row, meta){ 
                    if(type === 'display'){
                        var distance = row["distance"];
                        var consumption_kwh = row["consumption_kwh"];
                        var avg_consumption = consumption_kwh / distance * 100 * <?= $LengthFactor ?>;
                        
                        return parseFloat(avg_consumption).toFixed(1);
                    }

                    return "";
                }},
                { "data": "consumption_kwh"},
                { "data": "charged_kwh"},
                { "render": function(data, type, row, meta){
                    if(type === 'display'){
                        var MINUTES = row["drive_duration_minutes"];
                        return minutesToHHMM(MINUTES);
                    }

                    return "";
                }},
                { "render": function(data, type, row, meta){
                    if(type === 'display'){
                        var MINUTES = row["charge_duration_minutes"];
                        return minutesToHHMM(MINUTES);
                    }

                    return "";
                }},
                { "render": function(data, type, row, meta){
                    if(type === 'display'){
                        var distance = row["distance"] / <?= $LengthFactor ?>;
                        return parseFloat(distance).toFixed(1);;
                    }

                    return "";
                }},
                { "render": function(data, type, row, meta){
                    if(type === 'display'){
                        
                        return "<a href='javascript:delJourney("+row["Id"] +")'>Del</a>";
                    }

                    return "";
                }}
            ],
            "ajax": 
            {
                "url": "teslaloggerstream.php",
                "type": "POST",
                "data": {url: url, data: JSON.stringify(d)}
            }
        });

        var url = "journeys/create/start";
        var d = {
                    carid: carid
				};
        var jqxhr = $.post("teslaloggerstream.php", {url: url, data: JSON.stringify(d)}).always(function (data) {
            $("#start").empty();
            var json = JSON.parse(data);
            $.each(json, function(){
                $("#start").append('<option value="'+ this.Key +'">'+ this.Value +'</option>');
            });
        });

        $('#start').on('change', function()
        {
            $("#end").empty();
            $("#end").append('<option>Please Wait!</option>');

            var d = {
                    carid: carid,
                    StartPosID: this.value
				};
            var url = "journeys/create/end";
            $.post("teslaloggerstream.php", {url: url, data: JSON.stringify(d)}).always(function (data) {
                $("#end").empty();
                var json = JSON.parse(data);
                $.each(json, function(){
                    $("#end").append('<option value="'+ this.Key +'">'+ this.Value +'</option>');
                });
            });
        });

        $("#btnSave").click(function() {
            if ($("#name").val().length == 0)
            {
                alert("Journey Name Missing!");
                reutrn;
            }
            else if ($("#start").val() == "")
            {
                alert("Please Select Start Point!");
                reutrn;
            }
            else if ($("#end").val() == "")
            {
                alert("Please Select End Point!");
                reutrn;
            }

            var d = {
                    CarID: carid,
                    StartPosID: $("#start").val(),
                    EndPosID: $("#end").val(),
                    name: $("#name").val()
				};
            var url = "journeys/create/create";
            $.post("teslaloggerstream.php", {url: url, data: JSON.stringify(d)}).always(function (data) {
                location.reload();
            });
        });
    });

    function delJourney(id)
    {
        if (confirm("Do you really want to delete journey " + id))
        {
            var url = "journeys/delete/delete";
            var d = {
                        load: "1",
                        id: id
                    };
            var jqxhr = $.post("teslaloggerstream.php", {url: url, data: JSON.stringify(d)}).always(function (data) {
                location.reload();
            });
        }
    }
	
    function minutesToHHMM(MINUTES)
    {
        var m = MINUTES % 60;
        var h = (MINUTES-m)/60;
        var HHMM = h.toString() + ":" + (m<10?"0":"") + m.toString();
        return HHMM;
    }
    

</script>
</head>
<body style="padding-top: 5px; padding-left: 10px;">
<div>
<?php 
include "menu.php";
menu("Journeys");

// echo("Unit: $LengthUnit<br>");
// echo("Milefactor: $milefactor");
?>
<div>
    <h1><?php t("Journeys"); ?>:  <a href="https://github.com/bassmaster187/TeslaLogger/blob/master/journeys.md"><img src="img/icon-help-24.png" class="pointer"/></a></h1>
<div>

<table id="myDT">
<thead>
    <tr>
        <th>Id</th>
        <th><?php t("Journey"); ?></th>
        <th><?php t("Start Adresse"); ?></th>
        <th><?php t("Start"); ?></th>
        <th><?php t("Ziel Adresse"); ?></th>
        <th><?php t("Ende"); ?></th>
        <th><?php t("Ø Verbrauch kWh"); ?></th>
        <th><?php t("Verbraucht kWh"); ?></th>
        <th><?php t("geladen kWh"); ?></th>
        <th><?php t("Fahrzeit [h]"); ?></th>
        <th><?php t("Ladezeit [h]"); ?></th>
        <th><?php if ($LengthUnit == "mile") t("Strecke [mi]"); else t("Strecke [km]"); ?></th>
        <th></th>
    </tr>
</thead>
</table>

<h2>New Journey</h2>
<table>
    <tr><td>Journey Name:</td><td><input width="100%" id="name"></td></tr>
    <tr><td>Start</td><td><select width="100%" id="start"><option>Please Wait!</option></select></td></tr>
    <tr><td>End</td><td><select width="100%" id="end"><option>Please Select Start First!</option></select></td></tr>
    <tr><td></td><td><button id="btnSave">Save</button></td></tr>
</table>
</div>
</body>
</html>

