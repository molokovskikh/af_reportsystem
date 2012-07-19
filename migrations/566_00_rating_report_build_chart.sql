insert into Reports.report_type_properties(ReportTypeCode,
	PropertyName,
	DisplayName,
	PropertyType,
	Optional,
	DefaultValue)
values (
	(select ReportTypeCode from Reports.ReportTypes where ReportTypeFilePrefix = 'RatingReport'),
	'BuildChart',
	'Сформировать диаграмму',
	'BOOL',
	0,
	'0'
)
