INSERT
INTO
    reports.report_type_properties
    (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
VALUES
    ('GetRegion'/*?p0*/, 'RegionNonEqual'/*?p1*/, 'Список исключений "Регион"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 20/*?p6*/, NULL/*?p7*/);

INSERT
INTO
    reports.report_type_properties
    (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
VALUES
    ('GetRegion'/*?p0*/, 'RegionEqual'/*?p1*/, 'Список значений "Регион"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 20/*?p6*/, NULL/*?p7*/);
