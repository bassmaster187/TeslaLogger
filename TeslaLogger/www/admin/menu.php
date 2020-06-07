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

function menu($title)
{
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
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/degradation_token.php?token=<?PHP echo(GetTaskerToken()); ?>">My Degradation</a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/mycharging.php?token=<?PHP echo(GetTaskerToken()); ?>">My Charging AVG</a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/degradation.php">Fleet Degradation AVG</a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/charger.php">Fleet Charging AVG</a></li>
                            <li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/map.php">Fleet Fast Charging Map</a></li>
                            <li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/firmware.php">Firmware Tracker</a></li>
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
