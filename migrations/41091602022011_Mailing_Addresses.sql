CREATE TABLE `reports`.`Mailing_Addresses` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Mail` VARCHAR(45) NOT NULL,
  `GeneralReport` INT(10) UNSIGNED NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;

ALTER TABLE `reports`.`Mailing_Addresses` MODIFY COLUMN `GeneralReport` INT(10) UNSIGNED DEFAULT NULL;

ALTER TABLE `reports`.`Mailing_Addresses` MODIFY COLUMN `GeneralReport` BIGINT(20) UNSIGNED DEFAULT NULL;


ALTER TABLE `reports`.`Mailing_Addresses` ADD CONSTRAINT `FK_Mailing_Addresses_1` FOREIGN KEY `FK_Mailing_Addresses_1` (`GeneralReport`)
    REFERENCES `General_Reports` (`GeneralReportCode`)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
