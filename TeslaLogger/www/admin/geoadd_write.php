<?PHP
$Text = $_POST["Text"];
$lat = $_POST["lat"];
$lng = $_POST["lng"];
$Text = str_replace(","," ",$Text);
$tmp = "\r\n".$Text.",".$lat.",".$lng;

file_put_contents('/etc/teslalogger/geofence-private.csv', $tmp, FILE_APPEND );

// chmod('/etc/teslalogger/settings.json', 666);

?>