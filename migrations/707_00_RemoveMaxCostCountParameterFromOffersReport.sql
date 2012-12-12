 delete from reports.report_type_properties where ReportTypeCode=13 and PropertyName='MaxCostCount' and DisplayName='Количество отображаемых цен';
 
 delete from reports.report_type_properties where ReportTypeCode=13 and PropertyName='ByWeightCosts' and DisplayName='По взвешенным ценам';
 
 UPDATE reports.report_type_properties r SET optional=0 where reporttypecode=13 and propertyname = 'PriceCodeEqual';