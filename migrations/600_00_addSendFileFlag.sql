alter table reports.reports add column SendFile TINYINT(1);

ALTER TABLE `reports`.`reports` MODIFY COLUMN `SendFile` TINYINT(1) DEFAULT 0;

update reports.reports set
SendFile = Enabled;