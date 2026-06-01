<?php
// Disable timeout for large file uploads
set_time_limit(0);

// Custom logger that doesn't block on HTTP
function quick_logger($t) {
    $logfile = "/etc/teslalogger/nohup.out";
    if (file_exists("/tmp/teslalogger-DOCKER")) {
        file_put_contents("/tmp/restore_debug.log", date("d.m.Y H:i:s") . " - " . $t . "\n", FILE_APPEND);
        return;
    }
    file_put_contents($logfile, date("d.m.Y H:i:s") . " : RESTORE - ". $t ."\r\n", FILE_APPEND);
}

quick_logger("restore_upload.php!");

header('Content-Type: application/json');
header('Connection: close');
header('Content-Encoding: none');
header('X-Accel-Buffering: no');

$session_id = "";

if (isset($_FILES["fileToUpload"])) {
    if ($_FILES["fileToUpload"]["error"] !== UPLOAD_ERR_OK) {
        $upload_errors = [
            UPLOAD_ERR_INI_SIZE => "Upload exceeds server limit (php.ini)",
            UPLOAD_ERR_FORM_SIZE => "Upload exceeds form limit",
            UPLOAD_ERR_PARTIAL => "File was only partially uploaded",
            UPLOAD_ERR_NO_FILE => "No file was uploaded",
            UPLOAD_ERR_NO_TMP_DIR => "Missing temporary folder",
            UPLOAD_ERR_CANT_WRITE => "Failed to write file to disk",
            UPLOAD_ERR_EXTENSION => "A PHP extension stopped the upload"
        ];
        $err_msg = $upload_errors[$_FILES["fileToUpload"]["error"]] ?? "Unknown error (code: {$_FILES['fileToUpload']['error']})";
        quick_logger("Restore error: File upload failed - $err_msg");
        echo json_encode(["error" => "File upload failed - $err_msg"]);
        exit;
    } else {
        $originalfilename = $_FILES["fileToUpload"]["name"];
        $file_name = $_FILES["fileToUpload"]["tmp_name"];

        quick_logger("Restore: Uploading file: $originalfilename (" . filesize($file_name) . " bytes)");
        quick_logger("Restore: tmp_name: $file_name");

        if (!file_exists($file_name)) {
            quick_logger("Restore error: Temp file does not exist: $file_name");
            echo json_encode(["error" => "Temp file does not exist."]);
            exit;
        }

        if (!rename($file_name, $file_name.".gz")) {
            quick_logger("Restore error: Failed to rename temp file to .gz");
            echo json_encode(["error" => "Failed to process uploaded file."]);
            exit;
        }

        $file_name = $file_name.".gz";

        // Generate unique session ID
        $session_id = "restore_" . uniqid() . "_" . time();
        $progress_file = "/tmp/{$session_id}_progress.json";
        $pid_file = "/tmp/{$session_id}_pid.txt";

        // Initialize progress file
        file_put_contents($progress_file, json_encode([
            "status" => "decompressing",
            "progress" => 0,
            "message" => "Decompressing backup file..."
        ]));

        $out_file_name = "/tmp/{$session_id}.sql";

        // Determine MySQL command
        if (file_exists("/tmp/teslalogger-DOCKER")) {
            $mysql_cmd = "/usr/bin/mysql -hdatabase -uroot -pteslalogger -Dteslalogger";
        } else {
            $mysql_cmd = "/usr/bin/mysql -uroot -pteslalogger -Dteslalogger";
        }

        // Write the full restore script that does everything in background
        $full_script = "/tmp/{$session_id}_full.sh";
        
        // Use single quotes for PHP variables to prevent shell interpolation
        // Use $'...' syntax or escaped dollar signs for shell variables
        $script_lines = [];
        $script_lines[] = '#!/bin/bash';
        $script_lines[] = '';
        $script_lines[] = 'LOGFILE="/tmp/' . $session_id . '_log.txt"';
        $script_lines[] = 'PROGRESS_FILE="/tmp/' . $session_id . '_progress.json"';
        $script_lines[] = 'SQL_FILE="' . $out_file_name . '"';
        $script_lines[] = 'GZ_FILE="' . $file_name . '"';
        $script_lines[] = '';
        $script_lines[] = '# Start decompression';
        $script_lines[] = 'echo "Starting decompression..." >> "$LOGFILE"';
        $script_lines[] = 'gunzip -c "$GZ_FILE" > "$SQL_FILE" 2>> "$LOGFILE"';
        $script_lines[] = 'DECOMP_SIZE=$(wc -c < "$SQL_FILE")';
        $script_lines[] = 'echo "Decompression complete. Size: $DECOMP_SIZE" >> "$LOGFILE"';
        $script_lines[] = 'echo "{\"status\":\"ready\",\"progress\":10,\"message\":\"Decompression complete. File size: $DECOMP_SIZE bytes. Starting database restore...\"}" > "$PROGRESS_FILE"';
        $script_lines[] = '';
        $script_lines[] = '# Remove problematic MariaDB comments';
        $script_lines[] = 'grep -v "/\\*M!999999" "$SQL_FILE" > "$SQL_FILE.tmp" && mv "$SQL_FILE.tmp" "$SQL_FILE"';
        $script_lines[] = '';
        $script_lines[] = '# Run MySQL restore';
        $script_lines[] = 'echo "Starting MySQL restore..." >> "$LOGFILE"';
        $script_lines[] = $mysql_cmd . ' < "$SQL_FILE" 2>> "$LOGFILE"';
        $script_lines[] = 'RESULT=$?';
        $script_lines[] = 'echo "MySQL restore finished with result: $RESULT" >> "$LOGFILE"';
        $script_lines[] = '';
        $script_lines[] = 'if [ $RESULT -eq 0 ]; then';
        $script_lines[] = '  echo "{\"status\":\"completed\",\"progress\":100,\"message\":\"Restore completed successfully! Please reboot.\"}" > "$PROGRESS_FILE"';
        $script_lines[] = 'else';
        $script_lines[] = '  echo "{\"status\":\"error\",\"progress\":0,\"message\":\"Restore failed with exit code: $RESULT\"}" > "$PROGRESS_FILE"';
        $script_lines[] = 'fi';
        
        $full_content = implode("\n", $script_lines) . "\n";
        
        file_put_contents($full_script, $full_content);
        chmod($full_script, 0755);

        quick_logger("Restore: Script content:\n" . $full_content);

        // Start the full restore script in background
        exec("nohup bash $full_script > /tmp/{$session_id}_script_out.txt 2>&1 & echo \$!", $output, $return_var);
        $background_pid = array_pop($output);
        quick_logger("Restore: Background process PID: $background_pid, return var: $return_var");

        // Clean up uploaded file immediately
        if (file_exists($file_name))
            unlink($file_name);

        quick_logger("Restore: About to send response");
        echo json_encode([
            "session_id" => $session_id,
            "status" => "started",
            "message" => "Restore started in background."
        ]);
        quick_logger("Restore: Response sent");
        exit;
    }
}
?>
