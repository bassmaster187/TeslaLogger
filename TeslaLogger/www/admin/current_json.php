<?PHP
  header('Content-Type: application/json');
  session_start();

  $current_carid = $_SESSION["carid"];
    if (!isset($current_carid))
        $current_carid = 1;

  echo(file_get_contents("/etc/teslalogger/current_json_$current_carid.txt"));
?>