UPDATE reports.report_type_properties r SET Optional=1
where ReportTypeCode in (1,2,3)
and PropertyName='PriceCodeEqual';


UPDATE reports.report_type_properties r SET SelectStoredProcedure='GetPricesByRegionMaskByTypes'
where ReportTypeCode=2
and PropertyName='PriceCodeEqual';

UPDATE reports.report_type_properties r SET SelectStoredProcedure='GetPricesByRegionMaskByTypes'
where ReportTypeCode=3
and PropertyName='PriceCodeEqual';