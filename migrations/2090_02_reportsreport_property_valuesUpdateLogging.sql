CREATE TABLE  `logs`.`ReportPropertyValueRSLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` char(1) NOT NULL,
  `ValueId` bigint(20) unsigned,
  `NewReportPropertyID` bigint(20) unsigned,
  `OldReportPropertyID` bigint(20) unsigned,
  `NewValue` varchar(255),
  `OldValue` varchar(255),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;
DROP TRIGGER IF EXISTS reports.ReportPropertyValueLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER reports.ReportPropertyValueLogDelete AFTER DELETE ON reports.report_property_values
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ReportPropertyValueRSLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'D',
		ValueId = OLD.ID,
		OldReportPropertyID = OLD.ReportPropertyID,
		OldValue = OLD.Value;
END;
DROP TRIGGER IF EXISTS reports.ReportPropertyValueLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER reports.ReportPropertyValueLogUpdate AFTER UPDATE ON reports.report_property_values
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ReportPropertyValueRSLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'U',
		ValueId = OLD.ID,
		NewReportPropertyID = NEW.ReportPropertyID,
		OldReportPropertyID = OLD.ReportPropertyID,
		NewValue = NEW.Value,
		OldValue = OLD.Value;
END;
DROP TRIGGER IF EXISTS reports.ReportPropertyValueLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER reports.ReportPropertyValueLogInsert AFTER INSERT ON reports.report_property_values
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ReportPropertyValueRSLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'I',
		ValueId = NEW.ID,
		NewReportPropertyID = NEW.ReportPropertyID,
		NewValue = NEW.Value;
END;
