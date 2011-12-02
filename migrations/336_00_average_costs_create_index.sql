alter table reports.AverageCosts
add index (Date, SupplierId, RegionId, AssortmentId);
