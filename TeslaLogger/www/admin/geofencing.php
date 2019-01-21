<?PHP
	echo ("<h2>Private Geofence File:</h2><br>");
	
	$output  = file_get_contents('/etc/teslalogger/geofence-private.csv');
	if ($output === false) {
        // Handle the error
		echo "error";
    }
	else
		echo nl2br($output);
		
	echo ("<br><br><h2>Charger Geofence File:</h2><br>");	
	$output  = file_get_contents('/etc/teslalogger/geofence.csv');
	if ($output === false) {
        // Handle the error
		echo "error";
    }
	else
		echo nl2br($output);
		
	
?>