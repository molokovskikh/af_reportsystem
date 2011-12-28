alter table Reports.report_type_properties
change column PropertyType PropertyType enum('BOOL','INT','ENUM','LIST','STRING','DATETIME', 'FILE') NOT NULL DEFAULT 'INT';
