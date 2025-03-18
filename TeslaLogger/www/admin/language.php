<?php 

require_once("tools.php");
$language = "en";
$TemperatureUnit = "";
$LengthUnit = "";
$PowerUnit = "";
$LengthFactor = 1;
$URL_Grafana = "";
$Range = 'IR';

$Display100pctEnable = "false";

if (file_exists("/etc/teslalogger/settings.json"))
{
	$json = file_get_contents('/etc/teslalogger/settings.json');
	$json_data = json_decode($json,true);
	$language = $json_data["Language"];
	$TemperatureUnit = $json_data["Temperature"];
	$LengthUnit = $json_data["Length"];
	if ($LengthUnit == "mile")
		$LengthFactor = 1.609344;

	$PowerUnit = $json_data["Power"];
	$Display100pctEnable = $json_data["Display100pctEnable"];

	if (empty($json_data["URL_Grafana"]))
	{
		if (isDocker())
			$URL_Grafana = "http://localhost:3000/";
		else
			$URL_Grafana = "http://raspberry:3000/";
	}
	else
		$URL_Grafana = $json_data["URL_Grafana"];
		if (substr($URL_Grafana,-1) != "/") {
			$URL_Grafana = $URL_Grafana . "/";
		}
	$Range = $json_data["Range"];
}

$filename = "/tmp/language-$language.txt";
$filename_en = "/tmp/language-en.txt";
global $ln;
global $lnen;

// get files from Teslalogger
GetFileFromTeslaloggerAndWriteToTMP("language-$language.txt");
GetFileFromTeslaloggerAndWriteToTMP("language-en.txt");

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

		if (empty($a[1]))
			continue;

		$tmp = $a[1];

		if ($tmp[0] == '"' && $tmp[strlen($tmp)-1] == '"')
			$tmp = substr($tmp, 1,-1);

		$tmp = str_replace('"_QQ_"','"', $tmp);

		$ln[$a[0]] = $tmp;
	}
}

if(file_exists($filename_en))
{ 
	$lnen = array();
	// $ln = parse_ini_file($filename);
	$lines = file($filename_en, FILE_IGNORE_NEW_LINES);
	foreach ($lines as $l)
	{
		$a = explode("=",$l);
		if (count($a) == 1)
			continue;

		$tmp = $a[1];

		if ($tmp[0] == '"' && $tmp[strlen($tmp)-1] == '"')
			$tmp = substr($tmp, 1,-1);

		$tmp = str_replace('"_QQ_"','"', $tmp);

		$lnen[$a[0]] = $tmp;
	}
}
	
//print_r($ln);
//echo($ln["Fahrzeuginfo"]);

function t($t)
{
	global $ln;
	global $lnen;

	if ($ln == null)
		echo $t;
	else
	{
		if (isset($ln[$t]))
		{
			echo($ln[$t]);
		}
		else if (isset($lnen[$t]))
		{
			echo($lnen[$t]);
		}
		else
			echo $t;
	}
};

function get_text($t)
{
	global $ln;
	global $lnen;
	
	if ($ln == null)
		return $t;
	else
	{
		if (isset($ln[$t]))
		{
			return($ln[$t]);
		}
		else if (isset($lnen[$t]))
		{
			return($lnen[$t]);
		}
		else
			return $t;
	}
};

function logger($t)
{
	$logfile = "/etc/teslalogger/nohup.out";
	$time = date("d.m.Y H:i:s", time());
	$ret2 =	file_put_contents($logfile, $time . " : ". $t ."\r\n", FILE_APPEND);
}
?>
