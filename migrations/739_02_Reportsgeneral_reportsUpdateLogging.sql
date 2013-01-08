
alter table Logs.GeneralReportLogs
add column NoArchive tinyint(1),
add column OwnedByUser int(10) unsigned,
add column SendDescriptionFile tinyint(1),
add column Public tinyint(1) unsigned,
add column LastSuccess datetime
;

DROP TRIGGER IF EXISTS Reports.GeneralReportLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Reports.GeneralReportLogDelete AFTER DELETE ON Reports.general_reports
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.GeneralReportLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		GeneralReportCode = OLD.GeneralReportCode,
		FirmCode = OLD.FirmCode,
		Allow = OLD.Allow,
		EMailSubject = OLD.EMailSubject,
		ReportFileName = OLD.ReportFileName,
		ReportArchName = OLD.ReportArchName,
		ContactGroupId = OLD.ContactGroupId,
		Temporary = OLD.Temporary,
		TemporaryCreationDate = OLD.TemporaryCreationDate,
		Comment = OLD.Comment,
		PayerID = OLD.PayerID,
		Format = OLD.Format,
		NoArchive = OLD.NoArchive,
		OwnedByUser = OLD.OwnedByUser,
		SendDescriptionFile = OLD.SendDescriptionFile,
		Public = OLD.Public,
		LastSuccess = OLD.LastSuccess;
END;

DROP TRIGGER IF EXISTS Reports.GeneralReportLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Reports.GeneralReportLogUpdate AFTER UPDATE ON Reports.general_reports
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.GeneralReportLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		GeneralReportCode = OLD.GeneralReportCode,
		FirmCode = NULLIF(NEW.FirmCode, OLD.FirmCode),
		Allow = NULLIF(NEW.Allow, OLD.Allow),
		EMailSubject = NULLIF(NEW.EMailSubject, OLD.EMailSubject),
		ReportFileName = NULLIF(NEW.ReportFileName, OLD.ReportFileName),
		ReportArchName = NULLIF(NEW.ReportArchName, OLD.ReportArchName),
		ContactGroupId = NULLIF(NEW.ContactGroupId, OLD.ContactGroupId),
		Temporary = NULLIF(NEW.Temporary, OLD.Temporary),
		TemporaryCreationDate = NULLIF(NEW.TemporaryCreationDate, OLD.TemporaryCreationDate),
		Comment = NULLIF(NEW.Comment, OLD.Comment),
		PayerID = NULLIF(NEW.PayerID, OLD.PayerID),
		Format = NULLIF(NEW.Format, OLD.Format),
		NoArchive = NULLIF(NEW.NoArchive, OLD.NoArchive),
		OwnedByUser = NULLIF(NEW.OwnedByUser, OLD.OwnedByUser),
		SendDescriptionFile = NULLIF(NEW.SendDescriptionFile, OLD.SendDescriptionFile),
		Public = NULLIF(NEW.Public, OLD.Public),
		LastSuccess = NULLIF(NEW.LastSuccess, OLD.LastSuccess);
END;

DROP TRIGGER IF EXISTS Reports.GeneralReportLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Reports.GeneralReportLogInsert AFTER INSERT ON Reports.general_reports
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.GeneralReportLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		GeneralReportCode = NEW.GeneralReportCode,
		FirmCode = NEW.FirmCode,
		Allow = NEW.Allow,
		EMailSubject = NEW.EMailSubject,
		ReportFileName = NEW.ReportFileName,
		ReportArchName = NEW.ReportArchName,
		ContactGroupId = NEW.ContactGroupId,
		Temporary = NEW.Temporary,
		TemporaryCreationDate = NEW.TemporaryCreationDate,
		Comment = NEW.Comment,
		PayerID = NEW.PayerID,
		Format = NEW.Format,
		NoArchive = NEW.NoArchive,
		OwnedByUser = NEW.OwnedByUser,
		SendDescriptionFile = NEW.SendDescriptionFile,
		Public = NEW.Public,
		LastSuccess = NEW.LastSuccess;
END;
