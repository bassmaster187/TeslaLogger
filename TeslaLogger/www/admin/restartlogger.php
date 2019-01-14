<?PHP
	$output  = shell_exec('sudo /sbin/reboot');
	if ($output === false) {
        // Handle the error
		echo "error"; 
    }
	else
		echo ("OK");
?>