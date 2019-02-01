-- MySQL dump 10.13  Distrib 5.5.62, for debian-linux-gnu (armv8l)
--
-- Host: localhost    Database: test
-- ------------------------------------------------------
-- Server version	5.5.62-0+deb8u1

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `charging`
--

DROP TABLE IF EXISTS `charging`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `charging` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `battery_level` double NOT NULL,
  `charge_energy_added` double NOT NULL,
  `charger_power` double NOT NULL,
  `Datum` datetime NOT NULL,
  `ideal_battery_range_km` double NOT NULL,
  `charger_voltage` int(11) DEFAULT NULL,
  `charger_phases` int(11) DEFAULT NULL,
  `charger_actual_current` int(11) DEFAULT NULL,
  `outside_temp` double DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=3522 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `chargingstate`
--

DROP TABLE IF EXISTS `chargingstate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `chargingstate` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `StartDate` datetime NOT NULL,
  `EndDate` datetime DEFAULT NULL,
  `UnplugDate` datetime DEFAULT NULL,
  `Pos` int(11) DEFAULT NULL,
  `charge_energy_added` double DEFAULT NULL,
  `StartChargingID` int(11) DEFAULT NULL,
  `EndChargingID` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=73 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `drivestate`
--

DROP TABLE IF EXISTS `drivestate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `drivestate` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `StartDate` datetime NOT NULL,
  `StartPos` int(11) NOT NULL,
  `EndDate` datetime DEFAULT NULL,
  `EndPos` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=137 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `pos`
--

DROP TABLE IF EXISTS `pos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `pos` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `Datum` datetime NOT NULL,
  `lat` double NOT NULL,
  `lng` double NOT NULL,
  `speed` int(11) DEFAULT NULL,
  `power` int(11) DEFAULT NULL,
  `odometer` double DEFAULT NULL,
  `ideal_battery_range_km` double DEFAULT NULL,
  `address` varchar(250) COLLATE utf8_unicode_ci DEFAULT NULL,
  `outside_temp` double DEFAULT NULL,
  `altitude` double DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=16141 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `shiftstate`
--

DROP TABLE IF EXISTS `shiftstate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `shiftstate` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `StartDate` datetime NOT NULL,
  `state` varchar(5) COLLATE utf8_unicode_ci DEFAULT NULL,
  `EndDate` datetime DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `state`
--

DROP TABLE IF EXISTS `state`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `state` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `StartDate` datetime NOT NULL,
  `state` varchar(50) COLLATE utf8_unicode_ci DEFAULT NULL,
  `EndDate` datetime DEFAULT NULL,
  `StartPos` int(11) DEFAULT NULL,
  `EndPos` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=436 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Temporary table structure for view `trip`
--

DROP TABLE IF EXISTS `trip`;
/*!50001 DROP VIEW IF EXISTS `trip`*/;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
/*!50001 CREATE TABLE `trip` (
  `StartDate` tinyint NOT NULL,
  `EndDate` tinyint NOT NULL,
  `StartRange` tinyint NOT NULL,
  `EndRange` tinyint NOT NULL,
  `Start_address` tinyint NOT NULL,
  `End_address` tinyint NOT NULL,
  `km_diff` tinyint NOT NULL,
  `consumption_kWh` tinyint NOT NULL,
  `avg_consumption_kWh_100km` tinyint NOT NULL,
  `DurationMinutes` tinyint NOT NULL,
  `StartKm` tinyint NOT NULL,
  `EndKm` tinyint NOT NULL,
  `lat` tinyint NOT NULL,
  `lng` tinyint NOT NULL,
  `EndLat` tinyint NOT NULL,
  `EndLng` tinyint NOT NULL
) ENGINE=MyISAM */;
SET character_set_client = @saved_cs_client;

--
-- Final view structure for view `trip`
--

/*!50001 DROP TABLE IF EXISTS `trip`*/;
/*!50001 DROP VIEW IF EXISTS `trip`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8 */;
/*!50001 SET character_set_results     = utf8 */;
/*!50001 SET collation_connection      = utf8_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50001 VIEW `trip` AS select `drivestate`.`StartDate` AS `StartDate`,`drivestate`.`EndDate` AS `EndDate`,`pos_Start`.`ideal_battery_range_km` AS `StartRange`,`pos_End`.`ideal_battery_range_km` AS `EndRange`,`pos_Start`.`address` AS `Start_address`,`pos_End`.`address` AS `End_address`,(`pos_End`.`odometer` - `pos_Start`.`odometer`) AS `km_diff`,((`pos_Start`.`ideal_battery_range_km` - `pos_End`.`ideal_battery_range_km`) * 0.190052356) AS `consumption_kWh`,((((`pos_Start`.`ideal_battery_range_km` - `pos_End`.`ideal_battery_range_km`) * 0.190052356) / (`pos_End`.`odometer` - `pos_Start`.`odometer`)) * 100) AS `avg_consumption_kWh_100km`,timestampdiff(MINUTE,`drivestate`.`StartDate`,`drivestate`.`EndDate`) AS `DurationMinutes`,`pos_Start`.`odometer` AS `StartKm`,`pos_End`.`odometer` AS `EndKm`,`pos_Start`.`lat` AS `lat`,`pos_Start`.`lng` AS `lng`,`pos_End`.`lat` AS `EndLat`,`pos_End`.`lng` AS `EndLng` from ((`drivestate` join `pos` `pos_Start` on((`drivestate`.`StartPos` = `pos_Start`.`id`))) join `pos` `pos_End` on((`drivestate`.`EndPos` = `pos_End`.`id`))) where ((`pos_End`.`odometer` - `pos_Start`.`odometer`) > 0.1) */;
/*!50001 SET character_set_client      = @saved_cs_client */;
/*!50001 SET character_set_results     = @saved_cs_results */;
/*!50001 SET collation_connection      = @saved_col_connection */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2018-11-30  9:29:47
