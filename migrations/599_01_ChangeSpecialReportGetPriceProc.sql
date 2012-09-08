UPDATE reports.report_type_properties r SET SelectStoredProcedure='GetPricesByRegionMaskByTypes'
where ReportTypeCode=1
and PropertyName='PriceCodeEqual';