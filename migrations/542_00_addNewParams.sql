delete from reports.report_type_properties
where id in (451,452,453,454,455,456);

insert into reports.report_type_properties (ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, SelectStoredProcedure, DefaultValue) values
(6, 'AddressesEqual', 'Список значений "Адресов доставки"', 'LIST', 1, 'GetFirmCode', 0),
(8, 'AddressesEqual', 'Список значений "Адресов доставки"', 'LIST', 1, 'GetFirmCode', 0),
(11, 'AddressesEqual', 'Список значений "Адресов доставки"', 'LIST', 1, 'GetFirmCode', 0),
(6, 'AddressesNonEqual', 'Список исключений "Адресов доставки"', 'LIST', 1, 'GetFirmCode', 0),
(8, 'AddressesNonEqual', 'Список исключений "Адресов доставки"', 'LIST', 1, 'GetFirmCode', 0),
(11, 'AddressesNonEqual', 'Список исключений "Адресов доставки"', 'LIST', 1, 'GetFirmCode',0),
(11, 'AddressesPosition', 'Позиция "Адресов доставки" в отчете', 'INT', 1, '',7),
(8, 'AddressesPosition', 'Позиция "Адресов доставки" в отчете', 'INT', 1, '',7),
(6, 'AddressesPosition', 'Позиция "Адресов доставки" в отчете', 'INT', 1, '',7)
;