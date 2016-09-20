CREATE TABLE  `logs`.`ReportRSLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` char(1) NOT NULL,
  `NewReportCode` bigint(20) unsigned,
  `OldReportCode` bigint(20) unsigned,
  `NewGeneralReportCode` bigint(20) unsigned,
  `OldGeneralReportCode` bigint(20) unsigned,
  `NewReportCaption` varchar(26),
  `OldReportCaption` varchar(26),
  `NewReportTypeCode` bigint(20) unsigned,
  `OldReportTypeCode` bigint(20) unsigned,
  `NewEnabled` bit(1),
  `OldEnabled` bit(1),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;
DROP TRIGGER IF EXISTS reports.ReportLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER reports.ReportLogDelete AFTER DELETE ON reports.reports
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ReportRSLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'D',
		OldReportCode = OLD.ReportCode,
		OldGeneralReportCode = OLD.GeneralReportCode,
		OldReportCaption = OLD.ReportCaption,
		OldReportTypeCode = OLD.ReportTypeCode,
		OldEnabled = OLD.Enabled;
END;
DROP TRIGGER IF EXISTS reports.ReportLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER reports.ReportLogUpdate AFTER UPDATE ON reports.reports
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.ReportRSLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'U',
		NewReportCode = NEW.ReportCode,
		OldReportCode = OLD.ReportCode,
		NewGeneralReportCode = NEW.GeneralReportCode,
		OldGeneralReportCode = OLD.GeneralReportCode,
		NewReportCaption = NEW.ReportCaption,
		OldReportCaption = OLD.ReportCaption,
		NewReportTypeCode = NEW.ReportTypeCode,
		OldReportTypeCode = OLD.ReportTypeCode,
		NewEnabled = NEW.Enabled,
		OldEnabled = OLD.Enabled;
END;
DROP TRIGGER IF EXISTS reports.ReportLogInsert;
DROP TRIGGER IF EXISTS reports.AddNewReport;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER reports.ReportLogInsert AFTER INSERT ON reports.reports
FOR EACH ROW BEGIN
  insert into report_properties (ReportCode, PropertyID, PropertyValue)
    select NEW.ReportCode, r.ID, r.DefaultValue
    FROM
      report_type_properties r
    where
          r.ReportTypeCode = NEW.ReportTypeCode
      and r.Optional = 0;


	INSERT
	INTO `logs`.ReportRSLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'I',
		NewReportCode = NEW.ReportCode,
		NewGeneralReportCode = NEW.GeneralReportCode,
		NewReportCaption = NEW.ReportCaption,
		NewReportTypeCode = NEW.ReportTypeCode,
		NewEnabled = NEW.Enabled;
END;
