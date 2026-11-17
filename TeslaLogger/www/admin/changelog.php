<!DOCTYPE html>
<?php
require_once("language.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1.0" />
	<meta name="apple-mobile-web-app-title" content="Teslalogger Config">
	<link rel="apple-touch-icon" href="img/apple-touch-icon.png">
	<title>Teslalogger <?php t("Changelog"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
 	<link rel="stylesheet" href="static/teslalogger_style.css?v=4">
 	<link rel="stylesheet" href="static/teslalogger_ui.css?v=2">
 	<link rel="stylesheet" href="static/teslalogger_changelog.css?v=1">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<script src="static/jquery/jquery-migrate-1.4.1.min.js"></script>
	<link rel='stylesheet' id='genericons-css'  href='static/genericons.css?ver=3.0.3' type='text/css' media='all' />
</head>
<body>

<?php 
    include "menu.php";
    echo(menu("Changelog"));

require_once("Parsedown.php");
GetFileFromTeslaloggerAndWriteToTMP("changelog.md");
$md = file_get_contents("/tmp/changelog.md");
?>
<div class="tl-page">
	<div class="tl-page-head">
		<h2 class="tl-page-title"><?php t("Changelog"); ?></h2>
	</div>
	<div class="tl-card">
		<h2 class="tl-card-title"><?php t("Changelog"); ?></h2>
		<div class="tl-markdown">
<?php echo Parsedown::instance()->text($md); ?>
		</div>
	</div>
</div>
<?php
?>
</body>
</html>