CREATE TABLE `logs`.`ReportExecuteLogs` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `GeneralReportCode` INT(10) UNSIGNED NOT NULL,
  `StartTime` DATETIME,
  `EndTime` DATETIME,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;