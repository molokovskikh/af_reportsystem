DROP PROCEDURE IF EXISTS reports.`GetCostOptimizationFirmCode`;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE reports.`GetCostOptimizationFirmCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if (inID is not null) then
    select
      supps.Id as ID,
      convert(concat(supps.Id, '-', supps.Name, ' - ', rg.Region) using cp1251) as DisplayValue
    from
      future.suppliers supps,  
      farm.regions rg,
      usersettings.CostOptimizationRules cor
    where
          supps.Id = inID      
      and rg.RegionCode = supps.HomeRegion
      and cor.SupplierId = supps.Id
    order by supps.Name;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        supps.Id as ID,
        convert(concat(supps.Id, '-', supps.Name, ' - ', rg.Region) using cp1251) as DisplayValue
      from
        future.suppliers supps, 
        farm.regions rg,
        usersettings.CostOptimizationRules cor
      where
            supps.Name like filterStr        
        and rg.RegionCode = supps.HomeRegion
        and cor.SupplierId = supps.Id
      order by supps.Name;
    else
      select
        supps.Id as ID,
        convert(concat(supps.Id, '-', supps.Name, ' - ', rg.Region) using cp1251) as DisplayValue
      from
        future.suppliers supps, 
        farm.regions rg,
        usersettings.CostOptimizationRules cor
      where            
        rg.RegionCode = supps.HomeRegion
        and cor.SupplierId = supps.Id
      order by supps.Name;
    end if;
  end if;
end
