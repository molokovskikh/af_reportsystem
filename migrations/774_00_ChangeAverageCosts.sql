﻿ALTER TABLE `reports`.`averagecosts` ADD COLUMN `ProductId` INTEGER UNSIGNED NOT NULL DEFAULT 0 AFTER `Quantity`,
 ADD COLUMN `ProducerId` INTEGER UNSIGNED NOT NULL DEFAULT 0 AFTER `ProductId`;