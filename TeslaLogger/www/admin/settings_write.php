<?PHP
$SleepTimeSpanStart = $_POST["SleepTimeSpanStart"];
echo($SleepTimeSpanStart);

$SleepTimeSpanEnd = $_POST["SleepTimeSpanEnd"];
echo($SleepTimeSpanEnd);

$SleepTimeSpanEnable = $_POST["SleepTimeSpanEnable"];
echo($SleepTimeSpanEnable);

$j = array('SleepTimeSpanStart' => $SleepTimeSpanStart,
'SleepTimeSpanEnd' => $SleepTimeSpanEnd,
'SleepTimeSpanEnable' => $SleepTimeSpanEnable);

file_put_contents('/etc/teslalogger/settings.json', json_encode($j));

// chmod('/etc/teslalogger/settings.json', 666);

?>