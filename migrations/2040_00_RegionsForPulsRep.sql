use reports;
update reports.report_type_properties p
join reports.reporttypes t on t.ReportTypeCode = p.ReportTypeCode
set p.PropertyType = 'LIST', p.DisplayName = 'Список значений "Региона"', p.Optional = 1, p.DefaultValue = 1, p.PropertyName = 'RegionEqual'
where p.PropertyName = 'RegionId'
and t.ReportTypeFilePrefix = 'PulsOrderReport';
