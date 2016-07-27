use reports;

insert into reports.reporttypes(ReportTypeName, ReportTypeFilePrefix, AlternateSubject, ReportClassName)
values ('Индивидуальный отчет для Пульс', 'PulsOrderReport', 'Индивидуальный отчет для Пульс', 'Inforoom.ReportSystem.ByOrders.PulsOrderReport');

set @lastId = LAST_INSERT_ID();

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, SelectStoredProcedure)
values (@lastId, 'SupplierId', 'Поставщик', 'INT', 0, 'GetFirmCode');

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, SelectStoredProcedure)
values (@lastId, 'RegionId', 'Регион', 'INT', 0, 'GetRegion');

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, Position)
values (@lastId, 'ReportInterval', 'Интервал отчета (дни) от текущей даты', 'INT', 0, 1, 0);

insert into reports.report_type_properties(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, PropertyEnumID, DefaultValue, Position)
values (@lastId, 'ReportPeriod', 'Период подготовки отчета', 'ENUM', 0, 17, 2, 0);
