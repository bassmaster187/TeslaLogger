<?php
  $DEBUG = 0;
  $chargedata = array();
  $maxchargepower = 150;
  if ($DEBUG) { echo "load config<br>"; }
  $config = file('/etc/teslalogger/TeslaLogger.exe.config');
  if ($config) {
    if ($DEBUG) { echo "parse config<br>"; }
    foreach ($config as $configline) {
      preg_match ('/Server=(.+?);Database=(.+?);Uid=(.+?);Password=(.+?);/', $configline, $matches);
      if ($matches) {
        $server = $matches[1];
        $database = $matches[2];
        $uid = $matches[3];
        $password = $matches[4];
        if ($DEBUG) { echo "found database config for " . $uid . "@" . $server . "/" . $database . "<br>"; }
        $pdo = new PDO("mysql:host=".$server.";dbname=".$database, $uid, $password);
        if ($pdo) {
          if ($DEBUG) { echo "db connected<br>"; }
          $maxchargesql = "SELECT MAX(charger_power) FROM charging";
          foreach ($pdo->query($maxchargesql) as $maxchargesqlrow) {
            $maxchargepower = $maxchargesqlrow[0] * 1.1;
          }
          $charginstate = "SELECT Id, StartDate, EndDate FROM chargingstate WHERE export IS NOT NULL";
          foreach ($pdo->query($charginstate) as $charginstaterow) {
            $chargingstateID = $charginstaterow['Id'];
            $StartDate = $charginstaterow['StartDate'];
            $EndDate = $charginstaterow['EndDate'];
            if ($DEBUG) { echo "&nbsp;".$StartDate."--".$EndDate."<br>"; }
            $charging = "SELECT battery_level, charger_power FROM charging WHERE charger_power > 0 AND Datum BETWEEN '".$StartDate."' AND '".$EndDate."'";
            foreach ($pdo->query($charging) as $chargingrow) {
              $battery_level = $chargingrow['battery_level'];
              $charger_power = $chargingrow['charger_power'];
              if ($DEBUG) { echo "&nbsp;&nbsp;".$battery_level.":".$charger_power."<br>"; }
              $chargedata[$chargingstateID][$battery_level] = $charger_power;
            }
          }
        }
      }
    }
  }
  if (!$DEBUG) {
    echo '<html><head>';
    echo '<script type="text/javascript">'."\n";
    echo 'function mouseIn(_class) { var svg = document.getElementsByTagName("svg")[0]; var elements = document.getElementsByClassName(_class); for (var i = 0; i < elements.length; i++) { elements[i].setAttribute(\'stroke\', \'black\'); } for (var i = 0; i < elements.length; i++) { svg.appendChild(elements[i]); } }'."\n";
    echo 'function mouseOut(_class) { var elements = document.getElementsByClassName(_class); for (var i = 0; i < elements.length; i++) { elements[i].setAttribute(\'stroke\', \'lightgray\'); } }'."\n";
    echo "\n".'</script>';
    echo '</head><body>';
    echo '<svg xmlns="http://www.w3.org/2000/svg" version="1.1" width="1120" height="'.($maxchargepower*5+32).'"><g>';
    for ($i = 0; $i <= 10; $i++) {
      echo '<line x1="'.(100+$i*100).'" y1="'.($maxchargepower * 5).'" x2="'.(100+$i*100).'" y2="'.(10 + $maxchargepower * 5).'" style="stroke:rgb(0,0,0);stroke-width:2" />';
      echo '<line x1="'.(100+$i*100).'" y1="0" x2="'.(100+$i*100).'" y2="'.($maxchargepower * 5).'" style="stroke:rgb(192,192,192);stroke-width:2" />';
      echo '<text x="'.(100+$i*100).'" y="'.(24 + $maxchargepower * 5).'" fill="black" text-anchor="middle">'.($i*10).'%</text>';
    }
    for ($i = 0; $i < $maxchargepower; $i = $i + 10) {
      echo '<line x1="100" y1="'.(($maxchargepower-$i) * 5).'" x2="90" y2="'.(($maxchargepower-$i) * 5).'" style="stroke:rgb(0,0,0);stroke-width:2" />';
      echo '<line x1="100" y1="'.(($maxchargepower-$i) * 5).'" x2="1100" y2="'.(($maxchargepower-$i) * 5).'" style="stroke:rgb(192,192,192);stroke-width:2" />';
      echo '<text x="86" y="'.(($maxchargepower * 5) - ($i * 5) + 5).'" fill="black" text-anchor="end">'.$i.'kW</text>';
    }
    echo '<line x1="100" y1="0" x2="100" y2="'.($maxchargepower * 5).'" style="stroke:rgb(0,0,0);stroke-width:2" />';
    echo '<line x1="100" y1="'.($maxchargepower * 5).'" x2="1100" y2="'.($maxchargepower * 5).'" style="stroke:rgb(0,0,0);stroke-width:2" />';
    foreach (array_keys($chargedata) as $id) {
      $color = "rgb(".mt_rand(128, 255).",".mt_rand(128, 255).",".mt_rand(128, 255).")";
      $startx = -1;
      $starty = -1;
      $nextx = -1;
      $nexty = -1;
      foreach (array_keys($chargedata[$id]) as $soc) {
        $kw = $chargedata[$id][$soc];
        $startx = $nextx;
        $starty = $nexty;
        $nextx = 100 + $soc * 10;
        $nexty = ($maxchargepower-$kw) * 5;
        if ($startx > -1) {
          echo '<line x1="'.$startx.'" y1="'.$starty.'" x2="'.$nextx.'" y2="'.$nexty.'" style="stroke:'.$color.';stroke-width:2" />';
        }
        echo '<circle class="'."charge_".$id.'" cx="'.$nextx.'" cy="'.$nexty.'" r="3" stroke="lightgray" stroke-width="1" fill="'.$color.'" onmouseover="mouseIn(evt.target.getAttribute(\'class\'));" onmouseout="mouseOut(evt.target.getAttribute(\'class\'));"/>';
      }
    }
    echo "</g></svg></body></html>";
  }
?>
