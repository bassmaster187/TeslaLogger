<?PHP
	require("tools.php");

	$logfile = "/etc/teslalogger/nohup.out";

	$time = date("d.m.Y H:i:s", time());
	$ret2 =	file_put_contents($logfile, $time . " : Reboot request!\r\n", FILE_APPEND);
	$ret2 =	file_put_contents($logfile, $time . " : --------------------------------------------\r\n\r\n", FILE_APPEND);

	sleep(2);

	if (isDocker())
	{
		$ret2 =	file_put_contents($logfile, $time . " : Docker detected\r\n", FILE_APPEND);
		file_put_contents("/tmp/teslalogger-cmd-restart.txt","");

		echo (t("OK"));
	}
	else
	{
		$output  = shell_exec('sudo /sbin/reboot');
		if ($output === false) {
			// Handle the error
			echo "error";
		}
		else
			echo (t("OK"));
	}
?>
