<?PHP
	unlink("/etc/teslalogger/cmd_updated.txt");

	// persistent marker: survives the reboot so the update reports "manual" (instead of
	// "automatic") to teslalogger.de - see update_log.php / UpdateTeslalogger.cs
	file_put_contents("/etc/teslalogger/update_manual.txt", date("d.m.Y H:i:s", time()));

	$logfile = "/etc/teslalogger/nohup.out";
	$dockerfile = "/tmp/teslalogger-DOCKER";

	$time = date("d.m.Y H:i:s", time());
	$ret2 =	file_put_contents($logfile, $time . " : UPDATE request!\r\n", FILE_APPEND);
	$ret2 =	file_put_contents($logfile, $time . " : --------------------------------------------\r\n\r\n", FILE_APPEND);

	sleep(2);

	if (file_exists($dockerfile))
	{
		$ret2 =	file_put_contents($logfile, $time . " : Docker detected\r\n", FILE_APPEND);
		file_put_contents("/tmp/teslalogger-cmd-restart.txt","update");

		echo ("OK");
	}
	else
	{
		$output  = shell_exec('sudo /sbin/reboot');
		if ($output === false) {
			// Handle the error
			echo "error";
		}
		else
			echo ("OK");
	}
?>
