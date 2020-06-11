<?PHP
function GetTaskerToken()
{
    $taskertoken = "";
    if (file_exists("/etc/teslalogger/TASKERTOKEN"))
    {
        $taskertoken = file_get_contents("/etc/teslalogger/TASKERTOKEN");
    }

    return $taskertoken;
}

function full_path()
{
    $s = &$_SERVER;
    $ssl = (!empty($s['HTTPS']) && $s['HTTPS'] == 'on') ? true:false;
    $sp = strtolower($s['SERVER_PROTOCOL']);
    $protocol = substr($sp, 0, strpos($sp, '/')) . (($ssl) ? 's' : '');
    $port = $s['SERVER_PORT'];
    $port = ((!$ssl && $port=='80') || ($ssl && $port=='443')) ? '' : ':'.$port;
    $host = isset($s['HTTP_X_FORWARDED_HOST']) ? $s['HTTP_X_FORWARDED_HOST'] : (isset($s['HTTP_HOST']) ? $s['HTTP_HOST'] : null);
    $host = isset($host) ? $host : $s['SERVER_NAME'] . $port;
    $uri = $protocol . '://' . $host . $s['REQUEST_URI'];
    $segments = explode('?', $uri, 2);
    $url = $segments[0];
    return $url;
}

function menu($title)
{
    $ref = "?token=" . GetTaskerToken() . "&ref=" . full_path();
?>
<header id="masthead" class="site-header" role="banner">
    <div class="header-main">
        <h1 class="site-title"><a href="index.php" rel="home"><img src="logo.jpg" alt="Logo"> <?PHP echo($title); ?></a></h1>
        <nav id="primary-navigation" class="site-navigation primary-navigation" role="navigation">
            <button class="menu-toggle">Primary Menu</button>
            <div class="menu-menuoben-container">
                <ul id="primary-menu" class="nav-menu">
                    <!--<li id="menu-item-0" class="menu-item menu-item-type-custom menu-item-object-custom menu-item-0">
                        <a href="index.php">Home</a>
                    </li> -->
                    <li id="menu-item-1" class="menu-item menu-item-type-custom menu-item-object-custom menu-item-1">
                        <a href="logfile.php">Logfile</a>
                    </li>
                    <li id="menu-item-2" class="menu-item menu-item-type-custom menu-item-object-custom menu-item-2">
                        <a href="javascript:BackgroudRun('restartlogger.php', 'Reboot!');">Restart</a>
                    </li>
                    <li id="menu-item-3" class="menu-item menu-item-type-custom menu-item-object-custom menu-item-3">
                        <a href="javascript:BackgroudRun('update.php', 'Reboot!')">Update</a>
                    </li>
                    <li id="menu-item-4" class="page_item_has_children">
                        <a href="#">Fleet Statistic</a>
                        <ul class='children'>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/degradation_token.php<?PHP echo($ref); ?>">My Degradation</a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/mycharging.php<?PHP echo($ref); ?>">My Charging AVG</a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/degradation.php<?PHP echo($ref); ?>">Fleet Degradation AVG</a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/charger.php<?PHP echo($ref); ?>">Fleet Charging AVG</a></li>
                            <li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/charger_fw.php<?PHP echo($ref); ?>">Fleet Charging MAX (Firmware)</a></li>
                            <li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/map.php<?PHP echo($ref); ?>">Fleet Fast Charging Map</a></li>
                            <li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/firmware.php<?PHP echo($ref); ?>">Firmware Tracker</a></li>
						</ul>
                    </li>
					<li id="menu-item-5" class="page_item_has_children">
						<a href="#">Extras</a>
						<ul class='children'>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="backup.php">Backup</a></li>
                            <li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="restore.php">Restore</a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="geofencing.php">Geofence</a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="javascript:BackgroudRun('/wakeup.php', 'Wakeup!');">Wakeup!</a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="javascript:BackgroudRun('gosleep.php', 'Sleep!');">Sleep</a></li>
						</ul>
                    </li>
                    <li id="menu-item-6" class="menu-item menu-item-type-post_type menu-item-object-post menu-item-6">
                        <a href="settings.php">Settings</a>
                    </li>
                </ul>
            </div>
        </nav>
    </div>
</header>
<script>
function BackgroudRun($target, $text)
  {
	  $.ajax($target, {
		data: {
			id: ''
		}
		})
		.then(
		function success(name) {
			alert($text);
		},
		function fail(data, status) {
			alert($text);
		}
	);
  }
  </script>
<script type='text/javascript' src='https://www.impala64.de/blog/tesla/wp-content/themes/twentyfourteen/js/functions.js?ver=20150315'></script>
<?PHP
}
