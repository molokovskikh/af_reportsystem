insert into reports.report_type_properties(ReportTypeCode, PropertyName,
	DisplayName, PropertyType, Optional, PropertyEnumID,
	SelectStoredProcedure, DefaultValue)
select 24, PropertyName, DisplayName, PropertyType,
	Optional, PropertyEnumID, SelectStoredProcedure, DefaultValue
from Reports.report_type_properties
where ReportTypeCode = 9
and PropertyType = 'LIST'
and PropertyName <> 'RegionEqual'
and PropertyName <> 'RegionNonEqual';
