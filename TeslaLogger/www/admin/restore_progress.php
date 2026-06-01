<?php
require("language.php");

header('Content-Type: application/json');

$session_id = isset($_GET['session_id']) ? $_GET['session_id'] : '';

if (empty($session_id)) {
    echo json_encode(["error" => "No session_id provided"]);
    exit;
}

$progress_file = "/tmp/{$session_id}_progress.json";
$pid_file = "/tmp/{$session_id}_pid.txt";
$log_file = "/tmp/{$session_id}_log.txt";
$script_log = "/tmp/{$session_id}_script_out.txt";

// Check if restore process is still running
$process_running = false;
if (file_exists($pid_file)) {
    $pid = trim(file_get_contents($pid_file));
    if (!empty($pid) && is_numeric($pid)) {
        if (function_exists('posix_kill')) {
            $process_running = posix_kill((int)$pid, 0);
        } else {
            $check = shell_exec("kill -0 $pid 2>/dev/null");
            $process_running = ($check === '');
        }
    }
}

// Read progress file
if (file_exists($progress_file)) {
    $progress_data = json_decode(file_get_contents($progress_file), true);
    
    // If status is still "restoring" and process is not running, mark as completed
    if ($progress_data['status'] === 'restoring' && !$process_running) {
        $progress_data['status'] = 'completed';
        $progress_data['progress'] = 100;
        $progress_data['message'] = 'Restore completed successfully!';
        file_put_contents($progress_file, json_encode($progress_data));
    }
    
    // If status is "decompressing" and decompressed file exists, mark as ready
    if ($progress_data['status'] === 'decompressing') {
        $sql_file = "/tmp/{$session_id}.sql";
        if (file_exists($sql_file)) {
            $progress_data['status'] = 'ready';
            $progress_data['progress'] = 10;
            $progress_data['message'] = 'Decompression complete. Starting database restore...';
            file_put_contents($progress_file, json_encode($progress_data));
        }
    }
    
    // Append error log if available
    if (file_exists($log_file)) {
        $error_log = file_get_contents($log_file);
        $progress_data['error_log'] = $error_log;
    }
    if (file_exists($script_log)) {
        $script_log_content = file_get_contents($script_log);
        $progress_data['script_log'] = $script_log_content;
    }
    
    echo json_encode($progress_data);
} else {
    echo json_encode([
        "status" => "not_found",
        "progress" => 0,
        "message" => "No restore session found."
    ]);
}
?>
