<?PHP 

checkHTaccess();

function checkHTaccess()
{
    $htaccessPath = "/var/www/html/admin/.htaccess";
    $htpasswdPath = "/tmp/.htpasswd";

    if (filesize($htaccessPath) == 0 && file_exists($htpasswdPath))
    {
        error_log("TeslaLogger: .htaccess file is empty but .htpasswd exists - recreating .htaccess");
        createHTAccess();
    }
}

function createHTAccess()
{

    // Create .htaccess content
    $htaccessContent = "AuthType Basic\n";
    $htaccessContent .= "AuthName \"TeslaLogger Admin Panel\"\n";
    $htaccessContent .= "AuthUserFile /tmp/.htpasswd\n";
    $htaccessContent .= "Require valid-user\n";

    // Try to write .htaccess file to admin directory
    $htaccessPath = "/var/www/html/admin/.htaccess";
    if (file_put_contents($htaccessPath, $htaccessContent) === false) {
        error_log("TeslaLogger: Failed to write .htaccess file to " . $htaccessPath);
        
        // If that fails, write to tmp and try to copy
        $tempHtaccessPath = "/tmp/.htaccess";
        file_put_contents($tempHtaccessPath, $htaccessContent);
        if (!copy($tempHtaccessPath, $htaccessPath)) {
            error_log("TeslaLogger: Failed to copy .htaccess file from " . $tempHtaccessPath . " to " . $htaccessPath . ". Check permissions on /var/www/html/admin/");
            throw new Exception("Could not write .htaccess file. Please check permissions on /var/www/html/admin/");
        } else {
            error_log("TeslaLogger: Successfully copied .htaccess file from temp location");
        }
    } else {
        error_log("TeslaLogger: Successfully created .htaccess file");
    }
}

function isRedirectDockerToHost()
{
    return file_exists("REDIRECTDOCKERTOHOST");
}

function isDocker()
{
    $dockerfile = "/tmp/teslalogger-DOCKER";
    return file_exists($dockerfile);
}

function isDockerNET8()
{
    $dockerfile = "/var/tmp/dockernet8";
    return file_exists($dockerfile);
}

function GetFileFromTeslaloggerAndWriteToTMP($filename)
{
    $url = GetTeslaloggerURL("getfile/$filename");
    $contenturl = @file_get_contents($url);
    if ($contenturl)
        file_put_contents("/tmp/$filename", $contenturl);
}

function GetFromTeslalogger($path)
{
    $url = GetTeslaloggerURL($path);
    $contenturl = @file_get_contents($url);
    return $contenturl;
}

function GetTeslaloggerHTTPPort()
{
    $port = 5000;

    if (file_exists("/tmp/settings.json"))
	{
		$content = file_get_contents("/tmp/settings.json");
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
    if (empty($jsondate))
        return "";
        
    $ts = preg_replace( '/[^0-9]/', '', $jsondate);
    if (empty($ts))
        return "";
        
    $ts = intval($ts);
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
    if (filesize($a) !== filesize($b))
        return false;

    // Check if content is different
    $ah = fopen($a, 'rb');
    $bh = fopen($b, 'rb');

    if ($ah && $bh) {
        $result = true;
        while (feof($ah) !== false) {
            if (fread($ah, 8192) != fread($bh, 8192)) {
                $result = false;
                break;
            }
        }

        fclose($ah);
        fclose($bh);

        return $result;
    }
    return false;
}

function GetDefaultCarId()
{
    if (file_exists("/tmp/settings.json"))
    {
        $json = file_get_contents("/tmp/settings.json");
        $json_data = json_decode($json,true);

        if (!empty($carid = $json_data["defaultcarid"]))
            return $json_data["defaultcarid"];
    }

    return 1;
}

?>