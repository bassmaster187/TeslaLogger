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
$originalfilename = $_FILES["fileToUpload"]["name"];
$target_file = $target_dir . basename($_FILES["fileToUpload"]["name"]);
$uploadOk = 1;
$imageFileType = strtolower(pathinfo($target_file,PATHINFO_EXTENSION));
// Check if image file is a actual image or fake image
if(isset($_POST["submit"])) {
	$file_name = $_FILES["fileToUpload"]["tmp_name"];
    echo("filename:" . $file_name ." <br>Size compressed:". filesize($file_name));
	echo("<br>Original filename:" . $originalfilename);
	logger("Filesize compressed: ". filesize($file_name));
	
	rename($file_name, $file_name.".gz");
	
	$file_name = $file_name.".gz";

	// Raising this value may increase performance
	$buffer_size = 4096; // read 4kb at a time
	$out_file_name = "/tmp/mybackup.sql";

	// Open our files (in binary mode)
	$file = gzopen($file_name, 'rb');
	$out_file = fopen($out_file_name, 'wb');

	// Keep repeating until the end of the input file
	while(!gzeof($file)) {
		// Read buffer-size bytes
		// Both fwrite and gzread and binary-safe
		fwrite($out_file, gzread($file, $buffer_size));
	}

	// Files are done, close files
	fclose($out_file);
	gzclose($file);

	if (strpos($originalfilename, "geofence-private") === 0)
	{
		echo("<br>Geofence-Private CSV file detected.<br>");
		$csvtext = file_get_contents($out_file_name);
		$url = GetTeslaloggerURL("writefile/geofence-private.csv");
        echo file_get_contents($url, false, stream_context_create([
        'http' => [
                'method' => 'POST',
                'user_agent' => 'PHP',
                'header'  => "Content-type: application/x-www-form-urlencoded\r\nContent-Length: ".strlen($csvtext)."\r\n",
                'content' => $csvtext
        ]    
        ]));
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
			$command = exec("sed -i '/\\/\\*M!999999\\\\-/d' /tmp/mybackup.sql", $output, $return_var); // bug in mariaDB - removes special comment: enable sandbox mode

			logger("start mysql restore");
			$command = exec("/usr/bin/mysql -hdatabase -uroot -pteslalogger -Dteslalogger < /tmp/mybackup.sql", $output, $return_var);
		}
		else
			$command = exec("/usr/bin/mysql -uroot -pteslalogger -Dteslalogger < /tmp/mybackup.sql", $output, $return_var);

		logger("Output from mysql: " . var_export($output));
		echo "<br>Output from mysql: <br>";
		foreach ($output as $line) {
			echo htmlspecialchars($line) . "<br>";
		}
		echo("<br>Restore finished. Please Reboot!");	
	}
	
	if (file_exists($out_file_name))
		unlink($out_file_name);

	if (file_exists($file_name))
		unlink($file_name);
}
?>
</div>
