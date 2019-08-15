<?PHP
	$output  = shell_exec('/etc/teslalogger/backup.sh');
	if ($output === false) {
        // Handle the error
		echo "error";
    }
	else
		echo ("OK");
?>