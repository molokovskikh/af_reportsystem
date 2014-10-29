INSERT
INTO
    reports.report_type_properties
    (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
VALUES
    (NULL/*?p0*/,
'SupplierProductCodePosition'/*?p1*/,
'Позиция "Оригинальный код товара" в отчете'/*?p2*/,
'INT'/*?p3*/,
True/*?p4*/,
'0'/*?p5*/,
(select ReportTypeCode from reports.reporttypes where ReportTypeFilePrefix = 'OrdersStatistics'),
NULL/*?p7*/);


INSERT
INTO
    reports.report_type_properties
    (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
VALUES
    (NULL/*?p0*/,
'SupplierProductCodePosition'/*?p1*/,
'Позиция "Оригинальный код товара" в отчете'/*?p2*/,
'INT'/*?p3*/,
True/*?p4*/,
'0'/*?p5*/,
(select ReportTypeCode from reports.reporttypes where ReportTypeFilePrefix = 'Mixed'),
NULL/*?p7*/);


INSERT
INTO
    reports.report_type_properties
    (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
VALUES
    (NULL/*?p0*/,
'SupplierProductCodePosition'/*?p1*/,
'Позиция "Оригинальный код товара" в отчете'/*?p2*/,
'INT'/*?p3*/,
True/*?p4*/,
'0'/*?p5*/,
(select ReportTypeCode from reports.reporttypes where ReportTypeFilePrefix = 'PharmacyMixed'),
NULL/*?p7*/);

INSERT
INTO
    reports.report_type_properties
    (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
VALUES
    (NULL/*?p0*/,
		'SupplierProductCodePosition'/*?p1*/,
		'Позиция "Оригинальный код товара" в отчете'/*?p2*/,
		'INT'/*?p3*/,
		True/*?p4*/,
		'0'/*?p5*/,
(select ReportTypeCode from reports.reporttypes where ReportTypeFilePrefix = 'Rating'),
		NULL/*?p7*/);
