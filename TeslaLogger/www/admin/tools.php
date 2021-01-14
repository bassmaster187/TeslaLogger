<?PHP 

function isDocker()
{
    $dockerfile = "/tmp/teslalogger-DOCKER";
    return file_exists($dockerfile);
}

function GetTeslaloggerHTTPPort()
{
    $port = 5000;

    if (file_exists("/etc/teslalogger/settings.json"))
	{
		$content = file_get_contents("/etc/teslalogger/settings.json");
		$j = json_decode($content);
		if (!empty($j->{"HTTPPort"})) 
            $port = $j->{"HTTPPort"};	
    }
    
    return $port;
}

function GetTeslaloggerURL($path)
{
    $port = GetTeslaloggerHTTPPort();

    $url = "http://localhost:$port/";
    if (isDocker())
        $url = "http://teslalogger:$port/";

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

function JSONDatetoString($jsondate)
{
    $ts = preg_replace( '/[^0-9]/', '', $jsondate);
    $date = date("Y-m-d H:i:s", $ts / 1000);
    return $date;
}

function GrafanaVersion()
{
    $content = file_get_contents("http://grafana:3000/api/health");
    $j = json_decode($content);
    if (!empty($j->{"version"})) 
        return $j->{"version"};	

    return "?";
}
?>