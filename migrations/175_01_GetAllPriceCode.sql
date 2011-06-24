DROP PROCEDURE IF EXISTS reports.`GetAllPriceCode`;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE reports.`GetAllPriceCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  drop temporary table if exists tempGetPriceCode;
  create temporary table tempGetPriceCode
  engine=memory
  select
    pd.PriceCode as PriceCode,
    convert(concat(pd.PriceCode, ' - ', supps.Name, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) as PriceName
  from
    usersettings.pricesdata pd
    inner join future.suppliers supps on supps.Id = pd.FirmCode
    inner join farm.regions rg on rg.RegionCode = supps.HomeRegion;
  if (inID is not null) then
    select
      tmp.PriceCode as ID,
      tmp.PriceName as DisplayValue
    from
      tempGetPriceCode tmp
    where
      tmp.PriceCode = inID
    order by tmp.PriceName;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        tmp.PriceCode as ID,
        tmp.PriceName as DisplayValue
      from
        tempGetPriceCode tmp
      where
        tmp.PriceName like filterStr
      order by tmp.PriceName;
    else
      select
        tmp.PriceCode as ID,
        tmp.PriceName as DisplayValue
      from
        tempGetPriceCode tmp
      order by tmp.PriceName;
    end if;
  end if;
  drop table if exists tempGetPriceCode;
end
