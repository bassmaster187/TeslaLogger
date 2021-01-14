<?PHP
require("tools.php");

$data = $_REQUEST["data"];
$URL = $_REQUEST["url"];

$port = GetTeslaloggerHTTPPort();

$fullurl = "http://localhost:$port/$URL";
if (isDocker())
    $fullurl = "http://teslalogger:$port/$URL";

// $fullurl = "http://host.docker.internal:$port/$URL";

echo file_get_contents($fullurl, false, stream_context_create([
    'http' => [
        'method' => 'POST',
        'user_agent' => 'PHP',
        'header'  => "Content-type: application/x-www-form-urlencoded\r\nContent-Length: ".strlen($data)."\r\n",
        'content' => $data
    ]    
]));
?>
