    /* insert ReportTuner.Models.ReportType
        */ INSERT
        INTO
            reports.reporttypes
            (ReportTypeName, ReportClassName, AlternateSubject, ReportTypeFilePrefix)
        VALUES
            ('Статистика накладных'/*?p0*/, 'Inforoom.ReportSystem.Model.WaybillsStatReport'/*?p1*/, 'Статистика накладных'/*?p2*/, 'WaybillsStatReport'/*?p3*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'ProductNamePosition'/*?p1*/, 'Позиция "Наименование и форма выпуска" в отчете'/*?p2*/, 'INT'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetProductId'/*?p0*/, 'ProductNameNonEqual'/*?p1*/, 'Список исключений "Наименование и форма выпуска"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetProductId'/*?p0*/, 'ProductNameEqual'/*?p1*/, 'Список значений "Наименование и форма выпуска"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'FullNamePosition'/*?p1*/, 'Позиция "Наименование и форма выпуска" в отчете'/*?p2*/, 'INT'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetFullCode'/*?p0*/, 'FullNameNonEqual'/*?p1*/, 'Список исключений "Наименование и форма выпуска"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetFullCode'/*?p0*/, 'FullNameEqual'/*?p1*/, 'Список значений "Наименование и форма выпуска"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'ShortNamePosition'/*?p1*/, 'Позиция "Наименование" в отчете'/*?p2*/, 'INT'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetShortCode'/*?p0*/, 'ShortNameNonEqual'/*?p1*/, 'Список исключений "Наименование"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetShortCode'/*?p0*/, 'ShortNameEqual'/*?p1*/, 'Список значений "Наименование"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'MnnPosition'/*?p1*/, 'Позиция "МНН" в отчете'/*?p2*/, 'INT'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'MnnNonEqual'/*?p1*/, 'Список исключений "МНН"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'MnnEqual'/*?p1*/, 'Список значений "МНН"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'FirmCrPosition'/*?p1*/, 'Позиция "Производитель" в отчете'/*?p2*/, 'INT'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetFirmCr'/*?p0*/, 'FirmCrNonEqual'/*?p1*/, 'Список исключений "Производитель"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetFirmCr'/*?p0*/, 'FirmCrEqual'/*?p1*/, 'Список значений "Производитель"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'RegionPosition'/*?p1*/, 'Позиция "Регион" в отчете'/*?p2*/, 'INT'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetRegion'/*?p0*/, 'RegionNonEqual'/*?p1*/, 'Список исключений "Регион"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetRegion'/*?p0*/, 'RegionEqual'/*?p1*/, 'Список значений "Регион"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'FirmCodePosition'/*?p1*/, 'Позиция "Поставщик" в отчете'/*?p2*/, 'INT'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetFirmCode'/*?p0*/, 'FirmCodeNonEqual'/*?p1*/, 'Список исключений "Поставщик"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetFirmCode'/*?p0*/, 'FirmCodeEqual'/*?p1*/, 'Список значений "Поставщик"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'ClientCodePosition'/*?p1*/, 'Позиция "Аптека" в отчете'/*?p2*/, 'INT'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetAllClientCode'/*?p0*/, 'ClientCodeNonEqual'/*?p1*/, 'Список исключений "Аптека"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetAllClientCode'/*?p0*/, 'ClientCodeEqual'/*?p1*/, 'Список значений "Аптека"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'PayerPosition'/*?p1*/, 'Позиция "Плательщик" в отчете'/*?p2*/, 'INT'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetPayerCode'/*?p0*/, 'PayerNonEqual'/*?p1*/, 'Список исключений "Плательщик"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetPayerCode'/*?p0*/, 'PayerEqual'/*?p1*/, 'Список значений "Плательщик"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            (NULL/*?p0*/, 'AddressesPosition'/*?p1*/, 'Позиция "Адрес доставки" в отчете'/*?p2*/, 'INT'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetFirmCode'/*?p0*/, 'AddressesNonEqual'/*?p1*/, 'Список исключений "Адрес доставки"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();

    /* insert ReportTuner.Models.ReportTypeProperty
        */ INSERT
        INTO
            reports.report_type_properties
            (SelectStoredProcedure, PropertyName, DisplayName, PropertyType, Optional, DefaultValue, ReportTypeCode, PropertyEnumId)
        VALUES
            ('GetFirmCode'/*?p0*/, 'AddressesEqual'/*?p1*/, 'Список значений "Адрес доставки"'/*?p2*/, 'LIST'/*?p3*/, True/*?p4*/, '0'/*?p5*/, 30/*?p6*/, NULL/*?p7*/);

    SELECT
        LAST_INSERT_ID();
