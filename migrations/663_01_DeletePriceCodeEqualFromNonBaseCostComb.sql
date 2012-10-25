drop temporary table IF EXISTS reports.TmpReportCode;
CREATE temporary table reports.TmpReportCode(
  ReportCode bigint unsigned
  ) engine=MEMORY;
  
  insert into reports.TmpReportCode
  SELECT reportcode FROM reports.report_properties r where propertyID in 
  (select id from reports.report_type_properties rt where rt.reporttypecode=2 and propertyname='ByBaseCosts') and propertyvalue=0;
  
 delete from reports.report_properties where
PropertyID in (select id from reports.report_type_properties rt where rt.reporttypecode=2 and rt.propertyname='PriceCodeEqual') 
and reportcode in (select reportcode from reports.TmpReportCode);