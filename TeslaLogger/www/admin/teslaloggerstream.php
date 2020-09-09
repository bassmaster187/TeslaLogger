<?PHP
require("tools.php");

$data = $_REQUEST["data"];
$URL = $_REQUEST["url"];

$fullurl = 'http://localhost:5000/'.$URL;
if (isDocker())
    $fullurl = 'http://teslalogger:5000/'.$URL;

// $fullurl = 'http://host.docker.internal:5000/'.$URL;

echo file_get_contents($fullurl, false, stream_context_create([
    'http' => [
        'method' => 'POST',
        'user_agent' => 'PHP',
        'header'  => "Content-type: application/x-www-form-urlencoded\r\nContent-Length: ".strlen($data)."\r\n",
        'content' => $data
    ]    
]));
?>
