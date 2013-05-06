alter table Farm.Regions
add column CalculateOrders tinyint(1) unsigned not null default 0;

update Farm.Regions
set CalculateOrders = 1
where RegionCode in (1, 2, 2048, 4, 8, 32, 64, 16384, 32768, 65536, 128, 16777216, 33554432, 16, 256, 274877906944, 17592186044416, 512);
