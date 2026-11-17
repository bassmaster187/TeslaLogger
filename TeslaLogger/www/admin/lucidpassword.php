<!DOCTYPE html>
<?php
require("language.php");
require_once("tools.php");

$actual_link = GetRequestProtocol() . "://$_SERVER[HTTP_HOST]$_SERVER[REQUEST_URI]";

?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?php t("Teslalogger Lucid Credentials"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=12">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<script src="static/jquery/datatables/1.13.4/datatables.min.js"></script>
	<link rel='stylesheet' href="static/jquery/datatables/1.13.4/datatables.min.css">
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />

	<script>
	<?php
	if (isset($_REQUEST["id"]))
		echo("var dbid=".$_REQUEST["id"].";\n");
	?>
	$(document).ready(function(){
		$('input[type="checkbox"]').on('click keyup keypress keydown', function (event) {
			if($(this).is('[readonly]')) { return false; }
		});
	});

	function save() {
		var d = {
			email: $("#email").val(),
			password: $("#password").val(),
			region: $("#region").val(),
		};

		var jqxhr = $.post("teslaloggerstream.php", {url: "lucid/getallcars", data: JSON.stringify(d)})
		.always(function (data) {
			$("#carid").empty();

			if (data == "Error: StatusCode.UNAUTHENTICATED")
			{
				alert("<?php t('Wrong credentials!'); ?>");
				return;
			}
			else {
				$("#carid").closest(".tl-form-item").css("visibility", "visible");
				var obj = JSON.parse(data);
				for (var i=0; i < obj.length; i++)
				{
					$("#carid").append("<option id='"+obj[i]['VIN']+"'>"+obj[i]['VIN']+ " - "+obj[i]['Nickname']+  " - "+obj[i]['Model'] + "</option>");
				}
				$("#btnSave").css("visibility","");
			}
		});

	}

	function saveCar() {
		var d = {
			email: $("#email").val(),
			password: $("#password").val(),
			region: $("#region").val(),
			vin: $("#carid option:selected").attr("id"),
			id: dbid
		};

		var jqxhr = $.post("teslaloggerstream.php", {url: "lucid/savecar", data: JSON.stringify(d)})
		.always(function (data) {
			$("#carid").empty();

			if (data == "Error: StatusCode.UNAUTHENTICATED")
			{
				alert("<?php t('Wrong credentials!'); ?>");
				return;
			}
			else {
				alert("<?php t('Check Logfile!'); ?>");
				window.location.href='logfile.php';
			}
		});

	}

	function deleteCar()
	{
		if (confirm("<?php t('Do you really want to delete this vehicle?'); ?>"))
		{
			var d = {
				id: dbid,
				deletecar: 1
			};

			var jqxhr = $.post("teslaloggerstream.php", {url: "lucid/savecar", data: JSON.stringify(d)}).always(function () {
				alert("<?php t('Check Logfile in one minute!'); ?>");
				window.location.href='index.php';
			});
		}
	}

	</script>
</head>
<body>
<?php
include "menu.php";
menu("Credentials");
?>
<div class="tl-page">
<div class="tl-page-head">
	<h2 class="tl-page-title"><?php t("Teslalogger Lucid Credentials"); ?></h2>
</div>

<?php
if (isset($_REQUEST["id"]))
{
?>
<div class="tl-card">
	<h2 class="tl-card-title"><?php t("Please enter your Lucid Motors account credentials"); ?></h2>
	<div class="tl-form tl-small-form">
		<div class="tl-form-item">
			<label for="email"><?php t("Email"); ?></label>
			<input class="tl-small-input" id="email" type="text" autocomplete="new-password" />
		</div>
		<div class="tl-form-item">
			<label for="password"><?php t("Password"); ?></label>
			<input class="tl-small-input" id="password" type="password" autocomplete="new-password" />
		</div>
		<div class="tl-form-item">
			<label for="region"><?php t("Region"); ?></label>
			<select id="region">
				<option value="us">USA</option>
				<option value="eu">Europe</option>
				<option value="sa">Saudi Arabia</option>
			</select>
		</div>
		<div class="tl-form-item" style="visibility:collapse">
			<label for="carid"><?php t("Car"); ?></label>
			<select id="carid"></select>
			<span id="vinlabel"></span>
		</div>
		<div class="tl-form-actions">
			<?php if (isset($_REQUEST["id"]) && $_REQUEST["id"] != -1) { ?>
			<button id="deletebutton" onclick="deleteCar();" class="redbutton" style="margin-right:auto"><?php t("Delete"); ?></button>
			<?php } ?>
			<button id="btnGetCars" onclick="save();"><?php t("Get Cars"); ?></button>
			<button id="btnSave" style="visibility:collapse" onclick="saveCar();"><?php t("Save"); ?></button>
		</div>
	</div>
</div>
</div>
<?php
}
?>
</body>
</html>
