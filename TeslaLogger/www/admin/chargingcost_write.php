<?PHP
require("tools.php");

$JSON = $_REQUEST["JSON"];

$url = 'http://localhost:5000/setcost';
if (isDocker())
    $url = 'http://teslalogger:5000/setcost';

echo file_get_contents($url, false, stream_context_create([
    'http' => [
        'method' => 'POST',
        'user_agent' => 'PHP',
        'header'  => "Content-type: application/x-www-form-urlencoded\r\nContent-Length: ".strlen($JSON)."\r\n",
        'content' => $JSON
    ]    
]));
?>
