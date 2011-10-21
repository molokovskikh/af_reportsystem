insert into reports.reporttypes(ReportTypeName, ReportTypeFilePrefix, AlternateSubject, ReportClassName)
values('Статистика заказов по поставщику', 'SupplierOrdersStatistics', 'Статистика заказов по поставщику', 'Inforoom.ReportSystem.ByOrders.SupplierOrdersStatistics');

SET @NewReportTypeCode = Last_Insert_ID();

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
values(@NewReportTypeCode, 'ByPreviousMonth', 'За предыдущий месяц', 'BOOL', 0, '1');

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, SelectStoredProcedure, DefaultValue)
values(@NewReportTypeCode, 'SourceFirmCode', 'Поставщик', 'INT', 0, 'GetFirmCode', '0');


insert into reports.property_enums(EnumName)
values('Вариант отчета');

SET @NewEnumId = Last_Insert_ID();

#select @MaxEnumId := max(PropertyEnumId) from reports.enum_values;

insert into reports.enum_values(PropertyEnumID, Value, DisplayValue)
values(@NewEnumId, 1, 'Позаявочно');

insert into reports.enum_values(PropertyEnumID, Value, DisplayValue)
values(@NewEnumId, 2, 'Поклиентно');

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, PropertyEnumID, DefaultValue)
values(@NewReportTypeCode, 'ReportType', 'Вариант отчета', 'ENUM', 0, @NewEnumId, '1');

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
values(@NewReportTypeCode, 'DateFrom', 'Начало периода', 'DATETIME', 0, 'NOW');

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
values(@NewReportTypeCode, 'DateTo', 'Конец периода (включительно)', 'DATETIME', 0, 'NOW');


insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, SelectStoredProcedure, DefaultValue)
values(@NewReportTypeCode, 'RegionEqual', 'Список значений "Региона"', 'LIST', 1, 'GetRegion', '0');
