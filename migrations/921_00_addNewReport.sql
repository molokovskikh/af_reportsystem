insert into reports.reporttypes (ReportTypeName, ReportTypeFilePrefix, AlternateSubject, ReportClassName)
value
('Отчет товаров поставщиков, подпадающих под действие матрицы', 'MatrixReport', 'Товары поставщиков, подпадающие под действие матрицы', 'Inforoom.ReportSystem.ByOffers.MatrixReport');

set @reportId = last_Insert_Id();

insert into reports.report_type_properties
(ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, SelectStoredProcedure)
value
(@reportId, 'ClientCode', 'Клиент', 'INT', false, 'GetClientsForMatrix');