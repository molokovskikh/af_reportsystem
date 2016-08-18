use reports;
insert into reports.report_type_properties (ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, SelectStoredProcedure, DefaultValue)
values (5, 'FirmCodeEqual2', 'Оставить только позиции с мин. ценами выбранных поставщиков', 'LIST', 1, 'GetFirmCode', 0);
