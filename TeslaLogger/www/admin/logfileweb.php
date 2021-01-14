<?PHP
echo("apache error log:");

	$output  = shell_exec('sudo tail -n100 /var/log/apache2/error.log');
	if ($output === false) {
        // Handle the error
		echo "error";
    }
	
	echo nl2br($output);
?>