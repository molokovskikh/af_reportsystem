insert into Reports.report_type_properties(ReportTypeCode,
	PropertyName,
	DisplayName,
	PropertyType,
	Optional,
	DefaultValue)
values (
	(select ReportTypeCode from Reports.ReportTypes where ReportTypeFilePrefix = 'Rating'),
	'DoNotShowAbsoluteValues',
	'Скрывать все колонки кроме \'Доля рынка в %\' и \'Доля от общего заказа в %\'',
	'BOOL',
	0,
	'0'
)
