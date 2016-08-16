use reports;

alter TABLE reports.report_type_properties 
change column PropertyType PropertyType ENUM('BOOL','INT','ENUM','LIST','STRING','DATETIME','FILE','PERCENT') NOT NULL DEFAULT 'INT';

insert into reports.report_type_properties (ReportTypeCode, PropertyName, DisplayName, PropertyType, Optional, DefaultValue)
values (15, 'ShareMoreThan', 'Не показывать записи с долей не более', 'PERCENT', 1, 0);