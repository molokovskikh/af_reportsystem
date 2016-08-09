use reports;
update reports.report_type_properties p
join reporttypes t on t.ReportTypeCode = p.ReportTypeCode
set p.Optional = 0, p.PropertyName = 'Regions', p.DisplayName = 'Регионы' 
where t.ReportTypeFilePrefix = 'PulsOrderReport'
and p.SelectStoredProcedure = 'GetRegion';