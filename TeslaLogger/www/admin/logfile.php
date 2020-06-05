<?PHP
	$output  = shell_exec('tail -n1000 /etc/teslalogger/nohup.out');
	if ($output === false) {
        // Handle the error
		echo "error";
    }
	
	$output = str_replace("Start update","<b>Start update</b>", $output);
	$output = str_replace("End update","<b>End update</b>", $output);
	$output = str_replace("Rebooting","<b>Rebooting</b>", $output);
	$output = str_replace("Reboot request!","<b>Reboot request!</b>", $output);

	$output = str_replace("Error: Cloning into '/etc/teslalogger/git'...","...", $output);
	
	$output = str_replace("Rebooting","<b>Rebooting</b>", $output);
	
	$output = str_replace("Key","<b>Key</b>", $output);
	$output = str_replace("not translated!","<b>not translated!</b>", $output);
	
	$output = preg_replace("/(.*(Exception|Error).*)/", "<font color='red'>$1</font>", $output);
	$output = preg_replace("/(state: .*)/", "<b>$1</b>", $output);
	$output = preg_replace("/(http[s]?:\/\/[\w\.\/\-]+)/", "<a href='$1'>$1</a>", $output);
	
	echo nl2br($output);
?>