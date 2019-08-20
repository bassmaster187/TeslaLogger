<?php
$json = file_get_contents('/etc/teslalogger/settings.json');
$json_data = json_decode($json,true);
$language = $json_data["Language"];
$filename = "/etc/teslalogger/language-".$language.".txt";
if(!file_exists($filename)) $filename = "/etc/teslalogger/language-en.txt";
//print_r($filename);

global $ln;
$ln = parse_ini_file($filename);
//print_r($ln);
//echo($ln["Fahrzeuginfo"]);

function t($t)
{
	global $ln;
	if ($ln == null)
		echo $t;
		
	echo($ln[$t]);
};
?>
