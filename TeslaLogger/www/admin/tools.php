<?PHP 

function isRedirectDockerToHost()
{
    return file_exists("REDIRECTDOCKERTOHOST");
}

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
    
    if (isRedirectDockerToHost())
        $url = "http://host.docker.internal:$port/";
    else if (isDocker())
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

function DatasourceUpdated()
{
    $content = file_get_contents("/tmp/datasource-DOCKER");
    if (strpos($content, "secureJsonData:") > 0)
        return true;

    return false;
}

function startsWith( $haystack, $needle ) {
    $length = strlen( $needle );
    return substr( $haystack, 0, $length ) === $needle;
}

function files_are_equal($a, $b)
{
  // Check if filesize is different
  if(filesize($a) !== filesize($b))
      return false;

  // Check if content is different
  $ah = fopen($a, 'rb');
  $bh = fopen($b, 'rb');

  $result = true;
  while(!feof($ah))
  {
    if(fread($ah, 8192) != fread($bh, 8192))
    {
      $result = false;
      break;
    }
  }

  fclose($ah);
  fclose($bh);

  return $result;
}

function GetDefaultCarId()
{
    if (file_exists("/etc/teslalogger/settings.json"))
    {
        $json = file_get_contents("/etc/teslalogger/settings.json");
        $json_data = json_decode($json,true);

        if (!empty($carid = $json_data["defaultcarid"]))
            return $json_data["defaultcarid"];
    }

    return 1;
}

?>