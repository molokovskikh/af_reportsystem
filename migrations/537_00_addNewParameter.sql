insert into reports.report_type_properties (ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, SelectStoredProcedure, DefaultValue) values
(6, 'AddressesList', 'Список значений "Адресов доставки"', 'LIST', 1, 'GetFirmCode', 0),
(8, 'AddressesList', 'Список значений "Адресов доставки"', 'LIST', 1, 'GetFirmCode', 0),
(11, 'AddressesList', 'Список значений "Адресов доставки"', 'LIST', 1, 'GetFirmCode', 0),
(6, 'AddressesNonList', 'Список исключений "Адресов доставки"', 'LIST', 1, 'GetFirmCode', 0),
(8, 'AddressesNonList', 'Список исключений "Адресов доставки"', 'LIST', 1, 'GetFirmCode', 0),
(11, 'AddressesNonList', 'Список исключений "Адресов доставки"', 'LIST', 1, 'GetFirmCode',0)
;