<?PHP
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

$j = array('SleepTimeSpanStart' => $SleepTimeSpanStart,
'SleepTimeSpanEnd' => $SleepTimeSpanEnd,
'SleepTimeSpanEnable' => $SleepTimeSpanEnable,
'Power' => $Power,
'Temperature' => $Temperature,
'Length' => $Length,
'Language' => $Language,
'URL_Admin' =>$URL_Admin,
'ScanMyTesla' => $ScanMyTesla
);

file_put_contents('/etc/teslalogger/settings.json', json_encode($j));

// chmod('/etc/teslalogger/settings.json', 666);

?>