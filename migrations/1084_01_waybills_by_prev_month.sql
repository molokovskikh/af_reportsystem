    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'ByPreviousMonth'/*?p1*/, 'За предыдущий месяц'/*?p2*/, 'BOOL'/*?p3*/, False/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);
