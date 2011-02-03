CREATE TABLE `reports`.`Mailing_Addresses` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Mail` VARCHAR(45) NOT NULL,
  `GeneralReport` INT(10) UNSIGNED NOT NULL,
  PRIMARY KEY (`Id`)
)
ENGINE = InnoDB;

ALTER TABLE `reports`.`Mailing_Addresses` MODIFY COLUMN `GeneralReport` INT(10) UNSIGNED DEFAULT NULL;

ALTER TABLE `reports`.`Mailing_Addresses` MODIFY COLUMN `GeneralReport` BIGINT(20) UNSIGNED DEFAULT NULL;
