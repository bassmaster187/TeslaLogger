<?PHP
require_once("tools.php");

$JSON = $_REQUEST["JSON"];

$port = GetTeslaloggerHTTPPort();

$url = "http://localhost:$port/setcost";
if (isDocker())
    $url = "http://teslalogger:$port/setcost";

echo file_get_contents($url, false, stream_context_create([
    'http' => [
        'method' => 'POST',
        'user_agent' => 'PHP',
        'header'  => "Content-type: application/x-www-form-urlencoded\r\nContent-Length: ".strlen($JSON)."\r\n",
        'content' => $JSON
    ]    
]));
?>
