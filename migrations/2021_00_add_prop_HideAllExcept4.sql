use reports;
insert into report_type_properties (ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
values (1, 'HideAllExcept4', 'Скрывать все колонки, кроме Код, Наименование, Производитель, Цена', 'BOOL', 0, 0);