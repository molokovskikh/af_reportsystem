insert into Reports.report_type_properties(ReportTypeCode,
	PropertyName,
	DisplayName,
	PropertyType,
	Optional,
	DefaultValue)
values (
	(select ReportTypeCode from Reports.ReportTypes where ReportTypeFilePrefix = 'PharmacyMixed'),
	'AddressRivals',
	'Список адресов доставки конкурентов',
	'LIST',
	1,
	'0'
)
