CREATE TABLE  `logs`.`ReportPropertyRSLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` char(1) NOT NULL,
  `PropertyId` bigint(20) unsigned,
  `NewReportCode` bigint(20) unsigned,
  `OldReportCode` bigint(20) unsigned,
  `NewPropertyID` bigint(20) unsigned,
  `OldPropertyID` bigint(20) unsigned,
  `NewPropertyValue` varchar(255),
  `OldPropertyValue` varchar(255),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;
DROP TRIGGER IF EXISTS reports.ReportPropertyLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER reports.ReportPropertyLogDelete AFTER DELETE ON reports.report_properties
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ReportPropertyRSLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'D',
		PropertyId = OLD.ID,
		OldReportCode = OLD.ReportCode,
		OldPropertyID = OLD.PropertyID,
		OldPropertyValue = OLD.PropertyValue;
END;
DROP TRIGGER IF EXISTS reports.ReportPropertyLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER reports.ReportPropertyLogUpdate AFTER UPDATE ON reports.report_properties
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ReportPropertyRSLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'U',
		PropertyId = OLD.ID,
		NewReportCode = NEW.ReportCode,
		OldReportCode = OLD.ReportCode,
		NewPropertyID = NEW.PropertyID,
		OldPropertyID = OLD.PropertyID,
		NewPropertyValue = NEW.PropertyValue,
		OldPropertyValue = OLD.PropertyValue;
END;
DROP TRIGGER IF EXISTS reports.ReportPropertyLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER reports.ReportPropertyLogInsert AFTER INSERT ON reports.report_properties
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ReportPropertyRSLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'I',
		PropertyId = NEW.ID,
		NewReportCode = NEW.ReportCode,
		NewPropertyID = NEW.PropertyID,
		NewPropertyValue = NEW.PropertyValue;
END;
