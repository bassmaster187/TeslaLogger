<?PHP
	require("admin/tools.php");

	$prefix = "/etc/teslalogger/";
    if (isDocker())
		$prefix = "/tmp/";
		
	$carid = 1;
	
	if (isset($_REQUEST["id"]) && strlen($_REQUEST["id"]) > 0)
		$carid = $_REQUEST["id"];

	unlink($prefix."cmd_gosleep_$carid.txt");
	$filename = $prefix."wakeupteslalogger_$carid.txt";
	
	$ret =	file_put_contents ($filename,".");
	chmod ($filename, 0666);
	
	$logfile = "/etc/teslalogger/nohup.out";
	$time = date("d.m.Y H:i:s", time());
	$ret2 =	file_put_contents($logfile, $time . " : Wakeup file $carid created!\r\n", FILE_APPEND);
		
	$returnpage = $_SERVER['HTTP_REFERER'];
	if (isset($returnpage))
		header("Location:$returnpage");
	else
		echo "ok " . $ret . " / " . $ret2. " / " . $time . " / ID: ".$carid;
?>