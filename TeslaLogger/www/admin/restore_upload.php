<?php
require("language.php");
require_once("tools.php");

// The restore uncompresses more than a gigabyte into /tmp and the mysql import runs
// for minutes: neither the PHP time limit nor a closed browser tab may stop it.
@set_time_limit(0);
ignore_user_abort(true);

// Without this Apache buffers the whole page and the "restore is running" message
// would only appear when the restore is already finished.
@ini_set('zlib.output_compression', 'Off');
if (function_exists('apache_setenv'))
    @apache_setenv('no-gzip', '1');

// logger() posts the text to the Teslalogger service and echoes its answer ("ok"),
// which would be printed on the page. Throw the answer away.
function restore_logger($text)
{
    ob_start();
    logger($text);
    ob_end_clean();
}

function restore_flush()
{
    while (ob_get_level() > 0)
        ob_end_flush();
    flush();
}

function format_size($bytes)
{
    if (!is_numeric($bytes))
        return "";

    $units = array("B", "KB", "MB", "GB", "TB");
    $i = 0;
    while ($bytes >= 1024 && $i < count($units) - 1)
    {
        $bytes /= 1024;
        $i++;
    }

    return round($bytes, 1) . " " . $units[$i];
}

// A translatable hint for the errors that appear over and over. The raw output of
// the mysql command is shown below it, so the hint only has to say what to do.
function restore_hint($output)
{
    $text = implode(" ", $output);

    if (strpos($text, "ERROR 2026") !== false || stripos($text, "TLS/SSL") !== false)
        return get_text("The database server does not support the encrypted connection that the database client requires. Please update the Teslalogger Docker images.");

    if (strpos($text, "ERROR 1045") !== false)
        return get_text("The database rejected the login (wrong user or password).");

    if (strpos($text, "ERROR 2002") !== false || strpos($text, "ERROR 2003") !== false)
        return get_text("The database server is not reachable. Please check whether the database container is running.");

    if (strpos($text, "ERROR 1064") !== false)
        return get_text("The backup contains SQL statements that this database does not accept. It may come from a newer Teslalogger version.");

    if (strpos($text, "ERROR 1114") !== false || stripos($text, "No space left on device") !== false)
        return get_text("The disk of the database is full.");

    return "";
}

$status = "";                // "" | "ok" | "error"
$message = "";               // result shown in the alert
$hint = "";                  // what the user can do now
$details = array();          // label => value
$mysql_output = array();
$is_geofence = false;
$originalfilename = "";
$duration = null;
$out_file_name = "/tmp/mybackup.sql";
$file_name = "";

// Opened without the form (bookmark, refresh of an old result): show the form again.
if (!isset($_POST["submit"]))
{
    header("Location: restore.php");
    exit();
}

restore_logger("restore_upload.php!");

if (!isset($_FILES["fileToUpload"]))
{
    // PHP drops the whole POST body, including $_FILES, when it is larger than
    // post_max_size - the usual sign of a backup file that is too large.
    if (isset($_SERVER["CONTENT_LENGTH"]) && intval($_SERVER["CONTENT_LENGTH"]) > 0)
    {
        $status = "error";
        $message = get_text("The uploaded file is larger than the maximum allowed upload size.");
        $hint = get_text("The limit is set in php.ini (upload_max_filesize and post_max_size).");
        restore_logger("Restore error: POST body larger than post_max_size");
    }
    else
    {
        $status = "error";
        $message = get_text("No file was uploaded.");
        restore_logger("Restore error: No fileToUpload in \$_FILES");
    }
}
else if ($_FILES["fileToUpload"]["error"] !== UPLOAD_ERR_OK)
{
    $upload_errors = [
        UPLOAD_ERR_INI_SIZE => "upload exceeds the limit of php.ini",
        UPLOAD_ERR_FORM_SIZE => "upload exceeds the limit of the form",
        UPLOAD_ERR_PARTIAL => "the file was only uploaded partially",
        UPLOAD_ERR_NO_FILE => "no file was uploaded",
        UPLOAD_ERR_NO_TMP_DIR => "the temporary folder of PHP is missing",
        UPLOAD_ERR_CANT_WRITE => "PHP could not write the file to disk",
        UPLOAD_ERR_EXTENSION => "a PHP extension stopped the upload"
    ];

    $err_msg = $upload_errors[$_FILES["fileToUpload"]["error"]] ?? "unknown error (code: " . $_FILES["fileToUpload"]["error"] . ")";
    $status = "error";
    $message = get_text("The file upload failed.");
    $hint = $err_msg;
    restore_logger("Restore error: File upload failed - $err_msg");
}
else
{
    $originalfilename = $_FILES["fileToUpload"]["name"];
    $file_name = $_FILES["fileToUpload"]["tmp_name"];

    restore_logger("Restore: Uploading file: $originalfilename (" . filesize($file_name) . " bytes)");
    $details[get_text("File")] = $originalfilename;
    $details[get_text("Compressed size")] = format_size(filesize($file_name));

    if (!rename($file_name, $file_name . ".gz"))
    {
        $status = "error";
        $message = get_text("The uploaded file could not be read.");
        restore_logger("Restore error: Failed to rename temp file to .gz");
    }
    else
    {
        $file_name = $file_name . ".gz";

        // The dump is uncompressed into /tmp, the volume the containers share.
        // A mysqldump is roughly a tenth of its uncompressed size, so the free
        // space has to be checked before the file is written.
        $free = disk_free_space("/tmp");
        $needed = filesize($file_name) * 12;
        if ($free !== false && $free < $needed)
        {
            $status = "error";
            $message = get_text("Not enough free disk space in /tmp.");
            $details[get_text("Free disk space in /tmp")] = format_size($free);
            restore_logger("Restore error: not enough free space in /tmp, free " . format_size($free) . ", needed about " . format_size($needed));
        }
        else
        {
            $gz = gzopen($file_name, 'rb');
            if (!$gz)
            {
                $status = "error";
                $message = get_text("The file could not be read as a gzip backup. Please use a mysqldump file from the backup folder.");
                restore_logger("Restore error: Failed to open gz file: $file_name");
            }
            else
            {
                $out_file = fopen($out_file_name, 'wb');
                if (!$out_file)
                {
                    $status = "error";
                    $message = get_text("The temporary file /tmp/mybackup.sql could not be written.");
                    restore_logger("Restore error: Failed to create output file: $out_file_name");
                }
                else
                {
                    // Raising this value may increase performance
                    $buffer_size = 4096; // read 4kb at a time

                    while (!gzeof($gz))
                        // Both fwrite and gzread are binary-safe
                        fwrite($out_file, gzread($gz, $buffer_size));

                    fclose($out_file);
                    gzclose($gz);

                    $decompressed = filesize($out_file_name);
                    $details[get_text("Decompressed size")] = format_size($decompressed);
                    restore_logger("Decompression complete, output: $out_file_name ($decompressed bytes)");
                    restore_logger("Filesize decompressed: " . $decompressed);
                }
            }
        }
    }
}

// Geofence backup files are created by backup.sh with a retention-class prefix:
//   DAY-geofence-private<date>.gz   (daily)
//   MON-geofence-private<date>.gz   (monthly, 1st of the month)
//   yeargeofence-<year>.gz          (yearly, Jan 1st)
//   geofence-private<date>.gz       (legacy, no prefix)
// Detect them regardless of the prefix (but never the project-managed geofence.csv).
if ($status !== "error")
{
    $lowerName = strtolower($originalfilename);
    $is_geofence = (strpos($lowerName, "geofence-private") !== false) || (strpos($lowerName, "yeargeofence-") === 0);
}

// A wrong file (a photo, a csv) would be fed into mysql and fail with a confusing
// SQL error. Look at the first bytes instead.
if ($status !== "error" && !$is_geofence)
{
    $head = file_get_contents($out_file_name, false, null, 0, 64000);
    $is_dump = $head !== false && (stripos($head, "CREATE TABLE") !== false || stripos($head, "INSERT INTO") !== false
        || stripos($head, "MariaDB dump") !== false || stripos($head, "MySQL dump") !== false);
    if (!$is_dump)
    {
        $status = "error";
        $message = get_text("The selected file does not look like a Teslalogger database backup.");
        restore_logger("Restore error: file does not look like a mysqldump, head: " . substr($head === false ? "" : $head, 0, 120));
    }
}

// Page shell, same head and menu as restore.php. The result is appended below once
// the work is done, so a failed upload and a failed import look the same.
?>
<!DOCTYPE html>
<html lang="<?php echo $json_data["Language"]; ?>">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title><?php t("Restore"); ?></title>
	<link rel="stylesheet" href="static/jquery/ui/1.12.1/themes/smoothness/jquery-ui.css">
	<link rel="stylesheet" href="static/teslalogger_style.css?v=5">
	<link rel="stylesheet" href="static/teslalogger_ui.css?v=30">
	<script src="static/jquery/jquery-1.12.4.js"></script>
	<script src="static/jquery/ui/1.12.1/jquery-ui.js"></script>
  </head>
<body>
<?php
include "menu.php";
menu("Restore");
?>
<div class="tl-page">
	<div class="tl-page-head">
		<h2 class="tl-page-title"><?php t("Restore"); ?></h2>
	</div>
<?php
if ($status !== "error" && !$is_geofence)
{
    // The import runs for minutes. Show it and count the seconds, then send
    // everything out before mysql is started.
    echo("	<div class=\"tl-alert tl-alert-info\" id=\"restore_running\">" . htmlspecialchars(get_text("Restore running")) . " <span id=\"restore_elapsed\">0 s</span></div>\n");
    echo("	<script>\n");
    echo("	var restore_started = Date.now();\n");
    echo("	setInterval(function () {\n");
    echo("		var el = document.getElementById(\"restore_elapsed\");\n");
    echo("		if (el != null)\n");
    echo("			el.textContent = Math.round((Date.now() - restore_started) / 1000) + \" s\";\n");
    echo("	}, 1000);\n");
    echo("	</script>\n");
    restore_flush();
}

if ($status !== "error")
{
    if ($is_geofence)
    {
        $csvtext = file_get_contents($out_file_name);
        if ($csvtext === false)
        {
            $status = "error";
            $message = get_text("The geofence file could not be read.");
            restore_logger("Restore error: Failed to read decompressed geofence file");
        }
        else
        {
            $url = GetTeslaloggerURL("writefile/geofence-private.csv");
            restore_logger("Restoring geofence-private.csv to: $url");
            $result = @file_get_contents($url, false, stream_context_create([
                'http' => [
                    'method' => 'POST',
                    'user_agent' => 'PHP',
                    'header'  => "Content-type: application/x-www-form-urlencoded\r\nContent-Length: ".strlen($csvtext)."\r\n",
                    'content' => $csvtext
                ]
            ]));
            restore_logger("Geofence restore result: " . var_export($result, true));

            if ($result === false || trim($result) !== "ok")
            {
                $status = "error";
                $message = get_text("The geofence file could not be written.");
                $hint = get_text("Teslalogger did not accept the file.");
            }
            else
            {
                $status = "ok";
                $message = get_text("The geofence file has been restored.");
            }
        }
    }
    else
    {
        $start = microtime(true);
        restore_logger("Start Restore");

        $return_var = NULL;

        if (isDocker())
        {
            restore_logger("Docker detected, using database host 'database'");
            $sed_result = exec("sed -i '/\\/\\*M!999999\\\\-/d' /tmp/mybackup.sql", $sed_output, $sed_return); // bug in mariaDB - removes special comment: enable sandbox mode
            restore_logger("Sed command return: $sed_return");

            restore_logger("start mysql restore");
            // --skip-ssl is required since the webserver image is based on trixie: mariadb-client 11.x
            // enables TLS (and server certificate verification) by default (MDEV-31857), but the
            // mariadb server of the stack runs without TLS, which aborts the connection with
            // "ERROR 2026 (HY000): TLS/SSL error: SSL is required, but the server does not support it".
            // 2>&1 is required because exec() only returns stdout and would hide the mysql errors.
            $command = exec("/usr/bin/mysql --skip-ssl -hdatabase -uroot -pteslalogger -Dteslalogger < /tmp/mybackup.sql 2>&1", $mysql_output, $return_var);
        }
        else
        {
            restore_logger("start mysql restore");
            $command = exec("/usr/bin/mysql -uroot -pteslalogger -Dteslalogger < /tmp/mybackup.sql 2>&1", $mysql_output, $return_var);
        }

        $duration = microtime(true) - $start;
        $details[get_text("Duration")] = round($duration) . " s";

        restore_logger("MySQL restore return var: $return_var");
        restore_logger("Output from mysql: " . var_export($mysql_output, true));

        if ($return_var !== 0)
        {
            $status = "error";
            $message = get_text("Restore failed");
            $hint = restore_hint($mysql_output);
            restore_logger("Restore FAILED: mysql return var: $return_var");
        }
        else
        {
            $status = "ok";
            $message = get_text("Restore completed successfully");
            $hint = get_text("Please restart Teslalogger after the restore.");
            restore_logger("Restore completed successfully");
        }
    }
}

if (file_exists($out_file_name))
    unlink($out_file_name);

if ($file_name !== "" && file_exists($file_name))
    unlink($file_name);

restore_logger("Restore: Cleanup complete");

// The "restore is running" banner has served its purpose.
if (!$is_geofence)
    echo("	<script>var running = document.getElementById(\"restore_running\"); if (running != null) running.remove();</script>\n");

echo("	<div class=\"tl-alert " . ($status === "ok" ? "tl-alert-success" : "tl-alert-danger") . "\">\n");
echo("		<ul class=\"tl-alert-list\">\n");
echo("			<li>" . htmlspecialchars($message) . "</li>\n");
if ($hint !== "")
    echo("			<li>" . htmlspecialchars($hint) . "</li>\n");
echo("		</ul>\n");
echo("	</div>\n");

if (count($details) > 0)
{
    echo("	<div class=\"tl-card\">\n");
    echo("		<div class=\"tl-card-title\">" . htmlspecialchars(get_text("Details")) . "</div>\n");
    echo("		<div class=\"tl-row-list\">\n");
    foreach ($details as $label => $value)
        echo("			<div class=\"tl-row-list-item\"><span>" . htmlspecialchars($label) . "</span><span>" . htmlspecialchars($value) . "</span></div>\n");
    echo("		</div>\n");
    echo("	</div>\n");
}

if (count($mysql_output) > 0)
{
    echo("	<div class=\"tl-card\">\n");
    echo("		<div class=\"tl-card-title\">" . htmlspecialchars(get_text("Database output")) . "</div>\n");
    echo("		<pre class=\"tl-restore-output\">" . htmlspecialchars(implode("\n", $mysql_output)) . "</pre>\n");
    echo("	</div>\n");
}

echo("	<div class=\"tl-restore-actions\">\n");
echo("		<a class=\"tl-muted\" href=\"restore.php\">" . htmlspecialchars(get_text("Back to restore")) . "</a>\n");

if ($status === "ok")
{
    // Same flow as the "Restart" entry of the menu: confirmation dialog, then
    // restartlogger.php, which writes /tmp/teslalogger-cmd-restart.txt in Docker.
    // The last parameter switches to the start page afterwards - reloading this
    // page would start the restore again.
    // json_encode keeps the translated texts valid inside the JavaScript call.
    echo("		<script>\n");
    echo("			function restart_teslalogger() {\n");
    echo("				confirmAction(" . json_encode(get_text("Restart confirmation")) . ", "
        . json_encode(get_text("Do you really want to restart TeslaLogger?")) . ", 'restartlogger.php', "
        . json_encode(get_text("Teslalogger is restarting.")) . ", false, 'index.php');\n");
    echo("			}\n");
    echo("		</script>\n");
    echo("		<button type=\"button\" onclick=\"restart_teslalogger()\">" . htmlspecialchars(get_text("Restart TeslaLogger")) . "</button>\n");
}

echo("	</div>\n");
?>
</div>
</body>
</html>
