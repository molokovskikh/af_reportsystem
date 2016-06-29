alter table reports.report_type_properties
add column Position int not null default 100;

update reports.report_type_properties
set Position = 100;

update reports.report_type_properties
set Position = 0
where PropertyName in ('ReportPeriod', 'ReportInterval');

update reports.report_type_properties
set Position = 1
where PropertyName in ('StartDate', 'EndDate');

update reports.report_type_properties
set Position = 2
where PropertyName in ('RegionEqual');
