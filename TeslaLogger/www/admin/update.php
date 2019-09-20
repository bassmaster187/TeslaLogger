<?PHP
	unlink("/etc/teslalogger/cmd_updated.txt");
	
	$logfile = "/etc/teslalogger/nohup.out";
	$time = date("d.m.Y H:i:s", time());
	$ret2 =	file_put_contents($logfile, $time . " : UPDATE request!\r\n", FILE_APPEND);
	$ret2 =	file_put_contents($logfile, $time . " : --------------------------------------------\r\n\r\n", FILE_APPEND);
	
	sleep(2);

	shell_exec('pkill mono');
	
	$output  = shell_exec('sudo /sbin/reboot');
	if ($output === false) {
        // Handle the error
		echo "error"; 
    }
	else
		echo ("OK");
?>