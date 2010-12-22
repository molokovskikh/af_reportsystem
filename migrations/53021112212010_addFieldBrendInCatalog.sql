ALTER TABLE `catalogs`.`Catalog` ADD COLUMN `Brand` TINYINT(1) UNSIGNED NOT NULL AFTER `UpdateTime`;


UPDATE catalogs.catalog C
JOIN
(select id from catalogs.Catalog
where
(SELECT count(*)
from catalogs.assortment
where catalogid = Catalog.id and Checked = true) =
(SELECT count(*)
from catalogs.assortment
where catalogid = Catalog.id) and
(SELECT count(*)
from catalogs.assortment
where catalogid = Catalog.id) = 1) as newt on newt.id = C.Id
SET C.Brand = true ;