insert into reports.reporttypes
  (ReportTypeCode, ReportTypeName, ReportTypeFilePrefix, AlternateSubject, ReportClassName)
values
  (16, 'Заказ вне разрешенного ассортимента', 'OrderOutAllowedAssortment', 'Заказ вне разрешенного ассортимента', 'Inforoom.ReportSystem.ByOrders.OrderOutAllowedAssortment');

insert into reports.report_type_properties 
  (ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, SelectStoredProcedure, DefaultValue) 
values 
  (16, 'ClientCode', 'Клиент', 'INT', 0, 'GetClientWithMatrix', 1),
  (16, 'ByPreviousMonth', 'За предыдущий месяц', 'BOOL', 0, null, 0),
  (16, 'ReportInterval', 'Интервал отчета (дни) от текущей даты', 'INT', 0, null, 0);
  

DROP PROCEDURE IF EXISTS reports.`GetClientWithMatrix`;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE reports.`GetClientWithMatrix`(in inFilter varchar(255), in inID bigint)
begin
  declare filterStr varchar(257);
  if (inID is not null) then
      SELECT CL.Name as DisplayValue,
      R.ClientCode as ID
      FROM usersettings.RetClientsSet R, future.Clients CL
      where R.BuyingMatrixPriceId is not null and
      CL.ID = R.ClientCode and
      CL.ID = inID;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
        SELECT CL.Name as DisplayValue,
        R.ClientCode as ID
        FROM usersettings.RetClientsSet R, future.Clients CL
        where R.BuyingMatrixPriceId is not null and
        CL.ID = R.ClientCode and
        CL.Name like filterStr;
    else
      SELECT CL.Name as DisplayValue,
      R.ClientCode as ID
      FROM usersettings.RetClientsSet R, future.Clients CL
      where R.BuyingMatrixPriceId is not null and
      CL.ID = R.ClientCode;
    end if;
  end if;
end;