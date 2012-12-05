INSERT INTO reports.report_type_properties (ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
 VALUES(13, 'ByWeightCosts', 'По взвешенным ценам', 'BOOL', 0, 0);
INSERT INTO reports.report_type_properties (ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
 VALUES(13, 'MaxCostCount', 'Количество отображаемых цен', 'INT', 0, 3);
 
 UPDATE reports.report_type_properties r SET optional=1 where reporttypecode=13 and propertyname = 'PriceCodeEqual';