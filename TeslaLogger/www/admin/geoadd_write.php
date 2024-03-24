<?PHP
require_once("tools.php");
$Text = $_POST["Text"];
$lat = $_POST["lat"];
$lng = $_POST["lng"];
$radius = $_POST["radius"];
$flag = $_POST["flag"];
$id = $_POST["id"];
$delete= $_POST["delete"];

$Text = str_replace(","," ",$Text);
$radius = str_replace(",","",$radius);
$flag = str_replace(",","",$flag);

$filename = '/tmp/geofence-private.csv';

$csvtext = "";
$i = 0;
$fp = null;

// Copy all entries before edited item
if (isset($id) && strlen($id) > 0)
{
        $date = date("ymdhis");
        copy($filename, "/tmp/geofence-private-$date.csv");

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
if(isset($delete))
{
        $tmp = "";
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

        $url = GetTeslaloggerURL("writefile/geofence-private.csv");
        echo file_get_contents($url, false, stream_context_create([
        'http' => [
                'method' => 'POST',
                'user_agent' => 'PHP',
                'header'  => "Content-type: application/x-www-form-urlencoded\r\nContent-Length: ".strlen($csvtext)."\r\n",
                'content' => $csvtext
        ]    
        ]));
}
else
{
        file_put_contents($filename, $tmp, FILE_APPEND );

        $csvtext = file_get_contents($filename);

        $url = GetTeslaloggerURL("writefile/geofence-private.csv");
        echo file_get_contents($url, false, stream_context_create([
                'http' => [
                        'method' => 'POST',
                        'user_agent' => 'PHP',
                        'header'  => "Content-type: application/x-www-form-urlencoded\r\nContent-Length: ".strlen($csvtext)."\r\n",
                        'content' => $csvtext
                ]    
                ]));
}

// chmod('/etc/teslalogger/settings.json', 666);

?>
