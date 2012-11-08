DROP PROCEDURE Reports.GetOrg;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE Reports.`GetOrg`(in inFilter varchar(255), in inID int)
begin
  declare filterStr varchar(257);
  if ((inFilter is not null) and (length(inFilter) > 0)) then
    set filterStr = concat('%', inFilter, '%');
    select
      o.Id as ID,
      o.Name as DisplayValue
    from Billing.LegalEntities o
    where
      o.Name like filterStr
    order by o.Name;
  else
    select
      o.Id as ID,
      o.Name as DisplayValue
    from Billing.LegalEntities o
    order by o.Name;
  end if;
end;
