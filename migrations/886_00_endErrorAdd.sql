ALTER TABLE `logs`.`ReportExecuteLogs` ADD COLUMN `EndError` TINYINT(1) UNSIGNED NOT NULL AFTER `EndTime`;

update `logs`.ReportExecuteLogs
set EndError = if(EndTime is null, true, false);