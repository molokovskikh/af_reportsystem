DROP PROCEDURE IF EXISTS Reports.GetOrg;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE Reports.`GetOrg`(in inFilter varchar(255), in inID int)
begin
  declare filterStr varchar(257);
  if ((inFilter is not null) and (length(inFilter) > 0)) then
    set filterStr = concat('%', inFilter, '%');
    select
      o.Id as ID,
      concat(p.ShortName, ', ', ifnull(o.Name, ''))  as DisplayValue
    from Billing.LegalEntities o
		join Billing.Payers p on p.PayerId = o.PayerId
    where
      concat(p.ShortName, ', ', ifnull(o.Name, '')) like filterStr
    order by concat(p.ShortName, ', ', ifnull(o.Name, ''));
  else
    select
      o.Id as ID,
      concat(p.ShortName, ', ', ifnull(o.Name, '')) as DisplayValue
    from Billing.LegalEntities o
		join Billing.Payers p on p.PayerId = o.PayerId
    order by concat(p.ShortName, ', ', ifnull(o.Name, ''));
  end if;
end;
