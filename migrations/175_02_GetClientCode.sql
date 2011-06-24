DROP PROCEDURE IF EXISTS reports.`GetClientCode`;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE reports.`GetClientCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if (inID is not null) then
    select
        cl.Id as ID,
        convert(concat(cl.Id, '-', cl.Name) using cp1251) as DisplayValue
    from
        future.clients cl
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
        future.clients cl
      where
           cl.Name like filterStr        
        and cl.Status = 1
      order by cl.Name;
    else
      select
        cl.Id as ID,
        convert(concat(cl.Id, '-', cl.Name) using cp1251) as DisplayValue
      from
        future.clients cl
      where            
        cl.Status = 1
      order by cl.Name;
    end if;
  end if;
end
