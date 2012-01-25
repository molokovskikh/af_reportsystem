update Reports.report_type_properties
set SelectStoredProcedure = 'GetPriceCode'
where SelectStoredProcedure = 'GetAllPriceCode';
