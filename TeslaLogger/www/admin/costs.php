<?php
$mysqluser="root";
$mysqlpwd="teslalogger";
$db = new PDO('mysql:host=192.168.200.184;dbname=teslalogger', $mysqluser, $mysqlpwd );
$db->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
if ($_SERVER['REQUEST_METHOD'] !== 'POST')
	{
	echo '<form action="costs.php" method="POST">';
	echo "<table border='1'>";
	echo "<tr>";
	echo "<th>Von</th>";
	echo "<th>Bis</th>";
	echo "<th>Wo</th>";
	echo "<th>Wieviel</th>";
	echo '<th>Kosten</th>';
	echo "</tr>";
	$sql = "SELECT chargingstate.id as stateid, StartDate as Begin, EndDate as End, pos.address as Position, charging.charge_energy_added as KWH_charged, chargingstate.costs FROM teslalogger.chargingstate LEFT JOIN pos ON pos.id = chargingstate.pos LEFT JOIN charging ON charging.id = chargingstate.EndChargingID WHERE chargingstate.costs is NULL";
	foreach ($db->query($sql) as $row) {
		$stateid = $row['stateid'];	
		echo "<tr>";     
		echo "<td>" . $row['Begin']."</td>";
		echo "<td>" . $row['End']."</td>";
		echo "<td>" . $row['Position']."</td>";
		echo "<td>" . $row['KWH_charged']."</td>";
		echo '<th><input type="text" id="'.$stateid.'" name="'.$stateid.'"></th>';
		echo "</tr>";
		}
	echo "</table>";
	echo '<button type="submit">Speichern</button>';
	echo "</form>";
	}
else
	{
	$sql = "SELECT chargingstate.id as stateid, StartDate as Begin, EndDate as End, pos.address as Position, charging.charge_energy_added as KWH_charged, chargingstate.costs FROM teslalogger.chargingstate LEFT JOIN pos ON pos.id = chargingstate.pos LEFT JOIN charging ON charging.id = chargingstate.EndChargingID WHERE chargingstate.costs is NULL";
	foreach ($db->query($sql) as $row) {
		$stateid = $row['stateid'];
		if ($_POST[$stateid] != "") {
			$statement = $db->prepare("UPDATE chargingstate SET costs=? WHERE id=".$stateid);
			$statement->execute(array($_POST[$stateid]));
			}
		}	
 	} 		
?>
