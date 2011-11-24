update reports.report_type_properties set SelectStoredProcedure = 'GetPricesByRegionMask'
where ReportTypeCode = 1 and ID = 308;

update reports.report_type_properties set SelectStoredProcedure = 'GetPricesByRegionMask'
where ReportTypeCode = 2 and ID = 314;

update reports.report_type_properties set SelectStoredProcedure = 'GetPricesByRegionMask'
where ReportTypeCode = 3 and ID = 320;

update reports.report_type_properties set SelectStoredProcedure = 'GetPricesByRegionMask'
where ReportTypeCode = 13 and ID = 326;



DROP PROCEDURE IF EXISTS reports.GetPricesByRegionMask;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE reports.GetPricesByRegionMask(in inFilter varchar(255), in inID bigint)
BEGIN
declare filterStr varchar(512);
if(inFilter is null) then
    set filterStr = '0';
else
    set filterStr = inFilter;
end if;

SET @s = CONCAT(
"select T.PriceCode ID, T.PriceName DisplayValue
from
(
select
		distinct pd.PriceCode as PriceCode,
  		convert(concat(pd.PriceCode, ' - ', supps.Name, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) as PriceName,
        convert(concat(supps.Name, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) as PriceName2
	from usersettings.pricesdata pd
  		inner join future.suppliers supps on pd.FirmCode = supps.id
  		inner join usersettings.pricesregionaldata prd on prd.PriceCode = pd.PriceCode
  		inner join farm.Regions rg on supps.HomeRegion = rg.RegionCode
	where
  		supps.RegionMask & prd.RegionCode > 0
  		and pd.enabled = 1
		and pd.agencyenabled = 1
  		and prd.enabled = 1
  		and prd.RegionCode &", convert(concat(inID) using cp1251), "> 0

	union
	
	select
		pd.PriceCode as PriceCode,
  	convert(concat(pd.PriceCode, ' - ', supps.Name, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) as PriceName,
    convert(concat(supps.Name, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) as PriceName2
	from usersettings.pricesdata pd
		inner join future.suppliers supps on pd.FirmCode = supps.id
		inner join farm.Regions rg on supps.HomeRegion = rg.RegionCode
    where pd.PriceCode in (", filterStr, ")
)T
order by T.PriceName2;");

Prepare _sql From @s;
execute _sql;
END
