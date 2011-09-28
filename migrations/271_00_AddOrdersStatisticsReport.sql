insert into reports.reporttypes(ReportTypeName, ReportTypeFilePrefix, AlternateSubject, ReportClassName)
values('Статистика заказов для бухгалтерии', 'OrdersStatistics', 'Статистика заказов для бухгалтерии', 'Inforoom.ReportSystem.ByOrders.OrdersStatistics');

SET @NewReportType = Last_Insert_ID();

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
values(@NewReportType, 'ByPreviousMonth', 'За предыдущий месяц', 'BOOL', 0, '1');

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
values(@NewReportType, 'DateFrom', 'Начало периода', 'DATETIME', 0, 'NOW');

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
values(@NewReportType, 'DateTo', 'Конец периода (включительно)', 'DATETIME', 0, 'NOW');




DROP PROCEDURE IF EXISTS orders.CalculateOrders;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` PROCEDURE orders.CalculateOrders(IN StartDate Date, IN EndDate Date)
BEGIN
SELECT 
    supps.Payer PayerId,
    supps.Name SupplierName,
    r.region, 
    round(sum(if(free.ClientPayerId is null,cost*quantity, 0)), 2) OrdersSum
FROM 
    (
    ordershead oh
    inner join usersettings.pricesdata pd on oh.pricecode=pd.pricecode
    inner join future.suppliers supps on pd.firmcode=supps.Id
    inner join farm.regions r on  oh.regioncode=r.regioncode
    inner join orderslist ol on ol.orderid=oh.rowid
    inner join usersettings.retclientsset rcs on rcs.clientcode=oh.clientcode
    )
    left join future.clients cl on cl.id=oh.clientcode
    left join billing.FreeOrders free on free.ClientPayerId=cl.PayerId and free.SupplierPayerId = supps.Payer
where 
    oh.writetime between '2011-09-01' and '2011-09-28'    
    and supps.segment = 0   
    and rcs.invisibleonfirm < 2
    and if(cl.id is not null, cl.PayerId!=921,1)
    and rcs.ServiceClient = 0
    and oh.deleted = 0
    and oh.processed = 1
    and oh.regionCode in ( 1, 2, 2048, 4, 8,32,64, 16384, 32768, 65536, 128,16777216, 33554432 )
group by supps.id, 
         r.regioncode
order by 2, 1;
END



