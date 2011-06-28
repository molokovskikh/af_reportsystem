DROP PROCEDURE IF EXISTS reports.`GetPayerCode`;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE reports.`GetPayerCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if (inID is not null) then
    select
      p.PayerId as ID,
      convert(concat(p.PayerId, '-', p.ShortName) using cp1251) as DisplayValue
    from
      billing.payers p
    where
          p.PayerId = inID
        and (exists(select * from future.Clients cl inner join billing.PayerClients pc on cl.Id = pc.ClientId where pc.PayerId = p.PayerId))
    order by p.ShortName;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        p.PayerId as ID,
        convert(concat(p.PayerId, '-', p.ShortName) using cp1251) as DisplayValue
      from
        billing.payers p
      where
            p.ShortName like filterStr
        and (exists(select * from future.Clients cl inner join billing.PayerClients pc on cl.Id = pc.ClientId where pc.PayerId = p.PayerId))
      order by p.ShortName;
    else
      select
        p.PayerId as ID,
        convert(concat(p.PayerId, '-', p.ShortName) using cp1251) as DisplayValue
      from
        billing.payers p
      where
        (exists(select * from future.Clients cl inner join billing.PayerClients pc on cl.Id = pc.ClientId where pc.PayerId = p.PayerId))
      order by p.ShortName;
    end if;
  end if;
end
