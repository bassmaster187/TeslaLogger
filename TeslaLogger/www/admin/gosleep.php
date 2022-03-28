<?PHP
	require("tools.php");

	$prefix = "/etc/teslalogger/";
    if (isDocker())
		$prefix = "/tmp/";
		
	$carid = GetDefaultCarId();
	
	if (isset($_REQUEST["id"]) && strlen($_REQUEST["id"]) > 0)
		$carid = $_REQUEST["id"];

	unlink($prefix."wakeupteslalogger_$carid.txt");
	$filename = $prefix."cmd_gosleep_$carid.txt";
	
	$ret =	file_put_contents ($filename,".");
	chmod ($filename, 0666);
	
	$logfile = "/etc/teslalogger/nohup.out";
	$time = date("d.m.Y H:i:s", time());
	$ret2 =	file_put_contents($logfile, $time . " : Go-Sleep file $carid created!\r\n", FILE_APPEND);
		
	//echo "ok " . $ret . " / " . $ret2. " / " . $time;
	$returnpage = $_SERVER['HTTP_REFERER'];
	header("Location:$returnpage");
	
?>