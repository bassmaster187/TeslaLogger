<?PHP
  header('Content-Type: application/json');
  session_start();

  require_once("tools.php");

  $current_carid = $_SESSION["carid"];
    if (!isset($current_carid))
        $current_carid = 1;

  // echo(file_get_contents("/etc/teslalogger/current_json_$current_carid.txt"));
  echo file_get_contents(GetTeslaloggerURL("currentjson/".$current_carid),0, stream_context_create(["http"=>["timeout"=>2]])); 
?>