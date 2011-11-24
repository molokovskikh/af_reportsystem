create table Reports.ClientRatings
(
	Id int unsigned not null auto_increment,
	Date DateTime not null,
	ClientId int unsigned not null,
	RegionId bigint unsigned not null,
	Rating decimal(19, 5) not null,
	primary key (Id)
)
