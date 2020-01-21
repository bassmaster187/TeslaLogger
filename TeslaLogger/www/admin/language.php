<?php
$language = "en";
if (file_exists("/etc/teslalogger/settings.json"))
{
	$json = file_get_contents('/etc/teslalogger/settings.json');
	$json_data = json_decode($json,true);
	$language = $json_data["Language"];
}

$filename = "/etc/teslalogger/language-".$language.".txt";
global $ln;

if(file_exists($filename)) 
	$ln = parse_ini_file($filename);
	
//print_r($ln);
//echo($ln["Fahrzeuginfo"]);

function t($t)
{
	global $ln;
	if ($ln == null)
		echo $t;
	else
	{
		if (isset($ln[$t]))
			echo($ln[$t]);
		else
			echo $t;
	}
};

function logger($t)
{
	$logfile = "/etc/teslalogger/nohup.out";
	$time = date("d.m.Y H:i:s", time());
	$ret2 =	file_put_contents($logfile, $time . " : ". $t ."\r\n", FILE_APPEND);
}
?>
