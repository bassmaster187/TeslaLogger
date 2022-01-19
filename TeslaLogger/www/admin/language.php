<?php 
$language = "en";
$TemperatureUnit = "";
$LengthUnit = "";
$PowerUnit = "";
$LengthFactor = 1;

$Display100pctEnable = "false";

if (file_exists("/etc/teslalogger/settings.json"))
{
	$json = file_get_contents('/etc/teslalogger/settings.json');
	$json_data = json_decode($json,true);
	$language = $json_data["Language"];
	$TemperatureUnit = $json_data["Temperature"];
	$LengthUnit = $json_data["Length"];
	if ($LengthUnit == "mile")
		$LengthFactor = 1.60934;

	$PowerUnit = $json_data["Power"];
	$Display100pctEnable = $json_data["Display100pctEnable"];

}

$filename = "/etc/teslalogger/language-".$language.".txt";
global $ln;

if(file_exists($filename))
{ 
	$ln = array();
	// $ln = parse_ini_file($filename);
	$lines = file($filename, FILE_IGNORE_NEW_LINES);
	foreach ($lines as $l)
	{
		$a = explode("=",$l);
		if (count($a) == 1)
			continue;

		$ln[$a[0]] = $a[1];
	}
}
	
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
		{
			echo($ln[$t]);
		}
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
