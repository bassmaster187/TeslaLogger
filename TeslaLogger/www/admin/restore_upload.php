<!DOCTYPE html>
<?php
require("language.php");
?>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Teslalogger Restore</title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=4">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
	<link rel="stylesheet" href="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.css">
	<script src="//cdnjs.cloudflare.com/ajax/libs/timepicker/1.3.5/jquery.timepicker.min.js"></script>
	<script>
	
	$( function() {
		// $( "restorebutton" ).button();
	
	});
    

</script>
<?php

logger("restore_upload.php!");

$target_dir = "uploads/";
$uploadOk = 1;

if(isset($_POST["submit"])) {
    if (!isset($_FILES["fileToUpload"])) {
        logger("Restore error: No fileToUpload in \$_FILES");
        echo("Error: No file uploaded.<br>");
    } elseif ($_FILES["fileToUpload"]["error"] !== UPLOAD_ERR_OK) {
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
        logger("Restore error: File upload failed - $err_msg");
        echo("Error: File upload failed - $err_msg<br>");
    } else {
        $originalfilename = $_FILES["fileToUpload"]["name"];
        $target_file = $target_dir . basename($_FILES["fileToUpload"]["name"]);
        $imageFileType = strtolower(pathinfo($target_file,PATHINFO_EXTENSION));
        $file_name = $_FILES["fileToUpload"]["tmp_name"];

        logger("Restore: Uploading file: $originalfilename (" . filesize($file_name) . " bytes)");
        echo("filename:" . $file_name ." <br>Size compressed:". filesize($file_name));
        echo("<br>Original filename:" . $originalfilename);
        logger("Filesize compressed: ". filesize($file_name));

        if (!rename($file_name, $file_name.".gz")) {
            logger("Restore error: Failed to rename temp file to .gz");
            echo("Error: Failed to process uploaded file.<br>");
            exit;
        }

        $file_name = $file_name.".gz";

        // Raising this value may increase performance
        $buffer_size = 4096; // read 4kb at a time
        $out_file_name = "/tmp/mybackup.sql";

        // Open our files (in binary mode)
        $file = gzopen($file_name, 'rb');
        if (!$file) {
            logger("Restore error: Failed to open gz file: $file_name");
            echo("Error: Failed to open compressed file.<br>");
            exit;
        }
        $out_file = fopen($out_file_name, 'wb');
        if (!$out_file) {
            logger("Restore error: Failed to create output file: $out_file_name");
            echo("Error: Failed to create output file.<br>");
            exit;
        }

        // Keep repeating until the end of the input file
        while(!gzeof($file)) {
            // Read buffer-size bytes
            // Both fwrite and gzread and binary-safe
            fwrite($out_file, gzread($file, $buffer_size));
        }

        // Files are done, close files
        fclose($out_file);
        gzclose($file);

        logger("Decompression complete, output: $out_file_name (" . filesize($out_file_name) . " bytes)");

        if (strpos($originalfilename, "geofence-private") === 0)
        {
            echo("<br>Geofence-Private CSV file detected.<br>");
            $csvtext = file_get_contents($out_file_name);
            if ($csvtext === false) {
                logger("Restore error: Failed to read decompressed geofence file");
                echo("Error: Failed to read decompressed file.<br>");
                exit;
            }
            $url = GetTeslaloggerURL("writefile/geofence-private.csv");
            logger("Restoring geofence-private.csv to: $url");
            $result = file_get_contents($url, false, stream_context_create([
            'http' => [
                    'method' => 'POST',
                    'user_agent' => 'PHP',
                    'header'  => "Content-type: application/x-www-form-urlencoded\r\nContent-Length: ".strlen($csvtext)."\r\n",
                    'content' => $csvtext
            ]    
            ]));
            logger("Geofence restore result: " . var_export($result, true));
        }
        else
        {
            echo("<br>Decompressed file: <br>");
            echo("<br>filename:" . $out_file_name ." Size:". filesize($out_file_name));

            logger("Filesize decompressed: ". filesize($out_file_name));

            logger("Start Restore");
            echo("<br>Start Restore:<br>");
            $return_var = NULL;
            $output = NULL;

            if (file_exists("/tmp/teslalogger-DOCKER"))
            {
                echo("<br>Docker detected, using database host 'database'");
                logger("Docker detected, using database host 'database'");
                $sed_result = exec("sed -i '/\\/\\*M!999999\\\\-/d' /tmp/mybackup.sql", $sed_output, $sed_return); // bug in mariaDB - removes special comment: enable sandbox mode
                logger("Sed command return: $sed_return");

                logger("start mysql restore");
                $command = exec("/usr/bin/mysql -hdatabase -uroot -pteslalogger -Dteslalogger < /tmp/mybackup.sql", $output, $return_var);
                logger("MySQL restore return var: $return_var");
            }
            else {
                $command = exec("/usr/bin/mysql -uroot -pteslalogger -Dteslalogger < /tmp/mybackup.sql", $output, $return_var);
                logger("MySQL restore return var: $return_var");
            }

            logger("Output from mysql: " . var_export($output));
            echo "<br>Output from mysql: <br>";
            foreach ($output as $line) {
                echo htmlspecialchars($line) . "<br>";
            }
            echo("<br>Restore finished. Please Reboot!");
            logger("Restore completed successfully");
        }

        if (file_exists($out_file_name))
            unlink($out_file_name);

        if (file_exists($file_name))
            unlink($file_name);

        logger("Restore: Cleanup complete");
    }
} else {
    logger("Restore: No submit received");
}
?>
</div>
