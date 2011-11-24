create table Reports.AverageCosts
(
	Id int unsigned not null auto_increment,
	Date DateTime not null,
	SupplierId int unsigned not null,
	RegionId bigint unsigned not null,
	AssortmentId int unsigned not null,
	Cost decimal(19, 5) not null,
	primary key (Id)
)
