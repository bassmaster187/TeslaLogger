<?PHP
$SleepTimeSpanStart = $_POST["SleepTimeSpanStart"];
echo($SleepTimeSpanStart);

$SleepTimeSpanEnd = $_POST["SleepTimeSpanEnd"];
echo($SleepTimeSpanEnd);

$SleepTimeSpanEnable = $_POST["SleepTimeSpanEnable"];
echo($SleepTimeSpanEnable);

$SleepTimeSpanEnable = $_POST["SleepTimeSpanEnable"];
echo($SleepTimeSpanEnable);

$Language = $_POST["Language"];
$Power = $_POST["Power"];
$Temperature = $_POST["Temperature"];
$Length = $_POST["Length"];

$j = array('SleepTimeSpanStart' => $SleepTimeSpanStart,
'SleepTimeSpanEnd' => $SleepTimeSpanEnd,
'SleepTimeSpanEnable' => $SleepTimeSpanEnable,
'Power' => $Power,
'Temperature' => $Temperature,
'Length' => $Length,
'Language' => $Language
);

file_put_contents('/etc/teslalogger/settings.json', json_encode($j));

// chmod('/etc/teslalogger/settings.json', 666);

?>