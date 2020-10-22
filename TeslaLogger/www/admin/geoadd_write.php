<?PHP
$Text = $_POST["Text"];
$lat = $_POST["lat"];
$lng = $_POST["lng"];
$radius = $_POST["radius"];
$flag = $_POST["flag"];
$id = $_POST["id"];

$Text = str_replace(","," ",$Text);
$radius = str_replace(",","",$radius);
$flag = str_replace(",","",$flag);

$filename = '/etc/teslalogger/geofence-private.csv';

$csvtext = "";
$i = 0;
$fp = null;

// Copy all entries before edited item
if (isset($id) && strlen($id) > 0)
{
        $date = date("ymdhis");
        copy($file, "/etc/teslalogger/geofence-private-$date.csv");

        $fp = fopen($filename, "r+");
        while ($line = fgets($fp)) {
                if ($i == $id)
                        break;

                $csvtext .= trim($line)."\r\n";
                $i++;
        }
}

if(strpos($flag,"+") !== false)
{
        $tmp = "\r\n".$Text.",".$lat.",".$lng.",".$radius.",".$flag;
}
else
{
        $tmp = "\r\n".$Text.",".$lat.",".$lng.",".$radius;
}

// Copy all entries after edited item
if (isset($id) && strlen($id) > 0)
{
        $csvtext .= trim($tmp)."\r\n";

        while ($line = fgets($fp)) {
                $csvtext .= trim($line)."\r\n";
        }
        fclose($fp);
        file_put_contents($filename, $csvtext);
}
else
        file_put_contents($filename, $tmp, FILE_APPEND );

// chmod('/etc/teslalogger/settings.json', 666);

?>
