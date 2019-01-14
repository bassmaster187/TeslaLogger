<?PHP
	$output  = file_get_contents('/etc/teslalogger/geofence.csv');
	if ($output === false) {
        // Handle the error
		echo "error";
    }
	else
		echo nl2br($output);
?>