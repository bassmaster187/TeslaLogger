<?PHP
	$output  = shell_exec('tail -n100 /etc/teslalogger/nohup.out');
	if ($output === false) {
        // Handle the error
		echo "error";
    }
	
	echo nl2br($output);
?>