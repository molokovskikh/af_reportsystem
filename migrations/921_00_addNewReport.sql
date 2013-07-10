insert into reports.reporttypes (ReportTypeName, ReportTypeFilePrefix, AlternateSubject, ReportClassName)
value
('Отчет товаров поставщиков, подпадающих под действие матрицы', 'MatrixReport', 'Отчет товаров поставщиков, подпадающих под действие матрицы', 'Inforoom.ReportSystem.ByOffers.MatrixReport');

set @reportId = last_Insert_Id();

insert into reports.report_type_properties
(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, SelectStoredProcedure, DefaultValue)
values
(@reportId, 'ClientCode', 'Клиент', 'INT', false, 'GetClientsForMatrix', 0),
(@reportId, 'FirmCodeEqual', 'Список значений "Поставщик"', 'LIST', true, 'GetFirmCode', 0),
(@reportId, 'IgnoredSuppliers', 'Игнорируемые поставщики', 'LIST', true, 'GetFirmCode', 0),
(@reportId, 'PriceCodeEqual', 'Список значений "Прайс"', 'LIST', true, 'GetPriceCode', 0),
(@reportId, 'PriceCodeNonValues', 'Список исключений "Прайс"', 'LIST', true, 'GetPriceCode', 0),
(@reportId, 'RegionClientEqual', 'Список доступных клиенту регионов', 'LIST', true, 'GetRegionsForClient', 0)
;