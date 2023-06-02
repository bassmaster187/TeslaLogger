<?PHP
require_once("language.php");
require_once("tools.php");
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

function menu($title, $prefix = "")
{
    $car = "";
    $tasker_token = "";
    global $display_name;

    $current_carid = $_SESSION["carid"];
    if (!isset($current_carid))
        $current_carid = GetDefaultCarId();

    $alldashboards = file_get_contents("/etc/teslalogger/dashboardlinks.txt");

    $allcars = file_get_contents(GetTeslaloggerURL("getallcars"),0, stream_context_create(["http"=>["timeout"=>2]]));

    $jcars = json_decode($allcars);
    if ($jcars !== null)
    {
        foreach ($jcars as $k => $v) {
            if ($v->{"id"} == $current_carid)
            {
                $display_name = $v->{"display_name"};
                $tasker_token = $v->{"tasker_hash"};
                $car = $v->{"model_name"};

                if (strlen($display_name) == 0)
                    $display_name = "Car ".$v->{"id"};
            }
        }
    }

    $ref = "?token=" . $tasker_token . "&ref=" . full_path()."&car=".$car;
?>
<header id="masthead" class="site-header" role="banner">
    <div class="header-main">
        <h1 class="site-title"><a href="<?php echo $prefix; ?>index.php?carid=<?=$current_carid?>" rel="home"><img src="logo.jpg" alt="Logo"> <?PHP echo(t($title)); ?></a></h1>
        <nav id="primary-navigation" class="site-navigation primary-navigation" role="navigation">
            <button class="menu-toggle">Primary Menu</button>
            <div class="menu-menuoben-container">
                <ul id="primary-menu" class="nav-menu">
                    <li id="menu-item-0" class="page_item_has_children">
                        <a href="<?php echo $prefix; ?>index.php"><?PHP echo($display_name);?></a>
                        <ul class='children'>
<?PHP
                        if ($jcars !== null)
                        {
                            foreach($jcars as $k => $v) {
                                $dn = $v->{"display_name"};
                                $carid = $v->{"id"};

                                if (strlen($dn) == 0)
                                    $dn = "Car ".$carid;
								echo(str_repeat("\t",7));
								echo('<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="'.$prefix.'index.php?carid='.$carid.'">'.$dn.'</a></li>');
								echo("\n");
                            }
                        }
?>
						</ul>
					</li>
					<li id="menu-item-1" class="menu-item menu-item-type-custom menu-item-object-custom menu-item-1">
						<a href="<?php echo $prefix; ?>logfile.php"><?php t("Logfile"); ?></a>
					</li>
					<li id="menu-item-2" class="menu-item menu-item-type-custom menu-item-object-custom menu-item-2">
						<a href="javascript:BackgroudRun('restartlogger.php', '<?php t("Reboot!"); ?>')"><?php t("Restart"); ?></a>
					</li>
					<li id="menu-item-3" class="menu-item menu-item-type-custom menu-item-object-custom menu-item-3">
						<a href="javascript:BackgroudRun('update.php', '<?php t("Reboot!"); ?>')"><?php t("Update"); ?></a>
					</li>
					<li id="menu-item-7" class="page_item_has_children">
						<a href="#"><?php t("Dashboards"); ?></a>
						<ul class='children'>
<?PHP
						$d = explode("\r\n", $alldashboards);
                        foreach($d as $dl) {
							$dlargs = explode("|", $dl);
							if (strlen($dlargs[0]) > 0) {
								echo(str_repeat("\t",7));
								$urlpart = $dlargs[1];
								$urlpart = str_replace("DEBUG Wh/TR","debug-wh-tr",$urlpart);
								echo('<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="' . $urlpart .'">');
								echo str_replace("CO2","CO<sub>2</sub>",$dlargs[0]);
								echo('</a></li>');
								echo("\n");
							}
						}
?>
						</ul>
					</li>

					<li id="menu-item-4" class="page_item_has_children">
						<a href="#"><?php t("Fleet Statistic"); ?></a>
						<ul class='children'>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/degradation_token.php<?PHP echo($ref); ?>"><?php t("My Degradation"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/mycharging.php<?PHP echo($ref); ?>"><?php t("My Charging AVG"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/degradation.php<?PHP echo($ref); ?>"><?php t("Fleet Degradation AVG"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/charger.php<?PHP echo($ref); ?>"><?php t("Fleet Charging AVG"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/charger_fw.php<?PHP echo($ref); ?>"><?php t("Fleet Charging MAX (Firmware)"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/charging_locations.php<?PHP echo($ref); ?>"><?php t("Fleet Charging Locations"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/map.php<?PHP echo($ref); ?>"><?php t("Fleet Fast Charging Map"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/suc-map.php<?PHP echo($ref); ?>"><?php t("Supercharger Usage"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="https://teslalogger.de/firmware.php<?PHP echo($ref); ?>"><?php t("Firmware Tracker"); ?></a></li>
						</ul>
					</li>
					<li id="menu-item-5" class="page_item_has_children">
						<a href="#"><?php t("Extras"); ?></a>
						<ul class='children'>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="<?php echo $prefix; ?>changelog.php"><?php t("Changelog"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="<?php echo $prefix; ?>backup.php"><?php t("Backup"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="<?php echo $prefix; ?>restore.php"><?php t("Restore"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="<?php echo $prefix; ?>geofencing.php"><?php t("Geofence"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="<?php echo $prefix; ?>journeys.php?carid=<?= $current_carid ?>"><?php t("Journeys"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="<?php echo $prefix; ?>dashboard.php?carid=<?= $current_carid ?>"><?php t("Dashboard"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="<?php echo $prefix; ?>abrp.php?carid=<?= $current_carid ?>"><?php t("Abetterrouteplanner"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="<?php echo $prefix; ?>superchargebingo.php?carid=<?= $current_carid ?>"><?php t("SuperChargeBingo"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="<?php echo $prefix; ?>wallbox.php?carid=<?= $current_carid ?>"><?php t("Wallbox"); ?></a></li>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="/wakeup.php?id=<?= $current_carid ?>"><?php t("Wakeup Teslalogger"); ?></a></li>
<?PHP if (!file_exists("/etc/teslalogger/cmd_gosleep_$current_carid.txt"))
							{?>
							<li class="menu-item menu-item-type-custom menu-item-object-custom"><a href="gosleep.php?id=<?= $current_carid ?>"><?php t("Suspend Teslalogger"); ?></a></li>
<?PHP
							}?>
						</ul>
					</li>
					<li id="menu-item-6" class="menu-item menu-item-type-post_type menu-item-object-post menu-item-6">
						<a href="<?php echo $prefix; ?>settings.php"><?php t("Settings"); ?></a>
					</li>
				</ul>
			</div>
		</nav>
	</div>
</header>
<script>
function BackgroudRun($target, $text, $reload=false)
  {
	  $.ajax($target, {
		data: {
			id: ''
		}
		})
		.then(
		function success(name) {
			alert($text);

            if ($reload)
                location.reload(true);
		},
		function fail(data, status) {
			alert($text);

            if ($reload)
                location.reload(true);
		}
	);
  }
  </script>
<script type='text/javascript' src='static/functions.js?ver=20150315'></script>
<?PHP
}
