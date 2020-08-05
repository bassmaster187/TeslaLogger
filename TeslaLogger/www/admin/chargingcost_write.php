<?PHP
require("tools.php");

$JSON = $_REQUEST["JSON"];
file_put_contents("/etc/teslalogger/SetCost.txt", $JSON);
echo($JSON);
$output = exec("/etc/teslalogger/TeslaLogger.exe setcost");
?>
