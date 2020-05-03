<?PHP
	if (file_exists("/tmp/teslalogger-DOCKER"))
                $output  = shell_exec('sh /etc/teslalogger/backup.sh');
        else
                $output  = shell_exec('/etc/teslalogger/backup.sh');
	if ($output === false) {
        // Handle the error
		echo "error";
    }
	else
		echo ("OK");
?>
