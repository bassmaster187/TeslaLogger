<!DOCTYPE html>
<?php
require("language.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger Restore Database 1.0</title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=17">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.css">
	<script src="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
  </head>
<body>
<div class="tl-page">
<?php 
    include "menu.php";
    echo(menu("Restore"));
?>
	<div class="tl-page-head">
		<h2 class="tl-page-title"><?php t("Restore"); ?></h2>
	</div>

	<div class="tl-card">
		<p class="tl-muted" style="margin-top:0;"><?php t("TextRestore1"); ?></p>
		<p class="tl-muted"><?php t("TextRestore2"); ?></p>
		<p class="tl-muted" style="margin-bottom:0;"><?php t("TextRestore3"); ?></p>
	</div>

	<div class="tl-card tl-small-form-card">
		<form action="restore_upload.php" method="post" enctype="multipart/form-data">
			<div class="tl-form tl-small-form">
				<div class="tl-form-item">
					<label for="fileToUpload"><?php t("File"); ?></label>
					<input class="tl-small-input" type="file" name="fileToUpload" id="fileToUpload">
				</div>
				<div class="tl-form-actions">
					<input type="submit" value="<?php t("Restore"); ?>" name="submit">
				</div>
			</div>
		</form>
	</div>
</div>
</body>
</html>