
    select
        reporttype0_.ReportTypeCode as ReportTy1_113_
    into @id
    from
        reports.reporttypes reporttype0_
    where
        reporttype0_.ReportClassName='Inforoom.ReportSystem.Model.WaybillsStatReport'/*?p0*/ limit 1/*?p1*/;


    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'ShowCode'/*?p1*/, 'Показывать код поставщика'/*?p2*/, 'BOOL'/*?p3*/, False/*?p4*/, '0'/*?p5*/, @id/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'ShowCodeCr'/*?p1*/, 'Показывать код изготовителя'/*?p2*/, 'BOOL'/*?p3*/, False/*?p4*/, '0'/*?p5*/, @id/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetFirmCode'/*?p0*/, 'SupplierId'/*?p1*/, 'Поставщик'/*?p2*/, 'INT'/*?p3*/, False/*?p4*/, '0'/*?p5*/, @id/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'ReportInterval'/*?p1*/, 'Интервал отчета (дни) от текущей даты'/*?p2*/, 'INT'/*?p3*/, False/*?p4*/, '0'/*?p5*/, @id/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();
