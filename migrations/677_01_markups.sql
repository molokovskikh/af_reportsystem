create table Reports.Markups ( Id int unsigned not null auto_increment,
	Type int unsigned not null,
	RegionId bigint unsigned not null,
	Begin decimal not null,
	End decimal not null,
	Value decimal not null,
	primary key(Id),
	constraint FK_RegionId foreign key (RegionId) references farm.regions(RegionCode) on delete cascade
);

insert into Reports.Markups(Type, RegionId, Begin, End, Value)
select t.Type, r.RegionCode, m.Begin, m.End, 20
from farm.regions r, (
select 0 as begin, 50 as end
union
select 50 as begin, 500 as end
union
select 500 as begin, 1000000 as end
) as m,
(select 0 as Type union select 1 as Type) as t
where r.RegionCode > 0 and r.Retail = 0;
