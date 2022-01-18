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
                { "data": "distance"},
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
    });

    function delJourney(id)
    {
        if (confirm("Do you really want to delete journey " + id))
        {
            var url = "/journeys/delete/delete";
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
<div style="max-width: 1260px;">
<?php 
include "menu.php";
menu("Journeys");
?>
<div>
    <h1><?php t("Journeys"); ?>:  <a href="https://github.com/bassmaster187/TeslaLogger/blob/master/journeys.md"><img src="img/icon-help-24.png" class="pointer"/></a></h1>
<div>

<table id="myDT">
<thead>
    <tr>
        <th>Id</th>
        <th>Journey</th>
        <th>Origin</th>
        <th>Start</th>
        <th>Destination</th>
        <th>End</th>
        <th>Consumption</th>
        <th>Charged</th>
        <th>Driving Duration</th>
        <th>Charging Duration</th>
        <th>Distance</th>
        <th></th>
    </tr>
</thead>
</table>
</div>
</body>
</html>

