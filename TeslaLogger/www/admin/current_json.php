<?php
  header('Content-Type: application/json');
  session_start();

  require_once("tools.php");

  $current_carid = isset($_SESSION["carid"]) ? $_SESSION["carid"] : null;
  if (!$current_carid && isset($_REQUEST["carid"])) {
      $current_carid = $_REQUEST["carid"];
  }

  if (!$current_carid) {
      error_log("current_json.php: carid is not set");
      http_response_code(400); // Bad Request
      echo json_encode(["error" => "carid is not set"]);
      exit;
  }

  error_log("current_json.php: carid=$current_carid");
  echo file_get_contents(
      GetTeslaloggerURL("currentjson/" . $current_carid),
      false,
      stream_context_create(["http" => ["timeout" => 2]])
  );
?>