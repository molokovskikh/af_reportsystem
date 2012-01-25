drop procedure Reports.GetAllPriceCodeWithOrder;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE Reports.`GetAllPriceCodeWithOrder`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  drop temporary table if exists tempGetPriceCode;
  create temporary table tempGetPriceCode
  engine=memory
  select
    supps.Id as SID,
    supps.Name as SName,
    pd.PriceCode as PriceCode,
    convert(concat(pd.PriceCode, ' - ', supps.Name, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) as PriceName
  from
    usersettings.pricesdata pd
    inner join future.suppliers supps on supps.Id = pd.FirmCode
    inner join farm.regions rg on rg.RegionCode = supps.HomeRegion
  where supps.Disabled = 0
    and pd.AgencyEnabled = 1
    and pd.Enabled = 1;
  if (inID is not null) then
    select
      tmp.PriceCode as ID,
      tmp.PriceName as DisplayValue
    from
      tempGetPriceCode tmp
    where
      tmp.PriceCode = inID
      order by tmp.SName, tmp.SID;
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
        order by tmp.SName, tmp.SID;
    else
      select
        tmp.PriceCode as ID,
        tmp.PriceName as DisplayValue
      from
        tempGetPriceCode tmp
        order by tmp.SName, tmp.SID;
    end if;
  end if;
  drop table if exists tempGetPriceCode;
end;
