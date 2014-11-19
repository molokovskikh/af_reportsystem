CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE Reports.GetUser(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if (inID is not null) then
    select
      u.Id as ID,
      convert(concat(u.Id, '-', c.Name, ' - ', r.Region) using cp1251) as DisplayValue
    from Customers.Users u
      join Customers.Clients c on c.Id = u.ClientId
      join farm.regions r on r.RegionCode = c.RegionCode
    where
      u.Id = inID
    order by convert(concat(u.Id, '-', c.Name, ' - ', r.Region) using cp1251);
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        u.Id as ID,
        convert(concat(u.Id, '-', c.Name, ' - ', r.Region) using cp1251) as DisplayValue
      from Customers.Users u
        join Customers.Clients c on c.Id = u.ClientId
        join farm.regions r on r.RegionCode = c.RegionCode
      where
        convert(concat(u.Id, '-', c.Name, ' - ', r.Region) using cp1251) like filterStr
      order by convert(concat(u.Id, '-', c.Name, ' - ', r.Region) using cp1251);
    else
      select
        u.Id as ID,
        convert(concat(u.Id, '-', c.Name, ' - ', r.Region) using cp1251) as DisplayValue
      from Customers.Users u
        join Customers.Clients c on c.Id = u.ClientId
        join farm.regions r on r.RegionCode = c.RegionCode
      order by convert(concat(u.Id, '-', c.Name, ' - ', r.Region) using cp1251);
    end if;
  end if;
end;
