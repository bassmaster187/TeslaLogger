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
<<<<<<< HEAD
$URL_Admin = $_POST["URL_Admin"];
=======
>>>>>>> c57eb95ff09f78417f0d5d5e6e2f8bbafec29531

$j = array('SleepTimeSpanStart' => $SleepTimeSpanStart,
'SleepTimeSpanEnd' => $SleepTimeSpanEnd,
'SleepTimeSpanEnable' => $SleepTimeSpanEnable,
'Power' => $Power,
'Temperature' => $Temperature,
'Length' => $Length,
<<<<<<< HEAD
'Language' => $Language,
'URL_Admin' =>$URL_Admin
=======
'Language' => $Language
>>>>>>> c57eb95ff09f78417f0d5d5e6e2f8bbafec29531
);

file_put_contents('/etc/teslalogger/settings.json', json_encode($j));

// chmod('/etc/teslalogger/settings.json', 666);

?>