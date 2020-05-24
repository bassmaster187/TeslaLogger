namespace TeslaLogger
{
    internal class DBViews
    {
        public const string Trip = 
            @"CREATE 
    ALGORITHM = UNDEFINED 
    SQL SECURITY DEFINER
VIEW `trip` AS
    SELECT 
        `drivestate`.`StartDate` AS `StartDate`,
        `drivestate`.`EndDate` AS `EndDate`,
        `pos_start`.`ideal_battery_range_km` AS `StartRange`,
        `pos_end`.`ideal_battery_range_km` AS `EndRange`,
        `pos_start`.`address` AS `Start_address`,
        `pos_end`.`address` AS `End_address`,
        (`pos_end`.`odometer` - `pos_start`.`odometer`) AS `km_diff`,
        ((`pos_start`.`ideal_battery_range_km` - `pos_end`.`ideal_battery_range_km`) * 0.190052356) AS `consumption_kWh`,
        ((((`pos_start`.`ideal_battery_range_km` - `pos_end`.`ideal_battery_range_km`) * 0.190052356) / (`pos_end`.`odometer` - `pos_start`.`odometer`)) * 100) AS `avg_consumption_kWh_100km`,
        TIMESTAMPDIFF(MINUTE,
            `drivestate`.`StartDate`,
            `drivestate`.`EndDate`) AS `DurationMinutes`,
        `pos_start`.`odometer` AS `StartKm`,
        `pos_end`.`odometer` AS `EndKm`,
        `pos_start`.`lat` AS `lat`,
        `pos_start`.`lng` AS `lng`,
        `pos_end`.`lat` AS `EndLat`,
        `pos_end`.`lng` AS `EndLng`,
        `drivestate`.`outside_temp_avg` AS `outside_temp_avg`,
        `drivestate`.`speed_max` AS `speed_max`,
        `drivestate`.`power_max` AS `power_max`,
        `drivestate`.`power_min` AS `power_min`,
        `drivestate`.`power_avg` AS `power_avg`
    FROM
        ((`drivestate`
        JOIN `pos` `pos_start` ON ((`drivestate`.`StartPos` = `pos_start`.`id`)))
        JOIN `pos` `pos_end` ON ((`drivestate`.`EndPos` = `pos_end`.`id`)))
    WHERE
        ((`pos_end`.`odometer` - `pos_start`.`odometer`) > 0.1)";
    }
}
