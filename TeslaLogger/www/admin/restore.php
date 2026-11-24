<!DOCTYPE html>
<?php
require("language.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger <?php t("Restore"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=28">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.css">
	<script src="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
  </head>
<body>
<?php 
    include "menu.php";
    echo(menu("Restore"));
?>
<div class="tl-page">
	<div class="tl-page-head">
		<h2 class="tl-page-title"><?php t("Restore"); ?></h2>
	</div>

	<div class="tl-alert tl-alert-warn">
		<ul class="tl-alert-list">
			<li><?php t("TextRestore1"); ?></li>
			<li><?php t("TextRestore2"); ?></li>
			<li><?php t("TextRestore3"); ?></li>
		</ul>
	</div>

	<div class="tl-card tl-small-form-card">
		<h2 class="tl-card-title"><?php t("Restore"); ?></h2>
		<form action="restore_upload.php" method="post" enctype="multipart/form-data">
			<div class="tl-form tl-small-form">
				<div class="tl-form-item">
					<label for="fileToUpload"><?php t("File"); ?></label>
					<input class="tl-small-input" type="file" name="fileToUpload" id="fileToUpload" accept=".gz">
					<p class="tl-muted tl-form-note"><?php t("Select the backup file (.gz) from the backup folder of your Teslalogger."); ?></p>
					<p class="tl-muted tl-form-note"><?php t("You can also restore a geofence backup. A file named geofence-private*.gz replaces geofence-private.csv."); ?></p>
				</div>
				<div class="tl-form-actions">
					<a href="backup.php"><?php t("Create a backup first"); ?></a>
					<input type="submit" value="<?php t("Restore"); ?>" name="submit">
				</div>
			</div>
		</form>
	</div>
</div>
</body>
</html>