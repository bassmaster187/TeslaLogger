<?PHP
require_once("tools.php");
$allcars = file_get_contents(GetTeslaloggerURL("getallcars"),0, stream_context_create(["http"=>["timeout"=>2]]));
if ($allcars === "not found!" && strpos($_SERVER['REQUEST_URI'],"password.php") === false)
{
    header("Location: password.php?id=-1");
    exit();
}
else if(startsWith($allcars,"WAITFORMFA:") && strpos($_SERVER['REQUEST_URI'],"password_info.php") === false)
{
    $p = strpos($allcars, ":");
    $id = substr($allcars, $p+1);
    header("Location: password_info.php?id=".$id);
    exit();
}
?>