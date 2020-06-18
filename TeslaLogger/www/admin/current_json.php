<?PHP
  header('Content-Type: application/json');
  echo(file_get_contents("/etc/teslalogger/current_json.txt"));
?>