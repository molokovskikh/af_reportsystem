DROP PROCEDURE IF EXISTS reports.`GetClientsForMatrix` ;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE reports.`GetClientsForMatrix`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if (inID is not null) then
    select
        cl.Id as ID,
        convert(concat(cl.Id, '-', cl.Name) using cp1251) as DisplayValue
    from
        Customers.clients cl
		join usersettings.RetClientsSet r on r.ClientCode = cl.Id and (r.BuyingMatrix is not null or r.OfferMatrix is not null)
    where
          cl.Id = inID
      and cl.Status = 1
    order by cl.Name;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        cl.Id as ID,
        convert(concat(cl.Id, '-', cl.Name) using cp1251) as DisplayValue
      from
        Customers.clients cl
		join usersettings.RetClientsSet r on r.ClientCode = cl.Id and (r.BuyingMatrix is not null or r.OfferMatrix is not null)
      where
           cl.Name like filterStr
        and cl.Status = 1
      order by cl.Name;
    else
      select
        cl.Id as ID,
        convert(concat(cl.Id, '-', cl.Name) using cp1251) as DisplayValue
      from
        Customers.clients cl
		join usersettings.RetClientsSet r on r.ClientCode = cl.Id and (r.BuyingMatrix is not null or r.OfferMatrix is not null)
      where
        cl.Status = 1
      order by cl.Name;
    end if;
  end if;
end