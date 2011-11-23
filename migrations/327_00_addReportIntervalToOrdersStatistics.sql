delete from reports.report_type_properties where ReportTypeCode = 20 and PropertyName in ('DateFrom', 'DateTo');
delete from reports.report_type_properties where ReportTypeCode = 22 and PropertyName in ('DateFrom', 'DateTo');

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
values(20, 'ReportInterval', 'Интервал отчета (дни) от текущей даты', 'INT', 0, '30');

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
values(22, 'ReportInterval', 'Интервал отчета (дни) от текущей даты', 'INT', 0, '30');