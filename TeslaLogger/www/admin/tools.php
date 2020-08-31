<?PHP 

function isDocker()
{
    $dockerfile = "/tmp/teslalogger-DOCKER";
    return file_exists($dockerfile);
}

function GetTeslaloggerURL($path)
{
    $url = "http://localhost:5000/";
    if (isDocker())
        $url = "http://teslalogger:5000/";

    return $url.$path;
}

function isShareData()
{
    $prefix = "/etc/teslalogger/";
    if (isDocker())
        $prefix = "/tmp/";

    return file_exists($prefix."sharedata.txt");
}

function setShareData($share)
{
    $prefix = "/etc/teslalogger/";
    if (isDocker())
        $prefix = "/tmp/";

    if ($share)
    {
        file_put_contents($prefix."sharedata.txt","");
        if (file_exists($prefix."nosharedata.txt")) 
            unlink($prefix."nosharedata.txt");
    }
    else
    {
        file_put_contents($prefix."nosharedata.txt","");
        if (file_exists($prefix."sharedata.txt"))
            unlink($prefix."sharedata.txt");
    }
}
?>