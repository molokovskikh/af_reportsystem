use reports;
update reports.report_type_properties
set SelectStoredProcedure = 'GetAllFirmCode'
where ReportTypeCode = 5
and PropertyName = 'FirmCodeEqual2';