DELIMITER $$

-- Добавляем новый параметр к отчету и создаем его в каждом таком отчете
CREATE TRIGGER AddNewProperty AFTER Insert ON testreports.report_type_properties FOR EACH ROW
BEGIN
  if NEW.Optional = 0 then
    insert into report_properties (ReportCode, PropertyID, PropertyValue)
      select ReportCode, NEW.ID, NEW.DefaultValue
      FROM
        reports r
      where
        r.ReportTypeCode = NEW.ReportTypeCode;
  end if;
END;
$$

DELIMITER ;



DELIMITER $$

-- Добавляем новый отчет и создаем у него обязательные параметры
CREATE TRIGGER AddNewReport AFTER Insert ON testreports.reports FOR EACH ROW
BEGIN
  insert into report_properties (ReportCode, PropertyID, PropertyValue)
    select NEW.ReportCode, r.ID, r.DefaultValue
    FROM
      report_type_properties r
    where
          r.ReportTypeCode = NEW.ReportTypeCode
      and r.Optional = 0;
END;
$$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS `testreports`.`GetClientCode` $$
CREATE PROCEDURE `testreports`.`GetClientCode` (IN inFirmCode bigint, IN inFilter varchar(255))
BEGIN
  DECLARE filterStr varchar(257);
  if ((inFilter is not null) and (length(inFilter) > 0)) then
    set filterStr = concat('%', inFilter, '%');
    select
      cd.FirmCode as ID,
      cd.ShortName as DisplayValue
    from
      usersettings.clientsdata prod,
      usersettings.clientsdata cd
    where
          prod.FirmCode = inFirmCode
      and cd.firmsegment = prod.firmsegment
      and cd.firmtype <> prod.firmtype
      and cd.ShortName like filterStr;
  else
    select
      cd.FirmCode as ID,
      cd.ShortName as DisplayValue
    from
      usersettings.clientsdata prod,
      usersettings.clientsdata cd
    where
          prod.FirmCode = inFirmCode
      and cd.firmsegment = prod.firmsegment
      and cd.firmtype <> prod.firmtype;
  end if;
END $$

DELIMITER ;