alter table reports.general_reports
  drop key `FirmCode`,
  DROP FOREIGN KEY `FirmCode`;

alter table reports.general_reports
  CHANGE COLUMN FirmCode FirmCode int(11) unsigned DEFAULT NULL,
  CHANGE COLUMN Allow Allow tinyint(1) not null default '0',
  drop column EMailAddress;


alter table reports.general_reports
  add column `Temporary` tinyint(1) not null default '0',
  add column `TemporaryCreationDate` datetime default null,
  add column `Comment` varchar(255) default null,
  add column `PayerID` int(10) unsigned default null,
  add constraint `general_reports_FK_PayerID` foreign key (`PayerID`) references `billing`.`payers` (`PayerID`) on delete cascade on update cascade;

update
  reports.general_reports
set
  Comment = EMailSubject;

update 
  reports.general_reports, 
  usersettings.clientsdata
set
  general_reports.PayerId = clientsdata.BillingCode
where
  general_reports.FirmCode = clientsdata.FirmCode;

alter table reports.general_reports
  add constraint `general_reports_FK_FirmCode` FOREIGN KEY (`FirmCode`) REFERENCES `usersettings`.`clientsdata` (`FirmCode`);

INSERT INTO general_reports
(GeneralReportCode, FirmCode, Allow, Comment, PayerID)
VALUES(142, 234, 1, 'Отчет для шаблонов', 921);

INSERT INTO reports.reporttypes
(ReportTypeCode, ReportTypeName, ReportTypeFilePrefix, AlternateSubject, ReportClassName)
VALUES(9, 'Рейтинг по поставщикам', 'ProviderRating', 'Рейтинговый отчет по поставщикам', 'Inforoom.ReportSystem.ProviderRatingReport');

insert into reports.report_type_properties
(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, PropertyEnumID, SelectStoredProcedure, DefaultValue)
values
(9, 'ByPreviousMonth', 'За предыдущий месяц', 'BOOL', b'0', null, '', '0'),
(9, 'ReportInterval', 'Интервал отчета (дни) от текущей даты', 'INT', b'0', null, '', '1'),
(9, 'ProviderCount', 'Количество поставщиков', 'INT', b'0', null, '', '10'),
(9, 'ProductNameEqual', 'Список значений "Наименования продукта"', 'LIST', b'1', null, 'GetProductId', '1'),
(9, 'ProductNameNonEqual', 'Список исключений "Наименования продукта"', 'LIST', b'1', null, 'GetProductId', '1'),
(9, 'FullNameEqual', 'Список значений "Полного наименования"', 'LIST', b'1', null, 'GetFullCode', '1'),
(9, 'FullNameNonEqual', 'Список исключений "Полного наименования"', 'LIST', b'1', null, 'GetFullCode', '1'),
(9, 'ShortNameEqual', 'Список значений "Наименования"', 'LIST', b'1', null, 'GetShortCode', '1'),
(9, 'ShortNameNonEqual', 'Список исключений "Наименования"', 'LIST', b'1', null, 'GetShortCode', '1'),
(9, 'FirmCrEqual', 'Список значений "Производителя"', 'LIST', b'1', null, 'GetFirmCr', '1'),
(9, 'FirmCrNonEqual', 'Список исключений "Производителя"', 'LIST', b'1', null, 'GetFirmCr', '1'),
(9, 'RegionEqual', 'Список значений "Региона"', 'LIST', b'1', null, 'GetRegion', '1'),
(9, 'RegionNonEqual', 'Список исключений "Региона"', 'LIST', b'1', null, 'GetRegion', '1'),
(9, 'FirmCodeEqual', 'Список значений "Поставщик"', 'LIST', b'1', null, 'GetFirmCode', '1'),
(9, 'FirmCodeNonEqual', 'Список исключений "Поставщик"', 'LIST', b'1', null, 'GetFirmCode', '1'),
(9, 'PriceCodeEqual', 'Список значений "Прайс"', 'LIST', b'1', null, 'GetAllPriceCode', '1'),
(9, 'PriceCodeNonEqual', 'Список исключений "Прайс"', 'LIST', b'1', null, 'GetAllPriceCode', '1'),
(9, 'ClientCodeEqual', 'Список значений "Клиент"', 'LIST', b'1', null, 'GetAllClientCode', '1'),
(9, 'ClientCodeNonEqual', 'Список исключений "Клиент"', 'LIST', b'1', null, 'GetAllClientCode', '1'),
(9, 'PayerEqual', 'Список значений "Плательщик"', 'LIST', b'1', null, 'GetPayerCode', '0'),
(9, 'PayerNonEqual', 'Список исключений "Плательщик"', 'LIST', b'1', null, 'GetPayerCode', '0');



use reports;

drop PROCEDURE GetClientPriceCode;


delimiter /


drop PROCEDURE `GetAllClientCode`
/

-- 
-- Definition for stored procedure GetAllClientCode
-- 
create definer=`RootDBMS`@`127.0.0.1` procedure `GetAllClientCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if (inID is not null) then
    select
      cd.FirmCode as ID,
      convert(concat(cd.FirmCode, '-', cd.ShortName) using cp1251) as DisplayValue
    from
      usersettings.clientsdata cd
    where
          cd.FirmCode = inID
      and cd.firmtype = 1
    order by cd.ShortName;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        cd.FirmCode as ID,
        convert(concat(cd.FirmCode, '-', cd.ShortName) using cp1251) as DisplayValue
      from
        usersettings.clientsdata cd
      where
           cd.ShortName like filterStr
        and cd.firmtype = 1
      order by cd.ShortName;
    else
      select
        cd.FirmCode as ID,
        convert(concat(cd.FirmCode, '-', cd.ShortName) using cp1251) as DisplayValue
      from
        usersettings.clientsdata cd
      where
            cd.firmtype = 1
      order by cd.ShortName;
    end if;
  end if;
end;
/


drop PROCEDURE `GetAllPriceCode`
/


-- 
-- Definition for stored procedure GetAllPriceCode
-- 
create definer=`RootDBMS`@`127.0.0.1` procedure `GetAllPriceCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  drop temporary table if exists tempGetPriceCode;
  create temporary table tempGetPriceCode
  engine=memory
  select
    pd.PriceCode as PriceCode,
    convert(concat(pd.PriceCode, ' - ', cd.ShortName, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) as PriceName
  from
    usersettings.pricesdata pd
    inner join usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
    inner join farm.regions rg on rg.RegionCode = cd.RegionCode
  where
      cd.FirmType = 0;
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
end;
/


drop PROCEDURE `GetClientCode`
/

-- 
-- Definition for stored procedure GetClientCode
-- 
create definer=`RootDBMS`@`127.0.0.1` procedure `GetClientCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if (inID is not null) then
    select
      cd.FirmCode as ID,
      convert(concat(cd.FirmCode, '-', cd.ShortName) using cp1251) as DisplayValue
    from
      usersettings.clientsdata cd
    where
          cd.FirmCode = inID
      and cd.firmtype = 1
      and cd.FirmStatus = 1
    order by cd.ShortName;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        cd.FirmCode as ID,
        convert(concat(cd.FirmCode, '-', cd.ShortName) using cp1251) as DisplayValue
      from
        usersettings.clientsdata cd
      where
           cd.ShortName like filterStr
        and cd.firmtype = 1
        and cd.FirmStatus = 1
      order by cd.ShortName;
    else
      select
        cd.FirmCode as ID,
        convert(concat(cd.FirmCode, '-', cd.ShortName) using cp1251) as DisplayValue
      from
        usersettings.clientsdata cd
      where
            cd.firmtype = 1
        and cd.FirmStatus = 1
      order by cd.ShortName;
    end if;
  end if;
end;
/


drop PROCEDURE `GetFirmCode`
/


-- 
-- Definition for stored procedure GetFirmCode
-- 
create definer=`RootDBMS`@`127.0.0.1` procedure `GetFirmCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if (inID is not null) then
    select
      cd.FirmCode as ID,
      convert(concat(cd.FirmCode, '-', cd.ShortName, ' - ', rg.Region) using cp1251) as DisplayValue
    from
      usersettings.clientsdata cd,
      farm.regions rg
    where
          cd.FirmCode = inID
      and cd.firmtype = 0
      and rg.RegionCode = cd.RegionCode
    order by cd.ShortName;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        cd.FirmCode as ID,
        convert(concat(cd.FirmCode, '-', cd.ShortName, ' - ', rg.Region) using cp1251) as DisplayValue
      from
        usersettings.clientsdata cd,
        farm.regions rg
      where
            cd.ShortName like filterStr
        and cd.firmtype = 0
        and rg.RegionCode = cd.RegionCode
      order by cd.ShortName;
    else
      select
        cd.FirmCode as ID,
        convert(concat(cd.FirmCode, '-', cd.ShortName, ' - ', rg.Region) using cp1251) as DisplayValue
      from
        usersettings.clientsdata cd,
        farm.regions rg
      where
            cd.firmtype = 0
        and rg.RegionCode = cd.RegionCode
      order by cd.ShortName;
    end if;
  end if;
end;
/


drop PROCEDURE `GetFirmCr`
/

-- 
-- Definition for stored procedure GetFirmCr
-- 
create definer=`RootDBMS`@`127.0.0.1` procedure `GetFirmCr`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if ((inFilter is not null) and (length(inFilter) > 0)) then
    set filterStr = concat('%', inFilter, '%');
    select
      c.CodeFirmCr as ID,
      c.FirmCr as DisplayValue
    from
      farm.catalogfirmcr c
    where
      c.FirmCr like filterStr
    order by 2;
  else
    select
      c.CodeFirmCr as ID,
      c.FirmCr as DisplayValue
    from
      farm.catalogfirmcr c
    order by 2;
  end if;
end;
/


drop PROCEDURE `GetFullCode`
/

-- 
-- Definition for stored procedure GetFullCode
-- 
create definer=`RootDBMS`@`127.0.0.1` procedure `GetFullCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if ((inFilter is not null) and (length(inFilter) > 0)) then
    set filterStr = concat('%', inFilter, '%');
    select
      c.Id as ID,
      concat(cn.Name, ' ', cf.Form) as DisplayValue
    from
      catalogs.catalog c,
      catalogs.catalognames cn,
      catalogs.catalogforms cf
    where
          c.Hidden = 0
      and cn.Id = c.NameId
      and cf.Id = c.FormId
      and concat(cn.Name, ' ', cf.Form) like filterStr
    order by 2;
  else
    select
      c.Id as ID,
      concat(cn.Name, ' ', cf.Form) as DisplayValue
    from
      catalogs.catalog c,
      catalogs.catalognames cn,
      catalogs.catalogforms cf
    where
      c.Hidden = 0
      and cn.Id = c.NameId
      and cf.Id = c.FormId
    order by 2;
  end if;
end;
/


drop PROCEDURE `GetPayerCode`
/

-- 
-- Definition for stored procedure GetPayerCode
-- 
create definer=`RootDBMS`@`127.0.0.1` procedure `GetPayerCode`(in inFilter varchar(255), in inID bigint)
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
      and exists(select * from usersettings.clientsdata cd where cd.BillingCode = p.PayerId)
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
        and exists(select * from usersettings.clientsdata cd where cd.BillingCode = p.PayerId)
      order by p.ShortName;
    else
      select
        p.PayerId as ID,
        convert(concat(p.PayerId, '-', p.ShortName) using cp1251) as DisplayValue
      from
        billing.payers p
      where
            exists(select * from usersettings.clientsdata cd where cd.BillingCode = p.PayerId)
      order by p.ShortName;
    end if;
  end if;
end;
/


drop PROCEDURE `GetPriceCode`
/

-- 
-- Definition for stored procedure GetPriceCode
-- 
create definer=`RootDBMS`@`127.0.0.1` procedure `GetPriceCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  drop temporary table if exists tempGetPriceCode;
  create temporary table tempGetPriceCode
  engine=memory
  select
    pd.PriceCode as PriceCode,
    convert(concat(pd.PriceCode, ' - ', cd.ShortName, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) as PriceName
  from
    usersettings.pricesdata pd
    inner join usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
    inner join farm.regions rg on rg.RegionCode = cd.RegionCode
  where
      cd.FirmType = 0
  and cd.FirmStatus = 1
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
end;
/


drop PROCEDURE `GetProductId`
/

-- 
-- Definition for stored procedure GetProductId
-- 
create definer=`RootDBMS`@`127.0.0.1` procedure `GetProductId`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if ((inFilter is not null) and (length(inFilter) > 0)) then
    set filterStr = concat('%', inFilter, '%');
    select
      p.Id as ID,
      concat(cn.Name, ' ', catalogs.GetFullForm(p.Id)) as DisplayValue
    from
      catalogs.products p,
      catalogs.catalog c,
      catalogs.catalognames cn,
      catalogs.catalogforms cf
    where
          p.Hidden = 0
      and c.Id = p.CatalogId
      and c.Hidden = 0
      and cn.Id = c.NameId
      and cf.Id = c.FormId
      and concat(cn.Name, ' ', cf.Form) like filterStr
    order by 2;
  else
    select
      p.Id as ID,
      concat(cn.Name, ' ', catalogs.GetFullForm(p.Id)) as DisplayValue
    from
      catalogs.products p,
      catalogs.catalog c,
      catalogs.catalognames cn,
      catalogs.catalogforms cf
    where
          p.Hidden = 0
      and c.Id = p.CatalogId
      and c.Hidden = 0
      and cn.Id = c.NameId
      and cf.Id = c.FormId
    order by 2;
  end if;
end;
/


drop PROCEDURE `GetRegion`
/

-- 
-- Definition for stored procedure GetRegion
-- 
create definer=`RootDBMS`@`127.0.0.1` procedure `GetRegion`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if ((inFilter is not null) and (length(inFilter) > 0)) then
    set filterStr = concat('%', inFilter, '%');
    select
      r.RegionCode as ID,
      r.Region as DisplayValue
    from
      farm.regions r
    where
      r.Region like filterStr
    order by r.Region;
  else
    select
      r.RegionCode as ID,
      r.Region as DisplayValue
    from
      farm.regions r
    order by r.Region;
  end if;
end;
/


drop PROCEDURE `GetShortCode`
/

-- 
-- Definition for stored procedure GetShortCode
-- 
create definer=`RootDBMS`@`127.0.0.1` procedure `GetShortCode`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if ((inFilter is not null) and (length(inFilter) > 0)) then
    set filterStr = concat('%', inFilter, '%');
    select
      c.ID as ID,
      c.Name as DisplayValue
    from
      farm.catalog_names c
    where
      c.Name like filterStr
    order by 2;
  else
    select
      c.ID as ID,
      c.Name as DisplayValue
    from
      farm.catalog_names c
    order by 2;
  end if;
end;
/


delimiter ;

