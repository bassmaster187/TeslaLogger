<?PHP 

function isDocker()
{
    $dockerfile = "/tmp/teslalogger-DOCKER";
    return file_exists($dockerfile);
}

?>