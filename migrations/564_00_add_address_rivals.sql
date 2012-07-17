insert into Reports.report_type_properties(ReportTypeCode,
	PropertyName,
	DisplayName,
	PropertyType,
	Optional)
values (
	(select ReportTypeCode from Reports.ReportTypes where ReportTypeFilePrefix = 'PharmacyMixed'),
	'AddressRivals',
	'Список адресов доставки конкурентов',
	'LIST',
	1
)
