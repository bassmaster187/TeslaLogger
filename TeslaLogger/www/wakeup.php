<?PHP
	$filename = "/etc/teslalogger/wakeupteslalogger.txt";
	
	$ret =	file_put_contents ($filename,".");
	chmod ($filename, 0666);
	
	$logfile = "/etc/teslalogger/nohup.out";
	$time = date("d.m.Y H:i:s", time());
	$ret2 =	file_put_contents($logfile, $time . " : Wakeup file created!\r\n", FILE_APPEND);
		
	echo "ok " . $ret . " / " . $ret2. " / " . $time;
?>