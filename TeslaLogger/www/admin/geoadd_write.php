<?PHP
$Text = $_POST["Text"];
$lat = $_POST["lat"];
$lng = $_POST["lng"];
$radius = $_POST["radius"];
$Text = str_replace(","," ",$Text);
$radius = str_replace(",","",$radius);

$tmp = "\r\n".$Text.",".$lat.",".$lng.",".$radius;

file_put_contents('/etc/teslalogger/geofence-private.csv', $tmp, FILE_APPEND );

// chmod('/etc/teslalogger/settings.json', 666);

?>