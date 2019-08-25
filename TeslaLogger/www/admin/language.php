<?php
$json = file_get_contents('/etc/teslalogger/settings.json');
$json_data = json_decode($json,true);
$language = $json_data["Language"];
<<<<<<< HEAD

$filename = "/etc/teslalogger/language-".$language.".txt";
global $ln;

if(file_exists($filename)) 
	$ln = parse_ini_file($filename);
	
=======
$filename = "/etc/teslalogger/language-".$language.".txt";
if(!file_exists($filename)) $filename = "/etc/teslalogger/language-en.txt";
//print_r($filename);

global $ln;
$ln = parse_ini_file($filename);
>>>>>>> c57eb95ff09f78417f0d5d5e6e2f8bbafec29531
//print_r($ln);
//echo($ln["Fahrzeuginfo"]);

function t($t)
{
	global $ln;
<<<<<<< HEAD
	if ($ln == null || empty($language) || $language == "de" )
=======
	if ($ln == null)
>>>>>>> c57eb95ff09f78417f0d5d5e6e2f8bbafec29531
		echo $t;
		
	echo($ln[$t]);
};
?>
