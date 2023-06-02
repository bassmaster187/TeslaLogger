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
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="jquery/jquery-migrate-1.4.1.min.js"></script>
	<script src="static/jquery/datatables/1.13.4/datatables.min.js"></script>
	<link rel='stylesheet' href="static/jquery/datatables/1.13.4/datatables.min.css">
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />

<style>
input.newJourney {width: 300px;}
select.newJourney {width: 500px;}
</style>

<script>
	<?php
	if (isset($_REQUEST["carid"]))
        echo("var carid=".$_REQUEST["carid"].";\n");
    else
        echo("var carid=-1;\n");

    echo("var url_grafana='".$URL_Grafana."';\n");
	?>

    $(document).ready(function(){
		var loc;
		if (navigator.languages != undefined) loc = navigator.languages[0];
			else loc = navigator.language;
        var d = {
                    load: "1",
                    carid: carid
				};
        var url = "journeys/list";

        var dt = $("#myDT").DataTable({
            "language": {
				"lengthMenu": "<?php t("Display _MENU_ records per page"); ?>",
				"zeroRecords": "<?php t("Nothing found"); ?>",
				"info": "<?php t("Showing _START_ to _END_ of _TOTAL_ entries"); ?>",
				"infoEmpty": "<?php t("No records available"); ?>",
				"infoFiltered": "<?php t("(filtered from _MAX_ total records)"); ?>",
				"search": "<?php t("Search"); ?>",
				paginate: {
					first: "<?php t("First"); ?>",
					last: "<?php t("Last"); ?>",
					next: "<?php t("Next"); ?>",
					previous: "<?php t("Previous"); ?>"
				}
			},
			"order": [[1, "asc"]],
            "pageLength": 15,
            "columns": [
                { "render": function(data, type, row, meta){
                    if(type === 'display'){
                        return GetGrafanaLink(row, "JhRusymgk", row["Id"], "");
                    }

                    return row["Id"];
                }},

                { "render": function(data, type, row, meta){
                    if(type === 'display'){
                        return GetGrafanaLink(row, "RG_DxSmgk", row["name"], "&var-Charger=ON");
                    }

                    return row["name"];
                }},

                { "data": "Start_address"},

                { "render": function(data, type, row, meta){
                    if(type === 'display'){
                        var sdd = new Date(row["StartDate"]);
                        return sdd.toLocaleTimeString(loc,{ day: '2-digit', month: '2-digit', year: 'numeric' });
                    }
                    else if (type === 'sort')
                    {
                        return new Date(row["StartDate"]).getTime();
                    }

                    return "";
                }},

                { "data": "End_address"},

                { "render": function(data, type, row, meta){
                    if(type === 'display'){
                        var sdd = new Date(row["EndDate"]);
                        return sdd.toLocaleTimeString(loc,{ day: '2-digit', month: '2-digit', year: 'numeric' });
                    }
                    else if (type === 'sort')
                    {
                        return new Date(row["EndDate"]).getTime();
                    }
                    return "";
                }},

                {	className: 'dt-right',
					"render": function(data, type, row, meta){
                    if(type === 'display' || type === 'sort'){
                        var distance = row["distance"];
                        var consumption_kwh = row["consumption_kwh"];
                        var avg_consumption = consumption_kwh / distance * 100 * <?= $LengthFactor ?>;
						return type === "display" || type === "filter" ? avg_consumption.toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1}) : avg_consumption;
                    }
                    return row["charged_kwh"];
                }},

                {	data: "consumption_kwh",
					className: 'dt-right',
					"render": function(data, type, row, meta){
					if (data == null) {
						data = 0;
					}
					if ((isNaN(data)) || (data === "")) {
						data = 0;
					}
					if (typeof(data) === "number") {
						var output = data.toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1});
						return type === "display" || type === "filter" ? output : data;
					}
					return "";
				}},

                {	data: "charged_kwh",
					className: 'dt-right',
					"render": function(data, type, row, meta){
					if (data == null) {
						data = 0;
					}
					if ((isNaN(data)) || (data === "")) {
						data = 0;
					}
					if (typeof(data) === "number") {
						var output = data.toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1});
						if(type === 'display'){
							output = GetGrafanaLink(row, "TSmNYvRRk", output, "")
							return type === "display" || type === "filter" ? output : data;
						}
					}
                    return type === "display" || type === "filter" ? data : data;
				}},

                {	className: 'dt-right',
					"render": function(data, type, row, meta){
                    if(type === 'display'){
                        var MINUTES = row["drive_duration_minutes"];
                        return minutesToHHMM(MINUTES);
                    }
                    else if (type === 'sort')
                    {
                        return row["drive_duration_minutes"];
                    }

                    return "";
                }},

                {	className: 'dt-right',
					"render": function(data, type, row, meta){
						if(type === 'display'){
							var MINUTES = row["charge_duration_minutes"];
							return minutesToHHMM(MINUTES);
						}
						else if (type === 'sort')
						{
							return row["charge_duration_minutes"];
						}

						return "";
                }},

                { 	className: 'dt-right',
					"render": function(data, type, row, meta){
						var distance = row["distance"] / <?= $LengthFactor ?>;
						var output = distance.toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1});

						if (type === 'display'){
							output = GetGrafanaLink(row, "Y8upc6ZRk", output, "");
							return type === "display" || type === "filter" ? output : distance;
						}
						else if (type === 'sort')
							return type === "display" || type === "filter" ? output : distance;

                    return "";
                }},

                {	data: "cost_total",
					className: 'dt-right',
					"render": function(data, type, row, meta){
					if (data == null) {
						data = 0;
					}
					if ((isNaN(data)) || (data === "")) {
						data = 0;
					}
					if (typeof(data) === "number") {
						var output = data.toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1});
						return output;
					}
					return "";
				}},

                {	data: "CO2kg",
					className: 'dt-right',
					"render": function(data, type, row, meta){
					if (data == null) {
						data = 0;
					}
					if ((isNaN(data)) || (data === "")) {
						data = 0;
					}
					if (typeof(data) === "number") {
						var output = data.toLocaleString(loc,{maximumFractionDigits:1, minimumFractionDigits: 1});
						return output;
					}
					return "";
				}},

                { "render": function(data, type, row, meta){
                    if(type === 'display'){
                        return "<a href='javascript:delJourney("+row["Id"] +",\""+row["name"] + "\")'><?php t('Delete'); ?></a>";
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
			var dDate;
			var sLocation;
            $.each(json, function(){
				if (this.Value=="Please Select") {
					sValue = "<?php t("Please select"); ?>";
				} else {
					dDate = new Date(this.Value.substring(0,this.Value.search(" - ")));
					sLocation = this.Value.slice(this.Value.search(" - ")+3);
					sValue = dDate.toLocaleTimeString(loc,{ day: '2-digit', month: '2-digit', year: 'numeric' }) + " - " + sLocation;
				}
                $("#start").append('<option value="'+ this.Key +'">'+ sValue +'</option>');
            });
        });

        $('#start').on('change', function()
        {
            $("#end").empty();
            $("#end").append('<option><?php t("Please wait"); ?></option>');

            var d = {
                    carid: carid,
                    StartPosID: this.value
				};
            var url = "journeys/create/end";
            $.post("teslaloggerstream.php", {url: url, data: JSON.stringify(d)}).always(function (data) {
                $("#end").empty();
                var json = JSON.parse(data);
				var sValue;
                $.each(json, function(){
					if (this.Value=="Please Select") {
						sValue = "<?php t("Please select"); ?>";
					} else {
						dDate = new Date(this.Value.substring(0,this.Value.search(" - ")));
						sLocation = this.Value.slice(this.Value.search(" - ")+3);
						sValue = dDate.toLocaleTimeString(loc,{ day: '2-digit', month: '2-digit', year: 'numeric' }) + " - " + sLocation;
					}
                    $("#end").append('<option value="'+ this.Key +'">'+ sValue +'</option>');
                });
            });
        });

        $("#btnSave").click(function() {
            if ($("#name").val().length == 0)
            {
                alert("<?php t('Journey name missing!'); ?>");
                return;
            }
            else if ($("#start").val() == "")
            {
				alert("<?php t('Please select start point!'); ?>");
                return;
            }
            else if ($("#end").val() == "")
            {
                alert("<?php t('Please select end point!'); ?>");
                return;
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

    function delJourney(id,name)
    {
		var text = "<?php t('DelJourney'); ?>";
		text = text.replace("{id}",id + " (" + name + ")");
		if (confirm(text))
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

    function GetGrafanaLink(row, uid, text, parameters)
    {
        var start = new Date(row["StartDate"]);
        var ustart = start.getTime();

        var end = new Date(row["EndDate"]);
        var uend = end.getTime();
        var temp = "<a href='";
        temp += url_grafana;
        temp += temp.endsWith("/") ? "" : "/";
        temp += "d/";
        temp += uid;
        temp += "/dashboard?orgId=1&from=";
        temp += ustart +"&to="+ uend +"&var-Car="+carid+ parameters+ "' target=\"_blank\">";
        temp += text;
        temp += "</a>";

        return temp;
    }
</script>
</head>
<body>
<div>
<?php
	include "menu.php";
	menu("Journeys");
?>
    <h1><?php t("Journeys"); ?></h1>
    <p><?php t("TextJourneys"); ?></p>

<table id="myDT">
<thead>
    <tr>
        <th><?php t("ID"); ?></th>
        <th><?php t("Journey"); ?></th>
        <th><?php t("Start address"); ?></th>
        <th><?php t("Start"); ?></th>
        <th><?php t("Target address"); ?></th>
        <th><?php t("End"); ?></th>
        <th><?php t("Ø Consumption"); ?> <?php t("kWh"); ?></th>
        <th><?php t("Consumed kWh"); ?></th>
        <th><?php t("Charged kWh"); ?></th>
        <th><?php t("Drive time [h]"); ?></th>
        <th><?php t("Charge time [h]"); ?></th>
        <th><?php if ($LengthUnit == "mile") t("Distance [mi]"); else t("Distance [km]"); ?></th>
        <th><?php t("Charging costs"); ?></th>
        <th><?php echo str_replace("CO2","CO<sub>2</sub>",get_text("CO2 [kg]")); ?></th>
        <th></th>
    </tr>
</thead>
</table>

<h2><?php t("New Journey"); ?></h2>
<table>
    <tr><td><?php t("Journey name"); ?>:&nbsp;</td><td><input class="newJourney" id="name"></td></tr>
    <tr><td><?php t("Start"); ?>: </td><td><select class="newJourney" id="start"><option><?php t("Please wait"); ?></option></select></td></tr>
    <tr><td><?php t("End"); ?>: </td><td><select class="newJourney" id="end"><option><?php t("Please select start first"); ?></option></select></td></tr>
    <tr><td></td><td><button id="btnSave"><?php t("Save"); ?></button></td></tr>
</table>
</div>
</body>
</html>
