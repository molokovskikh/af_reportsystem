insert into reports.property_enums(EnumName)
values('ReportPeriod');
select last_insert_id() into @id;
insert into reports.enum_values(PropertyEnumId, Value, DisplayValue)
values(@id, 0, 'За предыдущий месяц'),
	(@id, 1, 'За текущий день'),
	(@id, 2, 'Интервал отчета (дни) от текущей даты');

insert into Reports.Report_Type_Properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, PropertyEnumId)
select ReportTypeCode, 'ReportPeriod', 'Период подготовки отчета', 'ENUM', 0, 2, @id
from reports.reporttypes
where ReportClassName in ('Inforoom.ReportSystem.MixedReport', 'Inforoom.ReportSystem.BaseOrdersReport', 'Inforoom.ReportSystem.PharmacyMixedReport', 'Inforoom.ReportSystem.ProviderRatingReport', 'Inforoom.ReportSystem.RatingReport', 'Inforoom.ReportSystem.Model.WaybillsStatReport', 'Inforoom.ReportSystem.ByOrders.OrderOutAllowedAssortment', 'Inforoom.ReportSystem.ByOrders.OrdersStatistics', 'Inforoom.ReportSystem.ByOrders.SupplierOrdersStatistics', 'Inforoom.ReportSystem.ByOrders.WaybillsReport', 'Inforoom.ReportSystem.ByOrders.SupplierMarketShareByUser', 'Inforoom.ReportSystem.Models.Reports.OrderDetails', 'Inforoom.ReportSystem.ByOffers.CostDynamic')
;

create temporary table for_update engine=memory
select rr.* from reports.report_properties rr
join reports.report_type_properties tp on tp.Id = rr.PropertyId
where tp.PropertyName = 'ReportPeriod'
and exists(select *
from reports.report_properties r
join reports.report_type_properties rtp on rtp.Id = r.PropertyId
where r.ReportCode = rr.ReportCode and rtp.PropertyName = 'ByPreviousMonth' and r.PropertyValue = '1');

update reports.report_properties p
join for_update u on u.Id = p.Id
set p.PropertyValue = 0;

drop temporary table for_update;
create temporary table for_update engine=memory
select rr.* from reports.report_properties rr
join reports.report_type_properties tp on tp.Id = rr.PropertyId
where tp.PropertyName = 'ReportPeriod'
and exists(select *
from reports.report_properties r
join reports.report_type_properties rtp on rtp.Id = r.PropertyId
where r.ReportCode = rr.ReportCode and rtp.PropertyName = 'ByToday' and r.PropertyValue = '1');

update reports.report_properties p
join for_update u on u.Id = p.Id
set p.PropertyValue = 1;

delete p from Reports.report_type_properties p
join reports.reporttypes r on r.ReportTypeCode = p.ReportTypeCode
where r.ReportClassName in ('Inforoom.ReportSystem.MixedReport', 'Inforoom.ReportSystem.BaseOrdersReport', 'Inforoom.ReportSystem.PharmacyMixedReport', 'Inforoom.ReportSystem.ProviderRatingReport', 'Inforoom.ReportSystem.RatingReport', 'Inforoom.ReportSystem.Model.WaybillsStatReport', 'Inforoom.ReportSystem.ByOrders.OrderOutAllowedAssortment', 'Inforoom.ReportSystem.ByOrders.OrdersStatistics', 'Inforoom.ReportSystem.ByOrders.SupplierOrdersStatistics', 'Inforoom.ReportSystem.ByOrders.WaybillsReport', 'Inforoom.ReportSystem.ByOrders.SupplierMarketShareByUser', 'Inforoom.ReportSystem.Models.Reports.OrderDetails', 'Inforoom.ReportSystem.ByOffers.CostDynamic') and p.PropertyName in ('ByPreviousMonth', 'ByToday')
;
