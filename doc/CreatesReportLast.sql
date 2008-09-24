-- ----------------------------------------------------------------------
-- MySQL GRT Application
-- SQL Script
-- ----------------------------------------------------------------------

SET FOREIGN_KEY_CHECKS = 0;

CREATE DATABASE IF NOT EXISTS `testreports`
  CHARACTER SET cp1251;
-- -------------------------------------
-- Tables

DROP TABLE IF EXISTS `testreports`.`general_reports`;
CREATE TABLE `testreports`.`general_reports` (
  `GeneralReportCode` BIGINT unsigned NOT NULL AUTO_INCREMENT,
  `FirmCode` INT(11) unsigned NOT NULL DEFAULT '0',
  `Allow` BIT NOT NULL DEFAULT 0,
  `EMailAddress` VARCHAR(255) NULL,
  `EMailSubject` VARCHAR(255) NULL,
  `ReportFileName` VARCHAR(255) NULL,
  `ReportArchName` VARCHAR(255) NULL,
  PRIMARY KEY (`GeneralReportCode`),
  INDEX `FirmCode` (`FirmCode`),
  CONSTRAINT `FirmCode` FOREIGN KEY `FirmCode` (`FirmCode`)
    REFERENCES `usersettings`.`clientsdata` (`FirmCode`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)
ENGINE = InnoDB
ROW_FORMAT = Compact
CHARACTER SET cp1251 COLLATE cp1251_general_ci;

DROP TABLE IF EXISTS `testreports`.`report_properties`;
CREATE TABLE `testreports`.`report_properties` (
  `ID` BIGINT unsigned NOT NULL AUTO_INCREMENT,
  `ReportCode` BIGINT UNSIGNED NOT NULL,
  `PropertyID` BIGINT UNSIGNED NOT NULL,
  `PropertyValue` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`ID`),
  INDEX `ReportCode` (`ReportCode`),
  CONSTRAINT `ReportCode` FOREIGN KEY `ReportCode` (`ReportCode`)
    REFERENCES `testreports`.`reports` (`ReportCode`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `FK_RP_PropertyID` FOREIGN KEY `FK_RP_PropertyID` (`PropertyID`)
    REFERENCES `testreports`.`report_type_properties` (`ID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)
ENGINE = InnoDB
ROW_FORMAT = Compact
CHARACTER SET cp1251 COLLATE cp1251_general_ci;

DROP TABLE IF EXISTS `testreports`.`reports`;
CREATE TABLE `testreports`.`reports` (
  `ReportCode` BIGINT unsigned NOT NULL AUTO_INCREMENT,
  `GeneralReportCode` BIGINT unsigned NOT NULL DEFAULT '0',
  `ReportCaption` VARCHAR(26) NOT NULL,
  `ReportTypeCode` BIGINT unsigned NOT NULL DEFAULT '1',
  `Enabled` BIT NOT NULL DEFAULT 1,
  PRIMARY KEY (`ReportCode`),
  INDEX `ReportTypeCode` (`ReportTypeCode`),
  INDEX `GeneralReportCode` (`GeneralReportCode`),
  CONSTRAINT `GeneralReportCode` FOREIGN KEY `GeneralReportCode` (`GeneralReportCode`)
    REFERENCES `testreports`.`general_reports` (`GeneralReportCode`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `ReportTypeCode` FOREIGN KEY `ReportTypeCode` (`ReportTypeCode`)
    REFERENCES `testreports`.`reporttypes` (`ReportTypeCode`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)
ENGINE = InnoDB
ROW_FORMAT = Compact
CHARACTER SET cp1251 COLLATE cp1251_general_ci;

DROP TABLE IF EXISTS `testreports`.`reporttypes`;
CREATE TABLE `testreports`.`reporttypes` (
  `ReportTypeCode` BIGINT unsigned NOT NULL AUTO_INCREMENT,
  `ReportTypeName` VARCHAR(255) NOT NULL,
  `ReportTypeFilePrefix` VARCHAR(255) NOT NULL,
  `AlternateSubject` VARCHAR(255) NOT NULL,
  `ReportClassName` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`ReportTypeCode`)
)
ENGINE = InnoDB
ROW_FORMAT = Compact
CHARACTER SET cp1251 COLLATE cp1251_general_ci;

DROP TABLE IF EXISTS `testreports`.`report_type_properties`;
CREATE TABLE `testreports`.`report_type_properties` (
  `ID` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `ReportTypeCode` BIGINT UNSIGNED NOT NULL,
  `PropertyName` VARCHAR(255) NOT NULL,
  `DisplayName` VARCHAR(255) NOT NULL,
  `PropertyType` ENUM('BOOL', 'INT', 'ENUM', 'LIST', 'STRING', 'DATETIME') NOT NULL DEFAULT 'int',
  `Optional` BIT NOT NULL DEFAULT 0,
  `PropertyEnumID` BIGINT UNSIGNED NULL,
  `SelectStoredProcedure` VARCHAR(255) NULL,
  `DefaultValue` VARCHAR(255) NULL,
  PRIMARY KEY (`ID`),
  CONSTRAINT `FK_RTP_ReportTypeCode` FOREIGN KEY `FK_RTP_ReportTypeCode` (`ReportTypeCode`)
    REFERENCES `testreports`.`reporttypes` (`ReportTypeCode`)
    ON DELETE CASCADE
    ON UPDATE CASCADE,
  CONSTRAINT `PropertyEnumID` FOREIGN KEY `PropertyEnumID` (`PropertyEnumID`)
    REFERENCES `testreports`.`property_enums` (`ID`)
    ON DELETE SET NULL
    ON UPDATE CASCADE
)
ENGINE = InnoDB
CHARACTER SET cp1251 COLLATE cp1251_general_ci;

DROP TABLE IF EXISTS `testreports`.`property_enums`;
CREATE TABLE `testreports`.`property_enums` (
  `ID` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `EnumName` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`ID`)
)
ENGINE = InnoDB
CHARACTER SET cp1251 COLLATE cp1251_general_ci;

DROP TABLE IF EXISTS `testreports`.`enum_values`;
CREATE TABLE `testreports`.`enum_values` (
  `ID` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `PropertyEnumID` BIGINT UNSIGNED NOT NULL,
  `Value` INT NOT NULL,
  `DisplayValue` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`ID`),
  CONSTRAINT `EnumID` FOREIGN KEY `EnumID` (`PropertyEnumID`)
    REFERENCES `testreports`.`property_enums` (`ID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)
ENGINE = InnoDB
CHARACTER SET cp1251 COLLATE cp1251_general_ci;

DROP TABLE IF EXISTS `testreports`.`report_property_values`;
CREATE TABLE `testreports`.`report_property_values` (
  `ID` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  `ReportPropertyID` BIGINT UNSIGNED NOT NULL,
  `Value` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`ID`),
  CONSTRAINT `FK_RPV_ReportPropertyID` FOREIGN KEY `FK_RPV_ReportPropertyID` (`ReportPropertyID`)
    REFERENCES `testreports`.`report_properties` (`ID`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)
ENGINE = InnoDB
CHARACTER SET cp1251 COLLATE cp1251_general_ci;



SET FOREIGN_KEY_CHECKS = 1;

-- ----------------------------------------------------------------------
-- EOF

