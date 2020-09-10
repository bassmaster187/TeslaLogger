<?PHP
require("tools.php");

$SleepTimeSpanStart = $_POST["SleepTimeSpanStart"];
echo($SleepTimeSpanStart);

$SleepTimeSpanEnd = $_POST["SleepTimeSpanEnd"];
echo($SleepTimeSpanEnd);

$SleepTimeSpanEnable = $_POST["SleepTimeSpanEnable"];
echo($SleepTimeSpanEnable);

$ScanMyTesla = $_POST["ScanMyTesla"];
echo($ScanMyTesla);

$Language = $_POST["Language"];
$Power = $_POST["Power"];
$Temperature = $_POST["Temperature"];
$Length = $_POST["Length"];
$URL_Admin = $_POST["URL_Admin"];
$URL_Grafana = $_POST["URL_Grafana"];
$HTTPPort = $_POST["HTTPPort"];
$ZoomLevel = $_POST["ZoomLevel"];
$update = $_POST["update"];
$Range = $_POST["Range"];

$j = array('SleepTimeSpanStart' => $SleepTimeSpanStart,
'SleepTimeSpanEnd' => $SleepTimeSpanEnd,
'SleepTimeSpanEnable' => $SleepTimeSpanEnable,
'Power' => $Power,
'Temperature' => $Temperature,
'Length' => $Length,
'Language' => $Language,
'URL_Admin' =>$URL_Admin,
'URL_Grafana' =>$URL_Grafana,
'HTTPPort' =>$HTTPPort,
'ZoomLevel' =>$ZoomLevel,
'ScanMyTesla' => $ScanMyTesla,
'update' => $update,
'Range' => $Range
);

file_put_contents('/etc/teslalogger/settings.json', json_encode($j));

if ($_POST["ShareData"] == "true")
    setShareData(true);
else
    setShareData(false);


// chmod('/etc/teslalogger/settings.json', 666);

?>
