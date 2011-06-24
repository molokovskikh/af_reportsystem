DROP PROCEDURE IF EXISTS reports.`GetFirmCode`;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE reports.`GetFirmCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if (inID is not null) then
    select
      supps.Id as ID,
      convert(concat(supps.Id, '-', supps.Name, ' - ', rg.Region) using cp1251) as DisplayValue
    from
      future.suppliers supps,
      farm.regions rg
    where
      supps.Id = inID
      and rg.RegionCode = supps.HomeRegion
    order by supps.Name;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        supps.Id as ID,
        convert(concat(supps.Id, '-', supps.Name, ' - ', rg.Region) using cp1251) as DisplayValue
      from
        future.suppliers supps,
        farm.regions rg
      where
        supps.Name like filterStr        
        and rg.RegionCode = supps.HomeRegion
      order by supps.Name;
    else
      select
        supps.Id as ID,
        convert(concat(supps.Id, '-', supps.Name, ' - ', rg.Region) using cp1251) as DisplayValue
      from
        future.suppliers supps,
        farm.regions rg
      where        
        rg.RegionCode = supps.HomeRegion
      order by supps.Name;
    end if;
  end if;
end
