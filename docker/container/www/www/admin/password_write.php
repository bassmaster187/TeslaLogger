<?PHP
$email = $_REQUEST["email"];
$password = $_REQUEST["password"];

$j = array('email' => $email,
'password' => $password);

file_put_contents('/etc/teslalogger/new_credentials.json', json_encode($j));

shell_exec('sudo /sbin/reboot');
?>