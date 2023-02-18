-- MySQL dump 10.19  Distrib 10.3.36-MariaDB, for debian-linux-gnu (x86_64)
--
-- Host: database    Database: teslalogger
-- ------------------------------------------------------
-- Server version	10.4.7-MariaDB-1:10.4.7+maria~bionic

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `TPMS`
--

DROP TABLE IF EXISTS `TPMS`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `TPMS` (
  `CarId` int(11) NOT NULL,
  `Datum` datetime NOT NULL,
  `TireId` int(11) NOT NULL,
  `Pressure` double NOT NULL,
  PRIMARY KEY (`CarId`,`Datum`,`TireId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `TPMS`
--

LOCK TABLES `TPMS` WRITE;
/*!40000 ALTER TABLE `TPMS` DISABLE KEYS */;
/*!40000 ALTER TABLE `TPMS` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `can`
--

DROP TABLE IF EXISTS `can`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `can` (
  `datum` datetime NOT NULL,
  `id` mediumint(9) NOT NULL,
  `val` double DEFAULT NULL,
  `CarID` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`datum`,`id`),
  KEY `can_ix2` (`id`,`CarID`,`datum`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `can`
--

LOCK TABLES `can` WRITE;
/*!40000 ALTER TABLE `can` DISABLE KEYS */;
/*!40000 ALTER TABLE `can` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `car_version`
--

DROP TABLE IF EXISTS `car_version`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `car_version` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `StartDate` datetime NOT NULL,
  `version` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `CarID` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `car_version`
--

LOCK TABLES `car_version` WRITE;
/*!40000 ALTER TABLE `car_version` DISABLE KEYS */;
/*!40000 ALTER TABLE `car_version` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `cars`
--

DROP TABLE IF EXISTS `cars`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `cars` (
  `id` int(11) NOT NULL,
  `tesla_name` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `tesla_password` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `tesla_carid` int(11) DEFAULT NULL,
  `tesla_token` text COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `tesla_token_expire` datetime DEFAULT NULL,
  `tasker_hash` varchar(10) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `model` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `model_name` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `wh_tr` double DEFAULT NULL,
  `db_wh_tr` double DEFAULT NULL,
  `db_wh_tr_count` int(11) DEFAULT NULL,
  `car_type` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `car_special_type` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `car_trim_badging` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `display_name` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `raven` bit(1) DEFAULT NULL,
  `Battery` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `vin` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `freesuc` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `lastscanmytesla` datetime DEFAULT NULL,
  `refresh_token` text COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ABRP_token` varchar(40) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ABRP_mode` tinyint(1) DEFAULT 0,
  `SuCBingo_user` varchar(40) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `SuCBingo_apiKey` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `meter_type` varchar(20) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `meter_host` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `meter_parameter` varchar(200) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `wheel_type` varchar(40) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `cars`
--

LOCK TABLES `cars` WRITE;
/*!40000 ALTER TABLE `cars` DISABLE KEYS */;
/*!40000 ALTER TABLE `cars` ENABLE KEYS */;
UNLOCK TABLES;

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
  `charger_pilot_current` int(11) DEFAULT NULL,
  `charge_current_request` int(11) DEFAULT NULL,
  `battery_heater` tinyint(1) DEFAULT NULL,
  `battery_range_km` double DEFAULT NULL,
  `CarID` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `IX_charging_carid_datum` (`CarID`,`Datum`)
) ENGINE=InnoDB AUTO_INCREMENT=3522 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `charging`
--

LOCK TABLES `charging` WRITE;
/*!40000 ALTER TABLE `charging` DISABLE KEYS */;
/*!40000 ALTER TABLE `charging` ENABLE KEYS */;
UNLOCK TABLES;

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
  `conn_charge_cable` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `fast_charger_brand` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `fast_charger_type` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `fast_charger_present` tinyint(1) DEFAULT NULL,
  `max_charger_power` int(11) DEFAULT NULL,
  `cost_total` double DEFAULT NULL,
  `cost_currency` varchar(3) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `cost_per_kwh` double DEFAULT NULL,
  `cost_per_session` double DEFAULT NULL,
  `cost_per_minute` double DEFAULT NULL,
  `cost_idle_fee_total` double DEFAULT NULL,
  `cost_kwh_meter_invoice` double DEFAULT NULL,
  `meter_vehicle_kwh_start` double DEFAULT NULL,
  `meter_vehicle_kwh_end` double DEFAULT NULL,
  `meter_utility_kwh_start` double DEFAULT NULL,
  `meter_utility_kwh_end` double DEFAULT NULL,
  `meter_utility_kwh_sum` double DEFAULT NULL,
  `hidden` tinyint(1) NOT NULL DEFAULT 0,
  `combined_into` int(11) DEFAULT NULL,
  `meter_vehicle_kwh_sum` double DEFAULT NULL,
  `CarID` int(10) unsigned DEFAULT NULL,
  `wheel_type` varchar(40) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `co2_g_kWh` int(11) DEFAULT NULL,
  `country` varchar(80) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `export` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `chargingsate_ix_pos` (`Pos`),
  KEY `ixAnalyzeChargingStates1` (`id`,`CarID`,`StartChargingID`,`EndChargingID`)
) ENGINE=InnoDB AUTO_INCREMENT=73 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `chargingstate`
--

LOCK TABLES `chargingstate` WRITE;
/*!40000 ALTER TABLE `chargingstate` DISABLE KEYS */;
/*!40000 ALTER TABLE `chargingstate` ENABLE KEYS */;
UNLOCK TABLES;

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
  `outside_temp_avg` double DEFAULT NULL,
  `speed_max` int(11) DEFAULT NULL,
  `power_max` int(11) DEFAULT NULL,
  `power_min` int(11) DEFAULT NULL,
  `power_avg` double DEFAULT NULL,
  `meters_up` double DEFAULT NULL,
  `meters_down` double DEFAULT NULL,
  `distance_up_km` double DEFAULT NULL,
  `distance_down_km` double DEFAULT NULL,
  `distance_flat_km` double DEFAULT NULL,
  `height_max` double DEFAULT NULL,
  `height_min` double DEFAULT NULL,
  `CarID` int(10) unsigned DEFAULT NULL,
  `wheel_type` varchar(40) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `export` tinyint(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_startpos` (`StartPos`),
  KEY `ix_endpos2` (`EndPos`)
) ENGINE=InnoDB AUTO_INCREMENT=137 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `drivestate`
--

LOCK TABLES `drivestate` WRITE;
/*!40000 ALTER TABLE `drivestate` DISABLE KEYS */;
/*!40000 ALTER TABLE `drivestate` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `httpcodes`
--

DROP TABLE IF EXISTS `httpcodes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `httpcodes` (
  `id` int(11) NOT NULL,
  `text` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `httpcodes`
--

LOCK TABLES `httpcodes` WRITE;
/*!40000 ALTER TABLE `httpcodes` DISABLE KEYS */;
INSERT INTO `httpcodes` VALUES (100,'Continue'),(101,'SwitchingProtocols'),(102,'Processing'),(103,'EarlyHints'),(200,'OK'),(201,'Created'),(202,'Accepted'),(203,'NonAuthoritativeInformation'),(204,'NoContent'),(205,'ResetContent'),(206,'PartialContent'),(207,'MultiStatus'),(208,'AlreadyReported'),(226,'IMUsed'),(300,'Ambiguous'),(301,'Moved'),(302,'Redirect'),(303,'RedirectMethod'),(304,'NotModified'),(305,'UseProxy'),(306,'Unused'),(307,'TemporaryRedirect'),(308,'PermanentRedirect'),(400,'BadRequest'),(401,'Unauthorized'),(402,'PaymentRequired'),(403,'Forbidden'),(404,'NotFound'),(405,'MethodNotAllowed'),(406,'NotAcceptable'),(407,'ProxyAuthenticationRequired'),(408,'RequestTimeout'),(409,'Conflict'),(410,'Gone'),(411,'LengthRequired'),(412,'PreconditionFailed'),(413,'RequestEntityTooLarge'),(414,'RequestUriTooLong'),(415,'UnsupportedMediaType'),(416,'RequestedRangeNotSatisfiable'),(417,'ExpectationFailed'),(421,'MisdirectedRequest'),(422,'UnprocessableEntity'),(423,'Locked'),(424,'FailedDependency'),(426,'UpgradeRequired'),(428,'PreconditionRequired'),(429,'TooManyRequests'),(431,'RequestHeaderFieldsTooLarge'),(451,'UnavailableForLegalReasons'),(500,'InternalServerError'),(501,'NotImplemented'),(502,'BadGateway'),(503,'ServiceUnavailable'),(504,'GatewayTimeout'),(505,'HttpVersionNotSupported'),(506,'VariantAlsoNegotiates'),(507,'InsufficientStorage'),(508,'LoopDetected'),(510,'NotExtended'),(511,'NetworkAuthenticationRequired');
/*!40000 ALTER TABLE `httpcodes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `journeys`
--

DROP TABLE IF EXISTS `journeys`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `journeys` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `CarID` tinyint(4) NOT NULL,
  `StartPosID` int(11) NOT NULL,
  `EndPosID` int(11) NOT NULL,
  `consumption_kwh` double DEFAULT NULL,
  `charged_kwh` double DEFAULT NULL,
  `drive_duration_minutes` int(11) DEFAULT NULL,
  `charge_duration_minutes` int(11) DEFAULT NULL,
  `name` varchar(250) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `journeys`
--

LOCK TABLES `journeys` WRITE;
/*!40000 ALTER TABLE `journeys` DISABLE KEYS */;
/*!40000 ALTER TABLE `journeys` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `mothership`
--

DROP TABLE IF EXISTS `mothership`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `mothership` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `ts` datetime NOT NULL,
  `commandid` int(11) NOT NULL,
  `duration` double DEFAULT NULL,
  `httpcode` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `ix_id_ts` (`id`,`ts`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `mothership`
--

LOCK TABLES `mothership` WRITE;
/*!40000 ALTER TABLE `mothership` DISABLE KEYS */;
INSERT INTO `mothership` VALUES (1,'2023-02-18 22:03:36',1,0.115463,200);
/*!40000 ALTER TABLE `mothership` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `mothershipcommands`
--

DROP TABLE IF EXISTS `mothershipcommands`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `mothershipcommands` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `command` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `mothershipcommands`
--

LOCK TABLES `mothershipcommands` WRITE;
/*!40000 ALTER TABLE `mothershipcommands` DISABLE KEYS */;
INSERT INTO `mothershipcommands` VALUES (1,'teslalogger.de/share_degradation.php');
/*!40000 ALTER TABLE `mothershipcommands` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pos`
--

DROP TABLE IF EXISTS `pos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `pos` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `Datum` datetime(3) NOT NULL,
  `lat` double NOT NULL,
  `lng` double NOT NULL,
  `speed` int(11) DEFAULT NULL,
  `power` int(11) DEFAULT NULL,
  `odometer` double DEFAULT NULL,
  `ideal_battery_range_km` double DEFAULT NULL,
  `address` varchar(250) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `outside_temp` double DEFAULT NULL,
  `altitude` double DEFAULT NULL,
  `battery_level` double DEFAULT NULL,
  `inside_temp` double DEFAULT NULL,
  `battery_heater` tinyint(1) DEFAULT NULL,
  `is_preconditioning` tinyint(1) DEFAULT NULL,
  `sentry_mode` tinyint(1) DEFAULT NULL,
  `battery_range_km` double DEFAULT NULL,
  `CarID` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `idx_pos_CarID_id` (`CarID`,`id`),
  KEY `idx_pos_CarID_datum` (`CarID`,`Datum`)
) ENGINE=InnoDB AUTO_INCREMENT=16141 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pos`
--

LOCK TABLES `pos` WRITE;
/*!40000 ALTER TABLE `pos` DISABLE KEYS */;
/*!40000 ALTER TABLE `pos` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `shiftstate`
--

DROP TABLE IF EXISTS `shiftstate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `shiftstate` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `StartDate` datetime NOT NULL,
  `state` varchar(5) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `EndDate` datetime DEFAULT NULL,
  `CarID` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `shiftstate`
--

LOCK TABLES `shiftstate` WRITE;
/*!40000 ALTER TABLE `shiftstate` DISABLE KEYS */;
/*!40000 ALTER TABLE `shiftstate` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `state`
--

DROP TABLE IF EXISTS `state`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `state` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `StartDate` datetime NOT NULL,
  `state` varchar(50) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `EndDate` datetime DEFAULT NULL,
  `StartPos` int(11) DEFAULT NULL,
  `EndPos` int(11) DEFAULT NULL,
  `CarID` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=436 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `state`
--

LOCK TABLES `state` WRITE;
/*!40000 ALTER TABLE `state` DISABLE KEYS */;
/*!40000 ALTER TABLE `state` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `superchargers`
--

DROP TABLE IF EXISTS `superchargers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `superchargers` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `name` varchar(250) COLLATE utf8mb4_unicode_ci NOT NULL,
  `lat` double NOT NULL,
  `lng` double NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `superchargers`
--

LOCK TABLES `superchargers` WRITE;
/*!40000 ALTER TABLE `superchargers` DISABLE KEYS */;
/*!40000 ALTER TABLE `superchargers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `superchargerstate`
--

DROP TABLE IF EXISTS `superchargerstate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8 */;
CREATE TABLE `superchargerstate` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `nameid` int(11) NOT NULL,
  `ts` datetime NOT NULL,
  `available_stalls` tinyint(4) NOT NULL,
  `total_stalls` tinyint(4) NOT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `superchargerstate`
--

LOCK TABLES `superchargerstate` WRITE;
/*!40000 ALTER TABLE `superchargerstate` DISABLE KEYS */;
/*!40000 ALTER TABLE `superchargerstate` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Temporary table structure for view `trip`
--

DROP TABLE IF EXISTS `trip`;
/*!50001 DROP VIEW IF EXISTS `trip`*/;
SET @saved_cs_client     = @@character_set_client;
SET character_set_client = utf8;
/*!50001 CREATE VIEW `trip` AS SELECT
 1 AS `StartDate`,
  1 AS `EndDate`,
  1 AS `StartRange`,
  1 AS `EndRange`,
  1 AS `Start_address`,
  1 AS `End_address`,
  1 AS `km_diff`,
  1 AS `consumption_kWh`,
  1 AS `avg_consumption_kWh_100km`,
  1 AS `DurationMinutes`,
  1 AS `StartKm`,
  1 AS `EndKm`,
  1 AS `lat`,
  1 AS `lng`,
  1 AS `EndLat`,
  1 AS `EndLng`,
  1 AS `StartPosID`,
  1 AS `EndPosID`,
  1 AS `outside_temp_avg`,
  1 AS `speed_max`,
  1 AS `power_max`,
  1 AS `power_min`,
  1 AS `power_avg`,
  1 AS `CarID`,
  1 AS `wheel_type` */;
SET character_set_client = @saved_cs_client;

--
-- Dumping routines for database 'teslalogger'
--

--
-- Final view structure for view `trip`
--

/*!50001 DROP VIEW IF EXISTS `trip`*/;
/*!50001 SET @saved_cs_client          = @@character_set_client */;
/*!50001 SET @saved_cs_results         = @@character_set_results */;
/*!50001 SET @saved_col_connection     = @@collation_connection */;
/*!50001 SET character_set_client      = utf8mb4 */;
/*!50001 SET character_set_results     = utf8mb4 */;
/*!50001 SET collation_connection      = utf8mb4_general_ci */;
/*!50001 CREATE ALGORITHM=UNDEFINED */
/*!50013 DEFINER=`root`@`%` SQL SECURITY DEFINER */
/*!50001 VIEW `trip` AS select `drivestate`.`StartDate` AS `StartDate`,`drivestate`.`EndDate` AS `EndDate`,`pos_start`.`ideal_battery_range_km` AS `StartRange`,`pos_end`.`ideal_battery_range_km` AS `EndRange`,`pos_start`.`address` AS `Start_address`,`pos_end`.`address` AS `End_address`,`pos_end`.`odometer` - `pos_start`.`odometer` AS `km_diff`,(`pos_start`.`ideal_battery_range_km` - `pos_end`.`ideal_battery_range_km`) * `cars`.`wh_tr` AS `consumption_kWh`,(`pos_start`.`ideal_battery_range_km` - `pos_end`.`ideal_battery_range_km`) * `cars`.`wh_tr` / (`pos_end`.`odometer` - `pos_start`.`odometer`) * 100 AS `avg_consumption_kWh_100km`,timestampdiff(MINUTE,`drivestate`.`StartDate`,`drivestate`.`EndDate`) AS `DurationMinutes`,`pos_start`.`odometer` AS `StartKm`,`pos_end`.`odometer` AS `EndKm`,`pos_start`.`lat` AS `lat`,`pos_start`.`lng` AS `lng`,`pos_end`.`lat` AS `EndLat`,`pos_end`.`lng` AS `EndLng`,`pos_start`.`id` AS `StartPosID`,`pos_end`.`id` AS `EndPosID`,`drivestate`.`outside_temp_avg` AS `outside_temp_avg`,`drivestate`.`speed_max` AS `speed_max`,`drivestate`.`power_max` AS `power_max`,`drivestate`.`power_min` AS `power_min`,`drivestate`.`power_avg` AS `power_avg`,`drivestate`.`CarID` AS `CarID`,`drivestate`.`wheel_type` AS `wheel_type` from (((`drivestate` join `pos` `pos_start` on(`drivestate`.`StartPos` = `pos_start`.`id`)) join `pos` `pos_end` on(`drivestate`.`EndPos` = `pos_end`.`id`)) join `cars` on(`cars`.`id` = `drivestate`.`CarID`)) where `pos_end`.`odometer` - `pos_start`.`odometer` > 0.1 */;
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

-- Dump completed on 2023-02-18 22:06:01
